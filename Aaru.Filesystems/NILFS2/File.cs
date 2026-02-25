// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ReadLink: path='{0}'", path);

        // Verify it's a symlink
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: Stat failed with {0}", errno);

            return errno;
        }

        if(!stat.Attributes.HasFlag(FileAttributes.Symlink))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: path is not a symbolic link");

            return ErrorNumber.InvalidArgument;
        }

        // Resolve path to inode number
        errno = LookupInode(path, out ulong inodeNumber);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: LookupInode failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInodeFromIfile(_ifileInode, inodeNumber, out Inode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadInodeFromIfile failed with {0}", errno);

            return errno;
        }

        if(inode.size == 0)
        {
            dest = string.Empty;

            return ErrorNumber.NoError;
        }

        // NILFS2 uses "slow symlinks" (page_symlink): the target is stored as regular
        // file data in the inode's data blocks. The kernel limits symlink targets to
        // one block size, so reading logical block 0 is sufficient.
        errno = ReadLogicalBlock(inode, 0, false, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadLogicalBlock failed with {0}", errno);

            return errno;
        }

        // The target is a null-terminated string, clamped to inode size
        var maxLen = (int)Math.Min(inode.size, _blockSize);
        dest = StringHandlers.CToString(blockData, _encoding);

        if(dest.Length > maxLen) dest = dest[..maxLen];

        AaruLogging.Debug(MODULE_NAME, "ReadLink: target='{0}'", dest);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath is "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read the root inode
            ErrorNumber rootErrno = ReadInodeFromIfile(_ifileInode, NILFS2_ROOT_INO, out Inode rootInode);

            if(rootErrno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading root inode: {0}", rootErrno);

                return rootErrno;
            }

            stat = InodeToFileEntryInfo(rootInode, NILFS2_ROOT_INO);

            return ErrorNumber.NoError;
        }

        // Parse path and navigate to target
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c is not ("." or ".."))
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Navigate to the target
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;
        string                                 targetName     = pathComponents[^1];

        // Traverse all but the last component (parent directories)
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            AaruLogging.Debug(MODULE_NAME, "Stat: Navigating to component '{0}'", component);

            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo dirEntry))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            if(dirEntry.Type != FileType.Dir)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read the child directory inode
            ErrorNumber errno = ReadInodeFromIfile(_ifileInode, dirEntry.InodeNumber, out Inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading inode {0}: {1}", dirEntry.InodeNumber, errno);

                return errno;
            }

            // Verify it's a directory (S_IFDIR = 0x4000)
            if((childInode.mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Inode {0} is not a directory", dirEntry.InodeNumber);

                return ErrorNumber.NotDirectory;
            }

            // Read directory entries
            var childEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            errno = ReadDirectoryEntries(childInode, childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Stat: Error reading directory entries for inode {0}: {1}",
                                  dirEntry.InodeNumber,
                                  errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Find the target in the current directory
        if(!currentEntries.TryGetValue(targetName, out DirectoryEntryInfo targetEntry))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: Target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        // Read the target inode
        ErrorNumber readErrno = ReadInodeFromIfile(_ifileInode, targetEntry.InodeNumber, out Inode targetInode);

        if(readErrno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Stat: Error reading target inode {0}: {1}",
                              targetEntry.InodeNumber,
                              readErrno);

            return readErrno;
        }

        stat = InodeToFileEntryInfo(targetInode, targetEntry.InodeNumber);

        AaruLogging.Debug(MODULE_NAME,
                          "Stat successful: name='{0}', size={1}, inode={2}, mode=0x{3:X4}",
                          targetName,
                          stat.Length,
                          stat.Inode,
                          stat.Mode);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Look up the inode number for the path
        ErrorNumber errno = LookupInode(path, out ulong inodeNumber);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LookupInode failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInodeFromIfile(_ifileInode, inodeNumber, out Inode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadInodeFromIfile failed with {0}", errno);

            return errno;
        }

        // Check it's not a directory (S_IFDIR = 0x4000)
        if((inode.mode & 0xF000) == 0x4000)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: path is a directory");

            return ErrorNumber.IsDirectory;
        }

        node = new Nilfs2FileNode
        {
            Path             = path,
            Length           = (long)inode.size,
            Offset           = 0,
            InodeNumber      = inodeNumber,
            Inode            = inode,
            CachedBlock      = null,
            CachedBlockIndex = -1
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, inode={0}, size={1}", inodeNumber, inode.size);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not Nilfs2FileNode fileNode) return ErrorNumber.InvalidArgument;

        fileNode.CachedBlock      = null;
        fileNode.CachedBlockIndex = -1;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not Nilfs2FileNode fileNode) return ErrorNumber.InvalidArgument;

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
            // Calculate which block contains the current offset
            long blockIndex    = currentOffset / _blockSize;
            var  offsetInBlock = (int)(currentOffset % _blockSize);

            byte[] blockData;

            // Use cached block if it matches
            if(blockIndex == fileNode.CachedBlockIndex && fileNode.CachedBlock != null)
                blockData = fileNode.CachedBlock;
            else
            {
                // Read the block through the bmap → DAT → physical chain
                ErrorNumber errno = ReadLogicalBlock(fileNode.Inode, (ulong)blockIndex, false, out blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadFile: ReadLogicalBlock failed for block {0}: {1}",
                                      blockIndex,
                                      errno);

                    if(bytesRead > 0) break;

                    return errno;
                }

                // Cache the block we just read
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

    /// <summary>Resolves a path to an inode number by traversing directories from the root</summary>
    /// <param name="path">File path</param>
    /// <param name="inodeNumber">Output inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupInode(string path, out ulong inodeNumber)
    {
        inodeNumber = 0;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath is "/")
        {
            inodeNumber = NILFS2_ROOT_INO;

            return ErrorNumber.NoError;
        }

        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c is not ("." or ".."))
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;
        string                                 targetName     = pathComponents[^1];

        // Traverse parent directories
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo dirEntry)) return ErrorNumber.NoSuchFile;

            if(dirEntry.Type != FileType.Dir) return ErrorNumber.NotDirectory;

            ErrorNumber errno = ReadInodeFromIfile(_ifileInode, dirEntry.InodeNumber, out Inode childInode);

            if(errno != ErrorNumber.NoError) return errno;

            if((childInode.mode & 0xF000) != 0x4000) return ErrorNumber.NotDirectory;

            var childEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);
            errno = ReadDirectoryEntries(childInode, childEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = childEntries;
        }

        if(!currentEntries.TryGetValue(targetName, out DirectoryEntryInfo targetEntry)) return ErrorNumber.NoSuchFile;

        inodeNumber = targetEntry.InodeNumber;

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a NILFS2 inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The NILFS2 inode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>FileEntryInfo populated from the inode</returns>
    FileEntryInfo InodeToFileEntryInfo(in Inode inode, ulong inodeNumber)
    {
        // Determine file type from mode field (Unix-style S_IFMT mask = 0xF000)
        FileAttributes attributes = (inode.mode & 0xF000) switch
                                    {
                                        0x4000 => FileAttributes.Directory,
                                        0xA000 => FileAttributes.Symlink,
                                        0x2000 => FileAttributes.CharDevice,
                                        0x6000 => FileAttributes.BlockDevice,
                                        0x1000 => FileAttributes.FIFO,
                                        0xC000 => FileAttributes.Socket,
                                        _      => FileAttributes.File
                                    };

        // Map NILFS2 inode flags to file attributes
        if((inode.flags & NILFS2_IMMUTABLE_FL) != 0) attributes |= FileAttributes.Immutable;

        if((inode.flags & NILFS2_APPEND_FL) != 0) attributes |= FileAttributes.AppendOnly;

        if((inode.flags & NILFS2_NODUMP_FL) != 0) attributes |= FileAttributes.NoDump;

        if((inode.flags & NILFS2_NOATIME_FL) != 0) attributes |= FileAttributes.NoAccessTime;

        if((inode.flags & NILFS2_SYNC_FL) != 0) attributes |= FileAttributes.Sync;

        return new FileEntryInfo
        if((inode.flags & NILFS2_DIRSYNC_FL) != 0) attributes |= FileAttributes.Sync;
        {
            Attributes          = attributes,
            Inode               = inodeNumber,
            Links               = inode.links_count,
            Length              = (long)inode.size,
            Blocks              = (long)inode.blocks,
            BlockSize           = _blockSize,
            UID                 = inode.uid,
            GID                 = inode.gid,
            Mode                = (uint)(inode.mode & 0x0FFF),
            CreationTimeUtc     = DateHandlers.UnixUnsignedToDateTime((uint)inode.ctime, inode.ctime_nsec),
            LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime((uint)inode.mtime, inode.mtime_nsec),
            StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime((uint)inode.ctime, inode.ctime_nsec)
        };
    }
}