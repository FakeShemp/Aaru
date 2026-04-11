// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser4 filesystem plugin.
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class Reiser4
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
        ErrorNumber errno = ResolvePath(path, out LargeKey statDataKey);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ReadStatData(statDataKey, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;
        if(stat.Length <= 0) return ErrorNumber.NoError;

        errno = FindOverlappingExtents(statDataKey, stat.Length, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = GetStatDataObjectId(statDataKey),
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(LargeKey statDataKey, long logicalSize,
                                       IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(logicalSize <= 0) return ErrorNumber.NoError;

        long currentOffset = 0;

        while(currentOffset < logicalSize)
        {
            bool     advanced = false;
            LargeKey bodyKey  = BuildFileBodyKey(statDataKey, (ulong)currentOffset);

            ErrorNumber errno = SearchByKey(bodyKey, out byte[] twigData, out int itemPos, TWIG_LEVEL);

            if(errno == ErrorNumber.NoError && itemPos >= 0)
            {
                Node40Header nodeHeader = Marshal.ByteArrayToStructureLittleEndian<Node40Header>(twigData);

                if(itemPos < nodeHeader.nr_items)
                {
                    ReadItemHeader(twigData,
                                   itemPos,
                                   nodeHeader.nr_items,
                                   out LargeKey itemKey,
                                   out ushort bodyOff,
                                   out _,
                                   out ushort pluginId);

                    if(pluginId == EXTENT_POINTER_ID && KeyMatchesFileBody(itemKey, statDataKey))
                    {
                        int   itemLen            = GetItemLength(twigData, itemPos, nodeHeader.nr_items, nodeHeader.free_space_start);
                        int   extentCount        = itemLen / EXTENT_SIZE;
                        ulong itemLogicalOffset  = GetKeyOffset(itemKey);
                        long  currentItemOffset  = (long)itemLogicalOffset;

                        for(int i = 0; i < extentCount; i++)
                        {
                            int extentOffset = bodyOff + i * EXTENT_SIZE;

                            if(extentOffset + EXTENT_SIZE > twigData.Length) break;

                            ulong start = BitConverter.ToUInt64(twigData, extentOffset);
                            ulong width = BitConverter.ToUInt64(twigData, extentOffset + 8);
                            ulong extentBytes = width * _blockSize;

                            if(start != 0 && extentBytes > 0)
                            {
                                ulong usefulBytes = Math.Min(extentBytes,
                                                             (ulong)Math.Max(0, logicalSize - currentItemOffset));

                                if(usefulBytes > 0)
                                    AddByteRangeOverlaps(start * _blockSize,
                                                         usefulBytes,
                                                         sectorExtents,
                                                         overlaps);
                            }

                            currentItemOffset += (long)extentBytes;
                        }

                        if(currentItemOffset > currentOffset)
                        {
                            currentOffset = currentItemOffset;
                            advanced      = true;
                        }
                    }
                }
            }

            if(advanced) continue;

            errno = SearchByKey(bodyKey, out byte[] leafData, out itemPos);

            if(errno == ErrorNumber.NoError && itemPos >= 0)
            {
                Node40Header nodeHeader = Marshal.ByteArrayToStructureLittleEndian<Node40Header>(leafData);

                if(itemPos < nodeHeader.nr_items)
                {
                    ReadItemHeader(leafData,
                                   itemPos,
                                   nodeHeader.nr_items,
                                   out LargeKey itemKey,
                                   out _,
                                   out _,
                                   out ushort pluginId);

                    if(pluginId == FORMATTING_ID && KeyMatchesFileBody(itemKey, statDataKey))
                    {
                        int   itemLen           = GetItemLength(leafData, itemPos, nodeHeader.nr_items, nodeHeader.free_space_start);
                        ulong itemLogicalOffset = GetKeyOffset(itemKey);
                        long  itemEnd           = (long)itemLogicalOffset + itemLen;

                        if(itemEnd > currentOffset)
                        {
                            currentOffset = itemEnd;
                            advanced      = true;
                        }
                    }
                }
            }

            if(!advanced) break;
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