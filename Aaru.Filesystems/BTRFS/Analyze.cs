// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
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
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
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
        ErrorNumber errno = ResolvePath(path, out ulong objectId, out ulong treeRoot);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ReadInode(objectId, treeRoot, out InodeItem inode);

        if(errno != ErrorNumber.NoError) return errno;
        if(inode.size == 0) return ErrorNumber.NoError;

        errno = FindOverlappingExtents(objectId, treeRoot, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = objectId,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(ulong objectId, ulong treeRoot,
                                       IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        ErrorNumber errno = ReadTreeBlock(treeRoot, out byte[] fsTreeData);

        if(errno != ErrorNumber.NoError) return errno;

        Header fsTreeHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(fsTreeData);
        List<ExtentEntry> extents = [];

        errno = WalkTreeForExtents(fsTreeData, fsTreeHeader, objectId, extents);

        if(errno != ErrorNumber.NoError) return errno;

        extents.Sort(static (left, right) => left.FileOffset.CompareTo(right.FileOffset));

        foreach(ExtentEntry extent in extents)
        {
            switch(extent.Type)
            {
                case BTRFS_FILE_EXTENT_INLINE:
                case BTRFS_FILE_EXTENT_PREALLOC:
                    continue;

                case BTRFS_FILE_EXTENT_REG:
                    if(extent.DiskBytenr == 0) continue;

                    if(extent.Compression != BTRFS_COMPRESS_NONE)
                        AddLogicalByteRangeOverlaps(extent.DiskBytenr, extent.DiskBytes, sectorExtents, overlaps);
                    else
                        AddLogicalByteRangeOverlaps(extent.DiskBytenr + extent.ExtentOffset,
                                                    extent.Length,
                                                    sectorExtents,
                                                    overlaps);

                    break;
            }
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    void AddLogicalByteRangeOverlaps(ulong logicalByteOffset, ulong lengthBytes,
                                     IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                     List<(ulong Start, ulong End)> overlaps)
    {
        ulong remainingBytes = lengthBytes;
        ulong currentLogical = logicalByteOffset;

        while(remainingBytes > 0)
        {
            ChunkMapping? currentMapping = null;

            foreach(ChunkMapping mapping in _chunkMap)
            {
                if(currentLogical < mapping.LogicalOffset || currentLogical >= mapping.LogicalOffset + mapping.Length)
                    continue;

                currentMapping = mapping;

                break;
            }

            if(currentMapping is null) break;

            ulong bytesInChunk = Math.Min(remainingBytes,
                                          currentMapping.Value.LogicalOffset + currentMapping.Value.Length - currentLogical);
            ulong physicalByte = LogicalToPhysical(currentLogical);

            if(physicalByte == ulong.MaxValue) break;

            AddPhysicalByteRangeOverlaps(physicalByte, bytesInChunk, sectorExtents, overlaps);

            currentLogical += bytesInChunk;
            remainingBytes -= bytesInChunk;
        }
    }

    void AddPhysicalByteRangeOverlaps(ulong physicalByteOffset, ulong lengthBytes,
                                      IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                      List<(ulong Start, ulong End)> overlaps)
    {
        if(lengthBytes == 0) return;

        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        ulong startSector    = _partition.Start + physicalByteOffset / sectorSize;
        ulong offsetInSector = physicalByteOffset % sectorSize;
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