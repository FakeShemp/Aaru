// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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
public sealed partial class Squash
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
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files,
                                                    UpdateProgressHandler updateProgress,
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

            string[] orderedEntries = entries.Where(static entry =>
                                                                !string.IsNullOrWhiteSpace(entry) &&
                                                                entry is not "." and not "..")
                                             .OrderBy(static entry => entry, StringComparer.Ordinal)
                                             .ToArray();
            long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

            for(int i = 0; i < orderedEntries.Length; i++)
            {
                string entryPath = path == "/" ? "/" + orderedEntries[i] : path + "/" + orderedEntries[i];

                updateProgress?.Invoke(entryPath, i + 1, maximum);

                errno = Stat(entryPath, out FileEntryInfo stat);

                if(errno != ErrorNumber.NoError) return errno;

                if(stat.Attributes.HasFlag(FileAttributes.Directory))
                {
                    errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                sectorExtents,
                                                                files,
                                                                updateProgress,
                                                                pulseProgress);

                    if(errno != ErrorNumber.NoError) return errno;

                    continue;
                }

                errno = AddOverlappingFile(entryPath, sectorExtents, files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            CloseDir(node);
        }
    }

    ErrorNumber AddOverlappingFile(string path, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                   List<FileSectorInfo> files)
    {
        ErrorNumber errno = LookupFile(path, out DirectoryEntryInfo entry);

        if(errno != ErrorNumber.NoError) return errno;

        if(entry.Type is SquashInodeType.Directory or SquashInodeType.ExtendedDirectory)
            return ErrorNumber.NoError;

        if(entry.Type is not SquashInodeType.RegularFile and not SquashInodeType.ExtendedRegularFile)
            return ErrorNumber.NoError;

        errno = ReadFileInode(entry.InodeBlock, entry.InodeOffset, out SquashFileNode fileNode);

        if(errno != ErrorNumber.NoError) return errno;
        if(fileNode.Length <= 0) return ErrorNumber.NoError;

        errno = FindOverlappingExtents(fileNode, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        ulong inodeId = ((ulong)entry.InodeBlock << 16) | entry.InodeOffset;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = inodeId,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(SquashFileNode fileNode,
                                       IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        for(int blockIndex = 0; blockIndex < fileNode.BlockCount; blockIndex++)
        {
            ErrorNumber errno = ReadBlockList(fileNode, blockIndex, out uint blockSize, out ulong blockOffset);

            if(errno != ErrorNumber.NoError) return errno;
            if(blockSize == 0) continue;

            ulong compressedSize = blockSize & 0x00FFFFFFU;

            if(compressedSize == 0) continue;

            AddByteRangeOverlaps(fileNode.StartBlock + blockOffset, compressedSize, sectorExtents, overlaps);
        }

        if(fileNode.Fragment != SQUASHFS_INVALID_FRAG)
        {
            ErrorNumber errno = ReadFragmentEntry(fileNode.Fragment, out ulong fragmentBlock, out uint fragmentSize);

            if(errno == ErrorNumber.NoError)
            {
                ulong compressedSize = fragmentSize & 0x00FFFFFFU;

                if(compressedSize > 0)
                    AddByteRangeOverlaps(fragmentBlock, compressedSize, sectorExtents, overlaps);
            }
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    void AddByteRangeOverlaps(ulong absoluteByteOffset, ulong lengthBytes,
                              IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                              List<(ulong Start, ulong End)> overlaps)
    {
        if(lengthBytes == 0) return;

        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        ulong startSector    = _partition.Start + absoluteByteOffset / sectorSize;
        ulong offsetInSector = absoluteByteOffset % sectorSize;
        ulong sectorCount    = (lengthBytes + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorCount == 0) sectorCount = 1;

        AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);
    }

    static void AddExtentOverlaps(ulong startSector, ulong endSector,
                                  IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                  List<(ulong Start, ulong End)> overlaps)
    {
        foreach((ulong Start, ulong End) requestedExtent in sectorExtents)
        {
            if(requestedExtent.End < startSector || requestedExtent.Start > endSector) continue;

            overlaps.Add((Math.Max(startSector, requestedExtent.Start), Math.Min(endSector, requestedExtent.End)));
        }
    }

    static List<(ulong Start, ulong End)> NormalizeAnalyzeExtents(IEnumerable<(ulong Start, ulong End)> extents)
    {
        List<(ulong Start, ulong End)> orderedExtents = extents.Where(static extent => extent.End >= extent.Start)
                                                               .OrderBy(static extent => extent.Start)
                                                               .ThenBy(static extent => extent.End)
                                                               .ToList();
        List<(ulong Start, ulong End)> normalizedExtents = [];

        if(orderedExtents.Count == 0) return normalizedExtents;

        (ulong Start, ulong End) currentExtent = orderedExtents[0];

        for(int i = 1; i < orderedExtents.Count; i++)
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