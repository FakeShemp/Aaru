// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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
public sealed partial class Squash
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting SquashFS volume");

        _imagePlugin        = imagePlugin;
        _partition          = partition;
        _encoding           = encoding ?? Encoding.GetEncoding("iso-8859-15");
        _rootDirectoryCache = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

        // Read and validate the superblock
        ErrorNumber errno = ReadSuperBlock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");

        // Validate supported version
        if(_superBlock.s_major < SQUASHFS_MAJOR)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Unsupported older SquashFS version {0}.{1}",
                              _superBlock.s_major,
                              _superBlock.s_minor);

            return ErrorNumber.NotSupported;
        }

        if(_superBlock.s_major > SQUASHFS_MAJOR)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Unsupported newer SquashFS version {0}.{1}",
                              _superBlock.s_major,
                              _superBlock.s_minor);

            return ErrorNumber.NotSupported;
        }

        // Validate block size
        if(_superBlock.block_size > SQUASHFS_FILE_MAX_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid block size: {0}", _superBlock.block_size);

            return ErrorNumber.InvalidArgument;
        }

        // Validate block_size and block_log match
        if(_superBlock.block_size != 1 << _superBlock.block_log)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Block size mismatch: block_size={0}, 1<<block_log={1}",
                              _superBlock.block_size,
                              1 << _superBlock.block_log);

            return ErrorNumber.InvalidArgument;
        }

        // Validate root inode
        var rootInodeOffset = (uint)(_superBlock.root_inode & 0xFFFF);

        if(rootInodeOffset > SQUASHFS_METADATA_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid root inode offset: {0}", rootInodeOffset);

            return ErrorNumber.InvalidArgument;
        }

        // Load root directory
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            CreationDate = DateHandlers.UnixUnsignedToDateTime(_superBlock.mkfs_time),
            ClusterSize  = _superBlock.block_size,
            Clusters     = _superBlock.bytes_used / _superBlock.block_size,
            Files        = _superBlock.inodes,
            FreeClusters = 0 // SquashFS is read-only
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Volume mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _rootDirectoryCache?.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _superBlock  = default(SuperBlock);
        _encoding    = null;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the SquashFS superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperBlock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        // Read the first sector
        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading first sector: {0}", errno);

            return errno;
        }

        // Check magic at offset 0
        var magic = BitConverter.ToUInt32(sector, 0);

        if(magic == SQUASH_MAGIC)
        {
            _superBlock   = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);
            _littleEndian = true;
        }
        else if(magic == SQUASH_CIGAM)
        {
            _superBlock   = Marshal.ByteArrayToStructureBigEndian<SuperBlock>(sector);
            _littleEndian = false;
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid magic: 0x{0:X8}", magic);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Magic: 0x{0:X8}, little-endian: {1}", _superBlock.magic, _littleEndian);

        AaruLogging.Debug(MODULE_NAME, "Version: {0}.{1}",           _superBlock.s_major, _superBlock.s_minor);
        AaruLogging.Debug(MODULE_NAME, "Filesystem size: {0} bytes", _superBlock.bytes_used);
        AaruLogging.Debug(MODULE_NAME, "Block size: {0}",            _superBlock.block_size);
        AaruLogging.Debug(MODULE_NAME, "Inodes: {0}",                _superBlock.inodes);
        AaruLogging.Debug(MODULE_NAME, "Fragments: {0}",             _superBlock.fragments);

        AaruLogging.Debug(MODULE_NAME, "Compression: {0}", (SquashCompression)_superBlock.compression);

        AaruLogging.Debug(MODULE_NAME, "Flags: 0x{0:X4}", _superBlock.flags);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and caches the root directory contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        // Extract root inode block and offset from root_inode reference
        // root_inode format: upper 48 bits = block, lower 16 bits = offset
        var rootInodeBlock  = (uint)(_superBlock.root_inode >> 16);
        var rootInodeOffset = (uint)(_superBlock.root_inode & 0xFFFF);

        AaruLogging.Debug(MODULE_NAME,
                          "Root inode reference: 0x{0:X16}, block: {1}, offset: {2}",
                          _superBlock.root_inode,
                          rootInodeBlock,
                          rootInodeOffset);

        // Calculate absolute position of root inode
        ulong rootInodePosition = _superBlock.inode_table_start + rootInodeBlock;

        AaruLogging.Debug(MODULE_NAME,
                          "Inode table start: 0x{0:X16}, root inode position: 0x{1:X16}",
                          _superBlock.inode_table_start,
                          rootInodePosition);

        // Read the metadata block containing the root inode
        ErrorNumber errno = ReadMetadataBlock(rootInodePosition, out byte[] inodeBlock);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode metadata block: {0}", errno);

            return errno;
        }

        if(inodeBlock == null || inodeBlock.Length <= rootInodeOffset)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid root inode block data");

            return ErrorNumber.InvalidArgument;
        }

        // Read the base inode to get the type
        var baseInodeData = new byte[System.Runtime.InteropServices.Marshal.SizeOf<BaseInode>()];
        Array.Copy(inodeBlock, rootInodeOffset, baseInodeData, 0, baseInodeData.Length);

        BaseInode baseInode = _littleEndian
                                  ? Marshal.ByteArrayToStructureLittleEndian<BaseInode>(baseInodeData)
                                  : Marshal.ByteArrayToStructureBigEndian<BaseInode>(baseInodeData);

        AaruLogging.Debug(MODULE_NAME, "Root inode type: {0}", (SquashInodeType)baseInode.inode_type);

        // Verify root is a directory
        if(baseInode.inode_type != (ushort)SquashInodeType.Directory &&
           baseInode.inode_type != (ushort)SquashInodeType.ExtendedDirectory)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Root inode is not a directory, type: {0}",
                              (SquashInodeType)baseInode.inode_type);

            return ErrorNumber.InvalidArgument;
        }

        // Read the directory inode based on type
        uint dirStartBlock;
        uint dirSize;
        uint dirOffset;

        if(baseInode.inode_type == (ushort)SquashInodeType.Directory)
        {
            var dirInodeData = new byte[System.Runtime.InteropServices.Marshal.SizeOf<DirInode>()];
            Array.Copy(inodeBlock, rootInodeOffset, dirInodeData, 0, dirInodeData.Length);

            DirInode dirInode = _littleEndian
                                    ? Marshal.ByteArrayToStructureLittleEndian<DirInode>(dirInodeData)
                                    : Marshal.ByteArrayToStructureBigEndian<DirInode>(dirInodeData);

            dirStartBlock = dirInode.start_block;
            dirSize       = dirInode.file_size;
            dirOffset     = dirInode.offset;

            _rootInode = baseInode.inode_number;

            AaruLogging.Debug(MODULE_NAME,
                              "Directory inode: start_block={0}, size={1}, offset={2}",
                              dirStartBlock,
                              dirSize,
                              dirOffset);
        }
        else // ExtendedDirectory
        {
            var extDirInodeData = new byte[System.Runtime.InteropServices.Marshal.SizeOf<ExtendedDirInode>()];
            Array.Copy(inodeBlock, rootInodeOffset, extDirInodeData, 0, extDirInodeData.Length);

            ExtendedDirInode extDirInode = _littleEndian
                                               ? Marshal
                                                  .ByteArrayToStructureLittleEndian<ExtendedDirInode>(extDirInodeData)
                                               : Marshal
                                                  .ByteArrayToStructureBigEndian<ExtendedDirInode>(extDirInodeData);

            dirStartBlock = extDirInode.start_block;
            dirSize       = extDirInode.file_size;
            dirOffset     = extDirInode.offset;

            _rootInode = baseInode.inode_number;

            AaruLogging.Debug(MODULE_NAME,
                              "Extended directory inode: start_block={0}, size={1}, offset={2}",
                              dirStartBlock,
                              dirSize,
                              dirOffset);
        }

        // Read directory entries
        return ReadDirectoryContents(dirStartBlock, dirOffset, dirSize, _rootDirectoryCache);
    }
}