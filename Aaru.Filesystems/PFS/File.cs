// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Professional File System plugin.
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
public sealed partial class PFS
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "GetAttributes: path='{0}'", path);

        // Use Stat to get file information
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

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                Inode      = ANODE_ROOTDIR,
                Links      = 1,
                BlockSize  = _blockSize,
                CreationTimeUtc =
                    DateHandlers.AmigaToDateTime(_rootBlock.creationday,
                                                 _rootBlock.creationminute,
                                                 _rootBlock.creationtick),
                LastWriteTimeUtc = DateHandlers.AmigaToDateTime(_rootBlock.creationday,
                                                                _rootBlock.creationminute,
                                                                _rootBlock.creationtick)
            };

            return ErrorNumber.NoError;
        }

        // Find the entry
        ErrorNumber errno = GetEntryForPath(normalizedPath, out DirEntryCacheItem entry);

        if(errno != ErrorNumber.NoError) return errno;

        // Build stat from entry
        stat = BuildStatFromEntry(entry);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets the directory entry for a given path</summary>
    /// <param name="path">The path to find</param>
    /// <param name="entry">The directory entry</param>
    /// <returns>Error code</returns>
    ErrorNumber GetEntryForPath(string path, out DirEntryCacheItem entry)
    {
        entry = null;

        // Remove leading slash
        string pathWithoutLeadingSlash = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirEntryCacheItem> currentEntries = _rootDirectoryCache;
        DirEntryCacheItem                     currentEntry   = null;

        // Traverse each path component
        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            // Find the component in current directory (case-insensitive)
            string foundKey = null;

            foreach(string key in currentEntries.Keys)
            {
                if(string.Equals(key, component, StringComparison.OrdinalIgnoreCase))
                {
                    foundKey = key;

                    break;
                }
            }

            if(foundKey == null)
            {
                AaruLogging.Debug(MODULE_NAME, "GetEntryForPath: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            currentEntry = currentEntries[foundKey];

            // If not the last component, it must be a directory
            if(i < pathComponents.Length - 1)
            {
                if(currentEntry.Type != EntryType.Directory && currentEntry.Type != EntryType.HardLinkDir)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "GetEntryForPath: '{0}' is not a directory (type={1})",
                                      component,
                                      currentEntry.Type);

                    return ErrorNumber.NotDirectory;
                }

                // Get the anode for this directory
                ErrorNumber errno = GetAnode(currentEntry.Anode, out Anode dirAnode);

                if(errno != ErrorNumber.NoError) return errno;

                // Read directory contents
                currentEntries = new Dictionary<string, DirEntryCacheItem>();
                errno          = ReadDirectoryBlocks(dirAnode, currentEntries);

                if(errno != ErrorNumber.NoError) return errno;
            }
        }

        entry = currentEntry;

        return ErrorNumber.NoError;
    }

    /// <summary>Builds a FileEntryInfo from a cached directory entry</summary>
    /// <param name="entry">The directory entry</param>
    /// <returns>FileEntryInfo structure</returns>
    FileEntryInfo BuildStatFromEntry(DirEntryCacheItem entry)
    {
        var stat = new FileEntryInfo
        {
            Inode           = entry.Anode,
            Links           = 1,
            BlockSize       = _blockSize,
            CreationTimeUtc = DateHandlers.AmigaToDateTime(entry.CreationDay, entry.CreationMinute, entry.CreationTick),
            LastWriteTimeUtc =
                DateHandlers.AmigaToDateTime(entry.CreationDay, entry.CreationMinute, entry.CreationTick),
            Mode = ProtectionToUnixMode(entry.Protection)
        };

        // Determine attributes from entry type
        switch(entry.Type)
        {
            case EntryType.Directory:
            case EntryType.HardLinkDir:
                stat.Attributes = FileAttributes.Directory;

                break;

            case EntryType.File:
            case EntryType.HardLinkFile:
            case EntryType.RolloverFile:
                stat.Attributes = FileAttributes.File;
                stat.Length     = entry.Size;

                break;

            case EntryType.SoftLink:
                stat.Attributes = FileAttributes.Symlink;

                break;

            default:
                stat.Attributes = FileAttributes.File;
                stat.Length     = entry.Size;

                break;
        }

        // Apply Amiga protection flags to attributes
        // Archive bit (active high)
        if(entry.Protection.HasFlag(ProtectionBits.Archive)) stat.Attributes |= FileAttributes.Archive;

        // Pure bit - maps to System (resident)
        if(entry.Protection.HasFlag(ProtectionBits.Pure)) stat.Attributes |= FileAttributes.System;

        // Calculate blocks used (for files)
        if(stat.Length > 0) stat.Blocks = (stat.Length + _blockSize - 1) / _blockSize;

        return stat;
    }

    /// <summary>Converts Amiga protection bits to Unix-style mode</summary>
    /// <param name="protect">Amiga protection bits</param>
    /// <returns>Unix-style mode</returns>
    static uint ProtectionToUnixMode(ProtectionBits protect)
    {
        // Amiga owner bits are active-low (0 = allowed), Unix are active-high
        uint mode = 0;

        // Owner permissions (active-low, so check if NOT set means allowed)
        if(!protect.HasFlag(ProtectionBits.Read)) // Read allowed
            mode |= 0x100;                        // S_IRUSR

        if(!protect.HasFlag(ProtectionBits.Write)) // Write allowed
            mode |= 0x080;                         // S_IWUSR

        if(!protect.HasFlag(ProtectionBits.Execute)) // Execute allowed
            mode |= 0x040;                           // S_IXUSR

        // Note: Group and Other permissions would require ExtendedProtectionBits
        // which are stored separately in ExtraFields. For basic PFS, we just
        // mirror owner permissions to group and other.
        if(!protect.HasFlag(ProtectionBits.Read))
        {
            mode |= 0x020; // S_IRGRP
            mode |= 0x004; // S_IROTH
        }

        if(!protect.HasFlag(ProtectionBits.Write))
        {
            mode |= 0x010; // S_IWGRP
            mode |= 0x002; // S_IWOTH
        }

        if(!protect.HasFlag(ProtectionBits.Execute))
        {
            mode |= 0x008; // S_IXGRP
            mode |= 0x001; // S_IXOTH
        }

        return mode;
    }
}