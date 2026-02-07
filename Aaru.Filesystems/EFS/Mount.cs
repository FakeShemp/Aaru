// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class EFS
{
    /// <summary>Number of inodes per basic block shift (log2(512/128) = 2)</summary>
    const int EFS_INOPBBSHIFT = 2;

    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting EFS volume");

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

        // Calculate inodes per cylinder group
        _inodesPerCg = (short)(_superblock.sb_cgisize << EFS_INOPBBSHIFT);

        AaruLogging.Debug(MODULE_NAME, "Inodes per cylinder group: {0}", _inodesPerCg);

        // Load root directory
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        string volumeName = StringHandlers.CToString(_superblock.sb_fname, _encoding);

        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            VolumeName   = volumeName,
            ClusterSize  = EFS_BBSIZE,
            Clusters     = (ulong)_superblock.sb_size,
            FreeClusters = (ulong)_superblock.sb_tfree,
            Dirty        = _superblock.sb_dirty != 0,
            VolumeSerial = $"{_superblock.sb_checksum:X8}",
            CreationDate = DateHandlers.UnixToDateTime(_superblock.sb_time)
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
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _superblock  = default(Superblock);
        _encoding    = null;
        _inodesPerCg = 0;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the EFS superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        uint sectorSize = _imagePlugin.Info.SectorSize;

        // Superblock is at basic block 1 (byte offset 0x200)
        const long sbByteOffset = EFS_SUPERBB * EFS_BBSIZE;

        // Calculate which sector contains the superblock and offset within it
        ulong sectorNumber   = (ulong)(sbByteOffset / sectorSize) + _partition.Start;
        var   offsetInSector = (int)(sbByteOffset % sectorSize);

        // Calculate how many sectors we need to read
        int sbStructSize  = Marshal.SizeOf<Superblock>();
        var sectorsToRead = (uint)((offsetInSector + sbStructSize + sectorSize - 1) / sectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, sectorsToRead, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

            return errno;
        }

        if(offsetInSector + sbStructSize > sector.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector too small for superblock");

            return ErrorNumber.InvalidArgument;
        }

        var sbData = new byte[sbStructSize];
        Array.Copy(sector, offsetInSector, sbData, 0, sbStructSize);

        _superblock = Marshal.ByteArrayToStructureBigEndian<Superblock>(sbData);

        AaruLogging.Debug(MODULE_NAME, "Magic: 0x{0:X8}", _superblock.sb_magic);

        // Validate magic number
        if(_superblock.sb_magic is not EFS_MAGIC and not EFS_MAGIC_NEW)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid magic: 0x{0:X8}, expected 0x{1:X8} or 0x{2:X8}",
                              _superblock.sb_magic,
                              EFS_MAGIC,
                              EFS_MAGIC_NEW);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Filesystem size: {0} blocks",        _superblock.sb_size);
        AaruLogging.Debug(MODULE_NAME, "First cylinder group at block: {0}", _superblock.sb_firstcg);
        AaruLogging.Debug(MODULE_NAME, "Cylinder group size: {0} blocks",    _superblock.sb_cgfsize);
        AaruLogging.Debug(MODULE_NAME, "Inodes per cg: {0} blocks",          _superblock.sb_cgisize);
        AaruLogging.Debug(MODULE_NAME, "Number of cylinder groups: {0}",     _superblock.sb_ncg);
        AaruLogging.Debug(MODULE_NAME, "Free blocks: {0}",                   _superblock.sb_tfree);
        AaruLogging.Debug(MODULE_NAME, "Free inodes: {0}",                   _superblock.sb_tinode);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();

        // Read the root inode (inode 2)
        ErrorNumber errno = ReadInode(EFS_ROOTINO, out Inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate that root inode is a directory
        var fileType = (FileType)(rootInode.di_mode & (ushort)FileType.IFMT);

        if(fileType != FileType.IFDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (type=0x{0:X4})", (ushort)fileType);

            return ErrorNumber.InvalidArgument;
        }

        _inodeCache[EFS_ROOTINO] = rootInode;

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.di_size);
        AaruLogging.Debug(MODULE_NAME, "Root inode extents: {0}",    rootInode.di_numextents);

        // Read directory contents from inode extents
        errno = ReadDirectoryContents(rootInode, out Dictionary<string, uint> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory contents: {0}", errno);

            return errno;
        }

        // Cache entries (skip . and ..)
        foreach(KeyValuePair<string, uint> entry in entries)
        {
            if(entry.Key is "." or "..") continue;

            _rootDirectoryCache[entry.Key] = entry.Value;

            AaruLogging.Debug(MODULE_NAME, "Cached entry: {0} -> inode {1}", entry.Key, entry.Value);
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);


        return ErrorNumber.NoError;
    }
}