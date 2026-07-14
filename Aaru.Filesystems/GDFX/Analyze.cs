// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
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
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

public sealed partial class GDFX
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber GetFilesWithAffectedSectors(IEnumerable<(ulong Start, ulong End)> sectorExtents,
                                                   out List<FileSectorInfo>              files,
                                                   InitProgressHandler                   initProgress   = null,
                                                   UpdateProgressHandler                 updateProgress = null,
                                                   PulseProgressHandler                  pulseProgress  = null,
                                                   EndProgressHandler                    endProgress    = null)
    {
        files = [];

        if(!_mounted) return ErrorNumber.AccessDenied;
        if(sectorExtents is null) return ErrorNumber.InvalidArgument;

        List<(ulong Start, ulong End)> normalizedExtents = NormalizeAnalyzeExtents(sectorExtents);

        if(normalizedExtents.Count == 0) return ErrorNumber.NoError;

        try
        {
            initProgress?.Invoke();

            return TraverseDirectoryForAffectedSectors("/", normalizedExtents, files, updateProgress, pulseProgress);
        }
        finally
        {
            endProgress?.Invoke();
        }
    }

#endregion

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, List<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        if(!_directoryCache.TryGetValue(path, out List<DecodedEntry> entries)) return ErrorNumber.NoSuchFile;

        DecodedEntry[] ordered = entries.OrderBy(static e => e.Name, StringComparer.OrdinalIgnoreCase).ToArray();

        long maximum = ordered.Length > 0 ? ordered.Length : 1;

        for(var i = 0; i < ordered.Length; i++)
        {
            DecodedEntry entry     = ordered[i];
            string       entryPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            if(entry.IsDirectory)
            {
                ErrorNumber errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                        sectorExtents,
                                                                        files,
                                                                        updateProgress,
                                                                        pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            if(entry.DataSize == 0) continue;

            ulong absoluteStart = _partitionBaseOffset / SECTOR_SIZE + entry.DataSector + _partition.Start;
            ulong sectorCount   = (entry.DataSize + SECTOR_SIZE - 1) / SECTOR_SIZE;
            ulong absoluteEnd   = absoluteStart + sectorCount - 1;

            List<(ulong Start, ulong End)> overlaps = [];

            foreach((ulong Start, ulong End) extent in sectorExtents)
            {
                if(extent.End < absoluteStart || extent.Start > absoluteEnd) continue;

                overlaps.Add((Math.Max(absoluteStart, extent.Start), Math.Min(absoluteEnd, extent.End)));
            }

            if(overlaps.Count == 0) continue;

            files.Add(new FileSectorInfo
            {
                Path            = entryPath,
                Inode           = entry.DataSector,
                AffectedSectors = NormalizeAnalyzeExtents(overlaps)
            });
        }

        return ErrorNumber.NoError;
    }

    static List<(ulong Start, ulong End)> NormalizeAnalyzeExtents(IEnumerable<(ulong Start, ulong End)> extents)
    {
        var orderedExtents = extents.Where(static e => e.End >= e.Start)
                                    .OrderBy(static e => e.Start)
                                    .ThenBy(static e => e.End)
                                    .ToList();

        List<(ulong Start, ulong End)> normalized = [];

        if(orderedExtents.Count == 0) return normalized;

        (ulong Start, ulong End) current = orderedExtents[0];

        for(var i = 1; i < orderedExtents.Count; i++)
        {
            if(orderedExtents[i].Start <= current.End + 1)
            {
                current.End = Math.Max(current.End, orderedExtents[i].End);

                continue;
            }

            normalized.Add(current);
            current = orderedExtents[i];
        }

        normalized.Add(current);

        return normalized;
    }
}