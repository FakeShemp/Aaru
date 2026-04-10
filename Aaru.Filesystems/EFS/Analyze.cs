// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Sector overlap analysis for SGI EFS volumes.
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

/// <inheritdoc />
public sealed partial class EFS
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

                errno = LookupFile(entryPath, out uint inodeNumber);

                if(errno != ErrorNumber.NoError) return errno;

                errno = ReadInode(inodeNumber, out Inode inode);

                if(errno != ErrorNumber.NoError) return errno;

                var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

                if(fileType == FileType.IFDIR)
                {
                    errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                sectorExtents,
                                                                files,
                                                                updateProgress,
                                                                pulseProgress);

                    if(errno != ErrorNumber.NoError) return errno;

                    continue;
                }

                errno = AddOverlappingFile(entryPath, inodeNumber, inode, sectorExtents, files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            CloseDir(node);
        }
    }

    ErrorNumber AddOverlappingFile(string                                  path,          uint inodeNumber, Inode inode,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(inode.di_size <= 0) return ErrorNumber.NoError;

        ErrorNumber errno =
            FindOverlappingExtents(inodeNumber, inode, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno          != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = inodeNumber,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(uint                                    inodeNumber, Inode inode,
                                       IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)>      overlaps)
    {
        overlaps = [];

        var logicalSize = (ulong)inode.di_size;

        if(logicalSize == 0) return ErrorNumber.NoError;

        var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

        if(fileType is FileType.IFLNK or FileType.IFCHRLNK or FileType.IFBLKLNK &&
           inode.di_numextents == 0                                             &&
           logicalSize         <= EFS_MAX_INLINE)
        {
            ulong inodeByteOffset = GetInodeByteOffset(inodeNumber);
            AddByteRangeOverlaps(inodeByteOffset, logicalSize, sectorExtents, overlaps);
            overlaps = NormalizeAnalyzeExtents(overlaps);

            return ErrorNumber.NoError;
        }

        ErrorNumber errno = LoadExtents(inode, out Extent[] extents);

        if(errno != ErrorNumber.NoError) return errno;
        if(extents is null || extents.Length == 0) return ErrorNumber.NoError;

        foreach(Extent extent in extents)
        {
            if(extent.Magic != 0 || extent.Length == 0) continue;

            ulong logicalByteOffset = (ulong)extent.Offset * EFS_BBSIZE;

            if(logicalByteOffset >= logicalSize) continue;

            ulong bytesInExtent = Math.Min((ulong)extent.Length * EFS_BBSIZE, logicalSize - logicalByteOffset);
            AddByteRangeOverlaps((ulong)extent.BlockNumber * EFS_BBSIZE, bytesInExtent, sectorExtents, overlaps);
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    ulong GetInodeByteOffset(uint inodeNumber)
    {
        ulong cylinderGroup  = inodeNumber / (ulong)_inodesPerCg;
        ulong cgInodeOffset  = inodeNumber % (ulong)_inodesPerCg;
        ulong basicBlockInCg = cgInodeOffset >> EFS_INOPBBSHIFT;

        ulong inodeBasicBlock = (ulong)_superblock.sb_firstcg                 +
                                cylinderGroup * (ulong)_superblock.sb_cgfsize +
                                basicBlockInCg;

        return inodeBasicBlock * EFS_BBSIZE;
    }

    void AddByteRangeOverlaps(ulong                                   absoluteByteOffset, ulong lengthBytes,
                              IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                              List<(ulong Start, ulong End)>          overlaps)
    {
        if(lengthBytes == 0) return;

        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        ulong startSector    = _partition.Start + absoluteByteOffset / sectorSize;
        ulong offsetInSector = absoluteByteOffset                              % sectorSize;
        ulong sectorsInRun   = (lengthBytes + offsetInSector + sectorSize - 1) / sectorSize;

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