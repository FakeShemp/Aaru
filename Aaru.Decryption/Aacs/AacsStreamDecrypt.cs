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

/// <summary>Decrypts AACS aligned units (Blu-ray 6144-byte CPS) and HD DVD per-pack encryption.</summary>
public static class AacsStreamDecrypt
{
    /// <summary>User-data sector size.</summary>
    public const int SectorLen = 2048;

    /// <summary>Three user sectors: BD CPS aligned unit length.</summary>
    public const int AlignedUnitLen = 6144;

    /// <summary>Unencrypted header portion of an HD DVD pack (bytes 0-127).</summary>
    public const int HddvdPackHeaderLen = 128;

    /// <summary>Encrypted portion of an HD DVD pack (bytes 128-2047).</summary>
    public const int HddvdPackEncryptedLen = 1920;

    /// <summary>Offset of the CPI field within a navigation pack.</summary>
    const int CPI_OFFSET = 0x3C;

    /// <summary>Length of the CPI field.</summary>
    const int CPI_LEN = 16;

    /// <summary>Offset of <c>PES_scrambling_control</c> in a pack (byte 20, bits 5:4).</summary>
    const int PES_SC_OFFSET = 20;

    /// <summary>Offset of <c>Title Key Data (Dtk)</c> within a regular encrypted pack (bytes 84-87).</summary>
    const int DTK_OFFSET = 84;

    /// <summary>Length of <c>Dtk</c>.</summary>
    const int DTK_LEN = 4;

    #region Blu-ray CPS aligned-unit decryption

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

    /// <summary>Decrypts one BD CPS unit with a given key.</summary>
    public static bool TryDecryptUnitWithKey(Span<byte> unit, ReadOnlySpan<byte> decryptedCpsUnitKey)
    {
        if(unit.Length != AlignedUnitLen)
            throw new ArgumentException($"Unit must be {AlignedUnitLen} bytes.", nameof(unit));

        if(decryptedCpsUnitKey.Length != 16)
            throw new ArgumentException("CPS unit key must be 16 bytes.", nameof(decryptedCpsUnitKey));

        Span<byte> key = stackalloc byte[16];
        AacsCrypto.AesG(decryptedCpsUnitKey, unit.Slice(0, 16), key);

        ReadOnlySpan<byte> cipher = unit.Slice(16, AlignedUnitLen - 16);
        byte[]             tmp    = new byte[AlignedUnitLen - 16];
        AacsCrypto.AacsCbcDecrypt(key, cipher, tmp);
        tmp.CopyTo(unit.Slice(16));

        return VerifyAndNormalizeTransportStream(unit);
    }

    /// <summary>Verifies and normalizes the transport stream. Removes copy-permission indicator bits.</summary>
    public static bool VerifyAndNormalizeTransportStream(Span<byte> unit)
    {
        if(unit.Length != AlignedUnitLen)
            throw new ArgumentException($"Unit must be {AlignedUnitLen} bytes.", nameof(unit));

        for(int i = 0; i < AlignedUnitLen; i += 192)
        {
            if(unit[i + 4] != 0x47)
                return false;

            unit[i] &= 0x3f;
        }

        return true;
    }

    #endregion

    #region HD DVD per-pack decryption

    /// <summary>Returns <see langword="true"/> if <paramref name="sector"/> is an HD DVD navigation pack (NV_PCK).</summary>
    public static bool IsHddvdNavPack(ReadOnlySpan<byte> sector)
    {
        if(sector.Length < SectorLen)
            return false;

        return sector[0x11] == 0xBB &&
               sector[0x2A] == 0x00 &&
               sector[0x2B] == 0x01 &&
               sector[0x2C] == 0xBF;
    }

    /// <summary>Extracts the CPI field and <c>TITLE_KEY_PTR</c> from an HD DVD navigation pack.</summary>
    /// <param name="sector">2048-byte navigation pack.</param>
    /// <param name="cpiField">16-byte CPI field.</param>
    /// <param name="titleKeyPtr">1-based index into the TKF, or 0 if no key is valid.</param>
    /// <returns><see langword="true"/> if <c>KEY_VF</c> indicates a valid title key.</returns>
    public static bool ParseHddvdNavPack(ReadOnlySpan<byte> sector, out byte[] cpiField, out int titleKeyPtr)
    {
        cpiField    = new byte[CPI_LEN];
        titleKeyPtr = 0;

        if(sector.Length < SectorLen)
            return false;

        sector.Slice(CPI_OFFSET, CPI_LEN).CopyTo(cpiField);

        bool keyValid = (cpiField[0] & 0x80) != 0;

        if(keyValid)
            titleKeyPtr = cpiField[2] & 0xFF;

        return keyValid;
    }

    /// <summary>Returns <see langword="true"/> if the pack is encrypted (<c>PES_scrambling_control == 01₂</c>).</summary>
    public static bool IsHddvdPackEncrypted(ReadOnlySpan<byte> sector)
    {
        if(sector.Length < SectorLen)
            return false;

        return ((sector[PES_SC_OFFSET] >> 4) & 3) == 1;
    }

    /// <summary>
    ///     Decrypts one HD DVD encrypted pack in-place. Derives <c>Kc = AES-G(Kt, Dtk || CPIlsb_96)</c>,
    ///     decrypts the 1920-byte encrypted portion (bytes 128-2047), and clears <c>PES_scrambling_control</c>.
    /// </summary>
    /// <param name="sector">2048-byte pack (modified in-place).</param>
    /// <param name="titleKey">Decrypted 16-byte title key (Kt).</param>
    /// <param name="cpiField">16-byte CPI field from the current navigation pack.</param>
    public static void DecryptHddvdPack(Span<byte> sector, ReadOnlySpan<byte> titleKey, ReadOnlySpan<byte> cpiField)
    {
        if(sector.Length != SectorLen)
            throw new ArgumentException($"Pack must be {SectorLen} bytes.", nameof(sector));

        if(titleKey.Length != 16)
            throw new ArgumentException("Title key must be 16 bytes.", nameof(titleKey));

        if(cpiField.Length != CPI_LEN)
            throw new ArgumentException($"CPI field must be {CPI_LEN} bytes.", nameof(cpiField));

        Span<byte> seed = stackalloc byte[16];
        sector.Slice(DTK_OFFSET, DTK_LEN).CopyTo(seed[..DTK_LEN]);
        cpiField.Slice(DTK_LEN, CPI_LEN - DTK_LEN).CopyTo(seed[DTK_LEN..]);

        Span<byte> contentKey = stackalloc byte[16];
        AacsCrypto.AesH(titleKey, seed, contentKey);

        byte[] encrypted = new byte[HddvdPackEncryptedLen];
        sector.Slice(HddvdPackHeaderLen, HddvdPackEncryptedLen).CopyTo(encrypted);

        byte[] decrypted = new byte[HddvdPackEncryptedLen];
        AacsCrypto.AacsCbcDecrypt(contentKey, encrypted, decrypted);
        decrypted.CopyTo(sector.Slice(HddvdPackHeaderLen));

        sector[PES_SC_OFFSET] &= 0xCF;
    }

    /// <summary>Clears the CPI-related bytes in a navigation pack header after extraction.</summary>
    public static void ClearHddvdNavPackCpi(Span<byte> sector)
    {
        if(sector.Length < SectorLen)
            return;

        sector[CPI_OFFSET] = 0;
        sector[0x48]       = 0;
    }

    #endregion
}
