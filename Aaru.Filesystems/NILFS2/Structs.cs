// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

// ReSharper disable UnusedMember.Local

using System;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

public sealed partial class NILFS2
{
#region Nested type: Inode

    /// <summary>Structure of an inode on disk</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Inode
    {
        /// <summary>Blocks count</summary>
        public readonly ulong blocks;
        /// <summary>Size in bytes</summary>
        public readonly ulong size;
        /// <summary>Creation time (seconds)</summary>
        public readonly ulong ctime;
        /// <summary>Modification time (seconds)</summary>
        public readonly ulong mtime;
        /// <summary>Creation time (nanoseconds)</summary>
        public readonly uint ctime_nsec;
        /// <summary>Modification time (nanoseconds)</summary>
        public readonly uint mtime_nsec;
        /// <summary>User id</summary>
        public readonly uint uid;
        /// <summary>Group id</summary>
        public readonly uint gid;
        /// <summary>File mode</summary>
        public readonly ushort mode;
        /// <summary>Links count</summary>
        public readonly ushort links_count;
        /// <summary>File flags</summary>
        public readonly uint flags;
        /// <summary>Block mapping (7 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NILFS2_INODE_BMAP_SIZE)]
        public readonly ulong[] bmap;
        /// <summary>Extended attributes</summary>
        public readonly ulong xattr;
        /// <summary>File generation (for NFS)</summary>
        public readonly uint generation;
        /// <summary>Padding</summary>
        public readonly uint pad;
    }

#endregion

#region Nested type: SuperRoot

    /// <summary>Structure of super root</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SuperRoot
    {
        /// <summary>Check sum</summary>
        public readonly uint sum;
        /// <summary>Byte count of the structure</summary>
        public readonly ushort bytes;
        /// <summary>Flags (reserved)</summary>
        public readonly ushort flags;
        /// <summary>Write time of the last segment not for cleaner operation</summary>
        public readonly ulong nongc_ctime;
        /// <summary>DAT file inode</summary>
        public readonly Inode dat;
        /// <summary>Checkpoint file inode</summary>
        public readonly Inode cpfile;
        /// <summary>Segment usage file inode</summary>
        public readonly Inode sufile;
    }

#endregion

#region Nested type: Superblock

    /// <summary>Structure of super block on disk</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Superblock
    {
        /// <summary>Revision level</summary>
        public readonly uint rev_level;
        /// <summary>Minor revision level</summary>
        public readonly ushort minor_rev_level;
        /// <summary>Magic signature</summary>
        public readonly ushort magic;
        /// <summary>Bytes count of CRC calculation for this structure. s_reserved is excluded.</summary>
        public readonly ushort bytes;
        /// <summary>Flags</summary>
        public readonly ushort flags;
        /// <summary>Seed value of CRC calculation</summary>
        public readonly uint crc_seed;
        /// <summary>Check sum of super block</summary>
        public readonly uint sum;
        /// <summary>Block size represented as: blocksize = 1 &lt;&lt; (s_log_block_size + 10)</summary>
        public readonly uint log_block_size;
        /// <summary>Number of segments in filesystem</summary>
        public readonly ulong nsegments;
        /// <summary>Block device size in bytes</summary>
        public readonly ulong dev_size;
        /// <summary>First segment disk block number</summary>
        public readonly ulong first_data_block;
        /// <summary>Number of blocks per full segment</summary>
        public readonly uint blocks_per_segment;
        /// <summary>Reserved segments percentage</summary>
        public readonly uint r_segments_percentage;
        /// <summary>Last checkpoint number</summary>
        public readonly ulong last_cno;
        /// <summary>Disk block address of partial segment written last</summary>
        public readonly ulong last_pseg;
        /// <summary>Sequence number of segment written last</summary>
        public readonly ulong last_seq;
        /// <summary>Free blocks count</summary>
        public readonly ulong free_blocks_count;
        /// <summary>Creation time (execution time of newfs)</summary>
        public readonly ulong ctime;
        /// <summary>Mount time</summary>
        public readonly ulong mtime;
        /// <summary>Write time</summary>
        public readonly ulong wtime;
        /// <summary>Mount count</summary>
        public readonly ushort mnt_count;
        /// <summary>Maximal mount count</summary>
        public readonly ushort max_mnt_count;
        /// <summary>File system state</summary>
        public readonly State state;
        /// <summary>Behaviour when detecting errors</summary>
        public readonly ushort errors;
        /// <summary>Time of last check</summary>
        public readonly ulong lastcheck;
        /// <summary>Max time between checks</summary>
        public readonly uint checkinterval;
        /// <summary>Creator OS</summary>
        public readonly uint creator_os;
        /// <summary>Default uid for reserved blocks</summary>
        public readonly ushort def_resuid;
        /// <summary>Default gid for reserved blocks</summary>
        public readonly ushort def_resgid;
        /// <summary>First non-reserved inode</summary>
        public readonly uint first_ino;
        /// <summary>Size of an inode</summary>
        public readonly ushort inode_size;
        /// <summary>Size of a DAT entry</summary>
        public readonly ushort dat_entry_size;
        /// <summary>Size of a checkpoint</summary>
        public readonly ushort checkpoint_size;
        /// <summary>Size of a segment usage</summary>
        public readonly ushort segment_usage_size;
        /// <summary>128-bit UUID for volume</summary>
        public readonly Guid uuid;
        /// <summary>Volume name (80 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public readonly byte[] volume_name;
        /// <summary>Commit interval of segment</summary>
        public readonly uint c_interval;
        /// <summary>Threshold of data amount for the segment construction</summary>
        public readonly uint c_block_max;
        /// <summary>Compatible feature set</summary>
        public readonly ulong feature_compat;
        /// <summary>Read-only compatible feature set</summary>
        public readonly ulong feature_compat_ro;
        /// <summary>Incompatible feature set</summary>
        public readonly ulong feature_incompat;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>Structure of a directory entry (same as ext2)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectoryEntry
    {
        /// <summary>Inode number</summary>
        public readonly ulong inode;
        /// <summary>Directory entry length</summary>
        public readonly ushort rec_len;
        /// <summary>Name length</summary>
        public readonly byte name_len;
        /// <summary>Directory entry type (file, dir, etc)</summary>
        public readonly FileType file_type;
        /// <summary>File name (255 bytes max)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NILFS2_NAME_LEN)]
        public readonly byte[] name;
        /// <summary>Padding</summary>
        public readonly byte pad;
    }

#endregion

#region Nested type: FileInfo

    /// <summary>File information structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileInfo
    {
        /// <summary>Inode number</summary>
        public readonly ulong ino;
        /// <summary>Checkpoint number</summary>
        public readonly ulong cno;
        /// <summary>Number of blocks (including intermediate blocks)</summary>
        public readonly uint nblocks;
        /// <summary>Number of file data blocks</summary>
        public readonly uint ndatablk;
    }

#endregion

#region Nested type: BInfoV

    /// <summary>Information on a data block (except DAT)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BInfoV
    {
        /// <summary>Virtual block number</summary>
        public readonly ulong vblocknr;
        /// <summary>Block offset</summary>
        public readonly ulong blkoff;
    }

#endregion

#region Nested type: BInfoDat

    /// <summary>Information on a DAT node block</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BInfoDat
    {
        /// <summary>Block offset</summary>
        public readonly ulong blkoff;
        /// <summary>Level</summary>
        public readonly byte level;
        /// <summary>Padding</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public readonly byte[] pad;
    }

#endregion

#region Nested type: SegmentSummary

    /// <summary>Segment summary header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SegmentSummary
    {
        /// <summary>Checksum of data</summary>
        public readonly uint datasum;
        /// <summary>Checksum of segment summary</summary>
        public readonly uint sumsum;
        /// <summary>Magic number</summary>
        public readonly uint magic;
        /// <summary>Size of this structure in bytes</summary>
        public readonly ushort bytes;
        /// <summary>Flags</summary>
        public readonly SegmentSummaryFlags flags;
        /// <summary>Sequence number</summary>
        public readonly ulong seq;
        /// <summary>Creation timestamp</summary>
        public readonly ulong create;
        /// <summary>Next segment</summary>
        public readonly ulong next;
        /// <summary>Number of blocks</summary>
        public readonly uint nblocks;
        /// <summary>Number of finfo structures</summary>
        public readonly uint nfinfo;
        /// <summary>Total size of segment summary in bytes</summary>
        public readonly uint sumbytes;
        /// <summary>Padding</summary>
        public readonly uint pad;
        /// <summary>Checkpoint number</summary>
        public readonly ulong cno;
    }

#endregion

#region Nested type: BTreeNode

    /// <summary>Header of B-tree node block</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BTreeNode
    {
        /// <summary>Flags</summary>
        public readonly byte flags;
        /// <summary>Level</summary>
        public readonly byte level;
        /// <summary>Number of children</summary>
        public readonly ushort nchildren;
        /// <summary>Padding</summary>
        public readonly uint pad;
    }

#endregion

#region Nested type: DirectNode

    /// <summary>Header of built-in bmap array</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectNode
    {
        /// <summary>Flags</summary>
        public readonly byte flags;
        /// <summary>Padding</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public readonly byte[] pad;
    }

#endregion

#region Nested type: PallocGroupDesc

    /// <summary>Block group descriptor</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PallocGroupDesc
    {
        /// <summary>Number of free entries in block group</summary>
        public readonly uint nfrees;
    }

#endregion

#region Nested type: DatEntry

    /// <summary>Disk address translation entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DatEntry
    {
        /// <summary>Block number</summary>
        public readonly ulong blocknr;
        /// <summary>Start checkpoint number</summary>
        public readonly ulong start;
        /// <summary>End checkpoint number</summary>
        public readonly ulong end;
        /// <summary>Reserved for future use</summary>
        public readonly ulong rsv;
    }

#endregion

#region Nested type: SnapshotList

    /// <summary>Snapshot list</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SnapshotList
    {
        /// <summary>Next checkpoint number on snapshot list</summary>
        public readonly ulong next;
        /// <summary>Previous checkpoint number on snapshot list</summary>
        public readonly ulong prev;
    }

#endregion

#region Nested type: Checkpoint

    /// <summary>Checkpoint structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Checkpoint
    {
        /// <summary>Flags</summary>
        public readonly CheckpointFlags flags;
        /// <summary>Checkpoints count in a block</summary>
        public readonly uint checkpoints_count;
        /// <summary>Snapshot list</summary>
        public readonly SnapshotList snapshot_list;
        /// <summary>Checkpoint number</summary>
        public readonly ulong cno;
        /// <summary>Creation timestamp</summary>
        public readonly ulong create;
        /// <summary>Number of blocks incremented by this checkpoint</summary>
        public readonly ulong nblk_inc;
        /// <summary>Inodes count</summary>
        public readonly ulong inodes_count;
        /// <summary>Blocks count</summary>
        public readonly ulong blocks_count;
        /// <summary>Inode of ifile</summary>
        public readonly Inode ifile_inode;
    }

#endregion

#region Nested type: CpFileHeader

    /// <summary>Checkpoint file header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CpFileHeader
    {
        /// <summary>Number of checkpoints</summary>
        public readonly ulong ncheckpoints;
        /// <summary>Number of snapshots</summary>
        public readonly ulong nsnapshots;
        /// <summary>Snapshot list</summary>
        public readonly SnapshotList snapshot_list;
    }

#endregion

#region Nested type: SegmentUsage

    /// <summary>Segment usage</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SegmentUsage
    {
        /// <summary>Last modified timestamp</summary>
        public readonly ulong lastmod;
        /// <summary>Number of blocks in segment</summary>
        public readonly uint nblocks;
        /// <summary>Flags</summary>
        public readonly SegmentUsageFlags flags;
    }

#endregion

#region Nested type: SuFileHeader

    /// <summary>Segment usage file header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SuFileHeader
    {
        /// <summary>Number of clean segments</summary>
        public readonly ulong ncleansegs;
        /// <summary>Number of dirty segments</summary>
        public readonly ulong ndirtysegs;
        /// <summary>Last allocated segment number</summary>
        public readonly ulong last_alloc;
    }

#endregion
}