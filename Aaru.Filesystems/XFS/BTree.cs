// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    /// <summary>Recursively reads a BMAP btree block to find directory data extents</summary>
    /// <param name="fsBlock">Filesystem block number of the btree block</param>
    /// <param name="level">Level of this btree block (0 = leaf)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBmapBtreeBlock(ulong fsBlock, int level)
    {
        ErrorNumber errno = ReadBlock(fsBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError) return errno;

        // Validate the btree block header
        if(blockData.Length < 8) return ErrorNumber.InvalidArgument;

        var magic = BigEndianBitConverter.ToUInt32(blockData, 0);

        if(magic != XFS_BMAP_MAGIC && magic != XFS_BMAP_CRC_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid BMAP btree magic: 0x{0:X8}", magic);

            return ErrorNumber.InvalidArgument;
        }

        var btLevel   = BigEndianBitConverter.ToUInt16(blockData, 4);
        var btNumrecs = BigEndianBitConverter.ToUInt16(blockData, 6);

        // Header size depends on whether it's CRC or not (long form)
        int headerSize = magic == XFS_BMAP_CRC_MAGIC
                             ? 72  // XFS_BTREE_LBLOCK_CRC_LEN
                             : 24; // XFS_BTREE_LBLOCK_LEN

        if(btLevel == 0)
        {
            // Leaf: contains BMBT extent records
            int recPos = headerSize;

            for(var i = 0; i < btNumrecs; i++)
            {
                if(recPos + 16 > blockData.Length) break;

                var l0 = BigEndianBitConverter.ToUInt64(blockData, recPos);
                var l1 = BigEndianBitConverter.ToUInt64(blockData, recPos + 8);
                recPos += 16;

                DecodeBmbtRec(l0, l1, out _, out ulong startBlock, out uint blockCount);

                // Read all blocks in this extent as potential directory data blocks
                for(uint bb = 0; bb < blockCount; bb++)
                {
                    errno = ReadBlock(startBlock + bb, out byte[] dirBlockData);

                    if(errno != ErrorNumber.NoError) continue;

                    ParseDirectoryDataBlock(dirBlockData);
                }
            }
        }
        else
        {
            // Internal node: keys then pointers (64-bit pointers for long form)
            int ptrsPos = headerSize + btNumrecs * 8;

            for(var i = 0; i < btNumrecs; i++)
            {
                int ptrOffset = ptrsPos + i * 8;

                if(ptrOffset + 8 > blockData.Length) break;

                var childBlock = BigEndianBitConverter.ToUInt64(blockData, ptrOffset);

                errno = ReadBmapBtreeBlock(childBlock, btLevel - 1);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading btree child block {0}: {1}", childBlock, errno);
                }
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Decodes a BMBT (Bmap btree) extent record from its packed 128-bit format</summary>
    /// <param name="l0">First 64-bit word</param>
    /// <param name="l1">Second 64-bit word</param>
    /// <param name="startOff">Output: starting file offset in blocks</param>
    /// <param name="startBlock">Output: starting filesystem block number</param>
    /// <param name="blockCount">Output: number of blocks in extent</param>
    static void DecodeBmbtRec(ulong l0, ulong l1, out ulong startOff, out ulong startBlock, out uint blockCount)
    {
        // l0:63       = extent flag (1 = unwritten)
        // l0:9-62     = startoff (54 bits)
        // l0:0-8      = startblock high 9 bits
        // l1:21-63    = startblock low 43 bits
        // l1:0-20     = blockcount (21 bits)
        startOff   = (l0 & 0x7FFFFFFFFFFFFE00UL) >> 9;
        startBlock = (l0 & 0x1FFUL) << 43 | l1 >> 21;
        blockCount = (uint)(l1 & 0x1FFFFFUL);
    }
}