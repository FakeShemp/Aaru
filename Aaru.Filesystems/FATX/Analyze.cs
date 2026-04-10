// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : FATX filesystem plugin.
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
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

public sealed partial class XboxFatPlugin
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

        ErrorNumber errno = OpenDir(path, out IDirNode node);

        if(errno != ErrorNumber.NoError) return errno;

        if(node is not FatxDirNode dirNode)
        {
            CloseDir(node);

            return ErrorNumber.InvalidArgument;
        }

        DirectoryEntry[] entries = dirNode.Entries
                                         ?.OrderBy(static entry =>
                                                       entry.filenameSize > 0
                                                           ? entry.filename[0].ToString()
                                                           : string.Empty,
                                                   StringComparer.CurrentCultureIgnoreCase)
                                          .ToArray() ?? [];

        CloseDir(node);

        long maximum = entries.Length > 0 ? entries.Length : 1;

        for(var i = 0; i < entries.Length; i++)
        {
            string entryName = entries[i].filenameSize > 0
                                   ? _encoding.GetString(entries[i].filename, 0, entries[i].filenameSize)
                                   : string.Empty;

            if(string.IsNullOrWhiteSpace(entryName)) continue;

            string entryPath = path == "/" ? "/" + entryName : path + "/" + entryName;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            if(entries[i].attributes.HasFlag(Attributes.Directory))
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            AddOverlappingFile(entryPath, entries[i], sectorExtents, files);
        }

        return ErrorNumber.NoError;
    }

    void AddOverlappingFile(string path, DirectoryEntry entry, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                            List<FileSectorInfo> files)
    {
        if(entry.length == 0 || entry.firstCluster == 0) return;

        uint[] clusters = GetClusters(entry.firstCluster);

        if(clusters is null || clusters.Length == 0) return;

        List<(ulong Start, ulong End)> overlaps = FindOverlappingExtents(clusters, entry.length, sectorExtents);

        if(overlaps.Count == 0) return;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = entry.firstCluster,
            AffectedSectors = overlaps
        });
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(uint[] clusters, uint logicalSize,
                                                          IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps       = [];
        ulong                          remainingBytes = logicalSize;
        ulong                          sectorSize     = _imagePlugin.Info.SectorSize;

        foreach(uint cluster in clusters)
        {
            if(remainingBytes == 0) break;

            if(cluster == 0) continue;

            ulong bytesInCluster = Math.Min(_bytesPerCluster, remainingBytes);

            if(bytesInCluster == 0) break;

            ulong startSector = _firstClusterSector + (cluster - 1) * _sectorsPerCluster;
            ulong sectorCount = (bytesInCluster                + sectorSize - 1) / sectorSize;

            if(sectorCount == 0) sectorCount = 1;

            AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);

            remainingBytes -= bytesInCluster;
        }

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