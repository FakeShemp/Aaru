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
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
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