// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft exFAT filesystem plugin.
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

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
public sealed partial class exFAT
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out CommonTypes.Structs.FileAttributes attributes)
    {
        attributes = new CommonTypes.Structs.FileAttributes();

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetFileEntry(path, out CompleteDirectoryEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        if(entry.IsDirectory)
            attributes |= CommonTypes.Structs.FileAttributes.Directory;
        else
            attributes |= CommonTypes.Structs.FileAttributes.File;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.ReadOnly))
            attributes |= CommonTypes.Structs.FileAttributes.ReadOnly;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.Hidden))
            attributes |= CommonTypes.Structs.FileAttributes.Hidden;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.System))
            attributes |= CommonTypes.Structs.FileAttributes.System;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.Archive))
            attributes |= CommonTypes.Structs.FileAttributes.Archive;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetFileEntry(path, out CompleteDirectoryEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        stat = new FileEntryInfo
        {
            Attributes = new CommonTypes.Structs.FileAttributes(),
            BlockSize  = _bytesPerCluster,
            Length     = (long)entry.DataLength,
            Inode      = entry.FirstCluster,
            Links      = 1
        };

        // Calculate blocks
        stat.Blocks = (long)(entry.DataLength / _bytesPerCluster);

        if(entry.DataLength % _bytesPerCluster > 0) stat.Blocks++;

        // Convert timestamps
        stat.CreationTimeUtc = DateHandlers.ExFatToDateTime(entry.FileEntry.CreateTimestamp,
                                                            entry.FileEntry.Create10msIncrement,
                                                            entry.FileEntry.CreateUtcOffset);

        stat.LastWriteTimeUtc = DateHandlers.ExFatToDateTime(entry.FileEntry.LastModifiedTimestamp,
                                                             entry.FileEntry.LastModified10msIncrement,
                                                             entry.FileEntry.LastModifiedUtcOffset);

        stat.AccessTimeUtc = DateHandlers.ExFatToDateTime(entry.FileEntry.LastAccessedTimestamp,
                                                          0,
                                                          entry.FileEntry.LastAccessedUtcOffset);

        // Set file attributes
        if(entry.IsDirectory)
            stat.Attributes |= CommonTypes.Structs.FileAttributes.Directory;
        else
            stat.Attributes |= CommonTypes.Structs.FileAttributes.File;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.ReadOnly))
            stat.Attributes |= CommonTypes.Structs.FileAttributes.ReadOnly;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.Hidden))
            stat.Attributes |= CommonTypes.Structs.FileAttributes.Hidden;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.System))
            stat.Attributes |= CommonTypes.Structs.FileAttributes.System;

        if(entry.FileEntry.FileAttributes.HasFlag(FileAttributes.Archive))
            stat.Attributes |= CommonTypes.Structs.FileAttributes.Archive;

        return ErrorNumber.NoError;
    }

    // ...existing code...
    /// <param name="path">Path to the file or directory.</param>
    /// <param name="entry">The directory entry if found.</param>
    /// <returns>Error number.</returns>
    ErrorNumber GetFileEntry(string path, out CompleteDirectoryEntry entry)
    {
        entry = null;

        if(string.IsNullOrWhiteSpace(path) || path == "/") return ErrorNumber.IsDirectory;

        string cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] pieces = cutPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pieces.Length == 0) return ErrorNumber.IsDirectory;

        // Get parent directory
        string parentPath;
        string fileName;

        if(pieces.Length == 1)
        {
            parentPath = "/";
            fileName   = pieces[0];
        }
        else
        {
            parentPath = string.Join("/", pieces, 0, pieces.Length - 1);
            fileName   = pieces[^1];
        }

        ErrorNumber errno =
            GetDirectoryEntries(parentPath, out Dictionary<string, CompleteDirectoryEntry> parentEntries);

        if(errno != ErrorNumber.NoError) return errno;

        // Find the entry (case-insensitive per exFAT spec)
        foreach(KeyValuePair<string, CompleteDirectoryEntry> kvp in parentEntries)
        {
            if(kvp.Key.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                entry = kvp.Value;

                return ErrorNumber.NoError;
            }
        }

        return ErrorNumber.NoSuchFile;
    }
}