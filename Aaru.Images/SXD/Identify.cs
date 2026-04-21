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
//     Identifies Sydex CopyQM+ Self-eXtracting Disk (SXD) images.
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

public sealed partial class SXD
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
    ///     Locate the SXD header within <paramref name="stream" />. SXD images are either "bare" (the 'SXD' signature at
    ///     offset 0) or wrapped in a DOS or DOS/OS-2 self-extracting executable stub. An optional "WB" block may sit
    ///     between the stub and the SXD payload.
    /// </summary>
    /// <returns>Offset of the SXD header in <paramref name="stream" />, or -1 if not found.</returns>
    static long FindHeaderOffset(Stream stream)
    {
        if(stream.Length < SXD_HEADER_SIZE) return -1;

        stream.Seek(0, SeekOrigin.Begin);

        var first3 = new byte[3];

        if(stream.EnsureRead(first3, 0, 3) != 3) return -1;

        // Bare SXD image.
        if(SignatureMatches(first3, _sxdSignature)) return 0;

        // Otherwise must start with the 'MZ' DOS executable signature.
        if(first3[0] != 'M' || first3[1] != 'Z') return -1;

        long fileSize = stream.Length;

        long hdrOfs = CalcSkip(stream, fileSize, 0);

        if(hdrOfs < 0) return -1;

        if(hdrOfs + SXD_HEADER_SIZE > fileSize) return -1;

        stream.Seek(hdrOfs, SeekOrigin.Begin);

        if(stream.EnsureRead(first3, 0, 3) != 3) return -1;

        if(SignatureMatches(first3, _sxdSignature)) return hdrOfs;

        // A 'WB' block may precede the actual SXD payload.
        if(first3[0] != 'W' || first3[1] != 'B') return -1;

        stream.Seek(hdrOfs, SeekOrigin.Begin);

        hdrOfs = CalcSkip(stream, fileSize, hdrOfs);

        if(hdrOfs < 0) return -1;

        if(hdrOfs + SXD_HEADER_SIZE > fileSize) return -1;

        stream.Seek(hdrOfs, SeekOrigin.Begin);

        if(stream.EnsureRead(first3, 0, 3) != 3) return -1;

        return SignatureMatches(first3, _sxdSignature) ? hdrOfs : -1;
    }

    /// <summary>
    ///     Skip a DOS-style stub/block by reading its MZ-header length (bytes 2..3 = last-block bytes, bytes 4..5 =
    ///     number of 512-byte blocks) from the current position, and computing the absolute offset of what follows.
    /// </summary>
    static long CalcSkip(Stream stream, long fileSize, long startOfs)
    {
        // The caller already consumed the first two bytes of the stub/block signature; skip to offset 2 of it.
        stream.Seek(startOfs + 2, SeekOrigin.Begin);

        var word = new byte[2];

        if(stream.EnsureRead(word, 0, 2) != 2) return -1;

        var lastBlkSz = (ushort)(word[0] | word[1] << 8);

        if(lastBlkSz > 512) return -1;

        if(stream.EnsureRead(word, 0, 2) != 2) return -1;

        var nBlks = (ushort)(word[0] | word[1] << 8);

        if(nBlks > 256) return -1;

        long skipSz = nBlks * 512L - (512 - lastBlkSz);

        if(skipSz <= 0 || skipSz > fileSize) return -1;

        return startOfs + skipSz;
    }

    static bool SignatureMatches(ReadOnlySpan<byte> data, byte[] expected)
    {
        if(data.Length < expected.Length) return false;

        for(var i = 0; i < expected.Length; i++)
        {
            if(data[i] != expected[i]) return false;
        }

        return true;
    }

    /// <summary>Compute the SXD CRC-16 (reversed poly 0xA001, initial value 0, non-inverted) over <paramref name="data" />.</summary>
    static ushort SxdCrc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;

        for(var i = 0; i < data.Length; i++)
        {
            crc ^= data[i];

            for(var b = 0; b < 8; b++)
            {
                if((crc & 1) != 0)
                    crc = (ushort)(crc >> 1 ^ SXD_CRC_POLY);
                else
                    crc = (ushort)(crc >> 1);
            }
        }

        return crc;
    }
}