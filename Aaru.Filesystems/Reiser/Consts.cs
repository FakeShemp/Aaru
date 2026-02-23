// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser filesystem plugin
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
/// <summary>Implements the Reiser v3 filesystem</summary>
public sealed partial class Reiser
{
    const string MODULE_NAME = "ReiserFS";

    const uint   REISER_SUPER_OFFSET = 0x10000;
    const string FS_TYPE             = "reiserfs";

    // Root directory key components
    const uint REISERFS_ROOT_OBJECTID        = 2;
    const uint REISERFS_ROOT_PARENT_OBJECTID = 1;

    // Item types
    const int TYPE_STAT_DATA = 0;
    const int TYPE_INDIRECT  = 1;
    const int TYPE_DIRECT    = 2;
    const int TYPE_DIRENTRY  = 3;
    const int TYPE_MAXTYPE   = 3;
    const int TYPE_ANY       = 15;

    // Key formats
    const int KEY_FORMAT_3_5 = 0;
    const int KEY_FORMAT_3_6 = 1;

    // Directory entry offsets
    const uint DOT_OFFSET     = 1;
    const uint DOT_DOT_OFFSET = 2;

    // V1 uniqueness values (encode item type in v3.5 keys)
    const uint V1_SD_UNIQUENESS       = 0;
    const uint V1_INDIRECT_UNIQUENESS = 0xFFFFFFFE;
    const uint V1_DIRECT_UNIQUENESS   = 0xFFFFFFFF;
    const uint V1_DIRENTRY_UNIQUENESS = 500;
    const uint V1_ANY_UNIQUENESS      = 555;

    // Directory entry header state flags
    const int DEH_VISIBLE = 2;

    // Block level values
    const int FREE_LEVEL           = 0;
    const int DISK_LEAF_NODE_LEVEL = 1;

    // Hash function codes (from superblock)
    const uint UNSET_HASH   = 0;
    const uint TEA_HASH     = 1;
    const uint YURA_HASH    = 2;
    const uint R5_HASH      = 3;
    const uint DEFAULT_HASH = R5_HASH;

    // Superblock version values
    const ushort REISERFS_VERSION_1 = 0;
    const ushort REISERFS_VERSION_2 = 2;

    // Filesystem state values
    const ushort REISERFS_VALID_FS = 1;
    const ushort REISERFS_ERROR_FS = 2;

    // Maximum filename length
    const int REISERFS_MAX_NAME = 255;

    // Sizes of on-disk structures
    const int BLKH_SIZE = 24; // sizeof(BlockHead): 2+2+2+2+16
    const int KEY_SIZE  = 16; // sizeof(Key): 4+4+8
    const int IH_SIZE   = 24; // sizeof(ItemHead): 16+2+2+2+2
    const int DC_SIZE   = 8;  // sizeof(DiskChild): 4+2+2
    const int DEH_SIZE  = 16; // sizeof(DirectoryEntryHead): 4+4+4+2+2

    // POSIX inode mode masks
    const ushort S_IFMT   = 0xF000; // file type mask
    const ushort S_IFSOCK = 0xC000; // socket
    const ushort S_IFLNK  = 0xA000; // symbolic link
    const ushort S_IFREG  = 0x8000; // regular file
    const ushort S_IFBLK  = 0x6000; // block device
    const ushort S_IFDIR  = 0x4000; // directory
    const ushort S_IFCHR  = 0x2000; // character device
    const ushort S_IFIFO  = 0x1000; // FIFO/pipe
    const ushort S_ISUID  = 0x0800; // set-user-ID
    const ushort S_ISGID  = 0x0400; // set-group-ID
    const ushort S_ISVTX  = 0x0200; // sticky bit

    // Persistent inode flags stored in StatDataV2.sd_attrs (ext2-compatible)
    const ushort REISERFS_SECRM_FL     = 0x0001; // secure deletion
    const ushort REISERFS_UNRM_FL      = 0x0002; // undelete
    const ushort REISERFS_SYNC_FL      = 0x0008; // synchronous writes
    const ushort REISERFS_IMMUTABLE_FL = 0x0010; // immutable file
    const ushort REISERFS_APPEND_FL    = 0x0020; // append-only
    const ushort REISERFS_NODUMP_FL    = 0x0040; // no dump
    const ushort REISERFS_NOATIME_FL   = 0x0080; // no access time update
    const ushort REISERFS_NOTAIL_FL    = 0x8000; // no tail packing

    // Maximum tree height
    const int MAX_HEIGHT = 5;

    // Xattr support
    const string PRIVROOT_NAME        = ".reiserfs_priv";
    const string XAROOT_NAME          = "xattrs";
    const uint   REISERFS_XATTR_MAGIC = 0x52465841; // "RFXA"
    const int    XATTR_HEADER_SIZE    = 8;          // sizeof(XattrHeader): 4+4

    // Journal description block magic
    static readonly byte[] JOURNAL_DESC_MAGIC = "ReIsErLB"u8.ToArray();

    readonly byte[] _magic35 = "ReIsErFs\0\0"u8.ToArray();
    readonly byte[] _magic36 = "ReIsEr2Fs\0"u8.ToArray();
    readonly byte[] _magicJr = "ReIsEr3Fs\0"u8.ToArray();
}