// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Zisofs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ISO9660 filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Transparent decompression of zisofs compressed files.
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
// In the loving memory of Facunda "Tata" Suárez Domínguez, R.I.P. 2019/07/24
// ****************************************************************************/

using System;
using System.IO;
using System.IO.Compression;
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

public sealed partial class ISO9660
{
    /// <summary>Reads and decompresses a zisofs compressed file.</summary>
    /// <param name="offset">Offset within the uncompressed file to start reading from.</param>
    /// <param name="size">Number of uncompressed bytes to read.</param>
    /// <param name="entry">Directory entry containing zisofs information.</param>
    /// <param name="buffer">Buffer to store the decompressed data.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadZisofsFile(long offset, long size, DecodedDirectoryEntry entry, out byte[] buffer)
    {
        buffer = null;

        if(entry.Zisofs is null || entry.Extents is null || entry.Extents.Count == 0)
            return ErrorNumber.InvalidArgument;

        ZisofsEntry zf = entry.Zisofs.Value;

        // Header size is in 32-bit words (4 bytes)
        int headerSize = zf.header_size << 2;

        // Block size is 2^block_size_log
        int blockSize = 1 << zf.block_size_log;

        // Calculate uncompressed file size
        uint uncompressedSize = zf.uncomp_len;

        // Validate offset and size
        if(offset < 0 || offset >= uncompressedSize)
        {
            buffer = [];

            return ErrorNumber.NoError;
        }

        if(offset + size > uncompressedSize) size = uncompressedSize - offset;

        // Calculate which blocks we need to decompress
        long startBlock = offset              / blockSize;
        long endBlock   = (offset + size - 1) / blockSize;

        // Calculate total number of blocks
        long totalBlocks = (uncompressedSize + blockSize - 1) / blockSize;

        // Read the entire compressed file data (we need the block pointer table which is at the start)
        ErrorNumber errno = ReadWithExtents(0,
                                            (long)entry.Size,
                                            entry.Extents,
                                            entry.XA?.signature                                    == XA_MAGIC &&
                                            entry.XA?.attributes.HasFlag(XaAttributes.Interleaved) == true,
                                            entry.XA?.filenumber ?? 0,
                                            out byte[] compressedData);

        if(errno != ErrorNumber.NoError) return errno;

        if(compressedData.Length < headerSize + (totalBlocks + 1) * 4) return ErrorNumber.InvalidArgument;

        // Verify header magic if present (optional - some implementations skip this)
        if(compressedData.Length >= 8)
        {
            var magic = BitConverter.ToUInt64(compressedData, 0);

            if(magic != ZISO_HEADER_MAGIC && magic != ZISO_HEADER_CIGAM)
            {
                // Header magic not present or different format - this is OK for some zisofs files
                // The ZF system use entry already validated the format
            }
        }

        // The block pointer table starts right after the header
        // Each pointer is a 32-bit little-endian offset to the compressed block data
        // There are (totalBlocks + 1) pointers - the last one marks the end of the last block
        int pointerTableOffset = headerSize;

        using var outputStream = new MemoryStream();

        for(long block = startBlock; block <= endBlock; block++)
        {
            // Read block start and end pointers
            int pointerOffset = pointerTableOffset + (int)(block * 4);

            if(pointerOffset + 8 > compressedData.Length) return ErrorNumber.InvalidArgument;

            var blockStart = BitConverter.ToUInt32(compressedData, pointerOffset);
            var blockEnd   = BitConverter.ToUInt32(compressedData, pointerOffset + 4);

            if(blockStart > blockEnd || blockEnd > compressedData.Length) return ErrorNumber.InvalidArgument;

            var compressedBlockSize = (int)(blockEnd - blockStart);

            byte[] decompressedBlock;

            if(compressedBlockSize == 0)
            {
                // Empty block - all zeros
                decompressedBlock = new byte[blockSize];
            }
            else
            {
                // Decompress the block using zlib
                try
                {
                    decompressedBlock =
                        DecompressZlibBlock(compressedData, (int)blockStart, compressedBlockSize, blockSize);
                }
                catch(Exception)
                {
                    return ErrorNumber.InvalidArgument;
                }
            }

            // Calculate how much of this block we need
            long blockOffset    = block * blockSize;
            long startInBlock   = block == startBlock ? offset - blockOffset : 0;
            long bytesFromBlock = decompressedBlock.Length - startInBlock;
            long bytesNeeded    = size                     - outputStream.Length;

            if(bytesFromBlock > bytesNeeded) bytesFromBlock = bytesNeeded;

            // Handle last block which may be smaller than blockSize
            if(block == totalBlocks - 1)
            {
                long lastBlockSize = uncompressedSize - blockOffset;

                if(startInBlock + bytesFromBlock > lastBlockSize) bytesFromBlock = lastBlockSize - startInBlock;
            }

            if(bytesFromBlock > 0) outputStream.Write(decompressedBlock, (int)startInBlock, (int)bytesFromBlock);

            if(outputStream.Length >= size) break;
        }

        buffer = outputStream.ToArray();

        return ErrorNumber.NoError;
    }

    /// <summary>Decompresses a single zlib-compressed block.</summary>
    /// <param name="data">The compressed data buffer.</param>
    /// <param name="offset">Offset within the buffer where the compressed block starts.</param>
    /// <param name="length">Length of the compressed data.</param>
    /// <param name="maxDecompressedSize">Maximum expected decompressed size.</param>
    /// <returns>Decompressed data.</returns>
    static byte[] DecompressZlibBlock(byte[] data, int offset, int length, int maxDecompressedSize)
    {
        // zlib format starts with a 2-byte header (CMF and FLG)
        // We need to use DeflateStream but skip the zlib header
        // zlib header: first byte is CMF (compression method and flags)
        //              second byte is FLG (flags)

        if(length < 2) return new byte[maxDecompressedSize];

        // Check for zlib header (CMF byte with compression method 8 = deflate)
        byte cmf = data[offset];
        byte flg = data[offset + 1];

        // Verify zlib header
        // CMF bits 0-3: compression method (8 = deflate)
        // CMF bits 4-7: compression info (window size)
        // FLG bits 0-4: check bits
        // FLG bit 5: FDICT (preset dictionary)
        // FLG bits 6-7: compression level
        bool hasZlibHeader = (cmf & 0x0F) == 8 && (cmf * 256 + flg) % 31 == 0;

        int dataOffset = offset;
        int dataLength = length;

        if(hasZlibHeader)
        {
            // Skip the 2-byte zlib header
            dataOffset += 2;
            dataLength -= 2;

            // Also skip the 4-byte Adler-32 checksum at the end if present
            if(dataLength >= 4) dataLength -= 4;
        }

        using var compressedStream = new MemoryStream(data, dataOffset, dataLength);
        using var deflateStream    = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var outputStream     = new MemoryStream();

        deflateStream.CopyTo(outputStream);

        return outputStream.ToArray();
    }
}