// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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
public sealed partial class BFS
{
    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNum">The inode number (starting from BFS_ROOT_INO = 2)</param>
    /// <param name="inode">The read inode</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNum, out Inode inode)
    {
        inode = default(Inode);

        if(inodeNum < BFS_ROOT_INO || inodeNum > _lastInode)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNum);

            return ErrorNumber.InvalidArgument;
        }

        // Inodes start at block 1, 8 inodes per block, each inode is 64 bytes
        // Block = (ino - BFS_ROOT_INO) / BFS_INODES_PER_BLOCK + 1
        uint block  = (inodeNum - BFS_ROOT_INO) / BFS_INODES_PER_BLOCK + 1;
        uint offset = (inodeNum - BFS_ROOT_INO) % BFS_INODES_PER_BLOCK * 64;

        ErrorNumber errno = ReadBlock(block, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode block {0}: {1}", block, errno);

            return errno;
        }

        inode = _littleEndian
                    ? Marshal.ByteArrayToStructureLittleEndian<Inode>(blockData, (int)offset, 64)
                    : Marshal.ByteArrayToStructureBigEndian<Inode>(blockData, (int)offset, 64);

        return ErrorNumber.NoError;
    }
}