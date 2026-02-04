// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX6
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        // Read and validate the superblock
        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");

        // Load the root directory
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory loaded successfully with {0} entries",
                          _rootDirectoryCache.Count);

        Metadata = new FileSystem
        {
            Clusters         = _superblock.sb_num_blocks,
            ClusterSize      = _blockSize,
            Type             = FS_TYPE,
            FreeClusters     = _superblock.sb_free_blocks,
            Files            = _superblock.sb_num_inodes - _superblock.sb_free_inodes,
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(_superblock.sb_ctime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(_superblock.sb_atime),
            VolumeSerial     = $"{_superblock.sb_serial:X16}"
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Mount complete");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _rootDirectoryCache.Clear();
        _mounted      = false;
        _imagePlugin  = null;
        _partition    = default(Partition);
        _superblock   = default(qnx6_super_block);
        _encoding     = null;
        Metadata      = null;
        _isAudiMmi    = false;
        _littleEndian = true;
        _blockSize    = 0;
        _blockOffset  = 0;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the filesystem superblock</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        // First try to read Audi MMI superblock at partition start
        ErrorNumber errno = TryReadAudiMmiSuperblock();

        if(errno == ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Audi MMI superblock found");
            _isAudiMmi = true;

            return ErrorNumber.NoError;
        }

        // Try standard QNX6 superblock with boot blocks
        errno = TryReadStandardSuperblock(true);

        if(errno == ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Standard QNX6 superblock found (with boot blocks)");

            return ErrorNumber.NoError;
        }

        // Try standard QNX6 superblock without boot blocks
        errno = TryReadStandardSuperblock(false);

        if(errno == ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Standard QNX6 superblock found (without boot blocks)");

            return ErrorNumber.NoError;
        }

        AaruLogging.Debug(MODULE_NAME, "No valid QNX6 superblock found");

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Tries to read and validate an Audi MMI superblock at partition start</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber TryReadAudiMmiSuperblock()
    {
        // Audi MMI superblock is at partition start
        uint sectorsToRead = (QNX6_SUPERBLOCK_SIZE + _imagePlugin.Info.SectorSize - 1) / _imagePlugin.Info.SectorSize;

        ErrorNumber errno =
            _imagePlugin.ReadSectors(_partition.Start, false, sectorsToRead, out byte[] sbSector, out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(sbSector.Length < QNX6_SUPERBLOCK_SIZE) return ErrorNumber.InvalidArgument;

        qnx6_mmi_super_block mmiSb = Marshal.ByteArrayToStructureLittleEndian<qnx6_mmi_super_block>(sbSector);

        // Check magic
        if(mmiSb.sb_magic != QNX6_MAGIC) return ErrorNumber.InvalidArgument;

        // Validate checksum (CRC32 from byte 8 to byte 512)
        uint calculatedCrc = CalculateCrc32Be(sbSector, 8, 504);

        if(mmiSb.sb_checksum != calculatedCrc)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Audi MMI superblock checksum mismatch: expected {0:X8}, got {1:X8}",
                              mmiSb.sb_checksum,
                              calculatedCrc);

            return ErrorNumber.InvalidArgument;
        }

        // Convert MMI superblock to standard superblock format for internal use
        _superblock = new qnx6_super_block
        {
            sb_magic       = mmiSb.sb_magic,
            sb_checksum    = mmiSb.sb_checksum,
            sb_serial      = mmiSb.sb_serial,
            sb_blocksize   = mmiSb.sb_blocksize,
            sb_num_inodes  = mmiSb.sb_num_inodes,
            sb_free_inodes = mmiSb.sb_free_inodes,
            sb_num_blocks  = mmiSb.sb_num_blocks,
            sb_free_blocks = mmiSb.sb_free_blocks,
            Inode          = mmiSb.Inode,
            Bitmap         = mmiSb.Bitmap,
            Longfile       = mmiSb.Longfile,
            Unknown        = mmiSb.Unknown
        };

        _blockSize    = mmiSb.sb_blocksize;
        _blockOffset  = 0;
        _littleEndian = true; // Audi MMI is always little-endian

        // Sanity checks from Linux kernel
        if(_superblock.Inode.levels > QNX6_PTR_MAX_LEVELS)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Too many inode levels: {0} (max {1})",
                              _superblock.Inode.levels,
                              QNX6_PTR_MAX_LEVELS);

            return ErrorNumber.InvalidArgument;
        }

        if(_superblock.Longfile.levels > QNX6_PTR_MAX_LEVELS)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Too many longfile levels: {0} (max {1})",
                              _superblock.Longfile.levels,
                              QNX6_PTR_MAX_LEVELS);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Audi MMI superblock validated successfully");
        AaruLogging.Debug(MODULE_NAME, "Block size: {0}", _blockSize);
        AaruLogging.Debug(MODULE_NAME, "Num blocks: {0}", _superblock.sb_num_blocks);
        AaruLogging.Debug(MODULE_NAME, "Num inodes: {0}", _superblock.sb_num_inodes);

        return ErrorNumber.NoError;
    }

    /// <summary>Tries to read and validate a standard QNX6 superblock</summary>
    /// <param name="withBootBlocks">Whether to expect boot blocks before superblock</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber TryReadStandardSuperblock(bool withBootBlocks)
    {
        uint bootBlockOffset = withBootBlocks ? QNX6_BOOT_BLOCKS_SIZE : 0;

        // Calculate sector offset for superblock
        ulong sbSectorOffset = bootBlockOffset                                           / _imagePlugin.Info.SectorSize;
        uint  sectorsToRead  = (QNX6_SUPERBLOCK_SIZE + _imagePlugin.Info.SectorSize - 1) / _imagePlugin.Info.SectorSize;

        if(_partition.Start + sbSectorOffset >= _partition.End) return ErrorNumber.InvalidArgument;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + sbSectorOffset,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] sbSector,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(sbSector.Length < QNX6_SUPERBLOCK_SIZE) return ErrorNumber.InvalidArgument;

        // Try little-endian first
        qnx6_super_block sb1 = Marshal.ByteArrayToStructureLittleEndian<qnx6_super_block>(sbSector);

        // Check magic
        if(sb1.sb_magic == QNX6_MAGIC)
        {
            _littleEndian = true;
            AaruLogging.Debug(MODULE_NAME, "Little-endian filesystem detected");
        }
        else
        {
            // Try big-endian
            sb1 = Marshal.ByteArrayToStructureBigEndian<qnx6_super_block>(sbSector);

            if(sb1.sb_magic == QNX6_MAGIC)
            {
                _littleEndian = false;
                AaruLogging.Debug(MODULE_NAME, "Big-endian filesystem detected");
            }
            else
                return ErrorNumber.InvalidArgument;
        }

        // Validate checksum (CRC32-BE from byte 8 to byte 512)
        uint calculatedCrc = CalculateCrc32Be(sbSector, 8, 504);

        if(sb1.sb_checksum != calculatedCrc)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Superblock #1 checksum mismatch: expected {0:X8}, got {1:X8}",
                              sb1.sb_checksum,
                              calculatedCrc);

            return ErrorNumber.InvalidArgument;
        }

        _blockSize = sb1.sb_blocksize;

        // Calculate block offset
        _blockOffset = (bootBlockOffset + QNX6_SUPERBLOCK_AREA) / _blockSize;

        // Try to read and validate second superblock
        ulong sb2BlockOffset  = sb1.sb_num_blocks + _blockOffset;
        ulong sb2SectorOffset = sb2BlockOffset * _blockSize / _imagePlugin.Info.SectorSize;

        qnx6_super_block sb2      = default;
        var              sb2Valid = false;

        if(_partition.Start + sb2SectorOffset < _partition.End)
        {
            errno = _imagePlugin.ReadSectors(_partition.Start + sb2SectorOffset,
                                             false,
                                             sectorsToRead,
                                             out byte[] sb2Sector,
                                             out _);

            if(errno == ErrorNumber.NoError && sb2Sector.Length >= QNX6_SUPERBLOCK_SIZE)
            {
                sb2 = _littleEndian
                          ? Marshal.ByteArrayToStructureLittleEndian<qnx6_super_block>(sb2Sector)
                          : Marshal.ByteArrayToStructureBigEndian<qnx6_super_block>(sb2Sector);

                if(sb2.sb_magic == QNX6_MAGIC)
                {
                    uint sb2Crc = CalculateCrc32Be(sb2Sector, 8, 504);
                    sb2Valid = sb2.sb_checksum == sb2Crc;
                }
            }
        }

        // Use the superblock with higher serial number (more recent)
        if(sb2Valid && sb2.sb_serial > sb1.sb_serial)
        {
            _superblock = sb2;
            AaruLogging.Debug(MODULE_NAME, "Using superblock #2 (serial {0:X16})", sb2.sb_serial);
        }
        else
        {
            _superblock = sb1;
            AaruLogging.Debug(MODULE_NAME, "Using superblock #1 (serial {0:X16})", sb1.sb_serial);
        }

        // Sanity checks from Linux kernel
        if(_superblock.Inode.levels > QNX6_PTR_MAX_LEVELS)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Too many inode levels: {0} (max {1})",
                              _superblock.Inode.levels,
                              QNX6_PTR_MAX_LEVELS);

            return ErrorNumber.InvalidArgument;
        }

        if(_superblock.Longfile.levels > QNX6_PTR_MAX_LEVELS)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Too many longfile levels: {0} (max {1})",
                              _superblock.Longfile.levels,
                              QNX6_PTR_MAX_LEVELS);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Standard QNX6 superblock validated successfully");
        AaruLogging.Debug(MODULE_NAME, "Endianness: {0}",   _littleEndian ? "Little-endian" : "Big-endian");
        AaruLogging.Debug(MODULE_NAME, "Block size: {0}",   _blockSize);
        AaruLogging.Debug(MODULE_NAME, "Block offset: {0}", _blockOffset);
        AaruLogging.Debug(MODULE_NAME, "Num blocks: {0}",   _superblock.sb_num_blocks);
        AaruLogging.Debug(MODULE_NAME, "Num inodes: {0}",   _superblock.sb_num_inodes);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory entries into the cache</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();

        // Read the root inode (inode 1)
        ErrorNumber errno = ReadInode(QNX6_ROOT_INO, out qnx6_inode_entry rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate root inode is a directory
        if(rootInode.di_status != QNX6_FILE_DIRECTORY)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (status: {0})", rootInode.di_status);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.di_size);
        AaruLogging.Debug(MODULE_NAME, "Root inode mode: {0:X4}",    rootInode.di_mode);
        AaruLogging.Debug(MODULE_NAME, "Root inode filelevels: {0}", rootInode.di_filelevels);

        if(rootInode.di_block_ptr != null)
        {
            for(var i = 0; i < rootInode.di_block_ptr.Length && i < 4; i++)
                AaruLogging.Debug(MODULE_NAME, "Root inode block_ptr[{0}]: {1}", i, rootInode.di_block_ptr[i]);
        }

        // Read directory entries
        errno = ReadDirectoryEntries(rootInode, out Dictionary<string, qnx6_inode_entry> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory entries: {0}", errno);

            return errno;
        }

        foreach(KeyValuePair<string, qnx6_inode_entry> entry in entries) _rootDirectoryCache[entry.Key] = entry.Value;

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}