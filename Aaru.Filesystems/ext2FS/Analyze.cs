// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
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

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
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
                                                       EXT2_ROOT_INO,
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, uint directoryInodeNumber,
                                                    List<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        ErrorNumber errno = ReadInode(directoryInodeNumber, out Inode directoryInode);

        if(errno                          != ErrorNumber.NoError) return errno;
        if((directoryInode.mode & S_IFMT) != S_IFDIR) return ErrorNumber.NotDirectory;

        errno = GetDirectoryEntries(directoryInodeNumber,
                                    directoryInode,
                                    out Dictionary<string, uint> directoryEntries);

        if(errno != ErrorNumber.NoError) return errno;

        KeyValuePair<string, uint>[] orderedEntries = directoryEntries
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

            errno = ReadInode(orderedEntries[i].Value, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            if((inode.mode & S_IFMT) == S_IFDIR)
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            orderedEntries[i].Value,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            errno = AddOverlappingFile(entryPath, orderedEntries[i].Value, inode, sectorExtents, files);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber GetDirectoryEntries(uint                         directoryInodeNumber, in Inode directoryInode,
                                    out Dictionary<string, uint> directoryEntries)
    {
        if(directoryInodeNumber == EXT2_ROOT_INO)
        {
            directoryEntries = new Dictionary<string, uint>(_rootDirectoryCache, StringComparer.Ordinal);

            return ErrorNumber.NoError;
        }

        ulong directorySize = (ulong)directoryInode.size_high << 32 | directoryInode.size_lo;

        return ReadDirectoryEntries(directoryInode, directoryInodeNumber, directorySize, out directoryEntries);
    }

    ErrorNumber AddOverlappingFile(string                                  path, uint inodeNumber, in Inode inode,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        ulong logicalSize = (ulong)inode.size_high << 32 | inode.size_lo;

        if(logicalSize == 0) return ErrorNumber.NoError;

        List<(ulong Start, ulong End)> overlaps;

        if((inode.i_flags & EXT4_INLINE_DATA_FL) != 0)
            overlaps = FindInlineOverlaps(inodeNumber, logicalSize, sectorExtents);
        else
        {
            ErrorNumber errno = GetInodeDataBlocks(inode,
                                                   out List<(ulong physicalBlock, uint length, bool unwritten)>
                                                           blockList);

            if(errno != ErrorNumber.NoError) return errno;

            overlaps = FindOverlappingExtents(blockList, logicalSize, sectorExtents);
        }

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = inodeNumber,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    List<(ulong Start, ulong End)> FindInlineOverlaps(uint inodeNumber, ulong logicalSize,
                                                      IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps = [];

        ErrorNumber errno = GetInodeByteOffset(inodeNumber, out ulong inodeByteOffset);

        if(errno != ErrorNumber.NoError) return overlaps;

        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        ulong bytesInExtent  = Math.Max(1, Math.Min(logicalSize, _inodeSize));
        ulong startSector    = _partition.Start + inodeByteOffset / sectorSize;
        ulong offsetInSector = inodeByteOffset                                   % sectorSize;
        ulong sectorCount    = (bytesInExtent + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorCount == 0) sectorCount = 1;

        AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);

        return NormalizeAnalyzeExtents(overlaps);
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(
        IReadOnlyList<(ulong physicalBlock, uint length, bool unwritten)> blockList, ulong logicalSize,
        IReadOnlyList<(ulong Start, ulong End)>                           sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps       = [];
        ulong                          remainingBytes = logicalSize;
        ulong                          sectorSize     = _imagePlugin.Info.SectorSize;

        foreach((ulong physicalBlock, uint length, bool unwritten) extent in blockList)
        {
            if(remainingBytes == 0) break;

            if(extent.length == 0) continue;

            ulong extentBytes = Math.Min((ulong)extent.length * _blockSize, remainingBytes);

            if(extentBytes == 0) break;

            ulong byteOffset      = extent.physicalBlock * _blockSize;
            ulong startSector     = _partition.Start + byteOffset / sectorSize;
            ulong offsetInSector  = byteOffset                                      % sectorSize;
            ulong sectorsInExtent = (extentBytes + offsetInSector + sectorSize - 1) / sectorSize;

            if(sectorsInExtent == 0) sectorsInExtent = 1;

            AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);

            remainingBytes -= extentBytes;
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