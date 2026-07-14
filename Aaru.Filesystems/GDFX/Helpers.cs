// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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

public sealed partial class GDFX
{
    /// <summary>Resolves a path string to a directory entry in the cache.</summary>
    ErrorNumber ResolveEntry(string path, out DecodedEntry entry)
    {
        entry = null;

        string[] parts = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(parts.Length == 0) return ErrorNumber.InvalidArgument;

        string dirPath  = parts.Length == 1 ? "/" : "/" + string.Join("/", parts[..^1]);
        string fileName = parts[^1];

        if(!_directoryCache.TryGetValue(dirPath, out List<DecodedEntry> dirEntries)) return ErrorNumber.NoSuchFile;

        foreach(DecodedEntry e in dirEntries)
        {
            if(!string.Equals(e.Name, fileName, StringComparison.OrdinalIgnoreCase)) continue;

            entry = e;

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }
}