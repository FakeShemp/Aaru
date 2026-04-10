// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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
public sealed partial class QNX4
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
                                                       _superblock.RootDir,
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, in qnx4_inode_entry directoryInode,
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        if((directoryInode.di_mode & 0xF000) != 0x4000) return ErrorNumber.NotDirectory;

        ErrorNumber errno = GetDirectoryEntries(path, directoryInode, out Dictionary<string, qnx4_inode_entry> entries);

        if(errno != ErrorNumber.NoError) return errno;

        KeyValuePair<string, qnx4_inode_entry>[] orderedEntries = entries
                                                                 .Where(static entry =>
                                                                            !string.IsNullOrWhiteSpace(entry.Key) &&
                                                                            entry.Key is not "." and not "..")
                                                                 .OrderBy(static entry => entry.Key,
                                                                          StringComparer.Ordinal)
                                                                 .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            string entryPath = path == "/" ? "/" + orderedEntries[i].Key : path + "/" + orderedEntries[i].Key;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            qnx4_inode_entry entry = orderedEntries[i].Value;

            if((entry.di_mode & 0xF000) == 0x4000)
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            entry,
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

    ErrorNumber GetDirectoryEntries(string                                   path, in qnx4_inode_entry directoryInode,
                                    out Dictionary<string, qnx4_inode_entry> entries)
    {
        if(path == "/")
        {
            entries = new Dictionary<string, qnx4_inode_entry>(_rootDirectoryCache, StringComparer.Ordinal);

            return ErrorNumber.NoError;
        }

        return ReadDirectoryEntries(directoryInode, out entries);
    }

    ErrorNumber AddOverlappingFile(string                                  path,          in qnx4_inode_entry  inode,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(inode.di_size == 0) return ErrorNumber.NoError;

        ErrorNumber errno = FindOverlappingExtents(inode, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        ulong inodeId = (ulong)inode.di_first_xtnt.xtnt_blk << 32 | inode.di_first_xtnt.xtnt_size;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = inodeId,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(in qnx4_inode_entry inode, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(inode.di_size == 0) return ErrorNumber.NoError;

        ulong remainingBytes = inode.di_size;
        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        uint  totalBlocks    = (inode.di_size + QNX4_BLOCK_SIZE - 1) / QNX4_BLOCK_SIZE;

        for(uint logicalBlock = 0; logicalBlock < totalBlocks && remainingBytes > 0; logicalBlock++)
        {
            ulong bytesInBlock = Math.Min(QNX4_BLOCK_SIZE, remainingBytes);

            ErrorNumber errno = MapBlock(inode, logicalBlock, out uint physicalBlock);

            if(errno == ErrorNumber.InvalidArgument)
            {
                remainingBytes -= bytesInBlock;

                continue;
            }

            if(errno != ErrorNumber.NoError) return errno;

            if(physicalBlock == 0)
            {
                remainingBytes -= bytesInBlock;

                continue;
            }

            ulong byteOffset      = ((ulong)physicalBlock - 1) * QNX4_BLOCK_SIZE;
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