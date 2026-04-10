// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple DOS filesystem plugin.
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

/// <inheritdoc />
public sealed partial class AppleDOS
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

        if(normalizedExtents.Count == 0 || _catalogCache is null || _catalogCache.Count == 0)
            return ErrorNumber.NoError;

        try
        {
            initProgress?.Invoke();
            pulseProgress?.Invoke("/");

            string[] orderedEntries = _catalogCache.Keys.OrderBy(static entry => entry,
                                                                 StringComparer.CurrentCultureIgnoreCase)
                                                   .ToArray();

            long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

            for(var i = 0; i < orderedEntries.Length; i++)
            {
                string entryPath = "/" + orderedEntries[i];

                updateProgress?.Invoke(entryPath, i + 1, maximum);

                ErrorNumber errno = AddOverlappingFile(entryPath, orderedEntries[i], normalizedExtents, files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            endProgress?.Invoke();
        }
    }

#endregion

    ErrorNumber AddOverlappingFile(string path, string filename, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                   List<FileSectorInfo> files)
    {
        ErrorNumber errno = FindOverlappingExtents(filename,
                                                   sectorExtents,
                                                   out List<(ulong Start, ulong End)> overlaps,
                                                   out ulong inode);

        if(errno          == ErrorNumber.NoSuchFile) return ErrorNumber.NoError;
        if(errno          != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = inode,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(string filename, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps, out ulong inode)
    {
        overlaps = [];
        inode    = 0;

        if(string.IsNullOrWhiteSpace(filename)) return ErrorNumber.InvalidArgument;

        string normalizedName = filename.ToUpperInvariant();

        if(!_catalogCache.TryGetValue(normalizedName, out ushort trackSector)) return ErrorNumber.NoSuchFile;

        inode = trackSector;

        var lba = (ulong)(((trackSector & 0xFF00) >> 8) * _sectorsPerTrack + (trackSector & 0xFF));

        if(lba == 0 || lba >= _device.Info.Sectors) return ErrorNumber.NoError;

        HashSet<ulong> visitedTrackSectorLists = [];

        while(lba != 0 && lba < _device.Info.Sectors && visitedTrackSectorLists.Add(lba))
        {
            ErrorNumber errno = _device.ReadSector(lba, false, out byte[] tsSectorBytes, out _);

            if(errno != ErrorNumber.NoError) return errno;

            TrackSectorList tsSector = Marshal.ByteArrayToStructureLittleEndian<TrackSectorList>(tsSectorBytes);

            if(tsSector.entries is not null)
            {
                foreach(TrackSectorListEntry entry in tsSector.entries)
                {
                    var blockLba = (ulong)(entry.track * _sectorsPerTrack + entry.sector);

                    if(blockLba == 0 || blockLba >= _device.Info.Sectors) break;

                    AddExtentOverlaps(blockLba, blockLba, sectorExtents, overlaps);
                }
            }

            lba = (ulong)(tsSector.nextListTrack * _sectorsPerTrack + tsSector.nextListSector);
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
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