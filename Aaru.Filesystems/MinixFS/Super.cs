// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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

using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class MinixFS
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Count free inodes by scanning inode bitmap
        // Inode 0 is reserved, so we check ninodes + 1 bits but bit 0 is always set
        ulong freeInodes = CountFreeBits(IMAP, _imapBlocks, _ninodes + 1);

        // Count free zones by scanning zone bitmap
        // Zone bitmap only covers data zones: [firstdatazone, nzones)
        // The number of bits to check is (nzones - firstdatazone + 1)
        // Bit 0 is reserved (always set), actual data zones start at bit 1
        uint  dataZoneBits = _zones - (uint)_firstDataZone + 1;
        ulong freeZones    = CountFreeBits(ZMAP, _zmapBlocks, dataZoneBits);

        // Apply zone scaling: zones may span multiple blocks
        // s_log_zone_size is log2 of blocks per zone (usually 0)
        int zoneScale = _logZoneSize;

        // Total data blocks = (nzones - firstdatazone) << log_zone_size
        // This excludes boot block, superblock, bitmaps, and inode area
        ulong totalDataBlocks = _zones - (uint)_firstDataZone << zoneScale;

        // Free blocks = free zones << log_zone_size
        ulong freeBlocks = freeZones << zoneScale;

        stat = new FileSystemInfo
        {
            Blocks         = totalDataBlocks,
            FreeBlocks     = freeBlocks,
            Files          = _ninodes,
            FreeFiles      = freeInodes,
            FilenameLength = (ushort)_filenameSize,
            Type = _version == FilesystemVersion.V3
                       ? FS_TYPE_V3
                       : _version == FilesystemVersion.V2
                           ? FS_TYPE_V2
                           : FS_TYPE_V1,
            PluginId = Id
        };

        return ErrorNumber.NoError;
    }

    /// <summary>Counts free bits in a bitmap</summary>
    /// <param name="mapType">IMAP for inode bitmap, ZMAP for zone bitmap</param>
    /// <param name="mapBlocks">Number of blocks in the bitmap</param>
    /// <param name="maxBits">Maximum number of bits to check</param>
    /// <returns>Number of free (zero) bits in the bitmap</returns>
    ulong CountFreeBits(int mapType, int mapBlocks, uint maxBits)
    {
        ulong freeBits = 0;

        // Calculate starting block for the bitmap
        // Layout: boot block (0), superblock (1), inode map, zone map
        int startBlock = mapType == IMAP ? START_BLOCK : START_BLOCK + _imapBlocks;

        uint bitsChecked = 0;

        for(var block = 0; block < mapBlocks && bitsChecked < maxBits; block++)
        {
            ErrorNumber errno = ReadBlock(startBlock + block, out byte[] blockData);

            if(errno != ErrorNumber.NoError) continue;

            // Count free bits in this block
            for(var byteIndex = 0; byteIndex < blockData.Length && bitsChecked < maxBits; byteIndex++)
            {
                byte b = blockData[byteIndex];

                for(var bit = 0; bit < 8 && bitsChecked < maxBits; bit++)
                {
                    // Bit 0 is always reserved (used), so skip it
                    if(bitsChecked == 0)
                    {
                        bitsChecked++;

                        continue;
                    }

                    // A zero bit means free
                    if((b & 1 << bit) == 0) freeBits++;

                    bitsChecked++;
                }
            }
        }

        return freeBits;
    }
}