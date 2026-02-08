// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Compression;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
{
    /// <summary>Reads a metadata block from the filesystem</summary>
    /// <param name="position">Absolute byte position of the metadata block</param>
    /// <param name="data">Output buffer containing the decompressed metadata</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadMetadataBlock(ulong position, out byte[] data) =>
        ReadMetadataBlockWithNext(position, out data, out _);

    /// <summary>Reads a metadata block from the filesystem and returns the next block position</summary>
    /// <param name="position">Absolute byte position of the metadata block</param>
    /// <param name="data">Output buffer containing the decompressed metadata</param>
    /// <param name="nextPosition">Output: Position of the next metadata block</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadMetadataBlockWithNext(ulong position, out byte[] data, out ulong nextPosition)
    {
        data         = null;
        nextPosition = 0;

        // Calculate which image sector contains this position
        ulong byteOffset     = _partition.Start * _imagePlugin.Info.SectorSize + position;
        ulong sectorNumber   = byteOffset / _imagePlugin.Info.SectorSize;
        var   offsetInSector = (uint)(byteOffset % _imagePlugin.Info.SectorSize);

        // Read enough sectors to get the metadata block header and data
        // Metadata blocks can be up to SQUASHFS_METADATA_SIZE (8192) + 2 bytes header
        uint sectorsToRead = (SQUASHFS_METADATA_SIZE + 2 + offsetInSector + _imagePlugin.Info.SectorSize - 1) /
                             _imagePlugin.Info.SectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading metadata block at position 0x{0:X16}: {1}", position, errno);

            return errno;
        }

        // Read the 2-byte header (size with compression flag)
        ushort header = _littleEndian
                            ? BitConverter.ToUInt16(sectorData, (int)offsetInSector)
                            : (ushort)(sectorData[offsetInSector] << 8 | sectorData[offsetInSector + 1]);

        bool compressed = (header & SQUASHFS_COMPRESSED_BIT) == 0;
        int  blockSize  = header & ~SQUASHFS_COMPRESSED_BIT;

        AaruLogging.Debug(MODULE_NAME,
                          "Metadata block: header=0x{0:X4}, compressed={1}, size={2}",
                          header,
                          compressed,
                          blockSize);

        if(blockSize > SQUASHFS_METADATA_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid metadata block size: {0}", blockSize);

            return ErrorNumber.InvalidArgument;
        }

        // Calculate the position of the next metadata block
        nextPosition = position + 2 + (ulong)blockSize;

        // Extract the block data (skip 2-byte header)
        var blockData = new byte[blockSize];
        Array.Copy(sectorData, offsetInSector + 2, blockData, 0, blockSize);

        if(!compressed)
        {
            data = blockData;

            return ErrorNumber.NoError;
        }

        // Decompress the data based on compression type
        data = new byte[SQUASHFS_METADATA_SIZE];

        int decompressedSize = DecompressBlock(blockData, data);

        if(decompressedSize < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Error decompressing metadata block");

            return ErrorNumber.InvalidArgument;
        }

        // Resize to actual decompressed size
        Array.Resize(ref data, decompressedSize);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads inode data starting at a given position, handling block spanning</summary>
    /// <param name="inodeBlock">Block offset from inode table start</param>
    /// <param name="inodeOffset">Offset within the metadata block</param>
    /// <param name="size">Number of bytes to read</param>
    /// <param name="data">Output buffer containing the inode data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInodeData(uint inodeBlock, ushort inodeOffset, int size, out byte[] data)
    {
        data = null;

        // Calculate absolute position of the inode
        ulong inodePosition = _superBlock.inode_table_start + inodeBlock;

        // Read the first metadata block
        ErrorNumber errno = ReadMetadataBlockWithNext(inodePosition, out byte[] blockData, out ulong nextPosition);

        if(errno != ErrorNumber.NoError) return errno;

        if(blockData == null) return ErrorNumber.InvalidArgument;

        int available = blockData.Length - inodeOffset;

        // If all data is in this block, just copy it
        if(available >= size)
        {
            data = new byte[size];
            Array.Copy(blockData, inodeOffset, data, 0, size);

            return ErrorNumber.NoError;
        }

        // Data spans multiple blocks - need to read more
        data = new byte[size];
        var copied = 0;

        // Copy what we have from the first block
        if(available > 0)
        {
            Array.Copy(blockData, inodeOffset, data, 0, available);
            copied = available;
        }

        // Read subsequent blocks until we have all the data
        while(copied < size)
        {
            errno = ReadMetadataBlockWithNext(nextPosition, out blockData, out nextPosition);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading continuation metadata block: {0}", errno);

                return errno;
            }

            if(blockData == null || blockData.Length == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Empty continuation metadata block");

                return ErrorNumber.InvalidArgument;
            }

            int toCopy = Math.Min(blockData.Length, size - copied);
            Array.Copy(blockData, 0, data, copied, toCopy);
            copied += toCopy;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Decompresses a data block using the filesystem's compression algorithm</summary>
    /// <param name="source">Compressed data</param>
    /// <param name="destination">Buffer for decompressed data</param>
    /// <returns>Number of decompressed bytes, or -1 on error</returns>
    int DecompressBlock(byte[] source, byte[] destination)
    {
        try
        {
            switch((SquashCompression)_superBlock.compression)
            {
                case SquashCompression.Zlib:
                    return DecompressZlib(source, destination);

                case SquashCompression.Lzma:
                    // LZMA in SquashFS has a 13-byte header:
                    // - Bytes 0-4: LZMA properties (5 bytes)
                    // - Bytes 5-12: Uncompressed size as 8-byte little-endian
                    // - Bytes 13+: Compressed data
                    const int lzmaHeaderSize = 13;
                    const int lzmaPropsSize  = 5;

                    if(source.Length < lzmaHeaderSize)
                    {
                        AaruLogging.Debug(MODULE_NAME, "LZMA data too short: {0} bytes", source.Length);

                        return -1;
                    }

                    // Extract properties (first 5 bytes)
                    var lzmaProps = new byte[lzmaPropsSize];
                    Array.Copy(source, 0, lzmaProps, 0, lzmaPropsSize);

                    // Extract compressed data (skip 13-byte header)
                    var lzmaData = new byte[source.Length - lzmaHeaderSize];
                    Array.Copy(source, lzmaHeaderSize, lzmaData, 0, lzmaData.Length);

                    return LZMA.DecodeBuffer(lzmaData, destination, lzmaProps);

                case SquashCompression.Lzo:
                    return LZO.DecodeBuffer(source, destination, LZO.Algorithm.LZO1X);

                case SquashCompression.Xz:
                    return XZ.DecodeBuffer(source, destination);

                case SquashCompression.Lz4:
                    return LZ4.DecodeBuffer(source, destination);

                case SquashCompression.Zstd:
                    return ZSTD.DecodeBuffer(source, destination);

                default:
                    AaruLogging.Debug(MODULE_NAME, "Unsupported compression: {0}", _superBlock.compression);

                    return -1;
            }
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "Decompression error: {0}", ex.Message);

            return -1;
        }
    }

    /// <summary>Decompresses zlib-compressed data</summary>
    /// <param name="source">Compressed data (with zlib header)</param>
    /// <param name="destination">Buffer for decompressed data</param>
    /// <returns>Number of decompressed bytes, or -1 on error</returns>
    static int DecompressZlib(byte[] source, byte[] destination)
    {
        if(source.Length < 2) return -1;

        // Check for zlib header (CMF byte with compression method 8 = deflate)
        byte cmf = source[0];
        byte flg = source[1];

        // Verify zlib header
        // CMF bits 0-3: compression method (8 = deflate)
        // CMF bits 4-7: compression info (window size)
        // FLG bits 0-4: check bits
        // FLG bit 5: FDICT (preset dictionary)
        // FLG bits 6-7: compression level
        bool hasZlibHeader = (cmf & 0x0F) == 8 && (cmf * 256 + flg) % 31 == 0;

        var dataOffset = 0;
        int dataLength = source.Length;

        if(hasZlibHeader)
        {
            // Skip the 2-byte zlib header
            dataOffset =  2;
            dataLength -= 2;

            // Also skip the 4-byte Adler-32 checksum at the end if present
            if(dataLength >= 4) dataLength -= 4;
        }

        using var compressedStream = new MemoryStream(source, dataOffset, dataLength);
        using var deflateStream    = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var outputStream     = new MemoryStream();

        deflateStream.CopyTo(outputStream);

        byte[] result = outputStream.ToArray();

        if(result.Length > destination.Length) return -1;

        Array.Copy(result, 0, destination, 0, result.Length);

        return result.Length;
    }
}