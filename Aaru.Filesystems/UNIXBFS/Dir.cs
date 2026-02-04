// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class BFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // BFS only has a root directory - no subdirectories
        if(normalizedPath != "/" && !string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
            return ErrorNumber.NotDirectory;

        if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

        node = new BfsDirNode
        {
            Path     = "/",
            Position = 0,
            Entries  = _rootDirectoryCache.Keys.ToArray()
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not BfsDirNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not BfsDirNode bfsDirNode) return ErrorNumber.InvalidArgument;

        if(bfsDirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(bfsDirNode.Position >= bfsDirNode.Entries.Length) return ErrorNumber.NoError;

        filename = bfsDirNode.Entries[bfsDirNode.Position++];

        return ErrorNumber.NoError;
    }
}