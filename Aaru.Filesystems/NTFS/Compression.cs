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

    /// <summary>
    ///     Decompresses a single Xpress (LZ77) compressed frame.
    ///     Implements the Microsoft Xpress Compression Algorithm (MS-XCA 2.3, plain LZ77).
    /// </summary>
    /// <param name="compressedData">The compressed frame data.</param>
    /// <param name="uncompressedSize">Expected size of the decompressed output.</param>
    /// <returns>The decompressed data, or <c>null</c> if decompression fails.</returns>
    static byte[] DecompressXpress(byte[] compressedData, int uncompressedSize)
    {
        var output    = new byte[uncompressedSize];
        var srcOffset = 0;
        var dstOffset = 0;

        while(dstOffset < uncompressedSize)
        {
            // Read 32-bit flags word
            if(srcOffset + 4 > compressedData.Length) break;

            var flags = BitConverter.ToUInt32(compressedData, srcOffset);
            srcOffset += 4;

            // Process 32 bits, LSB first
            for(var bit = 0; bit < 32 && dstOffset < uncompressedSize; bit++)
            {
                if((flags & 1u << bit) == 0)
                {
                    // Literal byte
                    if(srcOffset >= compressedData.Length) return output;

                    output[dstOffset++] = compressedData[srcOffset++];
                }
                else
                {
                    // Match reference
                    if(srcOffset + 2 > compressedData.Length) return output;

                    var matchValue = BitConverter.ToUInt16(compressedData, srcOffset);
                    srcOffset += 2;

                    int matchOffset = (matchValue >> 3) + 1;
                    int matchLength = (matchValue & 7)  + 3;

                    // Extended length encoding
                    if((matchValue & 7) == 7)
                    {
                        if(srcOffset >= compressedData.Length) return output;

                        byte extraLength = compressedData[srcOffset++];

                        if(extraLength == 255)
                        {
                            // Read 16-bit length
                            if(srcOffset + 2 > compressedData.Length) return output;

                            matchLength =  BitConverter.ToUInt16(compressedData, srcOffset);
                            srcOffset   += 2;

                            // If the 16-bit length is 0, read 32-bit length
                            if(matchLength == 0)
                            {
                                if(srcOffset + 4 > compressedData.Length) return output;

                                matchLength =  (int)BitConverter.ToUInt32(compressedData, srcOffset);
                                srcOffset   += 4;
                            }
                        }
                        else
                            matchLength = extraLength + 7 + 3;
                    }

                    // Validate back-reference
                    if(dstOffset - matchOffset < 0) return null;

                    // Copy bytes (byte-by-byte for overlapping regions)
                    int srcPos = dstOffset - matchOffset;

                    for(var i = 0; i < matchLength && dstOffset < uncompressedSize; i++)
                        output[dstOffset++] = output[srcPos++];
                }
            }
        }

        return output;
    }

    /// <summary>
    ///     Decompresses a single LZX compressed frame (32 KB window, as used by WOF).
    ///     Implements a subset of the Microsoft LZX algorithm (MS-PATCH / Cabinet LZX)
    ///     sufficient for Windows Overlay Filter decompression.
    /// </summary>
    /// <param name="compressedData">The compressed frame data.</param>
    /// <param name="uncompressedSize">Expected size of the decompressed output (max 32768).</param>
    /// <returns>The decompressed data, or <c>null</c> if decompression fails.</returns>
    static byte[] DecompressLzx(byte[] compressedData, int uncompressedSize)
    {
        var output = new byte[uncompressedSize];

        var state = new LzxState
        {
            Input       = compressedData,
            BitBuffer   = 0,
            BitsLeft    = 0,
            InputOffset = 0,
            Output      = output,
            OutputPos   = 0
        };

        // LZX for WOF uses a 32KB window → 8 position slots (num_position_slots)
        const int NUM_POSITION_SLOTS = 8;
        const int MAIN_TREE_SIZE     = 256 + (NUM_POSITION_SLOTS << 3); // 256 + 64 = 320
        const int LENGTH_TREE_SIZE   = 249;
        const int ALIGNED_TREE_SIZE  = 8;
        const int PRE_TREE_SIZE      = 20;

        // Persistent trees across blocks within a chunk
        var mainLengths   = new int[MAIN_TREE_SIZE];
        var lengthLengths = new int[LENGTH_TREE_SIZE];

        // Position base and extra bits tables (for 8 position slots → 16 entries)
        var positionBase = new int[16];
        var extraBits    = new int[16];

        for(var i = 0; i < 16; i++)
        {
            positionBase[i] = i < 2 ? i : 1 << (i >> 1) - 1;
            extraBits[i]    = i < 2 ? 0 : (i >> 1) - 1;

            if(i >= 2) positionBase[i] += positionBase[i - 1]; // cumulative? No.
        }

        // Correct position base table (non-cumulative)
        positionBase[0]  = 0;
        positionBase[1]  = 1;
        positionBase[2]  = 2;
        positionBase[3]  = 3;
        positionBase[4]  = 4;
        positionBase[5]  = 6;
        positionBase[6]  = 8;
        positionBase[7]  = 12;
        positionBase[8]  = 16;
        positionBase[9]  = 24;
        positionBase[10] = 32;
        positionBase[11] = 48;
        positionBase[12] = 64;
        positionBase[13] = 96;
        positionBase[14] = 128;
        positionBase[15] = 192;

        extraBits[0]  = 0;
        extraBits[1]  = 0;
        extraBits[2]  = 0;
        extraBits[3]  = 0;
        extraBits[4]  = 1;
        extraBits[5]  = 1;
        extraBits[6]  = 2;
        extraBits[7]  = 2;
        extraBits[8]  = 3;
        extraBits[9]  = 3;
        extraBits[10] = 4;
        extraBits[11] = 4;
        extraBits[12] = 5;
        extraBits[13] = 5;
        extraBits[14] = 6;
        extraBits[15] = 6;

        while(state.OutputPos < uncompressedSize)
        {
            // Read block type (3 bits) and block size (24 bits)
            int blockType = LzxReadBits(ref state, 3);

            if(blockType == 0) break; // invalid

            int blockSize;

            // Check if default block size
            if(LzxReadBits(ref state, 1) == 1)
                blockSize = 32768;
            else
                blockSize = LzxReadBits(ref state, 16);

            if(blockSize == 0) break;

            int blockEnd = Math.Min(state.OutputPos + blockSize, uncompressedSize);

            int[] alignedLengths;

            switch(blockType)
            {
                case 1: // Verbatim block
                    // Read pre-tree for main tree (first 256 elements)
                    LzxReadPreTreeAndLengths(ref state, mainLengths, 0, 256, PRE_TREE_SIZE);

                    // Read pre-tree for main tree (remaining elements)
                    LzxReadPreTreeAndLengths(ref state, mainLengths, 256, MAIN_TREE_SIZE, PRE_TREE_SIZE);

                    // Read pre-tree for length tree
                    LzxReadPreTreeAndLengths(ref state, lengthLengths, 0, LENGTH_TREE_SIZE, PRE_TREE_SIZE);

                    // Build Huffman tables
                    ushort[] mainTable = LzxBuildHuffmanTable(mainLengths, MAIN_TREE_SIZE, 12);

                    if(mainTable == null) return null;

                    ushort[] lengthTable = LzxBuildHuffmanTable(lengthLengths, LENGTH_TREE_SIZE, 12);

                    if(lengthTable == null) return null;

                    // Decode symbols
                    while(state.OutputPos < blockEnd)
                    {
                        int sym = LzxDecodeSymbol(ref state, mainTable, mainLengths, MAIN_TREE_SIZE, 12);

                        if(sym < 0) return null;

                        if(sym < 256)
                            output[state.OutputPos++] = (byte)sym;
                        else
                        {
                            // Match: sym = 256 + (position_slot * 8) + length_header
                            sym -= 256;
                            int positionSlot = sym >> 3;
                            int lengthHeader = sym & 7;

                            int matchLength = lengthHeader + 2;

                            if(lengthHeader == 7)
                            {
                                int extraLen =
                                    LzxDecodeSymbol(ref state, lengthTable, lengthLengths, LENGTH_TREE_SIZE, 12);

                                if(extraLen < 0) return null;

                                matchLength += extraLen;
                            }

                            int matchOffset;

                            if(positionSlot < 2)
                            {
                                // Position slots 0 and 1 are special (recent offsets)
                                // For simplicity in WOF context, use position_base directly
                                matchOffset = positionBase[positionSlot];
                            }
                            else
                            {
                                int extra = extraBits[positionSlot];

                                matchOffset = positionBase[positionSlot];

                                if(extra > 0) matchOffset += LzxReadBits(ref state, extra);
                            }

                            // Copy match
                            int srcPos = state.OutputPos - matchOffset - 1;

                            if(srcPos < 0)
                            {
                                // Reference before start of output — zero fill
                                for(var i = 0; i < matchLength && state.OutputPos < blockEnd; i++)
                                    output[state.OutputPos++] = 0;
                            }
                            else
                            {
                                for(var i = 0; i < matchLength && state.OutputPos < blockEnd; i++)
                                    output[state.OutputPos++] = output[srcPos++];
                            }
                        }
                    }

                    break;

                case 2: // Aligned offset block
                    // Read aligned offset tree (8 elements, 3 bits each)
                    alignedLengths = new int[ALIGNED_TREE_SIZE];

                    for(var i = 0; i < ALIGNED_TREE_SIZE; i++) alignedLengths[i] = LzxReadBits(ref state, 3);

                    ushort[] alignedTable = LzxBuildHuffmanTable(alignedLengths, ALIGNED_TREE_SIZE, 7);

                    if(alignedTable == null) return null;

                    // Read trees same as verbatim
                    LzxReadPreTreeAndLengths(ref state, mainLengths,   0,   256,              PRE_TREE_SIZE);
                    LzxReadPreTreeAndLengths(ref state, mainLengths,   256, MAIN_TREE_SIZE,   PRE_TREE_SIZE);
                    LzxReadPreTreeAndLengths(ref state, lengthLengths, 0,   LENGTH_TREE_SIZE, PRE_TREE_SIZE);

                    ushort[] mainTable2 = LzxBuildHuffmanTable(mainLengths, MAIN_TREE_SIZE, 12);

                    if(mainTable2 == null) return null;

                    ushort[] lengthTable2 = LzxBuildHuffmanTable(lengthLengths, LENGTH_TREE_SIZE, 12);

                    if(lengthTable2 == null) return null;

                    while(state.OutputPos < blockEnd)
                    {
                        int sym = LzxDecodeSymbol(ref state, mainTable2, mainLengths, MAIN_TREE_SIZE, 12);

                        if(sym < 0) return null;

                        if(sym < 256)
                            output[state.OutputPos++] = (byte)sym;
                        else
                        {
                            sym -= 256;
                            int positionSlot = sym >> 3;
                            int lengthHeader = sym & 7;

                            int matchLength = lengthHeader + 2;

                            if(lengthHeader == 7)
                            {
                                int extraLen =
                                    LzxDecodeSymbol(ref state, lengthTable2, lengthLengths, LENGTH_TREE_SIZE, 12);

                                if(extraLen < 0) return null;

                                matchLength += extraLen;
                            }

                            int matchOffset;

                            if(positionSlot < 2)
                                matchOffset = positionBase[positionSlot];
                            else
                            {
                                int extra = extraBits[positionSlot];

                                matchOffset = positionBase[positionSlot];

                                if(extra >= 3)
                                {
                                    // Use aligned offset tree for the low 3 bits
                                    int verbatimBits = LzxReadBits(ref state, extra - 3);

                                    matchOffset += verbatimBits << 3;

                                    int alignedBits =
                                        LzxDecodeSymbol(ref state, alignedTable, alignedLengths, ALIGNED_TREE_SIZE, 7);

                                    if(alignedBits < 0) return null;

                                    matchOffset += alignedBits;
                                }
                                else if(extra > 0) matchOffset += LzxReadBits(ref state, extra);
                            }

                            int srcPos = state.OutputPos - matchOffset - 1;

                            if(srcPos < 0)
                            {
                                for(var i = 0; i < matchLength && state.OutputPos < blockEnd; i++)
                                    output[state.OutputPos++] = 0;
                            }
                            else
                            {
                                for(var i = 0; i < matchLength && state.OutputPos < blockEnd; i++)
                                    output[state.OutputPos++] = output[srcPos++];
                            }
                        }
                    }

                    break;

                case 3: // Uncompressed block
                    // Align to 16-bit boundary
                    if(state.BitsLeft > 0)
                    {
                        state.BitsLeft  = 0;
                        state.BitBuffer = 0;
                    }

                    // Ensure input is aligned to 16-bit boundary
                    if((state.InputOffset & 1) != 0) state.InputOffset++;

                    // Copy raw bytes
                    int copyLen2 = Math.Min(blockSize, uncompressedSize - state.OutputPos);
                    copyLen2 = Math.Min(copyLen2, compressedData.Length - state.InputOffset);

                    if(copyLen2 > 0)
                    {
                        Array.Copy(compressedData, state.InputOffset, output, state.OutputPos, copyLen2);
                        state.InputOffset += copyLen2;
                        state.OutputPos   += copyLen2;
                    }

                    // Re-align to 16-bit boundary after uncompressed block
                    if((state.InputOffset & 1) != 0) state.InputOffset++;

                    break;

                default:
                    return null;
            }
        }

        return output;
    }

    /// <summary>Reads bits from the LZX bitstream (MSB-first, 16-bit word-aligned input).</summary>
    static int LzxReadBits(ref LzxState state, int count)
    {
        while(state.BitsLeft < count)
        {
            if(state.InputOffset + 2 > state.Input.Length) return 0;

            // LZX uses 16-bit little-endian words, read into the high bits of the buffer
            var word = BitConverter.ToUInt16(state.Input, state.InputOffset);
            state.InputOffset += 2;
            state.BitBuffer   |= (uint)word << 16 - state.BitsLeft;
            state.BitsLeft    += 16;
        }

        var result = (int)(state.BitBuffer >> 32 - count);
        state.BitBuffer <<= count;
        state.BitsLeft  -=  count;

        return result;
    }

    /// <summary>Reads a pre-tree (20 elements) and uses it to decode code lengths for another tree.</summary>
    static void LzxReadPreTreeAndLengths(ref LzxState state, int[] lengths, int start, int end, int preTreeSize)
    {
        var preTreeLengths = new int[preTreeSize];

        for(var i = 0; i < preTreeSize; i++) preTreeLengths[i] = LzxReadBits(ref state, 4);

        ushort[] preTable = LzxBuildHuffmanTable(preTreeLengths, preTreeSize, 6);

        if(preTable == null) return;

        int pos = start;

        while(pos < end)
        {
            int sym = LzxDecodeSymbol(ref state, preTable, preTreeLengths, preTreeSize, 6);

            if(sym < 0) return;

            if(sym <= 16)
            {
                // Delta code length
                lengths[pos] = (lengths[pos] - sym + 17) % 17;
                pos++;
            }
            else if(sym == 17)
            {
                // Run of zeros (4 + extra 4 bits)
                int runLength = LzxReadBits(ref state, 4) + 4;

                for(var i = 0; i < runLength && pos < end; i++) lengths[pos++] = 0;
            }
            else if(sym == 18)
            {
                // Longer run of zeros (20 + extra 5 bits)
                int runLength = LzxReadBits(ref state, 5) + 20;

                for(var i = 0; i < runLength && pos < end; i++) lengths[pos++] = 0;
            }
            else if(sym == 19)
            {
                // Run of same value (1 + extra 1 bit) times, followed by a delta
                int runLength = LzxReadBits(ref state, 1) + 4;
                int nextSym   = LzxDecodeSymbol(ref state, preTable, preTreeLengths, preTreeSize, 6);

                if(nextSym < 0) return;

                int newLen = (lengths[pos] - nextSym + 17) % 17;

                for(var i = 0; i < runLength && pos < end; i++) lengths[pos++] = newLen;
            }
        }
    }

    /// <summary>Builds a canonical Huffman decode table from code lengths.</summary>
    /// <param name="lengths">Array of code lengths for each symbol.</param>
    /// <param name="numSymbols">Number of symbols.</param>
    /// <param name="tableBits">Maximum number of bits for direct table lookup.</param>
    /// <returns>Decode table, or <c>null</c> on failure.</returns>
    static ushort[] LzxBuildHuffmanTable(int[] lengths, int numSymbols, int tableBits)
    {
        int tableSize = 1 << tableBits;
        var table     = new ushort[tableSize + numSymbols * 2];
        var blCount   = new int[17];

        // Count codes of each length
        for(var i = 0; i < numSymbols; i++)
        {
            if(lengths[i] > 16) return null;

            blCount[lengths[i]]++;
        }

        blCount[0] = 0;

        // Check for empty tree
        var allZero = true;

        for(var i = 1; i <= 16; i++)
        {
            if(blCount[i] != 0)
            {
                allZero = false;

                break;
            }
        }

        if(allZero) return table; // All lengths are 0, empty tree

        // Compute next code for each length
        var nextCode = new int[17];
        var code     = 0;

        for(var bits = 1; bits <= 16; bits++)
        {
            code           = code + blCount[bits - 1] << 1;
            nextCode[bits] = code;
        }

        // Fill direct lookup table
        for(var sym = 0; sym < numSymbols; sym++)
        {
            int len = lengths[sym];

            if(len == 0 || len > 16) continue;

            int huffCode = nextCode[len]++;

            if(len <= tableBits)
            {
                // Direct table entry: fill all entries with this symbol
                int fill = 1 << tableBits - len;

                for(var j = 0; j < fill; j++)
                {
                    int index = huffCode << tableBits - len | j;

                    if(index < tableSize) table[index] = (ushort)(len << 9 | sym);
                }
            }
        }

        return table;
    }

    /// <summary>Decodes a single Huffman symbol from the bitstream.</summary>
    static int LzxDecodeSymbol(ref LzxState state, ushort[] table, int[] lengths, int numSymbols, int tableBits)
    {
        // Ensure we have enough bits for a table lookup
        while(state.BitsLeft < 16)
        {
            if(state.InputOffset + 2 > state.Input.Length) break;

            var word = BitConverter.ToUInt16(state.Input, state.InputOffset);
            state.InputOffset += 2;
            state.BitBuffer   |= (uint)word << 16 - state.BitsLeft;
            state.BitsLeft    += 16;
        }

        // Direct table lookup
        var peek  = (int)(state.BitBuffer >> 32 - tableBits);
        int entry = table[peek];
        int len   = entry >> 9;
        int sym   = entry & 0x1FF;

        if(len > 0 && len <= tableBits)
        {
            state.BitBuffer <<= len;
            state.BitsLeft  -=  len;

            return sym;
        }

        // Slow path: bit-by-bit decoding for codes longer than tableBits
        var code2    = (int)(state.BitBuffer >> 32 - tableBits);
        int codeBits = tableBits;

        state.BitBuffer <<= tableBits;
        state.BitsLeft  -=  tableBits;

        for(; codeBits <= 16; codeBits++)
        {
            var nextBit = (int)(state.BitBuffer >> 31);
            code2           =   code2 << 1 | nextBit;
            state.BitBuffer <<= 1;
            state.BitsLeft--;

            // Search for matching code (linear scan — acceptable for rare overflow entries)
            for(var s = 0; s < numSymbols; s++)
            {
                if(lengths[s] != codeBits + 1) continue;

                // Compute the Huffman code for this symbol
                var nextCode2 = new int[17];
                var blCount2  = new int[17];

                for(var i = 0; i < numSymbols; i++)
                    if(lengths[i] <= 16)
                        blCount2[lengths[i]]++;

                blCount2[0] = 0;
                var c = 0;

                for(var bits = 1; bits <= 16; bits++)
                {
                    c               = c + blCount2[bits - 1] << 1;
                    nextCode2[bits] = c;
                }

                var symCode = 0;

                for(var i = 0; i <= s; i++)
                    if(lengths[i] == codeBits + 1)
                        symCode = nextCode2[codeBits + 1]++;

                if(symCode == code2) return s;
            }
        }

        return -1;
    }

    /// <summary>Bit-reading state for LZX decompression.</summary>
    struct LzxState
    {
        public byte[] Input;
        public uint   BitBuffer;
        public int    BitsLeft;
        public int    InputOffset;
        public byte[] Output;
        public int    OutputPos;
    }
}