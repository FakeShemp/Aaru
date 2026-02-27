// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    /// <summary>Reads an inode from the DAT file using direct block addressing</summary>
    /// <param name="datInode">The DAT inode</param>
    /// <param name="ino">Inode number to read (used for DAT entry lookup)</param>
    /// <param name="inode">Output inode structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInodeFromDat(in Inode datInode, ulong ino, out Inode inode)
    {
        inode = default(Inode);

        AaruLogging.Debug(MODULE_NAME, "Reading inode {0} from DAT...", ino);

        // The DAT file uses the persistent allocator (palloc) layout like the ifile
        // Calculate the location of the inode in the DAT using the same algorithm as the ifile
        uint  inodeSize       = _superblock.inode_size;
        uint  entriesPerBlock = _blockSize        / inodeSize;
        ulong entriesPerGroup = (ulong)_blockSize * 8;
        uint  blocksPerGroup  = (uint)((entriesPerGroup + entriesPerBlock - 1) / entriesPerBlock) + 2;

        ulong group         = ino / entriesPerGroup;
        ulong groupOffset   = ino % entriesPerGroup;
        ulong logicalBlock  = group * blocksPerGroup + 2 + groupOffset / entriesPerBlock;
        var   offsetInBlock = (uint)(groupOffset % entriesPerBlock * inodeSize);

        AaruLogging.Debug(MODULE_NAME,
                          "DAT palloc: group={0}, groupOffset={1}, logicalBlock={2}, offset={3}",
                          group,
                          groupOffset,
                          logicalBlock,
                          offsetInBlock);

        // The DAT file uses root metadata addressing (physical block numbers in bmap)
        ErrorNumber errno = ReadLogicalBlock(datInode, logicalBlock, true, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading DAT block: {0}", errno);

            return errno;
        }

        int inodeStructSize = Marshal.SizeOf<Inode>();

        if(offsetInBlock + inodeStructSize > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode extends past block boundary");

            return ErrorNumber.InvalidArgument;
        }

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(blockData, (int)offsetInBlock, inodeStructSize);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: size={1}, blocks={2}, mode=0x{3:X4}",
                          ino,
                          inode.size,
                          inode.blocks,
                          inode.mode);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an inode from the ifile using the persistent allocator layout</summary>
    /// <param name="ifileInode">The ifile inode from the latest checkpoint</param>
    /// <param name="ino">Inode number to read</param>
    /// <param name="inode">Output inode structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInodeFromIfile(in Inode ifileInode, ulong ino, out Inode inode)
    {
        inode = default(Inode);

        AaruLogging.Debug(MODULE_NAME, "Reading inode {0} from ifile...", ino);

        // The ifile uses the persistent allocator (palloc) layout:
        //   entries_per_block = block_size / inode_size
        //   entries_per_group = block_size * 8 (bits in a bitmap block)
        //   blocks_per_group  = 2 (desc + bitmap) + ceil(entries_per_group / entries_per_block)
        //
        // For inode number 'ino':
        //   group        = ino / entries_per_group
        //   group_offset = ino % entries_per_group
        //   logical_block = group * blocks_per_group + 2 + group_offset / entries_per_block
        //   offset_in_block = (group_offset % entries_per_block) * inode_size
        uint  inodeSize       = _superblock.inode_size;
        uint  entriesPerBlock = _blockSize        / inodeSize;
        ulong entriesPerGroup = (ulong)_blockSize * 8;
        uint  blocksPerGroup  = (uint)((entriesPerGroup + entriesPerBlock - 1) / entriesPerBlock) + 2;

        ulong group         = ino / entriesPerGroup;
        ulong groupOffset   = ino % entriesPerGroup;
        ulong logicalBlock  = group * blocksPerGroup + 2 + groupOffset / entriesPerBlock;
        var   offsetInBlock = (uint)(groupOffset % entriesPerBlock * inodeSize);

        AaruLogging.Debug(MODULE_NAME,
                          "Ifile palloc: group={0}, groupOffset={1}, logicalBlock={2}, offset={3}",
                          group,
                          groupOffset,
                          logicalBlock,
                          offsetInBlock);

        // The ifile is NOT a root metadata file, so bmap values are virtual block numbers
        // that need DAT translation
        ErrorNumber errno = ReadLogicalBlock(ifileInode, logicalBlock, false, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading ifile block: {0}", errno);

            return errno;
        }

        int inodeStructSize = Marshal.SizeOf<Inode>();

        if(offsetInBlock + inodeStructSize > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode extends past block boundary");

            return ErrorNumber.InvalidArgument;
        }

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(blockData, (int)offsetInBlock, inodeStructSize);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: size={1}, blocks={2}, mode=0x{3:X4}",
                          ino,
                          inode.size,
                          inode.blocks,
                          inode.mode);

        return ErrorNumber.NoError;
    }
}