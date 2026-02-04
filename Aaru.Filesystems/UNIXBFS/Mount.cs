// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
/// <summary>Implements the UNIX boot filesystem</summary>
public sealed partial class BFS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        // Read the superblock (block 0)
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

        string volName = StringHandlers.CToString(_superblock.s_volume, _encoding);

        Metadata = new FileSystem
        {
            Clusters    = (_superblock.s_end + 1) / BFS_BSIZE,
            ClusterSize = BFS_BSIZE,
            Type        = FS_TYPE,
            VolumeName  = volName
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
        _inodeCache.Clear();
        _mounted      = false;
        _imagePlugin  = null;
        _partition    = default(Partition);
        _superblock   = default(SuperBlock);
        _encoding     = null;
        _littleEndian = true;
        Metadata      = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the filesystem superblock</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        // Read block 0 (superblock)
        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] sbSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

            return errno;
        }

        if(sbSector.Length < 512)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock sector too small");

            return ErrorNumber.InvalidArgument;
        }

        // Check magic and determine endianness
        var magic = BitConverter.ToUInt32(sbSector, 0);

        if(magic == BFS_MAGIC)
        {
            _littleEndian = true;
            _superblock   = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sbSector);
        }
        else if(magic == BFS_MAGIC_BE)
        {
            _littleEndian = false;
            _superblock   = Marshal.ByteArrayToStructureBigEndian<SuperBlock>(sbSector);
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid magic: 0x{0:X8}", magic);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Endianness: {0}",   _littleEndian ? "Little-endian" : "Big-endian");
        AaruLogging.Debug(MODULE_NAME, "s_start: 0x{0:X8}", _superblock.s_start);
        AaruLogging.Debug(MODULE_NAME, "s_end: 0x{0:X8}",   _superblock.s_end);

        // Validate superblock (from Linux: s_start > s_end is invalid)
        if(_superblock.s_start > _superblock.s_end)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock corrupted: s_start > s_end");

            return ErrorNumber.InvalidArgument;
        }

        // s_start must be at least after superblock and one directory entry
        if(_superblock.s_start < BFS_BSIZE + BFS_DIRENT_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock corrupted: s_start too small");

            return ErrorNumber.InvalidArgument;
        }

        // Calculate last inode number (from Linux kernel)
        _lastInode = (_superblock.s_start - BFS_BSIZE) / 64 + BFS_ROOT_INO - 1;

        AaruLogging.Debug(MODULE_NAME, "Last inode: {0}", _lastInode);

        if(_lastInode > BFS_MAX_LASTI)
        {
            AaruLogging.Debug(MODULE_NAME, "Too many inodes: {0} > {1}", _lastInode, BFS_MAX_LASTI);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock validated successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();

        // Read the root inode (inode 2)
        ErrorNumber errno = ReadInode(BFS_ROOT_INO, out Inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate root inode is a directory
        if(rootInode.i_vtype != BFS_VDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (vtype={0})", rootInode.i_vtype);

            return ErrorNumber.InvalidArgument;
        }

        _inodeCache[BFS_ROOT_INO] = rootInode;

        AaruLogging.Debug(MODULE_NAME,
                          "Root inode: sblock={0}, eblock={1}, eoffset={2}",
                          rootInode.i_sblock,
                          rootInode.i_eblock,
                          rootInode.i_eoffset);

        // Read directory entries from root directory blocks
        for(uint block = rootInode.i_sblock; block <= rootInode.i_eblock; block++)
        {
            errno = ReadBlock(block, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading directory block {0}: {1}", block, errno);

                continue;
            }

            // Parse directory entries (32 entries per block, 16 bytes each)
            for(var i = 0; i < BFS_DIRS_PER_BLOCK; i++)
            {
                int offset = i * BFS_DIRENT_SIZE;

                // Check if we've passed the end of the directory
                if(block * BFS_BSIZE + offset > rootInode.i_eoffset) break;

                DirectoryEntry entry = _littleEndian
                                           ? Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(blockData,
                                               offset,
                                               BFS_DIRENT_SIZE)
                                           : Marshal.ByteArrayToStructureBigEndian<DirectoryEntry>(blockData,
                                               offset,
                                               BFS_DIRENT_SIZE);

                // Skip empty entries
                if(entry.ino == 0) continue;

                string filename = StringHandlers.CToString(entry.name, _encoding);

                // Skip "." and ".." entries
                if(string.IsNullOrWhiteSpace(filename) || filename == "." || filename == "..") continue;

                // Read the inode for this entry
                errno = ReadInode(entry.ino, out Inode entryInode);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Error reading inode {0} for '{1}': {2}",
                                      entry.ino,
                                      filename,
                                      errno);

                    continue;
                }

                if(!_rootDirectoryCache.ContainsKey(filename))
                {
                    _rootDirectoryCache[filename] = entry.ino;
                    _inodeCache[entry.ino]        = entryInode;

                    AaruLogging.Debug(MODULE_NAME, "Found '{0}' (inode {1})", filename, entry.ino);
                }
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);


        return ErrorNumber.NoError;
    }
}