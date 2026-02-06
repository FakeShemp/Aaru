// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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
public sealed partial class ProDOSPlugin
{
    /// <summary>Reads a ProDOS block (512 bytes)</summary>
    ErrorNumber ReadBlock(ushort blockNumber, out byte[] data)
    {
        data = null;

        // Calculate sector address accounting for multiplier
        ulong sectorAddress = blockNumber * _multiplier + _partition.Start;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddress, false, _multiplier, out data, out _);

        return errno;
    }

    /// <summary>Counts the number of free blocks by reading the volume bitmap</summary>
    /// <returns>Number of free blocks</returns>
    ulong CountFreeBlocks()
    {
        ulong freeBlocks = 0;

        // Calculate number of bitmap blocks needed
        // Each bitmap block covers 4096 blocks (512 bytes * 8 bits per byte)
        var bitmapBlocksNeeded = (_totalBlocks + 4095) / 4096;

        for(var i = 0; i < bitmapBlocksNeeded; i++)
        {
            ErrorNumber errno = ReadBlock((ushort)(_bitmapBlock + i), out byte[] bitmapBlock);

            if(errno != ErrorNumber.NoError) continue;

            // Count free blocks in this bitmap block
            // In ProDOS bitmap: 1 = free, 0 = used
            for(var byteIndex = 0; byteIndex < 512; byteIndex++)
            {
                var blockBase = (uint)(i * 4096 + byteIndex * 8);

                for(var bit = 7; bit >= 0; bit--)
                {
                    uint blockNumber = blockBase + (uint)(7 - bit);

                    if(blockNumber >= _totalBlocks) break;

                    if((bitmapBlock[byteIndex] & 1 << bit) != 0) freeBlocks++;
                }
            }
        }

        return freeBlocks;
    }
}