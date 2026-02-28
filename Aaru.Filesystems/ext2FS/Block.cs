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
using Aaru.CommonTypes.Enums;

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
        {
            if(inode.block[i] != 0) blockList.Add((inode.block[i], 1));
        }

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
}