// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Lfg.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Image conversion.
//
// --[ Description ] ----------------------------------------------------------
//
//     Lagged Fibonacci Generator for Nintendo GameCube/Wii junk fill.
//     Based on Dolphin emulator's LaggedFibonacciGenerator (CC0 licensed).
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program. If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2019-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Aaru.Core.Image.Ngcw;

/// <summary>Lagged Fibonacci Generator for Nintendo GameCube/Wii junk fill.</summary>
static class Lfg
{
    /// <summary>LFG buffer size (number of uint32 words in state).</summary>
    const int K = 521;

    /// <summary>LFG second tap.</summary>
    const int J = 32;

    /// <summary>Number of uint32 words needed to seed the LFG.</summary>
    public const int SEED_SIZE = 17;

    /// <summary>Minimum number of bytes needed for seed extraction (K * 4 = 2084).</summary>
    public const int MIN_SEED_DATA_BYTES = K * sizeof(uint);

    /// <summary>Total bytes in the LFG buffer (K * 4).</summary>
    const int BUFFER_BYTES = K * sizeof(uint);

    static uint Swap32(uint x) => BinaryPrimitives.ReverseEndianness(x);

    static void Forward(uint[] buffer)
    {
        for(var i = 0; i < J; i++) buffer[i] ^= buffer[i + K - J];

        for(int i = J; i < K; i++) buffer[i] ^= buffer[i - J];
    }

    static void Backward(uint[] buffer, int startWord, int endWord)
    {
        int loopEnd = J       > startWord ? J : startWord;
        int upper   = endWord < K ? endWord : K;

        for(int i = upper; i > loopEnd; --i) buffer[i - 1] ^= buffer[i - 1 - J];

        int upper2 = endWord < J ? endWord : J;

        for(int i = upper2; i > startWord; --i) buffer[i - 1] ^= buffer[i - 1 + K - J];
    }

    static bool Initialize(uint[] buffer, bool checkExisting)
    {
        for(int i = SEED_SIZE; i < K; i++)
        {
            uint calculated = buffer[i - 17] << 23 ^ buffer[i - 16] >> 9 ^ buffer[i - 1];

            if(checkExisting)
            {
                uint actual = buffer[i] & 0xFF00FFFF | buffer[i] << 2 & 0x00FC0000;

                if((calculated & 0xFFFCFFFF) != actual) return false;
            }

            buffer[i] = calculated;
        }

        // Apply the shift-by-18-instead-of-16 quirk + byteswap
        for(var i = 0; i < K; i++) buffer[i] = Swap32(buffer[i] & 0xFF00FFFF | buffer[i] >> 2 & 0x00FF0000);

        for(var i = 0; i < 4; i++) Forward(buffer);

        return true;
    }

    static bool Reinitialize(uint[] buffer, uint[] seedOut)
    {
        for(var i = 0; i < 4; i++) Backward(buffer, 0, K);

        for(var i = 0; i < K; i++) buffer[i] = Swap32(buffer[i]);

        // Reconstruct bits lost by the shift-by-18-instead-of-16 quirk
        for(var i = 0; i < SEED_SIZE; i++)
        {
            buffer[i] = buffer[i]                              & 0xFF00FFFF |
                        buffer[i]                         << 2 & 0x00FC0000 |
                        (buffer[i + 16] ^ buffer[i + 15]) << 9 & 0x00030000;
        }

        for(var i = 0; i < SEED_SIZE; i++) seedOut[i] = Swap32(buffer[i]);

        return Initialize(buffer, true);
    }

    /// <summary>Initialize LFG state from a 17-word big-endian seed.</summary>
    /// <param name="buffer">521-word LFG buffer to initialize.</param>
    /// <param name="seed">17-word seed (big-endian uint32 values).</param>
    public static void SetSeed(uint[] buffer, uint[] seed)
    {
        for(var i = 0; i < SEED_SIZE; i++) buffer[i] = Swap32(seed[i]);

        Initialize(buffer, false);
    }

    /// <summary>Generate junk bytes from the LFG state.</summary>
    /// <param name="buffer">521-word LFG buffer (must be initialized).</param>
    /// <param name="positionBytes">Current byte position within the buffer; updated on return.</param>
    /// <param name="output">Output buffer to fill.</param>
    /// <param name="offset">Start offset within <paramref name="output" />.</param>
    /// <param name="count">Number of bytes to generate.</param>
    public static void GetBytes(uint[] buffer, ref int positionBytes, byte[] output, int offset, int count)
    {
        Span<byte> bufferBytes = MemoryMarshal.AsBytes(buffer.AsSpan());

        while(count > 0)
        {
            int avail = BUFFER_BYTES - positionBytes;
            int chunk = count < avail ? count : avail;

            bufferBytes.Slice(positionBytes, chunk).CopyTo(output.AsSpan(offset, chunk));

            positionBytes += chunk;
            count         -= chunk;
            offset        += chunk;

            if(positionBytes == BUFFER_BYTES)
            {
                Forward(buffer);
                positionBytes = 0;
            }
        }
    }

    /// <summary>
    ///     Try to extract the LFG seed from a chunk of data suspected to be PRNG output.
    ///     Requires at least K * 4 = 2084 bytes of contiguous PRNG output.
    /// </summary>
    /// <param name="data">Data to test.</param>
    /// <param name="dataOffset">
    ///     Byte offset of data[0] within its LFG block (0x8000-aligned for Wii,
    ///     arbitrary for GC).
    /// </param>
    /// <param name="seedOut">If successful, receives the 17-word seed (big-endian).</param>
    /// <returns>
    ///     Number of consecutive bytes from data[0] that match the LFG.
    ///     Returns 0 if the data does not look like junk.
    /// </returns>
    public static int GetSeed(ReadOnlySpan<byte> data, int dataOffset, uint[] seedOut)
    {
        int size = data.Length;

        // Work on whole u32 words — skip partial leading bytes to reach uint32 alignment
        int bytesToSkip = (dataOffset + 3 & ~3) - dataOffset;

        if(bytesToSkip > size) return 0;

        // Reinterpret data as uint32 array (native endian)
        ReadOnlySpan<byte> aligned   = data.Slice(bytesToSkip);
        int                u32Size   = aligned.Length             / sizeof(uint);
        int                u32Offset = (dataOffset + bytesToSkip) / sizeof(uint);

        if(u32Size < K) return 0;

        ReadOnlySpan<uint> u32Data = MemoryMarshal.Cast<byte, uint>(aligned);

        // Quick sanity check: the top bits have a specific pattern from the shift quirk
        for(var i = 0; i < K; i++)
        {
            uint x = Swap32(u32Data[i]);

            if((x & 0x00C00000) != (x >> 2 & 0x00C00000)) return 0;
        }

        var lfgBuffer  = new uint[K];
        int offsetModK = u32Offset % K;
        int offsetDivK = u32Offset / K;

        // Place the data into the buffer at the correct position.
        // Copy raw native-endian u32 values — NO byte-swapping here.
        int firstPart = K - offsetModK;

        if(firstPart > K) firstPart = K;

        for(var i = 0; i < firstPart && i < K; i++) lfgBuffer[offsetModK + i] = u32Data[i];

        for(var i = 0; i < offsetModK; i++) lfgBuffer[i] = u32Data[firstPart + i];

        Backward(lfgBuffer, 0, offsetModK);

        for(var i = 0; i < offsetDivK; i++) Backward(lfgBuffer, 0, K);

        if(!Reinitialize(lfgBuffer, seedOut)) return 0;

        int positionBytes = dataOffset % BUFFER_BYTES;

        // Advance the LFG forward to match the data_offset position
        for(var i = 0; i < offsetDivK; i++) Forward(lfgBuffer);

        // Count how many bytes from data match the LFG output
        Span<byte> lfgBytes = MemoryMarshal.AsBytes(lfgBuffer.AsSpan());
        var        result   = 0;
        var        p        = 0;

        while(p < size)
        {
            byte expected = lfgBytes[positionBytes];

            if(data[p] != expected) break;

            result++;
            p++;
            positionBytes++;

            if(positionBytes == BUFFER_BYTES)
            {
                Forward(lfgBuffer);
                positionBytes = 0;
            }
        }

        return result;
    }
}