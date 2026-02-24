// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : IBM JFS filesystem plugin
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of IBM's Journaled File System</summary>
public sealed partial class JFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/")
        {
            ErrorNumber rootErrno = ReadFilesetInode(ROOT_I, out Inode rootInode);

            if(rootErrno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: error reading root inode: {0}", rootErrno);

                return rootErrno;
            }

            stat = InodeToFileEntryInfo(rootInode, ROOT_I);

            return ErrorNumber.NoError;
        }

        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Navigate to the parent directory
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse all but the last component (those must be directories)
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            AaruLogging.Debug(MODULE_NAME, "Stat: navigating to component '{0}'", component);

            if(!currentEntries.TryGetValue(component, out uint dirInodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            ErrorNumber errno = ReadFilesetInode(dirInodeNumber, out Inode dirInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: error reading inode {0}: {1}", dirInodeNumber, errno);

                return errno;
            }

            // Must be a directory
            if((dirInode.di_mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            errno = ParseDtreeRoot(dirInode.di_u, out Dictionary<string, uint> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: error parsing directory dtree: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Find the target in the current directory
        string targetName = pathComponents[^1];

        if(!currentEntries.TryGetValue(targetName, out uint targetInodeNumber))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        // Read the target inode
        ErrorNumber readErrno = ReadFilesetInode(targetInodeNumber, out Inode targetInode);

        if(readErrno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: error reading target inode {0}: {1}", targetInodeNumber, readErrno);

            return readErrno;
        }

        stat = InodeToFileEntryInfo(targetInode, targetInodeNumber);

        AaruLogging.Debug(MODULE_NAME,
                          "Stat: successful for '{0}': inode={1}, size={2}, mode=0x{3:X}",
                          targetName,
                          stat.Inode,
                          stat.Length,
                          stat.Mode);

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a JFS on-disk inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The JFS inode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The FileEntryInfo structure</returns>
    static FileEntryInfo InodeToFileEntryInfo(in Inode inode, uint inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = PSIZE,
            Inode               = inodeNumber,
            Length              = (long)inode.di_size,
            Links               = inode.di_nlink,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = inode.di_mode & 0x0FFFu,
            Blocks              = (long)inode.di_nblocks,
            AccessTimeUtc       = DateHandlers.UnixUnsignedToDateTime(inode.di_atime.tv_sec,
                                                                      inode.di_atime.tv_nsec),
            StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime(inode.di_ctime.tv_sec,
                                                                      inode.di_ctime.tv_nsec),
            LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime(inode.di_mtime.tv_sec,
                                                                      inode.di_mtime.tv_nsec),
            CreationTimeUtc     = DateHandlers.UnixUnsignedToDateTime(inode.di_otime.tv_sec,
                                                                      inode.di_otime.tv_nsec)
        };

        // Determine file type from di_mode (S_IFMT mask = 0xF000)
        info.Attributes = (inode.di_mode & 0xF000) switch
                          {
                              0x4000 => FileAttributes.Directory,   // S_IFDIR
                              0x8000 => FileAttributes.File,        // S_IFREG
                              0xA000 => FileAttributes.Symlink,     // S_IFLNK
                              0x2000 => FileAttributes.CharDevice,  // S_IFCHR
                              0x6000 => FileAttributes.BlockDevice, // S_IFBLK
                              0x1000 => FileAttributes.FIFO,        // S_IFIFO
                              0xC000 => FileAttributes.Socket,      // S_IFSOCK
                              _      => FileAttributes.File
                          };

        return info;
    }
}

