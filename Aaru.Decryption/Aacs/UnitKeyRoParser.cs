// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : UnitKeyRoParser.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Parses AACS/Unit_Key_RO.inf
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Buffers.Binary;

namespace Aaru.Decryption.Aacs;

/// <summary>Parsed CPS encrypted unit keys from <c>Unit_Key_RO.inf</c>.</summary>
public sealed class UnitKeyRoParseResult
{
    UnitKeyRoParseResult(byte[][] encryptedCpsUnitKeys, bool isAacs2Layout)
    {
        EncryptedCpsUnitKeys = encryptedCpsUnitKeys;
        IsAacs2Layout        = isAacs2Layout;
    }

    /// <summary>One 16-byte encrypted key per CPS unit, index order.</summary>
    public byte[][] EncryptedCpsUnitKeys { get; }

    /// <summary>True when the AACS2 key stride (64 bytes per key entry) was used.</summary>
    public bool IsAacs2Layout { get; }

    /// <summary>Parses Blu-ray <c>Unit_Key_RO.inf</c> (AACS1 layout by default).</summary>
    /// <param name="data">Data to parse.</param>
    /// <param name="tryAacs2">True to try AACS2 layout.</param>
    /// <returns>Parsed CPS encrypted unit keys.</returns>
    public static UnitKeyRoParseResult? TryParse(ReadOnlySpan<byte> data, bool tryAacs2 = false)
    {
        if(tryAacs2)
            return TryParseInternal(data, true);

        UnitKeyRoParseResult? r = TryParseInternal(data, false);

        return r ?? TryParseInternal(data, true);
    }

    /// <summary>Parses Blu-ray <c>Unit_Key_RO.inf</c> (AACS1 layout by default).</summary>
    /// <param name="data">Data to parse.</param>
    /// <param name="aacs2">True to try AACS2 layout.</param>
    /// <returns>Parsed CPS encrypted unit keys.</returns>
    static UnitKeyRoParseResult? TryParseInternal(ReadOnlySpan<byte> data, bool aacs2)
    {
        if(data.Length < 20)
            return null;

        byte numBdmvDir = data[17];

        if(numBdmvDir < 1)
            return null;

        if(data.Length < 4)
            return null;

        uint ukPos = BinaryPrimitives.ReadUInt32BigEndian(data);

        if(data.Length - 2 < ukPos)
            return null;

        if((int)ukPos >= data.Length)
            return null;

        ushort numUk = BinaryPrimitives.ReadUInt16BigEndian(data.Slice((int)ukPos, 2));

        if(numUk < 1)
            return null;

        if(data.Length - (int)ukPos < 16)
            return null;

        if((data.Length - (int)ukPos - 16) / 48 < numUk)
            return null;

        ReadOnlySpan<byte> emptyKey = stackalloc byte[16];

        if(aacs2 && numUk > 1 && data.Length >= 48 + 48 + 16)
        {
            if(data.Slice(48 + 48 + 16, 16).SequenceEqual(emptyKey))
                aacs2 = false;
            else if(data.Length < (int)ukPos + 64 * numUk + 16)
                return null;
        }

        byte[][] encKeys = new byte[numUk][];

        uint pos = ukPos;

        for(uint i = 0; i < numUk; i++)
        {
            pos += 48;

            if(pos + 16 > (uint)data.Length)
                return null;

            byte[] key = new byte[16];
            data.Slice((int)pos, 16).CopyTo(key);
            encKeys[i] = key;

            if(aacs2)
                pos += 16;
        }

        return new UnitKeyRoParseResult(encKeys, aacs2);
    }

    /// <summary>
    ///     Interprets a tag or buffer as raw concatenated 16-byte encrypted CPS keys (no .inf wrapper).
    /// </summary>
    /// <param name="data">Data to parse.</param>
    /// <returns>Parsed CPS encrypted unit keys.</returns>
    public static UnitKeyRoParseResult? TryParseRawEncryptedKeys(ReadOnlySpan<byte> data)
    {
        if(data.Length < 16 || data.Length % 16 != 0)
            return null;

        int           n      = data.Length / 16;
        byte[][]      keys   = new byte[n][];
        ReadOnlySpan<byte> p = data;

        for(int i = 0; i < n; i++)
        {
            byte[] key = new byte[16];
            p.Slice(i * 16, 16).CopyTo(key);
            keys[i] = key;
        }

        return new UnitKeyRoParseResult(keys, false);
    }
}
