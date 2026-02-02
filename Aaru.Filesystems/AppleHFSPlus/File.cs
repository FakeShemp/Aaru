// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System Plus plugin.
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

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Get file/directory info using Stat
        ErrorNumber statErr = Stat(path, out FileEntryInfo stat);

        if(statErr != ErrorNumber.NoError) return statErr;

        // Set attributes based on stat information
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
            if(_rootFolder.folderID != kHFSRootFolderID) return ErrorNumber.InvalidArgument;

            FileAttributes attributes = FileAttributes.Directory;

            // Translate Finder flags to file attributes
            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsAlias))
                attributes |= FileAttributes.Alias;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasBundle))
                attributes |= FileAttributes.Bundle;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasBeenInited))
                attributes |= FileAttributes.HasBeenInited;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasCustomIcon))
                attributes |= FileAttributes.HasCustomIcon;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasNoINITs))
                attributes |= FileAttributes.HasNoINITs;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsInvisible))
                attributes |= FileAttributes.Hidden;

            // HFS+ specific: immutable flag from BSD adminFlags (UF_IMMUTABLE = 0x00000002)
            if((_rootFolder.permissions.adminFlags & 0x02) != 0) attributes |= FileAttributes.Immutable;

            // HFS+ specific: archived flag from BSD ownerFlags (SF_ARCHIVED = 0x00000001)
            if((_rootFolder.permissions.ownerFlags & 0x01) != 0) attributes |= FileAttributes.Archive;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsOnDesk))
                attributes |= FileAttributes.IsOnDesk;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsShared))
                attributes |= FileAttributes.Shared;

            if(_rootFolder.userInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsStationery))
                attributes |= FileAttributes.Stationery;

            stat = new FileEntryInfo
            {
                Attributes       = attributes,
                BlockSize        = _volumeHeader.blockSize,
                Inode            = _rootFolder.folderID,
                Links            = 1,
                CreationTime     = DateHandlers.MacToDateTime(_rootFolder.createDate),
                LastWriteTime    = DateHandlers.MacToDateTime(_rootFolder.contentModDate),
                LastWriteTimeUtc = DateHandlers.MacToDateTime(_rootFolder.contentModDate),
                AccessTime       = DateHandlers.MacToDateTime(_rootFolder.accessDate),
                AccessTimeUtc    = DateHandlers.MacToDateTime(_rootFolder.accessDate),
                BackupTime       = DateHandlers.MacToDateTime(_rootFolder.backupDate),
                BackupTimeUtc    = DateHandlers.MacToDateTime(_rootFolder.backupDate),
                UID              = _rootFolder.permissions.ownerID,
                GID              = _rootFolder.permissions.groupID,
                Mode             = (uint)_rootFolder.permissions.fileMode & 0x0000FFFF
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
        uint                             currentDirCNID   = kHFSRootFolderID;

        // Traverse through all but the last path component
        for(var p = 0; p < pieces.Length - 1; p++)
        {
            string component = pieces[p];

            // Convert colons back to slashes for catalog lookup
            component = component.Replace(":", "/");

            // Look for the component in current directory
            CatalogEntry foundEntry = null;

            if(currentDirectory != null)
            {
                foreach(KeyValuePair<string, CatalogEntry> entry in currentDirectory)
                {
                    if(string.Equals(entry.Key, component, StringComparison.OrdinalIgnoreCase))
                    {
                        foundEntry = entry.Value;

                        break;
                    }
                }
            }

            if(foundEntry == null) return ErrorNumber.NoSuchFile;

            // Check if it's a directory
            if(foundEntry.Type != (int)BTreeRecordType.kHFSPlusFolderRecord) return ErrorNumber.NotDirectory;

            // Update current directory info
            currentDirCNID = foundEntry.CNID;

            // Load next directory level
            ErrorNumber cacheErr = CacheDirectoryIfNeeded(currentDirCNID);

            if(cacheErr != ErrorNumber.NoError) return cacheErr;

            currentDirectory = GetDirectoryEntries(currentDirCNID);

            if(currentDirectory == null) return ErrorNumber.NoSuchFile;
        }

        // Now look for the final component
        string lastComponent = pieces[pieces.Length - 1];

        // Convert colons back to slashes for catalog lookup
        lastComponent = lastComponent.Replace(":", "/");

        CatalogEntry finalEntry = null;

        if(currentDirectory != null)
        {
            foreach(KeyValuePair<string, CatalogEntry> entry in currentDirectory)
            {
                if(string.Equals(entry.Key, lastComponent, StringComparison.OrdinalIgnoreCase))
                {
                    finalEntry = entry.Value;

                    break;
                }
            }
        }

        if(finalEntry == null) return ErrorNumber.NoSuchFile;

        // Populate stat info based on entry type
        if(finalEntry is DirectoryEntry dirEntry)
        {
            FileAttributes attributes = FileAttributes.Directory;

            // Translate Finder flags to file attributes
            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsAlias))
                attributes |= FileAttributes.Alias;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasBundle))
                attributes |= FileAttributes.Bundle;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasBeenInited))
                attributes |= FileAttributes.HasBeenInited;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasCustomIcon))
                attributes |= FileAttributes.HasCustomIcon;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kHasNoINITs))
                attributes |= FileAttributes.HasNoINITs;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsInvisible))
                attributes |= FileAttributes.Hidden;

            // HFS+ specific: immutable flag from BSD adminFlags (UF_IMMUTABLE = 0x00000002)
            if((dirEntry.permissions.adminFlags & 0x02) != 0) attributes |= FileAttributes.Immutable;

            // HFS+ specific: archived flag from BSD ownerFlags (SF_ARCHIVED = 0x00000001)
            if((dirEntry.permissions.ownerFlags & 0x01) != 0) attributes |= FileAttributes.Archive;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsOnDesk))
                attributes |= FileAttributes.IsOnDesk;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsShared))
                attributes |= FileAttributes.Shared;

            if(dirEntry.FinderInfo.frFlags.HasFlag(AppleCommon.FinderFlags.kIsStationery))
                attributes |= FileAttributes.Stationery;

            stat = new FileEntryInfo
            {
                Attributes       = attributes,
                BlockSize        = _volumeHeader.blockSize,
                Inode            = dirEntry.CNID,
                Links            = 1,
                CreationTime     = DateHandlers.MacToDateTime(dirEntry.CreationDate),
                LastWriteTime    = DateHandlers.MacToDateTime(dirEntry.ContentModDate),
                LastWriteTimeUtc = DateHandlers.MacToDateTime(dirEntry.ContentModDate),
                AccessTime       = DateHandlers.MacToDateTime(dirEntry.AccessDate),
                AccessTimeUtc    = DateHandlers.MacToDateTime(dirEntry.AccessDate),
                BackupTime       = DateHandlers.MacToDateTime(dirEntry.BackupDate),
                BackupTimeUtc    = DateHandlers.MacToDateTime(dirEntry.BackupDate),
                UID              = dirEntry.permissions.ownerID,
                GID              = dirEntry.permissions.groupID,
                Mode             = (uint)dirEntry.permissions.fileMode & 0x0000FFFF
            };
        }
        else if(finalEntry is FileEntry fileEntry)
        {
            // Use data fork size as file length
            ulong fileSize = fileEntry.DataForkLogicalSize;

            FileAttributes attributes = FileAttributes.File;

            // Translate Finder flags to file attributes
            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsAlias))
                attributes |= FileAttributes.Alias;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasBundle))
                attributes |= FileAttributes.Bundle;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasBeenInited))
                attributes |= FileAttributes.HasBeenInited;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasCustomIcon))
                attributes |= FileAttributes.HasCustomIcon;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kHasNoINITs))
                attributes |= FileAttributes.HasNoINITs;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsInvisible))
                attributes |= FileAttributes.Hidden;

            // HFS+ specific: immutable flag from BSD adminFlags (UF_IMMUTABLE = 0x00000002)
            if((fileEntry.permissions.adminFlags & 0x02) != 0) attributes |= FileAttributes.Immutable;

            // HFS+ specific: archived flag from BSD ownerFlags (SF_ARCHIVED = 0x00000001)
            if((fileEntry.permissions.ownerFlags & 0x01) != 0) attributes |= FileAttributes.Archive;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsOnDesk))
                attributes |= FileAttributes.IsOnDesk;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsShared))
                attributes |= FileAttributes.Shared;

            if(fileEntry.FinderInfo.fdFlags.HasFlag(AppleCommon.FinderFlags.kIsStationery))
                attributes |= FileAttributes.Stationery;

            if(!attributes.HasFlag(FileAttributes.Alias)  &&
               !attributes.HasFlag(FileAttributes.Bundle) &&
               !attributes.HasFlag(FileAttributes.Stationery))
                attributes |= FileAttributes.File;

            stat = new FileEntryInfo
            {
                Attributes       = attributes,
                Blocks           = (long)((fileSize + _volumeHeader.blockSize - 1) / _volumeHeader.blockSize),
                BlockSize        = _volumeHeader.blockSize,
                Length           = (long)fileSize,
                Inode            = fileEntry.CNID,
                Links            = 1,
                CreationTime     = DateHandlers.MacToDateTime(fileEntry.CreationDate),
                LastWriteTime    = DateHandlers.MacToDateTime(fileEntry.ContentModDate),
                LastWriteTimeUtc = DateHandlers.MacToDateTime(fileEntry.ContentModDate),
                AccessTime       = DateHandlers.MacToDateTime(fileEntry.AccessDate),
                AccessTimeUtc    = DateHandlers.MacToDateTime(fileEntry.AccessDate),
                BackupTime       = DateHandlers.MacToDateTime(fileEntry.BackupDate),
                BackupTimeUtc    = DateHandlers.MacToDateTime(fileEntry.BackupDate),
                UID              = fileEntry.permissions.ownerID,
                GID              = fileEntry.permissions.groupID,
                Mode             = (uint)fileEntry.permissions.fileMode & 0x0000FFFF
            };
        }
        else
            return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }
}