// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Identify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies Disk eXPress (DXP) disk images.
//
//     Based on the work of Michal Necasek (fdimg).
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
using System.IO;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Images;

public sealed partial class Dxp
{
#region IMediaImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();

        return FindHeaderOffset(stream) >= 0;
    }

#endregion

    /// <summary>
    ///     Locate the DXP header within <paramref name="stream" />. DXP images are either "bare" (the header sits at
    ///     offset 0) or self-extracting DOS/OS2 executables (the header sits after the MZ stub plus a 4-byte checksum).
    /// </summary>
    /// <returns>Offset of the DXP header in <paramref name="stream" />, or -1 if not found.</returns>
    static long FindHeaderOffset(Stream stream)
    {
        if(stream.Length < DXP_HEADER_SIZE) return -1;

        stream.Seek(0, SeekOrigin.Begin);

        var word = new byte[2];

        if(stream.EnsureRead(word, 0, 2) != 2) return -1;

        var sig = (ushort)(word[0] | word[1] << 8);

        // Bare DXP image.
        if(sig == DXP_SIG) return 0;

        // Otherwise it must start with an MZ DOS executable.
        if(word[0] != 'M' || word[1] != 'Z') return -1;

        long fileSize = stream.Length;

        if(fileSize <= 0) return -1;

        // e_cblp at offset 2: bytes in last block.
        if(stream.EnsureRead(word, 0, 2) != 2) return -1;
        var lastBlkSz = (ushort)(word[0] | word[1] << 8);

        if(lastBlkSz > 512) return -1;

        // e_cp at offset 4: number of 512-byte blocks in the image.
        if(stream.EnsureRead(word, 0, 2) != 2) return -1;
        var nBlks = (ushort)(word[0] | word[1] << 8);

        if(nBlks > 256) return -1;

        long dosSz = nBlks * 512L - (512 - lastBlkSz);

        if(dosSz > fileSize) return -1;

        long hdrOfs = dosSz + 4;

        if(hdrOfs + DXP_HEADER_SIZE > fileSize) return -1;

        stream.Seek(hdrOfs, SeekOrigin.Begin);

        if(stream.EnsureRead(word, 0, 2) != 2) return -1;

        sig = (ushort)(word[0] | word[1] << 8);

        return sig == DXP_SIG ? hdrOfs : -1;
    }

    /// <summary>Compute the DXP CRC-32 over <paramref name="data" /> using the given seed. Result is not inverted.</summary>
    static uint DxpCrc32(ReadOnlySpan<byte> data, uint seed)
    {
        const uint poly = 0xEDB88320U;
        uint       crc  = seed;

        for(var i = 0; i < data.Length; i++)
        {
            crc ^= data[i];

            for(var b = 0; b < 8; b++) crc = crc >> 1 ^ (crc & 1) * poly;
        }

        return crc;
    }

    /// <summary>Find which CRC-32 seed matches the stored header CRC. Returns 0 when no known seed matches.</summary>
    static uint FindCrcSeed(byte[] rawHeader)
    {
        if(rawHeader.Length < DXP_HEADER_SIZE) return 0;

        var stored = (uint)(rawHeader[DXP_CRC_HDR_OFFSET]           |
                            rawHeader[DXP_CRC_HDR_OFFSET + 1] << 8  |
                            rawHeader[DXP_CRC_HDR_OFFSET + 2] << 16 |
                            rawHeader[DXP_CRC_HDR_OFFSET + 3] << 24);

        ReadOnlySpan<byte> covered = rawHeader.AsSpan(0, DXP_CRC_HDR_OFFSET);

        if(DxpCrc32(covered, DXP_CRC32_SEED) == stored) return DXP_CRC32_SEED;

        if(DxpCrc32(covered, DXP_CRC32_SEED_IBM) == stored) return DXP_CRC32_SEED_IBM;

        return 0;
    }
}