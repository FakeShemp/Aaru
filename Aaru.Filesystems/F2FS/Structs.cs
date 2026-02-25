// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Flash-Friendly File System (F2FS)</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class F2FS
{
#region Nested type: Device

    /// <summary>Device descriptor for multi-device support (struct f2fs_device)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Device
    {
        /// <summary>Device path</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_PATH_LEN)]
        public readonly byte[] path;
        /// <summary>Total number of segments in device</summary>
        public readonly uint total_segments;
    }

#endregion

#region Nested type: Superblock

    /// <summary>F2FS super block (struct f2fs_super_block)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Superblock
    {
        /// <summary>Magic Number</summary>
        public readonly uint   magic;
        /// <summary>Major Version</summary>
        public readonly ushort major_ver;
        /// <summary>Minor Version</summary>
        public readonly ushort minor_ver;
        /// <summary>log2 sector size in bytes</summary>
        public readonly uint   log_sectorsize;
        /// <summary>log2 # of sectors per block</summary>
        public readonly uint   log_sectors_per_block;
        /// <summary>log2 block size in bytes</summary>
        public readonly uint   log_blocksize;
        /// <summary>log2 # of blocks per segment</summary>
        public readonly uint   log_blocks_per_seg;
        /// <summary># of segments per section</summary>
        public readonly uint   segs_per_sec;
        /// <summary># of sections per zone</summary>
        public readonly uint   secs_per_zone;
        /// <summary>checksum offset inside super block</summary>
        public readonly uint   checksum_offset;
        /// <summary>total # of user blocks</summary>
        public readonly ulong  block_count;
        /// <summary>total # of sections</summary>
        public readonly uint   section_count;
        /// <summary>total # of segments</summary>
        public readonly uint   segment_count;
        /// <summary># of segments for checkpoint</summary>
        public readonly uint   segment_count_ckpt;
        /// <summary># of segments for SIT</summary>
        public readonly uint   segment_count_sit;
        /// <summary># of segments for NAT</summary>
        public readonly uint   segment_count_nat;
        /// <summary># of segments for SSA</summary>
        public readonly uint   segment_count_ssa;
        /// <summary># of segments for main area</summary>
        public readonly uint   segment_count_main;
        /// <summary>start block address of segment 0</summary>
        public readonly uint   segment0_blkaddr;
        /// <summary>start block address of checkpoint</summary>
        public readonly uint   cp_blkaddr;
        /// <summary>start block address of SIT</summary>
        public readonly uint   sit_blkaddr;
        /// <summary>start block address of NAT</summary>
        public readonly uint   nat_blkaddr;
        /// <summary>start block address of SSA</summary>
        public readonly uint   ssa_blkaddr;
        /// <summary>start block address of main area</summary>
        public readonly uint   main_blkaddr;
        /// <summary>root inode number</summary>
        public readonly uint   root_ino;
        /// <summary>node inode number</summary>
        public readonly uint   node_ino;
        /// <summary>meta inode number</summary>
        public readonly uint   meta_ino;
        /// <summary>128-bit uuid for volume</summary>
        public readonly Guid   uuid;
        /// <summary>volume name (UTF-16LE, MAX_VOLUME_NAME characters)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_VOLUME_NAME * 2)]
        public readonly byte[] volume_name;
        /// <summary># of extensions below</summary>
        public readonly uint extension_count;
        /// <summary>extension array (F2FS_MAX_EXTENSION * F2FS_EXTENSION_LEN)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = F2FS_MAX_EXTENSION * F2FS_EXTENSION_LEN)]
        public readonly byte[] extension_list;
        /// <summary># of checkpoint payload blocks</summary>
        public readonly uint cp_payload;
        /// <summary>the kernel version that last mounted this volume</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = VERSION_LEN)]
        public readonly byte[] version;
        /// <summary>the initial kernel version when volume was created</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = VERSION_LEN)]
        public readonly byte[] init_version;
        /// <summary>defined feature flags</summary>
        public readonly uint feature;
        /// <summary>versioning level for encryption</summary>
        public readonly byte encryption_level;
        /// <summary>Salt used for string2key algorithm</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] encrypt_pw_salt;
        /// <summary>device list (MAX_DEVICES entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DEVICES)]
        public readonly Device[] devs;
        /// <summary>quota inode numbers (F2FS_MAX_QUOTAS entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = F2FS_MAX_QUOTAS)]
        public readonly uint[] qf_ino;
        /// <summary># of hot file extension</summary>
        public readonly byte hot_ext_count;
        /// <summary>Filename charset encoding</summary>
        public readonly ushort s_encoding;
        /// <summary>Filename charset encoding flags</summary>
        public readonly ushort s_encoding_flags;
        /// <summary>stop checkpoint reason (MAX_STOP_REASON bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_STOP_REASON)]
        public readonly byte[] s_stop_reason;
        /// <summary>reason of image corrupts (MAX_F2FS_ERRORS bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_F2FS_ERRORS)]
        public readonly byte[] s_errors;
        /// <summary>valid reserved region</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 258)]
        public readonly byte[] reserved;
        /// <summary>checksum of superblock</summary>
        public readonly uint crc;
    }

#endregion

#region Nested type: Checkpoint

    /// <summary>F2FS checkpoint block (struct f2fs_checkpoint)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Checkpoint
    {
        /// <summary>checkpoint block version number</summary>
        public readonly ulong checkpoint_ver;
        /// <summary># of user blocks</summary>
        public readonly ulong user_block_count;
        /// <summary># of valid blocks in main area</summary>
        public readonly ulong valid_block_count;
        /// <summary># of reserved segments for gc</summary>
        public readonly uint rsvd_segment_count;
        /// <summary># of overprovision segments</summary>
        public readonly uint overprov_segment_count;
        /// <summary># of free segments in main area</summary>
        public readonly uint free_segment_count;
        /// <summary>current node segment numbers (MAX_ACTIVE_NODE_LOGS entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ACTIVE_NODE_LOGS)]
        public readonly uint[] cur_node_segno;
        /// <summary>current node block offsets (MAX_ACTIVE_NODE_LOGS entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ACTIVE_NODE_LOGS)]
        public readonly ushort[] cur_node_blkoff;
        /// <summary>current data segment numbers (MAX_ACTIVE_DATA_LOGS entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ACTIVE_DATA_LOGS)]
        public readonly uint[] cur_data_segno;
        /// <summary>current data block offsets (MAX_ACTIVE_DATA_LOGS entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ACTIVE_DATA_LOGS)]
        public readonly ushort[] cur_data_blkoff;
        /// <summary>Flags : umount and journal_present</summary>
        public readonly uint ckpt_flags;
        /// <summary>total # of one cp pack</summary>
        public readonly uint cp_pack_total_block_count;
        /// <summary>start block number of data summary</summary>
        public readonly uint cp_pack_start_sum;
        /// <summary>Total number of valid nodes</summary>
        public readonly uint valid_node_count;
        /// <summary>Total number of valid inodes</summary>
        public readonly uint valid_inode_count;
        /// <summary>Next free node number</summary>
        public readonly uint next_free_nid;
        /// <summary>SIT version bitmap byte size (default value 64)</summary>
        public readonly uint sit_ver_bitmap_bytesize;
        /// <summary>NAT version bitmap byte size (default value 256)</summary>
        public readonly uint nat_ver_bitmap_bytesize;
        /// <summary>checksum offset inside cp block</summary>
        public readonly uint checksum_offset;
        /// <summary>mounted time</summary>
        public readonly ulong elapsed_time;
        /// <summary>allocation type of current segment (MAX_ACTIVE_LOGS entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ACTIVE_LOGS)]
        public readonly byte[] alloc_type;
        /* SIT and NAT version bitmap follows as a variable-length field */
    }

#endregion

#region Nested type: OrphanBlock

    /// <summary>Orphan inode block (struct f2fs_orphan_block)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct OrphanBlock
    {
        /// <summary>inode numbers (F2FS_ORPHANS_PER_BLOCK entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = F2FS_ORPHANS_PER_BLOCK)]
        public readonly uint[] ino;
        /// <summary>reserved</summary>
        public readonly uint reserved;
        /// <summary>block index in current CP</summary>
        public readonly ushort blk_addr;
        /// <summary>Number of orphan inode blocks in CP</summary>
        public readonly ushort blk_count;
        /// <summary>Total number of orphan nodes in current CP</summary>
        public readonly uint entry_count;
        /// <summary>CRC32 for orphan inode block</summary>
        public readonly uint check_sum;
    }

#endregion

#region Nested type: Extent

    /// <summary>F2FS extent structure (struct f2fs_extent)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Extent
    {
        /// <summary>start file offset of the extent</summary>
        public readonly uint fofs;
        /// <summary>start block address of the extent</summary>
        public readonly uint blk;
        /// <summary>length of the extent</summary>
        public readonly uint len;
    }

#endregion

#region Nested type: NodeFooter

    /// <summary>Node footer (struct node_footer)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct NodeFooter
    {
        /// <summary>node id</summary>
        public readonly uint nid;
        /// <summary>inode number</summary>
        public readonly uint ino;
        /// <summary>include cold/fsync/dentry marks and offset</summary>
        public readonly uint flag;
        /// <summary>checkpoint version</summary>
        public readonly ulong cp_ver;
        /// <summary>next node page block address</summary>
        public readonly uint next_blkaddr;
    }

#endregion

#region Nested type: Inode

    /// <summary>F2FS on-disk inode (struct f2fs_inode)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Inode
    {
        /// <summary>file mode</summary>
        public readonly ushort i_mode;
        /// <summary>file hints</summary>
        public readonly byte i_advise;
        /// <summary>file inline flags</summary>
        public readonly byte i_inline;
        /// <summary>user ID</summary>
        public readonly uint i_uid;
        /// <summary>group ID</summary>
        public readonly uint i_gid;
        /// <summary>links count</summary>
        public readonly uint i_links;
        /// <summary>file size in bytes</summary>
        public readonly ulong i_size;
        /// <summary>file size in blocks</summary>
        public readonly ulong i_blocks;
        /// <summary>access time</summary>
        public readonly ulong i_atime;
        /// <summary>change time</summary>
        public readonly ulong i_ctime;
        /// <summary>modification time</summary>
        public readonly ulong i_mtime;
        /// <summary>access time in nano scale</summary>
        public readonly uint i_atime_nsec;
        /// <summary>change time in nano scale</summary>
        public readonly uint i_ctime_nsec;
        /// <summary>modification time in nano scale</summary>
        public readonly uint i_mtime_nsec;
        /// <summary>file version (for NFS)</summary>
        public readonly uint i_generation;
        /// <summary>only for directory depth (overlaps with i_gc_failures for regular files)</summary>
        public readonly uint i_current_depth;
        /// <summary>nid to save xattr</summary>
        public readonly uint i_xattr_nid;
        /// <summary>file attributes</summary>
        public readonly uint i_flags;
        /// <summary>parent inode number</summary>
        public readonly uint i_pino;
        /// <summary>file name length</summary>
        public readonly uint i_namelen;
        /// <summary>file name for SPOR (F2FS_NAME_LEN bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = F2FS_NAME_LEN)]
        public readonly byte[] i_name;
        /// <summary>dentry_level for large dir</summary>
        public readonly byte i_dir_level;
        /// <summary>caching a largest extent</summary>
        public readonly Extent i_ext;
        /// <summary>
        ///     Data block addresses or extra inode attributes.
        ///     When F2FS_EXTRA_ATTR is set, the beginning of this area contains:
        ///     i_extra_isize (u16), i_inline_xattr_size (u16), i_projid (u32),
        ///     i_inode_checksum (u32), i_crtime (u64), i_crtime_nsec (u32),
        ///     i_compr_blocks (u64), i_compress_algorithm (u8), i_log_cluster_size (u8),
        ///     i_compress_flag (u16), i_extra_end[0].
        ///     The remaining bytes are data block addresses.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DEF_ADDRS_PER_INODE)]
        public readonly uint[] i_addr;
        /// <summary>direct(2), indirect(2), double_indirect(1) node ids</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DEF_NIDS_PER_INODE)]
        public readonly uint[] i_nid;
    }

#endregion

#region Nested type: DirectNode

    /// <summary>Direct node block (struct direct_node)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct DirectNode
    {
        /// <summary>array of data block addresses (DEF_ADDRS_PER_BLOCK entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DEF_ADDRS_PER_BLOCK)]
        public readonly uint[] addr;
    }

#endregion

#region Nested type: IndirectNode

    /// <summary>Indirect node block (struct indirect_node)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct IndirectNode
    {
        /// <summary>array of node ids (NIDS_PER_BLOCK entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NIDS_PER_BLOCK)]
        public readonly uint[] nid;
    }

#endregion

#region Nested type: NatEntry

    /// <summary>NAT entry (struct f2fs_nat_entry)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct NatEntry
    {
        /// <summary>latest version of cached nat entry</summary>
        public readonly byte version;
        /// <summary>inode number</summary>
        public readonly uint ino;
        /// <summary>block address</summary>
        public readonly uint block_addr;
    }

#endregion

#region Nested type: NatBlock

    /// <summary>NAT block (struct f2fs_nat_block)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct NatBlock
    {
        /// <summary>NAT entries (NAT_ENTRY_PER_BLOCK entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAT_ENTRY_PER_BLOCK)]
        public readonly NatEntry[] entries;
    }

#endregion

#region Nested type: SitEntry

    /// <summary>SIT entry (struct f2fs_sit_entry)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct SitEntry
    {
        /// <summary>
        ///     [15:10] allocation type such as CURSEG_XXXX_TYPE,
        ///     [9:0] valid block count
        /// </summary>
        public readonly ushort vblocks;
        /// <summary>bitmap for valid blocks (SIT_VBLOCK_MAP_SIZE bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIT_VBLOCK_MAP_SIZE)]
        public readonly byte[] valid_map;
        /// <summary>segment age for cleaning</summary>
        public readonly ulong mtime;
    }

#endregion

#region Nested type: SitBlock

    /// <summary>SIT block (struct f2fs_sit_block)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct SitBlock
    {
        /// <summary>SIT entries (SIT_ENTRY_PER_BLOCK entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIT_ENTRY_PER_BLOCK)]
        public readonly SitEntry[] entries;
    }

#endregion

#region Nested type: Summary

    /// <summary>Segment summary entry (struct f2fs_summary)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct Summary
    {
        /// <summary>parent node id</summary>
        public readonly uint nid;
        /// <summary>node version number</summary>
        public readonly byte version;
        /// <summary>block index in parent node</summary>
        public readonly ushort ofs_in_node;
    }

#endregion

#region Nested type: SummaryFooter

    /// <summary>Summary block footer (struct summary_footer)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct SummaryFooter
    {
        /// <summary>SUM_TYPE_XXX</summary>
        public readonly byte entry_type;
        /// <summary>summary checksum</summary>
        public readonly uint check_sum;
    }

#endregion

#region Nested type: NatJournalEntry

    /// <summary>NAT journal entry (struct nat_journal_entry)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct NatJournalEntry
    {
        /// <summary>node id</summary>
        public readonly uint nid;
        /// <summary>NAT entry</summary>
        public readonly NatEntry ne;
    }

#endregion

#region Nested type: NatJournal

    /// <summary>NAT journal (struct nat_journal)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct NatJournal
    {
        /// <summary>NAT journal entries (NAT_JOURNAL_ENTRIES entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAT_JOURNAL_ENTRIES)]
        public readonly NatJournalEntry[] entries;
        /// <summary>reserved (NAT_JOURNAL_RESERVED bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAT_JOURNAL_RESERVED)]
        public readonly byte[] reserved;
    }

#endregion

#region Nested type: SitJournalEntry

    /// <summary>SIT journal entry (struct sit_journal_entry)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct SitJournalEntry
    {
        /// <summary>segment number</summary>
        public readonly uint segno;
        /// <summary>SIT entry</summary>
        public readonly SitEntry se;
    }

#endregion

#region Nested type: SitJournal

    /// <summary>SIT journal (struct sit_journal)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct SitJournal
    {
        /// <summary>SIT journal entries (SIT_JOURNAL_ENTRIES entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIT_JOURNAL_ENTRIES)]
        public readonly SitJournalEntry[] entries;
        /// <summary>reserved (SIT_JOURNAL_RESERVED bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIT_JOURNAL_RESERVED)]
        public readonly byte[] reserved;
    }

#endregion

#region Nested type: ExtraInfo

    /// <summary>Extra info stored in journal area (struct f2fs_extra_info)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct ExtraInfo
    {
        /// <summary>kilobytes written</summary>
        public readonly ulong kbytes_written;
        /// <summary>reserved (EXTRA_INFO_RESERVED bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EXTRA_INFO_RESERVED)]
        public readonly byte[] reserved;
    }

#endregion

#region Nested type: DirEntry

    /// <summary>Directory entry (struct f2fs_dir_entry)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct DirEntry
    {
        /// <summary>hash code of file name</summary>
        public readonly uint hash_code;
        /// <summary>inode number</summary>
        public readonly uint ino;
        /// <summary>length of file name</summary>
        public readonly ushort name_len;
        /// <summary>file type</summary>
        public readonly byte file_type;
    }

#endregion

#region Nested type: DentryBlock

    /// <summary>Block-sized directory entry block (struct f2fs_dentry_block)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct DentryBlock
    {
        /// <summary>validity bitmap for directory entries in each block</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIZE_OF_DENTRY_BITMAP)]
        public readonly byte[] dentry_bitmap;
        /// <summary>reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIZE_OF_RESERVED)]
        public readonly byte[] reserved;
        /// <summary>directory entries (NR_DENTRY_IN_BLOCK entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NR_DENTRY_IN_BLOCK)]
        public readonly DirEntry[] dentry;
        /// <summary>file names (NR_DENTRY_IN_BLOCK * F2FS_SLOT_LEN bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NR_DENTRY_IN_BLOCK * 8)]
        public readonly byte[] filename;
    }

#endregion

#region Nested type: XattrHeader

    /// <summary>Extended attribute header (struct f2fs_xattr_header)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct XattrHeader
    {
        /// <summary>magic number for identification</summary>
        public readonly uint h_magic;
        /// <summary>reference count</summary>
        public readonly uint h_refcount;
        /// <summary>reserved (4 x uint, zero right now)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly uint[] h_reserved;
    }

#endregion

#region Nested type: XattrEntry

    /// <summary>Extended attribute entry (struct f2fs_xattr_entry), excluding variable-length name</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct XattrEntry
    {
        /// <summary>attribute name index</summary>
        public readonly byte e_name_index;
        /// <summary>attribute name length</summary>
        public readonly byte e_name_len;
        /// <summary>size of attribute value</summary>
        public readonly ushort e_value_size;
        /* variable-length e_name[] follows */
    }

#endregion

#region Nested type: AclHeader

    /// <summary>ACL header (struct f2fs_acl_header)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct AclHeader
    {
        /// <summary>ACL version</summary>
        public readonly uint a_version;
    }

#endregion

#region Nested type: AclEntry

    /// <summary>ACL entry (struct f2fs_acl_entry)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct AclEntry
    {
        /// <summary>tag</summary>
        public readonly ushort e_tag;
        /// <summary>permissions</summary>
        public readonly ushort e_perm;
        /// <summary>user or group id</summary>
        public readonly uint e_id;
    }

#endregion

#region Nested type: AclEntryShort

    /// <summary>Short ACL entry without id (struct f2fs_acl_entry_short)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct AclEntryShort
    {
        /// <summary>tag</summary>
        public readonly ushort e_tag;
        /// <summary>permissions</summary>
        public readonly ushort e_perm;
    }

#endregion

#region Nested type: CompressData

    /// <summary>Compressed data header (struct compress_data from f2fs.h)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct CompressData
    {
        /// <summary>compressed data size</summary>
        public readonly uint clen;
        /// <summary>compressed data checksum</summary>
        public readonly uint chksum;
        /// <summary>reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = COMPRESS_DATA_RESERVED_SIZE)]
        public readonly uint[] reserved;
        /* variable-length compressed data (cdata[]) follows */
    }

#endregion
}