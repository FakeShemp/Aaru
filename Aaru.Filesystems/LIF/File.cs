// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : HP Logical Interchange Format plugin
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;

namespace Aaru.Filesystems;

public sealed partial class LIF
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || string.Equals(path, "/", StringComparison.OrdinalIgnoreCase))
        {
            stat = new FileEntryInfo
            {
                Attributes   = FileAttributes.Directory,
                BlockSize    = 256,
                CreationTime = DateHandlers.LifToDateTime(_systemBlock.creationDate)
            };

            return ErrorNumber.NoError;
        }

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        string filename = pathElements[0];

        foreach(DirectoryEntry entry in _rootDirectoryCache)
        {
            string entryName = StringHandlers.CToString(entry.fileName, _encoding).TrimEnd();

            if(!string.Equals(entryName, filename, StringComparison.OrdinalIgnoreCase)) continue;

            stat = new FileEntryInfo
            {
                Attributes   = FileAttributes.File,
                BlockSize    = 256,
                Blocks       = entry.fileLength,
                Length       = entry.fileLength * 256,
                CreationTime = DateHandlers.LifToDateTime(entry.creationDate),
                Links        = 1
            };

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }
}