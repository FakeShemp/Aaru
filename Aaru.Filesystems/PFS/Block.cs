// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Professional File System plugin.
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

using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class PFS
{
    /// <summary>Reads a reserved block by block number</summary>
    /// <param name="blockNumber">The reserved block number</param>
    /// <param name="data">The block data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadReservedBlock(uint blockNumber, out byte[] data)
    {
        data = null;

        // Reserved blocks are stored starting at firstreserved
        // Each reserved block can span multiple sectors
        uint sectorsPerReservedBlock = _reservedBlockSize / _imagePlugin.Info.SectorSize;

        if(sectorsPerReservedBlock == 0) sectorsPerReservedBlock = 1;

        ulong sectorAddress = _partition.Start + blockNumber;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddress, false, sectorsPerReservedBlock, out data, out _);

        return errno;
    }

    /// <summary>Reads a data block by block number</summary>
    /// <param name="blockNumber">The block number</param>
    /// <param name="data">The block data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadBlock(uint blockNumber, out byte[] data)
    {
        data = null;

        ulong sectorAddress = _partition.Start + blockNumber;

        return _imagePlugin.ReadSector(sectorAddress, false, out data, out _);
    }
}