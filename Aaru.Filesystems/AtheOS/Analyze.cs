// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AtheOS filesystem plugin.
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

/// <inheritdoc />
/// <summary>Implements the AtheOS filesystem</summary>
public sealed partial class AtheOS
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

        long rootBlockAddress = (long)_superblock.root_dir_ag * _superblock.blocks_per_ag + _superblock.root_dir_start;
        ErrorNumber errno = ReadInode(rootBlockAddress, out Inode rootInode);

        if(errno != ErrorNumber.NoError) return errno;

        try
        {
            initProgress?.Invoke();

            return TraverseDirectoryForAffectedSectors("/",
                                                       rootBlockAddress,
                                                       rootInode,
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, long directoryInodeAddress, Inode directoryInode,
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        if(!IsDirectory(directoryInode)) return ErrorNumber.NotDirectory;

        ErrorNumber errno = GetDirectoryEntries(directoryInodeAddress,
                                                directoryInode,
                                                out Dictionary<string, long> entries);

        if(errno != ErrorNumber.NoError) return errno;

        KeyValuePair<string, long>[] orderedEntries = entries
                                                     .Where(static entry =>
                                                                !string.IsNullOrWhiteSpace(entry.Key) &&
                                                                entry.Key is not "." and not "..")
                                                     .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
                                                     .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            string entryPath = path == "/" ? "/" + orderedEntries[i].Key : path + "/" + orderedEntries[i].Key;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            errno = ReadInode(orderedEntries[i].Value, out Inode entryInode);

            if(errno != ErrorNumber.NoError) return errno;

            if(IsDirectory(entryInode))
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            orderedEntries[i].Value,
                                                            entryInode,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            errno = AddOverlappingFile(entryPath, orderedEntries[i].Value, entryInode, sectorExtents, files);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber GetDirectoryEntries(long                         directoryInodeAddress, Inode directoryInode,
                                    out Dictionary<string, long> entries)
    {
        long rootBlockAddress = (long)_superblock.root_dir_ag * _superblock.blocks_per_ag + _superblock.root_dir_start;

        if(directoryInodeAddress == rootBlockAddress)
        {
            entries = new Dictionary<string, long>(_rootDirectoryCache, StringComparer.Ordinal);

            return ErrorNumber.NoError;
        }

        return ParseDirectoryBTree(directoryInode.data, out entries);
    }

    ErrorNumber AddOverlappingFile(string                                  path, long inodeAddress, Inode inode,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(inode.data.size <= 0) return ErrorNumber.NoError;

        ErrorNumber errno =
            FindOverlappingExtents(inode.data, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = (ulong)inodeAddress,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(DataStream dataStream, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(dataStream.size <= 0) return ErrorNumber.NoError;

        var remainingBytes = (ulong)dataStream.size;

        for(var i = 0; i < DIRECT_BLOCK_COUNT && remainingBytes > 0; i++)
        {
            BlockRun directRun = dataStream.direct[i];

            if(directRun.len == 0) break;

            AddBlockRunOverlaps(directRun.group,
                                directRun.start,
                                directRun.len,
                                ref remainingBytes,
                                sectorExtents,
                                overlaps);
        }

        if(remainingBytes > 0 && dataStream.indirect.len > 0)
        {
            ErrorNumber errno =
                AddIndirectRunOverlaps(dataStream.indirect, ref remainingBytes, sectorExtents, overlaps);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(remainingBytes > 0 && dataStream.double_indirect.len > 0)
        {
            ErrorNumber errno = AddDoubleIndirectRunOverlaps(dataStream.double_indirect,
                                                             ref remainingBytes,
                                                             sectorExtents,
                                                             overlaps);

            if(errno != ErrorNumber.NoError) return errno;
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    ErrorNumber AddIndirectRunOverlaps(BlockRun                                indirectRun, ref ulong remainingBytes,
                                       IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       List<(ulong Start, ulong End)>          overlaps)
    {
        var  blockSize    = (int)_superblock.block_size;
        int  ptrsPerBlock = blockSize / 8;
        long indirectBase = (long)indirectRun.group * _superblock.blocks_per_ag + indirectRun.start;

        for(var i = 0; i < indirectRun.len && remainingBytes > 0; i++)
        {
            ErrorNumber errno = ReadBlock(indirectBase + i, out byte[] indirectBlockData);

            if(errno != ErrorNumber.NoError) return errno;

            for(var j = 0; j < ptrsPerBlock && remainingBytes > 0; j++)
            {
                int offset = j * 8;
                var group  = BitConverter.ToInt32(indirectBlockData, offset);
                var start  = BitConverter.ToUInt16(indirectBlockData, offset + 4);
                var len    = BitConverter.ToUInt16(indirectBlockData, offset + 6);

                if(len == 0) break;

                AddBlockRunOverlaps(group, start, len, ref remainingBytes, sectorExtents, overlaps);
            }
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber AddDoubleIndirectRunOverlaps(BlockRun doubleIndirectRun, ref ulong remainingBytes,
                                             IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                             List<(ulong Start, ulong End)> overlaps)
    {
        var  blockSize          = (int)_superblock.block_size;
        int  ptrsPerBlock       = blockSize / 8;
        long doubleIndirectBase = (long)doubleIndirectRun.group * _superblock.blocks_per_ag + doubleIndirectRun.start;

        for(var i = 0; i < doubleIndirectRun.len && remainingBytes > 0; i++)
        {
            ErrorNumber errno = ReadBlock(doubleIndirectBase + i, out byte[] indirectBlockData);

            if(errno != ErrorNumber.NoError) return errno;

            for(var j = 0; j < ptrsPerBlock && remainingBytes > 0; j++)
            {
                int offset      = j * 8;
                var directGroup = BitConverter.ToInt32(indirectBlockData, offset);
                var directStart = BitConverter.ToUInt16(indirectBlockData, offset + 4);
                var directLen   = BitConverter.ToUInt16(indirectBlockData, offset + 6);

                if(directLen == 0) break;

                long directBase = (long)directGroup * _superblock.blocks_per_ag + directStart;

                for(var blockIndex = 0; blockIndex < directLen && remainingBytes > 0; blockIndex++)
                {
                    errno = ReadBlock(directBase + blockIndex, out byte[] directBlockData);

                    if(errno != ErrorNumber.NoError) return errno;

                    for(var k = 0; k < ptrsPerBlock && remainingBytes > 0; k++)
                    {
                        int directOffset = k * 8;
                        var group        = BitConverter.ToInt32(directBlockData, directOffset);
                        var start        = BitConverter.ToUInt16(directBlockData, directOffset + 4);
                        var len          = BitConverter.ToUInt16(directBlockData, directOffset + 6);

                        if(len == 0) break;

                        AddBlockRunOverlaps(group, start, len, ref remainingBytes, sectorExtents, overlaps);
                    }
                }
            }
        }

        return ErrorNumber.NoError;
    }

    void AddBlockRunOverlaps(int group, ushort start, ushort len, ref ulong remainingBytes,
                             IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                             List<(ulong Start, ulong End)> overlaps)
    {
        if(len == 0 || remainingBytes == 0) return;

        ulong runBytes   = len * (ulong)_superblock.block_size;
        ulong bytesInRun = Math.Min(runBytes, remainingBytes);

        if(bytesInRun == 0) return;

        long  blockStart      = (long)group * _superblock.blocks_per_ag + start;
        ulong byteOffset      = (ulong)blockStart * _superblock.block_size;
        ulong sectorSize      = _imagePlugin.Info.SectorSize;
        ulong startSector     = _partition.Start + byteOffset / sectorSize;
        ulong offsetInSector  = byteOffset                                     % sectorSize;
        ulong sectorsInExtent = (bytesInRun + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorsInExtent == 0) sectorsInExtent = 1;

        AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);
        remainingBytes -= bytesInRun;
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