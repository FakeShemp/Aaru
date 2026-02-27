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

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    /// <summary>Recursively reads a BMAP btree block to find directory data extents</summary>
    /// <param name="fsBlock">Filesystem block number of the btree block</param>
    /// <param name="level">Level of this btree block (0 = leaf)</param>
    /// <param name="entries">Dictionary to populate with directory entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBmapBtreeBlock(ulong fsBlock, int level, Dictionary<string, ulong> entries)
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

                DecodeBmbtRec(l0, l1, out _, out ulong startBlock, out uint blockCount, out _);

                // Read all blocks in this extent as potential directory data blocks
                for(uint bb = 0; bb < blockCount; bb++)
                {
                    errno = ReadBlock(startBlock + bb, out byte[] dirBlockData);

                    if(errno != ErrorNumber.NoError) continue;

                    ParseDirectoryDataBlock(dirBlockData, entries);
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

                errno = ReadBmapBtreeBlock(childBlock, btLevel - 1, entries);

                if(errno != ErrorNumber.NoError)
                    AaruLogging.Debug(MODULE_NAME, "Error reading btree child block {0}: {1}", childBlock, errno);
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
    /// <param name="unwritten">Output: true if extent is unwritten (preallocated)</param>
    static void DecodeBmbtRec(ulong    l0, ulong l1, out ulong startOff, out ulong startBlock, out uint blockCount,
                              out bool unwritten)
    {
        // l0:63       = extent flag (1 = unwritten)
        // l0:9-62     = startoff (54 bits)
        // l0:0-8      = startblock high 9 bits
        // l1:21-63    = startblock low 43 bits
        // l1:0-20     = blockcount (21 bits)
        unwritten  = l0 >> 63 != 0;
        startOff   = (l0 & 0x7FFFFFFFFFFFFE00UL) >> 9;
        startBlock = (l0 & 0x1FFUL) << 43 | l1 >> 21;
        blockCount = (uint)(l1 & 0x1FFFFFUL);
    }

    /// <summary>Loads all data-fork extents for a file inode into a sorted array</summary>
    /// <param name="inodeNumber">The inode number</param>
    /// <param name="inode">The decoded dinode structure</param>
    /// <param name="extents">Output sorted extent array</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadFileExtents(ulong inodeNumber, Dinode inode, out XfsExtent[] extents)
    {
        extents = null;

        switch(inode.di_format)
        {
            case XFS_DINODE_FMT_EXTENTS:
                return LoadFileExtentList(inodeNumber, inode, out extents);
            case XFS_DINODE_FMT_BTREE:
                return LoadFileBtreeExtents(inodeNumber, inode, out extents);
            case XFS_DINODE_FMT_LOCAL:
                // Inline data — no extents, data is in the inode fork itself
                extents = [];

                return ErrorNumber.NoError;
            default:
                AaruLogging.Debug(MODULE_NAME, "LoadFileExtents: unsupported format {0}", inode.di_format);

                return ErrorNumber.NotSupported;
        }
    }

    /// <summary>Loads extents from an extent-format data fork</summary>
    /// <param name="inodeNumber">The inode number</param>
    /// <param name="inode">The decoded dinode structure</param>
    /// <param name="extents">Output sorted extent array</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadFileExtentList(ulong inodeNumber, Dinode inode, out XfsExtent[] extents)
    {
        extents = null;

        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        // Determine extent count based on NREXT64 feature
        ulong extentCount;

        if(_v3Inodes && (inode.di_flags2 & XFS_DIFLAG2_NREXT64) != 0)
        {
            if(rawInode.Length < 32)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated inode data for NREXT64");

                return ErrorNumber.InvalidArgument;
            }

            extentCount = BigEndianBitConverter.ToUInt64(rawInode, 24);
        }
        else
            extentCount = inode.di_nextents;

        int pos     = coreSize;
        var extList = new List<XfsExtent>();

        for(ulong i = 0; i < extentCount; i++)
        {
            if(pos + 16 > rawInode.Length) break;

            var l0 = BigEndianBitConverter.ToUInt64(rawInode, pos);
            var l1 = BigEndianBitConverter.ToUInt64(rawInode, pos + 8);
            pos += 16;

            DecodeBmbtRec(l0, l1, out ulong startOff, out ulong startBlock, out uint blockCount, out bool unwritten);

            extList.Add(new XfsExtent
            {
                StartOff   = startOff,
                StartBlock = startBlock,
                BlockCount = blockCount,
                Unwritten  = unwritten
            });
        }

        extents = extList.OrderBy(e => e.StartOff).ToArray();

        return ErrorNumber.NoError;
    }

    /// <summary>Loads extents from a btree-format data fork</summary>
    /// <param name="inodeNumber">The inode number</param>
    /// <param name="inode">The decoded dinode structure</param>
    /// <param name="extents">Output sorted extent array</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadFileBtreeExtents(ulong inodeNumber, Dinode inode, out XfsExtent[] extents)
    {
        extents = null;

        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int pos = coreSize;

        if(pos + 4 > rawInode.Length) return ErrorNumber.InvalidArgument;

        var level   = BigEndianBitConverter.ToUInt16(rawInode, pos);
        var numrecs = BigEndianBitConverter.ToUInt16(rawInode, pos + 2);

        var extList = new List<XfsExtent>();

        if(level == 0)
        {
            // Leaf BMDR: extent records directly in the inode fork
            int recPos = pos + 4;

            for(var i = 0; i < numrecs; i++)
            {
                if(recPos + 16 > rawInode.Length) break;

                var l0 = BigEndianBitConverter.ToUInt64(rawInode, recPos);
                var l1 = BigEndianBitConverter.ToUInt64(rawInode, recPos + 8);
                recPos += 16;

                DecodeBmbtRec(l0,
                              l1,
                              out ulong startOff,
                              out ulong startBlock,
                              out uint blockCount,
                              out bool unwritten);

                extList.Add(new XfsExtent
                {
                    StartOff   = startOff,
                    StartBlock = startBlock,
                    BlockCount = blockCount,
                    Unwritten  = unwritten
                });
            }
        }
        else
        {
            int forkSize  = inode.di_forkoff > 0 ? inode.di_forkoff * 8 : rawInode.Length - coreSize;
            int maxrecs   = (forkSize - 4) / 16;
            int ptrsStart = pos + 4 + maxrecs * 8;

            for(var i = 0; i < numrecs; i++)
            {
                int ptrPos = ptrsStart + i * 8;

                if(ptrPos + 8 > rawInode.Length) break;

                var childBlock = BigEndianBitConverter.ToUInt64(rawInode, ptrPos);

                errno = CollectBmapBtreeExtents(childBlock, level - 1, extList);

                if(errno != ErrorNumber.NoError)
                    AaruLogging.Debug(MODULE_NAME, "Error reading bmap btree child block {0}: {1}", childBlock, errno);
            }
        }

        extents = extList.OrderBy(e => e.StartOff).ToArray();

        return ErrorNumber.NoError;
    }

    /// <summary>Recursively collects BMBT extent records from btree blocks</summary>
    /// <param name="fsBlock">Filesystem block number of the btree block</param>
    /// <param name="level">Level of this btree block (0 = leaf)</param>
    /// <param name="extents">List to populate with extent records</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber CollectBmapBtreeExtents(ulong fsBlock, int level, List<XfsExtent> extents)
    {
        ErrorNumber errno = ReadBlock(fsBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError) return errno;

        if(blockData.Length < 8) return ErrorNumber.InvalidArgument;

        var magic = BigEndianBitConverter.ToUInt32(blockData, 0);

        if(magic != XFS_BMAP_MAGIC && magic != XFS_BMAP_CRC_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid BMAP btree magic: 0x{0:X8}", magic);

            return ErrorNumber.InvalidArgument;
        }

        var btLevel   = BigEndianBitConverter.ToUInt16(blockData, 4);
        var btNumrecs = BigEndianBitConverter.ToUInt16(blockData, 6);

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

                DecodeBmbtRec(l0,
                              l1,
                              out ulong startOff,
                              out ulong startBlock,
                              out uint blockCount,
                              out bool unwritten);

                extents.Add(new XfsExtent
                {
                    StartOff   = startOff,
                    StartBlock = startBlock,
                    BlockCount = blockCount,
                    Unwritten  = unwritten
                });
            }
        }
        else
        {
            // Internal node: keys then pointers
            int ptrsPos = headerSize + btNumrecs * 8;

            for(var i = 0; i < btNumrecs; i++)
            {
                int ptrOffset = ptrsPos + i * 8;

                if(ptrOffset + 8 > blockData.Length) break;

                var childBlock = BigEndianBitConverter.ToUInt64(blockData, ptrOffset);

                errno = CollectBmapBtreeExtents(childBlock, btLevel - 1, extents);

                if(errno != ErrorNumber.NoError)
                    AaruLogging.Debug(MODULE_NAME, "Error reading btree child block {0}: {1}", childBlock, errno);
            }
        }

        return ErrorNumber.NoError;
    }
}