// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MicroDOS filesystem plugin
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class MicroDOS
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

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
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = BLOCK_SIZE,
                Blocks     = 1,
                Length     = BLOCK_SIZE
            };

            return ErrorNumber.NoError;
        }

        // Remove leading slash for lookup
        string filename = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        // MicroDOS has no subdirectories, so any path with slashes (after removing leading) is invalid
        if(filename.Contains('/'))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: MicroDOS does not support subdirectories");

            return ErrorNumber.NoSuchFile;
        }

        // Look up the file in the root directory cache
        if(!_rootDirectoryCache.TryGetValue(filename, out DirectoryEntry entry))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: File '{0}' not found", filename);

            return ErrorNumber.NoSuchFile;
        }

        stat = EntryToFileEntryInfo(entry);

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a MicroDOS directory entry to a FileEntryInfo structure</summary>
    /// <param name="entry">The MicroDOS directory entry</param>
    /// <returns>The FileEntryInfo structure</returns>
    static FileEntryInfo EntryToFileEntryInfo(DirectoryEntry entry)
    {
        var info = new FileEntryInfo
        {
            Attributes = FileAttributes.None,
            BlockSize  = BLOCK_SIZE,
            Blocks     = entry.blocks,
            Length     = entry.length
        };

        // Determine attributes based on status
        switch(entry.status)
        {
            case (byte)FileStatus.Protected:
                info.Attributes = FileAttributes.File | FileAttributes.ReadOnly;

                break;
            case (byte)FileStatus.LogicalDisk:
                info.Attributes = FileAttributes.File | FileAttributes.System;

                break;
            default:
                info.Attributes = FileAttributes.File;

                break;
        }

        return info;
    }
}