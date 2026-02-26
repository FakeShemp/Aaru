// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Bitmap.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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

public sealed partial class F2FS
{
    /// <summary>Tests a bit in a bitmap (little-endian bit order within each byte)</summary>
    static bool TestBit(uint bitNumber, byte[] bitmap)
    {
        if(bitmap == null) return false;

        uint byteIndex = bitNumber >> 3;

        if(byteIndex >= bitmap.Length) return false;

        var bitIndex = (int)(bitNumber & 7);

        return (bitmap[byteIndex] & 1 << bitIndex) != 0;
    }

    /// <summary>Finds the next set bit in a bitmap with little-endian bit order</summary>
    static int FindNextBitLE(byte[] data, int bitmapOffset, int max, int start)
    {
        for(int i = start; i < max; i++)
        {
            int byteIndex = bitmapOffset + (i >> 3);

            if(byteIndex >= data.Length) return max;

            int bitIndex = i & 7;

            if((data[byteIndex] & 1 << bitIndex) != 0) return i;
        }

        return max;
    }
}