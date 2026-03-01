// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Compression.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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

namespace Aaru.Filesystems;

public sealed partial class NTFS
{
    /// <summary>Size of an LZNT1 sub-block in bytes (4 KiB).</summary>
    const int LZNT1_SB_SIZE = 0x1000;

    /// <summary>Mask for the compressed data size field in a sub-block header.</summary>
    const ushort LZNT1_SB_SIZE_MASK = 0x0FFF;

    /// <summary>Flag indicating the sub-block is compressed.</summary>
    const ushort LZNT1_SB_IS_COMPRESSED = 0x8000;

    /// <summary>Decompresses LZNT1-compressed data from a full compression unit.</summary>
    /// <param name="compressedData">The raw data (compressed sub-blocks) from the compression unit on disk.</param>
    /// <param name="uncompressedSize">Expected size of the uncompressed output.</param>
    /// <returns>The decompressed data, or <c>null</c> if decompression fails.</returns>
    static byte[] DecompressLznt1(byte[] compressedData, int uncompressedSize)
    {
        var output    = new byte[uncompressedSize];
        var srcOffset = 0;
        var dstOffset = 0;

        while(srcOffset + 2 <= compressedData.Length && dstOffset < uncompressedSize)
        {
            var sbHeader = BitConverter.ToUInt16(compressedData, srcOffset);

            // A zero header signals end of compressed data
            if(sbHeader == 0) break;

            int sbDataSize = (sbHeader & LZNT1_SB_SIZE_MASK) + 3;

            srcOffset += 2;

            if(srcOffset + sbDataSize - 2 > compressedData.Length) break;

            if((sbHeader & LZNT1_SB_IS_COMPRESSED) == 0)
            {
                // Uncompressed sub-block: raw copy of LZNT1_SB_SIZE bytes
                int copyLen = Math.Min(LZNT1_SB_SIZE, uncompressedSize - dstOffset);
                copyLen = Math.Min(copyLen, sbDataSize - 2);

                Array.Copy(compressedData, srcOffset, output, dstOffset, copyLen);
                dstOffset += LZNT1_SB_SIZE;
                srcOffset += sbDataSize - 2;

                continue;
            }

            // Compressed sub-block
            int sbEnd      = srcOffset + sbDataSize - 2;
            int sbDstStart = dstOffset;
            int sbDstEnd   = Math.Min(dstOffset + LZNT1_SB_SIZE, uncompressedSize);

            while(srcOffset < sbEnd && dstOffset < sbDstEnd)
            {
                byte tag = compressedData[srcOffset++];

                for(var token = 0; token < 8 && srcOffset < sbEnd && dstOffset < sbDstEnd; token++, tag >>= 1)
                {
                    if((tag & 1) == 0)
                    {
                        // Symbol token: literal byte copy
                        output[dstOffset++] = compressedData[srcOffset++];
                    }
                    else
                    {
                        // Phrase token: back-reference
                        if(srcOffset + 2 > sbEnd) break;

                        var phraseToken = BitConverter.ToUInt16(compressedData, srcOffset);
                        srcOffset += 2;

                        // Calculate the number of displacement bits (lg) based on position in sub-block
                        int posInSb = dstOffset - sbDstStart;

                        // Cannot have a phrase token at position 0
                        if(posInSb == 0) return null;

                        var lg = 0;

                        for(int i = posInSb - 1; i >= 0x10; i >>= 1) lg++;

                        int displacement = (phraseToken >> 12 - lg)    + 1;
                        int length       = (phraseToken & 0xFFF >> lg) + 3;

                        // Validate back-reference
                        if(dstOffset - displacement < sbDstStart) return null;

                        // Copy bytes (may overlap, so byte-by-byte for overlapping regions)
                        int srcPos = dstOffset - displacement;

                        for(var i = 0; i < length && dstOffset < sbDstEnd; i++) output[dstOffset++] = output[srcPos++];
                    }
                }
            }

            // If the sub-block was not fully decompressed, zero-fill remainder
            if(dstOffset < sbDstEnd)
            {
                Array.Clear(output, dstOffset, sbDstEnd - dstOffset);
                dstOffset = sbDstEnd;
            }
        }

        // Zero-fill any remaining output
        if(dstOffset < uncompressedSize) Array.Clear(output, dstOffset, uncompressedSize - dstOffset);

        return output;
    }
}