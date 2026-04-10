// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commodore filesystem plugin.
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
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class CBM
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

        if(normalizedExtents.Count == 0 || _root is null || _root.Length == 0) return ErrorNumber.NoError;

        try
        {
            initProgress?.Invoke();
            pulseProgress?.Invoke("/");

            Encoding                                  petscii        = new PETSCII();
            List<(string Path, DirectoryEntry Entry)> orderedEntries = [];

            for(var offset = 0; offset + 32 <= _root.Length; offset += 32)
            {
                DirectoryEntry dirEntry = Marshal.ByteArrayToStructureBigEndian<DirectoryEntry>(_root, offset, 32);

                if(dirEntry.fileType == 0) continue;

                var nameBytes = new byte[dirEntry.name.Length];
                Array.Copy(dirEntry.name, nameBytes, dirEntry.name.Length);

                for(var i = 0; i < nameBytes.Length; i++)
                {
                    if(nameBytes[i] == 0xA0) nameBytes[i] = 0;
                }

                string name = StringHandlers.CToString(nameBytes, petscii);

                if(string.IsNullOrWhiteSpace(name)) continue;

                orderedEntries.Add(("/" + name, dirEntry));
            }

            orderedEntries = orderedEntries.OrderBy(static entry => entry.Path, StringComparer.CurrentCultureIgnoreCase)
                                           .ToList();

            long maximum = orderedEntries.Count > 0 ? orderedEntries.Count : 1;

            for(var i = 0; i < orderedEntries.Count; i++)
            {
                updateProgress?.Invoke(orderedEntries[i].Path, i + 1, maximum);

                ErrorNumber errno = AddOverlappingFile(orderedEntries[i].Path,
                                                       orderedEntries[i].Entry,
                                                       normalizedExtents,
                                                       files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            endProgress?.Invoke();
        }
    }

#endregion

    ErrorNumber AddOverlappingFile(string                                  path,          DirectoryEntry       entry,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        ErrorNumber errno = FindOverlappingExtents(entry, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno          != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = (ulong)entry.firstFileBlockTrack << 8 | entry.firstFileBlockSector,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(DirectoryEntry entry, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(entry.blocks <= 0 || entry.firstFileBlockTrack == 0) return ErrorNumber.NoError;
        if(_device is null) return ErrorNumber.AccessDenied;

        HashSet<ulong> visitedSectors  = [];
        byte           currentTrack    = entry.firstFileBlockTrack;
        byte           currentSector   = entry.firstFileBlockSector;
        short          remainingBlocks = entry.blocks;

        while(currentTrack != 0 && remainingBlocks > 0)
        {
            ulong currentLba = CbmChsToLba(currentTrack, currentSector, _is1581);

            if(currentLba >= _device.Info.Sectors || !visitedSectors.Add(currentLba)) break;

            AddExtentOverlaps(currentLba, currentLba, sectorExtents, overlaps);

            ErrorNumber errno = _device.ReadSector(currentLba, false, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) return errno;

            currentTrack  = sector[0];
            currentSector = sector[1];
            remainingBlocks--;
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