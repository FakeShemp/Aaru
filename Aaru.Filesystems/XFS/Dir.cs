// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    /// <summary>Reads a shortform (inline) directory from the inode data fork</summary>
    /// <param name="inode">The directory inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadShortformDirectory(Dinode inode)
    {
        AaruLogging.Debug(MODULE_NAME, "Reading shortform directory");

        // The data fork starts immediately after the inode core.
        // The inode core size depends on version.
        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100; // v2 core is 100 bytes

        // We need the full inode including the data fork
        ErrorNumber errno = ReadInodeRaw(_superblock.rootino, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        if(rawInode.Length < coreSize + 6) // minimum sf header is 6 bytes
        {
            AaruLogging.Debug(MODULE_NAME, "Raw inode too small for shortform directory");

            return ErrorNumber.InvalidArgument;
        }

        // Parse the shortform header at the start of the data fork
        int pos = coreSize;

        byte count   = rawInode[pos];
        byte i8count = rawInode[pos + 1];

        bool use64BitInodes = i8count > 0;

        // Header size: 1(count) + 1(i8count) + parent_ino_size
        int parentInoSize = use64BitInodes ? 8 : 4;
        int headerSize    = 2 + parentInoSize;

        AaruLogging.Debug(MODULE_NAME, "SF dir: count={0}, i8count={1}, use64={2}", count, i8count, use64BitInodes);

        if(pos + headerSize > rawInode.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Raw inode too small for shortform header");

            return ErrorNumber.InvalidArgument;
        }

        // Skip past header
        pos += headerSize;

        // The total number of entries is count + i8count (the counts just distinguish
        // how many use 4-byte vs 8-byte inode numbers)
        // Actually, count is the total entry count; i8count is how many use 8-byte inodes.
        // But per the kernel, the total number of entries is simply "count".
        int totalEntries = count;

        for(var i = 0; i < totalEntries; i++)
        {
            if(pos + 3 > rawInode.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated shortform entry at index {0}", i);

                break;
            }

            byte nameLen = rawInode[pos];

            // offset[2] follows namelen
            pos += 1 + 2; // skip namelen + offset

            if(pos + nameLen > rawInode.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated name in shortform entry at index {0}", i);

                break;
            }

            string name = _encoding.GetString(rawInode, pos, nameLen);
            pos += nameLen;

            // If ftype is enabled, there's a 1-byte file type field after the name
            if(_hasFtype)
            {
                if(pos + 1 > rawInode.Length) break;

                pos += 1; // skip filetype byte
            }

            // Inode number follows
            ulong entryIno;

            if(use64BitInodes)
            {
                if(pos + 8 > rawInode.Length) break;

                entryIno =  BigEndianBitConverter.ToUInt64(rawInode, pos);
                pos      += 8;
            }
            else
            {
                if(pos + 4 > rawInode.Length) break;

                entryIno =  BigEndianBitConverter.ToUInt32(rawInode, pos);
                pos      += 4;
            }

            if(name is "." or "..") continue;

            _rootDirectoryCache[name] = entryIno;

            AaruLogging.Debug(MODULE_NAME, "SF entry: \"{0}\" -> inode {1}", name, entryIno);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a directory stored in extent format (single data block or multiple data blocks)</summary>
    /// <param name="inode">The directory inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadExtentDirectory(Dinode inode)
    {
        AaruLogging.Debug(MODULE_NAME, "Reading extent directory ({0} extents)", inode.di_nextents);

        // Read the extent list from the data fork
        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

        ErrorNumber errno = ReadInodeRaw(_superblock.rootino, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int pos = coreSize;

        // Each BMBT record is 16 bytes (2 x uint64)
        var extentCount = (int)inode.di_nextents;

        for(var i = 0; i < extentCount; i++)
        {
            if(pos + 16 > rawInode.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Truncated extent record at index {0}", i);

                break;
            }

            // Decode BMBT record
            var l0 = BigEndianBitConverter.ToUInt64(rawInode, pos);
            var l1 = BigEndianBitConverter.ToUInt64(rawInode, pos + 16 - 8);
            pos += 16;

            DecodeBmbtRec(l0, l1, out _, out ulong startBlock, out uint blockCount);

            AaruLogging.Debug(MODULE_NAME, "Extent {0}: startblock={1}, count={2}", i, startBlock, blockCount);

            // Read all blocks in this extent
            for(uint b = 0; b < blockCount; b++)
            {
                ulong blockAddr = startBlock + b;

                errno = ReadBlock(blockAddr, out byte[] blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading directory block {0}: {1}", blockAddr, errno);

                    continue;
                }

                ParseDirectoryDataBlock(blockData);
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a directory stored in btree format</summary>
    /// <param name="inode">The directory inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBtreeDirectory(Dinode inode)
    {
        AaruLogging.Debug(MODULE_NAME, "Reading btree directory");

        // The data fork contains a bmdr_block header followed by keys and pointers.
        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

        ErrorNumber errno = ReadInodeRaw(_superblock.rootino, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int pos = coreSize;

        if(pos + 4 > rawInode.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Raw inode too small for bmdr_block header");

            return ErrorNumber.InvalidArgument;
        }

        // Read the bmdr_block header
        var level   = BigEndianBitConverter.ToUInt16(rawInode, pos);
        var numrecs = BigEndianBitConverter.ToUInt16(rawInode, pos + 2);

        AaruLogging.Debug(MODULE_NAME, "BMDR: level={0}, numrecs={1}", level, numrecs);

        if(level == 0)
        {
            // Leaf node: records are BMBT extent records starting at pos + 4
            int recPos = pos + 4;

            for(var i = 0; i < numrecs; i++)
            {
                if(recPos + 16 > rawInode.Length) break;

                var l0 = BigEndianBitConverter.ToUInt64(rawInode, recPos);
                var l1 = BigEndianBitConverter.ToUInt64(rawInode, recPos + 8);
                recPos += 16;

                DecodeBmbtRec(l0, l1, out _, out ulong startBlock, out uint blockCount);

                for(uint b = 0; b < blockCount; b++)
                {
                    errno = ReadBlock(startBlock + b, out byte[] blockData);

                    if(errno != ErrorNumber.NoError) continue;

                    ParseDirectoryDataBlock(blockData);
                }
            }
        }
        else
        {
            // Internal node: keys are at pos + 4, pointers are after keys.
            // Keys: numrecs * 8 bytes (xfs_bmbt_key = __be64 br_startoff)
            // Pointers: numrecs * 8 bytes (__be64) — positioned at end of fork area
            int forkSize = inode.di_forkoff > 0 ? inode.di_forkoff * 8 : rawInode.Length - coreSize;

            int ptrsStart = pos + forkSize - numrecs * 8;

            for(var i = 0; i < numrecs; i++)
            {
                int ptrPos = ptrsStart + i * 8;

                if(ptrPos + 8 > rawInode.Length) break;

                var childBlock = BigEndianBitConverter.ToUInt64(rawInode, ptrPos);

                errno = ReadBmapBtreeBlock(childBlock, level - 1);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading bmap btree child block {0}: {1}", childBlock, errno);
                }
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a directory data block (v2 or v3 format) and adds entries to the cache</summary>
    /// <param name="blockData">The raw block data</param>
    void ParseDirectoryDataBlock(byte[] blockData)
    {
        if(blockData.Length < 16) return;

        var magic = BigEndianBitConverter.ToUInt32(blockData, 0);

        int dataStart;

        switch(magic)
        {
            case XFS_DIR2_BLOCK_MAGIC:
            case XFS_DIR2_DATA_MAGIC:
                // v2 header: 4 bytes magic + 3 * 4 bytes bestfree = 16 bytes
                dataStart = 16;

                break;
            case XFS_DIR3_BLOCK_MAGIC:
            case XFS_DIR3_DATA_MAGIC:
                // v3 header: dir3_data_hdr = 48 (dir3_blk_hdr) + 12 (3*dir2_data_free) + 4 (pad) = 64 bytes
                dataStart = 64;

                break;
            default:
                AaruLogging.Debug(MODULE_NAME, "Unknown directory block magic: 0x{0:X8}", magic);

                return;
        }

        // For block format, there's a tail at the end with leaf entries.
        // We stop when we hit free space tags or run out of data.
        int pos = dataStart;

        while(pos + 10 < blockData.Length) // minimum entry: 8 (inumber) + 1 (namelen) + 1 (name)
        {
            // Check for unused entry (freetag = 0xFFFF)
            var freetag = BigEndianBitConverter.ToUInt16(blockData, pos);

            if(freetag == 0xFFFF)
            {
                // This is a free entry: read length and skip
                if(pos + 4 > blockData.Length) break;

                var freeLen = BigEndianBitConverter.ToUInt16(blockData, pos + 2);

                if(freeLen == 0 || pos + freeLen > blockData.Length) break;

                pos += freeLen;

                continue;
            }

            // Active entry: xfs_dir2_data_entry
            if(pos + 9 > blockData.Length) break;

            var  entryIno = BigEndianBitConverter.ToUInt64(blockData, pos);
            byte nameLen  = blockData[pos + 8];

            if(nameLen == 0 || pos + 9 + nameLen > blockData.Length) break;

            string name = _encoding.GetString(blockData, pos + 9, nameLen);

            // After the name:
            // - if ftype: 1 byte file type
            // - padding to 8-byte align
            // - 2 byte tag (starting offset of this entry)
            int afterName = pos + 9 + nameLen;

            if(_hasFtype) afterName++; // skip filetype byte

            // Round up to next 8-byte boundary, then add 2 for tag
            int entryEnd = (afterName + 2 + 7) / 8 * 8;

            if(name is not "." and not "..")
            {
                _rootDirectoryCache[name] = entryIno;

                AaruLogging.Debug(MODULE_NAME, "Block entry: \"{0}\" -> inode {1}", name, entryIno);
            }

            pos = entryEnd;
        }
    }
}