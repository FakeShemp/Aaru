// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Zone.cs
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
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class Xia
{
    /// <summary>Reads a zone from the filesystem</summary>
    /// <param name="zoneNumber">The zone number to read</param>
    /// <param name="zoneData">The zone data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadZone(uint zoneNumber, out byte[] zoneData)
    {
        zoneData = null;

        // Calculate the byte offset within the partition
        ulong byteOffset = (ulong)zoneNumber * _superblock.s_zone_size;

        // Convert to sector address
        ulong sectorAddress  = byteOffset / _imagePlugin.Info.SectorSize;
        var   offsetInSector = (int)(byteOffset % _imagePlugin.Info.SectorSize);

        // Calculate how many sectors to read
        uint sectorsToRead = (_superblock.s_zone_size + (uint)offsetInSector + _imagePlugin.Info.SectorSize - 1) /
                             _imagePlugin.Info.SectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + sectorAddress,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] sectorData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        zoneData = new byte[_superblock.s_zone_size];
        Array.Copy(sectorData, offsetInSector, zoneData, 0, _superblock.s_zone_size);

        return ErrorNumber.NoError;
    }
}