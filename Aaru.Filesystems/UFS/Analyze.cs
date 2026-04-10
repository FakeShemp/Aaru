// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX FIle System plugin.
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class UFSPlugin
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
                                                       UFS_ROOTINO,
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
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        List<DirectoryEntryInfo> entries;

        if(directoryInodeNumber == UFS_ROOTINO && _rootEntries is not null)
            entries = _rootEntries;
        else
        {
            ErrorNumber errno = ParseDirectory(directoryInodeNumber, out entries);

            if(errno != ErrorNumber.NoError) return errno;
        }

        DirectoryEntryInfo[] orderedEntries = entries
                                             .Where(static entry =>
                                                        !string.IsNullOrWhiteSpace(entry.Name) &&
                                                        entry.Name is not "." and not "..")
                                             .OrderBy(static entry => entry.Name, StringComparer.Ordinal)
                                             .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            string entryPath = path == "/" ? "/" + orderedEntries[i].Name : path + "/" + orderedEntries[i].Name;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            ErrorNumber errno = AddEntryOverlaps(entryPath,
                                                 orderedEntries[i].Inode,
                                                 sectorExtents,
                                                 files,
                                                 updateProgress,
                                                 pulseProgress);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber AddEntryOverlaps(string path, uint inodeNumber, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                 List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                 PulseProgressHandler pulseProgress)
    {
        ushort mode;
        ulong  logicalSize;
        long[] directBlocks;
        long[] indirectBlocks;
        bool   fastInline;
        int    inodeSize;

        ErrorNumber errno = GetAnalyzeEntryData(inodeNumber,
                                                out mode,
                                                out logicalSize,
                                                out directBlocks,
                                                out indirectBlocks,
                                                out fastInline,
                                                out inodeSize);

        if(errno != ErrorNumber.NoError) return errno;

        if((mode & 0xF000) == 0x4000)
        {
            return TraverseDirectoryForAffectedSectors(path,
                                                       inodeNumber,
                                                       sectorExtents,
                                                       files,
                                                       updateProgress,
                                                       pulseProgress);
        }

        if(logicalSize == 0) return ErrorNumber.NoError;

        List<(ulong Start, ulong End)> overlaps;

        if(fastInline)
            overlaps = FindInlineOverlaps(inodeNumber, inodeSize, logicalSize, sectorExtents);
        else
        {
            errno = GetBlockList(directBlocks, indirectBlocks, logicalSize, out List<long> blockList);

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

    ErrorNumber GetAnalyzeEntryData(uint inodeNumber, out ushort mode, out ulong logicalSize, out long[] directBlocks,
                                    out long[] indirectBlocks, out bool fastInline, out int inodeSize)
    {
        mode           = 0;
        logicalSize    = 0;
        directBlocks   = null;
        indirectBlocks = null;
        fastInline     = false;
        inodeSize      = 0;

        if(_superBlock.fs_isUfs2)
        {
            ErrorNumber errno = ReadInode2(inodeNumber, out Inode2 inode2);

            if(errno != ErrorNumber.NoError) return errno;

            mode           = inode2.di_mode;
            logicalSize    = inode2.di_size;
            directBlocks   = inode2.di_db;
            indirectBlocks = inode2.di_ib;
            inodeSize      = 256;
        }
        else
        {
            ErrorNumber errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            mode        = inode.di_mode;
            logicalSize = inode.di_size;
            inodeSize   = 128;

            directBlocks = new long[NDADDR];

            for(var i = 0; i < NDADDR; i++) directBlocks[i] = inode.di_db[i];

            indirectBlocks = new long[NIADDR];

            for(var i = 0; i < NIADDR; i++) indirectBlocks[i] = inode.di_ib[i];
        }

        fastInline = (mode & 0xF000)              == 0xA000                       &&
                     (long)logicalSize            <= _superBlock.fs_maxsymlinklen &&
                     _superBlock.fs_maxsymlinklen > 0;

        return ErrorNumber.NoError;
    }

    List<(ulong Start, ulong End)> FindInlineOverlaps(uint inodeNumber, int inodeSize, ulong logicalSize,
                                                      IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps   = [];
        ulong                          sectorSize = _imagePlugin.Info.SectorSize;

        if(sectorSize is 2336 or 2352 or 2448) sectorSize = 2048;

        var cg = (int)(inodeNumber / (uint)_superBlock.fs_ipg);

        long fragAddr = CgImin(cg) +
                        (inodeNumber % (uint)_superBlock.fs_ipg / _superBlock.fs_inopb << _superBlock.fs_fragshift);

        ulong byteOffset = (ulong)fragAddr * (ulong)_superBlock.fs_fsize +
                           (ulong)((int)(inodeNumber % _superBlock.fs_inopb) * inodeSize);

        ulong bytesInExtent  = Math.Max(1, Math.Min(logicalSize, (ulong)inodeSize));
        ulong startSector    = _partition.Start + byteOffset / sectorSize;
        ulong offsetInSector = byteOffset                                        % sectorSize;
        ulong sectorCount    = (bytesInExtent + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorCount == 0) sectorCount = 1;

        AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);

        return NormalizeAnalyzeExtents(overlaps);
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(IReadOnlyList<long> blockList, ulong logicalSize,
                                                          IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps       = [];
        ulong                          remainingBytes = logicalSize;
        ulong                          sectorSize     = _imagePlugin.Info.SectorSize;

        if(sectorSize is 2336 or 2352 or 2448) sectorSize = 2048;

        foreach(long fragAddr in blockList)
        {
            if(remainingBytes == 0) break;

            ulong bytesInBlock = Math.Min((ulong)_superBlock.fs_bsize, remainingBytes);

            if(bytesInBlock == 0) break;

            if(fragAddr == 0)
            {
                remainingBytes -= bytesInBlock;

                continue;
            }

            ulong byteOffset      = (ulong)fragAddr * (ulong)_superBlock.fs_fsize;
            ulong startSector     = _partition.Start + byteOffset / sectorSize;
            ulong offsetInSector  = byteOffset                                       % sectorSize;
            ulong sectorsInExtent = (bytesInBlock + offsetInSector + sectorSize - 1) / sectorSize;

            if(sectorsInExtent == 0) sectorsInExtent = 1;

            AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);

            remainingBytes -= bytesInBlock;
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