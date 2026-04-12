// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsConvertBuffer.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Buffers 2048-byte sectors into 6144-byte AACS CPS units across reads.
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

/// <summary>Aligns contiguous 2048-byte sectors to CPS units for decrypt during image conversion.</summary>
public sealed class AacsConvertBuffer
{
    byte[] _pending = Array.Empty<byte>();
    /// <summary>LBA of the first 2048-byte sector in <see cref="_pending"/>.</summary>
    ulong _pendingStartLba;

    /// <summary>Clears any trailing sectors (e.g. at end of conversion).</summary>
    public void Reset()
    {
        _pending         = Array.Empty<byte>();
        _pendingStartLba = 0;
    }

    /// <summary>
    ///     Prepends pending data to <paramref name="sectorBuffer"/> (first <paramref name="sectorCount"/> sectors),
    ///     decrypts all complete 6144-byte units, writes decrypted bytes back into <paramref name="sectorBuffer"/>,
    ///     and stores an incomplete trailing 0–2 sectors for the next call.
    /// </summary>
    /// <param name="sectorBuffer">Sector buffer to decrypt.</param>
    /// <param name="sectorCount">Number of sectors to decrypt.</param>
    /// <param name="firstSectorLba">LBA of the first sector.</param>
    /// <param name="decryptedCpsUnitKeys">Decrypted CPS unit keys.</param>
    public void DecryptChunk(ref byte[] sectorBuffer, uint sectorCount, ulong firstSectorLba,
                             byte[][] decryptedCpsUnitKeys)
    {
        if(sectorCount == 0)
            return;

        int newBytes = (int)(sectorCount * AacsStreamDecrypt.SectorLen);

        if(sectorBuffer.Length < newBytes)
            throw new ArgumentException("sectorBuffer shorter than sectorCount implies.", nameof(sectorBuffer));

        int pl = _pending.Length;

        if(pl > 0)
        {
            ulong expected = _pendingStartLba + (ulong)(pl / AacsStreamDecrypt.SectorLen);

            if(expected != firstSectorLba)
            {
                _pending         = Array.Empty<byte>();
                _pendingStartLba = 0;
                pl               = 0;
            }
        }

        if(pl == 0)
            _pendingStartLba = firstSectorLba;

        int  totalLen = pl + newBytes;
        byte[] merged = new byte[totalLen];

        if(pl > 0)
            Buffer.BlockCopy(_pending, 0, merged, 0, pl);

        Buffer.BlockCopy(sectorBuffer, 0, merged, pl, newBytes);

        int fullUnitsBytes = totalLen / AacsStreamDecrypt.AlignedUnitLen * AacsStreamDecrypt.AlignedUnitLen;

        Span<byte> mergedSpan = merged;

        for(int o = 0; o < fullUnitsBytes; o += AacsStreamDecrypt.AlignedUnitLen)
        {
            Span<byte> unit = mergedSpan.Slice(o, AacsStreamDecrypt.AlignedUnitLen);
            AacsStreamDecrypt.TryDecryptAlignedUnit(unit, decryptedCpsUnitKeys);
        }

        int rem = totalLen - fullUnitsBytes;

        if(rem > 0)
        {
            _pending = new byte[rem];
            Buffer.BlockCopy(merged, fullUnitsBytes, _pending, 0, rem);
        }
        else
        {
            _pending         = Array.Empty<byte>();
            _pendingStartLba = 0;
        }

        Buffer.BlockCopy(merged, pl, sectorBuffer, 0, newBytes);
    }
}
