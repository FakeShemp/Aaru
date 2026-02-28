// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNumber">The inode number to read (1-based)</param>
    /// <param name="inode">The parsed inode structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNumber, out Inode inode)
    {
        inode = default(Inode);

        if(inodeNumber == 0 || inodeNumber > _superblock.inodes)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNumber);

            return ErrorNumber.InvalidArgument;
        }

        // Calculate which block group this inode belongs to
        uint blockGroup = (inodeNumber - 1) / _superblock.inodes_per_grp;
        uint inodeInGrp = (inodeNumber - 1) % _superblock.inodes_per_grp;

        if(blockGroup >= _blockGroupCount)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode {0} block group {1} out of range", inodeNumber, blockGroup);

            return ErrorNumber.InvalidArgument;
        }

        // Get the inode table block from the block group descriptor
        BlockGroupDescriptor bgd = _blockGroupDescriptors[blockGroup];

        ulong inodeTableBlock = _is64Bit ? (ulong)bgd.inode_table_hi << 32 | bgd.inode_table_lo : bgd.inode_table_lo;

        // Calculate byte offset of the inode on disk
        ulong inodeByteOffset = inodeTableBlock * _blockSize + (ulong)inodeInGrp * _inodeSize;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0}: group={1}, index={2}, table_block={3}, offset=0x{4:X}",
                          inodeNumber,
                          blockGroup,
                          inodeInGrp,
                          inodeTableBlock,
                          inodeByteOffset);

        // Read the inode data
        int inodeStructSize = Marshal.SizeOf<Inode>();
        int bytesToRead     = Math.Min(_inodeSize, (ushort)inodeStructSize);

        ErrorNumber errno = ReadBytes(inodeByteOffset, (uint)bytesToRead, out byte[] inodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode data: {0}", errno);

            return errno;
        }

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData, 0, bytesToRead);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: mode=0x{1:X4}, size={2}, links={3}, flags=0x{4:X8}",
                          inodeNumber,
                          inode.mode,
                          (ulong)inode.size_high << 32 | inode.size_lo,
                          inode.links_count,
                          inode.i_flags);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets the list of physical data blocks for an inode</summary>
    /// <param name="inode">The inode to get blocks for</param>
    /// <param name="blockList">List of (physical block number, number of contiguous blocks)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetInodeDataBlocks(Inode inode, out List<(ulong physicalBlock, uint length)> blockList)
    {
        blockList = [];

        bool usesExtents = (inode.i_flags & EXT4_EXTENTS_FL) != 0;

        if(usesExtents) return GetExtentBlocks(inode, blockList);

        return GetIndirectBlocks(inode, blockList);
    }

    /// <summary>Reads the raw bytes of an inode from disk (full inode size, not just struct size)</summary>
    ErrorNumber ReadRawInodeBytes(uint inodeNumber, out byte[] rawInode)
    {
        rawInode = null;

        if(inodeNumber == 0 || inodeNumber > _superblock.inodes) return ErrorNumber.InvalidArgument;

        uint blockGroup = (inodeNumber - 1) / _superblock.inodes_per_grp;
        uint inodeInGrp = (inodeNumber - 1) % _superblock.inodes_per_grp;

        if(blockGroup >= _blockGroupCount) return ErrorNumber.InvalidArgument;

        BlockGroupDescriptor bgd = _blockGroupDescriptors[blockGroup];

        ulong inodeTableBlock = _is64Bit ? (ulong)bgd.inode_table_hi << 32 | bgd.inode_table_lo : bgd.inode_table_lo;

        ulong inodeByteOffset = inodeTableBlock * _blockSize + (ulong)inodeInGrp * _inodeSize;

        return ReadBytes(inodeByteOffset, _inodeSize, out rawInode);
    }
}