// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Universal Disk Format plugin.
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class UDF
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

        ErrorNumber errno = GetDirectoryEntries(path, out Dictionary<string, UdfDirectoryEntry> entries);

        if(errno != ErrorNumber.NoError) return errno;

        var orderedEntries = entries.Values.Where(static entry => entry?.Filename != null)
                                    .OrderBy(static entry => entry.Filename, StringComparer.CurrentCultureIgnoreCase)
                                    .ToList();

        long maximum = orderedEntries.Count > 0 ? orderedEntries.Count : 1;

        for(var i = 0; i < orderedEntries.Count; i++)
        {
            UdfDirectoryEntry entry     = orderedEntries[i];
            string            entryPath = path == "/" ? "/" + entry.Filename : path + "/" + entry.Filename;

            updateProgress?.Invoke(entryPath, i + 1, maximum);

            if(entry.FileCharacteristics.HasFlag(FileCharacteristics.Directory))
            {
                errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                            sectorExtents,
                                                            files,
                                                            updateProgress,
                                                            pulseProgress);

                if(errno != ErrorNumber.NoError) return errno;

                continue;
            }

            errno = AddOverlappingFile(entryPath, null, entry.Icb, sectorExtents, files);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber AddOverlappingFile(string path, string stream, LongAllocationDescriptor icb,
                                   IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        ErrorNumber errno = ReadSectorFromPartition(icb.extentLocation.logicalBlockNumber,
                                                    icb.extentLocation.partitionReferenceNumber,
                                                    _partitionStartingLocation,
                                                    out byte[] feBuffer);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ParseFileEntryInfo(feBuffer, out UdfFileEntryInfo fileEntryInfo);

        if(errno != ErrorNumber.NoError) return errno;

        if(fileEntryInfo.IcbTag.fileType == FileType.Directory) return ErrorNumber.NoError;

        List<(ulong Start, ulong End)> overlaps = FindOverlappingExtents(fileEntryInfo,
                                                                         feBuffer,
                                                                         icb.extentLocation.partitionReferenceNumber,
                                                                         icb,
                                                                         sectorExtents);

        if(overlaps.Count > 0)
        {
            files.Add(new FileSectorInfo
            {
                Path            = path,
                Stream          = stream,
                Inode           = fileEntryInfo.UniqueId,
                AffectedSectors = overlaps
            });
        }

        if(!fileEntryInfo.IsExtended                          ||
           fileEntryInfo.StreamDirectoryICB.extentLength == 0 ||
           !string.IsNullOrWhiteSpace(stream))
            return ErrorNumber.NoError;

        return AddNamedStreams(path, fileEntryInfo.StreamDirectoryICB, sectorExtents, files);
    }

    ErrorNumber AddNamedStreams(string path, LongAllocationDescriptor streamDirectoryIcb,
                                IReadOnlyList<(ulong Start, ulong End)> sectorExtents, List<FileSectorInfo> files)
    {
        ErrorNumber errno = ReadNamedStreams(streamDirectoryIcb, out List<UdfNamedStream> streams);

        if(errno != ErrorNumber.NoError) return errno;

        foreach(UdfNamedStream stream in streams)
        {
            if(stream == null || string.IsNullOrWhiteSpace(stream.Name)) continue;

            string streamName = !string.IsNullOrWhiteSpace(stream.XattrName) ? stream.XattrName : stream.Name;

            errno = AddOverlappingFile(path, streamName, stream.Icb, sectorExtents, files);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    List<(ulong Start, ulong End)> FindOverlappingExtents(UdfFileEntryInfo info, byte[] feBuffer,
                                                          ushort partitionReferenceNumber, LongAllocationDescriptor icb,
                                                          IReadOnlyList<(ulong Start, ulong End)> sectorExtents)
    {
        List<(ulong Start, ulong End)> overlaps = [];
        var adType = (byte)((ushort)info.IcbTag.flags & 0x07);
        int fixedSize = info.IsExtended ? 216 : 176;
        int adOffset = fixedSize + (int)info.LengthOfExtendedAttributes;
        var adLength = (int)info.LengthOfAllocationDescriptors;
        long remainingBytes = info.InformationLength > long.MaxValue ? long.MaxValue : (long)info.InformationLength;

        if(remainingBytes <= 0 && adType  != 3) return overlaps;
        if(adOffset       < 0 || adOffset > feBuffer.Length || adLength < 0) return overlaps;

        switch(adType)
        {
            case 0:
            case 1:
                CollectAllocationDescriptorOverlaps(feBuffer,
                                                    adOffset,
                                                    adLength,
                                                    adType,
                                                    partitionReferenceNumber,
                                                    remainingBytes,
                                                    sectorExtents,
                                                    overlaps);

                break;
            case 2:
                CollectExtendedAllocationOverlaps(feBuffer,
                                                  adOffset,
                                                  adLength,
                                                  remainingBytes,
                                                  sectorExtents,
                                                  overlaps);

                break;
            case 3:
            {
                long embeddedLength = fixedSize + info.LengthOfExtendedAttributes + remainingBytes;

                if(embeddedLength <= 0) break;

                ulong startSector = TranslateLogicalBlock(icb.extentLocation.logicalBlockNumber,
                                                          icb.extentLocation.partitionReferenceNumber,
                                                          _partitionStartingLocation);

                var sectorsInExtent = (ulong)((embeddedLength + _sectorSize - 1) / _sectorSize);

                if(sectorsInExtent == 0) sectorsInExtent = 1;

                AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);

                break;
            }
        }

        return NormalizeAnalyzeExtents(overlaps);
    }

    /// <summary>
    ///     Collects affected-sector overlaps from a Short or Long allocation descriptor chain,
    ///     transparently following type-3 continuation pointers and skipping sparse (types 1/2) extents
    ///     which have no on-disk sectors backing them.
    /// </summary>
    void CollectAllocationDescriptorOverlaps(byte[] feBuffer, int adOffset, int adLength, byte adType,
                                             ushort partitionReferenceNumber, long remainingBytes,
                                             IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                             List<(ulong Start, ulong End)> overlaps)
    {
        ErrorNumber errno = CollectAllocationDescriptors(feBuffer,
                                                         adOffset,
                                                         adLength,
                                                         adType,
                                                         partitionReferenceNumber,
                                                         out List<UdfExtent> extents);

        if(errno != ErrorNumber.NoError) return;

        foreach(UdfExtent extent in extents)
        {
            if(remainingBytes <= 0) break;

            if(extent.Length == 0) continue;

            long bytesInExtent = Math.Min(extent.Length, remainingBytes);

            // Sparse extents (types 1 and 2) don't occupy any physical sectors, but they still
            // consume logical file space — so advance remainingBytes without emitting an overlap.
            if(extent.Type != 0)
            {
                remainingBytes -= bytesInExtent;

                continue;
            }

            ulong startSector = TranslateLogicalBlock(extent.LogicalBlock,
                                                      extent.PartitionReferenceNumber,
                                                      _partitionStartingLocation);

            var sectorsInExtent = (ulong)((bytesInExtent + _sectorSize - 1) / _sectorSize);

            if(sectorsInExtent == 0) sectorsInExtent = 1;

            AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);
            remainingBytes -= bytesInExtent;
        }
    }

    void CollectExtendedAllocationOverlaps(byte[] feBuffer, int adOffset, int adLength, long remainingBytes,
                                           IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                           List<(ulong Start, ulong End)> overlaps)
    {
        int adPos   = adOffset;
        int eadSize = System.Runtime.InteropServices.Marshal.SizeOf<ExtendedAllocationDescriptor>();

        while(adPos + eadSize <= feBuffer.Length && adPos < adOffset + adLength && remainingBytes > 0)
        {
            ExtendedAllocationDescriptor ead =
                Marshal.ByteArrayToStructureLittleEndian<ExtendedAllocationDescriptor>(feBuffer, adPos, eadSize);

            uint extentLength = ead.extentLength & 0x3FFFFFFF;

            if(extentLength == 0)
            {
                adPos += eadSize;

                continue;
            }

            long bytesInExtent = Math.Min(extentLength, remainingBytes);

            if(bytesInExtent > 0)
            {
                ulong startSector = TranslateLogicalBlock(ead.extentLocation.logicalBlockNumber,
                                                          ead.extentLocation.partitionReferenceNumber,
                                                          _partitionStartingLocation);

                var sectorsInExtent = (ulong)((bytesInExtent + _sectorSize - 1) / _sectorSize);

                if(sectorsInExtent == 0) sectorsInExtent = 1;

                AddExtentOverlaps(startSector, startSector + sectorsInExtent - 1, sectorExtents, overlaps);
                remainingBytes -= bytesInExtent;
            }

            adPos += eadSize;
        }
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