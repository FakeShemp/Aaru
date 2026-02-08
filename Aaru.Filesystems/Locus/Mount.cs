// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Locus
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting Locus volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        if(imagePlugin.Info.SectorSize < 512)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size too small: {0}", imagePlugin.Info.SectorSize);

            return ErrorNumber.InvalidArgument;
        }

        // Read and validate the superblock
        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");

        // Calculate block size based on version
        // SB_SB4096 = smallblock filesystem with 4096-byte blocks
        // SB_B1024 = regular 1024-byte block filesystem
        _blockSize = _superblock.s_version == Version.SB_SB4096 ? 4096 : 1024;

        // Smallblock filesystems use 512-byte inodes (with inline data buffer)
        // Regular filesystems use 128-byte inodes
        _smallBlocks = _superblock.s_version == Version.SB_SB4096;
        int inodeSize = _smallBlocks ? DINODE_SMALLBLOCK_SIZE : DINODE_SIZE;

        AaruLogging.Debug(MODULE_NAME, "Block size: {0}",            _blockSize);
        AaruLogging.Debug(MODULE_NAME, "Smallblock filesystem: {0}", _smallBlocks);
        AaruLogging.Debug(MODULE_NAME, "Inode size: {0}",            inodeSize);

        // Calculate inodes per block
        _inodesPerBlock = _blockSize / inodeSize;

        AaruLogging.Debug(MODULE_NAME, "Inodes per block: {0}", _inodesPerBlock);

        // Load root directory (inode 2 is always root)
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        string volumeName = StringHandlers.CToString(_superblock.s_fsmnt, _encoding);

        if(string.IsNullOrEmpty(volumeName)) volumeName = StringHandlers.CToString(_superblock.s_fpack, _encoding);

        Metadata = new FileSystem
        {
            Type = FS_TYPE,
            VolumeName = volumeName,
            ClusterSize = (uint)_blockSize,
            Clusters = (ulong)_superblock.s_fsize,
            FreeClusters = (ulong)_superblock.s_tfree,
            Dirty = !_superblock.s_flags.HasFlag(Flags.SB_CLEAN) || _superblock.s_flags.HasFlag(Flags.SB_DIRTY),
            ModificationDate = DateHandlers.UnixToDateTime(_superblock.s_time)
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

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();
        _smallBlockDataCache.Clear();
        _mounted        = false;
        _imagePlugin    = null;
        _partition      = default(Partition);
        _superblock     = default(Superblock);
        _encoding       = null;
        _blockSize      = 0;
        _inodesPerBlock = 0;
        _smallBlocks    = false;
        Metadata        = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the Locus superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        uint sectorSize   = _imagePlugin.Info.SectorSize;
        int  sbStructSize = Marshal.SizeOf<Superblock>();

        // Superblock can be at sectors 0-8, search for it
        for(ulong location = 0; location <= 8; location++)
        {
            var sbSize = (uint)(sbStructSize / sectorSize);

            if(sbStructSize % sectorSize != 0) sbSize++;

            if(_partition.Start + location + sbSize >= _imagePlugin.Info.Sectors) continue;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(_partition.Start + location, false, sbSize, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) continue;

            if(sector.Length < sbStructSize) continue;

            Superblock sb = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

            AaruLogging.Debug(MODULE_NAME, "Magic at location {0} = 0x{1:X8}", location, sb.s_magic);

            if(sb.s_magic is not LOCUS_MAGIC and not LOCUS_CIGAM and not LOCUS_MAGIC_OLD and not LOCUS_CIGAM_OLD)
                continue;

            // Handle big-endian superblock
            if(sb.s_magic is LOCUS_CIGAM or LOCUS_CIGAM_OLD)
            {
                _superblock         = Marshal.ByteArrayToStructureBigEndian<Superblock>(sector);
                _superblock.s_flags = (Flags)Swapping.Swap((ushort)_superblock.s_flags);
                _bigEndian          = true;
            }
            else
            {
                _superblock = sb;
                _bigEndian  = false;
            }

            _superblockLocation = location;

            AaruLogging.Debug(MODULE_NAME, "Superblock found at location {0}", location);
            AaruLogging.Debug(MODULE_NAME, "s_magic = 0x{0:X8}",               _superblock.s_magic);
            AaruLogging.Debug(MODULE_NAME, "s_gfs = {0}",                      _superblock.s_gfs);
            AaruLogging.Debug(MODULE_NAME, "s_fsize = {0}",                    _superblock.s_fsize);
            AaruLogging.Debug(MODULE_NAME, "s_isize = {0}",                    _superblock.s_isize);
            AaruLogging.Debug(MODULE_NAME, "s_tfree = {0}",                    _superblock.s_tfree);
            AaruLogging.Debug(MODULE_NAME, "s_tinode = {0}",                   _superblock.s_tinode);
            AaruLogging.Debug(MODULE_NAME, "s_flags = {0}",                    _superblock.s_flags);
            AaruLogging.Debug(MODULE_NAME, "s_version = {0}",                  _superblock.s_version);

            return ErrorNumber.NoError;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock not found");

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();

        // Root inode is always inode 2
        ErrorNumber errno = ReadInode(ROOT_INO, out Dinode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate that root inode is a directory
        var fileType = (FileMode)(rootInode.di_mode & (ushort)FileMode.IFMT);

        if(fileType != FileMode.IFDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (type=0x{0:X4})", (ushort)fileType);

            return ErrorNumber.InvalidArgument;
        }

        _inodeCache[ROOT_INO] = rootInode;

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.di_size);

        // Read directory contents
        errno = ReadDirectoryContents(ROOT_INO, rootInode, out Dictionary<string, int> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory contents: {0}", errno);

            return errno;
        }

        // Cache entries (skip . and ..)
        foreach(KeyValuePair<string, int> entry in entries)
        {
            if(entry.Key is "." or "..") continue;

            _rootDirectoryCache[entry.Key] = entry.Value;

            AaruLogging.Debug(MODULE_NAME, "Cached entry: {0} -> inode {1}", entry.Key, entry.Value);
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);


        return ErrorNumber.NoError;
    }
}