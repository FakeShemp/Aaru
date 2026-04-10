// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Sector overlap analysis for the High Performance Optical File System.
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

/// <inheritdoc />
public sealed partial class HPOFS
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

            return TraverseDirectoryForAffectedSectors("/",
                                                       _rootDirectoryCache,
                                                       normalizedExtents,
                                                       files,
                                                       updateProgress,
                                                       pulseProgress);
        }
        finally
        {
            endProgress?.Invoke();
        }
    }

#endregion

    ErrorNumber TraverseDirectoryForAffectedSectors(string path,
                                                    Dictionary<string, CachedDirectoryEntry> directoryEntries,
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        KeyValuePair<string, CachedDirectoryEntry>[] orderedEntries = directoryEntries
                                                                     .Where(static entry =>
                                                                                !string.IsNullOrWhiteSpace(entry.Key) &&
                                                                                entry.Key is not "." and not "..")
                                                                     .OrderBy(static entry => entry.Key,
                                                                              StringComparer.OrdinalIgnoreCase)
                                                                     .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            string entryPath = path == "/" ? "/" + orderedEntries[i].Key : path + "/" + orderedEntries[i].Key;
            CachedDirectoryEntry entry = orderedEntries[i].Value;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            if(entry.IsDirectory)
            {
                string subdirectoryPath = entryPath.Trim('/');

                if(_directoryCache != null &&
                   _directoryCache.TryGetValue(subdirectoryPath,
                                               out Dictionary<string, CachedDirectoryEntry> subDirectory))
                {
                    ErrorNumber errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                            subDirectory,
                                                                            sectorExtents,
                                                                            files,
                                                                            updateProgress,
                                                                            pulseProgress);

                    if(errno != ErrorNumber.NoError) return errno;
                }

                continue;
            }

            ErrorNumber overlapErrno = AddOverlappingFile(entryPath, entry, sectorExtents, files);

            if(overlapErrno != ErrorNumber.NoError) return overlapErrno;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber AddOverlappingFile(string                                  path,          CachedDirectoryEntry entry,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(entry.FileSize == 0) return ErrorNumber.NoError;

        List<(uint startLba, ushort sectorCount)> extents = BuildFileExtentList(entry);

        ErrorNumber errno = FindOverlappingExtents(extents,
                                                   entry.FileSize,
                                                   sectorExtents,
                                                   out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = entry.SectorAddress,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(IReadOnlyList<(uint startLba, ushort sectorCount)> extents, uint logicalSize,
                                       IReadOnlyList<(ulong Start, ulong End)>            sectorExtents,
                                       out List<(ulong Start, ulong End)>                 overlaps)
    {
        overlaps = [];

        if(logicalSize == 0 || extents.Count == 0) return ErrorNumber.NoError;

        ulong remainingBytes = logicalSize;
        ulong sectorSize     = _image.Info.SectorSize;

        for(var i = 0; i < extents.Count && remainingBytes > 0; i++)
        {
            if(extents[i].sectorCount == 0) continue;

            ulong extentBytes = (ulong)extents[i].sectorCount * _bpb.bps;
            ulong bytesInRun  = Math.Min(extentBytes, remainingBytes);

            if(bytesInRun == 0) continue;

            ulong startSector  = _partition.Start + extents[i].startLba;
            ulong sectorsInRun = (bytesInRun + sectorSize - 1) / sectorSize;

            if(sectorsInRun == 0) sectorsInRun = 1;

            AddExtentOverlaps(startSector, startSector + sectorsInRun - 1, sectorExtents, overlaps);
            remainingBytes -= bytesInRun;
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