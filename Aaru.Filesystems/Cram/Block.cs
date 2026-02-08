// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Cram
{
    /// <summary>Reads bytes from the filesystem</summary>
    /// <param name="offset">Byte offset from start of filesystem</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="data">Output data buffer</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBytes(uint offset, uint length, out byte[] data)
    {
        data = null;

        // Add base offset
        uint actualOffset = _baseOffset + offset;

        // Calculate sector and offset
        uint sectorSize     = _imagePlugin.Info.SectorSize;
        uint startSector    = actualOffset / sectorSize;
        var  offsetInSector = (int)(actualOffset                                % sectorSize);
        var  sectorsToRead  = (uint)((offsetInSector + length + sectorSize - 1) / sectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + startSector,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] sectorData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        data = new byte[length];

        if(offsetInSector + length <= sectorData.Length)
            Array.Copy(sectorData, offsetInSector, data, 0, length);
        else
            Array.Copy(sectorData, offsetInSector, data, 0, sectorData.Length - offsetInSector);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and decompresses a single block from a file</summary>
    /// <param name="fileNode">The file node</param>
    /// <param name="blockIndex">Block index (0-based)</param>
    /// <param name="blockData">Output decompressed block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlock(CramFileNode fileNode, int blockIndex, out byte[] blockData)
    {
        blockData = null;

        if(blockIndex < 0 || blockIndex >= fileNode.BlockCount) return ErrorNumber.InvalidArgument;

        // Read the block pointer
        uint blkPtrOffset = fileNode.BlockPtrOffset + (uint)(blockIndex * 4);

        ErrorNumber errno = ReadBytes(blkPtrOffset, 4, out byte[] ptrData);

        if(errno != ErrorNumber.NoError) return errno;

        uint blockPtr = _littleEndian
                            ? BitConverter.ToUInt32(ptrData, 0)
                            : (uint)(ptrData[0] << 24 | ptrData[1] << 16 | ptrData[2] << 8 | ptrData[3]);

        bool uncompressed = (blockPtr & CRAMFS_BLK_FLAG_UNCOMPRESSED) != 0;
        bool direct       = (blockPtr & CRAMFS_BLK_FLAG_DIRECT_PTR)   != 0;

        blockPtr &= ~CRAMFS_BLK_FLAGS;

        uint blockStart;
        uint blockLen;

        if(direct)
        {
            // Direct pointer: absolute start pointer shifted by 2 bits
            blockStart = blockPtr << CRAMFS_BLK_DIRECT_PTR_SHIFT;

            if(uncompressed)
            {
                // Uncompressed: size is PAGE_SIZE (or less for last block)
                blockLen = PAGE_SIZE;

                if(blockIndex == fileNode.BlockCount - 1)
                {
                    // Last block: cap to remaining file length
                    blockLen = (uint)(fileNode.Length % PAGE_SIZE);

                    if(blockLen == 0) blockLen = PAGE_SIZE;
                }
            }
            else
            {
                // Compressed: size is in first 2 bytes
                errno = ReadBytes(blockStart, 2, out byte[] sizeData);

                if(errno != ErrorNumber.NoError) return errno;

                blockLen = _littleEndian ? BitConverter.ToUInt16(sizeData, 0) : (uint)(sizeData[0] << 8 | sizeData[1]);

                blockStart += 2;
            }
        }
        else
        {
            // Non-direct: block pointer indicates end of current block
            // Start comes from end of block pointer table (for first block) or previous block's pointer
            blockStart = fileNode.BlockPtrOffset + fileNode.BlockCount * 4;

            if(blockIndex > 0)
            {
                // Read previous block's pointer to get start
                errno = ReadBytes(blkPtrOffset - 4, 4, out byte[] prevPtrData);

                if(errno != ErrorNumber.NoError) return errno;

                uint prevPtr = _littleEndian
                                   ? BitConverter.ToUInt32(prevPtrData, 0)
                                   : (uint)(prevPtrData[0] << 24 |
                                            prevPtrData[1] << 16 |
                                            prevPtrData[2] << 8  |
                                            prevPtrData[3]);

                // Handle case where previous pointer is direct
                if((prevPtr & CRAMFS_BLK_FLAG_DIRECT_PTR) != 0)
                {
                    uint prevStart = (prevPtr & ~CRAMFS_BLK_FLAGS) << CRAMFS_BLK_DIRECT_PTR_SHIFT;

                    if((prevPtr & CRAMFS_BLK_FLAG_UNCOMPRESSED) != 0)
                        blockStart = prevStart + PAGE_SIZE;
                    else
                    {
                        // Read previous block's size
                        errno = ReadBytes(prevStart, 2, out byte[] prevSizeData);

                        if(errno != ErrorNumber.NoError) return errno;

                        uint prevLen = _littleEndian
                                           ? BitConverter.ToUInt16(prevSizeData, 0)
                                           : (uint)(prevSizeData[0] << 8 | prevSizeData[1]);

                        blockStart = prevStart + 2 + prevLen;
                    }
                }
                else
                    blockStart = prevPtr & ~CRAMFS_BLK_FLAGS;
            }

            blockLen = (blockPtr & ~CRAMFS_BLK_FLAGS) - blockStart;
        }

        // Handle hole (zero-length block)
        if(blockLen == 0)
        {
            blockData = new byte[PAGE_SIZE];

            return ErrorNumber.NoError;
        }

        // Sanity check block length
        if(blockLen > 2 * PAGE_SIZE || uncompressed && blockLen > PAGE_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadBlock: bad block length {0}", blockLen);

            return ErrorNumber.InvalidArgument;
        }

        // Read the block data
        errno = ReadBytes(blockStart, blockLen, out byte[] compressedData);

        if(errno != ErrorNumber.NoError) return errno;

        if(uncompressed)
        {
            // Pad to PAGE_SIZE if needed
            if(compressedData.Length >= PAGE_SIZE)
            {
                blockData = compressedData;

                return ErrorNumber.NoError;
            }

            blockData = new byte[PAGE_SIZE];
            Array.Copy(compressedData, blockData, compressedData.Length);

            return ErrorNumber.NoError;
        }

        // Decompress data
        // Standard cramfs uses zlib compression (with header, not raw deflate)
        // Note: The CramCompression enum exists for potential future extensions,
        // but standard cramfs only uses zlib. There's no flag in the superblock
        // to indicate alternative compression methods.
        try
        {
            blockData = new byte[PAGE_SIZE];

            using var compressedStream = new MemoryStream(compressedData);
            using var zlibStream       = new ZLibStream(compressedStream, CompressionMode.Decompress);

            var totalRead = 0;

            while(totalRead < PAGE_SIZE)
            {
                int bytesDecompressed = zlibStream.Read(blockData, totalRead, PAGE_SIZE - totalRead);

                if(bytesDecompressed == 0) break;

                totalRead += bytesDecompressed;
            }

            return ErrorNumber.NoError;
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadBlock: decompression failed: {0}", ex);

            return ErrorNumber.InvalidArgument;
        }
    }
}