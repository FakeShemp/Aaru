// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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
public sealed partial class Cram
{
    /// <summary>Reads bytes from the filesystem</summary>
    /// <param name="offset">Byte offset from start of filesystem</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="data">Output data buffer</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBytes(uint offset, uint length, out byte[] data)
    {
        data = null;

        // Add base offset
        uint actualOffset = _baseOffset + offset;

        // Calculate sector and offset
        uint sectorSize     = _imagePlugin.Info.SectorSize;
        uint startSector    = actualOffset / sectorSize;
        var  offsetInSector = (int)(actualOffset                                % sectorSize);
        var  sectorsToRead  = (uint)((offsetInSector + length + sectorSize - 1) / sectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + startSector,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] sectorData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        data = new byte[length];

        if(offsetInSector + length <= sectorData.Length)
            Array.Copy(sectorData, offsetInSector, data, 0, length);
        else
            Array.Copy(sectorData, offsetInSector, data, 0, sectorData.Length - offsetInSector);

        return ErrorNumber.NoError;
    }
}