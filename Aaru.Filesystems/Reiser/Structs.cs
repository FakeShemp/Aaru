// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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

using System;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Reiser v3 filesystem</summary>
public sealed partial class Reiser
{
#region Nested type: JournalParameters

    /// <summary>On-disk journal parameters, embedded in the superblock.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct JournalParameters
    {
        /// <summary>Block number where the journal starts on its device.</summary>
        public readonly uint journal_1stblock;
        /// <summary>Journal device st_rdev.</summary>
        public readonly uint journal_dev;
        /// <summary>Size of the journal in blocks.</summary>
        public readonly uint journal_size;
        /// <summary>Max number of blocks in a transaction.</summary>
        public readonly uint journal_trans_max;
        /// <summary>Random value made on filesystem creation.</summary>
        public readonly uint journal_magic;
        /// <summary>Max number of blocks to batch into a transaction.</summary>
        public readonly uint journal_max_batch;
        /// <summary>In seconds, how old can an async commit be.</summary>
        public readonly uint journal_max_commit_age;
        /// <summary>In seconds, how old can a transaction be.</summary>
        public readonly uint journal_max_trans_age;
    }

#endregion

#region Nested type: Superblock

    /// <summary>
    ///     On-disk superblock. Contains the v1 fields followed by the extended v2 fields.
    ///     Located at offset 64 KiB from the start of the partition.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Superblock
    {
        /// <summary>Total number of blocks in the filesystem.</summary>
        public readonly uint block_count;
        /// <summary>Number of free blocks.</summary>
        public readonly uint free_blocks;
        /// <summary>Block number of the root node.</summary>
        public readonly uint root_block;
        /// <summary>Journal parameters.</summary>
        public readonly JournalParameters journal;
        /// <summary>Block size in bytes.</summary>
        public readonly ushort blocksize;
        /// <summary>Max size of the object id array.</summary>
        public readonly ushort oid_maxsize;
        /// <summary>Current size of the object id array.</summary>
        public readonly ushort oid_cursize;
        /// <summary>1 if filesystem was cleanly unmounted, 2 if not.</summary>
        public readonly ushort umount_state;
        /// <summary>Magic string: "ReIsErFs", "ReIsEr2Fs", or "ReIsEr3Fs".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly byte[] magic;
        /// <summary>Filesystem state, used by fsck to mark rebuild phases.</summary>
        public readonly ushort fs_state;
        /// <summary>Hash function code used for directory names.</summary>
        public readonly uint hash_function_code;
        /// <summary>Height of the disk tree.</summary>
        public readonly ushort tree_height;
        /// <summary>Number of bitmap blocks needed to address all blocks.</summary>
        public readonly ushort bmap_nr;
        /// <summary>Filesystem format version (only reliable on non-standard journal).</summary>
        public readonly ushort version;
        /// <summary>Size in blocks of the journal area reserved on main device.</summary>
        public readonly ushort reserved_for_journal;
        /// <summary>Inode generation counter.</summary>
        public readonly uint inode_generation;
        /// <summary>Superblock flags (e.g. attrs_cleared).</summary>
        public readonly uint flags;
        /// <summary>Filesystem unique identifier (UUID).</summary>
        public readonly Guid uuid;
        /// <summary>Volume label (16 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] label;
        /// <summary>Count of mounts since last fsck.</summary>
        public readonly ushort mnt_count;
        /// <summary>Maximum mounts before check.</summary>
        public readonly ushort max_mnt_count;
        /// <summary>Timestamp of last fsck.</summary>
        public readonly uint last_check;
        /// <summary>Interval between checks in seconds.</summary>
        public readonly uint check_interval;
        /// <summary>Unused, zero-filled.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 76)]
        public readonly byte[] unused;
    }

#endregion

#region Nested type: OffsetV1

    /// <summary>
    ///     Key offset for v3.5 format objects. The k_uniqueness field encodes the item type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OffsetV1
    {
        /// <summary>Offset within the object.</summary>
        public readonly uint k_offset;
        /// <summary>Uniqueness value encoding the item type.</summary>
        public readonly uint k_uniqueness;
    }

#endregion

#region Nested type: OffsetV2

    /// <summary>
    ///     Key offset for v3.6 format objects. The type is stored in the upper 4 bits
    ///     and the offset in the lower 60 bits of the 64-bit value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OffsetV2
    {
        /// <summary>
        ///     Packed 64-bit value: bits 60–63 = type, bits 0–59 = offset.
        /// </summary>
        public readonly ulong v;
    }

#endregion

#region Nested type: Key

    /// <summary>
    ///     On-disk key that determines the location of an item in the S+tree.
    ///     Composed of 4 components: directory id, object id, and offset (v1 or v2).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Key
    {
        /// <summary>Packing locality: by default the parent directory's object id.</summary>
        public readonly uint k_dir_id;
        /// <summary>Object identifier.</summary>
        public readonly uint k_objectid;
        /// <summary>
        ///     Offset and type. In v3.5 format this is an <see cref="OffsetV1" />;
        ///     in v3.6 format this is an <see cref="OffsetV2" />.
        ///     Both are 8 bytes, so this field is stored as a raw 8-byte value.
        /// </summary>
        public readonly ulong k_offset_v2;
    }

#endregion

#region Nested type: ItemHead

    /// <summary>
    ///     On-disk item header. Every item stored in a leaf node is preceded by this header.
    ///     It contains the item's key, free space or entry count, length, location within
    ///     the block, and version.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ItemHead
    {
        /// <summary>Key of the item, used to locate it in the tree.</summary>
        public readonly Key ih_key;
        /// <summary>
        ///     For indirect items: free space in the last unformatted node (0xFFFF for direct/stat items).
        ///     For directory items: number of directory entries.
        /// </summary>
        public readonly ushort ih_free_space_or_entry_count;
        /// <summary>Total size of the item body in bytes.</summary>
        public readonly ushort ih_item_len;
        /// <summary>Offset to the item body within the block.</summary>
        public readonly ushort ih_item_location;
        /// <summary>
        ///     Item version: 0 for all old (3.5) items, 2 for new (3.6) items.
        ///     Highest bit may be temporarily set by fsck.
        /// </summary>
        public readonly ushort ih_version;
    }

#endregion

#region Nested type: BlockHead

    /// <summary>
    ///     On-disk block header. Present at the start of every formatted node
    ///     (both leaf and internal nodes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BlockHead
    {
        /// <summary>Level of the block in the tree (1 = leaf, higher = internal).</summary>
        public readonly ushort blk_level;
        /// <summary>Number of keys/items in the block.</summary>
        public readonly ushort blk_nr_item;
        /// <summary>Free space in the block in bytes.</summary>
        public readonly ushort blk_free_space;
        /// <summary>Reserved.</summary>
        public readonly ushort blk_reserved;
        /// <summary>Right delimiting key, kept for compatibility.</summary>
        public readonly Key blk_right_delim_key;
    }

#endregion

#region Nested type: StatDataV1

    /// <summary>
    ///     On-disk stat data for v3.5 format objects. 32 bytes long.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct StatDataV1
    {
        /// <summary>File type and permissions.</summary>
        public readonly ushort sd_mode;
        /// <summary>Number of hard links.</summary>
        public readonly ushort sd_nlink;
        /// <summary>Owner user id.</summary>
        public readonly ushort sd_uid;
        /// <summary>Owner group id.</summary>
        public readonly ushort sd_gid;
        /// <summary>File size in bytes.</summary>
        public readonly uint sd_size;
        /// <summary>Time of last access.</summary>
        public readonly uint sd_atime;
        /// <summary>Time file was last modified.</summary>
        public readonly uint sd_mtime;
        /// <summary>Time inode (stat data) was last changed (except atime/mtime).</summary>
        public readonly uint sd_ctime;
        /// <summary>For device files: device number. For regular files: number of blocks used.</summary>
        public readonly uint sd_rdev_or_blocks;
        /// <summary>
        ///     First byte stored in a direct item. 1 means symlink,
        ///     0xFFFFFFFF means no direct item.
        /// </summary>
        public readonly uint sd_first_direct_byte;
    }

#endregion

#region Nested type: StatDataV2

    /// <summary>
    ///     On-disk stat data for v3.6 format objects. 44 bytes long.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct StatDataV2
    {
        /// <summary>File type and permissions.</summary>
        public readonly ushort sd_mode;
        /// <summary>Persistent inode flags (e.g. immutable, append, sync).</summary>
        public readonly ushort sd_attrs;
        /// <summary>Number of hard links.</summary>
        public readonly uint sd_nlink;
        /// <summary>File size in bytes (64-bit).</summary>
        public readonly ulong sd_size;
        /// <summary>Owner user id.</summary>
        public readonly uint sd_uid;
        /// <summary>Owner group id.</summary>
        public readonly uint sd_gid;
        /// <summary>Time of last access.</summary>
        public readonly uint sd_atime;
        /// <summary>Time file was last modified.</summary>
        public readonly uint sd_mtime;
        /// <summary>Time inode (stat data) was last changed (except atime/mtime).</summary>
        public readonly uint sd_ctime;
        /// <summary>Number of blocks used by the file.</summary>
        public readonly uint sd_blocks;
        /// <summary>For device files: device number. For regular files: inode generation.</summary>
        public readonly uint sd_rdev_or_generation;
    }

#endregion

#region Nested type: DirectoryEntryHead

    /// <summary>
    ///     On-disk directory entry header. Each directory entry within a directory
    ///     item is preceded by this structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectoryEntryHead
    {
        /// <summary>Third component of the directory entry key (hash of name + generation).</summary>
        public readonly uint deh_offset;
        /// <summary>Object id of the parent directory of the referenced object.</summary>
        public readonly uint deh_dir_id;
        /// <summary>Object id of the object referenced by this directory entry.</summary>
        public readonly uint deh_objectid;
        /// <summary>Offset of the entry name within the whole directory item.</summary>
        public readonly ushort deh_location;
        /// <summary>Entry state: bit 0 = has stat data (future), bit 2 = visible (not unlinked).</summary>
        public readonly ushort deh_state;
    }

#endregion

#region Nested type: DiskChild

    /// <summary>
    ///     On-disk child pointer used in internal (non-leaf) tree nodes.
    ///     Points from an internal node to a child node on disk.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DiskChild
    {
        /// <summary>Block number of the child node.</summary>
        public readonly uint dc_block_number;
        /// <summary>Used space in the child node.</summary>
        public readonly ushort dc_size;
        /// <summary>Reserved.</summary>
        public readonly ushort dc_reserved;
    }

#endregion

#region Nested type: JournalDescriptionBlock

    /// <summary>
    ///     On-disk journal description block. First block written in a transaction commit.
    ///     Followed by a variable-length array of real block locations (not represented here).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct JournalDescriptionBlock
    {
        /// <summary>Transaction id.</summary>
        public readonly uint j_trans_id;
        /// <summary>Length of the transaction in blocks. len + 1 is the commit block.</summary>
        public readonly uint j_len;
        /// <summary>Mount id of this transaction.</summary>
        public readonly uint j_mount_id;
    }

#endregion

#region Nested type: JournalCommitBlock

    /// <summary>
    ///     On-disk journal commit block. Last block written in a transaction commit.
    ///     Followed by a variable-length array of real block locations (not represented here).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct JournalCommitBlock
    {
        /// <summary>Transaction id (must match the corresponding description block).</summary>
        public readonly uint j_trans_id;
        /// <summary>Length of the transaction in blocks (must match the description block).</summary>
        public readonly uint j_len;
    }

#endregion

#region Nested type: JournalHeader

    /// <summary>
    ///     On-disk journal header block. Written whenever a transaction is considered
    ///     fully flushed and is more recent than the last fully flushed transaction.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct JournalHeader
    {
        /// <summary>Id of the last fully flushed transaction.</summary>
        public readonly uint j_last_flush_trans_id;
        /// <summary>Offset in the log of where to start replay after a crash.</summary>
        public readonly uint j_first_unflushed_offset;
        /// <summary>Mount id.</summary>
        public readonly uint j_mount_id;
        /// <summary>Journal parameters (copy of the superblock journal params).</summary>
        public readonly JournalParameters jh_journal;
    }

#endregion
}