// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Sector overlap analysis for CramFS volumes.
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
public sealed partial class Cram
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

                errno = LookupFile(entryPath, out DirectoryEntryInfo entry);

                if(errno != ErrorNumber.NoError) return errno;

                if(IsDirectory(GetInodeMode(entry.Inode)))
                {
                    errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                sectorExtents,
                                                                files,
                                                                updateProgress,
                                                                pulseProgress);

                    if(errno != ErrorNumber.NoError) return errno;

                    continue;
                }

                errno = AddOverlappingFile(entryPath, entry, sectorExtents, files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            CloseDir(node);
        }
    }

    ErrorNumber AddOverlappingFile(string                                  path,          DirectoryEntryInfo   entry,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        uint size = GetInodeSize(entry.Inode);

        if(size == 0) return ErrorNumber.NoError;

        ErrorNumber errno =
            FindOverlappingExtents(entry.Inode, size, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno          != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = entry.Offset,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(Inode inode, uint size, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(size == 0) return ErrorNumber.NoError;

        uint blockPtrOffset = GetInodeOffset(inode) << 2;
        uint blockCount     = (size + PAGE_SIZE - 1) / PAGE_SIZE;

        for(var blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            ErrorNumber errno = GetCompressedBlockRange(blockPtrOffset,
                                                        blockCount,
                                                        size,
                                                        blockIndex,
                                                        out uint blockStart,
                                                        out uint blockLength);

            if(errno != ErrorNumber.NoError) return errno;

            if(blockLength == 0) continue;

            AddByteRangeOverlaps((ulong)_baseOffset + blockStart, blockLength, sectorExtents, overlaps);
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    ErrorNumber GetCompressedBlockRange(uint     blockPtrOffset, uint     blockCount, uint fileSize, int blockIndex,
                                        out uint blockStart,     out uint blockLength)
    {
        blockStart  = 0;
        blockLength = 0;

        if(blockIndex < 0 || blockIndex >= blockCount) return ErrorNumber.InvalidArgument;

        uint pointerOffset = blockPtrOffset + (uint)blockIndex * 4;

        ErrorNumber errno = ReadBytes(pointerOffset, 4, out byte[] ptrData);

        if(errno != ErrorNumber.NoError) return errno;

        uint blockPtr = _littleEndian
                            ? BitConverter.ToUInt32(ptrData, 0)
                            : (uint)(ptrData[0] << 24 | ptrData[1] << 16 | ptrData[2] << 8 | ptrData[3]);

        bool uncompressed = (blockPtr & CRAMFS_BLK_FLAG_UNCOMPRESSED) != 0;
        bool direct       = (blockPtr & CRAMFS_BLK_FLAG_DIRECT_PTR)   != 0;

        blockPtr &= ~CRAMFS_BLK_FLAGS;

        if(direct)
        {
            blockStart = blockPtr << CRAMFS_BLK_DIRECT_PTR_SHIFT;

            if(uncompressed)
            {
                blockLength = PAGE_SIZE;

                if(blockIndex == blockCount - 1)
                {
                    blockLength = fileSize % PAGE_SIZE;

                    if(blockLength == 0) blockLength = PAGE_SIZE;
                }
            }
            else
            {
                errno = ReadBytes(blockStart, 2, out byte[] sizeData);

                if(errno != ErrorNumber.NoError) return errno;

                blockLength = _littleEndian
                                  ? BitConverter.ToUInt16(sizeData, 0)
                                  : (uint)(sizeData[0] << 8 | sizeData[1]);

                blockStart += 2;
            }
        }
        else
        {
            blockStart = blockPtrOffset + blockCount * 4;

            if(blockIndex > 0)
            {
                errno = ReadBytes(pointerOffset - 4, 4, out byte[] previousPtrData);

                if(errno != ErrorNumber.NoError) return errno;

                uint previousPtr = _littleEndian
                                       ? BitConverter.ToUInt32(previousPtrData, 0)
                                       : (uint)(previousPtrData[0] << 24 |
                                                previousPtrData[1] << 16 |
                                                previousPtrData[2] << 8  |
                                                previousPtrData[3]);

                if((previousPtr & CRAMFS_BLK_FLAG_DIRECT_PTR) != 0)
                {
                    uint previousStart = (previousPtr & ~CRAMFS_BLK_FLAGS) << CRAMFS_BLK_DIRECT_PTR_SHIFT;

                    if((previousPtr & CRAMFS_BLK_FLAG_UNCOMPRESSED) != 0)
                        blockStart = previousStart + PAGE_SIZE;
                    else
                    {
                        errno = ReadBytes(previousStart, 2, out byte[] previousSizeData);

                        if(errno != ErrorNumber.NoError) return errno;

                        uint previousLength = _littleEndian
                                                  ? BitConverter.ToUInt16(previousSizeData, 0)
                                                  : (uint)(previousSizeData[0] << 8 | previousSizeData[1]);

                        blockStart = previousStart + 2 + previousLength;
                    }
                }
                else
                    blockStart = previousPtr & ~CRAMFS_BLK_FLAGS;
            }

            blockLength = (blockPtr & ~CRAMFS_BLK_FLAGS) - blockStart;
        }

        if(blockLength == 0) return ErrorNumber.NoError;

        if(blockLength > 2 * PAGE_SIZE || uncompressed && blockLength > PAGE_SIZE) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
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