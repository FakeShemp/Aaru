// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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

using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AtheOS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");
        AaruLogging.Debug(MODULE_NAME, "Volume name: {0}",      StringHandlers.CToString(_superblock.name, _encoding));
        AaruLogging.Debug(MODULE_NAME, "Block size: {0} bytes", _superblock.block_size);

        AaruLogging.Debug(MODULE_NAME,
                          "Total blocks: {0} ({1} bytes)",
                          _superblock.num_blocks,
                          _superblock.num_blocks * _superblock.block_size);

        AaruLogging.Debug(MODULE_NAME,
                          "Used blocks: {0} ({1} bytes)",
                          _superblock.used_blocks,
                          _superblock.used_blocks * _superblock.block_size);

        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded successfully");

        Metadata = new FileSystem
        {
            Clusters     = (ulong)_superblock.num_blocks,
            ClusterSize  = _superblock.block_size,
            Dirty        = _superblock.flags == AFS_FLAG_DIRTY,
            FreeClusters = (ulong)(_superblock.num_blocks - _superblock.used_blocks),
            Type         = FS_TYPE,
            VolumeName   = StringHandlers.CToString(_superblock.name, _encoding)
        };

        AaruLogging.Debug(MODULE_NAME,
                          "Mount complete. Dirty: {0}, Free clusters: {1}",
                          Metadata.Dirty,
                          Metadata.FreeClusters);

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting filesystem...");

        // Clear cached root directory entries
        _rootDirectoryCache.Clear();

        // Clear superblock data
        _superblock = default(SuperBlock);

        // Clear plugin reference
        _imagePlugin = null;

        // Clear encoding reference
        _encoding = null;

        // Reset mounted flag
        _mounted = false;

        AaruLogging.Debug(MODULE_NAME, "Filesystem unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the filesystem superblock</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");
        AaruLogging.Debug(MODULE_NAME, "Sector size: {0} bytes", _imagePlugin.Info.SectorSize);

        // AtheOS superblock is at block 1 (after 1024-byte boot block)
        // The superblock itself is 1024 bytes

        uint sectorSize             = _imagePlugin.Info.SectorSize;
        long superblockByteOffset   = AFS_BOOTBLOCK_SIZE;
        var  superblockSectorOffset = (ulong)(superblockByteOffset / sectorSize);
        var  offsetInSector         = (int)(superblockByteOffset   % sectorSize);

        // How many sectors do we need to read to get the full superblock?
        int sectorsNeeded = (offsetInSector + (int)AFS_SUPERBLOCK_SIZE + (int)sectorSize - 1) / (int)sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + superblockSectorOffset,
                                                     false,
                                                     (uint)sectorsNeeded,
                                                     out byte[] sbSector,
                                                     out SectorStatus[] _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sectors: {0}", errno);

            return errno;
        }

        // Extract the superblock from the read data
        var sbData = new byte[AFS_SUPERBLOCK_SIZE];
        Array.Copy(sbSector, offsetInSector, sbData, 0, AFS_SUPERBLOCK_SIZE);

        // Check magic - AtheOS is always little-endian
        var magic1 = BitConverter.ToUInt32(sbData, 0x20);

        AaruLogging.Debug(MODULE_NAME, "Magic1 at offset 0x20: 0x{0:X8}", magic1);

        if(magic1 != AFS_MAGIC1)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid magic1! Expected 0x{0:X8}, got 0x{1:X8}", AFS_MAGIC1, magic1);

            return ErrorNumber.InvalidArgument;
        }

        _superblock = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sbData);

        AaruLogging.Debug(MODULE_NAME, "Validating superblock...");

        // Validate superblock
        if(_superblock.magic1                != AFS_MAGIC1 ||
           _superblock.magic2                != AFS_MAGIC2 ||
           _superblock.magic3                != AFS_MAGIC3 ||
           _superblock.root_dir_len          != 1          ||
           1 << (int)_superblock.block_shift != _superblock.block_size)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock validation failed!");

            AaruLogging.Debug(MODULE_NAME, "  magic1: 0x{0:X8} (expected 0x{1:X8})", _superblock.magic1, AFS_MAGIC1);

            AaruLogging.Debug(MODULE_NAME, "  magic2: 0x{0:X8} (expected 0x{1:X8})", _superblock.magic2, AFS_MAGIC2);

            AaruLogging.Debug(MODULE_NAME, "  magic3: 0x{0:X8} (expected 0x{1:X8})", _superblock.magic3, AFS_MAGIC3);

            AaruLogging.Debug(MODULE_NAME, "  root_dir_len: {0} (expected 1)", _superblock.root_dir_len);

            AaruLogging.Debug(MODULE_NAME,
                              "  block_shift: {0}, block_size: {1} (1 << {0} should equal {1})",
                              _superblock.block_shift,
                              _superblock.block_size);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock validation successful!");

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and parses the root directory</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        AaruLogging.Debug(MODULE_NAME,
                          "Root dir block_run: ag={0}, start={1}, len={2}",
                          _superblock.root_dir_ag,
                          _superblock.root_dir_start,
                          _superblock.root_dir_len);

        AaruLogging.Debug(MODULE_NAME,
                          "Superblock info: blocks_per_ag={0}, ag_shift={1}, block_size={2}, inode_size={3}",
                          _superblock.blocks_per_ag,
                          _superblock.ag_shift,
                          _superblock.block_size,
                          _superblock.inode_size);

        // Calculate the block address: (ag * blocks_per_ag) + start
        long blockAddress = (long)_superblock.root_dir_ag * _superblock.blocks_per_ag + _superblock.root_dir_start;

        AaruLogging.Debug(MODULE_NAME,
                          "Block calculation: AG {0} * {1} + start {2} = block {3}",
                          _superblock.root_dir_ag,
                          _superblock.blocks_per_ag,
                          _superblock.root_dir_start,
                          blockAddress);

        // Read the root directory inode
        ErrorNumber errno = ReadInode(blockAddress, out Inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root directory inode: {0}", errno);

            return errno;
        }

        // Validate root inode
        if(rootInode.magic1 != INODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid root i-node magic! Expected 0x{0:X8}, got 0x{1:X8}",
                              INODE_MAGIC,
                              rootInode.magic1);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Root i-node valid. Data stream size: {0} bytes", rootInode.data.size);

        // Parse the B+tree from the root directory's data stream
        errno = ParseDirectoryBTree(rootInode.data, out Dictionary<string, long> rootDirEntries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error parsing root directory B+tree: {0}", errno);

            return errno;
        }

        // Cache the root directory entries
        foreach(KeyValuePair<string, long> kvp in rootDirEntries) _rootDirectoryCache[kvp.Key] = kvp.Value;

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory B+tree parsed successfully. Cached {0} entries",
                          _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}