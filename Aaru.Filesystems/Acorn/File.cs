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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
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
}