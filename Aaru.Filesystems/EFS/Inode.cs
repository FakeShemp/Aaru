// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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
public sealed partial class EFS
{
    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNumber">Inode number to read</param>
    /// <param name="inode">The read inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNumber, out Inode inode)
    {
        inode = default(Inode);

        // Calculate inode location using EFS layout macros
        // EFS_ITOBB: inode to disk bb number
        // fs_firstcg + (cg * fs_cgfsize) + ((inum / inodes_per_bb) % fs_cgisize)
        var cylinderGroup   = (int)(inodeNumber / _inodesPerCg);
        var cgInodeOffset   = (int)(inodeNumber % _inodesPerCg);
        int bbInCg          = cgInodeOffset >> EFS_INOPBBSHIFT;
        var inodeOffsetInBb = (int)(inodeNumber & EFS_INOPBB - 1);

        int blockNumber = _superblock.sb_firstcg + cylinderGroup * _superblock.sb_cgfsize + bbInCg;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0}: cg={1}, bb={2}, offset={3}",
                          inodeNumber,
                          cylinderGroup,
                          blockNumber,
                          inodeOffsetInBb);

        // Read the basic block containing the inode
        ErrorNumber errno = ReadBasicBlock(blockNumber, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode block: {0}", errno);

            return errno;
        }

        // Extract the inode from the block
        int inodeOffset = inodeOffsetInBb * EFS_INODE_SIZE;

        if(inodeOffset + EFS_INODE_SIZE > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode offset exceeds block size");

            return ErrorNumber.InvalidArgument;
        }

        inode = Marshal.ByteArrayToStructureBigEndian<Inode>(blockData, inodeOffset, EFS_INODE_SIZE);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: mode=0x{1:X4}, size={2}, extents={3}",
                          inodeNumber,
                          inode.di_mode,
                          inode.di_size,
                          inode.di_numextents);

        return ErrorNumber.NoError;
    }
}

