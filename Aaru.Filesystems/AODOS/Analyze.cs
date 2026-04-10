// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AO-DOS file system plugin.
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
using Aaru.Helpers;

namespace Aaru.Filesystems;

// Information has been extracted looking at available disk images
/// <inheritdoc />
public sealed partial class AODOS
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

        if(normalizedExtents.Count == 0 || _directoryCache.Count == 0) return ErrorNumber.NoError;

        try
        {
            initProgress?.Invoke();
            pulseProgress?.Invoke("/");

            Dictionary<byte, string> directoryNames = [];

            foreach(DirectoryEntry entry in _directoryCache)
            {
                if(entry.directoryNumber == 0 || entry.directory != 0) continue;

                string directoryName = StringHandlers.CToString(entry.filename, _encoding).Trim();

                if(string.IsNullOrWhiteSpace(directoryName) || directoryNames.ContainsKey(entry.directoryNumber))
                    continue;

                directoryNames.Add(entry.directoryNumber, directoryName);
            }

            List<(string Path, DirectoryEntry Entry)> orderedEntries = [];

            foreach(DirectoryEntry entry in _directoryCache)
            {
                if(entry.directoryNumber > 0) continue;

                string fileName = StringHandlers.CToString(entry.filename, _encoding).Trim();

                if(string.IsNullOrWhiteSpace(fileName)) continue;

                if(entry.directory == 0)
                    orderedEntries.Add(("/" + fileName, entry));
                else if(directoryNames.TryGetValue(entry.directory, out string directoryName))
                    orderedEntries.Add(("/" + directoryName + "/" + fileName, entry));
            }

            orderedEntries = orderedEntries.OrderBy(static entry => entry.Path, StringComparer.CurrentCultureIgnoreCase)
                                           .ToList();

            long maximum = orderedEntries.Count > 0 ? orderedEntries.Count : 1;

            for(var i = 0; i < orderedEntries.Count; i++)
            {
                updateProgress?.Invoke(orderedEntries[i].Path, i + 1, maximum);
                AddOverlappingFile(orderedEntries[i].Path, orderedEntries[i].Entry, normalizedExtents, files);
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            endProgress?.Invoke();
        }
    }

#endregion

    void AddOverlappingFile(string path, in DirectoryEntry entry, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                            List<FileSectorInfo> files)
    {
        if(entry.directoryNumber > 0 || entry.length == 0 || entry.blocks == 0) return;

        List<(ulong Start, ulong End)> overlaps = FindOverlappingExtents(entry, sectorExtents);

        if(overlaps.Count == 0) return;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = entry.blockNumber,
            AffectedSectors = overlaps
        });
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(in DirectoryEntry                       entry,
                                                          IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps = [];

        ulong sectorCount = entry.blocks;

        if(entry.length > 0)
        {
            ulong logicalSectors = ((ulong)entry.length + SECTOR_SIZE - 1) / SECTOR_SIZE;

            if(logicalSectors > 0 && logicalSectors < sectorCount) sectorCount = logicalSectors;
        }

        if(sectorCount == 0) return overlaps;

        ulong startSector = _partition.Start + entry.blockNumber;

        AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);

        return NormalizeAnalyzeExtents(overlaps);
    }

    static void AddExtentOverlaps(ulong                                   startSector, ulong endSector,
                                  IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                  List<(ulong Start, ulong End)>          overlaps)
    {
        foreach((ulong Start, ulong End) requestedExtent in sectorExtents)
        {
            if(requestedExtent.End < startSector || requestedExtent.Start > endSector) continue;

            overlaps.Add((Math.Max(startSector, requestedExtent.Start), Math.Min(endSector, requestedExtent.End)));
        }
    }

    static List<(ulong Start, ulong End)> NormalizeAnalyzeExtents(IEnumerable<(ulong Start, ulong End)> extents)
    {
        var orderedExtents = extents.Where(static extent => extent.End >= extent.Start)
                                    .OrderBy(static extent => extent.Start)
                                    .ThenBy(static extent => extent.End)
                                    .ToList();

        List<(ulong Start, ulong End)> normalizedExtents = [];

        if(orderedExtents.Count == 0) return normalizedExtents;

        (ulong Start, ulong End) currentExtent = orderedExtents[0];

        for(var i = 1; i < orderedExtents.Count; i++)
        {
            if(orderedExtents[i].Start <= currentExtent.End + 1)
            {
                currentExtent.End = Math.Max(currentExtent.End, orderedExtents[i].End);

                continue;
            }

            normalizedExtents.Add(currentExtent);
            currentExtent = orderedExtents[i];
        }

        normalizedExtents.Add(currentExtent);

        return normalizedExtents;
    }
}