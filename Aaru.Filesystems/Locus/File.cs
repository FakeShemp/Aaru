// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
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

using System;
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Locus
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
            ErrorNumber errno = ReadInode(ROOT_INO, out Dinode rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInode, ROOT_INO);

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, int> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out int inodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Read the inode
            ErrorNumber errno = ReadInode(inodeNumber, out Dinode inode);

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
            var fileType = (FileMode)(inode.di_mode & (ushort)FileMode.IFMT);

            if(fileType != FileMode.IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents for next iteration
            errno = ReadDirectoryContents(inode, out Dictionary<string, int> dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory contents: {0}", errno);

                return errno;
            }

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts a Locus disk inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The Locus disk inode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The FileEntryInfo structure</returns>
    FileEntryInfo InodeToFileEntryInfo(Dinode inode, int inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = _blockSize,
            Inode               = (ulong)inodeNumber,
            Length              = inode.di_size,
            Links               = (ulong)inode.di_nlink,
            UID                 = (ulong)inode.di_uid,
            GID                 = (ulong)inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0x0FFF), // Lower 12 bits are permissions
            AccessTimeUtc       = DateHandlers.UnixToDateTime(inode.di_atime),
            LastWriteTimeUtc    = DateHandlers.UnixToDateTime(inode.di_mtime),
            StatusChangeTimeUtc = DateHandlers.UnixToDateTime(inode.di_ctime),
            CreationTimeUtc     = DateHandlers.UnixToDateTime(inode.di_ctime),
            Blocks              = inode.di_blocks
        };

        // Determine file type from di_mode
        var fileType = (FileMode)(inode.di_mode & (ushort)FileMode.IFMT);

        info.Attributes = fileType switch
                          {
                              FileMode.IFDIR => FileAttributes.Directory,
                              FileMode.IFREG => FileAttributes.File,
                              FileMode.IFIFO => FileAttributes.Pipe,
                              FileMode.IFCHR => FileAttributes.CharDevice,
                              FileMode.IFBLK => FileAttributes.BlockDevice,
                              FileMode.IFMPC => FileAttributes.CharDevice,
                              FileMode.IFMPB => FileAttributes.BlockDevice,
                              _              => FileAttributes.File
                          };

        // Check disk flags for symbolic link
        if((inode.di_dflag & (short)DiskFlags.DILINK) != 0) info.Attributes = FileAttributes.Symlink;

        // Check disk flags for socket
        if((inode.di_dflag & (short)DiskFlags.DISOCKET) != 0) info.Attributes = FileAttributes.Socket;

        // Check disk flags for hidden
        if((inode.di_dflag & (short)DiskFlags.DIHIDDEN) != 0) info.Attributes |= FileAttributes.Hidden;

        // Extract device numbers for block/character devices
        if(fileType is not (FileMode.IFCHR or FileMode.IFBLK or FileMode.IFMPC or FileMode.IFMPB) ||
           inode.di_addr is not { Length: > 0 })
            return info;

        // Device numbers are stored in the first address entry
        var dev = (uint)inode.di_addr[0];

        // Old Unix format: upper 8 bits are major, lower 8 bits are minor
        uint major = dev >> 8 & 0xFF;
        uint minor = dev      & 0xFF;

        info.DeviceNo = (ulong)major << 32 | minor;

        return info;
    }
}