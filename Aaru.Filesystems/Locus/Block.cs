// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin
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

/// <inheritdoc />
public sealed partial class Locus
{
    /// <summary>Reads a filesystem block</summary>
    /// <param name="blockNumber">Block number to read</param>
    /// <param name="data">The read block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlock(int blockNumber, out byte[] data)
    {
        data = null;

        if(blockNumber < 0 || blockNumber >= _superblock.s_fsize)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid block number: {0}", blockNumber);

            return ErrorNumber.InvalidArgument;
        }

        uint sectorSize      = _imagePlugin.Info.SectorSize;
        uint sectorsPerBlock = (uint)_blockSize / sectorSize;

        ulong sectorNumber = _partition.Start + (ulong)blockNumber * sectorsPerBlock;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, sectorsPerBlock, out data, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading block {0}: {1}", blockNumber, errno);

            return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an indirect block and its data</summary>
    /// <param name="blockNum">Indirect block number</param>
    /// <param name="level">Indirection level (1=single, 2=double, 3=triple)</param>
    /// <param name="data">Data buffer to fill</param>
    /// <param name="bytesRead">Current bytes read</param>
    /// <param name="fileSize">Total file size</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadIndirectBlock(int blockNum, int level, ref byte[] data, ref int bytesRead, int fileSize)
    {
        if(blockNum == 0 || bytesRead >= fileSize) return ErrorNumber.NoError;

        ErrorNumber errno = ReadBlock(blockNum, out byte[] indirectData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading indirect block {0}: {1}", blockNum, errno);

            return errno;
        }

        int pointersPerBlock = _blockSize / 4; // 4 bytes per block pointer

        for(var i = 0; i < pointersPerBlock && bytesRead < fileSize; i++)
        {
            int offset = i * 4;

            int pointer = _bigEndian
                              ? indirectData[offset]     << 24 |
                                indirectData[offset + 1] << 16 |
                                indirectData[offset + 2] << 8  |
                                indirectData[offset + 3]
                              : indirectData[offset]           |
                                indirectData[offset + 1] << 8  |
                                indirectData[offset + 2] << 16 |
                                indirectData[offset + 3] << 24;

            if(pointer == 0)
            {
                // Sparse file
                int toFill = Math.Min(_blockSize, fileSize - bytesRead);
                bytesRead += toFill;

                continue;
            }

            if(level == 1)
            {
                // Direct data block
                errno = ReadBlock(pointer, out byte[] blockData);

                if(errno != ErrorNumber.NoError) return errno;

                int toCopy = Math.Min(blockData.Length, fileSize - bytesRead);
                Array.Copy(blockData, 0, data, bytesRead, toCopy);
                bytesRead += toCopy;
            }
            else
            {
                // Another level of indirection
                errno = ReadIndirectBlock(pointer, level - 1, ref data, ref bytesRead, fileSize);

                if(errno != ErrorNumber.NoError) return errno;
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Gets the physical block number for a logical block in a file</summary>
    /// <param name="inode">File inode</param>
    /// <param name="logicalBlock">Logical block number within the file</param>
    /// <param name="physicalBlock">Physical block number on disk (0 for sparse hole)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetPhysicalBlock(Dinode inode, int logicalBlock, out int physicalBlock)
    {
        physicalBlock = 0;

        if(inode.di_addr == null || inode.di_addr.Length < NADDR)
        {
            AaruLogging.Debug(MODULE_NAME, "GetPhysicalBlock: di_addr is null or too short");

            return ErrorNumber.InvalidArgument;
        }

        int pointersPerBlock = _blockSize / 4; // 4 bytes per block pointer

        // Direct blocks (first NDADDR = 10 blocks)
        if(logicalBlock < NDADDR)
        {
            physicalBlock = inode.di_addr[logicalBlock];

            return ErrorNumber.NoError;
        }

        // Single indirect block
        int remaining = logicalBlock - NDADDR;

        if(remaining < pointersPerBlock)
        {
            int indirectBlock = inode.di_addr[NDADDR];

            if(indirectBlock == 0) return ErrorNumber.NoError; // Sparse

            return ReadIndirectPointer(indirectBlock, remaining, out physicalBlock);
        }

        // Double indirect block
        remaining -= pointersPerBlock;

        if(remaining < pointersPerBlock * pointersPerBlock)
        {
            int doubleIndirectBlock = inode.di_addr[NDADDR + 1];

            if(doubleIndirectBlock == 0) return ErrorNumber.NoError; // Sparse

            int firstIndex  = remaining / pointersPerBlock;
            int secondIndex = remaining % pointersPerBlock;

            ErrorNumber errno = ReadIndirectPointer(doubleIndirectBlock, firstIndex, out int indirectBlock);

            if(errno != ErrorNumber.NoError) return errno;

            if(indirectBlock == 0) return ErrorNumber.NoError; // Sparse

            return ReadIndirectPointer(indirectBlock, secondIndex, out physicalBlock);
        }

        // Triple indirect block
        remaining -= pointersPerBlock * pointersPerBlock;

        int tripleIndirectBlock = inode.di_addr[NDADDR + 2];

        if(tripleIndirectBlock == 0) return ErrorNumber.NoError; // Sparse

        int firstIdx  = remaining / (pointersPerBlock * pointersPerBlock);
        int remainder = remaining % (pointersPerBlock * pointersPerBlock);
        int secondIdx = remainder / pointersPerBlock;
        int thirdIdx  = remainder % pointersPerBlock;

        ErrorNumber err = ReadIndirectPointer(tripleIndirectBlock, firstIdx, out int doubleBlock);

        if(err != ErrorNumber.NoError) return err;

        if(doubleBlock == 0) return ErrorNumber.NoError; // Sparse

        err = ReadIndirectPointer(doubleBlock, secondIdx, out int singleBlock);

        if(err != ErrorNumber.NoError) return err;

        if(singleBlock == 0) return ErrorNumber.NoError; // Sparse

        return ReadIndirectPointer(singleBlock, thirdIdx, out physicalBlock);
    }

    /// <summary>Reads a block pointer from an indirect block</summary>
    /// <param name="indirectBlockNum">Indirect block number</param>
    /// <param name="index">Index within the indirect block</param>
    /// <param name="pointer">The block pointer value</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadIndirectPointer(int indirectBlockNum, int index, out int pointer)
    {
        pointer = 0;

        ErrorNumber errno = ReadBlock(indirectBlockNum, out byte[] blockData);

        if(errno != ErrorNumber.NoError) return errno;

        int offset = index * 4;

        if(offset + 4 > blockData.Length) return ErrorNumber.InvalidArgument;

        pointer = _bigEndian
                      ? blockData[offset]     << 24 |
                        blockData[offset + 1] << 16 |
                        blockData[offset + 2] << 8  |
                        blockData[offset + 3]
                      : blockData[offset]           |
                        blockData[offset + 1] << 8  |
                        blockData[offset + 2] << 16 |
                        blockData[offset + 3] << 24;

        return ErrorNumber.NoError;
    }
}