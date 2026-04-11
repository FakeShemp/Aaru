// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System Plus plugin.
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
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
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
                                                       kHFSRootFolderID,
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, uint directoryCnid,
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        ErrorNumber errno = CacheDirectoryIfNeeded(directoryCnid);

        if(errno != ErrorNumber.NoError) return errno;

        Dictionary<string, CatalogEntry> entries = GetDirectoryEntries(directoryCnid);

        if(entries is null || entries.Count == 0) return ErrorNumber.NoError;

        KeyValuePair<string, CatalogEntry>[] orderedEntries = entries
                                                             .Where(static entry =>
                                                                        !string.IsNullOrWhiteSpace(entry.Key))
                                                             .OrderBy(static entry => entry.Key,
                                                                      StringComparer.OrdinalIgnoreCase)
                                                             .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            CatalogEntry entry = orderedEntries[i].Value;

            string displayName = string.IsNullOrEmpty(entry.Name)
                                     ? orderedEntries[i].Key.Replace("/", ":")
                                     : entry.Name.Replace("/", ":");

            string entryPath = path == "/" ? "/" + displayName : path + "/" + displayName;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            if(entry is DirectoryEntry directoryEntry)
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            directoryEntry.CNID,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            if(entry is not FileEntry fileEntry) continue;

            errno = AddOverlappingForks(entryPath, fileEntry, sectorExtents, files);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber AddOverlappingForks(string                                  path,          FileEntry fileEntry,
                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(fileEntry.DataForkLogicalSize > 0)
        {
            ErrorNumber errno = GetFileExtents(fileEntry.CNID,
                                               fileEntry.DataForkExtents,
                                               fileEntry.DataForkTotalBlocks,
                                               out List<HFSPlusExtentDescriptor> dataExtents);

            if(errno != ErrorNumber.NoError) return errno;

            errno = AddForkOverlaps(path,
                                    null,
                                    fileEntry.CNID,
                                    fileEntry.DataForkLogicalSize,
                                    dataExtents,
                                    sectorExtents,
                                    files);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(fileEntry.ResourceForkLogicalSize == 0) return ErrorNumber.NoError;

        ErrorNumber resourceErr = GetResourceForkExtents(fileEntry, out List<HFSPlusExtentDescriptor> resourceExtents);

        if(resourceErr != ErrorNumber.NoError) return resourceErr;

        return AddForkOverlaps(path,
                               Xattrs.XATTR_APPLE_RESOURCE_FORK,
                               fileEntry.CNID,
                               fileEntry.ResourceForkLogicalSize,
                               resourceExtents,
                               sectorExtents,
                               files);
    }

    ErrorNumber AddForkOverlaps(string path, string stream, uint inode, ulong logicalSize,
                                IReadOnlyList<HFSPlusExtentDescriptor> extents,
                                IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        ErrorNumber errno = FindOverlappingExtents(extents,
                                                   logicalSize,
                                                   sectorExtents,
                                                   out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Stream          = stream,
            Inode           = inode,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(IReadOnlyList<HFSPlusExtentDescriptor>  extents, ulong logicalSize,
                                       IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)>      overlaps)
    {
        overlaps = [];

        if(logicalSize == 0 || extents.Count == 0) return ErrorNumber.NoError;

        ulong remainingBytes = logicalSize;

        for(var i = 0; i < extents.Count && remainingBytes > 0; i++)
        {
            HFSPlusExtentDescriptor extent = extents[i];

            if(extent.blockCount == 0) continue;

            ulong extentBytes = (ulong)extent.blockCount * _volumeHeader.blockSize;
            ulong bytesInRun  = Math.Min(extentBytes, remainingBytes);

            if(bytesInRun == 0) continue;

            ulong absoluteByteOffset = (_partitionStart + _hfsPlusVolumeOffset) * _sectorSize +
                                       (ulong)extent.startBlock                 * _volumeHeader.blockSize;

            ulong startSector    = absoluteByteOffset                              / _sectorSize;
            ulong offsetInSector = absoluteByteOffset                              % _sectorSize;
            ulong sectorsInRun   = (bytesInRun + offsetInSector + _sectorSize - 1) / _sectorSize;

            if(sectorsInRun == 0) sectorsInRun = 1;

            AddExtentOverlaps(startSector, startSector + sectorsInRun - 1, sectorExtents, overlaps);
            remainingBytes -= bytesInRun;
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