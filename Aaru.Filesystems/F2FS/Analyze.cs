// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Sector overlap analysis for F2FS volumes.
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

public sealed partial class F2FS
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        ErrorNumber errno = OpenDir(path, out IDirNode node);

        if(errno != ErrorNumber.NoError) return errno;

        try
        {
            List<string> entries = [];

            while(true)
            {
                errno = ReadDir(node, out string entryName);

                if(errno != ErrorNumber.NoError) return errno;

                if(entryName is null) break;

                entries.Add(entryName);
            }

            string[] orderedEntries = entries.Where(static entry => !string.IsNullOrWhiteSpace(entry))
                                             .OrderBy(static entry => entry, StringComparer.Ordinal)
                                             .ToArray();

            long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

            for(var i = 0; i < orderedEntries.Length; i++)
            {
                string entryPath = path == "/" ? "/" + orderedEntries[i] : path + "/" + orderedEntries[i];

                updateProgress?.Invoke(entryPath, i + 1, maximum);

                errno = LookupFile(entryPath, out uint nid);

                if(errno != ErrorNumber.NoError) return errno;

                errno = ReadInode(nid, out Inode inode);

                if(errno != ErrorNumber.NoError) return errno;

                if((inode.i_mode & 0xF000) == 0x4000)
                {
                    errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                sectorExtents,
                                                                files,
                                                                updateProgress,
                                                                pulseProgress);

                    if(errno != ErrorNumber.NoError) return errno;

                    continue;
                }

                errno = AddOverlappingFile(entryPath, nid, inode, sectorExtents, files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            CloseDir(node);
        }
    }

    ErrorNumber AddOverlappingFile(string                                  path,          uint nid, Inode inode,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        ulong logicalSize = inode.i_size;

        if(logicalSize == 0) return ErrorNumber.NoError;

        ErrorNumber errno =
            FindOverlappingExtents(nid, inode, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno          != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = nid,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(uint nid, Inode inode, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        ulong logicalSize = inode.i_size;

        if(logicalSize == 0) return ErrorNumber.NoError;

        ErrorNumber errno = LookupNat(nid, out uint nodeBlockAddr);

        if(errno != ErrorNumber.NoError) return errno;

        if((inode.i_inline & F2FS_INLINE_DATA) != 0)
        {
            if(nodeBlockAddr != 0)
                AddBlockOverlaps(nodeBlockAddr, Math.Min(logicalSize, _blockSize), sectorExtents, overlaps);

            overlaps = NormalizeAnalyzeExtents(overlaps);

            return ErrorNumber.NoError;
        }

        int   addrsPerInode = GetAddrsPerInode(inode);
        ulong pageCount     = (logicalSize + _blockSize - 1) / _blockSize;

        for(uint pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            errno = ResolveDataBlock(inode, pageIndex, addrsPerInode, out uint blockAddr);

            if(errno != ErrorNumber.NoError) return errno;

            if(!IsValidDataBlockAddress(blockAddr)) continue;

            ulong logicalOffset = (ulong)pageIndex * _blockSize;

            if(logicalOffset >= logicalSize) break;

            ulong bytesInBlock = Math.Min(_blockSize, logicalSize - logicalOffset);
            AddBlockOverlaps(blockAddr, bytesInBlock, sectorExtents, overlaps);
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    void AddBlockOverlaps(uint blockAddr, ulong lengthBytes, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                          List<(ulong Start, ulong End)> overlaps)
    {
        if(lengthBytes == 0) return;

        ulong absoluteByteOffset = (ulong)blockAddr * _blockSize;
        ulong sectorSize         = _imagePlugin.Info.SectorSize;
        ulong startSector        = _partition.Start + absoluteByteOffset / sectorSize;
        ulong offsetInSector     = absoluteByteOffset                              % sectorSize;
        ulong sectorsInRun       = (lengthBytes + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorsInRun == 0) sectorsInRun = 1;

        AddExtentOverlaps(startSector, startSector + sectorsInRun - 1, sectorExtents, overlaps);
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