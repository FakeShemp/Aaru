// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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

public sealed partial class XFS
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
        if(normalizedPath == "/")
        {
            ErrorNumber errno = ReadInode(_superblock.rootino, out Dinode rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInode, _superblock.rootino);

            return ErrorNumber.NoError;
        }

        // Traverse the path to find the target inode
        ErrorNumber lookupErrno = LookupInode(normalizedPath, out ulong inodeNumber);

        if(lookupErrno != ErrorNumber.NoError) return lookupErrno;

        ErrorNumber readErrno = ReadInode(inodeNumber, out Dinode inode);

        if(readErrno != ErrorNumber.NoError) return readErrno;

        stat = InodeToFileEntryInfo(inode, inodeNumber);

        AaruLogging.Debug(MODULE_NAME,
                          "Stat successful: path='{0}', size={1}, inode={2}, mode=0x{3:X4}",
                          normalizedPath,
                          stat.Length,
                          stat.Inode,
                          stat.Mode);

        return ErrorNumber.NoError;
    }

    /// <summary>Looks up an inode number by traversing a path from the root directory</summary>
    /// <param name="path">Absolute path to look up</param>
    /// <param name="inodeNumber">Output inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupInode(string path, out ulong inodeNumber)
    {
        inodeNumber = 0;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or "." or "/")
        {
            inodeNumber = _superblock.rootino;

            return ErrorNumber.NoError;
        }

        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, ulong> currentEntries = _rootDirectoryCache;

        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out ulong foundIno))
            {
                AaruLogging.Debug(MODULE_NAME, "LookupInode: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // If this is the last component, we found it
            if(p == pathComponents.Length - 1)
            {
                inodeNumber = foundIno;

                return ErrorNumber.NoError;
            }

            // Not the last component — must be a directory, read its contents
            ErrorNumber errno = ReadInode(foundIno, out Dinode inode);

            if(errno != ErrorNumber.NoError) return errno;

            if((inode.di_mode & S_IFMT) != S_IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "LookupInode: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            errno = GetDirectoryContents(foundIno, inode, out Dictionary<string, ulong> dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts an XFS dinode to a FileEntryInfo structure</summary>
    /// <param name="inode">The dinode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The populated FileEntryInfo</returns>
    FileEntryInfo InodeToFileEntryInfo(Dinode inode, ulong inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = _superblock.blocksize,
            Inode               = inodeNumber,
            Length              = inode.di_size,
            Links               = inode.di_nlink,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0x0FFF),
            Blocks              = inode.di_nblocks,
            AccessTimeUtc       = XfsTimestampToDateTime(inode.di_atime),
            LastWriteTimeUtc    = XfsTimestampToDateTime(inode.di_mtime),
            StatusChangeTimeUtc = XfsTimestampToDateTime(inode.di_ctime)
        };

        // v3 inodes have a creation time
        if(_v3Inodes) info.CreationTimeUtc = XfsTimestampToDateTime(inode.di_crtime);

        // Determine file type from di_mode
        info.Attributes = (inode.di_mode & S_IFMT) switch
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

        // XFS inode flags → file attributes
        if((inode.di_flags & XFS_DIFLAG_IMMUTABLE) != 0) info.Attributes |= FileAttributes.Immutable;

        if((inode.di_flags & XFS_DIFLAG_APPEND) != 0) info.Attributes |= FileAttributes.AppendOnly;

        return info;
    }

    /// <summary>
    ///     Converts an XFS on-disk timestamp (packed int64: upper 32 = seconds, lower 32 = nanoseconds)
    ///     to a DateTime in UTC.
    /// </summary>
    /// <param name="xfsTimestamp">The packed XFS timestamp</param>
    /// <returns>DateTime in UTC</returns>
    static DateTime XfsTimestampToDateTime(long xfsTimestamp)
    {
        var seconds     = (int)(xfsTimestamp >> 32);
        var nanoseconds = (int)(xfsTimestamp & 0xFFFFFFFF);

        return DateHandlers.UnixToDateTime(seconds).AddTicks(nanoseconds / 100);
    }
}