// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Compression.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Extent decompression methods.
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
using System.IO.Compression;
using Aaru.Compression;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
    /// <summary>Decompresses extent data using the specified compression algorithm</summary>
    /// <param name="compressedData">The compressed data bytes</param>
    /// <param name="uncompressedSize">The expected uncompressed size</param>
    /// <param name="compression">The compression type (ZLIB, LZO, ZSTD)</param>
    /// <returns>The decompressed data, or null if the compression type is unsupported</returns>
    static byte[] DecompressExtent(byte[] compressedData, uint uncompressedSize, byte compression)
    {
        switch(compression)
        {
            case BTRFS_COMPRESS_ZLIB:
            {
                var       output = new byte[uncompressedSize];
                using var ms     = new MemoryStream(compressedData);
                using var zlib   = new ZLibStream(ms, CompressionMode.Decompress);
                var       offset = 0;

                while(offset < (int)uncompressedSize)
                {
                    int read = zlib.Read(output, offset, (int)uncompressedSize - offset);

                    if(read == 0) break;

                    offset += read;
                }

                return output;
            }

            case BTRFS_COMPRESS_LZO:
                return DecompressBtrfsLzo(compressedData, uncompressedSize);

            case BTRFS_COMPRESS_ZSTD:
            {
                var output = new byte[uncompressedSize];
                ZSTD.DecodeBuffer(compressedData, output);

                return output;
            }

            default:
                return null;
        }
    }

    /// <summary>Decompresses BTRFS LZO page-wrapped data</summary>
    /// <param name="compressedData">The compressed data in BTRFS LZO container format</param>
    /// <param name="uncompressedSize">The expected total uncompressed size</param>
    /// <returns>The decompressed data</returns>
    static byte[] DecompressBtrfsLzo(byte[] compressedData, uint uncompressedSize)
    {
        var decompressed = new byte[uncompressedSize];

        // First 4 bytes are the total compressed payload length (LE), skip them
        var pos       = 4;
        var decompPos = 0;

        while(pos < compressedData.Length && decompPos < (int)uncompressedSize)
        {
            if(pos + 4 > compressedData.Length) break;

            var segLen = BitConverter.ToUInt32(compressedData, pos);
            pos += 4;

            if(segLen == 0 || pos + (int)segLen > compressedData.Length) break;

            int pageSize = Math.Min(4096, (int)uncompressedSize - decompPos);
            var page     = new byte[pageSize];
            var segment  = new byte[segLen];

            Array.Copy(compressedData, pos, segment, 0, (int)segLen);

            int decoded = LZO.DecodeBuffer(segment, page, LZO.Algorithm.LZO1X);

            if(decoded > 0)
            {
                Array.Copy(page, 0, decompressed, decompPos, decoded);
                decompPos += decoded;
            }

            pos += (int)segLen;

            // Align position to 4-byte boundary for next segment
            pos = pos + 3 & ~3;
        }

        return decompressed;
    }
}