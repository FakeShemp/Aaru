// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System plugin.
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

// ReSharper disable UnusedType.Local

// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Local

using System;
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;

namespace Aaru.Filesystems;

// Information from Inside Macintosh
// https://developer.apple.com/legacy/library/documentation/mac/pdf/Files/File_Manager.pdf
public sealed partial class AppleHFS
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Get file/directory info using Stat
        ErrorNumber statErr = Stat(path, out FileEntryInfo stat);

        if(statErr != ErrorNumber.NoError) return statErr;

        // Set attributes based on file type
        attributes = stat.Attributes;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrEmpty(path) ? "/" : path;

        // Root directory case
        if(string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            if(_rootDirectory.dirDirID != kRootCnid) return ErrorNumber.InvalidArgument;

            stat = new FileEntryInfo
            {
                Attributes       = FileAttributes.Directory,
                BlockSize        = _mdb.drAlBlkSiz,
                Inode            = _rootDirectory.dirDirID,
                Links            = 1,
                CreationTime     = DateHandlers.MacToDateTime(_rootDirectory.dirCrDat),
                LastWriteTime    = DateHandlers.MacToDateTime(_rootDirectory.dirMdDat),
                LastWriteTimeUtc = DateHandlers.MacToDateTime(_rootDirectory.dirMdDat),
                AccessTime       = DateHandlers.MacToDateTime(_rootDirectory.dirMdDat),
                AccessTimeUtc    = DateHandlers.MacToDateTime(_rootDirectory.dirMdDat),
                BackupTime       = DateHandlers.MacToDateTime(_rootDirectory.dirBkDat),
                BackupTimeUtc    = DateHandlers.MacToDateTime(_rootDirectory.dirBkDat)
            };

            return ErrorNumber.NoError;
        }

        // Parse path components
        string cutPath = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                             ? normalizedPath[1..]
                             : normalizedPath;

        string[] pieces = cutPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pieces.Length == 0) return ErrorNumber.NoSuchFile;

        // Start from root directory
        Dictionary<string, CatalogEntry> currentDirectory = _rootDirectoryCache;
        uint                             currentDirCNID   = kRootCnid;

        // Traverse through all but the last path component
        for(var p = 0; p < pieces.Length - 1; p++)
        {
            string component = pieces[p];

            // Replace ':' back to '/' in names
            component = component.Replace(":", "/");

            // Look for the component in current directory
            KeyValuePair<string, CatalogEntry> foundEntry = default;
            var                                found      = false;

            if(currentDirectory != null)
            {
                foreach(KeyValuePair<string, CatalogEntry> entry in currentDirectory)
                {
                    if(string.Equals(entry.Key, component, StringComparison.OrdinalIgnoreCase))
                    {
                        foundEntry = entry;
                        found      = true;

                        break;
                    }
                }
            }

            if(!found) return ErrorNumber.NoSuchFile;

            CatalogEntry catalogEntry = foundEntry.Value;

            // Check if it's a directory
            if(catalogEntry.Type != kCatalogRecordTypeDirectory) return ErrorNumber.NotDirectory;

            // Update current directory info
            currentDirCNID = catalogEntry.CNID;

            // Load next directory level
            ErrorNumber cacheErr = CacheDirectoryIfNeeded(currentDirCNID);

            if(cacheErr != ErrorNumber.NoError) return cacheErr;

            currentDirectory = GetDirectoryEntries(currentDirCNID);

            if(currentDirectory == null) return ErrorNumber.NoSuchFile;
        }

        // Now look for the final component
        string lastComponent = pieces[pieces.Length - 1];

        // Replace ':' back to '/' in names
        lastComponent = lastComponent.Replace(":", "/");

        KeyValuePair<string, CatalogEntry> finalEntry = default;
        var                                foundFinal = false;

        if(currentDirectory != null)
        {
            foreach(KeyValuePair<string, CatalogEntry> entry in currentDirectory)
            {
                if(string.Equals(entry.Key, lastComponent, StringComparison.OrdinalIgnoreCase))
                {
                    finalEntry = foundFinal ? throw new InvalidOperationException("Duplicate file entries") : entry;
                    foundFinal = true;
                }
            }
        }

        if(!foundFinal) return ErrorNumber.NoSuchFile;

        CatalogEntry finalCatalogEntry = finalEntry.Value;

        // Populate stat info based on entry type
        if(finalCatalogEntry is DirectoryEntry dirEntry)
        {
            stat = new FileEntryInfo
            {
                Attributes       = FileAttributes.Directory,
                BlockSize        = _mdb.drAlBlkSiz,
                Inode            = dirEntry.CNID,
                Links            = 1,
                CreationTime     = DateHandlers.MacToDateTime(dirEntry.CreationDate),
                LastWriteTime    = DateHandlers.MacToDateTime(dirEntry.ModificationDate),
                LastWriteTimeUtc = DateHandlers.MacToDateTime(dirEntry.ModificationDate),
                AccessTime       = DateHandlers.MacToDateTime(dirEntry.ModificationDate),
                AccessTimeUtc    = DateHandlers.MacToDateTime(dirEntry.ModificationDate),
                BackupTime       = DateHandlers.MacToDateTime(dirEntry.BackupDate),
                BackupTimeUtc    = DateHandlers.MacToDateTime(dirEntry.BackupDate)
            };
        }
        else if(finalCatalogEntry is FileEntry fileEntry)
        {
            // Use data fork size as file length
            uint fileSize = fileEntry.DataForkLogicalSize;

            stat = new FileEntryInfo
            {
                Attributes       = FileAttributes.File,
                Blocks           = (fileSize + _mdb.drAlBlkSiz - 1) / _mdb.drAlBlkSiz,
                BlockSize        = _mdb.drAlBlkSiz,
                Length           = fileSize,
                Inode            = fileEntry.CNID,
                Links            = 1,
                CreationTime     = DateHandlers.MacToDateTime(fileEntry.CreationDate),
                LastWriteTime    = DateHandlers.MacToDateTime(fileEntry.ModificationDate),
                LastWriteTimeUtc = DateHandlers.MacToDateTime(fileEntry.ModificationDate),
                AccessTime       = DateHandlers.MacToDateTime(fileEntry.ModificationDate),
                BackupTime       = DateHandlers.MacToDateTime(fileEntry.BackupDate),
                BackupTimeUtc    = DateHandlers.MacToDateTime(fileEntry.BackupDate)
            };
        }
        else
            return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }
}