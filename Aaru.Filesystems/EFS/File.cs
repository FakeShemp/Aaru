// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class EFS
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
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read root inode
            ErrorNumber errno = ReadInode(EFS_ROOTINO, out Inode rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInode, EFS_ROOTINO);

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out uint inodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Read the inode
            ErrorNumber errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading inode {0}: {1}", inodeNumber, errno);

                return errno;
            }

            // If this is the last component, return its stat
            if(p == pathComponents.Length - 1)
            {
                stat = InodeToFileEntryInfo(inode, inodeNumber);

                return ErrorNumber.NoError;
            }

            // Not the last component - must be a directory
            var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

            if(fileType != FileType.IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents for next iteration
            errno = ReadDirectoryContents(inode, out Dictionary<string, uint> dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory contents: {0}", errno);

                return errno;
            }

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts an EFS inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The EFS inode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The FileEntryInfo structure</returns>
    static FileEntryInfo InodeToFileEntryInfo(Inode inode, uint inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = EFS_BBSIZE,
            Inode               = inodeNumber,
            Length              = inode.di_size,
            Links               = (ulong)inode.di_nlink,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0x0FFF), // Lower 12 bits are permissions
            AccessTimeUtc       = DateHandlers.UnixToDateTime(inode.di_atime),
            LastWriteTimeUtc    = DateHandlers.UnixToDateTime(inode.di_mtime),
            StatusChangeTimeUtc = DateHandlers.UnixToDateTime(inode.di_ctime),
            CreationTimeUtc     = DateHandlers.UnixToDateTime(inode.di_ctime)
        };

        // Calculate blocks used from extents
        long blocksUsed = 0;

        if(inode.di_extents != null)
        {
            for(var i = 0; i < inode.di_numextents && i < EFS_DIRECTEXTENTS; i++)
            {
                if(inode.di_extents[i].Magic == 0) blocksUsed += inode.di_extents[i].Length;
            }
        }

        info.Blocks = blocksUsed;

        // Determine file type from di_mode
        var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

        info.Attributes = fileType switch
                          {
                              FileType.IFDIR    => FileAttributes.Directory,
                              FileType.IFREG    => FileAttributes.File,
                              FileType.IFLNK    => FileAttributes.Symlink,
                              FileType.IFCHR    => FileAttributes.CharDevice,
                              FileType.IFBLK    => FileAttributes.BlockDevice,
                              FileType.IFCHRLNK => FileAttributes.CharDevice,
                              FileType.IFBLKLNK => FileAttributes.BlockDevice,
                              _                 => FileAttributes.File
                          };

        return info;
    }
}