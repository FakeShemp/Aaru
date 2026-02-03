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
using Aaru.Logging;

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

    /// <summary>Maps a logical zone number to a physical zone number</summary>
    /// <remarks>
    ///     Implements the xiafs_bmap logic from Linux:
    ///     - Zones 0-7: direct zones (i_zone[0-7])
    ///     - Zone 8+: indirect zone (i_zone[8] points to zone with addresses)
    ///     - Higher zones: double indirect (i_zone[9])
    ///     Zone addresses are stored in 24 bits (mask with 0x00FFFFFF)
    /// </remarks>
    /// <param name="inode">The file inode</param>
    /// <param name="logicalZone">The logical zone number</param>
    /// <param name="physicalZone">The physical zone number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber MapZone(in Inode inode, uint logicalZone, out uint physicalZone)
    {
        physicalZone = 0;

        uint addressesPerZone = _superblock.s_zone_size / 4; // 4 bytes per zone pointer

        // Check bounds: max zones = 8 + (1 + addressesPerZone) * addressesPerZone
        uint maxZones = 8 + (1 + addressesPerZone) * addressesPerZone;

        if(logicalZone >= maxZones)
        {
            AaruLogging.Debug(MODULE_NAME, "MapZone: zone {0} exceeds max {1}", logicalZone, maxZones);

            return ErrorNumber.InvalidArgument;
        }

        // Direct zones (0-7)
        if(logicalZone < 8)
        {
            physicalZone = inode.i_zone[logicalZone] & 0x00FFFFFF;

            return ErrorNumber.NoError;
        }

        uint zone = logicalZone - 8;

        // Single indirect zone
        if(zone < addressesPerZone)
        {
            uint indirectZone = inode.i_zone[8] & 0x00FFFFFF;

            if(indirectZone == 0)
            {
                physicalZone = 0; // Sparse

                return ErrorNumber.NoError;
            }

            ErrorNumber errno = ReadZone(indirectZone, out byte[] indirectData);

            if(errno != ErrorNumber.NoError) return errno;

            physicalZone = BitConverter.ToUInt32(indirectData, (int)(zone * 4)) & 0x00FFFFFF;

            return ErrorNumber.NoError;
        }

        zone -= addressesPerZone;

        // Double indirect zone
        uint dindirectZone = inode.i_zone[9] & 0x00FFFFFF;

        if(dindirectZone == 0)
        {
            physicalZone = 0; // Sparse

            return ErrorNumber.NoError;
        }

        // Read first level of indirection
        ErrorNumber err = ReadZone(dindirectZone, out byte[] dindirectData);

        if(err != ErrorNumber.NoError) return err;

        uint indirectIndex = zone / addressesPerZone;
        uint indirectAddr  = BitConverter.ToUInt32(dindirectData, (int)(indirectIndex * 4)) & 0x00FFFFFF;

        if(indirectAddr == 0)
        {
            physicalZone = 0; // Sparse

            return ErrorNumber.NoError;
        }

        // Read second level of indirection
        err = ReadZone(indirectAddr, out byte[] indirectData2);

        if(err != ErrorNumber.NoError) return err;

        uint zoneIndex = zone % addressesPerZone;
        physicalZone = BitConverter.ToUInt32(indirectData2, (int)(zoneIndex * 4)) & 0x00FFFFFF;

        return ErrorNumber.NoError;
    }
}