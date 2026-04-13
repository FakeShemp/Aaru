// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;

namespace Aaru.Filesystems;

public partial class SonyPFS
{
    /// <summary>Reads an inode from the image at the specified block number and sub-partition.</summary>
    /// <param name="blockNumber">Inode block number.</param>
    /// <param name="subpart">Sub-partition (only 0/main is supported).</param>
    /// <param name="inode">The deserialized inode structure.</param>
    /// <returns><see cref="ErrorNumber.NoError" /> on success.</returns>
    ErrorNumber ReadInode(uint blockNumber, ushort subpart, out Inode inode)
    {
        inode = default;

        // Only main partition is accessible through the image
        if(subpart != 0)
            return ErrorNumber.InvalidArgument;

        // Calculate the sector offset: blockNumber << inode_scale gives the block in zone units,
        // then multiply by sectors per zone to get sector offset
        ulong sectorOffset = (ulong)(blockNumber << (int)_inodeScale) * _sectorsPerZone;
        ulong sector       = _partitionStart + sectorOffset;

        int inodeSize     = Marshal.SizeOf<Inode>();
        var sectorsToRead = (uint)((inodeSize + _sectorSize - 1) / _sectorSize);

        ErrorNumber errno = _image.ReadSectors(sector, false, sectorsToRead, out byte[] data, out _);

        if(errno != ErrorNumber.NoError)
            return errno;

        if(data.Length < inodeSize)
            return ErrorNumber.InvalidArgument;

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(data);

        if(inode.magic != PFS_SEGD_MAGIC && inode.magic != PFS_SEGI_MAGIC)
            return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a data block from the image.</summary>
    /// <param name="bi">Block info describing the location.</param>
    /// <param name="offsetInZone">Block offset within the zone run.</param>
    /// <param name="data">The raw data read (one zone worth).</param>
    /// <returns><see cref="ErrorNumber.NoError" /> on success.</returns>
    ErrorNumber ReadDataBlock(BlockInfo bi, uint offsetInZone, out byte[] data)
    {
        data = null;

        if(bi.subpart != 0)
            return ErrorNumber.InvalidArgument;

        ulong sectorOffset = (ulong)((bi.number + offsetInZone) << (int)_inodeScale) * _sectorsPerZone;
        ulong sector       = _partitionStart + sectorOffset;

        ErrorNumber errno = _image.ReadSectors(sector, false, _sectorsPerZone, out data, out _);

        return errno;
    }

    /// <summary>Converts a PFS DateTime to a .NET DateTimeOffset.</summary>
    static DateTimeOffset PfsDateTimeToDateTimeOffset(DateTime pfsDateTime)
    {
        try
        {
            return new DateTimeOffset(pfsDateTime.year, pfsDateTime.month, pfsDateTime.day,
                                      pfsDateTime.hour, pfsDateTime.min, pfsDateTime.sec,
                                      TimeSpan.Zero);
        }
        catch
        {
            return DateTimeOffset.MinValue;
        }
    }
}