// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsMkbProcessor.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Derive an AACS Media Key from a raw Media Key Block and a list of device
//     keys, walking the AES-G3 subset-difference tree as described in the
//     AACS Common Cryptographic Elements §3.2.4.
//
//     Device keys are expected to carry subset-difference metadata (device node,
//     KEY_UV, KEY_U_MASK_SHIFT) as in libaacs KEYDB.cfg DK entries. The processor
//     follows the same pruned path as libaacs's <c>_calc_mk_dks</c>: locate the
//     matching subset-difference row and walk a single descent path to the
//     processing key.
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
using System.Collections.Generic;

namespace Aaru.Decryption.Aacs;

/// <summary>
///     Derives an AACS Media Key from a Media Key Block and a list of device keys.
/// </summary>
public static class AacsMkbProcessor
{
    static readonly byte[] _validateMagic = [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF];

    /// <summary>
    ///     Compute the AACS subset-difference V-mask for the given UV. Mirrors libaacs <c>_calc_v_mask</c>.
    ///     Starting with all bits set, shift left while no bit of <paramref name="uv" /> falls outside the
    ///     mask.
    /// </summary>
    /// <param name="uv">UV value.</param>
    /// <returns>V-mask.</returns>
    public static uint CalcVMask(uint uv)
    {
        uint vMask = 0xFFFFFFFFu;

        while((uv & ~vMask) == 0)
        {
            uint shifted = vMask << 1;

            if(shifted == 0) return vMask;

            vMask = shifted;
        }

        return vMask;
    }

    /// <summary>
    ///     Compute the AACS subset-difference U-mask for the given U-mask shift. Spec is
    ///     <c>0xFFFFFFFFu &lt;&lt; shift</c>; this version clamps shifts of 32 or higher to all-zero
    ///     (matching the spec's "no bits left" semantic and avoiding C# &lt;&lt; modulo-32 behavior).
    /// </summary>
    /// <param name="shift">U-mask shift.</param>
    /// <returns>U-mask.</returns>
    public static uint CalcUMask(byte shift) => shift >= 32 ? 0u : 0xFFFFFFFFu << shift;

    /// <summary>
    ///     Walk the AES-G3 subset-difference tree downwards from the supplied device key to the processing
    ///     key for subset difference <c>(uv, vMask)</c>. Faithfully transcribed from libaacs <c>_calc_pk</c>.
    /// </summary>
    /// <param name="deviceKey">16-byte device key sitting at depth <paramref name="devKeyVMask" />.</param>
    /// <param name="uv">Row UV.</param>
    /// <param name="vMask">Target V-mask of the subset-difference row.</param>
    /// <param name="devKeyVMask">Device key's own V-mask (its current depth in the tree).</param>
    /// <param name="processingKey">Receives the 16-byte processing key.</param>
    public static void CalcPk(ReadOnlySpan<byte> deviceKey,
                              uint               uv,
                              uint               vMask,
                              uint               devKeyVMask,
                              Span<byte>         processingKey)
    {
        Span<byte> leftChild  = stackalloc byte[16];
        Span<byte> rightChild = stackalloc byte[16];
        Span<byte> currentKey = stackalloc byte[16];

        AacsCrypto.AesG3Step(deviceKey, 0, leftChild);
        AacsCrypto.AesG3Step(deviceKey, 1, processingKey);
        AacsCrypto.AesG3Step(deviceKey, 2, rightChild);

        while(devKeyVMask != vMask)
        {
            int i;

            for(i = 31; i >= 0; i--)
            {
                if((devKeyVMask & (1u << i)) == 0) break;
            }

            if(i < 0 || (uv & (1u << i)) == 0)
                leftChild.CopyTo(currentKey);
            else
                rightChild.CopyTo(currentKey);

            AacsCrypto.AesG3Step(currentKey, 0, leftChild);
            AacsCrypto.AesG3Step(currentKey, 1, processingKey);
            AacsCrypto.AesG3Step(currentKey, 2, rightChild);

            devKeyVMask = (uint)((int)devKeyVMask >> 1);
        }
    }

    /// <summary>
    ///     AACS Media Key validation (Common Cryptographic Elements §3.2.5):
    ///     <c>mk = AES-128D(pk, cValue); mk[12..15] ^= uv[0..3]</c>; the first 8 bytes of
    ///     <c>AES-128D(mk, dv)</c> must equal <c>01 23 45 67 89 AB CD EF</c>.
    /// </summary>
    /// <param name="processingKey">Processing key (16 bytes).</param>
    /// <param name="cValue">Encrypted Media Key Data row (16 bytes).</param>
    /// <param name="uvBytes">Big-endian UV (4 bytes).</param>
    /// <param name="dv">Verify Media Key Data (16 bytes).</param>
    /// <param name="mediaKey">On success, receives the 16-byte Media Key.</param>
    /// <returns><c>true</c> if the processing key validates.</returns>
    public static bool ValidatePk(ReadOnlySpan<byte> processingKey,
                                  ReadOnlySpan<byte> cValue,
                                  ReadOnlySpan<byte> uvBytes,
                                  ReadOnlySpan<byte> dv,
                                  Span<byte>         mediaKey)
    {
        if(processingKey.Length != 16 ||
           cValue.Length        != 16 ||
           uvBytes.Length       != 4  ||
           dv.Length            != 16 ||
           mediaKey.Length      != 16)
            return false;

        AacsCrypto.Aes128EcbDecrypt(processingKey, cValue, mediaKey);

        for(int a = 0; a < 4; a++) mediaKey[a + 12] ^= uvBytes[a];

        Span<byte> decryptedDv = stackalloc byte[16];
        AacsCrypto.Aes128EcbDecrypt(mediaKey, dv, decryptedDv);

        return decryptedDv[..8].SequenceEqual(_validateMagic);
    }

    /// <summary>
    ///     Try to derive the AACS Media Key from <paramref name="rawMkb" /> using the supplied
    ///     <paramref name="keys" />.
    /// </summary>
    /// <param name="rawMkb">Raw MKB bytes as read from the disc / drive.</param>
    /// <param name="keys">Device keys with subset-difference metadata (entries without it are skipped).</param>
    /// <param name="mediaKey">On success, the 16-byte Media Key.</param>
    /// <param name="error">On failure, a human-readable error message.</param>
    /// <returns><c>true</c> if a Media Key was found.</returns>
    public static bool TryDeriveMediaKey(ReadOnlySpan<byte>          rawMkb,
                                         IReadOnlyList<AacsDeviceKey> keys,
                                         out byte[]?                  mediaKey,
                                         out string?                  error)
    {
        mediaKey = null;
        error    = null;

        if(rawMkb.IsEmpty)
        {
            error = "MKB is empty.";

            return false;
        }

        if(keys.Count == 0)
        {
            error = "No device keys supplied.";

            return false;
        }

        if(!AacsMkb.TryGetSubsetDifferenceRows(rawMkb, out ReadOnlySpan<byte> subsetDiff))
        {
            bool hasHostRevocation = AacsMkb.TryGetRecordPayload(rawMkb, AacsMkb.RECORD_HOST_REVOCATION, out _);
            bool hasDriveRevocation = AacsMkb.TryGetRecordPayload(rawMkb, AacsMkb.RECORD_DRIVE_REVOCATION, out _);
            bool hasMediaKeyData = AacsMkb.TryGetCValues(rawMkb, out _);

            if((hasHostRevocation || hasDriveRevocation) && !hasMediaKeyData)
            {
                error = "MKB has no Subset-Difference Record (0x04). " +
                        "It contains only revocation records (0x20/0x21), so this is the lead-in Partial MKB " +
                        "the drive returns for authentication; the full MKB at /AACS/MKBROM.AACS (HD DVD) or " +
                        "/AACS/MKB_RO.inf (Blu-ray) is needed for Media Key derivation but was not loaded.";

                return false;
            }

            error = "MKB has no Subset-Difference Record (0x04). The drive returned only a Partial MKB and " +
                    "no full /AACS/MKBROM.AACS (HD DVD) or /AACS/MKB_RO.inf (Blu-ray) was found in the dumped image.";

            return false;
        }

        if(!AacsMkb.TryGetCValues(rawMkb, out ReadOnlySpan<byte> cvalues))
        {
            error = "MKB is missing the Media Key Data record (0x05).";

            return false;
        }

        if(!AacsMkb.TryGetVerifyMediaKey(rawMkb, out ReadOnlySpan<byte> dv))
        {
            error = "MKB is missing the Verify Media Key Data record (0x81 or 0x86).";

            return false;
        }

        int rowCount = subsetDiff.Length / 5;
        int cvCount  = cvalues.Length    / 16;
        int rows     = Math.Min(rowCount, cvCount);

        if(rows == 0)
        {
            error = "MKB has no subset-difference rows.";

            return false;
        }

        Span<byte> processingKey = stackalloc byte[16];
        Span<byte> mk            = stackalloc byte[16];

        // Collect the parsed (uMaskShift, uv, uvBytes, cValue) for each usable row up-front so we don't
        // re-parse for each device key. Stop at the first end-of-list / revocation marker.
        int usableRows = 0;
        Span<RowInfo> rowInfos = rows <= 256 ? stackalloc RowInfo[rows] : new RowInfo[rows];

        for(int i = 0; i < rows; i++)
        {
            byte uMaskShift = subsetDiff[i * 5];

            if((uMaskShift & 0xC0) != 0) break;

            uint uv = (uint)((subsetDiff[i * 5 + 1] << 24) |
                             (subsetDiff[i * 5 + 2] << 16) |
                             (subsetDiff[i * 5 + 3] << 8)  |
                             subsetDiff[i * 5 + 4]);

            if(uv == 0) continue;

            rowInfos[usableRows++] = new RowInfo(uMaskShift, uv, i);
        }

        if(usableRows == 0)
        {
            error = "MKB has no usable subset-difference rows (all marked end-of-list or empty).";

            return false;
        }

        foreach(AacsDeviceKey key in keys)
        {
            if(!key.HasMetadata)
                continue;

            if(TryPrunedPath(in key, rowInfos[..usableRows], subsetDiff, cvalues, dv, processingKey, mk))
            {
                mediaKey = mk.ToArray();

                return true;
            }
        }

        error = "No supplied device key could derive a valid Media Key from the MKB.";

        return false;
    }

    static bool TryPrunedPath(in AacsDeviceKey key,
                              ReadOnlySpan<RowInfo> rowInfos,
                              ReadOnlySpan<byte> subsetDiff,
                              ReadOnlySpan<byte> cvalues,
                              ReadOnlySpan<byte> dv,
                              Span<byte>         processingKey,
                              Span<byte>         mediaKey)
    {
        uint deviceNode      = key.Node!.Value;
        uint deviceKeyUv     = key.Uv!.Value;
        uint deviceKeyUMask  = CalcUMask(key.UMaskShift!.Value);
        uint deviceKeyVMask  = CalcVMask(deviceKeyUv);

        for(int r = 0; r < rowInfos.Length; r++)
        {
            RowInfo info  = rowInfos[r];
            uint    uMask = CalcUMask(info.UMaskShift);
            uint    vMask = CalcVMask(info.Uv);

            if((deviceNode & uMask) != (info.Uv & uMask)) continue;

            if((deviceNode & vMask) == (info.Uv & vMask)) continue;

            if(uMask != deviceKeyUMask) continue;

            if((info.Uv & deviceKeyVMask) != (deviceKeyUv & deviceKeyVMask)) continue;

            CalcPk(key.Key, info.Uv, vMask, deviceKeyVMask, processingKey);

            ReadOnlySpan<byte> uvBytes = subsetDiff.Slice(info.RowIndex * 5 + 1, 4);
            ReadOnlySpan<byte> cValue  = cvalues.Slice(info.RowIndex * 16, 16);

            if(ValidatePk(processingKey, cValue, uvBytes, dv, mediaKey)) return true;
        }

        return false;
    }

    readonly struct RowInfo
    {
        public RowInfo(byte uMaskShift, uint uv, int rowIndex)
        {
            UMaskShift = uMaskShift;
            Uv         = uv;
            RowIndex   = rowIndex;
        }

        public byte UMaskShift { get; }
        public uint Uv         { get; }
        public int  RowIndex   { get; }
    }
}