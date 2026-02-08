// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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

/// <inheritdoc />
public sealed partial class MinixFS
{
    /// <summary>Reads a filesystem block</summary>
    /// <param name="blockNumber">Block number to read</param>
    /// <param name="data">The read block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlock(int blockNumber, out byte[] data)
    {
        data = null;

        if(blockNumber < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid block number: {0}", blockNumber);

            return ErrorNumber.InvalidArgument;
        }

        uint sectorSize = _imagePlugin.Info.SectorSize;

        // Calculate byte offset of the block
        long byteOffset = (long)blockNumber * _blockSize;

        // Calculate which sector contains this byte offset and the offset within that sector
        ulong sectorNumber   = (ulong)(byteOffset / sectorSize) + _partition.Start;
        var   offsetInSector = (int)(byteOffset % sectorSize);

        // Calculate how many sectors we need to read to get the full block
        var sectorsToRead = (uint)((offsetInSector + _blockSize + sectorSize - 1) / sectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading block {0}: {1}", blockNumber, errno);

            return errno;
        }

        // Extract the block data from the sector data
        if(offsetInSector == 0 && sectorData.Length == _blockSize)
            data = sectorData;
        else
        {
            data = new byte[_blockSize];
            Array.Copy(sectorData, offsetInSector, data, 0, Math.Min(_blockSize, sectorData.Length - offsetInSector));
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Maps a logical file block to a physical disk block (zone)</summary>
    /// <param name="zones">Zone pointers from the inode</param>
    /// <param name="directZones">Number of direct zone pointers</param>
    /// <param name="logicalBlock">Logical block number within the file</param>
    /// <param name="physicalBlock">Physical block number on disk (0 for sparse hole)</param>
    /// <returns>Error number indicating success or failure</returns>
    /// <remarks>
    ///     Based on read_map() in mfs/read.c:
    ///     - First directZones blocks are direct
    ///     - Next nr_indirects blocks are in single indirect
    ///     - Remaining blocks are in double indirect
    ///     For V1: directZones=7, nr_indirects=512 (1024/2)
    ///     For V2/V3: directZones=7, nr_indirects=256 or 1024 (blockSize/4)
    /// </remarks>
    ErrorNumber ReadMap(uint[] zones, int directZones, int logicalBlock, out int physicalBlock)
    {
        physicalBlock = 0;

        if(zones == null) return ErrorNumber.InvalidArgument;

        // Calculate zone size scale (log2 of blocks per zone, usually 0)
        int scale             = _logZoneSize;
        int zone              = logicalBlock >> scale;          // Position's zone
        int blockOffsetInZone = logicalBlock - (zone << scale); // Relative block within zone

        // Number of indirect pointers per block
        int pointerSize = _version == FilesystemVersion.V1 ? 2 : 4;
        int nrIndirects = _blockSize / pointerSize;

        // Is position in direct zones?
        if(zone < directZones)
        {
            uint z = zones[zone];

            if(z == 0) return ErrorNumber.NoError; // Sparse hole

            physicalBlock = (int)((z << scale) + blockOffsetInZone);

            return ErrorNumber.NoError;
        }

        // Not in direct zones - check single or double indirect
        int excess = zone - directZones;

        uint indirectZone;

        if(excess < nrIndirects)
        {
            // Single indirect block
            indirectZone = zones[directZones];
        }
        else
        {
            // Double indirect block
            if(directZones + 1 >= zones.Length) return ErrorNumber.InvalidArgument;

            uint doubleIndirectZone = zones[directZones + 1];

            if(doubleIndirectZone == 0) return ErrorNumber.NoError; // Sparse

            // Read double indirect block
            var doubleIndirectBlock = (int)(doubleIndirectZone << scale);

            ErrorNumber errno = ReadBlock(doubleIndirectBlock, out byte[] doubleIndirectData);

            if(errno != ErrorNumber.NoError) return errno;

            // Calculate index into double indirect block
            excess -= nrIndirects;
            int index = excess / nrIndirects;

            if(index >= nrIndirects) return ErrorNumber.InvalidArgument; // Beyond double indirect

            // Read zone pointer from double indirect block
            indirectZone = ReadZonePointer(doubleIndirectData, index);

            excess %= nrIndirects;
        }

        // Now indirectZone points to single indirect block, excess is index into it
        if(indirectZone == 0) return ErrorNumber.NoError; // Sparse

        var indirectBlock = (int)(indirectZone << scale);

        ErrorNumber err = ReadBlock(indirectBlock, out byte[] indirectData);

        if(err != ErrorNumber.NoError) return err;

        uint z2 = ReadZonePointer(indirectData, excess);

        if(z2 == 0) return ErrorNumber.NoError; // Sparse

        physicalBlock = (int)((z2 << scale) + blockOffsetInZone);

        return ErrorNumber.NoError;
    }
}