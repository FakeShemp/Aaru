// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsMkb.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Read records from an AACS Media Key Block. Each record starts with a
//     1-byte type and a big-endian 24-bit length covering the whole record
//     header plus payload. See AACS Common Cryptographic Elements, §3.2.5.
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

/// <summary>
///     Static helpers for reading the records of an AACS Media Key Block.
/// </summary>
public static class AacsMkb
{
    /// <summary>MKB record type: Type and Version.</summary>
    public const byte RECORD_TYPE_AND_VERSION = 0x10;

    /// <summary>MKB record type: Drive Revocation List.</summary>
    public const byte RECORD_DRIVE_REVOCATION = 0x20;

    /// <summary>MKB record type: Host Revocation List.</summary>
    public const byte RECORD_HOST_REVOCATION = 0x21;

    /// <summary>MKB record type: Explicit Subset-Difference.</summary>
    public const byte RECORD_SUBSET_DIFFERENCE = 0x04;

    /// <summary>MKB record type: Media Key Data (C-values).</summary>
    public const byte RECORD_MEDIA_KEY_DATA = 0x05;

    /// <summary>MKB record type: Verify Media Key Data (used by most MKB types).</summary>
    public const byte RECORD_VERIFY_MEDIA_KEY = 0x81;

    /// <summary>MKB record type: Verify Media Key Data (used by Category C MKBs).</summary>
    public const byte RECORD_VERIFY_MEDIA_KEY_CAT_C = 0x86;

    /// <summary>MKB record type: End-of-MKB / Signature.</summary>
    public const byte RECORD_SIGNATURE = 0x02;

    /// <summary>MKB type: Type 3.</summary>
    public const uint MKB_TYPE_3 = 0x00031003;

    /// <summary>MKB type: Type 4.</summary>
    public const uint MKB_TYPE_4 = 0x00041003;

    /// <summary>MKB type: Type 10 Class II.</summary>
    public const uint MKB_TYPE_10_CLASS_II = 0x000A1003;

    /// <summary>MKB type: Category C, version 20.</summary>
    public const uint MKB_20_CATEGORY_C = 0x48141003;

    /// <summary>MKB type: Category C, version 21.</summary>
    public const uint MKB_21_CATEGORY_C = 0x48151003;

    /// <summary>
    ///     Walk the MKB records sequentially looking for one whose first byte equals
    ///     <paramref name="recordType" />. Returns the entire record (header + payload).
    /// </summary>
    /// <param name="mkb">Raw MKB bytes.</param>
    /// <param name="recordType">Record type to look for.</param>
    /// <param name="record">Receives the full record on success.</param>
    /// <returns><c>true</c> if the record was found and not truncated.</returns>
    public static bool TryFindRecord(ReadOnlySpan<byte> mkb, byte recordType, out ReadOnlySpan<byte> record)
    {
        record = default;
        int pos = 0;

        while(pos + 4 <= mkb.Length)
        {
            int len = (mkb[pos + 1] << 16) | (mkb[pos + 2] << 8) | mkb[pos + 3];

            if(mkb[pos] == recordType)
            {
                if(len < 4 || pos + len > mkb.Length) return false;

                record = mkb.Slice(pos, len);

                return true;
            }

            if(len == 0) return false;

            if(pos + len > mkb.Length) return false;

            pos += len;
        }

        return false;
    }

    /// <summary>Read the MKB type (BE32 at offset 4 of record 0x10).</summary>
    /// <param name="mkb">Raw MKB bytes.</param>
    /// <param name="mkbType">Receives the MKB type.</param>
    /// <param name="version">Receives the MKB version.</param>
    /// <returns><c>true</c> if the Type and Version record was found and at least 12 bytes long.</returns>
    public static bool TryGetTypeAndVersion(ReadOnlySpan<byte> mkb, out uint mkbType, out uint version)
    {
        mkbType = 0;
        version = 0;

        if(!TryFindRecord(mkb, RECORD_TYPE_AND_VERSION, out ReadOnlySpan<byte> rec)) return false;

        if(rec.Length < 12) return false;

        mkbType = (uint)((rec[4] << 24) | (rec[5] << 16) | (rec[6] << 8) | rec[7]);
        version = (uint)((rec[8] << 24) | (rec[9] << 16) | (rec[10] << 8) | rec[11]);

        return true;
    }

    /// <summary>Returns the payload (record body without 4-byte header) for the given record type.</summary>
    /// <param name="mkb">Raw MKB bytes.</param>
    /// <param name="recordType">Record type to look for.</param>
    /// <param name="payload">Receives the record's payload on success.</param>
    /// <returns><c>true</c> if the record was found and has a non-empty payload.</returns>
    public static bool TryGetRecordPayload(ReadOnlySpan<byte> mkb, byte recordType, out ReadOnlySpan<byte> payload)
    {
        payload = default;

        if(!TryFindRecord(mkb, recordType, out ReadOnlySpan<byte> rec)) return false;

        if(rec.Length < 4) return false;

        payload = rec[4..];

        return true;
    }

    /// <summary>Get the Explicit Subset-Difference record payload (record 0x04).</summary>
    /// <param name="mkb">Raw MKB bytes.</param>
    /// <param name="payload">Receives the payload, a sequence of 5-byte (u_mask_shift, uv_be32) tuples.</param>
    /// <returns><c>true</c> if the record was found.</returns>
    public static bool TryGetSubsetDifferenceRows(ReadOnlySpan<byte> mkb, out ReadOnlySpan<byte> payload) =>
        TryGetRecordPayload(mkb, RECORD_SUBSET_DIFFERENCE, out payload);

    /// <summary>Get the Media Key Data record payload (record 0x05) — concatenated 16-byte C-values.</summary>
    /// <param name="mkb">Raw MKB bytes.</param>
    /// <param name="payload">Receives the payload.</param>
    /// <returns><c>true</c> if the record was found.</returns>
    public static bool TryGetCValues(ReadOnlySpan<byte> mkb, out ReadOnlySpan<byte> payload) =>
        TryGetRecordPayload(mkb, RECORD_MEDIA_KEY_DATA, out payload);

    /// <summary>
    ///     Get the 16-byte Verify Media Key Data <c>Dv</c> from record 0x81 or 0x86, depending on the MKB
    ///     type as reported by the Type and Version record. Mirrors <c>mkb_mk_dv</c> in libaacs.
    /// </summary>
    /// <param name="mkb">Raw MKB bytes.</param>
    /// <param name="dv">Receives the 16-byte Dv.</param>
    /// <returns><c>true</c> if the record was found and at least 16 bytes long.</returns>
    public static bool TryGetVerifyMediaKey(ReadOnlySpan<byte> mkb, out ReadOnlySpan<byte> dv)
    {
        dv = default;

        byte recordType = RECORD_VERIFY_MEDIA_KEY;

        if(TryGetTypeAndVersion(mkb, out uint mkbType, out _))
        {
            if(mkbType == MKB_20_CATEGORY_C || mkbType == MKB_21_CATEGORY_C) recordType = RECORD_VERIFY_MEDIA_KEY_CAT_C;
        }

        if(!TryGetRecordPayload(mkb, recordType, out ReadOnlySpan<byte> payload)) return false;

        if(payload.Length < 16) return false;

        dv = payload[..16];

        return true;
    }

    /// <summary>
    ///     Count the number of valid (u_mask_shift, uv) tuples in a subset-difference payload, stopping
    ///     when the high bits of <c>u_mask_shift</c> mark the end of the list (mask byte AND 0xC0 != 0)
    ///     or when the row's <c>uv</c> is zero.
    /// </summary>
    /// <param name="subsetDifferencePayload">The record 0x04 payload.</param>
    /// <returns>The number of usable rows.</returns>
    public static int CountUsableRows(ReadOnlySpan<byte> subsetDifferencePayload)
    {
        int count = 0;

        for(int i = 0; i + 5 <= subsetDifferencePayload.Length; i += 5)
        {
            byte uMaskShift = subsetDifferencePayload[i];

            if((uMaskShift & 0xC0) != 0) break;

            uint uv = (uint)((subsetDifferencePayload[i + 1] << 24) |
                             (subsetDifferencePayload[i + 2] << 16) |
                             (subsetDifferencePayload[i + 3] << 8)  |
                             subsetDifferencePayload[i + 4]);

            if(uv == 0) continue;

            count++;
        }

        return count;
    }

    /// <summary>Read the <paramref name="rowIndex" />-th 5-byte tuple from a subset-difference payload.</summary>
    /// <param name="subsetDifferencePayload">The record 0x04 payload.</param>
    /// <param name="rowIndex">Tuple index (0-based).</param>
    /// <param name="uMaskShift">Receives the U-mask shift byte.</param>
    /// <param name="uv">Receives the 32-bit big-endian UV.</param>
    /// <param name="uvBytes">Receives a 4-byte slice of the UV (big-endian) for use as MAC/MK input.</param>
    /// <returns><c>true</c> if the tuple was read.</returns>
    public static bool TryGetRow(ReadOnlySpan<byte>     subsetDifferencePayload,
                                 int                    rowIndex,
                                 out byte               uMaskShift,
                                 out uint               uv,
                                 out ReadOnlySpan<byte> uvBytes)
    {
        uMaskShift = 0;
        uv         = 0;
        uvBytes    = default;

        int offset = rowIndex * 5;

        if(offset + 5 > subsetDifferencePayload.Length) return false;

        uMaskShift = subsetDifferencePayload[offset];
        uvBytes    = subsetDifferencePayload.Slice(offset + 1, 4);
        uv         = (uint)((uvBytes[0] << 24) | (uvBytes[1] << 16) | (uvBytes[2] << 8) | uvBytes[3]);

        return true;
    }
}