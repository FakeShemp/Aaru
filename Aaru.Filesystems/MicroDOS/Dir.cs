// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MicroDOS filesystem plugin
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

using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class MicroDOS
{
    /// <summary>Processes a single directory entry and adds it to cache if valid</summary>
    /// <param name="entry">The directory entry to process</param>
    /// <param name="entriesRead">Counter for entries read (incremented for valid entries)</param>
    void ProcessDirectoryEntry(DirectoryEntry entry, ref int entriesRead)
    {
        // Skip deleted entries
        if(entry.status == (byte)FileStatus.Deleted) return;

        // Skip BAD-file entries (they mark bad blocks)
        if(entry.status == (byte)FileStatus.BadFile) return;

        // Skip subdirectory markers (first byte of filename = 0x7F)
        // These are just directory names, not actual files
        if(entry.filename is { Length: > 0 } && entry.filename[0] == SUBDIR_MARKER) return;

        // Skip entries not in root directory (directory byte != 0)
        if(entry.directory != 0) return;

        // Get filename
        string filename = StringHandlers.CToString(entry.filename, _encoding).Trim();

        if(string.IsNullOrWhiteSpace(filename)) return;

        if(_rootDirectoryCache.TryAdd(filename, entry))
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Found '{0}' (block={1}, blocks={2}, length={3})",
                              filename,
                              entry.blockNo,
                              entry.blocks,
                              entry.length);
        }

        entriesRead++;
    }
}