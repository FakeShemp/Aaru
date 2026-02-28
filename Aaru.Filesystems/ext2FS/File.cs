// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
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
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        uint targetInodeNum;

        // Root directory
        if(normalizedPath is "/")
            targetInodeNum = EXT2_ROOT_INO;
        else
        {
            // Resolve path to inode number
            ErrorNumber errno = ResolvePathToInode(normalizedPath, out targetInodeNum);

            if(errno != ErrorNumber.NoError) return errno;
        }

        // Read the target inode
        ErrorNumber readError = ReadInode(targetInodeNum, out Inode inode);

        if(readError != ErrorNumber.NoError) return readError;

        stat = InodeToFileEntryInfo(inode, targetInodeNum);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Resolve path to inode
        ErrorNumber errno = ResolvePathToInode(path, out uint inodeNumber);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ResolvePathToInode failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInode(inodeNumber, out Inode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadInode failed with {0}", errno);

            return errno;
        }

        // Check it's not a directory
        if((inode.mode & S_IFMT) == S_IFDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: path is a directory");

            return ErrorNumber.IsDirectory;
        }

        // Pre-compute data block list
        errno = GetInodeDataBlocks(inode, out List<(ulong physicalBlock, uint length)> blockList);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: GetInodeDataBlocks failed with {0}", errno);

            return errno;
        }

        ulong fileSize = (ulong)inode.size_high << 32 | inode.size_lo;

        node = new Ext2FileNode
        {
            Path             = path,
            Length           = (long)fileSize,
            Offset           = 0,
            InodeNumber      = inodeNumber,
            Inode            = inode,
            BlockList        = blockList,
            CachedBlock      = null,
            CachedBlockIndex = -1
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, inode={0}, size={1}", inodeNumber, fileSize);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not Ext2FileNode fileNode) return ErrorNumber.InvalidArgument;

        fileNode.CachedBlock      = null;
        fileNode.CachedBlockIndex = -1;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not Ext2FileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate which logical block contains the current offset
            long blockIndex    = currentOffset / _blockSize;
            var  offsetInBlock = (int)(currentOffset % _blockSize);

            byte[] blockData;

            // Use cached block if it matches
            if(blockIndex == fileNode.CachedBlockIndex && fileNode.CachedBlock != null)
                blockData = fileNode.CachedBlock;
            else
            {
                // Find the physical block from the pre-computed block list
                ErrorNumber errno = ReadLogicalBlock(fileNode.BlockList, (ulong)blockIndex, out blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadFile: ReadLogicalBlock failed for block {0}: {1}",
                                      blockIndex,
                                      errno);

                    if(bytesRead > 0) break;

                    return errno;
                }

                // Cache the block
                fileNode.CachedBlock      = blockData;
                fileNode.CachedBlockIndex = blockIndex;
            }

            if(blockData == null || blockData.Length == 0)
            {
                // Sparse block — fill with zeros
                long bytesToZero = Math.Min(_blockSize - offsetInBlock, toRead - bytesRead);
                Array.Clear(buffer, (int)bytesRead, (int)bytesToZero);
                bytesRead     += bytesToZero;
                currentOffset += bytesToZero;

                continue;
            }

            // Copy data from block to buffer
            long bytesToCopy = Math.Min(blockData.Length - offsetInBlock, toRead - bytesRead);
            Array.Copy(blockData, offsetInBlock, buffer, bytesRead, bytesToCopy);

            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: read {0} bytes, new offset={1}", read, fileNode.Offset);

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a filesystem path to its inode number</summary>
    /// <param name="path">The path to resolve</param>
    /// <param name="inodeNumber">The resolved inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ResolvePathToInode(string path, out uint inodeNumber)
    {
        inodeNumber = EXT2_ROOT_INO;

        string stripped = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] components = stripped.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(components.Length == 0) return ErrorNumber.InvalidArgument;

        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < components.Length; i++)
        {
            string component = components[i];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out uint childInode)) return ErrorNumber.NoSuchFile;

            // Last component — found it
            if(i == components.Length - 1)
            {
                inodeNumber = childInode;

                return ErrorNumber.NoError;
            }

            // Intermediate — must be a directory, read its entries
            ErrorNumber errno = ReadInode(childInode, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            if((inode.mode & S_IFMT) != S_IFDIR) return ErrorNumber.NotDirectory;

            ulong dirSize = (ulong)inode.size_high << 32 | inode.size_lo;

            errno = ReadDirectoryEntries(inode, dirSize, out Dictionary<string, uint> subEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = subEntries.Where(e => e.Key is not ("." or "..")).ToDictionary(e => e.Key, e => e.Value);
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts an ext2/3/4 inode to a <see cref="FileEntryInfo" /></summary>
    /// <param name="inode">The inode structure</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The file entry info</returns>
    FileEntryInfo InodeToFileEntryInfo(Inode inode, uint inodeNumber)
    {
        ulong fileSize = (ulong)inode.size_high << 32 | inode.size_lo;

        FileEntryInfo info = new()
        {
            Inode     = inodeNumber,
            Links     = inode.links_count,
            Length    = (long)fileSize,
            BlockSize = _blockSize,
            Blocks    = (long)((fileSize + _blockSize - 1) / _blockSize),
            UID       = (ulong)inode.uid_high << 16 | inode.uid,
            GID       = (ulong)inode.gid_high << 16 | inode.gid,
            Mode      = inode.mode,

            // Standard timestamps (seconds since Unix epoch)
            AccessTimeUtc       = DateHandlers.UnixUnsignedToDateTime(inode.atime),
            LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime(inode.mtime),
            StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime(inode.ctime)
        };

        // Extended timestamps with nanosecond precision (when inode size > 128)
        if(_inodeSize > EXT2_GOOD_OLD_INODE_SIZE && inode.extra_isize > 0)
        {
            // Creation time (crtime) is only present in extended inodes
            if(inode.crtime != 0 || inode.crtime_extra != 0)
                info.CreationTimeUtc = DateHandlers.UnixUnsignedToDateTime(inode.crtime, inode.crtime_extra >> 2);

            // Override standard timestamps with extended precision when available
            if(inode.atime_extra != 0)
                info.AccessTimeUtc = DateHandlers.UnixUnsignedToDateTime(inode.atime, inode.atime_extra >> 2);

            if(inode.mtime_extra != 0)
                info.LastWriteTimeUtc = DateHandlers.UnixUnsignedToDateTime(inode.mtime, inode.mtime_extra >> 2);

            if(inode.ctime_extra != 0)
                info.StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime(inode.ctime, inode.ctime_extra >> 2);
        }

        // Determine file type from mode
        info.Attributes = (inode.mode & S_IFMT) switch
                          {
                              S_IFDIR  => FileAttributes.Directory,
                              S_IFLNK  => FileAttributes.Symlink,
                              S_IFCHR  => FileAttributes.CharDevice,
                              S_IFBLK  => FileAttributes.BlockDevice,
                              S_IFIFO  => FileAttributes.Pipe,
                              S_IFSOCK => FileAttributes.Socket,
                              _        => FileAttributes.File
                          };

        // Map ext2/ext4 inode flags to FileAttributes
        if((inode.i_flags & EXT2_APPEND_FL) != 0) info.Attributes |= FileAttributes.AppendOnly;

        if((inode.i_flags & EXT2_IMMUTABLE_FL) != 0) info.Attributes |= FileAttributes.Immutable;

        if((inode.i_flags & EXT2_NODUMP_FL) != 0) info.Attributes |= FileAttributes.NoDump;

        if((inode.i_flags & EXT2_NOATIME_FL) != 0) info.Attributes |= FileAttributes.NoAccessTime;

        if((inode.i_flags & EXT2_SYNC_FL) != 0) info.Attributes |= FileAttributes.Sync;

        if((inode.i_flags & EXT2_COMPR_FL) != 0) info.Attributes |= FileAttributes.Compressed;

        if((inode.i_flags & EXT2_ECOMPR_FL) != 0) info.Attributes |= FileAttributes.CompressionError;

        if((inode.i_flags & EXT4_EXTENTS_FL) != 0) info.Attributes |= FileAttributes.Extents;

        if((inode.i_flags & EXT2_INDEX_FL) != 0) info.Attributes |= FileAttributes.IndexedDirectory;

        if((inode.i_flags & EXT3_JOURNAL_DATA_FL) != 0) info.Attributes |= FileAttributes.Journaled;

        if((inode.i_flags & EXT2_TOPDIR_FL) != 0) info.Attributes |= FileAttributes.TopDirectory;

        if((inode.i_flags & EXT4_INLINE_DATA_FL) != 0) info.Attributes |= FileAttributes.Inline;

        if((inode.i_flags & EXT4_ENCRYPT_FL) != 0) info.Attributes |= FileAttributes.Encrypted;

        // Device number for block/char devices
        if((inode.mode & S_IFMT) is S_IFCHR or S_IFBLK)
        {
            // Device number is stored in block[0] (old format) or block[1] (new format)
            uint dev = inode.block[0];

            if(dev == 0 && inode.block[1] != 0) dev = inode.block[1];

            // Linux new device format: major = bits 8-19, minor = bits 0-7 + bits 20-31
            uint major = dev >> 8 & 0xFFF;
            uint minor = dev & 0xFF | dev >> 12 & 0xFFF00;

            info.DeviceNo = (ulong)major << 32 | minor;
        }

        return info;
    }
}