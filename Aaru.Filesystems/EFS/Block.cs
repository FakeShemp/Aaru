// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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
public sealed partial class EFS
{
    /// <summary>Reads a basic block from disk</summary>
    /// <param name="blockNumber">Block number to read</param>
    /// <param name="blockData">The read block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBasicBlock(int blockNumber, out byte[] blockData)
    {
        blockData = null;

        // Handle optical disc alignment
        if(_imagePlugin.Info.MetadataMediaType == MetadataMediaType.OpticalDisc)
        {
            // On optical discs, basic blocks are at byte offsets within sectors
            long byteOffset     = blockNumber * EFS_BBSIZE;
            long sectorNumber   = byteOffset / _imagePlugin.Info.SectorSize + (long)_partition.Start;
            var  offsetInSector = (int)(byteOffset % _imagePlugin.Info.SectorSize);

            // Calculate how many sectors to read
            var sectorsToRead = (uint)((offsetInSector + EFS_BBSIZE + _imagePlugin.Info.SectorSize - 1) /
                                       _imagePlugin.Info.SectorSize);

            ErrorNumber errno =
                _imagePlugin.ReadSectors((ulong)sectorNumber, false, sectorsToRead, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            blockData = new byte[EFS_BBSIZE];

            if(offsetInSector + EFS_BBSIZE <= sectorData.Length)
                Array.Copy(sectorData, offsetInSector, blockData, 0, EFS_BBSIZE);
            else
                return ErrorNumber.InvalidArgument;
        }
        else
        {
            // Standard disk: basic blocks map directly to sectors (assuming 512-byte sectors)
            uint sectorsPerBb = EFS_BBSIZE / _imagePlugin.Info.SectorSize;

            if(sectorsPerBb == 0) sectorsPerBb = 1;

            ulong sectorNumber = _partition.Start + (ulong)blockNumber * sectorsPerBb;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(sectorNumber, false, sectorsPerBb, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            if(sectorData.Length >= EFS_BBSIZE)
            {
                blockData = new byte[EFS_BBSIZE];
                Array.Copy(sectorData, 0, blockData, 0, EFS_BBSIZE);
            }
            else
                blockData = sectorData;
        }

        return ErrorNumber.NoError;
    }
}

