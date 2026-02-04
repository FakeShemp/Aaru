// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Crc32Be.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX6
{
    /// <summary>CRC32-BE polynomial (used by Linux crc32_be)</summary>
    const uint CRC32_BE_POLY = 0x04C11DB7;

    /// <summary>Pre-computed CRC32-BE lookup table</summary>
    static readonly uint[] Crc32BeTable;

    /// <summary>Static constructor to initialize CRC32-BE table</summary>
    static QNX6()
    {
        Crc32BeTable = new uint[256];

        for(uint i = 0; i < 256; i++)
        {
            uint crc = i << 24;

            for(var j = 0; j < 8; j++)
            {
                if((crc & 0x80000000) != 0)
                    crc = crc << 1 ^ CRC32_BE_POLY;
                else
                    crc <<= 1;
            }

            Crc32BeTable[i] = crc;
        }
    }

    /// <summary>Calculates a CRC32-BE checksum (as used by Linux crc32_be)</summary>
    /// <param name="data">Data buffer</param>
    /// <param name="offset">Offset into buffer</param>
    /// <param name="length">Length of data to checksum</param>
    /// <returns>CRC32-BE checksum</returns>
    static uint CalculateCrc32Be(byte[] data, int offset, int length)
    {
        uint crc = 0; // Linux crc32_be starts with seed 0

        for(var i = 0; i < length; i++)
        {
            byte b     = data[offset + i];
            var  index = (byte)(crc >> 24 ^ b);
            crc = crc << 8 ^ Crc32BeTable[index];
        }

        return crc;
    }
}