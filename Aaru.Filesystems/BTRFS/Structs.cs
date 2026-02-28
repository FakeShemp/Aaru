// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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

using System;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the b-tree filesystem (btrfs)</summary>
public sealed partial class BTRFS
{
#region Nested type: DevItem

    /// <summary>On-disk device item structure (btrfs_dev_item). Stores information about a device in the filesystem.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DevItem
    {
        /// <summary>The internal btrfs device id (devid).</summary>
        public readonly ulong id;

        /// <summary>Total size of the device in bytes (total_bytes).</summary>
        public readonly ulong bytes;

        /// <summary>Number of bytes used on this device (bytes_used).</summary>
        public readonly ulong used;

        /// <summary>Optimal I/O alignment for this device (io_align).</summary>
        public readonly uint optimal_align;

        /// <summary>Optimal I/O width for this device (io_width).</summary>
        public readonly uint optimal_width;

        /// <summary>Minimal I/O size (sector size) for this device (sector_size).</summary>
        public readonly uint minimal_size;

        /// <summary>Type and info about this device.</summary>
        public readonly ulong type;

        /// <summary>Expected generation for this device.</summary>
        public readonly ulong generation;

        /// <summary>Starting byte of this partition on the device, to allow for stripe alignment.</summary>
        public readonly ulong start_offset;

        /// <summary>Grouping information for allocation decisions.</summary>
        public readonly uint dev_group;

        /// <summary>Seek speed 0-100 where 100 is fastest.</summary>
        public readonly byte seek_speed;

        /// <summary>Bandwidth 0-100 where 100 is fastest.</summary>
        public readonly byte bandwidth;

        /// <summary>Btrfs generated UUID for this device (uuid).</summary>
        public readonly Guid device_uuid;

        /// <summary>UUID of the filesystem that owns this device (fsid).</summary>
        public readonly Guid uuid;
    }

#endregion

#region Nested type: SuperBlock

    /// <summary>
    ///     On-disk superblock structure (btrfs_super_block). The superblock lists the main trees of the filesystem and is
    ///     located at physical offset 0x10000 (64 KiB). Total size is 4096 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SuperBlock
    {
        /// <summary>Checksum of everything past this field (csum). 32 bytes, algorithm determined by csum_type.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public readonly byte[] checksum;

        /// <summary>Filesystem specific UUID, visible to the user (fsid).</summary>
        public readonly Guid uuid;

        /// <summary>Physical address (byte number) of this block (bytenr).</summary>
        public readonly ulong pba;

        /// <summary>Flags for this superblock.</summary>
        public readonly ulong flags;

        /// <summary>Magic number, must be "_BHRfS_M" (0x4D5F53665248425F).</summary>
        public readonly ulong magic;

        /// <summary>Generation of the superblock (transaction id).</summary>
        public readonly ulong generation;

        /// <summary>Logical address of the root tree root (root).</summary>
        public readonly ulong root_lba;

        /// <summary>Logical address of the chunk tree root (chunk_root).</summary>
        public readonly ulong chunk_lba;

        /// <summary>Logical address of the log tree root (log_root).</summary>
        public readonly ulong log_lba;

        /// <summary>
        ///     Unused log root transid (__unused_log_root_transid). This member has never been utilized; generation + 1 is
        ///     always used to read the log tree root.
        /// </summary>
        public readonly ulong log_root_transid;

        /// <summary>Total number of bytes in the filesystem.</summary>
        public readonly ulong total_bytes;

        /// <summary>Number of bytes used in the filesystem.</summary>
        public readonly ulong bytes_used;

        /// <summary>Object id of the root directory (typically 6).</summary>
        public readonly ulong root_dir_objectid;

        /// <summary>Number of devices in the filesystem.</summary>
        public readonly ulong num_devices;

        /// <summary>Sector size in bytes.</summary>
        public readonly uint sectorsize;

        /// <summary>Node (metadata block) size in bytes.</summary>
        public readonly uint nodesize;

        /// <summary>Unused leaf size (__unused_leafsize). Historically was leaf size, now unused.</summary>
        public readonly uint leafsize;

        /// <summary>Stripe size in bytes.</summary>
        public readonly uint stripesize;

        /// <summary>Size of the system chunk array in bytes (sys_chunk_array_size).</summary>
        public readonly uint n;

        /// <summary>Generation of the chunk root.</summary>
        public readonly ulong chunk_root_generation;

        /// <summary>Compatible feature flags.</summary>
        public readonly ulong compat_flags;

        /// <summary>Read-only compatible feature flags.</summary>
        public readonly ulong compat_ro_flags;

        /// <summary>Incompatible feature flags.</summary>
        public readonly ulong incompat_flags;

        /// <summary>Checksum type (see btrfs_csum_type enum: 0=CRC32, 1=XXHASH, 2=SHA256, 3=BLAKE2).</summary>
        public readonly ushort csum_type;

        /// <summary>Level of the root tree root node.</summary>
        public readonly byte root_level;

        /// <summary>Level of the chunk tree root node.</summary>
        public readonly byte chunk_root_level;

        /// <summary>Level of the log tree root node.</summary>
        public readonly byte log_root_level;

        /// <summary>Device item for the device that holds this superblock.</summary>
        public readonly DevItem dev_item;

        /// <summary>Volume label (up to 256 bytes including null terminator).</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
        public readonly string label;

        /// <summary>
        ///     Reserved area containing cache_generation (8 bytes), uuid_tree_generation (8 bytes), metadata_uuid (16 bytes),
        ///     nr_global_roots (8 bytes), and 27 reserved __le64 fields (216 bytes). Total 256 bytes.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public readonly byte[] reserved;

        /// <summary>
        ///     System chunk array (sys_chunk_array). Contains chunk items needed to bootstrap reading the chunk tree. 2048
        ///     bytes.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x800)]
        public readonly byte[] chunkpairs;

        /// <summary>
        ///     Contains 4 root backup entries (super_roots, 672 bytes) followed by padding (565 bytes) to fill the superblock
        ///     to 4096 bytes. Total 1237 bytes.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4D5)]
        public readonly byte[] unused;
    }

#endregion

#region Nested type: DiskKey

    /// <summary>
    ///     On-disk key structure (btrfs_disk_key). Defines the sort order in the B-tree. All fields are little-endian.
    ///     The key forms a 136-bit search space: (objectid &lt;&lt; 72) + (type &lt;&lt; 64) + offset.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DiskKey
    {
        /// <summary>Object id. Corresponds to the inode number for inode-related items.</summary>
        public readonly ulong objectid;

        /// <summary>Item type. Defines the kind of data stored (e.g. BTRFS_INODE_ITEM_KEY = 1).</summary>
        public readonly byte type;

        /// <summary>Offset. The starting byte offset for this key in its stream.</summary>
        public readonly ulong offset;
    }

#endregion

#region Nested type: Header

    /// <summary>
    ///     On-disk tree block header (btrfs_header). Every tree block (leaf or node) starts with this header. The first
    ///     four fields must match the superblock layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Header
    {
        /// <summary>Checksum of everything past this field. 32 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public readonly byte[] csum;

        /// <summary>Filesystem specific UUID (fsid). 16 bytes.</summary>
        public readonly Guid fsid;

        /// <summary>Logical byte number where this node is supposed to live (bytenr).</summary>
        public readonly ulong bytenr;

        /// <summary>Flags for this header (e.g. BTRFS_HEADER_FLAG_WRITTEN, BTRFS_HEADER_FLAG_RELOC).</summary>
        public readonly ulong flags;

        /// <summary>UUID of the chunk tree that allocated this block (chunk_tree_uuid). 16 bytes.</summary>
        public readonly Guid chunk_tree_uuid;

        /// <summary>Transaction generation that created or last modified this block.</summary>
        public readonly ulong generation;

        /// <summary>The tree that owns this block (objectid of the tree root).</summary>
        public readonly ulong owner;

        /// <summary>Number of items (for leaves) or key pointers (for nodes) in this block.</summary>
        public readonly uint nritems;

        /// <summary>Level of this block in the tree (0 = leaf, &gt;0 = internal node).</summary>
        public readonly byte level;
    }

#endregion

#region Nested type: RootBackup

    /// <summary>
    ///     On-disk root backup structure (btrfs_root_backup). Stored in the superblock to allow recovery if tree roots
    ///     are lost. There are BTRFS_NUM_BACKUP_ROOTS (4) entries in the superblock.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct RootBackup
    {
        /// <summary>Logical address of the root tree root.</summary>
        public readonly ulong tree_root;

        /// <summary>Generation of the root tree root.</summary>
        public readonly ulong tree_root_gen;

        /// <summary>Logical address of the chunk tree root.</summary>
        public readonly ulong chunk_root;

        /// <summary>Generation of the chunk tree root.</summary>
        public readonly ulong chunk_root_gen;

        /// <summary>Logical address of the extent tree root.</summary>
        public readonly ulong extent_root;

        /// <summary>Generation of the extent tree root.</summary>
        public readonly ulong extent_root_gen;

        /// <summary>Logical address of the filesystem tree root.</summary>
        public readonly ulong fs_root;

        /// <summary>Generation of the filesystem tree root.</summary>
        public readonly ulong fs_root_gen;

        /// <summary>Logical address of the device tree root.</summary>
        public readonly ulong dev_root;

        /// <summary>Generation of the device tree root.</summary>
        public readonly ulong dev_root_gen;

        /// <summary>Logical address of the checksum tree root.</summary>
        public readonly ulong csum_root;

        /// <summary>Generation of the checksum tree root.</summary>
        public readonly ulong csum_root_gen;

        /// <summary>Total filesystem size in bytes.</summary>
        public readonly ulong total_bytes;

        /// <summary>Number of bytes used.</summary>
        public readonly ulong bytes_used;

        /// <summary>Number of devices.</summary>
        public readonly ulong num_devices;

        /// <summary>Reserved for future use. 4 x 8 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly ulong[] unused_64;

        /// <summary>Level of the root tree root node.</summary>
        public readonly byte tree_root_level;

        /// <summary>Level of the chunk tree root node.</summary>
        public readonly byte chunk_root_level;

        /// <summary>Level of the extent tree root node.</summary>
        public readonly byte extent_root_level;

        /// <summary>Level of the filesystem tree root node.</summary>
        public readonly byte fs_root_level;

        /// <summary>Level of the device tree root node.</summary>
        public readonly byte dev_root_level;

        /// <summary>Level of the checksum tree root node.</summary>
        public readonly byte csum_root_level;

        /// <summary>Reserved for future use and alignment. 10 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly byte[] unused_8;
    }

#endregion

#region Nested type: Item

    /// <summary>
    ///     On-disk leaf item structure (btrfs_item). A leaf is full of items; offset and size indicate where to find the
    ///     item's data in the leaf (relative to the start of the data area).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Item
    {
        /// <summary>Key that identifies this item.</summary>
        public readonly DiskKey key;

        /// <summary>Offset of the item's data relative to the end of the header, in bytes.</summary>
        public readonly uint offset;

        /// <summary>Size of the item's data in bytes.</summary>
        public readonly uint size;
    }

#endregion

#region Nested type: KeyPtr

    /// <summary>
    ///     On-disk key pointer structure (btrfs_key_ptr). Used in internal (non-leaf) nodes to hold keys and pointers to
    ///     child blocks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct KeyPtr
    {
        /// <summary>Key at this position.</summary>
        public readonly DiskKey key;

        /// <summary>Logical block pointer to the child node (blockptr).</summary>
        public readonly ulong blockptr;

        /// <summary>Transaction generation of the child node.</summary>
        public readonly ulong generation;
    }

#endregion

#region Nested type: Stripe

    /// <summary>On-disk stripe structure (btrfs_stripe). Describes how a chunk maps to a physical device location.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Stripe
    {
        /// <summary>Device id this stripe resides on.</summary>
        public readonly ulong devid;

        /// <summary>Physical offset on the device.</summary>
        public readonly ulong offset;

        /// <summary>UUID of the device (dev_uuid). 16 bytes.</summary>
        public readonly Guid dev_uuid;
    }

#endregion

#region Nested type: Chunk

    /// <summary>
    ///     On-disk chunk item structure (btrfs_chunk). Stores translations from logical to physical block numbering. The
    ///     chunk tree holds these items. Followed by additional stripe entries.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Chunk
    {
        /// <summary>Size of this chunk in bytes.</summary>
        public readonly ulong length;

        /// <summary>Object id of the root referencing this chunk (owner).</summary>
        public readonly ulong owner;

        /// <summary>Stripe length in bytes.</summary>
        public readonly ulong stripe_len;

        /// <summary>Chunk type flags (BTRFS_BLOCK_GROUP_DATA, SYSTEM, METADATA, RAID profiles, etc.).</summary>
        public readonly ulong type;

        /// <summary>Optimal I/O alignment for this chunk.</summary>
        public readonly uint io_align;

        /// <summary>Optimal I/O width for this chunk.</summary>
        public readonly uint io_width;

        /// <summary>Minimal I/O size (sector size) for this chunk.</summary>
        public readonly uint sector_size;

        /// <summary>Number of stripes in this chunk (max 2^16).</summary>
        public readonly ushort num_stripes;

        /// <summary>Number of sub-stripes (only meaningful for RAID10).</summary>
        public readonly ushort sub_stripes;

        /// <summary>First stripe entry. Additional stripes follow immediately after.</summary>
        public readonly Stripe stripe;
    }

#endregion

#region Nested type: FreeSpaceEntry

    /// <summary>On-disk free space entry (btrfs_free_space_entry). Records a free or used extent in a free space cache.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FreeSpaceEntry
    {
        /// <summary>Byte offset of the extent.</summary>
        public readonly ulong offset;

        /// <summary>Length of the extent in bytes.</summary>
        public readonly ulong bytes;

        /// <summary>Type: BTRFS_FREE_SPACE_EXTENT (1) or BTRFS_FREE_SPACE_BITMAP (2).</summary>
        public readonly byte type;
    }

#endregion

#region Nested type: FreeSpaceHeader

    /// <summary>
    ///     On-disk free space header (btrfs_free_space_header). Points to the inode that holds the free space cache for a
    ///     block group.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FreeSpaceHeader
    {
        /// <summary>Key of the inode that stores the free space cache.</summary>
        public readonly DiskKey location;

        /// <summary>Generation when the free space cache was last written.</summary>
        public readonly ulong generation;

        /// <summary>Number of free space entries in the cache.</summary>
        public readonly ulong num_entries;

        /// <summary>Number of bitmap entries in the cache.</summary>
        public readonly ulong num_bitmaps;
    }

#endregion

#region Nested type: RaidStride

    /// <summary>On-disk RAID stride structure (btrfs_raid_stride). Describes a single physical location in a RAID stripe.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct RaidStride
    {
        /// <summary>The id of the device this RAID extent lives on.</summary>
        public readonly ulong devid;

        /// <summary>The physical location on the device.</summary>
        public readonly ulong physical;
    }

#endregion

#region Nested type: ExtentItem

    /// <summary>
    ///     On-disk extent item (btrfs_extent_item). Records the reference count, generation, and flags for an extent in
    ///     the extent tree. May be followed by inline back references.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtentItem
    {
        /// <summary>Number of references to this extent.</summary>
        public readonly ulong refs;

        /// <summary>Transaction generation that allocated or last modified this extent.</summary>
        public readonly ulong generation;

        /// <summary>
        ///     Flags: BTRFS_EXTENT_FLAG_DATA (1 &lt;&lt; 0) for data extents, BTRFS_EXTENT_FLAG_TREE_BLOCK (1 &lt;&lt; 1) for
        ///     tree block extents.
        /// </summary>
        public readonly ulong flags;
    }

#endregion

#region Nested type: ExtentItemV0

    /// <summary>On-disk legacy extent item (btrfs_extent_item_v0). Old format that only stores a 32-bit reference count.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtentItemV0
    {
        /// <summary>Number of references to this extent (32-bit).</summary>
        public readonly uint refs;
    }

#endregion

#region Nested type: TreeBlockInfo

    /// <summary>
    ///     On-disk tree block info (btrfs_tree_block_info). Appears after btrfs_extent_item for tree block extents when
    ///     not using skinny metadata.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct TreeBlockInfo
    {
        /// <summary>The key that identifies the first item in this tree block.</summary>
        public readonly DiskKey key;

        /// <summary>Level of the tree block.</summary>
        public readonly byte level;
    }

#endregion

#region Nested type: ExtentDataRef

    /// <summary>
    ///     On-disk extent data reference (btrfs_extent_data_ref). Records which file data references a particular extent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtentDataRef
    {
        /// <summary>Object id of the tree root that references this extent.</summary>
        public readonly ulong root;

        /// <summary>Inode number of the file that references this extent.</summary>
        public readonly ulong objectid;

        /// <summary>Offset within the file where the extent is referenced.</summary>
        public readonly ulong offset;

        /// <summary>Number of references from this file/root/offset combination.</summary>
        public readonly uint count;
    }

#endregion

#region Nested type: SharedDataRef

    /// <summary>
    ///     On-disk shared data reference (btrfs_shared_data_ref). Records a shared reference count for tree block extents
    ///     shared between snapshots.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SharedDataRef
    {
        /// <summary>Number of shared references.</summary>
        public readonly uint count;
    }

#endregion

#region Nested type: ExtentOwnerRef

    /// <summary>
    ///     On-disk extent owner reference (btrfs_extent_owner_ref). Stores the id of the subvolume which originally
    ///     created the extent. Used by simple quotas.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtentOwnerRef
    {
        /// <summary>Root id of the subvolume that owns this extent.</summary>
        public readonly ulong root_id;
    }

#endregion

#region Nested type: ExtentInlineRef

    /// <summary>
    ///     On-disk extent inline reference (btrfs_extent_inline_ref). Inline back references stored within extent items,
    ///     appearing after btrfs_extent_item or btrfs_tree_block_info.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtentInlineRef
    {
        /// <summary>
        ///     Type of the inline reference (BTRFS_TREE_BLOCK_REF_KEY, BTRFS_EXTENT_DATA_REF_KEY,
        ///     BTRFS_SHARED_BLOCK_REF_KEY, BTRFS_SHARED_DATA_REF_KEY, or BTRFS_EXTENT_OWNER_REF_KEY).
        /// </summary>
        public readonly byte type;

        /// <summary>Offset whose meaning depends on the type field.</summary>
        public readonly ulong offset;
    }

#endregion

#region Nested type: DevExtent

    /// <summary>
    ///     On-disk device extent (btrfs_dev_extent). Records free space on individual devices. The owner field points back
    ///     to the chunk allocation mapping tree that allocated the extent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DevExtent
    {
        /// <summary>Object id of the chunk tree that owns this extent.</summary>
        public readonly ulong chunk_tree;

        /// <summary>Object id within the chunk tree.</summary>
        public readonly ulong chunk_objectid;

        /// <summary>Offset of the chunk that allocated this device extent.</summary>
        public readonly ulong chunk_offset;

        /// <summary>Length of this device extent in bytes.</summary>
        public readonly ulong length;

        /// <summary>UUID of the chunk tree for double-checking ownership. 16 bytes.</summary>
        public readonly Guid chunk_tree_uuid;
    }

#endregion

#region Nested type: InodeRef

    /// <summary>
    ///     On-disk inode reference (btrfs_inode_ref). Links an inode to a directory entry. The name data follows
    ///     immediately after this structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InodeRef
    {
        /// <summary>Index of the directory entry in the directory.</summary>
        public readonly ulong index;

        /// <summary>Length of the name that follows this structure.</summary>
        public readonly ushort name_len;
    }

#endregion

#region Nested type: InodeExtref

    /// <summary>
    ///     On-disk inode extended reference (btrfs_inode_extref). Used when BTRFS_FEATURE_INCOMPAT_EXTENDED_IREF is
    ///     enabled, for hard links that exceed the inode ref item capacity. The name follows immediately after.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InodeExtref
    {
        /// <summary>Object id of the parent directory.</summary>
        public readonly ulong parent_objectid;

        /// <summary>Index of this entry in the parent directory.</summary>
        public readonly ulong index;

        /// <summary>Length of the name that follows this structure.</summary>
        public readonly ushort name_len;
    }

#endregion

#region Nested type: Timespec

    /// <summary>On-disk time specification (btrfs_timespec). Stores a timestamp with nanosecond precision.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Timespec
    {
        /// <summary>Seconds since the Unix epoch.</summary>
        public readonly ulong sec;

        /// <summary>Nanoseconds (0-999999999).</summary>
        public readonly uint nsec;
    }

#endregion

#region Nested type: InodeItem

    /// <summary>
    ///     On-disk inode item (btrfs_inode_item). Stores the metadata typically returned by stat() and other information
    ///     about file/directory characteristics. There is one for every file and directory in the filesystem.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InodeItem
    {
        /// <summary>NFS-style generation number.</summary>
        public readonly ulong generation;

        /// <summary>Transaction id that last touched this inode.</summary>
        public readonly ulong transid;

        /// <summary>Size of the file in bytes.</summary>
        public readonly ulong size;

        /// <summary>Number of bytes used on disk (including metadata overhead).</summary>
        public readonly ulong nbytes;

        /// <summary>Block group hint for allocations.</summary>
        public readonly ulong block_group;

        /// <summary>Number of hard links.</summary>
        public readonly uint nlink;

        /// <summary>User id of the owner.</summary>
        public readonly uint uid;

        /// <summary>Group id of the owner.</summary>
        public readonly uint gid;

        /// <summary>File mode (permissions and file type bits).</summary>
        public readonly uint mode;

        /// <summary>Device number (for device special files).</summary>
        public readonly ulong rdev;

        /// <summary>Inode flags (BTRFS_INODE_NODATASUM, BTRFS_INODE_NODATACOW, etc.).</summary>
        public readonly ulong flags;

        /// <summary>Modification sequence number for NFS.</summary>
        public readonly ulong sequence;

        /// <summary>Reserved for future expansion. 4 x 8 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly ulong[] reserved;

        /// <summary>Last access time.</summary>
        public readonly Timespec atime;

        /// <summary>Last inode change time.</summary>
        public readonly Timespec ctime;

        /// <summary>Last data modification time.</summary>
        public readonly Timespec mtime;

        /// <summary>Creation time.</summary>
        public readonly Timespec otime;
    }

#endregion

#region Nested type: DirLogItem

    /// <summary>On-disk directory log item (btrfs_dir_log_item). Used in the tree log for directory fsync.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirLogItem
    {
        /// <summary>End of the logged range.</summary>
        public readonly ulong end;
    }

#endregion

#region Nested type: DirItem

    /// <summary>
    ///     On-disk directory item (btrfs_dir_item). Name-to-inode pointer in a directory. The name and optional extended
    ///     attribute data follow immediately after this structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirItem
    {
        /// <summary>Key of the inode this entry points to.</summary>
        public readonly DiskKey location;

        /// <summary>Transaction id when this entry was created or last modified.</summary>
        public readonly ulong transid;

        /// <summary>Length of extended attribute data that follows the name (0 for normal directory entries).</summary>
        public readonly ushort data_len;

        /// <summary>Length of the name that follows this structure.</summary>
        public readonly ushort name_len;

        /// <summary>File type (BTRFS_FT_REG_FILE, BTRFS_FT_DIR, BTRFS_FT_SYMLINK, etc.).</summary>
        public readonly byte type;
    }

#endregion

#region Nested type: RootItem

    /// <summary>
    ///     On-disk root item (btrfs_root_item). Points to a tree root. Typically stored in the root tree used by the
    ///     superblock to find all other trees. Contains subvolume UUIDs and timestamps when the generation_v2 field is valid.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct RootItem
    {
        /// <summary>The inode item for the root directory of this tree.</summary>
        public readonly InodeItem inode;

        /// <summary>Transaction generation of this root.</summary>
        public readonly ulong generation;

        /// <summary>Object id of the root directory.</summary>
        public readonly ulong root_dirid;

        /// <summary>Logical byte number of the root node (bytenr).</summary>
        public readonly ulong bytenr;

        /// <summary>Byte limit for this root (unused, always 0).</summary>
        public readonly ulong byte_limit;

        /// <summary>Number of bytes used by this root.</summary>
        public readonly ulong bytes_used;

        /// <summary>Transaction id of the last snapshot taken from this root.</summary>
        public readonly ulong last_snapshot;

        /// <summary>Root flags (e.g. BTRFS_ROOT_SUBVOL_RDONLY).</summary>
        public readonly ulong flags;

        /// <summary>Reference count for this root.</summary>
        public readonly uint refs;

        /// <summary>Progress key for an in-progress drop operation.</summary>
        public readonly DiskKey drop_progress;

        /// <summary>Level at which the drop operation is currently working.</summary>
        public readonly byte drop_level;

        /// <summary>Level of the root node.</summary>
        public readonly byte level;

        /// <summary>
        ///     Generation v2. Copied from generation on every write. Used to validate that UUID/timestamp fields are up to
        ///     date. Fields below are only valid if generation_v2 matches generation.
        /// </summary>
        public readonly ulong generation_v2;

        /// <summary>UUID of this subvolume. 16 bytes.</summary>
        public readonly Guid uuid;

        /// <summary>UUID of the parent subvolume (for snapshots). 16 bytes.</summary>
        public readonly Guid parent_uuid;

        /// <summary>UUID of the subvolume this was received from (for send/receive). 16 bytes.</summary>
        public readonly Guid received_uuid;

        /// <summary>Transaction id when an inode in this root was last changed.</summary>
        public readonly ulong ctransid;

        /// <summary>Transaction id when this root was created.</summary>
        public readonly ulong otransid;

        /// <summary>Transaction id when this root was sent (non-zero for received subvolumes).</summary>
        public readonly ulong stransid;

        /// <summary>Transaction id when this root was received (non-zero for received subvolumes).</summary>
        public readonly ulong rtransid;

        /// <summary>Time of last inode change in this root.</summary>
        public readonly Timespec ctime;

        /// <summary>Creation time of this root.</summary>
        public readonly Timespec otime;

        /// <summary>Send time.</summary>
        public readonly Timespec stime;

        /// <summary>Receive time.</summary>
        public readonly Timespec rtime;

        /// <summary>Reserved for future use. 8 x 8 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly ulong[] reserved;
    }

#endregion

#region Nested type: RootRef

    /// <summary>
    ///     On-disk root reference (btrfs_root_ref). Used for both forward and backward root refs, linking subvolumes and
    ///     snapshots to directory entries. The name follows immediately after.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct RootRef
    {
        /// <summary>Directory id of the entry that references this root.</summary>
        public readonly ulong dirid;

        /// <summary>Index sequence number of this entry.</summary>
        public readonly ulong sequence;

        /// <summary>Length of the name that follows this structure.</summary>
        public readonly ushort name_len;
    }

#endregion

#region Nested type: DiskBalanceArgs

    /// <summary>
    ///     On-disk balance arguments (btrfs_disk_balance_args). Stores per-type filtering criteria for balance operations.
    ///     This is the on-disk version of the user-space btrfs_balance_args. Contains union fields for usage and limit ranges.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DiskBalanceArgs
    {
        /// <summary>
        ///     Profiles to operate on. Single profile is denoted by BTRFS_AVAIL_ALLOC_BIT_SINGLE.
        /// </summary>
        public readonly ulong profiles;

        /// <summary>
        ///     Usage filter (union). When BTRFS_BALANCE_ARGS_USAGE is set, this is a single upper bound. When
        ///     BTRFS_BALANCE_ARGS_USAGE_RANGE is set, the low 32 bits are usage_min and the high 32 bits are usage_max.
        /// </summary>
        public readonly ulong usage;

        /// <summary>Device id filter.</summary>
        public readonly ulong devid;

        /// <summary>Start of the physical device range filter.</summary>
        public readonly ulong pstart;

        /// <summary>End of the physical device range filter.</summary>
        public readonly ulong pend;

        /// <summary>Start of the virtual (logical) address space range filter.</summary>
        public readonly ulong vstart;

        /// <summary>End of the virtual (logical) address space range filter.</summary>
        public readonly ulong vend;

        /// <summary>Profile to convert to. Single profile is denoted by BTRFS_AVAIL_ALLOC_BIT_SINGLE.</summary>
        public readonly ulong target;

        /// <summary>BTRFS_BALANCE_ARGS_* flags controlling which filters are active.</summary>
        public readonly ulong flags;

        /// <summary>
        ///     Limit filter (union). When BTRFS_BALANCE_ARGS_LIMIT is set, this is the maximum number of chunks to process.
        ///     When BTRFS_BALANCE_ARGS_LIMIT_RANGE is set, the low 32 bits are limit_min and the high 32 bits are limit_max.
        /// </summary>
        public readonly ulong limit;

        /// <summary>Minimum number of stripes a chunk must cross (BTRFS_BALANCE_ARGS_STRIPES_RANGE).</summary>
        public readonly uint stripes_min;

        /// <summary>Maximum number of stripes a chunk must cross.</summary>
        public readonly uint stripes_max;

        /// <summary>Reserved for future use. 6 x 8 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly ulong[] unused;
    }

#endregion

#region Nested type: BalanceItem

    /// <summary>
    ///     On-disk balance item (btrfs_balance_item). Stores balance parameters to disk so that a balance operation can be
    ///     properly resumed after a crash or unmount.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BalanceItem
    {
        /// <summary>BTRFS_BALANCE_* flags (DATA, SYSTEM, METADATA, FORCE, RESUME).</summary>
        public readonly ulong flags;

        /// <summary>Balance arguments for data block groups.</summary>
        public readonly DiskBalanceArgs data;

        /// <summary>Balance arguments for metadata block groups.</summary>
        public readonly DiskBalanceArgs meta;

        /// <summary>Balance arguments for system block groups.</summary>
        public readonly DiskBalanceArgs sys;

        /// <summary>Reserved for future use. 4 x 8 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly ulong[] unused;
    }

#endregion

#region Nested type: FileExtentItem

    /// <summary>
    ///     On-disk file extent item (btrfs_file_extent_item). Describes a file's data extent. For inline extents, the
    ///     data follows immediately after the compression/encryption/type fields. For regular/prealloc extents, the
    ///     disk_bytenr/disk_num_bytes/offset/num_bytes fields describe the extent location.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileExtentItem
    {
        /// <summary>Transaction id that created this extent.</summary>
        public readonly ulong generation;

        /// <summary>
        ///     Maximum number of bytes to hold this extent in RAM. Upper limit for compressed extents since the exact
        ///     decompressed size of split pieces is unknown.
        /// </summary>
        public readonly ulong ram_bytes;

        /// <summary>Compression type (0=none, 1=zlib, 2=lzo, 3=zstd).</summary>
        public readonly byte compression;

        /// <summary>Encryption type (currently always 0=none).</summary>
        public readonly byte encryption;

        /// <summary>Spare for future encoding types (other_encoding).</summary>
        public readonly ushort other_encoding;

        /// <summary>
        ///     Extent type: BTRFS_FILE_EXTENT_INLINE (0), BTRFS_FILE_EXTENT_REG (1), or BTRFS_FILE_EXTENT_PREALLOC (2).
        /// </summary>
        public readonly byte type;

        /// <summary>Logical byte number of the extent on disk. For inline extents, the data starts at this offset.</summary>
        public readonly ulong disk_bytenr;

        /// <summary>Number of bytes on disk consumed by the extent (including checksum blocks).</summary>
        public readonly ulong disk_num_bytes;

        /// <summary>
        ///     Logical offset into the extent. Allows a file extent to point into the middle of an existing extent on disk.
        /// </summary>
        public readonly ulong offset;

        /// <summary>Logical number of file blocks (uncompressed, without encoding).</summary>
        public readonly ulong num_bytes;
    }

#endregion

#region Nested type: CsumItem

    /// <summary>
    ///     On-disk checksum item (btrfs_csum_item). Stores checksums for data extents. The actual checksum data follows
    ///     this structure; the size depends on the checksum algorithm configured for the filesystem.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CsumItem
    {
        /// <summary>First byte of the checksum data. The rest follows inline.</summary>
        public readonly byte csum;
    }

#endregion

#region Nested type: DevStatsItem

    /// <summary>
    ///     On-disk device statistics item (btrfs_dev_stats_item). Stores I/O error counters for a device. Keyed as
    ///     (BTRFS_DEV_STATS_OBJECTID, BTRFS_DEV_STATS_KEY, 0) in the device tree.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DevStatsItem
    {
        /// <summary>
        ///     Statistics values array. Indices: 0=write_errs, 1=read_errs, 2=flush_errs, 3=corruption_errs,
        ///     4=generation_errs. Currently BTRFS_DEV_STAT_VALUES_MAX = 5.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly ulong[] values;
    }

#endregion

#region Nested type: DevReplaceItem

    /// <summary>
    ///     On-disk device replace item (btrfs_dev_replace_item). Persistently stores the device replace state. Keyed as
    ///     (0, BTRFS_DEV_REPLACE_KEY, 0) in the device tree.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DevReplaceItem
    {
        /// <summary>Device id of the source device being replaced.</summary>
        public readonly ulong src_devid;

        /// <summary>Left cursor position (start of the already-copied region).</summary>
        public readonly ulong cursor_left;

        /// <summary>Right cursor position (end of the already-copied region).</summary>
        public readonly ulong cursor_right;

        /// <summary>
        ///     Continue reading from source device mode:
        ///     BTRFS_DEV_REPLACE_ITEM_CONT_READING_FROM_SRCDEV_MODE_ALWAYS (0) or _AVOID (1).
        /// </summary>
        public readonly ulong cont_reading_from_srcdev_mode;

        /// <summary>
        ///     Replace state: NEVER_STARTED (0), STARTED (1), FINISHED (2), CANCELED (3), SUSPENDED (4).
        /// </summary>
        public readonly ulong replace_state;

        /// <summary>Time when the replace operation was started (seconds since epoch).</summary>
        public readonly ulong time_started;

        /// <summary>Time when the replace operation was stopped (seconds since epoch).</summary>
        public readonly ulong time_stopped;

        /// <summary>Number of write errors encountered during the replace.</summary>
        public readonly ulong num_write_errors;

        /// <summary>Number of uncorrectable read errors encountered during the replace.</summary>
        public readonly ulong num_uncorrectable_read_errors;
    }

#endregion

#region Nested type: BlockGroupItem

    /// <summary>
    ///     On-disk block group item (btrfs_block_group_item). Gives hints into the extent allocation trees about which
    ///     blocks are free. Keyed on (block_group_start, BTRFS_BLOCK_GROUP_ITEM_KEY, block_group_length).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BlockGroupItem
    {
        /// <summary>Number of bytes used in this block group.</summary>
        public readonly ulong used;

        /// <summary>Object id of the chunk that this block group belongs to.</summary>
        public readonly ulong chunk_objectid;

        /// <summary>Flags indicating type (DATA, SYSTEM, METADATA) and RAID profile.</summary>
        public readonly ulong flags;
    }

#endregion

#region Nested type: FreeSpaceInfo

    /// <summary>
    ///     On-disk free space info (btrfs_free_space_info). Stored in the free space tree. Contains accounting information
    ///     for a block group. Keyed on (block_group_start, BTRFS_FREE_SPACE_INFO_KEY, block_group_length).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FreeSpaceInfo
    {
        /// <summary>Number of free space extents in this block group.</summary>
        public readonly uint extent_count;

        /// <summary>Flags: BTRFS_FREE_SPACE_USING_BITMAPS (1 &lt;&lt; 0) if bitmaps are used instead of extents.</summary>
        public readonly uint flags;
    }

#endregion

#region Nested type: QgroupStatusItem

    /// <summary>
    ///     On-disk qgroup status item (btrfs_qgroup_status_item). Records the overall state of quota groups. There is
    ///     only one instance, keyed as (0, BTRFS_QGROUP_STATUS_KEY, 0).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct QgroupStatusItem
    {
        /// <summary>Version of the qgroup on-disk format.</summary>
        public readonly ulong version;

        /// <summary>
        ///     Generation number, updated during every commit. Used to detect inconsistencies if the filesystem was mounted
        ///     by an older kernel unaware of qgroups.
        /// </summary>
        public readonly ulong generation;

        /// <summary>
        ///     Flags: BTRFS_QGROUP_STATUS_FLAG_ON (1 &lt;&lt; 0), _RESCAN (1 &lt;&lt; 1), _INCONSISTENT (1 &lt;&lt; 2),
        ///     _SIMPLE_MODE (1 &lt;&lt; 3).
        /// </summary>
        public readonly ulong flags;

        /// <summary>Logical address recording the progress of a qgroup rescan operation.</summary>
        public readonly ulong rescan;

        /// <summary>
        ///     The generation when quotas were last enabled. Used by simple quotas to avoid decrementing when freeing extents
        ///     written before enable. Only valid when BTRFS_QGROUP_STATUS_FLAG_SIMPLE_MODE is set.
        /// </summary>
        public readonly ulong enable_gen;
    }

#endregion

#region Nested type: QgroupInfoItem

    /// <summary>
    ///     On-disk qgroup info item (btrfs_qgroup_info_item). Records the currently used space of a qgroup. One key per
    ///     qgroup, keyed as (0, BTRFS_QGROUP_INFO_KEY, qgroupid).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct QgroupInfoItem
    {
        /// <summary>Generation when this qgroup info was last updated.</summary>
        public readonly ulong generation;

        /// <summary>Referenced bytes (total bytes of all extents referenced by this qgroup).</summary>
        public readonly ulong rfer;

        /// <summary>Referenced compressed bytes.</summary>
        public readonly ulong rfer_cmpr;

        /// <summary>Exclusive bytes (bytes only referenced by this qgroup).</summary>
        public readonly ulong excl;

        /// <summary>Exclusive compressed bytes.</summary>
        public readonly ulong excl_cmpr;
    }

#endregion

#region Nested type: QgroupLimitItem

    /// <summary>
    ///     On-disk qgroup limit item (btrfs_qgroup_limit_item). Contains the user-configured limits for a qgroup. One key
    ///     per qgroup, keyed as (0, BTRFS_QGROUP_LIMIT_KEY, qgroupid).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct QgroupLimitItem
    {
        /// <summary>Flags indicating which limits are active. Only updated when any of the other values change.</summary>
        public readonly ulong flags;

        /// <summary>Maximum referenced bytes allowed.</summary>
        public readonly ulong max_rfer;

        /// <summary>Maximum exclusive bytes allowed.</summary>
        public readonly ulong max_excl;

        /// <summary>Reserved referenced bytes.</summary>
        public readonly ulong rsv_rfer;

        /// <summary>Reserved exclusive bytes.</summary>
        public readonly ulong rsv_excl;
    }

#endregion

#region Nested type: VerityDescriptorItem

    /// <summary>
    ///     On-disk verity descriptor item (btrfs_verity_descriptor_item). Stored at offset 0 for
    ///     BTRFS_VERITY_DESC_ITEM_KEY. Tracks the size of the fs-verity descriptor and encryption parameters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VerityDescriptorItem
    {
        /// <summary>Size of the verity descriptor in bytes.</summary>
        public readonly ulong size;

        /// <summary>
        ///     Reserved for eventual fscrypt initialization vector storage. 2 x 8 bytes (128 bits total).
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly ulong[] reserved;

        /// <summary>Encryption type (currently unused, reserved for fscrypt support).</summary>
        public readonly byte encryption;
    }

#endregion
}