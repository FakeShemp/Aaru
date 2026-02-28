// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the Linux extended filesystem 2, 3 and 4 and shows information.
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "UnusedMember.Local")]

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
#region Nested type: SuperBlock

    /// <summary>ext2/3/4 superblock</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct SuperBlock
    {
        /// <summary>0x000, inodes on volume</summary>
        public readonly uint inodes;
        /// <summary>0x004, blocks on volume</summary>
        public readonly uint blocks;
        /// <summary>0x008, reserved blocks</summary>
        public readonly uint reserved_blocks;
        /// <summary>0x00C, free blocks count</summary>
        public readonly uint free_blocks;
        /// <summary>0x010, free inodes count</summary>
        public readonly uint free_inodes;
        /// <summary>0x014, first data block</summary>
        public readonly uint first_block;
        /// <summary>0x018, block size</summary>
        public uint block_size;
        /// <summary>0x01C, fragment size</summary>
        public readonly int frag_size;
        /// <summary>0x020, blocks per group</summary>
        public readonly uint blocks_per_grp;
        /// <summary>0x024, fragments per group</summary>
        public readonly uint flags_per_grp;
        /// <summary>0x028, inodes per group</summary>
        public readonly uint inodes_per_grp;
        /// <summary>0x02C, last mount time</summary>
        public readonly uint mount_t;
        /// <summary>0x030, last write time</summary>
        public readonly uint write_t;
        /// <summary>0x034, mounts count</summary>
        public readonly ushort mount_c;
        /// <summary>0x036, max mounts</summary>
        public readonly short max_mount_c;
        /// <summary>0x038, (little endian)</summary>
        public readonly ushort magic;
        /// <summary>0x03A, filesystem state</summary>
        public readonly ushort state;
        /// <summary>0x03C, behaviour on errors</summary>
        public readonly ushort err_behaviour;
        /// <summary>0x03E, From 0.5b onward</summary>
        public readonly ushort minor_revision;
        /// <summary>0x040, last check time</summary>
        public readonly uint check_t;
        /// <summary>0x044, max time between checks</summary>
        public readonly uint check_inv;

        // From 0.5a onward
        /// <summary>0x048, Creation OS</summary>
        public readonly uint creator_os;
        /// <summary>0x04C, Revison level</summary>
        public readonly uint revision;
        /// <summary>0x050, Default UID for reserved blocks</summary>
        public readonly ushort default_uid;
        /// <summary>0x052, Default GID for reserved blocks</summary>
        public readonly ushort default_gid;

        // From 0.5b onward
        /// <summary>0x054, First unreserved inode</summary>
        public readonly uint first_inode;
        /// <summary>0x058, inode size</summary>
        public readonly ushort inode_size;
        /// <summary>0x05A, Block group number of THIS superblock</summary>
        public readonly ushort block_group_no;
        /// <summary>0x05C, Compatible features set</summary>
        public readonly uint ftr_compat;
        /// <summary>0x060, Incompatible features set</summary>
        public readonly uint ftr_incompat;

        // Found on Linux 2.0.40
        /// <summary>0x064, Read-only compatible features set</summary>
        public readonly uint ftr_ro_compat;

        // Found on Linux 2.1.132
        /// <summary>0x068, 16 bytes, UUID</summary>
        public readonly Guid uuid;
        /// <summary>0x078, 16 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] volume_name;
        /// <summary>0x088, 64 bytes, where last mounted</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] last_mount_dir;
        /// <summary>0x0C8, Usage bitmap algorithm, for compression</summary>
        public readonly uint algo_usage_bmp;
        /// <summary>0x0CC, Block to try to preallocate</summary>
        public readonly byte prealloc_blks;
        /// <summary>0x0CD, Blocks to try to preallocate for directories</summary>
        public readonly byte prealloc_dir_blks;
        /// <summary>0x0CE, Per-group desc for online growth</summary>
        public readonly ushort rsrvd_gdt_blocks;

        // Found on Linux 2.4
        // ext3
        /// <summary>0x0D0, 16 bytes, UUID of journal superblock</summary>
        public readonly Guid journal_uuid;
        /// <summary>0x0E0, inode no. of journal file</summary>
        public readonly uint journal_inode;
        /// <summary>0x0E4, device no. of journal file</summary>
        public readonly uint journal_dev;
        /// <summary>0x0E8, Start of list of inodes to delete</summary>
        public readonly uint last_orphan;
        /// <summary>0x0EC, First byte of 128bit HTREE hash seed</summary>
        public readonly uint hash_seed_1;
        /// <summary>0x0F0, Second byte of 128bit HTREE hash seed</summary>
        public readonly uint hash_seed_2;
        /// <summary>0x0F4, Third byte of 128bit HTREE hash seed</summary>
        public readonly uint hash_seed_3;
        /// <summary>0x0F8, Fourth byte of 128bit HTREE hash seed</summary>
        public readonly uint hash_seed_4;
        /// <summary>0x0FC, Hash version</summary>
        public readonly byte hash_version;
        /// <summary>0x0FD, Journal backup type</summary>
        public readonly byte jnl_backup_type;
        /// <summary>0x0FE, Size of group descriptor</summary>
        public readonly ushort desc_grp_size;
        /// <summary>0x100, Default mount options</summary>
        public readonly uint default_mnt_opts;
        /// <summary>0x104, First metablock block group</summary>
        public readonly uint first_meta_bg;

        // Introduced with ext4, some can be ext3
        /// <summary>0x108, Filesystem creation time</summary>
        public readonly uint mkfs_t;

        /// <summary>Backup of the journal inode</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public readonly uint[] jnl_blocks;

        // Following 3 fields are valid if EXT4_FEATURE_INCOMPAT_64BIT is set
        /// <summary>0x150, High 32 bits of blocks count</summary>
        public readonly uint blocks_hi;
        /// <summary>0x154, High 32 bits of reserved blocks count</summary>
        public readonly uint reserved_blocks_hi;
        /// <summary>0x158, High 32 bits of free blocks count</summary>
        public readonly uint free_blocks_hi;
        /// <summary>0x15C, All inodes have at least this many bytes</summary>
        public readonly ushort min_inode_size;
        /// <summary>0x15E, New inodes should reserve this many bytes</summary>
        public readonly ushort rsv_inode_size;
        /// <summary>0x160, Miscellaneous flags</summary>
        public readonly uint flags;
        /// <summary>0x164, RAID stride</summary>
        public readonly ushort raid_stride;
        /// <summary>0x166, Seconds to wait in MMP checking</summary>
        public readonly ushort mmp_interval;
        /// <summary>0x168, Block for multi-mount protection</summary>
        public readonly ulong mmp_block;
        /// <summary>0x170, Blocks on all data disks (N*stride)</summary>
        public readonly uint raid_stripe_width;
        /// <summary>0x174, FLEX_BG group size</summary>
        public readonly byte flex_bg_grp_size;
        /// <summary>0x175, Metadata checksum algorithm used</summary>
        public readonly byte checksum_type;
        /// <summary>0x176, Versioning level for encryption</summary>
        public readonly byte encryption_level;
        /// <summary>0x177, Padding to next 32 bits</summary>
        public readonly byte reserved_pad;

        // Following are introduced with ext4
        /// <summary>0x178, Kibibytes written in volume lifetime</summary>
        public readonly ulong kbytes_written;
        /// <summary>0x180, Active snapshot inode number</summary>
        public readonly uint snapshot_inum;
        /// <summary>0x184, Active snapshot sequential ID</summary>
        public readonly uint snapshot_id;
        /// <summary>0x188, Reserved blocks for active snapshot's future use</summary>
        public readonly ulong snapshot_blocks;
        /// <summary>0x190, inode number of the on-disk start of the snapshot list</summary>
        public readonly uint snapshot_list;

        // Optional ext4 error-handling features
        /// <summary>0x194, total registered filesystem errors</summary>
        public readonly uint error_count;
        /// <summary>0x198, time on first error</summary>
        public readonly uint first_error_t;
        /// <summary>0x19C, inode involved in first error</summary>
        public readonly uint first_error_inode;
        /// <summary>0x1A0, block involved of first error</summary>
        public readonly ulong first_error_block;
        /// <summary>0x1A8, 32 bytes, function where the error happened</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] first_error_func;
        /// <summary>0x1C8, line number where error happened</summary>
        public readonly uint first_error_line;
        /// <summary>0x1CC, time of most recent error</summary>
        public readonly uint last_error_t;
        /// <summary>0x1D0, inode involved in last error</summary>
        public readonly uint last_error_inode;
        /// <summary>0x1D4, line number where error happened</summary>
        public readonly uint last_error_line;
        /// <summary>0x1D8, block involved of last error</summary>
        public readonly ulong last_error_block;
        /// <summary>0x1E0, 32 bytes, function where the error happened</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] last_error_func;

        // End of optional error-handling features

        /// <summary>0x200, 64 bytes, last used mount options</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mount_options;

        /// <summary>0x240, Inode for tracking user quota</summary>
        public readonly uint usr_quota_inum;
        /// <summary>0x244, Inode for tracking group quota</summary>
        public readonly uint grp_quota_inum;
        /// <summary>0x248, Overhead blocks/clusters in filesystem</summary>
        public readonly uint overhead_clusters;
        /// <summary>0x24C, Groups with sparse_super2 SBs</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly uint[] backup_bgs;
        /// <summary>0x254, Encryption algorithms in use</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] encrypt_algos;
        /// <summary>0x258, Salt used for string2key algorithm</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] encrypt_pw_salt;
        /// <summary>0x268, Inode number of lost+found</summary>
        public readonly uint lpf_inum;
        /// <summary>0x26C, Inode number for tracking project quota</summary>
        public readonly uint prj_quota_inum;
        /// <summary>0x270, crc32c(uuid) if csum_seed is set</summary>
        public readonly uint checksum_seed;
        /// <summary>0x274, Write time high 8 bits</summary>
        public readonly byte wtime_hi;
        /// <summary>0x275, Mount time high 8 bits</summary>
        public readonly byte mtime_hi;
        /// <summary>0x276, mkfs time high 8 bits</summary>
        public readonly byte mkfs_time_hi;
        /// <summary>0x277, Last check time high 8 bits</summary>
        public readonly byte lastcheck_hi;
        /// <summary>0x278, First error time high 8 bits</summary>
        public readonly byte first_error_time_hi;
        /// <summary>0x279, Last error time high 8 bits</summary>
        public readonly byte last_error_time_hi;
        /// <summary>0x27A, First error errcode</summary>
        public readonly byte first_error_errcode;
        /// <summary>0x27B, Last error errcode</summary>
        public readonly byte last_error_errcode;
        /// <summary>0x27C, Filename charset encoding</summary>
        public readonly ushort encoding;
        /// <summary>0x27E, Filename charset encoding flags</summary>
        public readonly ushort encoding_flags;
        /// <summary>0x280, Inode for tracking orphan inodes</summary>
        public readonly uint orphan_file_inum;
        /// <summary>0x284, Default uid for reserved blocks (high 16 bits)</summary>
        public readonly ushort def_resuid_hi;
        /// <summary>0x286, Default gid for reserved blocks (high 16 bits)</summary>
        public readonly ushort def_resgid_hi;
        /// <summary>0x288, Padding to the end of the block</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 93)]
        public readonly uint[] reserved;
        /// <summary>0x3FC, crc32c(superblock)</summary>
        public readonly uint checksum;
    }

#endregion

#region Nested type: BlockGroupDescriptor

    /// <summary>ext2/3/4 block group descriptor</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct BlockGroupDescriptor
    {
        /// <summary>0x00, Blocks bitmap block (low 32 bits)</summary>
        public readonly uint block_bitmap_lo;
        /// <summary>0x04, Inodes bitmap block (low 32 bits)</summary>
        public readonly uint inode_bitmap_lo;
        /// <summary>0x08, Inodes table block (low 32 bits)</summary>
        public readonly uint inode_table_lo;
        /// <summary>0x0C, Free blocks count (low 16 bits)</summary>
        public readonly ushort free_blocks_count_lo;
        /// <summary>0x0E, Free inodes count (low 16 bits)</summary>
        public readonly ushort free_inodes_count_lo;
        /// <summary>0x10, Directories count (low 16 bits)</summary>
        public readonly ushort used_dirs_count_lo;
        /// <summary>0x12, EXT4_BG_flags (INODE_UNINIT, etc)</summary>
        public readonly ushort bg_flags;
        /// <summary>0x14, Exclude bitmap for snapshots (low 32 bits)</summary>
        public readonly uint exclude_bitmap_lo;
        /// <summary>0x18, crc32c(s_uuid+grp_num+bbitmap) LE (low 16 bits)</summary>
        public readonly ushort block_bitmap_csum_lo;
        /// <summary>0x1A, crc32c(s_uuid+grp_num+ibitmap) LE (low 16 bits)</summary>
        public readonly ushort inode_bitmap_csum_lo;
        /// <summary>0x1C, Unused inodes count (low 16 bits)</summary>
        public readonly ushort itable_unused_lo;
        /// <summary>0x1E, crc16(sb_uuid+group+desc)</summary>
        public readonly ushort bg_checksum;

        // Following fields only exist if s_desc_size >= 64 (64-bit feature)
        /// <summary>0x20, Blocks bitmap block MSB</summary>
        public readonly uint block_bitmap_hi;
        /// <summary>0x24, Inodes bitmap block MSB</summary>
        public readonly uint inode_bitmap_hi;
        /// <summary>0x28, Inodes table block MSB</summary>
        public readonly uint inode_table_hi;
        /// <summary>0x2C, Free blocks count MSB</summary>
        public readonly ushort free_blocks_count_hi;
        /// <summary>0x2E, Free inodes count MSB</summary>
        public readonly ushort free_inodes_count_hi;
        /// <summary>0x30, Directories count MSB</summary>
        public readonly ushort used_dirs_count_hi;
        /// <summary>0x32, Unused inodes count MSB</summary>
        public readonly ushort itable_unused_hi;
        /// <summary>0x34, Exclude bitmap block MSB</summary>
        public readonly uint exclude_bitmap_hi;
        /// <summary>0x38, crc32c(s_uuid+grp_num+bbitmap) BE (high 16 bits)</summary>
        public readonly ushort block_bitmap_csum_hi;
        /// <summary>0x3A, crc32c(s_uuid+grp_num+ibitmap) BE (high 16 bits)</summary>
        public readonly ushort inode_bitmap_csum_hi;
        /// <summary>0x3C, Reserved</summary>
        public readonly uint bg_reserved;
    }

#endregion

#region Nested type: Inode

    /// <summary>ext2/3/4 on-disk inode (Linux variant for OS-dependent fields)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Inode
    {
        /// <summary>0x00, File mode</summary>
        public readonly ushort mode;
        /// <summary>0x02, Low 16 bits of Owner Uid</summary>
        public readonly ushort uid;
        /// <summary>0x04, Size in bytes (low 32 bits)</summary>
        public readonly uint size_lo;
        /// <summary>0x08, Access time</summary>
        public readonly uint atime;
        /// <summary>0x0C, Inode Change time</summary>
        public readonly uint ctime;
        /// <summary>0x10, Modification time</summary>
        public readonly uint mtime;
        /// <summary>0x14, Deletion Time</summary>
        public readonly uint dtime;
        /// <summary>0x18, Low 16 bits of Group Id</summary>
        public readonly ushort gid;
        /// <summary>0x1A, Links count</summary>
        public readonly ushort links_count;
        /// <summary>0x1C, Blocks count (low 32 bits)</summary>
        public readonly uint blocks_lo;
        /// <summary>0x20, File flags</summary>
        public readonly uint i_flags;
        /// <summary>0x24, OS dependent 1 (Linux: inode version, Hurd: translator)</summary>
        public readonly uint osd1;
        /// <summary>0x28, Pointers to blocks (EXT4_N_BLOCKS = 15, 60 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public readonly uint[] block;
        /// <summary>0x64, File version (for NFS)</summary>
        public readonly uint generation;
        /// <summary>0x68, File ACL (low 32 bits)</summary>
        public readonly uint file_acl_lo;
        /// <summary>0x6C, Size high 32 bits (for regular files in rev 1+)</summary>
        public readonly uint size_high;
        /// <summary>0x70, Obsoleted fragment address</summary>
        public readonly uint obso_faddr;

        // OS dependent 2 (Linux variant)
        /// <summary>0x74, Blocks count high bits (Linux)</summary>
        public readonly ushort blocks_high;
        /// <summary>0x76, File ACL high bits (Linux)</summary>
        public readonly ushort file_acl_high;
        /// <summary>0x78, Owner Uid high 16 bits (Linux)</summary>
        public readonly ushort uid_high;
        /// <summary>0x7A, Group Id high 16 bits (Linux)</summary>
        public readonly ushort gid_high;
        /// <summary>0x7C, crc32c(uuid+inum+inode) LE (Linux)</summary>
        public readonly ushort checksum_lo;
        /// <summary>0x7E, Reserved (Linux)</summary>
        public readonly ushort osd2_reserved;

        // Extended inode fields (present when inode size > 128 bytes)
        /// <summary>0x80, On-disk additional length beyond 128-byte base</summary>
        public readonly ushort extra_isize;
        /// <summary>0x82, crc32c(uuid+inum+inode) BE</summary>
        public readonly ushort checksum_hi;
        /// <summary>0x84, Extra Change time (nsec &lt;&lt; 2 | epoch)</summary>
        public readonly uint ctime_extra;
        /// <summary>0x88, Extra Modification time (nsec &lt;&lt; 2 | epoch)</summary>
        public readonly uint mtime_extra;
        /// <summary>0x8C, Extra Access time (nsec &lt;&lt; 2 | epoch)</summary>
        public readonly uint atime_extra;
        /// <summary>0x90, File Creation time</summary>
        public readonly uint crtime;
        /// <summary>0x94, Extra File Creation time (nsec &lt;&lt; 2 | epoch)</summary>
        public readonly uint crtime_extra;
        /// <summary>0x98, High 32 bits for 64-bit version</summary>
        public readonly uint version_hi;
        /// <summary>0x9C, Project ID</summary>
        public readonly uint projid;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>ext2/3/4 original directory entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct DirectoryEntry
    {
        /// <summary>0x00, Inode number</summary>
        public readonly uint inode;
        /// <summary>0x04, Directory entry length</summary>
        public readonly ushort rec_len;
        /// <summary>0x06, Name length (full 16 bits)</summary>
        public readonly ushort name_len;
        /// <summary>0x08, File name (up to 255 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        public readonly byte[] name;
    }

#endregion

#region Nested type: DirectoryEntry2

    /// <summary>ext2/3/4 directory entry v2 (with file type)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct DirectoryEntry2
    {
        /// <summary>0x00, Inode number</summary>
        public readonly uint inode;
        /// <summary>0x04, Directory entry length</summary>
        public readonly ushort rec_len;
        /// <summary>0x06, Name length (8 bits)</summary>
        public readonly byte name_len;
        /// <summary>0x07, File type (see EXT4_FT_* constants)</summary>
        public readonly byte file_type;
        /// <summary>0x08, File name (up to 255 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        public readonly byte[] name;
    }

#endregion

#region Nested type: DirectoryEntryHash

    /// <summary>Hash placed after ext4_dir_entry_2 name for encrypted+casefolded entries</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct DirectoryEntryHash
    {
        /// <summary>0x00, Hash</summary>
        public readonly uint hash;
        /// <summary>0x04, Minor hash</summary>
        public readonly uint minor_hash;
    }

#endregion

#region Nested type: DirectoryEntryTail

    /// <summary>Bogus directory entry at the end of each leaf block that records checksums</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct DirectoryEntryTail
    {
        /// <summary>0x00, Pretend to be unused (must be zero)</summary>
        public readonly uint reserved_zero1;
        /// <summary>0x04, Always 12</summary>
        public readonly ushort rec_len;
        /// <summary>0x06, Zero name length</summary>
        public readonly byte reserved_zero2;
        /// <summary>0x07, 0xDE, fake file type</summary>
        public readonly byte reserved_ft;
        /// <summary>0x08, crc32c(uuid+inum+dirblock)</summary>
        public readonly uint det_checksum;
    }

#endregion

#region Nested type: ExtentHeader

    /// <summary>ext4 extent tree header (present at start of each extent block and in inode)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct ExtentHeader
    {
        /// <summary>0x00, Magic number (0xF30A)</summary>
        public readonly ushort magic;
        /// <summary>0x02, Number of valid entries</summary>
        public readonly ushort entries;
        /// <summary>0x04, Capacity of store in entries</summary>
        public readonly ushort max;
        /// <summary>0x06, Depth of tree (0 = leaf level with extents)</summary>
        public readonly ushort depth;
        /// <summary>0x08, Generation of the tree</summary>
        public readonly uint generation;
    }

#endregion

#region Nested type: Extent

    /// <summary>ext4 extent leaf entry (at the bottom of the extent tree)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Extent
    {
        /// <summary>0x00, First logical block extent covers</summary>
        public readonly uint block;
        /// <summary>0x04, Number of blocks covered by extent (MSB is unwritten flag)</summary>
        public readonly ushort len;
        /// <summary>0x06, High 16 bits of physical block</summary>
        public readonly ushort start_hi;
        /// <summary>0x08, Low 32 bits of physical block</summary>
        public readonly uint start_lo;
    }

#endregion

#region Nested type: ExtentIndex

    /// <summary>ext4 extent index entry (at all levels except the bottom of the extent tree)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct ExtentIndex
    {
        /// <summary>0x00, Logical block this index covers from</summary>
        public readonly uint block;
        /// <summary>0x04, Low 32 bits of physical block of next level</summary>
        public readonly uint leaf_lo;
        /// <summary>0x08, High 16 bits of physical block of next level</summary>
        public readonly ushort leaf_hi;
        /// <summary>0x0A, Unused</summary>
        public readonly ushort unused;
    }

#endregion

#region Nested type: ExtentTail

    /// <summary>ext4 extent block checksum tail (at end of non-inode extent blocks)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct ExtentTail
    {
        /// <summary>0x00, crc32c(uuid+inum+extent_block)</summary>
        public readonly uint checksum;
    }

#endregion

#region Nested type: MmpStruct

    /// <summary>ext4 multi-mount protection block</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct MmpStruct
    {
        /// <summary>0x00, Magic number for MMP (0x004D4D50)</summary>
        public readonly uint mmp_magic;
        /// <summary>0x04, Sequence number updated periodically</summary>
        public readonly uint mmp_seq;
        /// <summary>0x08, Time last updated</summary>
        public readonly ulong mmp_time;
        /// <summary>0x10, 64 bytes, node which last updated MMP block</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mmp_nodename;
        /// <summary>0x50, 32 bytes, bdev which last updated MMP block</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] mmp_bdevname;
        /// <summary>0x70, MMP check interval in seconds</summary>
        public readonly ushort mmp_check_interval;
        /// <summary>0x72, Padding</summary>
        public readonly ushort mmp_pad1;
        /// <summary>0x74, Padding</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 226)]
        public readonly uint[] mmp_pad2;
        /// <summary>0x3FC, crc32c(uuid+mmp_block)</summary>
        public readonly uint mmp_checksum;
    }

#endregion

#region Nested type: OrphanBlockTail

    /// <summary>Structure at the tail of an orphan block</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct OrphanBlockTail
    {
        /// <summary>0x00, Magic number (0x0b10ca04)</summary>
        public readonly uint ob_magic;
        /// <summary>0x04, Checksum</summary>
        public readonly uint ob_checksum;
    }

#endregion

#region Nested type: FastCommitTagLength

    /// <summary>ext4 fast commit on-disk tag-length structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct FastCommitTagLength
    {
        /// <summary>0x00, Tag type (EXT4_FC_TAG_*)</summary>
        public readonly ushort fc_tag;
        /// <summary>0x02, Length of value</summary>
        public readonly ushort fc_len;
    }

#endregion

#region Nested type: FastCommitHead

    /// <summary>ext4 fast commit head (tag EXT4_FC_TAG_HEAD)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct FastCommitHead
    {
        /// <summary>0x00, Features</summary>
        public readonly uint fc_features;
        /// <summary>0x04, Transaction ID</summary>
        public readonly uint fc_tid;
    }

#endregion

#region Nested type: FastCommitAddRange

    /// <summary>ext4 fast commit add range (tag EXT4_FC_TAG_ADD_RANGE)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct FastCommitAddRange
    {
        /// <summary>0x00, Inode number</summary>
        public readonly uint fc_ino;
        /// <summary>0x04, Extent (on-disk ext4_extent, 12 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] fc_ex;
    }

#endregion

#region Nested type: FastCommitDelRange

    /// <summary>ext4 fast commit delete range (tag EXT4_FC_TAG_DEL_RANGE)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct FastCommitDelRange
    {
        /// <summary>0x00, Inode number</summary>
        public readonly uint fc_ino;
        /// <summary>0x04, Logical block</summary>
        public readonly uint fc_lblk;
        /// <summary>0x08, Length in blocks</summary>
        public readonly uint fc_len;
    }

#endregion

#region Nested type: FastCommitDentryInfo

    /// <summary>ext4 fast commit dentry info (tags EXT4_FC_TAG_CREAT/LINK/UNLINK)</summary>
    /// <remarks>Followed by variable-length directory entry name (fc_dname)</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct FastCommitDentryInfo
    {
        /// <summary>0x00, Parent inode number</summary>
        public readonly uint fc_parent_ino;
        /// <summary>0x04, Inode number</summary>
        public readonly uint fc_ino;
    }

#endregion

#region Nested type: FastCommitInode

    /// <summary>ext4 fast commit inode (tag EXT4_FC_TAG_INODE)</summary>
    /// <remarks>Followed by variable-length raw inode data (fc_raw_inode)</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct FastCommitInode
    {
        /// <summary>0x00, Inode number</summary>
        public readonly uint fc_ino;
    }

#endregion

#region Nested type: FastCommitTail

    /// <summary>ext4 fast commit tail (tag EXT4_FC_TAG_TAIL)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct FastCommitTail
    {
        /// <summary>0x00, Transaction ID</summary>
        public readonly uint fc_tid;
        /// <summary>0x04, CRC</summary>
        public readonly uint fc_crc;
    }

#endregion
}