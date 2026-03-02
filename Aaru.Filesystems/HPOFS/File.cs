// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     File operations for the High Performance Optical File System.
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

public sealed partial class HPOFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string cleanPath = path.Replace('\\', '/').Trim('/');

        // Root directory
        if(string.IsNullOrEmpty(cleanPath))
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = _bpb.bps,
                Links      = 1
            };

            return ErrorNumber.NoError;
        }

        // Split into directory + filename
        int lastSep = cleanPath.LastIndexOf('/');

        CachedDirectoryEntry entry;

        if(lastSep < 0)
        {
            // Root-level entry
            if(!_rootDirectoryCache.TryGetValue(cleanPath, out entry)) return ErrorNumber.NoSuchFile;
        }
        else
        {
            string dirPath  = cleanPath[..lastSep];
            string fileName = cleanPath[(lastSep + 1)..];

            if(!_directoryCache.TryGetValue(dirPath, out Dictionary<string, CachedDirectoryEntry> dirEntries))
                return ErrorNumber.NoSuchFile;

            if(!dirEntries.TryGetValue(fileName, out entry)) return ErrorNumber.NoSuchFile;
        }

        FileAttributes attrs = FileAttributes.None;

        if(entry.IsDirectory) attrs              |= FileAttributes.Directory;
        if((entry.Attributes & 0x01) != 0) attrs |= FileAttributes.ReadOnly;
        if((entry.Attributes & 0x02) != 0) attrs |= FileAttributes.Hidden;
        if((entry.Attributes & 0x04) != 0) attrs |= FileAttributes.System;
        if((entry.Attributes & 0x20) != 0) attrs |= FileAttributes.Archive;

        stat = new FileEntryInfo
        {
            Attributes = attrs,
            BlockSize  = _bpb.bps,
            Links      = 1,
            Length     = entry.FileSize,
            Blocks     = entry.FileSize > 0 ? (entry.FileSize + _bpb.bps - 1) / _bpb.bps : 0,
            Inode      = entry.SectorAddress
        };

        if(entry.CreationTimestamp > 0)
            stat.CreationTimeUtc = DateTimeOffset.FromUnixTimeSeconds(entry.CreationTimestamp).UtcDateTime;

        if(entry.ModificationTimestamp > 0)
            stat.LastWriteTimeUtc = DateTimeOffset.FromUnixTimeSeconds(entry.ModificationTimestamp).UtcDateTime;

        return ErrorNumber.NoError;
    }
}