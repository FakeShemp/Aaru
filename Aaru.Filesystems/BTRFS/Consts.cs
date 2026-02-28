// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the B-tree file system and shows information.
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

public sealed partial class BTRFS
{
    /// <summary>BTRFS magic "_BHRfS_M"</summary>
    const ulong BTRFS_MAGIC = 0x4D5F53665248425F;

    const string FS_TYPE = "btrfs";

    const ushort BTRFS_NAME_LEN = 255;

    // Key type constants from btrfs_tree.h
    const byte BTRFS_INODE_ITEM_KEY = 1;
    const byte BTRFS_DIR_ITEM_KEY   = 84;
    const byte BTRFS_DIR_INDEX_KEY  = 96;
    const byte BTRFS_ROOT_ITEM_KEY  = 132;
    const byte BTRFS_CHUNK_ITEM_KEY = 228;

    // Well-known object IDs
    const ulong BTRFS_ROOT_TREE_OBJECTID        = 1;
    const ulong BTRFS_FS_TREE_OBJECTID          = 5;
    const ulong BTRFS_FIRST_FREE_OBJECTID       = 256;
    const ulong BTRFS_FIRST_CHUNK_TREE_OBJECTID = 256;

    // File type constants from btrfs_tree.h
    const byte BTRFS_FT_DIR = 2;

    // Block group / chunk type flags
    const ulong BTRFS_BLOCK_GROUP_RAID0  = 0x08;
    const ulong BTRFS_BLOCK_GROUP_RAID1  = 0x10;
    const ulong BTRFS_BLOCK_GROUP_DUP    = 0x20;
    const ulong BTRFS_BLOCK_GROUP_RAID10 = 0x40;
    const ulong BTRFS_BLOCK_GROUP_RAID5  = 0x80;
    const ulong BTRFS_BLOCK_GROUP_RAID6  = 0x100;

    // Unix file type constants (S_IFMT mask and values)
    const uint S_IFMT   = 0xF000;
    const uint S_IFSOCK = 0xC000;
    const uint S_IFLNK  = 0xA000;
    const uint S_IFREG  = 0x8000;
    const uint S_IFBLK  = 0x6000;
    const uint S_IFDIR  = 0x4000;
    const uint S_IFCHR  = 0x2000;
    const uint S_IFIFO  = 0x1000;

    // Extent data key type
    const byte BTRFS_EXTENT_DATA_KEY = 108;

    // File extent types
    const byte BTRFS_FILE_EXTENT_INLINE   = 0;
    const byte BTRFS_FILE_EXTENT_REG      = 1;
    const byte BTRFS_FILE_EXTENT_PREALLOC = 2;

    // Compression types
    const byte BTRFS_COMPRESS_NONE = 0;
    const byte BTRFS_COMPRESS_ZLIB = 1;
    const byte BTRFS_COMPRESS_LZO  = 2;
    const byte BTRFS_COMPRESS_ZSTD = 3;

    // Btrfs inode flags
    const ulong BTRFS_INODE_NODATASUM = 1UL << 0;
    const ulong BTRFS_INODE_NODATACOW = 1UL << 1;
    const ulong BTRFS_INODE_COMPRESS  = 1UL << 11;
    const ulong BTRFS_INODE_IMMUTABLE = 1UL << 6;
    const ulong BTRFS_INODE_APPEND    = 1UL << 7;
    const ulong BTRFS_INODE_SYNC      = 1UL << 5;
    const ulong BTRFS_INODE_NOATIME   = 1UL << 9;
}