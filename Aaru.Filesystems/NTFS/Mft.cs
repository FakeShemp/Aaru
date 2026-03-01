// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mft.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class NTFS
{
    /// <summary>Reads an MFT record by record number from the MFT located in the partition.</summary>
    /// <param name="recordNumber">MFT record number to read.</param>
    /// <param name="recordData">Output byte array with the raw MFT record data after USA fixup.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadMftRecord(uint recordNumber, out byte[] recordData)
    {
        recordData = null;

        long recordByteOffset = (long)recordNumber                     * _mftRecordSize;
        uint sectorsToRead    = (_mftRecordSize + _bytesPerSector - 1) / _bytesPerSector;

        // If we have parsed $MFT data runs, use them to locate the record in a possibly fragmented MFT
        if(_mftDataRuns is { Count: > 0 })
        {
            ErrorNumber errno = ReadMftRecordFromDataRuns(recordByteOffset, sectorsToRead, out recordData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading MFT record {0} via data runs: {1}", recordNumber, errno);

                return errno;
            }
        }
        else
        {
            // Fallback: linear calculation from BPB mft_lsn (bootstrap or unfragmented MFT)
            long mftStartByte = _bpb.mft_lsn * _bytesPerCluster;
            long recordOffset = mftStartByte + recordByteOffset;
            long recordSector = recordOffset / _bytesPerSector;
            var  sectorOffset = (int)(recordOffset % _bytesPerSector);
            uint sectors      = sectorsToRead;

            if(sectorOffset > 0) sectors++;

            ErrorNumber errno = _image.ReadSectors(_partition.Start + (ulong)recordSector,
                                                   false,
                                                   sectors,
                                                   out byte[] sectorData,
                                                   out _);

            if(errno != ErrorNumber.NoError) return errno;

            recordData = new byte[_mftRecordSize];
            Array.Copy(sectorData, sectorOffset, recordData, 0, _mftRecordSize);
        }

        // Apply Update Sequence Array fixup
        return ApplyUsaFixup(recordData);
    }

    /// <summary>
    ///     Reads MFT record data using the parsed data runs of the $MFT file, supporting fragmented MFT.
    /// </summary>
    /// <param name="byteOffset">Byte offset within the $MFT data of the target record.</param>
    /// <param name="sectorsToRead">Number of sectors comprising one MFT record.</param>
    /// <param name="recordData">Output buffer containing the raw record data.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadMftRecordFromDataRuns(long byteOffset, uint sectorsToRead, out byte[] recordData)
    {
        recordData = new byte[sectorsToRead * _bytesPerSector];

        long bytesRemaining = recordData.Length;
        var  destOffset     = 0;
        long currentOffset  = byteOffset;
        long runByteStart   = 0;

        foreach((long clusterOffset, long clusterCount) in _mftDataRuns)
        {
            long runBytes   = clusterCount * _bytesPerCluster;
            long runByteEnd = runByteStart + runBytes;

            // Skip runs before the target offset
            if(runByteEnd <= currentOffset)
            {
                runByteStart = runByteEnd;

                continue;
            }

            // Done reading
            if(bytesRemaining <= 0) break;

            // Calculate where in this run to start reading
            long offsetInRun     = currentOffset - runByteStart;
            long bytesInThisRun  = Math.Min(runBytes - offsetInRun, bytesRemaining);
            long clusterInRun    = offsetInRun / _bytesPerCluster;
            long offsetInCluster = offsetInRun % _bytesPerCluster;

            // Sparse run (should not happen for $MFT, but handle gracefully)
            if(clusterOffset == 0)
                Array.Clear(recordData, destOffset, (int)bytesInThisRun);
            else
            {
                long physicalCluster = clusterOffset + clusterInRun;

                ulong sectorStart = (ulong)(physicalCluster * _sectorsPerCluster) +
                                    (ulong)(offsetInCluster / _bytesPerSector);

                var sectorsInRun = (uint)((bytesInThisRun + _bytesPerSector - 1) / _bytesPerSector);

                ErrorNumber errno = _image.ReadSectors(_partition.Start + sectorStart,
                                                       false,
                                                       sectorsInRun,
                                                       out byte[] runData,
                                                       out _);

                if(errno != ErrorNumber.NoError) return errno;

                var srcOffset  = (int)(offsetInCluster % _bytesPerSector);
                var copyLength = (int)Math.Min(bytesInThisRun, runData.Length - srcOffset);

                Array.Copy(runData, srcOffset, recordData, destOffset, copyLength);
            }

            destOffset     += (int)bytesInThisRun;
            currentOffset  += bytesInThisRun;
            bytesRemaining -= bytesInThisRun;
            runByteStart   =  runByteEnd;
        }

        if(bytesRemaining > 0)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "MFT data runs exhausted before reading full record at offset {0}",
                              byteOffset);

            return ErrorNumber.InOutError;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Parses the unnamed $DATA attribute's data runs from the $MFT record (MFT record #0).
    ///     Called during mount to enable fragmented MFT support.
    /// </summary>
    /// <param name="mftRecord">Raw MFT record #0 data after USA fixup.</param>
    /// <param name="mftHeader">Parsed MFT record header.</param>
    /// <returns>List of data runs (cluster offset, cluster count), or null on failure.</returns>
    List<(long offset, long length)> ParseMftDataRuns(byte[] mftRecord, in MftRecord mftHeader)
    {
        int offset = mftHeader.attrs_offset;

        while(offset + 4 <= mftRecord.Length)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(mftRecord, offset);

            if(attrType == AttributeType.End || attrType == AttributeType.Unused) break;

            var attrLength = BitConverter.ToUInt32(mftRecord, offset + 4);

            if(attrLength == 0 || offset + attrLength > mftRecord.Length) break;

            byte nonResident = mftRecord[offset + 8];

            if(attrType == AttributeType.Data && nonResident == 1)
            {
                // Check this is the unnamed $DATA attribute
                byte nameLength = mftRecord[offset + 9];

                if(nameLength == 0)
                {
                    NonResidentAttributeRecord nrAttr =
                        Marshal.ByteArrayToStructureLittleEndian<NonResidentAttributeRecord>(mftRecord,
                            offset,
                            Marshal.SizeOf<NonResidentAttributeRecord>());

                    int runListOffset = offset + nrAttr.mapping_pairs_offset;

                    return ParseDataRuns(mftRecord, runListOffset, offset + (int)attrLength);
                }
            }

            offset += (int)attrLength;
        }

        return null;
    }
}