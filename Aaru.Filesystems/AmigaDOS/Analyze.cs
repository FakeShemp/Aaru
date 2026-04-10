// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Amiga Fast File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Sector overlap analysis for AmigaDOS volumes.
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
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AmigaDOSPlugin
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
                                             .OrderBy(static entry => entry, StringComparer.OrdinalIgnoreCase)
                                             .ToArray();

            long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

            for(var i = 0; i < orderedEntries.Length; i++)
            {
                string entryPath = path == "/" ? "/" + orderedEntries[i] : path + "/" + orderedEntries[i];

                updateProgress?.Invoke(entryPath, i + 1, maximum);

                errno = GetBlockForPath(entryPath, out uint blockNumber);

                if(errno != ErrorNumber.NoError) return errno;

                errno = ReadBlock(blockNumber, out byte[] blockData);

                if(errno != ErrorNumber.NoError) return errno;

                var secType       = BigEndianBitConverter.ToUInt32(blockData, blockData.Length - 4);
                int signedSecType = ToSignedSecType(secType);

                if(signedSecType is 1 or 2 or 4)
                {
                    errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                sectorExtents,
                                                                files,
                                                                updateProgress,
                                                                pulseProgress);

                    if(errno != ErrorNumber.NoError) return errno;

                    continue;
                }

                if(signedSecType is not (-3 or -4 or 3)) continue;

                errno = AddOverlappingFile(entryPath, blockNumber, blockData, sectorExtents, files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            CloseDir(node);
        }
    }

    ErrorNumber AddOverlappingFile(string                                  path, uint headerBlock, byte[] headerData,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        int byteSizeOffset = headerData.Length - 47 * 4;
        var fileSize       = BigEndianBitConverter.ToUInt32(headerData, byteSizeOffset);

        if(fileSize == 0) return ErrorNumber.NoError;

        ErrorNumber errno = FindOverlappingExtents(headerBlock,
                                                   headerData,
                                                   fileSize,
                                                   sectorExtents,
                                                   out List<(ulong Start, ulong End)> overlaps);

        if(errno          != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = headerBlock,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(uint currentBlockNumber, byte[] currentBlockData, uint fileSize,
                                       IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        ulong remainingBytes = fileSize;

        while(remainingBytes > 0 && currentBlockData != null)
        {
            int sizeBlock       = currentBlockData.Length / 4;
            int tableEnd        = sizeBlock - 51;
            int extensionOffset = sizeBlock - 2;

            for(int key = tableEnd; key >= BLK_TABLE_START && remainingBytes > 0; key--)
            {
                var dataBlockNumber = BigEndianBitConverter.ToUInt32(currentBlockData, key * 4);

                if(dataBlockNumber == 0) continue;

                ulong bytesInBlock = Math.Min(_blockSize, remainingBytes);
                AddBlockOverlaps(dataBlockNumber, bytesInBlock, sectorExtents, overlaps);
                remainingBytes -= bytesInBlock;
            }

            if(remainingBytes == 0) break;

            var nextExtension = BigEndianBitConverter.ToUInt32(currentBlockData, extensionOffset * 4);

            if(nextExtension == 0 || nextExtension == currentBlockNumber) break;

            currentBlockNumber = nextExtension;

            ErrorNumber errno = ReadBlock(nextExtension, out currentBlockData);

            if(errno != ErrorNumber.NoError) return errno;
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    void AddBlockOverlaps(uint blockNumber, ulong lengthBytes, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                          List<(ulong Start, ulong End)> overlaps)
    {
        if(lengthBytes == 0) return;

        ulong absoluteByteOffset = (ulong)blockNumber * _blockSize;
        ulong sectorSize         = _imagePlugin.Info.SectorSize;
        ulong startSector        = _partition.Start + absoluteByteOffset / sectorSize;
        ulong offsetInSector     = absoluteByteOffset                              % sectorSize;
        ulong sectorsInRun       = (lengthBytes + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorsInRun == 0) sectorsInRun = 1;

        AddExtentOverlaps(startSector, startSector + sectorsInRun - 1, sectorExtents, overlaps);
    }

    static int ToSignedSecType(uint secType)
    {
        var signedSecType = (int)secType;

        if(secType > 0x7FFFFFFF) signedSecType = (int)(secType - 0x100000000);

        return signedSecType;
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