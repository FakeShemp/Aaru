// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser filesystem plugin
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

public sealed partial class Reiser
{
    /// <summary>Reads a filesystem block by its block number</summary>
    /// <param name="blockNumber">Block number to read</param>
    /// <param name="data">The read block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlock(uint blockNumber, out byte[] data)
    {
        data = null;

        if(_blockCache != null && _blockCache.TryGetValue(blockNumber, out data)) return ErrorNumber.NoError;

        uint sectorSize = _imagePlugin.Info.SectorSize;

        // Calculate byte offset of the block
        long byteOffset = blockNumber * _blockSize;

        // Calculate which sector contains this byte offset
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

        // Cache the block (limit cache to avoid excessive memory use)
        if(_blockCache is { Count: < 4096 }) _blockCache[blockNumber] = data;

        return ErrorNumber.NoError;
    }
}