// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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

/// <inheritdoc />
public sealed partial class AtheOS
{
    /// <summary>Reads a single block from disk</summary>
    /// <param name="blockNumber">The absolute block number to read</param>
    /// <param name="blockData">Output buffer containing the block data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadBlock(long blockNumber, out byte[] blockData)
    {
        blockData = null;
        uint sectorSize = _imagePlugin.Info.SectorSize;
        var  blockSize  = (int)_superblock.block_size;

        long blockByteAddr       = blockNumber            * blockSize;
        long partitionByteOffset = (long)_partition.Start * sectorSize;
        long absoluteByteAddr    = blockByteAddr + partitionByteOffset;
        long startingSector      = absoluteByteAddr / sectorSize;
        var  offsetInSector      = (int)(absoluteByteAddr % sectorSize);

        int sectorsToRead = (offsetInSector + blockSize + (int)sectorSize - 1) / (int)sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors((ulong)startingSector,
                                                     false,
                                                     (uint)sectorsToRead,
                                                     out byte[] sectorData,
                                                     out SectorStatus[] _);

        if(errno != ErrorNumber.NoError) return errno;

        blockData = new byte[blockSize];
        Array.Copy(sectorData, offsetInSector, blockData, 0, blockSize);

        return ErrorNumber.NoError;
    }
}