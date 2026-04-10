// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Lisa filesystem plugin.
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
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Apple Lisa File System</summary>
public sealed partial class LisaFS
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
                                                       DIRID_ROOT,
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, short directoryId,
                                                    IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        CatalogEntry[] orderedEntries = _catalogCache
                                       .Where(entry => entry.parentID == (ushort)directoryId && entry.fileID > 0)
                                       .OrderBy(entry => StringHandlers.CToString(entry.filename, _encoding),
                                                StringComparer.InvariantCultureIgnoreCase)
                                       .ToArray();

        long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

        for(var i = 0; i < orderedEntries.Length; i++)
        {
            string displayName = StringHandlers.CToString(orderedEntries[i].filename, _encoding).Replace('/', '-');

            if(string.IsNullOrWhiteSpace(displayName)) continue;

            string entryPath = path == "/" ? "/" + displayName : path + "/" + displayName;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            if(orderedEntries[i].fileType == 0x01)
            {
                ErrorNumber errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                        orderedEntries[i].fileID,
                                                                        sectorExtents,
                                                                        files,
                                                                        updateProgress,
                                                                        pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            ErrorNumber overlapErr = AddOverlappingFile(entryPath, orderedEntries[i].fileID, sectorExtents, files);

            if(overlapErr != ErrorNumber.NoError) return overlapErr;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber AddOverlappingFile(string path, short fileId, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                   List<FileSectorInfo> files)
    {
        ErrorNumber errno = ReadExtentsFile(fileId, out ExtentFile extentFile);

        if(errno != ErrorNumber.NoError) return errno;

        if(extentFile.length <= 0) return ErrorNumber.NoError;

        errno = FindOverlappingExtents(extentFile, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno != ErrorNumber.NoError) return errno;

        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = (ushort)fileId,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(ExtentFile extentFile, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(extentFile.length <= 0 || extentFile.extents is null || extentFile.extents.Length == 0)
            return ErrorNumber.NoError;

        var   remainingBytes = (ulong)extentFile.length;
        ulong sectorSize     = _device.Info.SectorSize;

        for(var i = 0; i < extentFile.extents.Length && remainingBytes > 0; i++)
        {
            Extent extent = extentFile.extents[i];

            if(extent.start <= 0 || extent.length <= 0) continue;

            ulong extentBytes = (ushort)extent.length * sectorSize;
            ulong bytesInRun  = Math.Min(extentBytes, remainingBytes);

            if(bytesInRun == 0) continue;

            ulong startSector  = (ulong)extent.start + _mddf.mddf_block + _volumePrefix;
            ulong sectorsInRun = (bytesInRun + sectorSize - 1) / sectorSize;

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