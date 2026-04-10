// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ISO9660 filesystem plugin.
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
// In the loving memory of Facunda "Tata" Suárez Domínguez, R.I.P. 2019/07/24
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

public sealed partial class ISO9660
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

            return TraverseDirectoryForAffectedSectors("/", normalizedExtents, files, updateProgress, pulseProgress);
        }
        finally
        {
            endProgress?.Invoke();
        }
    }

#endregion

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, List<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        ErrorNumber errno = OpenDir(path, out IDirNode node);

        if(errno != ErrorNumber.NoError) return errno;

        if(node is not Iso9660DirNode dirNode)
        {
            CloseDir(node);

            return ErrorNumber.InvalidArgument;
        }

        string[] entries = dirNode.Entries?.Select(static entry => entry.Filename)
                                  .Where(static filename => !string.IsNullOrWhiteSpace(filename))
                                  .Distinct(StringComparer.CurrentCultureIgnoreCase)
                                  .ToArray() ?? [];

        CloseDir(node);

        long maximum = entries.Length > 0 ? entries.Length : 1;

        for(var i = 0; i < entries.Length; i++)
        {
            string entryPath = path == "/" ? "/" + entries[i] : path + "/" + entries[i];

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            errno = GetFileEntry(entryPath, out DecodedDirectoryEntry entry);

            if(errno != ErrorNumber.NoError) continue;

            if(entry.Flags.HasFlag(FileFlags.Directory))
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            AddOverlappingFile(entryPath, null,              entry,                sectorExtents, files);
            AddOverlappingFile(entryPath, "resource-fork",   entry.ResourceFork,   sectorExtents, files);
            AddOverlappingFile(entryPath, "associated-file", entry.AssociatedFile, sectorExtents, files);
        }

        return ErrorNumber.NoError;
    }

    void AddOverlappingFile(string                                  path, string stream, DecodedDirectoryEntry entry,
                            IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        if(entry?.Extents is null || entry.Extents.Count == 0) return;

        List<(ulong Start, ulong End)> overlaps = FindOverlappingExtents(entry, sectorExtents);

        if(overlaps.Count == 0) return;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Stream          = stream,
            Inode           = entry.Extents[0].extent,
            AffectedSectors = overlaps
        });
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(DecodedDirectoryEntry                   entry,
                                                          IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps        = [];
        ulong                          sectorsPerBlock = (ulong)_blockSize / 2048;

        if(_blockSize % 2048 > 0) sectorsPerBlock++;

        if(sectorsPerBlock == 0) sectorsPerBlock = 1;

        foreach((uint extent, uint size) fileExtent in entry.Extents)
        {
            ulong blocks = fileExtent.size / _blockSize;

            if(fileExtent.size % _blockSize > 0) blocks++;

            if(blocks == 0) blocks = 1;

            ulong startSector = _partitionStart                        + fileExtent.extent * sectorsPerBlock;
            ulong endSector   = startSector + blocks * sectorsPerBlock - 1;

            foreach((ulong Start, ulong End) requestedExtent in sectorExtents)
            {
                if(requestedExtent.End < startSector || requestedExtent.Start > endSector) continue;

                overlaps.Add((Math.Max(startSector, requestedExtent.Start), Math.Min(endSector, requestedExtent.End)));
            }
        }

        return NormalizeAnalyzeExtents(overlaps);
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