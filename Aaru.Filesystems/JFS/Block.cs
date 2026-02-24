// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : IBM JFS filesystem plugin
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
/// <summary>Implements detection of IBM's Journaled File System</summary>
public sealed partial class JFS
{
    /// <summary>Reads raw bytes from the partition at a given byte offset</summary>
    /// <param name="byteOffset">Byte offset from the start of the partition</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="data">Output buffer</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadBytes(long byteOffset, int length, out byte[] data)
    {
        data = null;

        uint  sectorSize     = _imagePlugin.Info.SectorSize;
        ulong startSector    = _partition.Start + (ulong)(byteOffset / sectorSize);
        var   offsetInSector = (int)(byteOffset                                  % sectorSize);
        var   sectorsToRead  = (uint)((offsetInSector + length + sectorSize - 1) / sectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(startSector, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(offsetInSector + length > sectorData.Length) return ErrorNumber.InvalidArgument;

        data = new byte[length];
        Array.Copy(sectorData, offsetInSector, data, 0, length);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a filesystem block</summary>
    /// <param name="blockNumber">Physical block number</param>
    /// <param name="data">The block data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadFsBlock(long blockNumber, out byte[] data)
    {
        long byteOffset = blockNumber * _superblock.s_bsize;

        return ReadBytes(byteOffset, (int)_superblock.s_bsize, out data);
    }
}