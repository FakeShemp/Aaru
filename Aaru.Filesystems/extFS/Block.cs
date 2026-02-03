// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class extFS
{
    /// <summary>Reads a block from the filesystem</summary>
    /// <param name="blockNumber">The block number to read</param>
    /// <param name="blockData">The block data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadBlock(uint blockNumber, out byte[] blockData)
    {
        blockData = null;

        // Calculate block size (1024 << log_zone_size)
        uint blockSize = 1024u << (int)_superblock.s_log_zone_size;

        // Calculate the byte offset within the partition
        ulong byteOffset = (ulong)blockNumber * blockSize;

        // Convert to sector address
        ulong sectorAddress  = byteOffset / _imagePlugin.Info.SectorSize;
        var   offsetInSector = (int)(byteOffset % _imagePlugin.Info.SectorSize);

        // Calculate how many sectors to read
        uint sectorsToRead = (blockSize + (uint)offsetInSector + _imagePlugin.Info.SectorSize - 1) /
                             _imagePlugin.Info.SectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + sectorAddress,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] sectorData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        blockData = new byte[blockSize];
        Array.Copy(sectorData, offsetInSector, blockData, 0, blockSize);

        return ErrorNumber.NoError;
    }

    /// <summary>Maps a logical block number to a physical block number</summary>
    /// <remarks>
    ///     Implements the ext_bmap logic from Linux:
    ///     - Blocks 0-8: direct blocks (i_zone[0-8])
    ///     - Block 9: single indirect (i_zone[9] points to block with addresses)
    ///     - Block 10: double indirect (i_zone[10])
    ///     - Block 11: triple indirect (i_zone[11])
    /// </remarks>
    /// <param name="inode">The file inode</param>
    /// <param name="logicalBlock">The logical block number</param>
    /// <param name="physicalBlock">The physical block number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber MapBlock(ext_inode inode, uint logicalBlock, out uint physicalBlock)
    {
        physicalBlock = 0;

        uint addressesPerBlock = (1024u << (int)_superblock.s_log_zone_size) / 4; // 4 bytes per block pointer

        // Check bounds: max blocks = 9 + 256 + 256*256 + 256*256*256
        uint maxBlocks =
            9                                     +
            addressesPerBlock                     +
            addressesPerBlock * addressesPerBlock +
            addressesPerBlock * addressesPerBlock * addressesPerBlock;

        if(logicalBlock >= maxBlocks)
        {
            AaruLogging.Debug(MODULE_NAME, "MapBlock: block {0} exceeds max {1}", logicalBlock, maxBlocks);

            return ErrorNumber.InvalidArgument;
        }

        // Direct blocks (0-8)
        if(logicalBlock < 9)
        {
            physicalBlock = inode.i_zone[logicalBlock];

            return ErrorNumber.NoError;
        }

        uint block = logicalBlock - 9;

        // Single indirect block
        if(block < addressesPerBlock)
        {
            uint indirectBlock = inode.i_zone[9];

            if(indirectBlock == 0)
            {
                physicalBlock = 0; // Sparse

                return ErrorNumber.NoError;
            }

            ErrorNumber errno = ReadBlock(indirectBlock, out byte[] indirectData);

            if(errno != ErrorNumber.NoError) return errno;

            physicalBlock = BitConverter.ToUInt32(indirectData, (int)(block * 4));

            return ErrorNumber.NoError;
        }

        block -= addressesPerBlock;

        // Double indirect block
        if(block < addressesPerBlock * addressesPerBlock)
        {
            uint dindirectBlock = inode.i_zone[10];

            if(dindirectBlock == 0)
            {
                physicalBlock = 0; // Sparse

                return ErrorNumber.NoError;
            }

            // Read first level of indirection
            ErrorNumber err = ReadBlock(dindirectBlock, out byte[] dindirectData);

            if(err != ErrorNumber.NoError) return err;

            uint indirectIndex = block / addressesPerBlock;
            var  indirectAddr  = BitConverter.ToUInt32(dindirectData, (int)(indirectIndex * 4));

            if(indirectAddr == 0)
            {
                physicalBlock = 0; // Sparse

                return ErrorNumber.NoError;
            }

            // Read second level of indirection
            err = ReadBlock(indirectAddr, out byte[] indirectData2);

            if(err != ErrorNumber.NoError) return err;

            uint blockIndex = block % addressesPerBlock;
            physicalBlock = BitConverter.ToUInt32(indirectData2, (int)(blockIndex * 4));

            return ErrorNumber.NoError;
        }

        block -= addressesPerBlock * addressesPerBlock;

        // Triple indirect block
        uint tindirectBlock = inode.i_zone[11];

        if(tindirectBlock == 0)
        {
            physicalBlock = 0; // Sparse

            return ErrorNumber.NoError;
        }

        // Read first level of indirection
        ErrorNumber errno3 = ReadBlock(tindirectBlock, out byte[] tindirectData);

        if(errno3 != ErrorNumber.NoError) return errno3;

        uint dindirectIndex = block / (addressesPerBlock * addressesPerBlock);
        var  dindirectAddr  = BitConverter.ToUInt32(tindirectData, (int)(dindirectIndex * 4));

        if(dindirectAddr == 0)
        {
            physicalBlock = 0; // Sparse

            return ErrorNumber.NoError;
        }

        // Read second level of indirection
        errno3 = ReadBlock(dindirectAddr, out byte[] dindirectData2);

        if(errno3 != ErrorNumber.NoError) return errno3;

        uint indirectIndex2 = block / addressesPerBlock % addressesPerBlock;
        var  indirectAddr2  = BitConverter.ToUInt32(dindirectData2, (int)(indirectIndex2 * 4));

        if(indirectAddr2 == 0)
        {
            physicalBlock = 0; // Sparse

            return ErrorNumber.NoError;
        }

        // Read third level of indirection
        errno3 = ReadBlock(indirectAddr2, out byte[] indirectData3);

        if(errno3 != ErrorNumber.NoError) return errno3;

        uint blockIndex2 = block % addressesPerBlock;
        physicalBlock = BitConverter.ToUInt32(indirectData3, (int)(blockIndex2 * 4));

        return ErrorNumber.NoError;
    }
}