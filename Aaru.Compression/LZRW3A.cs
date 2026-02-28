// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LZRW3A.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Compression algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the LZRW3A decompression algorithm by Ross Williams.
//     Used by the e2compr ext2 compression patches.
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

namespace Aaru.Compression;

/// <summary>Implements the LZRW3A decompression algorithm (Ross Williams, used by e2compr)</summary>
public static class LZRW3A
{
    /// <summary>Set to <c>true</c> if this algorithm is supported, <c>false</c> otherwise.</summary>
    public static bool IsSupported => true;

    /// <summary>Decodes a buffer compressed with LZRW3A</summary>
    /// <param name="source">Compressed buffer</param>
    /// <param name="destination">Buffer where to write the decoded data</param>
    /// <returns>The number of decoded bytes, or -1 on error</returns>
    public static int DecodeBuffer(byte[] source, byte[] destination)
    {
        if(source == null || destination == null) return -1;

        var srcPos = 0;
        var dstPos = 0;
        int srcLen = source.Length;
        int dstLen = destination.Length;

        while(srcPos < srcLen && dstPos < dstLen)
        {
            // Read flag byte — each bit controls one item (LSB first)
            if(srcPos >= srcLen) break;

            byte flags = source[srcPos++];

            for(var bit = 0; bit < 8 && srcPos < srcLen && dstPos < dstLen; bit++)
            {
                if((flags & 1 << bit) == 0)
                {
                    // Literal byte
                    destination[dstPos++] = source[srcPos++];
                }
                else
                {
                    // Match: 2-byte encoding
                    if(srcPos + 1 >= srcLen) return dstPos;

                    int word = source[srcPos] | source[srcPos + 1] << 8;
                    srcPos += 2;

                    int offset = (word >> 4)   + 1;
                    int length = (word & 0x0F) + 3;

                    if(offset > dstPos) return -1;

                    int matchPos = dstPos - offset;

                    for(var i = 0; i < length && dstPos < dstLen; i++)
                        destination[dstPos++] = destination[matchPos + i];
                }
            }
        }

        return dstPos;
    }
}