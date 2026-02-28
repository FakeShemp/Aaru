// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Aaru.CommonTypes.Enums;
using Aaru.Compression;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    /// <summary>Gets data blocks via direct/indirect block pointers</summary>
    /// <param name="inode">The inode with traditional block pointers</param>
    /// <param name="blockList">List to add blocks to</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetIndirectBlocks(Inode inode, List<(ulong physicalBlock, uint length)> blockList)
    {
        ulong fileSize   = (ulong)inode.size_high << 32 | inode.size_lo;
        var   blocksUsed = (uint)((fileSize + _blockSize - 1) / _blockSize);

        uint addrsPerBlock = _blockSize / 4;
        uint blockIndex    = 0;

        // Direct blocks (0-11)
        for(uint i = 0; i < 12 && blockIndex < blocksUsed; i++, blockIndex++)
            if(inode.block[i] != 0)
                blockList.Add((inode.block[i], 1));

        // Single indirect (block[12])
        if(blockIndex < blocksUsed && inode.block[12] != 0)
        {
            ErrorNumber errno =
                ReadIndirectBlock(inode.block[12], addrsPerBlock, blocksUsed, ref blockIndex, blockList);

            if(errno != ErrorNumber.NoError) return errno;
        }

        // Double indirect (block[13])
        if(blockIndex < blocksUsed && inode.block[13] != 0)
        {
            ErrorNumber errno = ReadBytes(inode.block[13] * _blockSize, _blockSize, out byte[] dindData);

            if(errno != ErrorNumber.NoError) return errno;

            for(uint i = 0; i < addrsPerBlock && blockIndex < blocksUsed; i++)
            {
                var indBlock = BitConverter.ToUInt32(dindData, (int)(i * 4));

                if(indBlock == 0)
                {
                    blockIndex += addrsPerBlock;

                    continue;
                }

                errno = ReadIndirectBlock(indBlock, addrsPerBlock, blocksUsed, ref blockIndex, blockList);

                if(errno != ErrorNumber.NoError) return errno;
            }
        }

        // Triple indirect (block[14])
        if(blockIndex < blocksUsed && inode.block[14] != 0)
        {
            ErrorNumber errno = ReadBytes(inode.block[14] * _blockSize, _blockSize, out byte[] tindData);

            if(errno != ErrorNumber.NoError) return errno;

            for(uint i = 0; i < addrsPerBlock && blockIndex < blocksUsed; i++)
            {
                var dindBlock = BitConverter.ToUInt32(tindData, (int)(i * 4));

                if(dindBlock == 0)
                {
                    blockIndex += addrsPerBlock * addrsPerBlock;

                    continue;
                }

                errno = ReadBytes(dindBlock * _blockSize, _blockSize, out byte[] dindData);

                if(errno != ErrorNumber.NoError) return errno;

                for(uint j = 0; j < addrsPerBlock && blockIndex < blocksUsed; j++)
                {
                    var indBlock = BitConverter.ToUInt32(dindData, (int)(j * 4));

                    if(indBlock == 0)
                    {
                        blockIndex += addrsPerBlock;

                        continue;
                    }

                    errno = ReadIndirectBlock(indBlock, addrsPerBlock, blocksUsed, ref blockIndex, blockList);

                    if(errno != ErrorNumber.NoError) return errno;
                }
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads block addresses from a single indirect block</summary>
    /// <param name="indirectBlock">Physical block number of the indirect block</param>
    /// <param name="addrsPerBlock">Number of block addresses per block</param>
    /// <param name="blocksUsed">Total number of blocks used by the file</param>
    /// <param name="blockIndex">Current block index (updated on return)</param>
    /// <param name="blockList">List to add blocks to</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadIndirectBlock(uint indirectBlock, uint addrsPerBlock, uint blocksUsed, ref uint blockIndex,
                                  List<(ulong physicalBlock, uint length)> blockList)
    {
        ErrorNumber errno = ReadBytes(indirectBlock * _blockSize, _blockSize, out byte[] indData);

        if(errno != ErrorNumber.NoError) return errno;

        for(uint i = 0; i < addrsPerBlock && blockIndex < blocksUsed; i++, blockIndex++)
        {
            var blockAddr = BitConverter.ToUInt32(indData, (int)(i * 4));

            if(blockAddr != 0) blockList.Add((blockAddr, 1));
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a specified number of bytes from a byte offset within the partition</summary>
    /// <param name="byteOffset">Byte offset from the start of the partition</param>
    /// <param name="count">Number of bytes to read</param>
    /// <param name="data">The read data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBytes(ulong byteOffset, uint count, out byte[] data)
    {
        data = null;

        uint  sectorSize   = _imagePlugin.Info.SectorSize;
        ulong sectorAddr   = byteOffset / sectorSize;
        var   offsetInSect = (uint)(byteOffset % sectorSize);

        uint sectorsToRead = (count + offsetInSect + sectorSize - 1) / sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + sectorAddr,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] sectorData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(offsetInSect + count > sectorData.Length)
        {
            // If we can't read all the requested data, return what we have
            var available = (uint)(sectorData.Length - offsetInSect);

            if(available == 0) return ErrorNumber.InvalidArgument;

            data = new byte[available];
            Array.Copy(sectorData, (int)offsetInSect, data, 0, (int)available);
        }
        else
        {
            data = new byte[count];
            Array.Copy(sectorData, (int)offsetInSect, data, 0, (int)count);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a logical block of a file from its pre-computed block list</summary>
    /// <param name="blockList">The file's block list (physical block, contiguous length) pairs</param>
    /// <param name="logicalBlock">The logical block index to read</param>
    /// <param name="blockData">The block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadLogicalBlock(List<(ulong physicalBlock, uint length)> blockList, ulong logicalBlock,
                                 out byte[]                               blockData)
    {
        blockData = null;

        // Walk the block list to find which entry covers this logical block
        ulong currentLogical = 0;

        foreach((ulong physicalBlock, uint length) in blockList)
        {
            if(logicalBlock < currentLogical + length)
            {
                // Found it — compute the exact physical block
                ulong offsetInExtent = logicalBlock  - currentLogical;
                ulong targetPhysical = physicalBlock + offsetInExtent;

                return ReadBytes(targetPhysical * _blockSize, _blockSize, out blockData);
            }

            currentLogical += length;
        }

        // Logical block not found in block list — sparse/hole, return empty
        blockData = Array.Empty<byte>();

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a logical block from a compressed file, decompressing the cluster as needed</summary>
    /// <param name="fileNode">The file node with compression state and cluster cache</param>
    /// <param name="logicalBlock">The logical block index to read</param>
    /// <param name="blockData">The decompressed block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadCompressedLogicalBlock(Ext2FileNode fileNode, ulong logicalBlock, out byte[] blockData)
    {
        blockData = null;

        uint clusterNBlocks = fileNode.ClusterNBlocks;
        var  clusterIndex   = (long)(logicalBlock         / clusterNBlocks);
        var  blockInCluster = (int)(logicalBlock          % clusterNBlocks);
        var  blockOffset    = (int)((ulong)blockInCluster * _blockSize);

        // Check cluster cache
        if(fileNode.DecompressedClusterCache.TryGetValue(clusterIndex, out byte[] clusterData))
        {
            if(blockOffset + (int)_blockSize <= clusterData.Length)
            {
                blockData = new byte[_blockSize];
                Array.Copy(clusterData, blockOffset, blockData, 0, (int)_blockSize);
            }
            else if(blockOffset < clusterData.Length)
            {
                // Partial last block
                int available = clusterData.Length - blockOffset;
                blockData = new byte[_blockSize];
                Array.Copy(clusterData, blockOffset, blockData, 0, available);
            }
            else
                blockData = new byte[_blockSize];

            return ErrorNumber.NoError;
        }

        // Read the cluster and decompress it
        ErrorNumber errno = ReadAndDecompressCluster(fileNode, clusterIndex, out clusterData);

        if(errno != ErrorNumber.NoError) return errno;

        // Cache the decompressed cluster
        fileNode.DecompressedClusterCache[clusterIndex] = clusterData;

        // Extract the requested block
        if(blockOffset + (int)_blockSize <= clusterData.Length)
        {
            blockData = new byte[_blockSize];
            Array.Copy(clusterData, blockOffset, blockData, 0, (int)_blockSize);
        }
        else if(blockOffset < clusterData.Length)
        {
            int available = clusterData.Length - blockOffset;
            blockData = new byte[_blockSize];
            Array.Copy(clusterData, blockOffset, blockData, 0, available);
        }
        else
            blockData = new byte[_blockSize];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads all physical blocks of a cluster and decompresses them</summary>
    /// <param name="fileNode">The file node</param>
    /// <param name="clusterIndex">The cluster index within the file</param>
    /// <param name="decompressedData">The decompressed cluster data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadAndDecompressCluster(Ext2FileNode fileNode, long clusterIndex, out byte[] decompressedData)
    {
        decompressedData = null;

        uint  clusterNBlocks     = fileNode.ClusterNBlocks;
        ulong firstLogicalBlock  = (ulong)clusterIndex * clusterNBlocks;
        uint  clusterSizeInBytes = clusterNBlocks      * _blockSize;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadAndDecompressCluster: cluster={0}, firstBlock={1}, nblocks={2}",
                          clusterIndex,
                          firstLogicalBlock,
                          clusterNBlocks);

        // Read all blocks of the cluster
        var rawCluster = new byte[clusterSizeInBytes];
        var bytesRead  = 0;

        for(uint i = 0; i < clusterNBlocks; i++)
        {
            ulong logBlock = firstLogicalBlock + i;

            ErrorNumber errno = ReadLogicalBlock(fileNode.BlockList, logBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadAndDecompressCluster: failed reading block {0}: {1}",
                                  logBlock,
                                  errno);

                return errno;
            }

            if(blockData != null && blockData.Length > 0)
            {
                int toCopy = Math.Min(blockData.Length, (int)_blockSize);
                Array.Copy(blockData, 0, rawCluster, bytesRead, toCopy);
                bytesRead += toCopy;
            }
            else
                bytesRead += (int)_blockSize;
        }

        // Check for cluster head magic in the first 2 bytes
        var magic = BitConverter.ToUInt16(rawCluster, 0);

        if(magic != EXT2_COMPRESS_MAGIC_04X)
        {
            // Not compressed — return raw data
            AaruLogging.Debug(MODULE_NAME,
                              "ReadAndDecompressCluster: cluster {0} not compressed (magic=0x{1:X4})",
                              clusterIndex,
                              magic);

            decompressedData = rawCluster;

            return ErrorNumber.NoError;
        }

        // Parse the cluster head
        int headSize = Marshal.SizeOf<CompressedClusterHead>();

        CompressedClusterHead head =
            Marshal.ByteArrayToStructureLittleEndian<CompressedClusterHead>(rawCluster, 0, headSize);

        AaruLogging.Debug(MODULE_NAME,
                          "ReadAndDecompressCluster: method={0}, ulen={1}, clen={2}, holemap_nbytes={3}",
                          head.method,
                          head.ulen,
                          head.clen,
                          head.holemap_nbytes);

        // Calculate offset to the compressed data (after header + holemap)
        int compressedDataOffset = headSize + head.holemap_nbytes;

        if(compressedDataOffset + (int)head.clen > rawCluster.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadAndDecompressCluster: compressed data extends beyond cluster");

            return ErrorNumber.InvalidArgument;
        }

        // Extract compressed data
        var compressedData = new byte[head.clen];
        Array.Copy(rawCluster, compressedDataOffset, compressedData, 0, (int)head.clen);

        // Decompress
        decompressedData = new byte[head.ulen];

        ErrorNumber decompResult = DecompressData(head.method, compressedData, decompressedData);

        if(decompResult != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ReadAndDecompressCluster: decompression failed for method {0}: {1}",
                              head.method,
                              decompResult);

            return decompResult;
        }

        AaruLogging.Debug(MODULE_NAME, "ReadAndDecompressCluster: decompressed {0} -> {1} bytes", head.clen, head.ulen);

        return ErrorNumber.NoError;
    }

    /// <summary>Decompresses data using the specified e2compr algorithm</summary>
    /// <param name="method">e2compr algorithm id</param>
    /// <param name="compressedData">The compressed data</param>
    /// <param name="decompressedData">Pre-allocated buffer for decompressed output</param>
    /// <returns>Error number indicating success or failure</returns>
    static ErrorNumber DecompressData(byte method, byte[] compressedData, byte[] decompressedData)
    {
        switch(method)
        {
            case EXT2_GZIP_ALG:
            {
                try
                {
                    using var ms   = new MemoryStream(compressedData);
                    using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
                    var       pos  = 0;

                    while(pos < decompressedData.Length)
                    {
                        int read = zlib.Read(decompressedData, pos, decompressedData.Length - pos);

                        if(read == 0) break;

                        pos += read;
                    }

                    return ErrorNumber.NoError;
                }
                catch(Exception)
                {
                    // The e2compr gzip format uses raw deflate (no zlib header), try that
                    try
                    {
                        using var ms      = new MemoryStream(compressedData);
                        using var deflate = new DeflateStream(ms, CompressionMode.Decompress);
                        var       pos     = 0;

                        while(pos < decompressedData.Length)
                        {
                            int read = deflate.Read(decompressedData, pos, decompressedData.Length - pos);

                            if(read == 0) break;

                            pos += read;
                        }

                        return ErrorNumber.NoError;
                    }
                    catch(Exception)
                    {
                        return ErrorNumber.InvalidArgument;
                    }
                }
            }

            case EXT2_BZIP2_ALG:
            {
                int decoded = BZip2.DecodeBuffer(compressedData, decompressedData);

                return decoded > 0 ? ErrorNumber.NoError : ErrorNumber.InvalidArgument;
            }

            case EXT2_LZO_ALG:
            {
                int decoded = LZO.DecodeBuffer(compressedData, decompressedData, LZO.Algorithm.LZO1X);

                return decoded > 0 ? ErrorNumber.NoError : ErrorNumber.InvalidArgument;
            }

            case EXT2_NONE_ALG:
            {
                int toCopy = Math.Min(compressedData.Length, decompressedData.Length);
                Array.Copy(compressedData, 0, decompressedData, 0, toCopy);

                return ErrorNumber.NoError;
            }

            case EXT2_LZRW3A_ALG:
            {
                int decoded = LZRW3A.DecodeBuffer(compressedData, decompressedData);

                return decoded > 0 ? ErrorNumber.NoError : ErrorNumber.InvalidArgument;
            }

            case EXT2_LZV1_ALG:
            {
                int decoded = LZV1.DecodeBuffer(compressedData, decompressedData);

                return decoded > 0 ? ErrorNumber.NoError : ErrorNumber.InvalidArgument;
            }

            default:
                return ErrorNumber.NotSupported;
        }
    }

    /// <summary>Assembles inline data from the inode body and optional system.data ibody xattr</summary>
    /// <param name="inodeNumber">The inode number</param>
    /// <param name="inode">The inode structure</param>
    /// <param name="fileSize">The file size in bytes</param>
    /// <param name="inlineData">The assembled inline data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetInlineData(uint inodeNumber, Inode inode, ulong fileSize, out byte[] inlineData)
    {
        inlineData = null;

        // First part: up to 60 bytes from inode.block[] (EXT4_MIN_INLINE_DATA_SIZE)
        var blockBytes = new byte[60];

        for(var i = 0; i < 15; i++)
        {
            byte[] b = BitConverter.GetBytes(inode.block[i]);
            Array.Copy(b, 0, blockBytes, i * 4, 4);
        }

        if(fileSize <= 60)
        {
            inlineData = new byte[fileSize];
            Array.Copy(blockBytes, 0, inlineData, 0, (int)fileSize);

            return ErrorNumber.NoError;
        }

        // Second part: continuation data in "system.data" ibody xattr
        byte[]      xattrData = null;
        ErrorNumber errno     = ReadInlineXattrValue(inodeNumber, inode, "system.data", ref xattrData);

        if(errno != ErrorNumber.NoError || xattrData == null || xattrData.Length == 0)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "GetInlineData: no system.data xattr found for inode {0}, truncating to 60 bytes",
                              inodeNumber);

            inlineData = new byte[Math.Min(60, (int)fileSize)];
            Array.Copy(blockBytes, 0, inlineData, 0, inlineData.Length);

            return ErrorNumber.NoError;
        }

        // Concatenate block[] data + xattr continuation data, truncated to file size
        var totalSize = (int)Math.Min(fileSize, (ulong)(60 + xattrData.Length));
        inlineData = new byte[totalSize];

        int firstPart = Math.Min(60, totalSize);
        Array.Copy(blockBytes, 0, inlineData, 0, firstPart);

        if(totalSize > 60)
        {
            int secondPart = totalSize - 60;
            Array.Copy(xattrData, 0, inlineData, 60, secondPart);
        }

        AaruLogging.Debug(MODULE_NAME,
                          "GetInlineData: inode {0}, size={1}, block_part=60, xattr_part={2}",
                          inodeNumber,
                          fileSize,
                          xattrData.Length);

        return ErrorNumber.NoError;
    }
}