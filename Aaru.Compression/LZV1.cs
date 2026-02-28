// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LZV1.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Compression algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the LZV1 decompression algorithm by Hermann Vogt.
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

/// <summary>Implements the LZV1 decompression algorithm (Hermann Vogt, used by e2compr)</summary>
public static class LZV1
{
    /// <summary>Set to <c>true</c> if this algorithm is supported, <c>false</c> otherwise.</summary>
    public static bool IsSupported => true;

    /// <summary>Decodes a buffer compressed with LZV1</summary>
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
            byte ctrl = source[srcPos++];

            int matchLen = ctrl >> 4;

            if(matchLen == 0)
            {
                // Literal run: copy (ctrl + 1) bytes
                int litLen = ctrl + 1;

                for(var i = 0; i < litLen && srcPos < srcLen && dstPos < dstLen; i++)
                    destination[dstPos++] = source[srcPos++];
            }
            else
            {
                // Match: length = high nibble + 1, offset encoded in low nibble + next byte
                if(srcPos >= srcLen) return dstPos;

                int offset = (ctrl & 0x0F) << 8 | source[srcPos++];
                offset++;

                matchLen++;

                if(offset > dstPos) return -1;

                int matchPos = dstPos - offset;

                for(var i = 0; i < matchLen && dstPos < dstLen; i++) destination[dstPos++] = destination[matchPos + i];
            }
        }

        return dstPos;
    }
}