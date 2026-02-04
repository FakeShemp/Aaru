// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
/// <summary>Implements detection of the UNIX boot filesystem</summary>
public sealed partial class BFS
{
    /// <summary>BFS magic number (little-endian)</summary>
    const uint BFS_MAGIC = 0x1BADFACE;
    /// <summary>BFS magic number (big-endian)</summary>
    const uint BFS_MAGIC_BE = 0xCEFAAD1B;

    /// <summary>Block size in bits</summary>
    const int BFS_BSIZE_BITS = 9;
    /// <summary>Block size in bytes (512)</summary>
    const int BFS_BSIZE = 1 << BFS_BSIZE_BITS;

    /// <summary>Root inode number</summary>
    const int BFS_ROOT_INO = 2;
    /// <summary>Number of inodes per block (8)</summary>
    const int BFS_INODES_PER_BLOCK = 8;

    /// <summary>Directory vnode type</summary>
    const uint BFS_VDIR = 2;
    /// <summary>Regular file vnode type</summary>
    const uint BFS_VREG = 1;

    /// <summary>Maximum filename length</summary>
    const int BFS_NAMELEN = 14;
    /// <summary>Directory entry size in bytes</summary>
    const int BFS_DIRENT_SIZE = 16;
    /// <summary>Number of directory entries per block</summary>
    const int BFS_DIRS_PER_BLOCK = 32;

    const string FS_TYPE = "bfs";
}