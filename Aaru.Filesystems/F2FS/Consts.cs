// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class F2FS
{
    const string FS_TYPE = "f2fs";

    // ReSharper disable InconsistentNaming
    const uint F2FS_MAGIC        = 0xF2F52010;
    const uint F2FS_SUPER_OFFSET = 1024;
    const uint F2FS_MIN_SECTOR   = 512;
    const uint F2FS_MAX_SECTOR   = 4096;
    const uint F2FS_BLOCK_SIZE   = 4096;

    // On-disk format constants from include/linux/f2fs_fs.h
    const int F2FS_MAX_EXTENSION          = 64;
    const int F2FS_EXTENSION_LEN          = 8;
    const int F2FS_MAX_QUOTAS             = 3;
    const int MAX_ACTIVE_LOGS             = 16;
    const int MAX_ACTIVE_NODE_LOGS        = 8;
    const int MAX_ACTIVE_DATA_LOGS        = 8;
    const int VERSION_LEN                 = 256;
    const int MAX_VOLUME_NAME             = 512;
    const int MAX_PATH_LEN                = 64;
    const int MAX_DEVICES                 = 8;
    const int MAX_STOP_REASON             = 32;
    const int MAX_F2FS_ERRORS             = 16;
    const int F2FS_NAME_LEN               = 255;
    const int DEF_NIDS_PER_INODE          = 5;
    const int SIT_VBLOCK_MAP_SIZE         = 64;
    const int COMPRESS_DATA_RESERVED_SIZE = 4;

    // Calculated on-disk array sizes (assuming 4K block size)
    const int DEF_ADDRS_PER_INODE    = 923;  // (4096 - 360 - 20 - 24) / 4
    const int DEF_ADDRS_PER_BLOCK    = 1018; // (4096 - 24) / 4
    const int NIDS_PER_BLOCK         = 1018; // (4096 - 24) / 4
    const int NAT_ENTRY_PER_BLOCK    = 455;  // 4096 / sizeof(NatEntry=9)
    const int SIT_ENTRY_PER_BLOCK    = 55;   // 4096 / sizeof(SitEntry=74)
    const int ENTRIES_IN_SUM         = 512;  // 4096 / 8
    const int NR_DENTRY_IN_BLOCK     = 214;  // (8*4096) / ((11+8)*8+1)
    const int SIZE_OF_DENTRY_BITMAP  = 27;   // (214+7) / 8
    const int SIZE_OF_RESERVED       = 3;    // 4096 - (19*214 + 27)
    const int F2FS_ORPHANS_PER_BLOCK = 1020; // (4096 - 16) / 4
    const int NAT_JOURNAL_ENTRIES    = 38;   // (507 - 2) / 13
    const int NAT_JOURNAL_RESERVED   = 11;   // (507 - 2) % 13
    const int SIT_JOURNAL_ENTRIES    = 6;    // (507 - 2) / 78
    const int SIT_JOURNAL_RESERVED   = 37;   // (507 - 2) % 78
    const int EXTRA_INFO_RESERVED    = 497;  // 507 - 2 - 8

    // Checkpoint flags
    const uint CP_RESIZEFS_FLAG         = 0x00004000;
    const uint CP_DISABLED_QUICK_FLAG   = 0x00002000;
    const uint CP_DISABLED_FLAG         = 0x00001000;
    const uint CP_QUOTA_NEED_FSCK_FLAG  = 0x00000800;
    const uint CP_LARGE_NAT_BITMAP_FLAG = 0x00000400;
    const uint CP_NOCRC_RECOVERY_FLAG   = 0x00000200;
    const uint CP_TRIMMED_FLAG          = 0x00000100;
    const uint CP_NAT_BITS_FLAG         = 0x00000080;
    const uint CP_CRC_RECOVERY_FLAG     = 0x00000040;
    const uint CP_FASTBOOT_FLAG         = 0x00000020;
    const uint CP_FSCK_FLAG             = 0x00000010;
    const uint CP_ERROR_FLAG            = 0x00000008;
    const uint CP_COMPACT_SUM_FLAG      = 0x00000004;
    const uint CP_ORPHAN_PRESENT_FLAG   = 0x00000002;
    const uint CP_UMOUNT_FLAG           = 0x00000001;

    // Feature flags
    const uint F2FS_FEATURE_ENCRYPT               = 0x00000001;
    const uint F2FS_FEATURE_BLKZONED              = 0x00000002;
    const uint F2FS_FEATURE_ATOMIC_WRITE          = 0x00000004;
    const uint F2FS_FEATURE_EXTRA_ATTR            = 0x00000008;
    const uint F2FS_FEATURE_PRJQUOTA              = 0x00000010;
    const uint F2FS_FEATURE_INODE_CHKSUM          = 0x00000020;
    const uint F2FS_FEATURE_FLEXIBLE_INLINE_XATTR = 0x00000040;
    const uint F2FS_FEATURE_QUOTA_INO             = 0x00000080;
    const uint F2FS_FEATURE_INODE_CRTIME          = 0x00000100;
    const uint F2FS_FEATURE_LOST_FOUND            = 0x00000200;
    const uint F2FS_FEATURE_VERITY                = 0x00000400;
    const uint F2FS_FEATURE_SB_CHKSUM             = 0x00000800;
    const uint F2FS_FEATURE_CASEFOLD              = 0x00001000;
    const uint F2FS_FEATURE_COMPRESSION           = 0x00002000;
    const uint F2FS_FEATURE_RO                    = 0x00004000;
    const uint F2FS_FEATURE_DEVICE_ALIAS          = 0x00008000;
    const uint F2FS_FEATURE_PACKED_SSA            = 0x00010000;

    // Inline inode flags
    const byte F2FS_INLINE_XATTR      = 0x01;
    const byte F2FS_INLINE_DATA       = 0x02;
    const byte F2FS_INLINE_DENTRY     = 0x04;
    const byte F2FS_DATA_EXIST        = 0x08;
    const byte F2FS_INLINE_DOTS       = 0x10;
    const byte F2FS_EXTRA_ATTR        = 0x20;
    const byte F2FS_PIN_FILE          = 0x40;
    const byte F2FS_COMPRESS_RELEASED = 0x80;

    // Summary types
    const byte SUM_TYPE_NODE = 1;
    const byte SUM_TYPE_DATA = 0;

    // SIT vblocks bit fields
    const int SIT_VBLOCKS_SHIFT = 10;

    // Xattr constants from xattr.h
    const uint F2FS_XATTR_MAGIC                   = 0xF2F52011;
    const int  F2FS_XATTR_REFCOUNT_MAX            = 1024;
    const byte F2FS_XATTR_INDEX_USER              = 1;
    const byte F2FS_XATTR_INDEX_POSIX_ACL_ACCESS  = 2;
    const byte F2FS_XATTR_INDEX_POSIX_ACL_DEFAULT = 3;
    const byte F2FS_XATTR_INDEX_TRUSTED           = 4;
    const byte F2FS_XATTR_INDEX_LUSTRE            = 5;
    const byte F2FS_XATTR_INDEX_SECURITY          = 6;
    const byte F2FS_XATTR_INDEX_ADVISE            = 7;
    const byte F2FS_XATTR_INDEX_ENCRYPTION        = 9;
    const byte F2FS_XATTR_INDEX_VERITY            = 11;

    // ACL constants from acl.h
    const ushort F2FS_ACL_VERSION = 0x0001;

    // ReSharper restore InconsistentNaming
}