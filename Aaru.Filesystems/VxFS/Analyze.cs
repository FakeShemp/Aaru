// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Analyze.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Veritas File System plugin.
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
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS
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

    ErrorNumber TraverseDirectoryForAffectedSectors(string path, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                                    List<FileSectorInfo> files, UpdateProgressHandler updateProgress,
                                                    PulseProgressHandler pulseProgress)
    {
        pulseProgress?.Invoke(path);

        ErrorNumber errno = OpenDir(path, out IDirNode node);

        if(errno != ErrorNumber.NoError) return errno;

        try
        {
            List<string> entries = [];

            while(true)
            {
                errno = ReadDir(node, out string entryName);

                if(errno != ErrorNumber.NoError) return errno;

                if(entryName is null) break;

                entries.Add(entryName);
            }

            string[] orderedEntries = entries
                                     .Where(static entry =>
                                                !string.IsNullOrWhiteSpace(entry) && entry is not "." and not "..")
                                     .OrderBy(static entry => entry, StringComparer.Ordinal)
                                     .ToArray();

            long maximum = orderedEntries.Length > 0 ? orderedEntries.Length : 1;

            for(var i = 0; i < orderedEntries.Length; i++)
            {
                string entryPath = path == "/" ? "/" + orderedEntries[i] : path + "/" + orderedEntries[i];

                updateProgress?.Invoke(entryPath, i + 1, maximum);

                errno = Stat(entryPath, out FileEntryInfo stat);

                if(errno != ErrorNumber.NoError) return errno;

                if(stat.Attributes.HasFlag(FileAttributes.Directory))
                {
                    errno = TraverseDirectoryForAffectedSectors(entryPath,
                                                                sectorExtents,
                                                                files,
                                                                updateProgress,
                                                                pulseProgress);

                    if(errno != ErrorNumber.NoError) return errno;

                    continue;
                }

                errno = AddOverlappingFile(entryPath, sectorExtents, files);

                if(errno != ErrorNumber.NoError) return errno;
            }

            return ErrorNumber.NoError;
        }
        finally
        {
            CloseDir(node);
        }
    }

    ErrorNumber AddOverlappingFile(string               path, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                   List<FileSectorInfo> files)
    {
        ErrorNumber errno = LookupInode(path, out DiskInode inode, out uint inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        if((VxfsFileType)(inode.vdi_mode & VXFS_TYPE_MASK) == VxfsFileType.Dir) return ErrorNumber.NoError;
        if(inode.vdi_size                                  == 0) return ErrorNumber.NoError;

        errno = FindOverlappingExtents(inode, sectorExtents, out List<(ulong Start, ulong End)> overlaps);

        if(errno          != ErrorNumber.NoError) return errno;
        if(overlaps.Count == 0) return ErrorNumber.NoError;

        files.Add(new FileSectorInfo
        {
            Path            = path,
            Inode           = inodeNumber,
            AffectedSectors = overlaps
        });

        return ErrorNumber.NoError;
    }

    ErrorNumber FindOverlappingExtents(DiskInode inode, IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                                       out List<(ulong Start, ulong End)> overlaps)
    {
        overlaps = [];

        if(inode.vdi_size == 0 || inode.vdi_org == null) return ErrorNumber.NoError;

        ulong remainingBytes = inode.vdi_size;
        int   blockSize      = _superblock.vs_bsize;

        switch((InodeOrgType)inode.vdi_orgtype)
        {
            case InodeOrgType.Immed:
                return ErrorNumber.NoError;

            case InodeOrgType.Ext4:
            {
                if(inode.vdi_org.Length < 96) return ErrorNumber.NoError;

                Ext4 ext4 = _bigEndian
                                ? Marshal.ByteArrayToStructureBigEndian<Ext4>(inode.vdi_org)
                                : Marshal.ByteArrayToStructureLittleEndian<Ext4>(inode.vdi_org);

                if(ext4.ve4_direct == null) return ErrorNumber.NoError;

                int directExtentSize = System.Runtime.InteropServices.Marshal.SizeOf<DirectExtent>();

                for(var i = 0; i < VXFS_NDADDR && remainingBytes > 0; i++)
                {
                    int offset = i * directExtentSize;

                    if(offset + directExtentSize > ext4.ve4_direct.Length) break;

                    var extBytes = new byte[directExtentSize];
                    Array.Copy(ext4.ve4_direct, offset, extBytes, 0, directExtentSize);

                    DirectExtent extent = _bigEndian
                                              ? Marshal.ByteArrayToStructureBigEndian<DirectExtent>(extBytes)
                                              : Marshal.ByteArrayToStructureLittleEndian<DirectExtent>(extBytes);

                    if(extent.size == 0) continue;

                    ulong extentBytes = Math.Min(extent.size * (ulong)blockSize, remainingBytes);
                    AddByteRangeOverlaps(extent.extent * (ulong)blockSize, extentBytes, sectorExtents, overlaps);
                    remainingBytes -= extentBytes;
                }

                break;
            }

            case InodeOrgType.Typed:
            {
                if(inode.vdi_org.Length < 96) return ErrorNumber.NoError;

                int typedExtentSize = System.Runtime.InteropServices.Marshal.SizeOf<TypedExtent>();

                for(var i = 0; i < VXFS_NTYPED && remainingBytes > 0; i++)
                {
                    int offset = i * typedExtentSize;

                    if(offset + typedExtentSize > inode.vdi_org.Length) break;

                    var extBytes = new byte[typedExtentSize];
                    Array.Copy(inode.vdi_org, offset, extBytes, 0, typedExtentSize);

                    TypedExtent extent = _bigEndian
                                             ? Marshal.ByteArrayToStructureBigEndian<TypedExtent>(extBytes)
                                             : Marshal.ByteArrayToStructureLittleEndian<TypedExtent>(extBytes);

                    var extentType = (byte)((extent.vt_hdr & VXFS_TYPED_TYPEMASK) >> VXFS_TYPED_TYPESHIFT);

                    if(extentType != (byte)TypedExtentType.Data || extent.vt_size == 0) continue;

                    ulong extentBytes = Math.Min(extent.vt_size * (ulong)blockSize, remainingBytes);
                    AddByteRangeOverlaps(extent.vt_block * (ulong)blockSize, extentBytes, sectorExtents, overlaps);
                    remainingBytes -= extentBytes;
                }

                break;
            }
        }

        overlaps = NormalizeAnalyzeExtents(overlaps);

        return ErrorNumber.NoError;
    }

    void AddByteRangeOverlaps(ulong                                   absoluteByteOffset, ulong lengthBytes,
                              IReadOnlyList<(ulong Start, ulong End)> sectorExtents,
                              List<(ulong Start, ulong End)>          overlaps)
    {
        if(lengthBytes == 0) return;

        ulong sectorSize     = _imagePlugin.Info.SectorSize;
        ulong startSector    = _partition.Start + absoluteByteOffset / sectorSize;
        ulong offsetInSector = absoluteByteOffset                              % sectorSize;
        ulong sectorCount    = (lengthBytes + offsetInSector + sectorSize - 1) / sectorSize;

        if(sectorCount == 0) sectorCount = 1;

        AddExtentOverlaps(startSector, startSector + sectorCount - 1, sectorExtents, overlaps);
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