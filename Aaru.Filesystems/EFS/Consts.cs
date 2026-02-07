// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements identification for the SGI Extent FileSystem</summary>
public sealed partial class EFS
{
    /// <summary>Original EFS magic number.</summary>
    const uint EFS_MAGIC = 0x00072959;

    /// <summary>New EFS magic number (IRIX 3.3+).</summary>
    const uint EFS_MAGIC_NEW = 0x0007295A;

    /// <summary>Filesystem type identifier.</summary>
    const string FS_TYPE = "efs";

    /// <summary>Number of directly mappable extents in an inode.</summary>
    const int EFS_DIRECTEXTENTS = 12;

    /// <summary>Maximum number of basic blocks in an indirect extent.</summary>
    const int EFS_MAXINDIRBBS = 64;

    /// <summary>Maximum number of extents per inode.</summary>
    const int EFS_MAXEXTENTS = 32767;

    /// <summary>Maximum length of a single extent in basic blocks (256 - 8).</summary>
    const int EFS_MAXEXTENTLEN = 248;

    /// <summary>Inode size in bytes (128).</summary>
    const int EFS_INODE_SIZE = 128;

    /// <summary>Inode size shift (log2 of 128 = 7).</summary>
    const int EFS_EFSINOSHIFT = 7;

    /// <summary>Basic block size (512 bytes).</summary>
    const int EFS_BBSIZE = 512;

    /// <summary>Directory block size (same as basic block).</summary>
    const int EFS_DIRBSIZE = EFS_BBSIZE;

    /// <summary>Directory block magic number.</summary>
    const ushort EFS_DIRBLK_MAGIC = 0xBEEF;

    /// <summary>Maximum filename length (255).</summary>
    const int EFS_MAXNAMELEN = 255;

    /// <summary>Minimum directory entry size (6 bytes: 4 for inum + 1 for namelen + 1 for min name).</summary>
    const int EFS_DENTSIZE = 6;

    /// <summary>Size of directory block header.</summary>
    const int EFS_DIRBLK_HEADERSIZE = 4;

    /// <summary>Superblock location in basic blocks.</summary>
    const int EFS_SUPERBB = 1;

    /// <summary>Bitmap location in basic blocks (for non-grown filesystems).</summary>
    const int EFS_BITMAPBB = 2;

    /// <summary>Root inode number.</summary>
    const uint EFS_ROOTINO = 2;

    /// <summary>Number of inodes per basic block.</summary>
    const int EFS_INOPBB = EFS_BBSIZE / EFS_INODE_SIZE;

    /// <summary>Maximum inline data size (96 bytes = 12 extents * 8 bytes).</summary>
    const int EFS_MAX_INLINE = EFS_DIRECTEXTENTS * 8;
}