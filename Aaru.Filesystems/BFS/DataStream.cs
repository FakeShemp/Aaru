// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DataStream.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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
/// <summary>Implements detection of the Be (new) filesystem</summary>
public sealed partial class BeFS
{
    /// <summary>Reads data from a data stream at a specified byte position</summary>
    /// <remarks>
    ///     Data streams in BFS are stored in blocks referenced by block_run structures.
    ///     Supports direct, indirect, and double-indirect block ranges.
    ///     This method locates the appropriate block(s) for the requested position,
    ///     reads the necessary sectors from the device, and extracts the requested bytes.
    /// </remarks>
    /// <param name="dataStream">The data stream structure containing block_run entries</param>
    /// <param name="position">The byte offset within the data stream to read from</param>
    /// <param name="length">The number of bytes to read</param>
    /// <param name="buffer">Output buffer containing the read data</param>
    /// <returns>Error code indicating success or failure</returns>
    private ErrorNumber ReadFromDataStream(data_stream dataStream, long position, int length, out byte[] buffer)
    {
        buffer = null;

        AaruLogging.Debug(MODULE_NAME, "ReadFromDataStream: pos=0x{0:X8}, len={1}", position, length);

        // Check which range the position falls into
        if(position < dataStream.max_direct_range)
        {
            AaruLogging.Debug(MODULE_NAME, "Position in direct range");

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
    private ErrorNumber ReadDataStreamDirect(data_stream dataStream, long position, int length, out byte[] buffer)
    {
        buffer = null;
        uint sectorSize           = _imagePlugin.Info.SectorSize;
        var  partitionStartSector = (long)_partition.Start;

        buffer = new byte[length];
        var  bytesRead         = 0;
        long currentByteOffset = 0;

        for(var i = 0; i < NUM_DIRECT_BLOCKS && bytesRead < length; i++)
        {
            if(dataStream.direct[i].len == 0) break;

            long blockStart = ((long)dataStream.direct[i].allocation_group << _superblock.ag_shift) +
                              dataStream.direct[i].start;

            long blockLen  = dataStream.direct[i].len;
            long blockSize = blockLen * _superblock.block_size;

            if(position >= currentByteOffset && position < currentByteOffset + blockSize)
            {
                long offsetInBlock = position - currentByteOffset;
                long bytesToRead   = Math.Min(blockSize - offsetInBlock, length - bytesRead);

                long blockByteAddr       = blockStart * _superblock.block_size;
                long startingSector      = blockByteAddr / sectorSize + partitionStartSector;
                var  offsetInFirstSector = (int)(blockByteAddr % sectorSize + offsetInBlock);

                var sectorsToRead = (int)((offsetInFirstSector + bytesToRead + sectorSize - 1) / sectorSize);

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
    private ErrorNumber ReadDataStreamIndirect(data_stream dataStream, long position, int length, out byte[] buffer)
    {
        buffer = null;
        AaruLogging.Debug(MODULE_NAME, "Reading from indirect blocks not yet fully implemented");

        return ErrorNumber.NotImplemented;
    }

    /// <summary>Reads from double-indirect blocks in the data stream</summary>
    private ErrorNumber ReadDataStreamDoubleIndirect(data_stream dataStream, long position, int length,
                                                     out byte[]  buffer)
    {
        buffer = null;
        AaruLogging.Debug(MODULE_NAME, "Reading from double-indirect blocks not yet fully implemented");

        return ErrorNumber.NotImplemented;
    }
}