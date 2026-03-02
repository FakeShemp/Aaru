// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Directory listing operations for the High Performance Optical File
//     System.
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

public sealed partial class HPOFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory case
        if(normalizedPath is "/" or "\\")
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new HpofsDirNode
            {
                Path     = "/",
                Position = 0,
                Contents = _rootDirectoryCache.Keys.ToArray(),
                Entries  = _rootDirectoryCache
            };

            return ErrorNumber.NoError;
        }

        // Strip leading slash
        string cleanPath = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                               ? normalizedPath[1..]
                               : normalizedPath;

        // Strip trailing slash
        if(cleanPath.EndsWith("/", StringComparison.Ordinal)) cleanPath = cleanPath[..^1];

        if(!_directoryCache.TryGetValue(cleanPath, out Dictionary<string, CachedDirectoryEntry> dirEntries))
            return ErrorNumber.NoSuchFile;

        node = new HpofsDirNode
        {
            Path     = normalizedPath,
            Position = 0,
            Contents = dirEntries.Keys.ToArray(),
            Entries  = dirEntries
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not HpofsDirNode hpofsNode) return ErrorNumber.InvalidArgument;

        hpofsNode.Position = -1;
        hpofsNode.Contents = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not HpofsDirNode hpofsNode) return ErrorNumber.InvalidArgument;

        if(hpofsNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(hpofsNode.Position >= hpofsNode.Contents.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = hpofsNode.Contents[hpofsNode.Position++];

        return ErrorNumber.NoError;
    }
}