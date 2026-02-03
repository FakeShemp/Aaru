// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS old filesystem plugin.
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

using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;

namespace Aaru.Filesystems;

public sealed partial class BOFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(string.IsNullOrEmpty(path) || path == "/")
        {
            // Root directory - use mode from superblock
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                Inode      = 0,
                Links      = 2, // . and ..
                Mode       = (uint)_track0.RootMode,
                Length     = 0,
                Blocks     = 0,
                BlockSize  = _track0.BytesPerSector
            };

            return ErrorNumber.NoError;
        }

        // Look up file in root directory cache
        lock(_rootDirectoryCache)
        {
            if(!_rootDirectoryCache.TryGetValue(path.TrimStart('/'), out FileEntry entry))
                return ErrorNumber.NoSuchFile;

            var stat_info = new FileEntryInfo
            {
                Inode  = (ulong)entry.RecordId,
                Links  = 1,
                Length = entry.LogicalSize,
                Blocks = entry.PhysicalSize > 0
                             ? (long)((ulong)(entry.PhysicalSize + _track0.BytesPerSector - 1) /
                                      (ulong)_track0.BytesPerSector)
                             : 0,
                BlockSize = _track0.BytesPerSector,
                Mode      = (uint)entry.Mode
            };

            // Set attributes based on FileType
            if(entry.FileType == DIR_TYPE)
                stat_info.Attributes |= FileAttributes.Directory;
            else
                stat_info.Attributes |= FileAttributes.File;

            // Convert BeOS timestamps (seconds since 1970) to .NET DateTime
            if(entry.CreationDate != 0) stat_info.CreationTimeUtc = DateHandlers.UnixToDateTime(entry.CreationDate);

            if(entry.ModificationDate != 0)
                stat_info.LastWriteTimeUtc = DateHandlers.UnixToDateTime(entry.ModificationDate);

            stat = stat_info;

            return ErrorNumber.NoError;
        }
    }
}