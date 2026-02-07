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

        uint sectorSize = _imagePlugin.Info.SectorSize;

        // Calculate byte offset of the basic block
        long byteOffset = blockNumber * EFS_BBSIZE;

        // Calculate which sector contains the start of this block and offset within it
        ulong sectorNumber   = (ulong)(byteOffset / sectorSize) + _partition.Start;
        var   offsetInSector = (int)(byteOffset % sectorSize);

        // Calculate how many sectors we need to read to get the full basic block
        var sectorsToRead = (uint)((offsetInSector + EFS_BBSIZE + sectorSize - 1) / sectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(offsetInSector + EFS_BBSIZE > sectorData.Length) return ErrorNumber.InvalidArgument;

        blockData = new byte[EFS_BBSIZE];
        Array.Copy(sectorData, offsetInSector, blockData, 0, EFS_BBSIZE);

        return ErrorNumber.NoError;
    }
}