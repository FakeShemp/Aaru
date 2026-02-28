// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Veritas File System plugin.
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
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS
{
    /// <summary>Reads filesystem blocks into a byte array</summary>
    /// <param name="block">Starting filesystem block number</param>
    /// <param name="count">Number of filesystem blocks to read</param>
    /// <returns>Byte array with read data, or null on error</returns>
    byte[] ReadBlocks(uint block, uint count)
    {
        int blockSize = _superblock.vs_bsize;

        long  byteOffset = block             * blockSize;
        ulong sectorOff  = (ulong)byteOffset / _imagePlugin.Info.SectorSize;
        var   subOff     = (uint)((ulong)byteOffset % _imagePlugin.Info.SectorSize);

        uint totalBytes    = count                                                    * (uint)blockSize;
        var  sectorsToRead = (subOff + totalBytes + _imagePlugin.Info.SectorSize - 1) / _imagePlugin.Info.SectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + sectorOff,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] data,
                                                     out _);

        if(errno != ErrorNumber.NoError) return null;

        if(subOff == 0 && data.Length == totalBytes) return data;

        if(subOff + totalBytes > data.Length) return null;

        var result = new byte[totalBytes];
        Array.Copy(data, subOff, result, 0, totalBytes);

        return result;
    }
}