// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Cram
{
    // POSIX file type constants (from mode)
    const ushort S_IFMT   = 0xF000; // File type mask
    const ushort S_IFSOCK = 0xC000; // Socket
    const ushort S_IFLNK  = 0xA000; // Symbolic link
    const ushort S_IFREG  = 0x8000; // Regular file
    const ushort S_IFBLK  = 0x6000; // Block device
    const ushort S_IFDIR  = 0x4000; // Directory
    const ushort S_IFCHR  = 0x2000; // Character device
    const ushort S_IFIFO  = 0x1000; // FIFO (named pipe)

    // Permission mask
    const ushort S_IPERM = 0x0FFF; // Permission bits

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
            stat = InodeToFileEntryInfo(_superBlock.root);

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo entry))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // If this is the last component, return its stat
            if(p == pathComponents.Length - 1)
            {
                stat = InodeToFileEntryInfo(entry.Inode);

                return ErrorNumber.NoError;
            }

            // Not the last component - must be a directory
            if(!IsDirectory(entry.Inode.Mode))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents for next iteration
            uint dirOffset = entry.Inode.Offset << 2;
            uint dirSize   = entry.Inode.Size;

            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            ErrorNumber errno = ReadDirectoryContents(dirOffset, dirSize, dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory contents: {0}", errno);

                return errno;
            }

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }
}