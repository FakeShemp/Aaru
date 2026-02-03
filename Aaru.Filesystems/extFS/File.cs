// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class extFS
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Use Stat to get file info including attributes
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        attributes = stat.Attributes;

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
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read the root inode
            ErrorNumber rootError = ReadInode(EXT_ROOT_INO, out ext_inode rootInode);

            if(rootError != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", rootError);

                return rootError;
            }

            stat = new FileEntryInfo
            {
                Attributes       = FileAttributes.Directory,
                Inode            = EXT_ROOT_INO,
                Links            = rootInode.i_nlinks,
                Length           = rootInode.i_size,
                BlockSize        = 1024u << (int)_superblock.s_log_zone_size,
                UID              = rootInode.i_uid,
                GID              = rootInode.i_gid,
                Mode             = rootInode.i_mode,
                LastWriteTimeUtc = DateHandlers.UnixToDateTime(rootInode.i_time),
                AccessTimeUtc    = DateHandlers.UnixToDateTime(rootInode.i_time),
                CreationTimeUtc  = DateHandlers.UnixToDateTime(rootInode.i_time)
            };

            return ErrorNumber.NoError;
        }

        // Parse path and navigate to target
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Navigate to the target
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;
        string                   targetName     = pathComponents[^1];

        // Traverse all but the last component
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            AaruLogging.Debug(MODULE_NAME, "Navigating to component '{0}'", component);

            if(!currentEntries.TryGetValue(component, out uint childInodeNum))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            ErrorNumber errno = ReadInode(childInodeNum, out ext_inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child inode: {0}", errno);

                return errno;
            }

            // Check if it's a directory (S_IFDIR = 0x4000)
            if((childInode.i_mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            errno = ReadDirectoryEntries(childInode, out Dictionary<string, uint> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child directory: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Find the target in the current directory
        if(!currentEntries.TryGetValue(targetName, out uint targetInodeNum))
        {
            AaruLogging.Debug(MODULE_NAME, "Target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        // Read the target inode
        ErrorNumber readError = ReadInode(targetInodeNum, out ext_inode targetInode);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading target inode: {0}", readError);

            return readError;
        }

        // Build FileEntryInfo from the inode
        stat = new FileEntryInfo
        {
            Inode            = targetInodeNum,
            Links            = targetInode.i_nlinks,
            Length           = targetInode.i_size,
            BlockSize        = 1024u << (int)_superblock.s_log_zone_size,
            UID              = targetInode.i_uid,
            GID              = targetInode.i_gid,
            Mode             = targetInode.i_mode,
            LastWriteTimeUtc = DateHandlers.UnixToDateTime(targetInode.i_time),
            AccessTimeUtc    = DateHandlers.UnixToDateTime(targetInode.i_time),
            CreationTimeUtc  = DateHandlers.UnixToDateTime(targetInode.i_time)
        };

        // Determine file type from mode field (Unix-style S_IFMT mask = 0xF000)
        stat.Attributes = (targetInode.i_mode & 0xF000) switch
                          {
                              0x4000 => // S_IFDIR - directory
                                  FileAttributes.Directory,
                              0xA000 => // S_IFLNK - symbolic link
                                  FileAttributes.Symlink,
                              0x2000 => // S_IFCHR - character device
                                  FileAttributes.CharDevice,
                              0x6000 => // S_IFBLK - block device
                                  FileAttributes.BlockDevice,
                              0x1000 => // S_IFIFO - FIFO
                                  FileAttributes.FIFO,
                              0xC000 => // S_IFSOCK - socket
                                  FileAttributes.Socket,
                              _ => FileAttributes.File
                          };

        AaruLogging.Debug(MODULE_NAME,
                          "Stat successful: name='{0}', size={1}, inode={2}, mode=0x{3:X4}",
                          targetName,
                          stat.Length,
                          stat.Inode,
                          stat.Mode);

        return ErrorNumber.NoError;
    }
}