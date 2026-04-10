// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin.
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
public sealed partial class Locus
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
                                                       ROOT_INO,
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, int directoryInodeNumber,
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        ErrorNumber errno = ReadInode(directoryInodeNumber, out Dinode directoryInode);

        if(errno != ErrorNumber.NoError) return errno;

        var fileType = (FileMode)(directoryInode.di_mode & (ushort)FileMode.IFMT);

        if(fileType != FileMode.IFDIR) return ErrorNumber.NotDirectory;

        errno = GetDirectoryEntries(directoryInodeNumber, directoryInode, out Dictionary<string, int> directoryEntries);

        if(errno != ErrorNumber.NoError) return errno;

        KeyValuePair<string, int>[] orderedEntries = directoryEntries
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

            errno = ReadInode(orderedEntries[i].Value, out Dinode inode);

            if(errno != ErrorNumber.NoError) return errno;

            fileType = (FileMode)(inode.di_mode & (ushort)FileMode.IFMT);

            if(fileType == FileMode.IFDIR)
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

    ErrorNumber GetDirectoryEntries(int                         directoryInodeNumber, Dinode directoryInode,
                                    out Dictionary<string, int> directoryEntries)
    {
        if(directoryInodeNumber == ROOT_INO)
        {
            directoryEntries = new Dictionary<string, int>(_rootDirectoryCache, StringComparer.Ordinal);

            return ErrorNumber.NoError;
        }

        return ReadDirectoryContents(directoryInodeNumber, directoryInode, out directoryEntries);
    }

    ErrorNumber AddOverlappingFile(string                                  path,          int inodeNumber, Dinode inode,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        ulong logicalSize = inode.di_size > 0 ? (ulong)inode.di_size : 0;

        if(logicalSize == 0) return ErrorNumber.NoError;

        List<(ulong Start, ulong End)> overlaps;

        if(_smallBlocks && _smallBlockDataCache.ContainsKey(inodeNumber))
            overlaps = FindInlineOverlaps(inodeNumber, logicalSize, sectorExtents);
        else
        {
            ErrorNumber errno = FindBlockOverlaps(inode, logicalSize, sectorExtents, out overlaps);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = (ulong)inodeNumber,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    List<(ulong Start, ulong End)> FindInlineOverlaps(int inodeNumber, ulong logicalSize,
                                                      IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps       = [];
        ulong                          sectorSize     = _imagePlugin.Info.SectorSize;
        int                            inodeSize      = _smallBlocks ? DINODE_SMALLBLOCK_SIZE : DINODE_SIZE;
        int                            inodeBlock     = (inodeNumber - 1) / _inodesPerBlock + 2;
        int                            inodeOffset    = (inodeNumber - 1) % _inodesPerBlock * inodeSize;
        ulong                          byteOffset     = (ulong)inodeBlock * (ulong)_blockSize + (ulong)inodeOffset;
        ulong                          bytesInExtent  = Math.Max(1, Math.Min(logicalSize, (ulong)inodeSize));
        ulong                          startSector    = _partition.Start + byteOffset / sectorSize;
        ulong                          offsetInSector = byteOffset                                        % sectorSize;
        ulong                          sectorCount    = (bytesInExtent + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorCount == 0) sectorCount = 1;

        AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);

        return NormalizeAnalyzeExtents(overlaps);
    }

    ErrorNumber FindBlockOverlaps(Dinode                                  inode, ulong logicalSize,
                                  IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                  out List<(ulong Start, ulong End)>      overlaps)
    {
        overlaps = [];

        if(logicalSize == 0) return ErrorNumber.NoError;

        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        ulong remainingBytes = logicalSize;
        var   totalBlocks    = (int)((logicalSize + (ulong)_blockSize - 1) / (ulong)_blockSize);

        for(var logicalBlock = 0; logicalBlock < totalBlocks && remainingBytes > 0; logicalBlock++)
        {
            ulong bytesInBlock = Math.Min((ulong)_blockSize, remainingBytes);

            ErrorNumber errno = GetPhysicalBlock(inode, logicalBlock, out int physicalBlock);

            if(errno != ErrorNumber.NoError) return errno;

            if(physicalBlock == 0)
            {
                remainingBytes -= bytesInBlock;

                continue;
            }

            ulong byteOffset      = (ulong)physicalBlock * (ulong)_blockSize;
            ulong startSector     = _partition.Start + byteOffset / sectorSize;
            ulong offsetInSector  = byteOffset                                       % sectorSize;
            ulong sectorsInExtent = (bytesInBlock + offsetInSector + sectorSize - 1) / sectorSize;

            if(sectorsInExtent == 0) sectorsInExtent = 1;

            AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);

            remainingBytes -= bytesInBlock;
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
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

        if(orderedExtents.Count == 0) return [];

        List<(ulong Start, ulong End)> normalizedExtents = [orderedExtents[0]];

        for(var i = 1; i < orderedExtents.Count; i++)
        {
            (ulong Start, ulong End) currentExtent = orderedExtents[i];
            (ulong Start, ulong End) lastExtent    = normalizedExtents[^1];

            if(currentExtent.Start <= lastExtent.End + 1)
                normalizedExtents[^1] = (lastExtent.Start, Math.Max(lastExtent.End, currentExtent.End));
            else
                normalizedExtents.Add(currentExtent);
        }

        return normalizedExtents;
    }
}