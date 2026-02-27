// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
#region Nested type: Superblock

    /// <summary>XFS on-disk superblock (struct xfs_dsb). Located at address 0 of each allocation group.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Superblock
    {
        /// <summary>Magic number == XFS_SB_MAGIC (0x58465342 'XFSB')</summary>
        public uint magicnum;
        /// <summary>Logical block size, bytes</summary>
        public uint blocksize;
        /// <summary>Number of data blocks</summary>
        public ulong dblocks;
        /// <summary>Number of realtime blocks</summary>
        public ulong rblocks;
        /// <summary>Number of realtime extents</summary>
        public ulong rextents;
        /// <summary>User-visible file system unique id</summary>
        public Guid uuid;
        /// <summary>Starting block of log if internal</summary>
        public ulong logstart;
        /// <summary>Root inode number</summary>
        public ulong rootino;
        /// <summary>Bitmap inode for realtime extents</summary>
        public ulong rbmino;
        /// <summary>Summary inode for rt bitmap</summary>
        public ulong rsumino;
        /// <summary>Realtime extent size, blocks</summary>
        public uint rextsize;
        /// <summary>Size of an allocation group, blocks</summary>
        public uint agblocks;
        /// <summary>Number of allocation groups</summary>
        public uint agcount;
        /// <summary>Number of rt bitmap blocks</summary>
        public uint rbmblocks;
        /// <summary>Number of log blocks</summary>
        public uint logblocks;
        /// <summary>Header version == XFS_SB_VERSION</summary>
        public ushort version;
        /// <summary>Volume sector size, bytes</summary>
        public ushort sectsize;
        /// <summary>Inode size, bytes</summary>
        public ushort inodesize;
        /// <summary>Inodes per block</summary>
        public ushort inopblock;
        /// <summary>File system name (up to 12 bytes, no terminating NULL)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] fname;
        /// <summary>log2 of blocksize</summary>
        public byte blocklog;
        /// <summary>log2 of sectsize</summary>
        public byte sectlog;
        /// <summary>log2 of inodesize</summary>
        public byte inodelog;
        /// <summary>log2 of inopblock</summary>
        public byte inopblog;
        /// <summary>log2 of agblocks (rounded up)</summary>
        public byte agblklog;
        /// <summary>log2 of rextents</summary>
        public byte rextslog;
        /// <summary>mkfs is in progress, don't mount</summary>
        public byte inprogress;
        /// <summary>Max % of fs for inode space</summary>
        public byte imax_pct;
        /// <summary>Allocated inodes</summary>
        public ulong icount;
        /// <summary>Free inodes</summary>
        public ulong ifree;
        /// <summary>Free data blocks</summary>
        public ulong fdblocks;
        /// <summary>Free realtime extents</summary>
        public ulong frextents;
        /// <summary>User quota inode</summary>
        public ulong uquotino;
        /// <summary>Group quota inode</summary>
        public ulong gquotino;
        /// <summary>Quota flags</summary>
        public ushort qflags;
        /// <summary>Misc. flags</summary>
        public byte flags;
        /// <summary>Shared version number</summary>
        public byte shared_vn;
        /// <summary>Inode chunk alignment, fsblocks</summary>
        public uint inoalignmt;
        /// <summary>Stripe or raid unit</summary>
        public uint unit;
        /// <summary>Stripe or raid width</summary>
        public uint width;
        /// <summary>log2 of dir block size (fsbs)</summary>
        public byte dirblklog;
        /// <summary>log2 of the log sector size</summary>
        public byte logsectlog;
        /// <summary>Sector size for the log, bytes</summary>
        public ushort logsectsize;
        /// <summary>Stripe unit size for the log</summary>
        public uint logsunit;
        /// <summary>Additional feature bits (sb_features2)</summary>
        public uint features2;
        /// <summary>Bad features2 field (copy of features2 from misaligned era)</summary>
        public uint bad_features2;

        // version 5 superblock fields start here

        /// <summary>Compatible feature mask</summary>
        public uint features_compat;
        /// <summary>Read-only compatible feature mask</summary>
        public uint features_ro_compat;
        /// <summary>Incompatible feature mask</summary>
        public uint features_incompat;
        /// <summary>Log incompatible feature mask</summary>
        public uint features_log_incompat;

        /// <summary>Superblock CRC (little-endian while rest of superblock is big-endian)</summary>
        public uint crc;
        /// <summary>Sparse inode chunk alignment</summary>
        public uint spino_align;
        /// <summary>Project quota inode</summary>
        public ulong pquotino;
        /// <summary>Last write sequence</summary>
        public ulong lsn;
        /// <summary>Metadata file system unique id</summary>
        public Guid meta_uuid;

        /// <summary>Metadata directory tree root inode</summary>
        public ulong metadirino;
        /// <summary>Number of realtime groups</summary>
        public uint rgcount;
        /// <summary>Size of a realtime group in rtx</summary>
        public uint rgextents;
        /// <summary>Rt group number shift (log2)</summary>
        public byte rgblklog;
        /// <summary>Zeroes padding (7 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] pad;
        /// <summary>Start of internal RT section (FSB)</summary>
        public ulong rtstart;
        /// <summary>Reserved (zoned) RT blocks</summary>
        public ulong rtreserved;
    }

#endregion

#region Nested type: AGF

    /// <summary>
    ///     Allocation Group Free space header (struct xfs_agf).
    ///     Located at sector 1 in each AG.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AGF
    {
        /// <summary>Magic number == XFS_AGF_MAGIC (0x58414746 'XAGF')</summary>
        public uint agf_magicnum;
        /// <summary>Header version == XFS_AGF_VERSION</summary>
        public uint agf_versionnum;
        /// <summary>Sequence # starting from 0</summary>
        public uint agf_seqno;
        /// <summary>Size in blocks of this AG</summary>
        public uint agf_length;
        /// <summary>bnobt root block</summary>
        public uint agf_bno_root;
        /// <summary>cntbt root block</summary>
        public uint agf_cnt_root;
        /// <summary>rmapbt root block</summary>
        public uint agf_rmap_root;
        /// <summary>bnobt btree levels</summary>
        public uint agf_bno_level;
        /// <summary>cntbt btree levels</summary>
        public uint agf_cnt_level;
        /// <summary>rmapbt btree levels</summary>
        public uint agf_rmap_level;
        /// <summary>First freelist block's index</summary>
        public uint agf_flfirst;
        /// <summary>Last freelist block's index</summary>
        public uint agf_fllast;
        /// <summary>Count of blocks in freelist</summary>
        public uint agf_flcount;
        /// <summary>Total free blocks</summary>
        public uint agf_freeblks;
        /// <summary>Longest free space</summary>
        public uint agf_longest;
        /// <summary># of blocks held in AGF btrees</summary>
        public uint agf_btreeblks;
        /// <summary>UUID of filesystem</summary>
        public Guid agf_uuid;
        /// <summary>rmapbt blocks used</summary>
        public uint agf_rmap_blocks;
        /// <summary>refcountbt blocks used</summary>
        public uint agf_refcount_blocks;
        /// <summary>refcount tree root block</summary>
        public uint agf_refcount_root;
        /// <summary>refcount btree levels</summary>
        public uint agf_refcount_level;
        /// <summary>Reserved for future logged fields (14 x uint64)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public ulong[] agf_spare64;
        /// <summary>Last write sequence</summary>
        public ulong agf_lsn;
        /// <summary>CRC of agf sector</summary>
        public uint agf_crc;
        /// <summary>Spare padding</summary>
        public uint agf_spare2;
    }

#endregion

#region Nested type: AGI

    /// <summary>
    ///     Allocation Group Inode header (struct xfs_agi).
    ///     Located at sector 2 in each AG.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AGI
    {
        /// <summary>Magic number == XFS_AGI_MAGIC (0x58414749 'XAGI')</summary>
        public uint agi_magicnum;
        /// <summary>Header version == XFS_AGI_VERSION</summary>
        public uint agi_versionnum;
        /// <summary>Sequence # starting from 0</summary>
        public uint agi_seqno;
        /// <summary>Size in blocks of this AG</summary>
        public uint agi_length;
        /// <summary>Count of allocated inodes</summary>
        public uint agi_count;
        /// <summary>Root of inode btree</summary>
        public uint agi_root;
        /// <summary>Levels in inode btree</summary>
        public uint agi_level;
        /// <summary>Number of free inodes</summary>
        public uint agi_freecount;
        /// <summary>New inode just allocated</summary>
        public uint agi_newino;
        /// <summary>Last directory inode chunk</summary>
        public uint agi_dirino;
        /// <summary>Hash table of unlinked but still referenced inodes (64 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public uint[] agi_unlinked;
        /// <summary>UUID of filesystem</summary>
        public Guid agi_uuid;
        /// <summary>CRC of agi sector</summary>
        public uint agi_crc;
        /// <summary>Padding to 32 bits</summary>
        public uint agi_pad32;
        /// <summary>Last write sequence</summary>
        public ulong agi_lsn;
        /// <summary>Root of the free inode btree</summary>
        public uint agi_free_root;
        /// <summary>Levels in free inode btree</summary>
        public uint agi_free_level;
        /// <summary>inobt blocks used</summary>
        public uint agi_iblocks;
        /// <summary>finobt blocks used</summary>
        public uint agi_fblocks;
    }

#endregion

#region Nested type: AGFL

    /// <summary>
    ///     Allocation Group Free List header (struct xfs_agfl).
    ///     Located at sector 3 in each AG. Only present in v5 filesystems.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AGFL
    {
        /// <summary>Magic number == XFS_AGFL_MAGIC (0x5841464c 'XAFL')</summary>
        public uint agfl_magicnum;
        /// <summary>AG sequence number</summary>
        public uint agfl_seqno;
        /// <summary>UUID of filesystem</summary>
        public Guid agfl_uuid;
        /// <summary>Last write sequence</summary>
        public ulong agfl_lsn;
        /// <summary>CRC of agfl sector</summary>
        public uint agfl_crc;
    }

#endregion

#region Nested type: LegacyTimestamp

    /// <summary>Legacy timestamp encoding format (struct xfs_legacy_timestamp).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct LegacyTimestamp
    {
        /// <summary>Timestamp seconds</summary>
        public int t_sec;
        /// <summary>Timestamp nanoseconds</summary>
        public int t_nsec;
    }

#endregion

#region Nested type: Dinode

    /// <summary>
    ///     On-disk inode structure (struct xfs_dinode).
    ///     This is the header / "dinode core"; the inode is expanded to fill a variable size
    ///     with the leftover area split into a data fork and an attribute fork.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dinode
    {
        /// <summary>Inode magic # = XFS_DINODE_MAGIC (0x494e 'IN')</summary>
        public ushort di_magic;
        /// <summary>Mode and type of file</summary>
        public ushort di_mode;
        /// <summary>Inode version</summary>
        public byte di_version;
        /// <summary>Format of di_c data (xfs_dinode_fmt)</summary>
        public byte di_format;
        /// <summary>XFS_METAFILE_* metatype; was di_onlink in v1</summary>
        public ushort di_metatype;
        /// <summary>Owner's user id</summary>
        public uint di_uid;
        /// <summary>Owner's group id</summary>
        public uint di_gid;
        /// <summary>Number of links to file</summary>
        public uint di_nlink;
        /// <summary>Lower part of owner's project id</summary>
        public ushort di_projid_lo;
        /// <summary>Higher part of owner's project id</summary>
        public ushort di_projid_hi;
        /// <summary>
        ///     Padding / flush counter / big extent count depending on version:
        ///     V2: 6 bytes pad + 2 bytes di_flushiter
        ///     V3 without NREXT64: 8 bytes pad
        ///     V3 with NREXT64: 8 bytes di_big_nextents
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] di_v2_pad;
        /// <summary>Flush iteration counter (v1/v2 only; zero for v3)</summary>
        public ushort di_flushiter;
        /// <summary>Time last accessed</summary>
        public long di_atime;
        /// <summary>Time last modified</summary>
        public long di_mtime;
        /// <summary>Time created/inode modified</summary>
        public long di_ctime;
        /// <summary>Number of bytes in file</summary>
        public long di_size;
        /// <summary># of direct &amp; btree blocks used</summary>
        public long di_nblocks;
        /// <summary>Basic/minimum extent size for file</summary>
        public uint di_extsize;
        /// <summary>Number of data fork extents (v2/v3 without NREXT64)</summary>
        public uint di_nextents;
        /// <summary>Number of attr fork extents (v2/v3 without NREXT64)</summary>
        public ushort di_anextents;
        /// <summary>Attr fork offset, &lt;&lt;3 for 64b align</summary>
        public byte di_forkoff;
        /// <summary>Format of attr fork's data</summary>
        public sbyte di_aformat;
        /// <summary>DMIG event mask</summary>
        public uint di_dmevmask;
        /// <summary>DMIG state info</summary>
        public ushort di_dmstate;
        /// <summary>Random flags, XFS_DIFLAG_...</summary>
        public ushort di_flags;
        /// <summary>Generation number</summary>
        public uint di_gen;

        // di_next_unlinked is the only non-core field in the old dinode
        /// <summary>AGI unlinked list pointer</summary>
        public uint di_next_unlinked;

        // start of the extended dinode (v3), writable fields
        /// <summary>CRC of the inode (little-endian)</summary>
        public uint di_crc;
        /// <summary>Number of attribute changes</summary>
        public ulong di_changecount;
        /// <summary>Flush sequence</summary>
        public ulong di_lsn;
        /// <summary>More random flags (XFS_DIFLAG2_...)</summary>
        public ulong di_flags2;
        /// <summary>Basic cow extent size for file / used blocks in RTG for rtrmap inode</summary>
        public uint di_cowextsize;
        /// <summary>Padding for future expansion (12 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] di_pad2;

        // fields only written to during inode creation
        /// <summary>Time created</summary>
        public long di_crtime;
        /// <summary>Inode number</summary>
        public ulong di_ino;
        /// <summary>UUID of the filesystem</summary>
        public Guid di_uuid;
    }

#endregion

#region Nested type: RealtimeSuperblock

    /// <summary>
    ///     Realtime superblock - on disk version (struct xfs_rtsb).
    ///     The first block of the realtime volume contains this superblock.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RealtimeSuperblock
    {
        /// <summary>Magic number == XFS_RTSB_MAGIC (0x46726F67 'Frog')</summary>
        public uint rsb_magicnum;
        /// <summary>Superblock CRC (little-endian)</summary>
        public uint rsb_crc;
        /// <summary>Zero padding</summary>
        public uint rsb_pad;
        /// <summary>File system name (up to 12 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] rsb_fname;
        /// <summary>User-visible file system unique id</summary>
        public Guid rsb_uuid;
        /// <summary>Metadata file system unique id</summary>
        public Guid rsb_meta_uuid;
    }

#endregion

#region Nested type: RtBufBlkInfo

    /// <summary>Realtime bitmap/summary block header (struct xfs_rtbuf_blkinfo).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RtBufBlkInfo
    {
        /// <summary>Validity check on block (XFS_RTBITMAP_MAGIC or XFS_RTSUMMARY_MAGIC)</summary>
        public uint rt_magic;
        /// <summary>CRC of block</summary>
        public uint rt_crc;
        /// <summary>Inode that owns the block</summary>
        public ulong rt_owner;
        /// <summary>First block of the buffer</summary>
        public ulong rt_blkno;
        /// <summary>Sequence number of last write</summary>
        public ulong rt_lsn;
        /// <summary>Filesystem we belong to</summary>
        public Guid rt_uuid;
    }

#endregion

#region Nested type: DiskDquot

    /// <summary>
    ///     On-disk representation of quota information for a user (struct xfs_disk_dquot).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DiskDquot
    {
        /// <summary>Dquot magic = XFS_DQUOT_MAGIC (0x4451 'DQ')</summary>
        public ushort d_magic;
        /// <summary>Dquot version</summary>
        public byte d_version;
        /// <summary>XFS_DQTYPE_USER/PROJ/GROUP</summary>
        public byte d_type;
        /// <summary>User, project, or group id</summary>
        public uint d_id;
        /// <summary>Absolute limit on disk blocks</summary>
        public ulong d_blk_hardlimit;
        /// <summary>Preferred limit on disk blocks</summary>
        public ulong d_blk_softlimit;
        /// <summary>Maximum # allocated inodes</summary>
        public ulong d_ino_hardlimit;
        /// <summary>Preferred inode limit</summary>
        public ulong d_ino_softlimit;
        /// <summary>Disk blocks owned by the user</summary>
        public ulong d_bcount;
        /// <summary>Inodes owned by the user</summary>
        public ulong d_icount;
        /// <summary>Zero if within inode limits; else when we refuse service</summary>
        public uint d_itimer;
        /// <summary>Similar to above; for disk blocks</summary>
        public uint d_btimer;
        /// <summary>Warnings issued wrt num inodes</summary>
        public ushort d_iwarns;
        /// <summary>Warnings issued wrt disk blocks</summary>
        public ushort d_bwarns;
        /// <summary>64 bit alignment padding</summary>
        public uint d_pad0;
        /// <summary>Absolute limit on realtime blocks</summary>
        public ulong d_rtb_hardlimit;
        /// <summary>Preferred limit on RT disk blocks</summary>
        public ulong d_rtb_softlimit;
        /// <summary>Realtime blocks owned</summary>
        public ulong d_rtbcount;
        /// <summary>Similar to above; for RT disk blocks</summary>
        public uint d_rtbtimer;
        /// <summary>Warnings issued wrt RT disk blocks</summary>
        public ushort d_rtbwarns;
        /// <summary>Padding</summary>
        public ushort d_pad;
    }

#endregion

#region Nested type: DquotBlock

    /// <summary>
    ///     On-disk dquot block (struct xfs_dqblk).
    ///     Wraps xfs_disk_dquot with CRC information for v5 filesystems.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DquotBlock
    {
        /// <summary>The disk dquot portion</summary>
        public DiskDquot dd_diskdq;
        /// <summary>Filling for posterity (4 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dd_fill;
        /// <summary>Checksum (v5 only)</summary>
        public uint dd_crc;
        /// <summary>Last modification in log (v5 only)</summary>
        public ulong dd_lsn;
        /// <summary>Location information (v5 only)</summary>
        public Guid dd_uuid;
    }

#endregion

#region Nested type: SymlinkHeader

    /// <summary>Remote symlink block header (struct xfs_dsymlink_hdr). CRC-enabled filesystems only.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct SymlinkHeader
    {
        /// <summary>Magic number == XFS_SYMLINK_MAGIC (0x58534c4d 'XSLM')</summary>
        public uint sl_magic;
        /// <summary>Offset of this block in the symlink data</summary>
        public uint sl_offset;
        /// <summary>Number of bytes in this block</summary>
        public uint sl_bytes;
        /// <summary>CRC of this block</summary>
        public uint sl_crc;
        /// <summary>UUID of the filesystem</summary>
        public Guid sl_uuid;
        /// <summary>Inode that owns this block</summary>
        public ulong sl_owner;
        /// <summary>First block of the buffer</summary>
        public ulong sl_blkno;
        /// <summary>Sequence number of last write</summary>
        public ulong sl_lsn;
    }

#endregion

#region Nested type: AllocRecord

    /// <summary>
    ///     Allocation btree data record/key (struct xfs_alloc_rec).
    ///     Used in both bnobt (sorted by block number) and cntbt (sorted by count) btrees.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AllocRecord
    {
        /// <summary>Starting block number</summary>
        public uint ar_startblock;
        /// <summary>Count of free blocks</summary>
        public uint ar_blockcount;
    }

#endregion

#region Nested type: InodeBtreeRecord

    /// <summary>
    ///     On-disk inode btree record (struct xfs_inobt_rec).
    ///     Used in both inobt and finobt.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct InodeBtreeRecord
    {
        /// <summary>Starting inode number</summary>
        public uint ir_startino;
        /// <summary>
        ///     Hole mask for sparse chunks (upper 16 bits) +
        ///     total inode count (next 8 bits) +
        ///     count of free inodes (lower 8 bits).
        ///     For full format, all 32 bits are freecount.
        /// </summary>
        public uint ir_u;
        /// <summary>Free inode bitmask</summary>
        public ulong ir_free;
    }

#endregion

#region Nested type: InodeBtreeKey

    /// <summary>Inode btree key (struct xfs_inobt_key).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct InodeBtreeKey
    {
        /// <summary>Starting inode number</summary>
        public uint ir_startino;
    }

#endregion

#region Nested type: RmapRecord

    /// <summary>Reverse mapping btree data record (struct xfs_rmap_rec).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RmapRecord
    {
        /// <summary>Extent start block</summary>
        public uint rm_startblock;
        /// <summary>Extent length</summary>
        public uint rm_blockcount;
        /// <summary>Extent owner</summary>
        public ulong rm_owner;
        /// <summary>Offset within the owner (with flags in high bits)</summary>
        public ulong rm_offset;
    }

#endregion

#region Nested type: RmapKey

    /// <summary>Reverse mapping btree key (struct xfs_rmap_key).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RmapKey
    {
        /// <summary>Extent start block</summary>
        public uint rm_startblock;
        /// <summary>Extent owner</summary>
        public ulong rm_owner;
        /// <summary>Offset within the owner</summary>
        public ulong rm_offset;
    }

#endregion

#region Nested type: RefcountRecord

    /// <summary>Reference count btree data record (struct xfs_refcount_rec).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RefcountRecord
    {
        /// <summary>Starting block number (bit 31 is CoW staging flag)</summary>
        public uint rc_startblock;
        /// <summary>Count of blocks</summary>
        public uint rc_blockcount;
        /// <summary>Number of inodes linked here</summary>
        public uint rc_refcount;
    }

#endregion

#region Nested type: RefcountKey

    /// <summary>Reference count btree key (struct xfs_refcount_key).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RefcountKey
    {
        /// <summary>Starting block number</summary>
        public uint rc_startblock;
    }

#endregion

#region Nested type: BmbtRecord

    /// <summary>
    ///     Bmap btree record and extent descriptor (struct xfs_bmbt_rec).
    ///     Packed format:
    ///     l0:63       = extent flag (1 = non-normal)
    ///     l0:9-62     = startoff (file logical block offset)
    ///     l0:0-8,l1:21-63 = startblock (physical)
    ///     l1:0-20     = blockcount
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct BmbtRecord
    {
        /// <summary>First 64-bit word (extent flag + startoff + startblock high bits)</summary>
        public ulong l0;
        /// <summary>Second 64-bit word (startblock low bits + blockcount)</summary>
        public ulong l1;
    }

#endregion

#region Nested type: BmbtKey

    /// <summary>Bmap btree key (struct xfs_bmbt_key).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct BmbtKey
    {
        /// <summary>Starting file offset</summary>
        public ulong br_startoff;
    }

#endregion

#region Nested type: BmdrBlock

    /// <summary>Bmap root header, on-disk form only (struct xfs_bmdr_block).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct BmdrBlock
    {
        /// <summary>0 is a leaf</summary>
        public ushort bb_level;
        /// <summary>Current # of data records</summary>
        public ushort bb_numrecs;
    }

#endregion

#region Nested type: RtrmapRoot

    /// <summary>Realtime rmap root header, on-disk form only (struct xfs_rtrmap_root).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RtrmapRoot
    {
        /// <summary>0 is a leaf</summary>
        public ushort bb_level;
        /// <summary>Current # of data records</summary>
        public ushort bb_numrecs;
    }

#endregion

#region Nested type: RtRefcountRoot

    /// <summary>Realtime refcount root header, on-disk form only (struct xfs_rtrefcount_root).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct RtRefcountRoot
    {
        /// <summary>0 is a leaf</summary>
        public ushort bb_level;
        /// <summary>Current # of data records</summary>
        public ushort bb_numrecs;
    }

#endregion

#region Nested type: BtreeBlockShdr

    /// <summary>Short form btree block header (struct xfs_btree_block_shdr). Used for AG-based btrees.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct BtreeBlockShdr
    {
        /// <summary>Left sibling block number (AG-relative)</summary>
        public uint bb_leftsib;
        /// <summary>Right sibling block number (AG-relative)</summary>
        public uint bb_rightsib;

        // v5 fields below

        /// <summary>First block of the buffer (disk address)</summary>
        public ulong bb_blkno;
        /// <summary>Sequence number of last write</summary>
        public ulong bb_lsn;
        /// <summary>Filesystem UUID</summary>
        public Guid bb_uuid;
        /// <summary>AG number that owns this block</summary>
        public uint bb_owner;
        /// <summary>CRC of block (little-endian)</summary>
        public uint bb_crc;
    }

#endregion

#region Nested type: BtreeBlockLhdr

    /// <summary>Long form btree block header (struct xfs_btree_block_lhdr). Used for inode-based btrees.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct BtreeBlockLhdr
    {
        /// <summary>Left sibling block number (filesystem-relative)</summary>
        public ulong bb_leftsib;
        /// <summary>Right sibling block number (filesystem-relative)</summary>
        public ulong bb_rightsib;

        // v5 fields below

        /// <summary>First block of the buffer (disk address)</summary>
        public ulong bb_blkno;
        /// <summary>Sequence number of last write</summary>
        public ulong bb_lsn;
        /// <summary>Filesystem UUID</summary>
        public Guid bb_uuid;
        /// <summary>Inode that owns this block</summary>
        public ulong bb_owner;
        /// <summary>CRC of block (little-endian)</summary>
        public uint bb_crc;
        /// <summary>Padding for alignment</summary>
        public uint bb_pad;
    }

#endregion

#region Nested type: BtreeBlock

    /// <summary>
    ///     Generic btree block header (struct xfs_btree_block).
    ///     Common header shared by short and long form btree blocks.
    ///     The rest (sibling pointers, CRC info) depends on the btree type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct BtreeBlock
    {
        /// <summary>Magic number for block type</summary>
        public uint bb_magic;
        /// <summary>0 is a leaf</summary>
        public ushort bb_level;
        /// <summary>Current # of data records</summary>
        public ushort bb_numrecs;
    }

#endregion

#region Nested type: AclEntry

    /// <summary>On-disk XFS access control list entry (struct xfs_acl_entry).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AclEntry
    {
        /// <summary>ACL tag (user/group/mask/other)</summary>
        public uint ae_tag;
        /// <summary>User or group id</summary>
        public uint ae_id;
        /// <summary>Permissions</summary>
        public ushort ae_perm;
        /// <summary>Padding to fill the implicit hole</summary>
        public ushort ae_pad;
    }

#endregion

#region Nested type: DaBlkInfo

    /// <summary>
    ///     DA btree block info, common to leaf and non-leaf nodes (struct xfs_da_blkinfo).
    ///     Used in v2 directory/attribute btrees.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DaBlkInfo
    {
        /// <summary>Previous block in list</summary>
        public uint forw;
        /// <summary>Following block in list</summary>
        public uint back;
        /// <summary>Validity check on block (magic)</summary>
        public ushort magic;
        /// <summary>Unused padding</summary>
        public ushort pad;
    }

#endregion

#region Nested type: Da3BlkInfo

    /// <summary>
    ///     DA btree v3 (CRC-enabled) block info (struct xfs_da3_blkinfo).
    ///     Extended version of DaBlkInfo with CRC and identification fields.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Da3BlkInfo
    {
        /// <summary>Previous block in list</summary>
        public uint forw;
        /// <summary>Following block in list</summary>
        public uint back;
        /// <summary>Validity check on block (magic)</summary>
        public ushort magic;
        /// <summary>Unused padding</summary>
        public ushort pad;
        /// <summary>CRC of block</summary>
        public uint crc;
        /// <summary>First block of the buffer</summary>
        public ulong blkno;
        /// <summary>Sequence number of last write</summary>
        public ulong lsn;
        /// <summary>Filesystem we belong to</summary>
        public Guid uuid;
        /// <summary>Inode that owns the block</summary>
        public ulong owner;
    }

#endregion

#region Nested type: DaNodeHeader

    /// <summary>DA btree non-leaf node header (struct xfs_da_node_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DaNodeHeader
    {
        /// <summary>Block type, links, etc.</summary>
        public DaBlkInfo info;
        /// <summary>Count of active entries</summary>
        public ushort count;
        /// <summary>Level above leaves (leaf == 0)</summary>
        public ushort level;
    }

#endregion

#region Nested type: Da3NodeHeader

    /// <summary>DA btree v3 non-leaf node header (struct xfs_da3_node_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Da3NodeHeader
    {
        /// <summary>Block type, links, etc.</summary>
        public Da3BlkInfo info;
        /// <summary>Count of active entries</summary>
        public ushort count;
        /// <summary>Level above leaves (leaf == 0)</summary>
        public ushort level;
        /// <summary>Padding to 64-bit alignment</summary>
        public uint pad32;
    }

#endregion

#region Nested type: DaNodeEntry

    /// <summary>DA btree node entry (struct xfs_da_node_entry).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DaNodeEntry
    {
        /// <summary>Hash value for this descendant</summary>
        public uint hashval;
        /// <summary>Btree block before this key</summary>
        public uint before;
    }

#endregion

#region Nested type: Dir2SfHeader

    /// <summary>Directory shortform header (struct xfs_dir2_sf_hdr). For inline directories.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Dir2SfHeader
    {
        /// <summary>Count of entries</summary>
        public byte count;
        /// <summary>Count of 8-byte inode # entries</summary>
        public byte i8count;
        /// <summary>Parent dir inode number (variable size: 4 or 8 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] parent;
    }

#endregion

#region Nested type: Dir2DataFree

    /// <summary>Directory v2 free area descriptor (struct xfs_dir2_data_free).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir2DataFree
    {
        /// <summary>Start of freespace</summary>
        public ushort offset;
        /// <summary>Length of freespace</summary>
        public ushort length;
    }

#endregion

#region Nested type: Dir2DataHeader

    /// <summary>Directory v2 data block header (struct xfs_dir2_data_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir2DataHeader
    {
        /// <summary>XFS_DIR2_DATA_MAGIC or XFS_DIR2_BLOCK_MAGIC</summary>
        public uint magic;
        /// <summary>Best free counts (3 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Dir2DataFree[] bestfree;
    }

#endregion

#region Nested type: Dir3BlkHeader

    /// <summary>Directory v3 block header (struct xfs_dir3_blk_hdr). CRC-enabled directory blocks.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir3BlkHeader
    {
        /// <summary>Magic number</summary>
        public uint magic;
        /// <summary>CRC of block</summary>
        public uint crc;
        /// <summary>First block of the buffer</summary>
        public ulong blkno;
        /// <summary>Sequence number of last write</summary>
        public ulong lsn;
        /// <summary>Filesystem we belong to</summary>
        public Guid uuid;
        /// <summary>Inode that owns the block</summary>
        public ulong owner;
    }

#endregion

#region Nested type: Dir3DataHeader

    /// <summary>Directory v3 data block header (struct xfs_dir3_data_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir3DataHeader
    {
        /// <summary>Block header</summary>
        public Dir3BlkHeader hdr;
        /// <summary>Best free counts (3 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Dir2DataFree[] best_free;
        /// <summary>Padding to 64 bit alignment</summary>
        public uint pad;
    }

#endregion

#region Nested type: Dir2LeafEntry

    /// <summary>Directory v2 leaf block entry (struct xfs_dir2_leaf_entry).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir2LeafEntry
    {
        /// <summary>Hash value of name</summary>
        public uint hashval;
        /// <summary>Address of data entry</summary>
        public uint address;
    }

#endregion

#region Nested type: Dir2LeafHeader

    /// <summary>Directory v2 leaf block header (struct xfs_dir2_leaf_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir2LeafHeader
    {
        /// <summary>Header for DA routines</summary>
        public DaBlkInfo info;
        /// <summary>Count of entries</summary>
        public ushort count;
        /// <summary>Count of stale entries</summary>
        public ushort stale;
    }

#endregion

#region Nested type: Dir3LeafHeader

    /// <summary>Directory v3 leaf block header (struct xfs_dir3_leaf_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir3LeafHeader
    {
        /// <summary>Header for DA routines</summary>
        public Da3BlkInfo info;
        /// <summary>Count of entries</summary>
        public ushort count;
        /// <summary>Count of stale entries</summary>
        public ushort stale;
        /// <summary>Padding to 64 bit alignment</summary>
        public uint pad;
    }

#endregion

#region Nested type: Dir2LeafTail

    /// <summary>Directory v2 leaf block tail (struct xfs_dir2_leaf_tail).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir2LeafTail
    {
        /// <summary>Count of best-free entries</summary>
        public uint bestcount;
    }

#endregion

#region Nested type: Dir2FreeHeader

    /// <summary>Directory v2 free space block header (struct xfs_dir2_free_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir2FreeHeader
    {
        /// <summary>XFS_DIR2_FREE_MAGIC</summary>
        public uint magic;
        /// <summary>DB of first entry</summary>
        public uint firstdb;
        /// <summary>Count of valid entries</summary>
        public uint nvalid;
        /// <summary>Count of used entries</summary>
        public uint nused;
    }

#endregion

#region Nested type: Dir3FreeHeader

    /// <summary>Directory v3 free space block header (struct xfs_dir3_free_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir3FreeHeader
    {
        /// <summary>Block header</summary>
        public Dir3BlkHeader hdr;
        /// <summary>DB of first entry</summary>
        public uint firstdb;
        /// <summary>Count of valid entries</summary>
        public uint nvalid;
        /// <summary>Count of used entries</summary>
        public uint nused;
        /// <summary>Padding to 64 bit alignment</summary>
        public uint pad;
    }

#endregion

#region Nested type: Dir2BlockTail

    /// <summary>Directory v2 single-block format tail (struct xfs_dir2_block_tail).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Dir2BlockTail
    {
        /// <summary>Count of leaf entries</summary>
        public uint count;
        /// <summary>Count of stale leaf entries</summary>
        public uint stale;
    }

#endregion

#region Nested type: AttrSfHeader

    /// <summary>Attribute shortform header (struct xfs_attr_sf_hdr). For inline attributes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AttrSfHeader
    {
        /// <summary>Total bytes in shortform list</summary>
        public ushort totsize;
        /// <summary>Count of active entries</summary>
        public byte count;
        /// <summary>Padding</summary>
        public byte padding;
    }

#endregion

#region Nested type: AttrLeafMap

    /// <summary>Attribute leaf free space map entry (struct xfs_attr_leaf_map).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AttrLeafMap
    {
        /// <summary>Base of free region</summary>
        public ushort @base;
        /// <summary>Length of free region</summary>
        public ushort size;
    }

#endregion

#region Nested type: AttrLeafHeader

    /// <summary>Attribute leaf block header (struct xfs_attr_leaf_hdr).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AttrLeafHeader
    {
        /// <summary>Block type, links, etc.</summary>
        public DaBlkInfo info;
        /// <summary>Count of active leaf entries</summary>
        public ushort count;
        /// <summary>Num bytes of names/values stored</summary>
        public ushort usedbytes;
        /// <summary>First used byte in name area</summary>
        public ushort firstused;
        /// <summary>!= 0 if blk needs compaction</summary>
        public byte holes;
        /// <summary>Padding</summary>
        public byte pad1;
        /// <summary>N largest free regions (3 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public AttrLeafMap[] freemap;
    }

#endregion

#region Nested type: Attr3LeafHeader

    /// <summary>Attribute leaf block v3 header (struct xfs_attr3_leaf_hdr). CRC-enabled.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Attr3LeafHeader
    {
        /// <summary>Block type, links, etc.</summary>
        public Da3BlkInfo info;
        /// <summary>Count of active leaf entries</summary>
        public ushort count;
        /// <summary>Num bytes of names/values stored</summary>
        public ushort usedbytes;
        /// <summary>First used byte in name area</summary>
        public ushort firstused;
        /// <summary>!= 0 if blk needs compaction</summary>
        public byte holes;
        /// <summary>Padding</summary>
        public byte pad1;
        /// <summary>N largest free regions (3 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public AttrLeafMap[] freemap;
        /// <summary>Padding to 64 bit alignment</summary>
        public uint pad2;
    }

#endregion

#region Nested type: AttrLeafEntry

    /// <summary>Attribute leaf entry, sorted on key (struct xfs_attr_leaf_entry).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AttrLeafEntry
    {
        /// <summary>Hash value of name</summary>
        public uint hashval;
        /// <summary>Index into buffer of name/value</summary>
        public ushort nameidx;
        /// <summary>LOCAL/ROOT/SECURE/INCOMPLETE flag</summary>
        public byte flags;
        /// <summary>Unused pad byte</summary>
        public byte pad2;
    }

#endregion

#region Nested type: Attr3RemoteHeader

    /// <summary>Remote attribute block header (struct xfs_attr3_rmt_hdr). CRC-enabled filesystems only.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct Attr3RemoteHeader
    {
        /// <summary>Magic number == XFS_ATTR3_RMT_MAGIC (0x5841524d 'XARM')</summary>
        public uint rm_magic;
        /// <summary>Offset of this block in the attribute data</summary>
        public uint rm_offset;
        /// <summary>Number of bytes in this block</summary>
        public uint rm_bytes;
        /// <summary>CRC of this block</summary>
        public uint rm_crc;
        /// <summary>UUID of the filesystem</summary>
        public Guid rm_uuid;
        /// <summary>Inode that owns this attribute</summary>
        public ulong rm_owner;
        /// <summary>First block of the buffer</summary>
        public ulong rm_blkno;
        /// <summary>Sequence number of last write</summary>
        public ulong rm_lsn;
    }

#endregion

#region Nested type: ParentRecord

    /// <summary>Parent pointer attribute record (struct xfs_parent_rec). Stored as xattr value.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct ParentRecord
    {
        /// <summary>Parent inode number</summary>
        public ulong p_ino;
        /// <summary>Parent generation</summary>
        public uint p_gen;
    }

#endregion
}