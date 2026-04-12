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

using System;
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class UDF
{
    /// <summary>
    ///     Gets physical sector extents for a file path.
    /// </summary>
    /// <param name="path">Absolute filesystem path.</param>
    /// <param name="extents">Physical extents as (startSector, sectorCount).</param>
    /// <returns>Error number.</returns>
    public ErrorNumber GetFilePhysicalSectorExtents(string path, out List<(ulong startSector, uint sectorCount)> extents)
    {
        extents = [];

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetFileEntryBuffer(path, out byte[] feBuffer, out ushort partitionReferenceNumber);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ParseFileEntryInfo(feBuffer, out UdfFileEntryInfo info);

        if(errno != ErrorNumber.NoError) return errno;

        var adType = (byte)((ushort)info.IcbTag.flags & 0x07);

        int fixedSize = info.IsExtended ? 216 : 176;
        int adOffset  = fixedSize + (int)info.LengthOfExtendedAttributes;
        int adLength  = (int)info.LengthOfAllocationDescriptors;

        return adType switch
               {
                   0 => CollectShortAdExtents(feBuffer,
                                              adOffset,
                                              adLength,
                                              partitionReferenceNumber,
                                              extents),
                   1 => CollectLongAdExtents(feBuffer, adOffset, adLength, extents),
                   2 => ErrorNumber.NotSupported,
                   3 => ErrorNumber.NoError,
                   _ => ErrorNumber.InvalidArgument
               };
    }

    /// <summary>
    ///     Collects short allocation descriptor extents.
    /// </summary>
    /// <param name="feBuffer">File entry buffer.</param>
    /// <param name="adOffset">Allocation descriptor offset.</param>
    /// <param name="adLength">Allocation descriptor length.</param>
    /// <param name="partitionReferenceNumber">Partition reference number.</param>
    /// <param name="extents">Physical extents as (startSector, sectorCount).</param>
    /// <returns>Error number.</returns>
    ErrorNumber CollectShortAdExtents(byte[] feBuffer, int adOffset, int adLength, ushort partitionReferenceNumber,
                                      List<(ulong startSector, uint sectorCount)> extents)
    {
        int sadSize = System.Runtime.InteropServices.Marshal.SizeOf<ShortAllocationDescriptor>();
        int adPos   = adOffset;

        while(adPos + sadSize <= adOffset + adLength)
        {
            ShortAllocationDescriptor sad =
                Marshal.ByteArrayToStructureLittleEndian<ShortAllocationDescriptor>(feBuffer, adPos, sadSize);

            uint extentLength = sad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            uint sectorCount = (extentLength + _sectorSize - 1) / _sectorSize;

            if(sectorCount > 0)
            {
                ulong start = TranslateLogicalBlock(sad.extentLocation, partitionReferenceNumber, _partitionStartingLocation);
                extents.Add((start, sectorCount));
            }

            adPos += sadSize;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Collects long allocation descriptor extents.
    /// </summary>
    /// <param name="feBuffer">File entry buffer.</param>
    /// <param name="adOffset">Allocation descriptor offset.</param>
    /// <param name="adLength">Allocation descriptor length.</param>
    /// <param name="extents">Physical extents as (startSector, sectorCount).</param>
    /// <returns>Error number.</returns>
    ErrorNumber CollectLongAdExtents(byte[] feBuffer, int adOffset, int adLength,
                                     List<(ulong startSector, uint sectorCount)> extents)
    {
        int ladSize = System.Runtime.InteropServices.Marshal.SizeOf<LongAllocationDescriptor>();
        int adPos   = adOffset;

        while(adPos + ladSize <= adOffset + adLength)
        {
            LongAllocationDescriptor lad =
                Marshal.ByteArrayToStructureLittleEndian<LongAllocationDescriptor>(feBuffer, adPos, ladSize);

            uint extentLength = lad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            uint sectorCount = (extentLength + _sectorSize - 1) / _sectorSize;

            if(sectorCount > 0)
            {
                ulong start = TranslateLogicalBlock(lad.extentLocation.logicalBlockNumber,
                                                    lad.extentLocation.partitionReferenceNumber,
                                                    _partitionStartingLocation);
                extents.Add((start, sectorCount));
            }

            adPos += ladSize;
        }

        return ErrorNumber.NoError;
    }
}
