// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Macintosh File System plugin.
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

// Information from Inside Macintosh Volume II
public sealed partial class AppleMFS
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
            pulseProgress?.Invoke("/");

            KeyValuePair<uint, string>[] orderedEntries = _idToFilename.OrderBy(static entry => entry.Value,
                                                                                    StringComparer
                                                                                       .CurrentCultureIgnoreCase)
                                                                       .ToArray();

            long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

            for(var i = 0; i < orderedEntries.Length; i++)
            {
                string entryPath = "/" + orderedEntries[i].Value;

                updateProgress?.Invoke(entryPath, i + 1, maximum);

                if(!_idToEntry.TryGetValue(orderedEntries[i].Key, out FileEntry entry)) continue;

                AddOverlappingFile(entryPath, entry, normalizedExtents, files);
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            endProgress?.Invoke();
        }
    }

#endregion

    void AddOverlappingFile(string path, FileEntry entry, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                            List<FileSectorInfo> files)
    {
        AddOverlappingFork(path, null,            entry.flFlNum, entry.flStBlk,  entry.flLgLen,  sectorExtents, files);
        AddOverlappingFork(path, "resource-fork", entry.flFlNum, entry.flRStBlk, entry.flRLgLen, sectorExtents, files);
    }

    void AddOverlappingFork(string path, string stream, uint fileId, ushort startBlock, uint logicalLength,
                            IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(logicalLength == 0 || startBlock <= BMAP_LAST || startBlock >= _blockMap.Length) return;

        List<(ulong Start, ulong End)> overlaps = FindOverlappingExtents(startBlock, logicalLength, sectorExtents);

        if(overlaps.Count == 0) return;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Stream          = stream,
            Inode           = fileId,
            AffectedSectors = overlaps
        });
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(ushort startBlock, uint logicalLength,
                                                          IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps       = [];
        HashSet<uint>                  visitedBlocks  = [];
        ulong                          remainingBytes = logicalLength;
        uint                           nextBlock      = startBlock;
        ulong                          sectorSize     = _device.Info.SectorSize;

        while(nextBlock      > BMAP_LAST        &&
              nextBlock      < _blockMap.Length &&
              remainingBytes > 0                &&
              visitedBlocks.Add(nextBlock))
        {
            ulong bytesInBlock = Math.Min(_volMdb.drAlBlkSiz, remainingBytes);

            if(bytesInBlock == 0) break;

            ulong startSector = (ulong)((nextBlock - 2) * _sectorsPerBlock) + _volMdb.drAlBlSt + _partitionStart;
            ulong sectorCount = (bytesInBlock + sectorSize - 1) / sectorSize;

            if(sectorCount == 0) sectorCount = 1;

            AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);

            remainingBytes -= bytesInBlock;

            if(_blockMap[nextBlock] == BMAP_FREE) break;

            nextBlock = _blockMap[nextBlock];
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