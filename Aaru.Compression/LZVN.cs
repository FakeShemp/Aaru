// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LZVN.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Compression algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the LZVN compression algorithm based on Apple's reference
//     implementation from lzvn_decode_base.c
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
// Based on Apple's LZVN reference implementation
// ****************************************************************************/

using System;

namespace Aaru.Compression;

// ReSharper disable once InconsistentNaming
/// <summary>Implements the LZVN compression algorithm (Apple's LZVN)</summary>
public static class LZVN
{
    /// <summary>Decodes a buffer compressed with LZVN</summary>
    /// <param name="source">Compressed buffer</param>
    /// <param name="destination">Buffer where to write the decoded data</param>
    /// <returns>The number of decoded bytes, or -1 on error</returns>
    public static int DecodeBuffer(byte[] source, byte[] destination)
    {
        if(source == null || destination == null) return -1;

        var srcPos    = 0;
        int srcEnd    = source.Length;
        var dstPos    = 0;
        int dstEnd    = destination.Length;
        var dstBegin  = 0;
        var matchDist = 0; // Previous match distance (d_prev)

        while(srcPos < srcEnd && dstPos < dstEnd)
        {
            byte opc = source[srcPos];

            int literalLen;
            int matchLen;
            int opcLen;

            // Decode based on opcode ranges from Apple's implementation
            if(opc >= 224)
            {
                // Literal only opcodes: 224-239 (sml_l and lrg_l)
                if(opc == 224)
                {
                    // lrg_l: Large literal
                    opcLen = 2;

                    if(srcPos + opcLen > srcEnd) return -1;

                    literalLen = source[srcPos + 1] + 16;

                    if(srcPos + opcLen + literalLen > srcEnd || dstPos + literalLen > dstEnd) return -1;

                    Array.Copy(source, srcPos + opcLen, destination, dstPos, literalLen);
                    srcPos += opcLen + literalLen;
                    dstPos += literalLen;
                }
                else if(opc >= 225 && opc <= 239)
                {
                    // sml_l: Small literal (1110LLLL)
                    opcLen     = 1;
                    literalLen = opc & 0x0F;

                    if(srcPos + opcLen + literalLen > srcEnd || dstPos + literalLen > dstEnd) return -1;

                    Array.Copy(source, srcPos + opcLen, destination, dstPos, literalLen);
                    srcPos += opcLen + literalLen;
                    dstPos += literalLen;
                }
                else if(opc == 240)
                {
                    // lrg_m: Large match (11110000 MMMMMMMM)
                    opcLen = 2;

                    if(srcPos + opcLen > srcEnd) return -1;

                    matchLen = source[srcPos + 1] + 16;

                    if(dstPos < matchDist || matchDist == 0 || dstPos + matchLen > dstEnd) return -1;

                    CopyMatch(destination, dstPos, matchDist, matchLen);
                    srcPos += opcLen;
                    dstPos += matchLen;
                }
                else // opc >= 241 && opc <= 255
                {
                    // sml_m: Small match (1111MMMM)
                    opcLen   = 1;
                    matchLen = opc & 0x0F;

                    if(srcPos + opcLen > srcEnd) return -1;

                    if(dstPos < matchDist || matchDist == 0 || dstPos + matchLen > dstEnd) return -1;

                    CopyMatch(destination, dstPos, matchDist, matchLen);
                    srcPos += opcLen;
                    dstPos += matchLen;
                }
            }
            else if(opc >= 160 && opc <= 191)
            {
                // med_d: Medium distance (101LLMMM DDDDDDMM DDDDDDDD)
                opcLen = 3;

                if(srcPos + opcLen > srcEnd) return -1;

                literalLen = opc >> 3 & 0x03;
                matchLen   = ((opc & 0x07) << 2 | source[srcPos + 1] & 0x03) + 3;
                matchDist  = source[srcPos + 1] >> 2 | source[srcPos + 2] << 6;

                if(srcPos + opcLen + literalLen > srcEnd) return -1;

                // Copy literal
                if(literalLen > 0)
                {
                    if(dstPos + literalLen > dstEnd) return -1;

                    Array.Copy(source, srcPos + opcLen, destination, dstPos, literalLen);
                    dstPos += literalLen;
                }

                srcPos += opcLen + literalLen;

                // Copy match
                if(dstPos < matchDist || matchDist == 0 || dstPos + matchLen > dstEnd) return -1;

                CopyMatch(destination, dstPos, matchDist, matchLen);
                dstPos += matchLen;
            }
            else if(opc == 6)
            {
                // eos: End of stream
                return dstPos;
            }
            else if((opc & 0x07) == 0x06)
            {
                // pre_d: Previous distance (LLMMM110)
                opcLen     = 1;
                literalLen = opc >> 6 & 0x03;
                matchLen   = (opc >> 3 & 0x07) + 3;

                if(srcPos + opcLen + literalLen > srcEnd) return -1;

                // Copy literal
                if(literalLen > 0)
                {
                    if(dstPos + literalLen > dstEnd) return -1;

                    Array.Copy(source, srcPos + opcLen, destination, dstPos, literalLen);
                    dstPos += literalLen;
                }

                srcPos += opcLen + literalLen;

                // Copy match using previous distance
                if(dstPos < matchDist || matchDist == 0 || dstPos + matchLen > dstEnd) return -1;

                CopyMatch(destination, dstPos, matchDist, matchLen);
                dstPos += matchLen;
            }
            else if((opc & 0x07) == 0x07)
            {
                // lrg_d: Large distance (LLMMM111 DDDDDDDD DDDDDDDD)
                opcLen     = 3;
                literalLen = opc >> 6 & 0x03;
                matchLen   = (opc >> 3 & 0x07) + 3;

                if(srcPos + opcLen + literalLen > srcEnd) return -1;

                matchDist = source[srcPos + 1] | source[srcPos + 2] << 8;

                // Copy literal
                if(literalLen > 0)
                {
                    if(dstPos + literalLen > dstEnd) return -1;

                    Array.Copy(source, srcPos + opcLen, destination, dstPos, literalLen);
                    dstPos += literalLen;
                }

                srcPos += opcLen + literalLen;

                // Copy match
                if(dstPos < matchDist || matchDist == 0 || dstPos + matchLen > dstEnd) return -1;

                CopyMatch(destination, dstPos, matchDist, matchLen);
                dstPos += matchLen;
            }
            else
            {
                // sml_d: Small distance (LLMMMDDD DDDDDDDD)
                opcLen     = 2;
                literalLen = opc >> 6 & 0x03;
                matchLen   = (opc >> 3 & 0x07) + 3;

                if(srcPos + opcLen + literalLen > srcEnd) return -1;

                matchDist = (opc & 0x07) << 8 | source[srcPos + 1];

                // Copy literal
                if(literalLen > 0)
                {
                    if(dstPos + literalLen > dstEnd) return -1;

                    Array.Copy(source, srcPos + opcLen, destination, dstPos, literalLen);
                    dstPos += literalLen;
                }

                srcPos += opcLen + literalLen;

                // Copy match
                if(dstPos < matchDist || matchDist == 0 || dstPos + matchLen > dstEnd) return -1;

                CopyMatch(destination, dstPos, matchDist, matchLen);
                dstPos += matchLen;
            }
        }

        return dstPos;
    }

    /// <summary>Copies a match (LZ77 backreference)</summary>
    /// <param name="buffer">Destination buffer</param>
    /// <param name="dstPos">Current position in destination</param>
    /// <param name="distance">Distance to copy from (0-based offset backwards)</param>
    /// <param name="length">Number of bytes to copy</param>
    private static void CopyMatch(byte[] buffer, int dstPos, int distance, int length)
    {
        int srcPos = dstPos - distance;

        // Handle overlapping copies (distance < length) byte-by-byte
        // This is important for patterns like RLE where distance = 1
        for(var i = 0; i < length; i++) buffer[dstPos + i] = buffer[srcPos + i];
    }
}