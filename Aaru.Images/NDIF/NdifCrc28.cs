// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : NdifCrc28.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 28-bit CRC used to protect Apple New Disk Image Format
//     images.
//
//     Reverse-engineered from the 68k code fragment (`oneb` 128) embedded in
//     the Apple DiskCopy 6.3.3 resource fork (exports `.CRC28_Initialize`,
//     `.CRC28_Start`, `.CRC28_ProcessBuffer`, `.CRC28_Finish`):
//
//         table[i] = i
//         for each of 8 bits: if (table[i] &amp; 1)
//                                 table[i] = (table[i] >> 1) ^ 0x04C11DB7
//                             else
//                                 table[i] >>= 1
//
//         crc = 0xFFFFFFFF
//         for each byte b of the decoded sector stream:
//             crc = (crc >> 8) ^ table[(crc ^ b) &amp; 0xFF]
//
//     CRC28_Finish is a no-op; the low 28 bits of the accumulator are what the
//     NDIF `bcem` header stores in its `crc` field.
//
//     Buffer contents are the reconstructed 512-byte sectors of the image, in
//     image order; `CHUNK_TYPE_NOCOPY` (and otherwise unwritten) sectors feed
//     in as zeros.
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

namespace Aaru.Images;

/// <summary>Table-driven implementation of Apple NDIF's 28-bit CRC.</summary>
static class NdifCrc28
{
    const uint POLYNOMIAL = 0x04C11DB7;
    const uint SEED       = 0xFFFFFFFF;
    const uint MASK_28    = 0x0FFFFFFF;

    static readonly uint[] _table = BuildTable();

    static uint[] BuildTable()
    {
        var table = new uint[256];

        for(uint i = 0; i < 256; i++)
        {
            uint c = i;

            for(var j = 0; j < 8; j++)
            {
                if((c & 1) != 0)
                    c = c >> 1 ^ POLYNOMIAL;
                else
                    c >>= 1;
            }

            table[i] = c;
        }

        return table;
    }

    /// <summary>Returns the initial CRC accumulator seed.</summary>
    public static uint Init() => SEED;

    /// <summary>
    ///     Feeds <paramref name="length" /> bytes from <paramref name="buffer" /> into the running
    ///     <paramref name="crc" />.
    /// </summary>
    public static uint Update(uint crc, byte[] buffer, int length)
    {
        for(var i = 0; i < length; i++) crc = crc >> 8 ^ _table[(crc ^ buffer[i]) & 0xFF];

        return crc;
    }

    /// <summary>Applies the final 28-bit mask to <paramref name="crc" />, yielding the value stored in the NDIF header.</summary>
    public static uint Finish(uint crc) => crc & MASK_28;
}