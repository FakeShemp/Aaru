// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the filesystem used by Nintendo Gamecube and Wii discs</summary>
public sealed partial class NintendoPlugin
{
    /// <summary>Get the direct children of a directory FST entry</summary>
    /// <param name="dirIndex">FST index of the directory</param>
    /// <returns>Dictionary mapping child names to their FST indices</returns>
    Dictionary<string, int> GetDirectoryEntries(int dirIndex)
    {
        Dictionary<string, int> entries = new();

        uint dirEnd = _fstEntries[dirIndex].SizeOrNext;

        for(int i = dirIndex + 1; i < (int)dirEnd; i++)
        {
            bool isDirectory = _fstEntries[i].TypeAndNameOffset >> 24 != 0;

            if(isDirectory)
            {
                // A directory is a direct child if its parent index points to dirIndex
                if((int)_fstEntries[i].OffsetOrParent == dirIndex) entries[_fstNames[i]] = i;

                // Skip past this directory's children — they belong to it, not to us
                i = (int)_fstEntries[i].SizeOrNext - 1; // -1 because loop will i++
            }
            else
            {
                // Files between dirIndex+1 and dirEnd that are not inside a subdirectory
                // are direct children. Since we skip past subdirectories above, any file
                // we encounter here is a direct child.
                entries[_fstNames[i]] = i;
            }
        }

        return entries;
    }

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            node = new NintendoDirNode
            {
                Path     = "/",
                Contents = _rootDirectoryCache.Keys.ToArray(),
                Position = 0
            };

            return ErrorNumber.NoError;
        }

        // Normalize the path: strip leading slash, lowercase for comparison
        string cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        // Check if already cached
        if(_directoryCache.TryGetValue(cutPath, out Dictionary<string, int> cachedDir))
        {
            node = new NintendoDirNode
            {
                Path     = path,
                Contents = cachedDir.Keys.ToArray(),
                Position = 0
            };

            return ErrorNumber.NoError;
        }

        // Walk the path components to find the target directory
        string[] pieces          = cutPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var      currentDirIndex = 0; // Start at root

        for(var p = 0; p < pieces.Length; p++)
        {
            // Get the children of the current directory
            Dictionary<string, int> currentEntries = currentDirIndex == 0
                                                         ? _rootDirectoryCache
                                                         : GetDirectoryEntries(currentDirIndex);

            // Find the matching child
            KeyValuePair<string, int> match =
                currentEntries.FirstOrDefault(e => e.Key.Equals(pieces[p], StringComparison.OrdinalIgnoreCase));

            if(match.Key is null) return ErrorNumber.NoSuchFile;

            int entryIndex = match.Value;

            // Verify it's a directory
            if(_fstEntries[entryIndex].TypeAndNameOffset >> 24 == 0) return ErrorNumber.NotDirectory;

            currentDirIndex = entryIndex;

            // Cache intermediate directories
            var intermediatePath = string.Join("/", pieces, 0, p + 1);

            if(!_directoryCache.ContainsKey(intermediatePath))
                _directoryCache[intermediatePath] = GetDirectoryEntries(currentDirIndex);
        }

        // Build final directory listing
        Dictionary<string, int> entries = GetDirectoryEntries(currentDirIndex);
        _directoryCache[cutPath] = entries;

        node = new NintendoDirNode
        {
            Path     = path,
            Contents = entries.Keys.ToArray(),
            Position = 0
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not NintendoDirNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(myNode.Position >= myNode.Contents.Length) return ErrorNumber.NoError;

        filename = myNode.Contents[myNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not NintendoDirNode myNode) return ErrorNumber.InvalidArgument;

        myNode.Position = -1;
        myNode.Contents = null;

        return ErrorNumber.NoError;
    }

#endregion
}