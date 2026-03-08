// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AO-DOS file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     File operations for the AO-DOS file system plugin.
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
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class AODOS
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

        // Root directory
        if(normalizedPath == "/")
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = SECTOR_SIZE,
                Blocks     = 1,
                Length     = SECTOR_SIZE
            };

            return ErrorNumber.NoError;
        }

        // Split into path components
        string[] components = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        switch(components.Length)
        {
            case 1:
            {
                // Single component: look in root directory (directory == 0)
                string filename = components[0];

                DirectoryEntry? found = _directoryCache.Where(static e => e.directory == 0)
                                                       .Cast<DirectoryEntry?>()
                                                       .FirstOrDefault(e => string.Equals(StringHandlers
                                                                              .CToString(e.Value.filename,
                                                                                   _encoding)
                                                                              .Trim(),
                                                                           filename,
                                                                           StringComparison.OrdinalIgnoreCase));

                if(found is null)
                {
                    AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' not found in root", filename);

                    return ErrorNumber.NoSuchFile;
                }

                stat = EntryToFileEntryInfo(found.Value);

                return ErrorNumber.NoError;
            }
            case 2:
            {
                // Two components: resolve subdirectory then find file
                string dirName  = components[0];
                string filename = components[1];

                // Find the directory marker
                DirectoryEntry? dirMarker = _directoryCache
                                           .Where(static e => e is { directoryNumber: > 0, directory: 0 })
                                           .Cast<DirectoryEntry?>()
                                           .FirstOrDefault(e => string.Equals(StringHandlers
                                                                             .CToString(e.Value.filename, _encoding)
                                                                             .Trim(),
                                                                              dirName,
                                                                              StringComparison.OrdinalIgnoreCase));

                if(dirMarker is null)
                {
                    AaruLogging.Debug(MODULE_NAME, "Stat: directory '{0}' not found", dirName);

                    return ErrorNumber.NoSuchFile;
                }

                byte dirNumber = dirMarker.Value.directoryNumber;

                // Find the file in the subdirectory
                DirectoryEntry? found = _directoryCache.Where(e => e.directory == dirNumber)
                                                       .Cast<DirectoryEntry?>()
                                                       .FirstOrDefault(e => string.Equals(StringHandlers
                                                                              .CToString(e.Value.filename,
                                                                                   _encoding)
                                                                              .Trim(),
                                                                           filename,
                                                                           StringComparison.OrdinalIgnoreCase));

                if(found is null)
                {
                    AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' not found in directory '{1}'", filename, dirName);

                    return ErrorNumber.NoSuchFile;
                }

                stat = EntryToFileEntryInfo(found.Value);

                return ErrorNumber.NoError;
            }
            default:
                // AO-DOS only supports one level of subdirectories
                AaruLogging.Debug(MODULE_NAME, "Stat: path too deep");

                return ErrorNumber.NoSuchFile;
        }
    }

    /// <summary>Converts an AO-DOS directory entry to a <see cref="FileEntryInfo" /></summary>
    /// <param name="entry">The directory entry</param>
    /// <returns>The file entry info</returns>
    static FileEntryInfo EntryToFileEntryInfo(in DirectoryEntry entry)
    {
        // Directory markers (directoryNumber > 0) are directories
        if(entry.directoryNumber > 0)
        {
            return new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = SECTOR_SIZE
            };
        }

        return new FileEntryInfo
        {
            Attributes = FileAttributes.File,
            BlockSize  = SECTOR_SIZE,
            Blocks     = entry.blocks,
            Length     = entry.length,
            Links      = 1
        };
    }
}