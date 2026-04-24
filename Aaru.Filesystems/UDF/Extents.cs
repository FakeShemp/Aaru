// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Extents.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Gets physical sector extents for a file path.
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
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System.Collections.Generic;
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

public sealed partial class UDF
{
    /// <summary>
    ///     Gets physical sector extents for a file path.
    /// </summary>
    /// <param name="path">Absolute filesystem path.</param>
    /// <param name="extents">Physical extents as (startSector, sectorCount).</param>
    /// <returns>Error number.</returns>
    public ErrorNumber GetFilePhysicalSectorExtents(string                                          path,
                                                    out List<(ulong startSector, uint sectorCount)> extents)
    {
        extents = [];

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetFileEntryBuffer(path, out byte[] feBuffer, out ushort partitionReferenceNumber);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ParseFileEntryInfo(feBuffer, out UdfFileEntryInfo info);

        if(errno != ErrorNumber.NoError) return errno;

        var adType = (byte)((ushort)info.IcbTag.flags & 0x07);

        // Embedded data and unsupported extended allocation descriptors have no physical extents
        if(adType == 3) return ErrorNumber.NoError;
        if(adType == 2) return ErrorNumber.NotSupported;
        if(adType != 0 && adType != 1) return ErrorNumber.InvalidArgument;

        int fixedSize = info.IsExtended ? 216 : 176;
        int adOffset  = fixedSize + (int)info.LengthOfExtendedAttributes;
        var adLength  = (int)info.LengthOfAllocationDescriptors;

        // Walk the allocation descriptor chain, following any type-3 continuation pointers.
        errno = CollectAllocationDescriptors(feBuffer,
                                             adOffset,
                                             adLength,
                                             adType,
                                             partitionReferenceNumber,
                                             out List<UdfExtent> adExtents);

        if(errno != ErrorNumber.NoError) return errno;

        foreach(UdfExtent extent in adExtents)
        {
            // Sparse extents (types 1 and 2) have no recorded physical sectors to surface here.
            if(extent.Type != 0) continue;

            uint sectorCount = (extent.Length + _sectorSize - 1) / _sectorSize;

            if(sectorCount == 0) continue;

            ulong start = TranslateLogicalBlock(extent.LogicalBlock,
                                                extent.PartitionReferenceNumber,
                                                _partitionStartingLocation);

            extents.Add((start, sectorCount));
        }

        return ErrorNumber.NoError;
    }
}