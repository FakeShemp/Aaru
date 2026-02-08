// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        stat = new FileSystemInfo
        {
            Blocks         = _discSize / (ulong)_blockSize,
            FilenameLength = (ushort)_maxNameLen,
            Type           = FS_TYPE,
            PluginId       = Id
        };

        // ADFS doesn't have a traditional inode system
        // Files and FreeFiles are not applicable
        stat.Files     = 0;
        stat.FreeFiles = 0;

        // Calculate free blocks based on format
        if(_isOldMap)
            stat.FreeBlocks = CalculateOldMapFreeBlocks();
        else
            stat.FreeBlocks = CalculateNewMapFreeBlocks();

        return ErrorNumber.NoError;
    }

    /// <summary>Calculates free blocks for old map format (ADFS-S, ADFS-M, ADFS-L, ADFS-D)</summary>
    /// <returns>Number of free blocks</returns>
    ulong CalculateOldMapFreeBlocks()
    {
        // Old map format stores free space as (start, length) pairs
        // freeStart in sector 0 contains start addresses (82 * 3 bytes)
        // freeStart in sector 1 contains lengths (82 * 3 bytes)
        // freeEnd in sector 1 indicates how many entries are used (0 = 82 entries, 1-81 = that many entries)

        int numEntries = _oldMap1.freeEnd == 0 ? 82 : _oldMap1.freeEnd;

        ulong totalFreeBytes = 0;

        // Each entry in sector 1's freeStart is the length of a free space block
        // The lengths are stored as 3-byte values representing the size / 256
        for(var i = 0; i < numEntries && i < 82; i++)
        {
            int offset = i * 3;

            if(offset + 2 >= _oldMap1.freeStart.Length) break;

            // Read 3-byte length value (little endian)
            var length = (uint)(_oldMap1.freeStart[offset]          |
                                _oldMap1.freeStart[offset + 1] << 8 |
                                _oldMap1.freeStart[offset + 2] << 16);

            // Length is in units of 256 bytes
            totalFreeBytes += length * 256UL;
        }

        return totalFreeBytes / (ulong)_blockSize;
    }

    /// <summary>Calculates free blocks for new map format (ADFS-E, ADFS-F, ADFS-F+, ADFS-G)</summary>
    /// <returns>Number of free blocks</returns>
    ulong CalculateNewMapFreeBlocks()
    {
        // New map format uses zones with bit-mapped fragments
        // Each zone contains a bitstream where fragments are identified by their fragment ID
        // Free space is tracked as a linked list starting at offset 8 in each zone

        if(_nzones == 0) return 0;

        ulong totalFreeBits = 0;
        int   idlen         = _discRecord.idlen;

        for(var zone = 0; zone < _nzones; zone++)
        {
            byte[] zoneData;

            // Use cached zone data if available
            if(_mapCache != null && _mapCache[zone] != null)
                zoneData = _mapCache[zone];
            else
            {
                // Fall back to reading from disk
                int map2blk = _discRecord.log2bpmb - _discRecord.log2secsize;

                long mapAddr = (_nzones >> 1) * _zoneSize - (_nzones > 1 ? DISC_RECORD_SIZE * 8 : 0);

                if(map2blk >= 0)
                    mapAddr <<= map2blk;
                else
                    mapAddr >>= -map2blk;

                ulong zoneSector = (ulong)(mapAddr / _blockSize) + (uint)zone;

                ErrorNumber errno = ReadAdfsSector(zoneSector, out zoneData);

                if(errno != ErrorNumber.NoError) continue;
            }

            // Scan the free list in this zone
            totalFreeBits += ScanZoneFreeSpace(zoneData, idlen, _zoneSize);
        }

        // Convert free bits to free blocks
        // Each bit represents (1 << log2bpmb) bytes
        ulong bytesPerMapBit = 1UL << _discRecord.log2bpmb;
        ulong totalFreeBytes = totalFreeBits * bytesPerMapBit;

        return totalFreeBytes / (ulong)_blockSize;
    }

    /// <summary>Scans a zone for free space using the free fragment linked list</summary>
    /// <param name="zoneData">Raw zone data</param>
    /// <param name="idlen">Fragment ID length in bits</param>
    /// <param name="bitsPerZone">Number of usable bits per zone</param>
    /// <returns>Number of free map bits in this zone</returns>
    static ulong ScanZoneFreeSpace(byte[] zoneData, int idlen, int bitsPerZone)
    {
        // End bit position
        int endBit = Math.Min(bitsPerZone, zoneData.Length * 8);

        // Read the initial free link at offset 8 (bits 8-22 typically, limited to 15 bits for free links)
        // Free link is stored in bits 8-22 (or less depending on idlen), masked to 15 bits max
        int fragIdLen = Math.Min(idlen, 15);
        int freeLink  = GetBits(zoneData, 8, fragIdLen);

        if(freeLink == 0) return 0; // No free space in this zone

        ulong totalFree = 0;
        int   position  = 8 + freeLink;

        // Follow the free list
        while(position < endBit)
        {
            // Get the fragment ID at current position (this is the offset to next free fragment)
            int fragId = GetBits(zoneData, position, fragIdLen);

            // Find the end of this fragment (next '1' bit after the fragment ID)
            int fragEnd = FindNextSetBit(zoneData, position + idlen, endBit);

            if(fragEnd < 0 || fragEnd >= endBit) break;

            // The fragment size in bits is (fragEnd + 1 - position)
            int fragSize = fragEnd + 1 - position;
            totalFree += (ulong)fragSize;

            // Move to next free fragment
            if(fragId < idlen + 1) break; // End of free list (fragment too small to contain another link)

            position += fragId;
        }

        return totalFree;
    }

    /// <summary>Gets a bit field from a byte array</summary>
    /// <param name="data">Source data</param>
    /// <param name="bitOffset">Starting bit offset</param>
    /// <param name="bitCount">Number of bits to read</param>
    /// <returns>Value of the bit field</returns>
    static int GetBits(byte[] data, int bitOffset, int bitCount)
    {
        int byteOffset = bitOffset / 8;
        int bitShift   = bitOffset % 8;

        if(byteOffset >= data.Length) return 0;

        // Read up to 4 bytes to cover the bit field
        uint value = data[byteOffset];

        if(byteOffset + 1 < data.Length) value |= (uint)data[byteOffset + 1] << 8;

        if(byteOffset + 2 < data.Length) value |= (uint)data[byteOffset + 2] << 16;

        if(byteOffset + 3 < data.Length) value |= (uint)data[byteOffset + 3] << 24;

        // Shift and mask
        value >>= bitShift;
        value &=  (1u << bitCount) - 1;

        return (int)value;
    }

    /// <summary>Finds the next set bit in a byte array starting from the given position</summary>
    /// <param name="data">Source data</param>
    /// <param name="startBit">Starting bit position</param>
    /// <param name="endBit">Ending bit position (exclusive)</param>
    /// <returns>Position of next set bit, or -1 if not found</returns>
    static int FindNextSetBit(byte[] data, int startBit, int endBit)
    {
        for(int bit = startBit; bit < endBit; bit++)
        {
            int byteIdx = bit / 8;
            int bitIdx  = bit % 8;

            if(byteIdx >= data.Length) return -1;

            if((data[byteIdx] & 1 << bitIdx) != 0) return bit;
        }

        return -1;
    }
}