// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX6
{
    /// <summary>Reads an inode from the inode tree</summary>
    /// <param name="inodeNum">The inode number (1-based)</param>
    /// <param name="inode">The read inode entry</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNum, out qnx6_inode_entry inode)
    {
        inode = default(qnx6_inode_entry);

        if(inodeNum == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadInode: Invalid inode number 0");

            return ErrorNumber.InvalidArgument;
        }

        // Calculate which block in the inode tree contains this inode
        // Inodes are 128 bytes each, so we need to find the right block and offset
        uint inodesPerBlock = _blockSize                      / QNX6_INODE_SIZE;
        uint blockIndex     = (inodeNum - 1)                  / inodesPerBlock;
        uint offsetInBlock  = (inodeNum - 1) % inodesPerBlock * QNX6_INODE_SIZE;

        // Map the logical block to physical block using the inode tree
        ErrorNumber errno = MapBlock(_superblock.Inode, blockIndex, out uint physicalBlock);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadInode: Error mapping block {0}: {1}", blockIndex, errno);

            return errno;
        }

        // Read the block
        errno = ReadBlock(physicalBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadInode: Error reading block {0}: {1}", physicalBlock, errno);

            return errno;
        }

        // Parse the inode
        inode = _littleEndian
                    ? Marshal.ByteArrayToStructureLittleEndian<qnx6_inode_entry>(blockData,
                        (int)offsetInBlock,
                        QNX6_INODE_SIZE)
                    : Marshal.ByteArrayToStructureBigEndian<qnx6_inode_entry>(blockData,
                                                                              (int)offsetInBlock,
                                                                              QNX6_INODE_SIZE);

        AaruLogging.Debug(MODULE_NAME,
                          "ReadInode: Inode {0} read successfully (size={1}, mode={2:X4}, status={3})",
                          inodeNum,
                          inode.di_size,
                          inode.di_mode,
                          inode.di_status);

        return ErrorNumber.NoError;
    }
}