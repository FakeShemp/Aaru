// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DataStream.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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
public sealed partial class AtheOS
{
    /// <summary>Reads data from a data stream at a specified byte position</summary>
    /// <param name="dataStream">The data stream structure containing block runs</param>
    /// <param name="position">The byte offset within the data stream to read from</param>
    /// <param name="length">The number of bytes to read</param>
    /// <param name="buffer">Output buffer containing the read data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadFromDataStream(DataStream dataStream, long position, int length, out byte[] buffer)
    {
        buffer = null;

        AaruLogging.Debug(MODULE_NAME, "ReadFromDataStream: pos=0x{0:X8}, len={1}", position, length);

        // Check which range the position falls into
        if(position < dataStream.max_direct_range)
        {
            AaruLogging.Debug(MODULE_NAME, "Position in direct range (max={0})", dataStream.max_direct_range);

            return ReadDataStreamDirect(dataStream, position, length, out buffer);
        }

        if(position < dataStream.max_indirect_range)
        {
            AaruLogging.Debug(MODULE_NAME, "Position in indirect range");

            return ReadDataStreamIndirect(dataStream, position, length, out buffer);
        }

        if(position < dataStream.max_double_indirect_range)
        {
            AaruLogging.Debug(MODULE_NAME, "Position in double-indirect range");

            return ReadDataStreamDoubleIndirect(dataStream, position, length, out buffer);
        }

        AaruLogging.Debug(MODULE_NAME, "Position beyond all allocated ranges");

        return ErrorNumber.OutOfRange;
    }

    /// <summary>Reads from direct blocks in the data stream</summary>
    ErrorNumber ReadDataStreamDirect(DataStream dataStream, long position, int length, out byte[] buffer)
    {
        buffer = null;
        uint sectorSize = _imagePlugin.Info.SectorSize;

        AaruLogging.Debug(MODULE_NAME, "ReadDataStreamDirect: position={0}, length={1}", position, length);

        buffer = new byte[length];
        var  bytesRead         = 0;
        long currentByteOffset = 0;

        for(var i = 0; i < DIRECT_BLOCK_COUNT && bytesRead < length; i++)
        {
            if(dataStream.direct[i].len == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Direct block {0}: empty, stopping", i);

                break;
            }

            // blockStart = (ag * blocks_per_ag) + start
            long blockStart = (long)dataStream.direct[i].group * _superblock.blocks_per_ag + dataStream.direct[i].start;

            long blockLen  = dataStream.direct[i].len;
            long blockSize = blockLen * _superblock.block_size;

            AaruLogging.Debug(MODULE_NAME,
                              "Direct block {0}: AG={1}, start={2}, len={3}, blockStart={4}, blockSize={5}",
                              i,
                              dataStream.direct[i].group,
                              dataStream.direct[i].start,
                              blockLen,
                              blockStart,
                              blockSize);

            if(position >= currentByteOffset && position < currentByteOffset + blockSize)
            {
                long offsetInBlock = position - currentByteOffset;
                long bytesToRead   = Math.Min(blockSize - offsetInBlock, length - bytesRead);

                // Convert block address to byte address
                long blockByteAddr       = blockStart             * _superblock.block_size;
                long partitionByteOffset = (long)_partition.Start * sectorSize;
                long absoluteByteAddr    = blockByteAddr + partitionByteOffset;
                long startingSector      = absoluteByteAddr / sectorSize;
                var  offsetInFirstSector = (int)(absoluteByteAddr % sectorSize + offsetInBlock);

                var sectorsToRead = (int)((offsetInFirstSector + bytesToRead + sectorSize - 1) / sectorSize);

                AaruLogging.Debug(MODULE_NAME,
                                  "Reading block: sector={0}, offset={1}, sectors={2}",
                                  startingSector,
                                  offsetInFirstSector,
                                  sectorsToRead);

                ErrorNumber errno = _imagePlugin.ReadSectors((ulong)startingSector,
                                                             false,
                                                             (uint)sectorsToRead,
                                                             out byte[] sectorData,
                                                             out SectorStatus[] _);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading sectors: {0}", errno);

                    return errno;
                }

                Array.Copy(sectorData, offsetInFirstSector, buffer, bytesRead, (int)bytesToRead);
                bytesRead += (int)bytesToRead;

                if(bytesRead >= length) break;

                position += bytesToRead;
            }

            currentByteOffset += blockSize;
        }

        if(bytesRead == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No data read from direct blocks");

            return ErrorNumber.InOutError;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads from indirect blocks in the data stream</summary>
    ErrorNumber ReadDataStreamIndirect(DataStream dataStream, long position, int length, out byte[] buffer)
    {
        buffer = null;
        uint sectorSize = _imagePlugin.Info.SectorSize;

        AaruLogging.Debug(MODULE_NAME, "ReadDataStreamIndirect: position={0}, length={1}", position, length);

        buffer = new byte[length];
        var bytesRead    = 0;
        var blockSize    = (int)_superblock.block_size;
        int ptrsPerBlock = blockSize / 8; // sizeof(BlockRun) = 8 bytes

        long indirectBlockNum = (long)dataStream.indirect.group * _superblock.blocks_per_ag + dataStream.indirect.start;

        // Position is in blocks, relative to start of indirect range
        // We need to find the position relative to the start of the indirect data
        long posInIndirectRange = position - dataStream.max_direct_range;

        AaruLogging.Debug(MODULE_NAME,
                          "Indirect block at {0}, posInIndirectRange={1}",
                          indirectBlockNum,
                          posInIndirectRange);

        // Read indirect pointer blocks and find the right data blocks
        long currentBlockPos = 0;

        for(var i = 0; i < dataStream.indirect.len && bytesRead < length; i++)
        {
            // Read the indirect pointer block
            ErrorNumber errno = ReadBlock(indirectBlockNum + i, out byte[] indirectBlockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading indirect pointer block: {0}", errno);

                return errno;
            }

            // Each entry in the indirect block is a BlockRun pointing to data blocks
            for(var j = 0; j < ptrsPerBlock && bytesRead < length; j++)
            {
                int offset = j * 8;
                var group  = BitConverter.ToInt32(indirectBlockData, offset);
                var start  = BitConverter.ToUInt16(indirectBlockData, offset + 4);
                var len    = BitConverter.ToUInt16(indirectBlockData, offset + 6);

                if(len == 0) break;

                long runBlockStart = (long)group * _superblock.blocks_per_ag + start;
                long runByteSize   = len * blockSize;

                // Check if our position falls within this run
                if(posInIndirectRange >= currentBlockPos && posInIndirectRange < currentBlockPos + len)
                {
                    long offsetInRun      = posInIndirectRange - currentBlockPos;
                    long blockInRun       = runBlockStart + offsetInRun;
                    long actualByteOffset = position - (dataStream.max_direct_range + currentBlockPos * blockSize);

                    // Calculate how much to read from this run
                    long remainingInRun = runByteSize - actualByteOffset % blockSize;
                    long bytesToRead    = Math.Min(remainingInRun, length - bytesRead);

                    // Read the data blocks
                    long blockByteAddr       = blockInRun             * blockSize;
                    long partitionByteOffset = (long)_partition.Start * sectorSize;
                    long absoluteByteAddr    = blockByteAddr + partitionByteOffset;
                    long startingSector      = absoluteByteAddr / sectorSize;
                    var  offsetInFirstSector = (int)(absoluteByteAddr % sectorSize + actualByteOffset % blockSize);

                    var sectorsToRead = (int)((offsetInFirstSector + bytesToRead + sectorSize - 1) / sectorSize);

                    errno = _imagePlugin.ReadSectors((ulong)startingSector,
                                                     false,
                                                     (uint)sectorsToRead,
                                                     out byte[] sectorData,
                                                     out SectorStatus[] _);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME, "Error reading indirect data sectors: {0}", errno);

                        return errno;
                    }

                    Array.Copy(sectorData, offsetInFirstSector, buffer, bytesRead, (int)bytesToRead);
                    bytesRead += (int)bytesToRead;
                    position  += bytesToRead;

                    // Update position in indirect range for next iteration
                    posInIndirectRange = position - dataStream.max_direct_range;
                }

                currentBlockPos += len;
            }
        }

        if(bytesRead == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No data read from indirect blocks");

            return ErrorNumber.InOutError;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads from double-indirect blocks in the data stream</summary>
    ErrorNumber ReadDataStreamDoubleIndirect(DataStream dataStream, long position, int length, out byte[] buffer)
    {
        buffer = null;
        uint sectorSize = _imagePlugin.Info.SectorSize;

        AaruLogging.Debug(MODULE_NAME, "ReadDataStreamDoubleIndirect: position={0}, length={1}", position, length);

        buffer = new byte[length];
        var bytesRead    = 0;
        var blockSize    = (int)_superblock.block_size;
        int ptrsPerBlock = blockSize / 8; // sizeof(BlockRun) = 8 bytes

        // Position relative to start of double-indirect range (in blocks)
        long posInDoubleIndirectRange = position - dataStream.max_indirect_range;

        // Calculate indices into the double-indirect structure
        // Structure: [indirect_block][indirect_ptr] -> [direct_block][direct_ptr] -> data_blocks
        int dBlkSize  = BLOCKS_PER_DI_RUN * ptrsPerBlock;
        int idPtrSize = BLOCKS_PER_DI_RUN * ptrsPerBlock * BLOCKS_PER_DI_RUN;
        int idBlkSize = BLOCKS_PER_DI_RUN * ptrsPerBlock * BLOCKS_PER_DI_RUN * ptrsPerBlock;

        long doubleIndirectBlockNum = (long)dataStream.double_indirect.group * _superblock.blocks_per_ag +
                                      dataStream.double_indirect.start;

        while(bytesRead < length)
        {
            // Recalculate position each iteration
            long curPos = posInDoubleIndirectRange + bytesRead / blockSize;

            var offsetInRun = (int)(curPos                     % BLOCKS_PER_DI_RUN);
            var dPtr        = (int)(curPos / BLOCKS_PER_DI_RUN % ptrsPerBlock);
            var dBlk        = (int)(curPos / dBlkSize          % BLOCKS_PER_DI_RUN);
            var idPtr       = (int)(curPos / idPtrSize         % ptrsPerBlock);
            var idBlk       = (int)(curPos                     / idBlkSize);

            if(idBlk >= BLOCKS_PER_DI_RUN)
            {
                AaruLogging.Debug(MODULE_NAME, "Position beyond double-indirect range");

                break;
            }

            AaruLogging.Debug(MODULE_NAME,
                              "Double-indirect: idBlk={0}, idPtr={1}, dBlk={2}, dPtr={3}, offset={4}",
                              idBlk,
                              idPtr,
                              dBlk,
                              dPtr,
                              offsetInRun);

            // Read the indirect block (first level)
            ErrorNumber errno = ReadBlock(doubleIndirectBlockNum + idBlk, out byte[] indirectBlockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading double-indirect block: {0}", errno);

                return errno;
            }

            // Get the direct block pointer from the indirect block
            int idOffset    = idPtr * 8;
            var directGroup = BitConverter.ToInt32(indirectBlockData, idOffset);
            var directStart = BitConverter.ToUInt16(indirectBlockData, idOffset + 4);
            var directLen   = BitConverter.ToUInt16(indirectBlockData, idOffset + 6);

            if(directLen == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Empty direct block pointer in double-indirect");

                break;
            }

            long directBlockNum = (long)directGroup * _superblock.blocks_per_ag + directStart + dBlk;

            // Read the direct block (second level)
            errno = ReadBlock(directBlockNum, out byte[] directBlockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading direct block: {0}", errno);

                return errno;
            }

            // Get the data block pointer from the direct block
            int dOffset   = dPtr * 8;
            var dataGroup = BitConverter.ToInt32(directBlockData, dOffset);
            var dataStart = BitConverter.ToUInt16(directBlockData, dOffset + 4);
            var dataLen   = BitConverter.ToUInt16(directBlockData, dOffset + 6);

            if(dataLen == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Empty data block pointer in direct block");

                break;
            }

            long dataBlockNum = (long)dataGroup * _superblock.blocks_per_ag + dataStart + offsetInRun;

            // Calculate how much to read
            long remainingInRun = (dataLen - offsetInRun) * blockSize;
            long bytesToRead    = Math.Min(remainingInRun, length - bytesRead);

            // Adjust for byte offset within the current position
            long byteOffsetInBlock = (position + bytesRead) % blockSize;

            if(byteOffsetInBlock > 0) bytesToRead = Math.Min(bytesToRead, blockSize - byteOffsetInBlock);

            // Read the actual data
            long blockByteAddr       = dataBlockNum           * blockSize;
            long partitionByteOffset = (long)_partition.Start * sectorSize;
            long absoluteByteAddr    = blockByteAddr + partitionByteOffset + byteOffsetInBlock;
            long startingSector      = absoluteByteAddr / sectorSize;
            var  offsetInFirstSector = (int)(absoluteByteAddr % sectorSize);

            var sectorsToRead = (int)((offsetInFirstSector + bytesToRead + sectorSize - 1) / sectorSize);

            errno = _imagePlugin.ReadSectors((ulong)startingSector,
                                             false,
                                             (uint)sectorsToRead,
                                             out byte[] sectorData,
                                             out SectorStatus[] _);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading double-indirect data sectors: {0}", errno);

                return errno;
            }

            Array.Copy(sectorData, offsetInFirstSector, buffer, bytesRead, (int)bytesToRead);
            bytesRead += (int)bytesToRead;
        }

        if(bytesRead == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No data read from double-indirect blocks");

            return ErrorNumber.InOutError;
        }

        return ErrorNumber.NoError;
    }
}