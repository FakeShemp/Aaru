// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser4 filesystem plugin
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

public sealed partial class Reiser4
{
#region Nested type: Superblock

    /// <summary>Master super block (reiser4_master_sb). Located at offset 64 KiB from the start of the device.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Superblock
    {
        /// <summary>Magic string "ReIsEr4" followed by zeroes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] magic;
        /// <summary>Id of the disk layout plugin.</summary>
        public readonly ushort diskformat;
        /// <summary>Block size in bytes.</summary>
        public readonly ushort blocksize;
        /// <summary>Filesystem unique identifier (UUID).</summary>
        public readonly Guid uuid;
        /// <summary>Filesystem label (16 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] label;
        /// <summary>Location of the diskmap on disk. 0 if not present.</summary>
        public readonly ulong diskmap;
    }

#endregion

#region Nested type: Format40DiskSuperblock

    /// <summary>
    ///     On-disk superblock for disk format 40 (format40_disk_super_block).
    ///     Located one page after the master super block. Total size is 512 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Format40DiskSuperblock
    {
        /// <summary>Total number of blocks in the filesystem.</summary>
        public readonly ulong block_count;
        /// <summary>Number of free blocks.</summary>
        public readonly ulong free_blocks;
        /// <summary>Block number of the filesystem tree root.</summary>
        public readonly ulong root_block;
        /// <summary>Smallest free object id (next oid to allocate).</summary>
        public readonly ulong oid;
        /// <summary>Number of files in the filesystem.</summary>
        public readonly ulong file_count;
        /// <summary>Number of times the super block has been flushed.</summary>
        public readonly ulong flushes;
        /// <summary>Unique filesystem identifier generated at mkfs time.</summary>
        public readonly uint mkfs_id;
        /// <summary>Magic string "ReIsEr40FoRmAt".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] magic;
        /// <summary>Height of the filesystem tree.</summary>
        public readonly ushort tree_height;
        /// <summary>Formatting policy (no longer used).</summary>
        public readonly ushort formatting_policy;
        /// <summary>Format flags (e.g. FORMAT40_LARGE_KEYS).</summary>
        public readonly ulong flags;
        /// <summary>On-disk format version number.</summary>
        public readonly uint version;
        /// <summary>Node plugin id.</summary>
        public readonly uint node_plugin_id;
        /// <summary>Unused space, zero-filled, to pad to 512 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 424)]
        public readonly byte[] not_used;
    }

#endregion

#region Nested type: StatusBlock

    /// <summary>
    ///     On-disk status block (reiser4_status).
    ///     Records filesystem health state. Located at block offset (REISER4_MASTER_OFFSET / PAGE_SIZE) + 5.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct StatusBlock
    {
        /// <summary>Magic string "ReiSeR4StATusBl".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] magic;
        /// <summary>Current filesystem state (REISER4_STATUS_OK, _CORRUPTED, _DAMAGED, _DESTROYED, _IOERROR).</summary>
        public readonly ulong status;
        /// <summary>Additional info (e.g. last sector where an IO error happened).</summary>
        public readonly ulong extended_status;
        /// <summary>Last ten functional call addresses (for post-mortem debugging).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly ulong[] stacktrace;
        /// <summary>Error message text (256 bytes), zero-filled if no error.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public readonly byte[] texterror;
    }

#endregion

#region Nested type: JournalHeader

    /// <summary>On-disk journal header block (journal_header). Located at block (REISER4_MASTER_OFFSET / PAGE_SIZE) + 3.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct JournalHeader
    {
        /// <summary>Block number of the last written (committed) transaction head.</summary>
        public readonly ulong last_committed_tx;
    }

#endregion

#region Nested type: JournalFooter

    /// <summary>On-disk journal footer block (journal_footer). Located at block (REISER4_MASTER_OFFSET / PAGE_SIZE) + 4.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct JournalFooter
    {
        /// <summary>Block number of the last flushed transaction.</summary>
        public readonly ulong last_flushed_tx;
        /// <summary>Free block count at the time of transaction flush.</summary>
        public readonly ulong free_blocks;
        /// <summary>Number of files (used OIDs) at the time of transaction flush.</summary>
        public readonly ulong nr_files;
        /// <summary>Next object id at the time of transaction flush.</summary>
        public readonly ulong next_oid;
    }

#endregion

#region Nested type: TransactionHeader

    /// <summary>
    ///     On-disk transaction header (tx_header).
    ///     The first wander record of a committed transaction has this special format.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct TransactionHeader
    {
        /// <summary>Magic string "TxMagic4".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] magic;
        /// <summary>Transaction id.</summary>
        public readonly ulong id;
        /// <summary>Total number of wander records (including this header) in the transaction.</summary>
        public readonly uint total;
        /// <summary>Padding to align next field to 8-byte boundary; always zero.</summary>
        public readonly uint padding;
        /// <summary>Block number of the previous transaction header.</summary>
        public readonly ulong prev_tx;
        /// <summary>Block number of the next wander record.</summary>
        public readonly ulong next_block;
        /// <summary>Committed version of free blocks counter.</summary>
        public readonly ulong free_blocks;
        /// <summary>Number of files (used OIDs) at commit time.</summary>
        public readonly ulong nr_files;
        /// <summary>Next object id at commit time.</summary>
        public readonly ulong next_oid;
    }

#endregion

#region Nested type: WanderRecordHeader

    /// <summary>
    ///     On-disk wander record header (wander_record_header).
    ///     Each wander record (except the first, which is a TransactionHeader) starts with this header,
    ///     followed by an array of WanderEntry structures.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct WanderRecordHeader
    {
        /// <summary>Magic string "LogMagc4".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] magic;
        /// <summary>Transaction id.</summary>
        public readonly ulong id;
        /// <summary>Total number of wander records in the current transaction.</summary>
        public readonly uint total;
        /// <summary>Serial number of this record within the transaction.</summary>
        public readonly uint serial;
        /// <summary>Block number of the next wander record in the chain.</summary>
        public readonly ulong next_block;
    }

#endregion

#region Nested type: WanderEntry

    /// <summary>
    ///     On-disk wander log entry (wander_entry).
    ///     Maps an original block location to its wandered (journal) copy location.
    ///     An array of these follows each WanderRecordHeader.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct WanderEntry
    {
        /// <summary>Original block location.</summary>
        public readonly ulong original;
        /// <summary>Wandered (journal copy) block location.</summary>
        public readonly ulong wandered;
    }

#endregion

#region Nested type: CommonNodeHeader

    /// <summary>
    ///     Common node header (common_node_header).
    ///     Must be at the very beginning of every node in the tree.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CommonNodeHeader
    {
        /// <summary>Identifier of the node plugin.</summary>
        public readonly ushort plugin_id;
    }

#endregion

#region Nested type: Node40Header

    /// <summary>
    ///     On-disk node header for format 40 (node40_header).
    ///     Located at the beginning of each formatted tree node.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Node40Header
    {
        /// <summary>Common node header containing the node plugin id.</summary>
        public readonly ushort plugin_id;
        /// <summary>Number of items stored in this node.</summary>
        public readonly ushort nr_items;
        /// <summary>Free space in the node, measured in bytes.</summary>
        public readonly ushort free_space;
        /// <summary>Offset to the start of free space within the node.</summary>
        public readonly ushort free_space_start;
        /// <summary>Magic value used to identify formatted nodes.</summary>
        public readonly uint magic;
        /// <summary>Unique mkfs identifier, matches the one in the super block.</summary>
        public readonly uint mkfs_id;
        /// <summary>Flush id (write counter), used by fsck to find the newest version of data.</summary>
        public readonly ulong flush_id;
        /// <summary>Node flags used by fsck and repacker.</summary>
        public readonly ushort flags;
        /// <summary>Tree level: 1 is leaf, 2 is twig, root is numerically largest.</summary>
        public readonly byte level;
        /// <summary>Padding byte.</summary>
        public readonly byte pad;
    }

#endregion

#region Nested type: Node41Header

    /// <summary>
    ///     On-disk node header for format 41 (node41_header).
    ///     Same as Node40Header but with an appended 32-bit checksum.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Node41Header
    {
        /// <summary>Common node header containing the node plugin id.</summary>
        public readonly ushort plugin_id;
        /// <summary>Number of items stored in this node.</summary>
        public readonly ushort nr_items;
        /// <summary>Free space in the node, measured in bytes.</summary>
        public readonly ushort free_space;
        /// <summary>Offset to the start of free space within the node.</summary>
        public readonly ushort free_space_start;
        /// <summary>Magic value used to identify formatted nodes.</summary>
        public readonly uint magic;
        /// <summary>Unique mkfs identifier, matches the one in the super block.</summary>
        public readonly uint mkfs_id;
        /// <summary>Flush id (write counter), used by fsck to find the newest version of data.</summary>
        public readonly ulong flush_id;
        /// <summary>Node flags used by fsck and repacker.</summary>
        public readonly ushort flags;
        /// <summary>Tree level: 1 is leaf, 2 is twig, root is numerically largest.</summary>
        public readonly byte level;
        /// <summary>Padding byte.</summary>
        public readonly byte pad;
        /// <summary>32-bit checksum of the node contents.</summary>
        public readonly uint checksum;
    }

#endregion

#region Nested type: Key

    /// <summary>
    ///     On-disk key without large-key support (reiser4_key, REISER4_LARGE_KEY=0).
    ///     3 little-endian 64-bit elements, 24 bytes total.
    ///     Fields encoded within elements: locality (60 bits), type (4 bits), band (4 bits),
    ///     objectid (60 bits), offset (64 bits).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Key
    {
        /// <summary>Element 0: locality (upper 60 bits) and type/minor locality (lower 4 bits).</summary>
        public readonly ulong el0;
        /// <summary>Element 1: band (upper 4 bits) and objectid (lower 60 bits).</summary>
        public readonly ulong el1;
        /// <summary>Element 2: offset or name hash.</summary>
        public readonly ulong el2;
    }

#endregion

#region Nested type: LargeKey

    /// <summary>
    ///     On-disk key with large-key support (reiser4_key, REISER4_LARGE_KEY=1).
    ///     4 little-endian 64-bit elements, 32 bytes total.
    ///     Adds an ordering element between locality and band/objectid.
    ///     Used when FORMAT40_LARGE_KEYS flag is set in format40_disk_super_block.flags.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LargeKey
    {
        /// <summary>Element 0: locality (upper 60 bits) and type/minor locality (lower 4 bits).</summary>
        public readonly ulong el0;
        /// <summary>Element 1: ordering.</summary>
        public readonly ulong el1;
        /// <summary>Element 2: band (upper 4 bits) and objectid (lower 60 bits).</summary>
        public readonly ulong el2;
        /// <summary>Element 3: offset or name hash.</summary>
        public readonly ulong el3;
    }

#endregion

#region Nested type: ItemHeader40

    /// <summary>
    ///     On-disk item header for node format 40 (item_header40) with standard (non-large) keys.
    ///     Located at the end of each node, growing inward. One per item.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ItemHeader40
    {
        /// <summary>Key of the item (24 bytes, standard key).</summary>
        public readonly Key key;
        /// <summary>Offset from the start of the node to the item body, measured in 8-byte chunks.</summary>
        public readonly ushort offset;
        /// <summary>Item flags.</summary>
        public readonly ushort flags;
        /// <summary>Item plugin id.</summary>
        public readonly ushort plugin_id;
    }

#endregion

#region Nested type: ItemHeader40Large

    /// <summary>
    ///     On-disk item header for node format 40 (item_header40) with large key support.
    ///     Used when FORMAT40_LARGE_KEYS flag is set.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ItemHeader40Large
    {
        /// <summary>Key of the item (32 bytes, large key).</summary>
        public readonly LargeKey key;
        /// <summary>Offset from the start of the node to the item body, measured in 8-byte chunks.</summary>
        public readonly ushort offset;
        /// <summary>Item flags.</summary>
        public readonly ushort flags;
        /// <summary>Item plugin id.</summary>
        public readonly ushort plugin_id;
    }

#endregion

#region Nested type: Extent

    /// <summary>
    ///     On-disk extent (reiser4_extent).
    ///     Describes a contiguous run of blocks belonging to a file.
    ///     start==0 means hole, start==1 means unallocated, start>=2 is allocated block number.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Extent
    {
        /// <summary>Starting block number (0=hole, 1=unallocated, >=2 allocated).</summary>
        public readonly ulong start;
        /// <summary>Number of contiguous blocks in this extent.</summary>
        public readonly ulong width;
    }

#endregion

#region Nested type: InternalItem

    /// <summary>
    ///     On-disk internal item (internal_item_layout).
    ///     Contains a down-link (block pointer) to a child node in the tree.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InternalItem
    {
        /// <summary>Block number of the child node.</summary>
        public readonly ulong pointer;
    }

#endregion

#region Nested type: StatDataBase

    /// <summary>
    ///     Minimal on-disk stat data header (reiser4_stat_data_base).
    ///     Contains the extension bitmask that indicates which stat-data extensions follow.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct StatDataBase
    {
        /// <summary>
        ///     Extension bitmask. Each bit indicates presence of a stat-data extension:
        ///     bit 0 = LIGHT_WEIGHT_STAT, bit 1 = UNIX_STAT, bit 2 = LARGE_TIMES_STAT,
        ///     bit 3 = SYMLINK_STAT, bit 4 = PLUGIN_STAT, bit 5 = FLAGS_STAT,
        ///     bit 6 = CAPABILITIES_STAT, bit 7 = CRYPTO_STAT, bit 8 = HEIR_STAT.
        /// </summary>
        public readonly ushort extmask;
    }

#endregion

#region Nested type: LightWeightStat

    /// <summary>
    ///     On-disk light-weight stat extension (reiser4_light_weight_stat).
    ///     Used for files that have minimal metadata; uid/gid are inherited from parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LightWeightStat
    {
        /// <summary>File mode (permissions and type).</summary>
        public readonly ushort mode;
        /// <summary>Number of hard links.</summary>
        public readonly uint nlink;
        /// <summary>File size in bytes.</summary>
        public readonly ulong size;
    }

#endregion

#region Nested type: UnixStat

    /// <summary>
    ///     On-disk unix stat extension (reiser4_unix_stat).
    ///     Contains the standard POSIX file attributes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct UnixStat
    {
        /// <summary>Owner user id.</summary>
        public readonly uint uid;
        /// <summary>Group id.</summary>
        public readonly uint gid;
        /// <summary>Access time (seconds since epoch, 32-bit).</summary>
        public readonly uint atime;
        /// <summary>Modification time (seconds since epoch, 32-bit).</summary>
        public readonly uint mtime;
        /// <summary>Change time (seconds since epoch, 32-bit).</summary>
        public readonly uint ctime;
        /// <summary>
        ///     For device files: encodes major:minor device numbers.
        ///     For regular files: number of bytes used (disk space consumed).
        /// </summary>
        public readonly ulong rdev_or_bytes;
    }

#endregion

#region Nested type: LargeTimesStat

    /// <summary>
    ///     On-disk large times extension (reiser4_large_times_stat).
    ///     Contains additional 32-bit sub-second time fields for nanosecond resolution.
    ///     Governed by the 32bittimes mount option.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LargeTimesStat
    {
        /// <summary>Sub-second access time.</summary>
        public readonly uint atime;
        /// <summary>Sub-second modification time.</summary>
        public readonly uint mtime;
        /// <summary>Sub-second change time.</summary>
        public readonly uint ctime;
    }

#endregion

#region Nested type: FlagsStat

    /// <summary>
    ///     On-disk flags extension (reiser4_flags_stat).
    ///     Contains persistent inode flags (immutable, append-only, etc.).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FlagsStat
    {
        /// <summary>32-bit inode flags bitmask.</summary>
        public readonly uint flags;
    }

#endregion

#region Nested type: CapabilitiesStat

    /// <summary>On-disk capabilities extension (reiser4_capabilities_stat).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CapabilitiesStat
    {
        /// <summary>Effective capabilities bitmask.</summary>
        public readonly uint effective;
        /// <summary>Permitted capabilities bitmask.</summary>
        public readonly uint permitted;
    }

#endregion

#region Nested type: ClusterStat

    /// <summary>
    ///     On-disk cluster extension (reiser4_cluster_stat).
    ///     Defines the cluster size for cryptcompress objects as PAGE_SIZE &lt;&lt; cluster_shift.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ClusterStat
    {
        /// <summary>Cluster shift value; cluster size = PAGE_SIZE &lt;&lt; cluster_shift.</summary>
        public readonly byte cluster_shift;
    }

#endregion

#region Nested type: CryptoStat

    /// <summary>
    ///     On-disk crypto extension header (reiser4_crypto_stat).
    ///     Contains the size of the secret key and is immediately followed by the key id bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CryptoStat
    {
        /// <summary>Secret key size in bits.</summary>
        public readonly ushort keysize;

        // Followed by variable-length keyid bytes.
    }

#endregion

#region Nested type: PluginSlot

    /// <summary>
    ///     On-disk plugin slot (reiser4_plugin_slot).
    ///     Identifies a non-default plugin associated with a file.
    ///     Followed by any plugin-specific persistent state.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PluginSlot
    {
        /// <summary>Plugin set member id (which slot in the plugin set this overrides).</summary>
        public readonly ushort pset_memb;
        /// <summary>Plugin id.</summary>
        public readonly ushort id;
    }

#endregion

#region Nested type: PluginStatHeader

    /// <summary>
    ///     On-disk plugin stat header (reiser4_plugin_stat).
    ///     Followed by an array of PluginSlot entries.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PluginStatHeader
    {
        /// <summary>Number of additional plugin slots associated with this object.</summary>
        public readonly ushort plugins_no;

        // Followed by plugins_no PluginSlot entries.
    }

#endregion

#region Nested type: ObjKeyId

    /// <summary>
    ///     On-disk object key id (obj_key_id) without large-key support.
    ///     Stores enough information to reconstruct the stat-data key of a file.
    ///     Used in directory entries.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ObjKeyId
    {
        /// <summary>Locality (8 bytes, stored as raw bytes for byte alignment).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] locality;
        /// <summary>Object id (8 bytes, stored as raw bytes for byte alignment).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] objectid;
    }

#endregion

#region Nested type: ObjKeyIdLarge

    /// <summary>
    ///     On-disk object key id (obj_key_id) with large-key support.
    ///     Includes the additional ordering field.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ObjKeyIdLarge
    {
        /// <summary>Locality (8 bytes, stored as raw bytes for byte alignment).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] locality;
        /// <summary>Ordering (8 bytes, stored as raw bytes for byte alignment).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] ordering;
        /// <summary>Object id (8 bytes, stored as raw bytes for byte alignment).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] objectid;
    }

#endregion

#region Nested type: DeId

    /// <summary>
    ///     On-disk directory entry id (de_id) without large-key support.
    ///     Sufficient to uniquely identify a directory entry within a compound directory item.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DeId
    {
        /// <summary>Object id (8 bytes, stored as raw bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] objectid;
        /// <summary>Offset (8 bytes, stored as raw bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] offset;
    }

#endregion

#region Nested type: DeIdLarge

    /// <summary>
    ///     On-disk directory entry id (de_id) with large-key support.
    ///     Includes the additional ordering field.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DeIdLarge
    {
        /// <summary>Ordering (8 bytes, stored as raw bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] ordering;
        /// <summary>Object id (8 bytes, stored as raw bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] objectid;
        /// <summary>Offset (8 bytes, stored as raw bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] offset;
    }

#endregion

#region Nested type: DirectoryEntryFormat

    /// <summary>
    ///     On-disk simple directory entry (directory_entry_format).
    ///     Contains the stat-data key id of the target object, followed by a null-terminated file name.
    ///     This is the header portion only (without large key support); the name follows immediately after.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectoryEntryFormat
    {
        /// <summary>Key id of the target object's stat-data.</summary>
        public readonly ObjKeyId id;

        // Followed by null-terminated file name.
    }

#endregion

#region Nested type: DirectoryEntryFormatLarge

    /// <summary>
    ///     On-disk simple directory entry (directory_entry_format) with large key support.
    ///     Contains the large stat-data key id of the target object, followed by a null-terminated file name.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectoryEntryFormatLarge
    {
        /// <summary>Key id of the target object's stat-data (large key variant).</summary>
        public readonly ObjKeyIdLarge id;

        // Followed by null-terminated file name.
    }

#endregion

#region Nested type: CdeUnitHeader

    /// <summary>
    ///     On-disk compound directory entry unit header (cde_unit_header).
    ///     Each unit in a compound directory item has this header.
    ///     This is the non-large key variant.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CdeUnitHeader
    {
        /// <summary>Hash of the directory entry (de_id without large key, 16 bytes).</summary>
        public readonly DeId hash;
        /// <summary>Offset within the item to the directory entry body.</summary>
        public readonly ushort offset;
    }

#endregion

#region Nested type: CdeUnitHeaderLarge

    /// <summary>
    ///     On-disk compound directory entry unit header (cde_unit_header) with large key support.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CdeUnitHeaderLarge
    {
        /// <summary>Hash of the directory entry (de_id with large key, 24 bytes).</summary>
        public readonly DeIdLarge hash;
        /// <summary>Offset within the item to the directory entry body.</summary>
        public readonly ushort offset;
    }

#endregion

#region Nested type: CdeItemHeader

    /// <summary>
    ///     On-disk compound directory entry item header (cde_item_format).
    ///     Followed by an array of CdeUnitHeader entries.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CdeItemHeader
    {
        /// <summary>Number of directory entries in this compound directory item.</summary>
        public readonly ushort num_of_entries;

        // Followed by num_of_entries CdeUnitHeader (or CdeUnitHeaderLarge) entries.
    }

#endregion

#region Nested type: CtailItemHeader

    /// <summary>
    ///     On-disk compressed tail item header (ctail_item_format).
    ///     Followed by the compressed body bytes.
    ///     A cluster_shift of 0xFF indicates an "unprepped" disk cluster.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CtailItemHeader
    {
        /// <summary>
        ///     Packed cluster shift. Disk cluster size = 1 &lt;&lt; cluster_shift.
        ///     Value 0xFF indicates an unprepped disk cluster.
        /// </summary>
        public readonly byte cluster_shift;

        // Followed by variable-length compressed body.
    }

#endregion

#region Nested type: SafeLink

    /// <summary>
    ///     On-disk safe-link (safelink_t).
    ///     Used to maintain filesystem consistency for operations spanning multiple transactions
    ///     (e.g. unlink of open files, multi-transaction truncate).
    ///     Stored as a blackbox item in the tree.
    ///     This is the non-large key variant (24-byte key).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SafeLink
    {
        /// <summary>Key of the stat-data for the file this safe-link refers to.</summary>
        public readonly Key sdkey;
        /// <summary>Size to which the file should be truncated (for truncate links).</summary>
        public readonly ulong size;
    }

#endregion

#region Nested type: SafeLinkLarge

    /// <summary>
    ///     On-disk safe-link (safelink_t) with large key support.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SafeLinkLarge
    {
        /// <summary>Key of the stat-data for the file this safe-link refers to (large key variant).</summary>
        public readonly LargeKey sdkey;
        /// <summary>Size to which the file should be truncated (for truncate links).</summary>
        public readonly ulong size;
    }

#endregion
}