// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Xia filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class Xia
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

        ErrorNumber errno = ReadInode(XIAFS_ROOT_INO, out Inode rootInode);

        if(errno != ErrorNumber.NoError) return errno;

        try
        {
            initProgress?.Invoke();

            return TraverseDirectoryForAffectedSectors("/",
                                                       XIAFS_ROOT_INO,
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, uint directoryInodeNumber, in Inode directoryInode,
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        if((directoryInode.i_mode & 0xF000) != 0x4000) return ErrorNumber.NotDirectory;

        ErrorNumber errno = GetDirectoryEntries(directoryInodeNumber,
                                                directoryInode,
                                                out Dictionary<string, uint> entries);

        if(errno != ErrorNumber.NoError) return errno;

        KeyValuePair<string, uint>[] orderedEntries = entries
                                                     .Where(static entry =>
                                                                !string.IsNullOrWhiteSpace(entry.Key) &&
                                                                entry.Key is not "." and not "..")
                                                     .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
                                                     .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            string entryPath = path == "/" ? "/" + orderedEntries[i].Key : path + "/" + orderedEntries[i].Key;
            uint   inodeNum  = orderedEntries[i].Value;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            errno = ReadInode(inodeNum, out Inode entryInode);

            if(errno != ErrorNumber.NoError) return errno;

            if((entryInode.i_mode & 0xF000) == 0x4000)
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            inodeNum,
                                                            entryInode,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            errno = AddOverlappingFile(entryPath, inodeNum, entryInode, sectorExtents, files);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber GetDirectoryEntries(uint                         directoryInodeNumber, in Inode directoryInode,
                                    out Dictionary<string, uint> entries)
    {
        if(directoryInodeNumber == XIAFS_ROOT_INO)
        {
            entries = new Dictionary<string, uint>(_rootDirectoryCache, StringComparer.Ordinal);

            return ErrorNumber.NoError;
        }

        return ReadDirectoryEntries(directoryInode, out entries);
    }

    ErrorNumber AddOverlappingFile(string                                  path, uint inodeNumber, in Inode inode,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(inode.i_size == 0) return ErrorNumber.NoError;

        ErrorNumber errno = FindOverlappingExtents(inode, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = inodeNumber,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(in  Inode inode, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(inode.i_size == 0) return ErrorNumber.NoError;

        ulong remainingBytes = inode.i_size;
        ulong zoneSize       = _superblock.s_zone_size;
        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        var   totalZones     = (uint)((inode.i_size + zoneSize - 1) / zoneSize);

        for(uint logicalZone = 0; logicalZone < totalZones && remainingBytes > 0; logicalZone++)
        {
            ulong bytesInZone = Math.Min(zoneSize, remainingBytes);

            ErrorNumber errno = MapZone(inode, logicalZone, out uint physicalZone);

            if(errno != ErrorNumber.NoError) return errno;

            if(physicalZone == 0)
            {
                remainingBytes -= bytesInZone;

                continue;
            }

            ulong byteOffset      = physicalZone * zoneSize;
            ulong startSector     = _partition.Start + byteOffset / sectorSize;
            ulong offsetInSector  = byteOffset                                      % sectorSize;
            ulong sectorsInExtent = (bytesInZone + offsetInSector + sectorSize - 1) / sectorSize;

            if(sectorsInExtent == 0) sectorsInExtent = 1;

            AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);
            remainingBytes -= bytesInZone;
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