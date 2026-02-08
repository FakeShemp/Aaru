// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles file operations
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetEntry(path, out DirectoryEntryInfo entry);

        if(err != ErrorNumber.NoError) return err;

        stat = EntryToFileEntryInfo(entry);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets a directory entry by path</summary>
    /// <param name="path">Path to the entry</param>
    /// <param name="entry">Output directory entry</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetEntry(string path, out DirectoryEntryInfo entry)
    {
        entry = null;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory case - return a dummy entry for root
        if(normalizedPath == "/")
        {
            entry = new DirectoryEntryInfo
            {
                Name       = "/",
                LoadAddr   = 0,
                ExecAddr   = 0,
                Length     = 0,
                IndAddr    = _rootDirectoryAddress,
                Attributes = 0x08 // Directory attribute
            };

            return ErrorNumber.NoError;
        }

        // Parse the path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo foundEntry)) return ErrorNumber.NoSuchFile;

            // If this is the last component, return this entry
            if(p == pathComponents.Length - 1)
            {
                entry = foundEntry;

                return ErrorNumber.NoError;
            }

            // Not the last component - check if it's a directory and traverse
            if((foundEntry.Attributes & 0x08) == 0) return ErrorNumber.NotDirectory;

            // Read the subdirectory
            ErrorNumber errno =
                ReadDirectoryContents(foundEntry.IndAddr, out Dictionary<string, DirectoryEntryInfo> subDirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = subDirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts a directory entry to a FileEntryInfo structure</summary>
    /// <param name="entry">Directory entry info</param>
    /// <returns>FileEntryInfo structure</returns>
    FileEntryInfo EntryToFileEntryInfo(DirectoryEntryInfo entry)
    {
        var info = new FileEntryInfo
        {
            Inode     = entry.IndAddr,
            Length    = entry.Length,
            BlockSize = _blockSize,
            Blocks    = (entry.Length + _blockSize - 1) / _blockSize,
            Links     = 1
        };

        // Set attributes
        var attrs = (FileAttributes)entry.Attributes;

        info.Attributes = attrs.HasFlag(FileAttributes.Directory)
                              ? CommonTypes.Structs.FileAttributes.Directory
                              : CommonTypes.Structs.FileAttributes.File;

        if(attrs.HasFlag(FileAttributes.Locked)) info.Attributes |= CommonTypes.Structs.FileAttributes.ReadOnly;

        // Check for symbolic link (filetype 0xFC0 = LinkFS)
        if(HasFiletype(entry.LoadAddr) && GetFiletype(entry.LoadAddr) == FILETYPE_LINKFS)
            info.Attributes |= CommonTypes.Structs.FileAttributes.Symlink;

        // Convert RISC OS timestamp to DateTime if the file is stamped
        // RISC OS timestamp: 40-bit centi-second value since 1 Jan 1900
        // When load address bits [31:20] == 0xFFF, it's a stamped file:
        // - load address bits [7:0] = top 8 bits of timestamp
        // - exec address = bottom 32 bits of timestamp
        if(!HasFiletype(entry.LoadAddr)) return info;

        // File is stamped - extract timestamp
        ulong timestamp = (ulong)(entry.LoadAddr & 0xFF) << 32 | entry.ExecAddr;

        // Convert from centi-seconds to ticks (100ns intervals)
        // 1 centi-second = 10,000,000 nanoseconds = 100,000 ticks
        const long ticksPerCentiSecond = 100000;

        // RISC OS epoch: 1 Jan 1900 00:00:00
        var riscOsEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var ticks = (long)(timestamp * ticksPerCentiSecond);

        // Check if the resulting date would be valid
        if(ticks >= 0 && ticks <= DateTime.MaxValue.Ticks - riscOsEpoch.Ticks)
        {
            info.LastWriteTimeUtc = riscOsEpoch.AddTicks(ticks);
            info.AccessTimeUtc    = info.LastWriteTimeUtc;
        }

        return info;
    }
}