// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ECMA-67 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Directory operations for the ECMA-67 file system.
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
/// <summary>Implements the filesystem described in ECMA-67</summary>
public sealed partial class ECMA67
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // ECMA-67 is a flat filesystem — only the root directory exists
        if(!string.IsNullOrEmpty(path) && !string.Equals(path, "/", StringComparison.OrdinalIgnoreCase))
            return ErrorNumber.NotDirectory;

        string[] contents = _fileLabels.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();

        node = new Ecma67DirNode
        {
            Path     = path ?? "/",
            Position = 0,
            Contents = contents
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not Ecma67DirNode myNode) return ErrorNumber.InvalidArgument;

        myNode.Position = -1;
        myNode.Contents = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not Ecma67DirNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(myNode.Position >= myNode.Contents.Length) return ErrorNumber.NoError;

        filename = myNode.Contents[myNode.Position++];

        return ErrorNumber.NoError;
    }

#endregion
}