// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.UTF8;

        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");

        AaruLogging.Debug(MODULE_NAME,
                          "Version: {0}.{1}, Block size: {2} bytes",
                          _superblock.major_ver,
                          _superblock.minor_ver,
                          _blockSize);

        AaruLogging.Debug(MODULE_NAME,
                          "Blocks: {0}, Segments: {1}, Sections: {2}",
                          _superblock.block_count,
                          _superblock.segment_count,
                          _superblock.section_count);

        errno = ReadCheckpoint();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading checkpoint: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Checkpoint read successfully, version: {0}", _checkpoint.checkpoint_ver);

        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory loaded successfully, {0} entries cached",
                          _rootDirectoryCache.Count);

        Metadata = new FileSystem
        {
            Clusters               = _superblock.block_count,
            ClusterSize            = _blockSize,
            Type                   = FS_TYPE,
            VolumeName             = StringHandlers.CToString(_superblock.volume_name, Encoding.Unicode, true),
            VolumeSerial           = _superblock.uuid.ToString(),
            SystemIdentifier       = StringHandlers.CToString(_superblock.version,      Encoding.ASCII),
            DataPreparerIdentifier = StringHandlers.CToString(_superblock.init_version, Encoding.ASCII),
            FreeClusters           = _checkpoint.free_segment_count * _blocksPerSegment
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting filesystem...");

        _rootDirectoryCache.Clear();
        _superblock     = default(Superblock);
        _checkpoint     = default(Checkpoint);
        _natBitmap      = null;
        _checkpointData = null;
        _imagePlugin    = null;
        _encoding       = null;
        _mounted        = false;

        AaruLogging.Debug(MODULE_NAME, "Filesystem unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the F2FS superblock</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        uint sectorSize = _imagePlugin.Info.SectorSize;

        if(sectorSize is < F2FS_MIN_SECTOR or > F2FS_MAX_SECTOR)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid sector size: {0}", sectorSize);

            return ErrorNumber.InvalidArgument;
        }

        uint sbAddr = F2FS_SUPER_OFFSET / sectorSize;

        if(sbAddr == 0) sbAddr = 1;

        var sbSize = (uint)(Marshal.SizeOf<Superblock>() / sectorSize);

        if(Marshal.SizeOf<Superblock>() % sectorSize != 0) sbSize++;

        if(_partition.Start + sbAddr + sbSize >= _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock extends beyond partition end");

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno =
            _imagePlugin.ReadSectors(_partition.Start + sbAddr, false, sbSize, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

            return errno;
        }

        if(sector.Length < Marshal.SizeOf<Superblock>())
        {
            AaruLogging.Debug(MODULE_NAME, "Read buffer too small for superblock");

            return ErrorNumber.InvalidArgument;
        }

        _superblock = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

        if(_superblock.magic != F2FS_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid superblock magic: 0x{0:X8} (expected 0x{1:X8})",
                              _superblock.magic,
                              F2FS_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        _blockSize        = (uint)(1 << (int)_superblock.log_blocksize);
        _blocksPerSegment = (uint)(1 << (int)_superblock.log_blocks_per_seg);

        AaruLogging.Debug(MODULE_NAME,
                          "Superblock valid. Block size: {0}, Blocks/segment: {1}",
                          _blockSize,
                          _blocksPerSegment);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and caches the root directory entries</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory, root_ino={0}", _superblock.root_ino);

        // Resolve root inode block address via NAT
        ErrorNumber errno = LookupNat(_superblock.root_ino, out uint rootBlockAddr);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error looking up root inode in NAT: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root inode block address: {0}", rootBlockAddr);

        if(rootBlockAddr == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode block address is NULL_ADDR");

            return ErrorNumber.InvalidArgument;
        }

        // Read the root inode node block
        errno = ReadBlock(rootBlockAddr, out byte[] nodeBlock);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode block: {0}", errno);

            return errno;
        }

        // Parse the inode from the node block
        Inode rootInode = Marshal.ByteArrayToStructureLittleEndian<Inode>(nodeBlock);

        // Validate it's a directory
        // S_IFDIR = 0x4000 in POSIX mode bits
        if((rootInode.i_mode & 0xF000) != 0x4000)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (mode: 0x{0:X4})", rootInode.i_mode);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root inode valid: mode=0x{0:X4}, size={1}, inline=0x{2:X2}",
                          rootInode.i_mode,
                          rootInode.i_size,
                          rootInode.i_inline);

        // Check if this directory uses inline dentry
        if((rootInode.i_inline & F2FS_INLINE_DENTRY) != 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Root directory uses inline dentry");

            ParseInlineDentry(rootInode, nodeBlock);
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME, "Root directory uses regular dentry blocks");

            errno = ParseRegularDentry(rootInode);

            if(errno != ErrorNumber.NoError) return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded, {0} entries cached", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}