// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft exFAT filesystem plugin.
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

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
public sealed partial class exFAT
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

        ErrorNumber errno = GetDirectoryEntries(path, out Dictionary<string, CompleteDirectoryEntry> entries);

        if(errno != ErrorNumber.NoError) return errno;

        CompleteDirectoryEntry[] orderedEntries = entries.Values.Where(static entry => entry?.FileName != null)
                                                         .OrderBy(static entry => entry.FileName,
                                                                  StringComparer.CurrentCultureIgnoreCase)
                                                         .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            CompleteDirectoryEntry entry     = orderedEntries[i];
            string                 entryPath = path == "/" ? "/" + entry.FileName : path + "/" + entry.FileName;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            if(entry.IsDirectory)
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            AddOverlappingFile(entryPath, entry, sectorExtents, files);
        }

        return ErrorNumber.NoError;
    }

    void AddOverlappingFile(string                                  path,          CompleteDirectoryEntry entry,
                            IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo>   files)
    {
        if(entry.DataLength == 0 || entry.FirstCluster < 2) return;

        List<(ulong Start, ulong End)> overlaps = FindOverlappingExtents(entry, sectorExtents);

        if(overlaps.Count == 0) return;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = entry.FirstCluster,
            AffectedSectors = overlaps
        });
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(CompleteDirectoryEntry                  entry,
                                                          IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps       = [];
        ulong                          remainingBytes = entry.DataLength;

        foreach(uint cluster in GetFileClusters(entry))
        {
            if(remainingBytes == 0) break;

            if(cluster < 2 || cluster > _clusterCount + 1) continue;

            ulong bytesInCluster = Math.Min(_bytesPerCluster, remainingBytes);

            if(bytesInCluster == 0) break;

            ulong startSector = _clusterHeapOffset + (ulong)(cluster - 2) * _sectorsPerCluster;
            ulong sectorCount = (bytesInCluster + _bytesPerSector - 1) / _bytesPerSector;

            if(sectorCount == 0) sectorCount = 1;

            AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);

            remainingBytes -= bytesInCluster;
        }

        return NormalizeAnalyzeExtents(overlaps);
    }

    IEnumerable<uint> GetFileClusters(CompleteDirectoryEntry entry)
    {
        if(entry.FirstCluster < 2) yield break;

        ulong clusterCount = (entry.DataLength + _bytesPerCluster - 1) / _bytesPerCluster;

        if(clusterCount == 0) yield break;

        if(entry.IsContiguous)
        {
            for(uint i = 0; i < clusterCount; i++) yield return entry.FirstCluster + i;

            yield break;
        }

        uint currentCluster = entry.FirstCluster;

        for(ulong i = 0; i < clusterCount && currentCluster >= 2 && currentCluster <= _clusterCount + 1; i++)
        {
            yield return currentCluster;

            if(currentCluster >= _fatEntries.Length) yield break;

            uint nextCluster = _fatEntries[currentCluster];

            if(nextCluster >= 0xFFFFFFF8 || nextCluster == 0xFFFFFFF7) yield break;

            currentCluster = nextCluster;
        }
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