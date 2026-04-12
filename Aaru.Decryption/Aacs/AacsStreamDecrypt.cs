// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsStreamDecrypt.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Blu-ray AACS aligned-unit (6144-byte) decrypt.
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

namespace Aaru.Decryption.Aacs;

/// <summary>Decrypts one 6144-byte aligned unit using decrypted CPS unit keys.</summary>
public static class AacsStreamDecrypt
{
    /// <summary>User-data sector size and bus-encryption block size.</summary>
    public const int SectorLen = 2048;

    /// <summary>Three user sectors: CPS aligned unit length.</summary>
    public const int AlignedUnitLen = 6144;

    /// <summary>
    ///     If the unit is marked unencrypted (MPEG-TS copy permission bits), returns <see langword="true"/>
    ///     without changing the buffer. Otherwise tries each CPS unit key until MPEG-TS sync verifies.
    /// </summary>
    /// <param name="unit">Unit to decrypt.</param>
    /// <param name="decryptedCpsUnitKeys">Decrypted CPS unit keys.</param>
    /// <returns>True if the unit was decrypted successfully.</returns>
    public static bool TryDecryptAlignedUnit(Span<byte> unit, ReadOnlySpan<byte[]> decryptedCpsUnitKeys)
    {
        if(unit.Length != AlignedUnitLen)
            throw new ArgumentException($"Unit must be {AlignedUnitLen} bytes.", nameof(unit));

        if(decryptedCpsUnitKeys.Length == 0)
            return false;

        // If the unit is not encrypted, return true.
        if((unit[0] & 0xc0) == 0)
            return true;

        byte[] work = new byte[AlignedUnitLen];

        for(int i = 0; i < decryptedCpsUnitKeys.Length; i++)
        {
            unit.CopyTo(work);

            if(!TryDecryptUnitWithKey(work, decryptedCpsUnitKeys[i]))
                continue;

            work.CopyTo(unit);

            return true;
        }

        return false;
    }

    /// <summary>Decrypts one unit with a given CPS unit key.</summary>
    /// <param name="unit">Unit to decrypt.</param>
    /// <param name="decryptedCpsUnitKey">Decrypted CPS unit key.</param>
    /// <returns>True if the unit was decrypted successfully.</returns>
    public static bool TryDecryptUnitWithKey(Span<byte> unit, ReadOnlySpan<byte> decryptedCpsUnitKey)
    {
        if(unit.Length != AlignedUnitLen)
            throw new ArgumentException($"Unit must be {AlignedUnitLen} bytes.", nameof(unit));

        if(decryptedCpsUnitKey.Length != 16)
            throw new ArgumentException("CPS unit key must be 16 bytes.", nameof(decryptedCpsUnitKey));

        Span<byte> key = stackalloc byte[16];
        AacsCrypto.Aes128EcbEncrypt(decryptedCpsUnitKey, unit.Slice(0, 16), key);

        for(int a = 0; a < 16; a++)
            key[a] ^= unit[a];

        ReadOnlySpan<byte> cipher = unit.Slice(16, AlignedUnitLen - 16);
        Span<byte>         plain  = unit.Slice(16, AlignedUnitLen - 16);
        byte[]             tmp    = new byte[AlignedUnitLen - 16];
        AacsCrypto.AacsCbcDecrypt(key, cipher, tmp);
        tmp.CopyTo(plain);

        return VerifyAndNormalizeTransportStream(unit);
    }

    /// <summary>Verifies and normalizes the transport stream. Removes copy-permission indicator bits.</summary>
    /// <param name="unit">Unit to verify and normalize.</param>
    /// <returns>True if the unit was verified and normalized successfully.</returns>
    public static bool VerifyAndNormalizeTransportStream(Span<byte> unit)
    {
        if(unit.Length != AlignedUnitLen)
            throw new ArgumentException($"Unit must be {AlignedUnitLen} bytes.", nameof(unit));

        // In AACS aligned units, payload is expected to be 32 packets of 192 bytes.
        // Each 192-byte packet has a 4-byte TP extra header followed by a TS packet.
        for(int i = 0; i < AlignedUnitLen; i += 192)
        {
            // The TS sync byte (0x47) must be at offset +4 because of that extra 4-byte header.
            if(unit[i + 4] != 0x47)
                return false;

            // Clear copy-permission indicator bits in the TP extra header.
            unit[i] &= 0x3f;
        }

        return true;
    }
}
