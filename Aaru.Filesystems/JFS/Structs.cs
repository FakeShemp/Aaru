// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : IBM JFS filesystem plugin
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of IBM's Journaled File System</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class JFS
{
#region Nested type: Extent

    /// <summary>Physical extent descriptor (pxd_t)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Extent
    {
        /// <summary>Leftmost 24 bits are extent length, rest 8 bits are most significant for <see cref="addr2" /></summary>
        public readonly uint len_addr;
        public readonly uint addr2;
    }

#endregion

#region Nested type: DataExtent

    /// <summary>Data extent descriptor (dxd_t). 16 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DataExtent
    {
        /// <summary>1: flags (DXD_INDEX, DXD_INLINE, DXD_EXTENT, DXD_FILE, DXD_CORRUPT)</summary>
        public readonly byte flag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] rsrvd;
        /// <summary>4: size in bytes</summary>
        public readonly uint size;
        /// <summary>8: address and length in unit of fsblksize</summary>
        public readonly Extent loc;
    }

#endregion

#region Nested type: Dasd

    /// <summary>DASD limit information - stored in directory inode. 16 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Dasd
    {
        /// <summary>Alert Threshold (in percent)</summary>
        public readonly byte thresh;
        /// <summary>Alert Threshold delta (in percent)</summary>
        public readonly byte delta;
        public readonly byte rsrvd1;
        /// <summary>DASD limit upper 8 bits (in logical blocks)</summary>
        public readonly byte limit_hi;
        /// <summary>DASD limit lower 32 bits (in logical blocks)</summary>
        public readonly uint limit_lo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] rsrvd2;
        /// <summary>DASD usage upper 8 bits (in logical blocks)</summary>
        public readonly byte used_hi;
        /// <summary>DASD usage lower 32 bits (in logical blocks)</summary>
        public readonly uint used_lo;
    }

#endregion

#region Nested type: ExtentAllocationDescriptor

    /// <summary>Extent allocation descriptor (xad_t). 16 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtentAllocationDescriptor
    {
        /// <summary>1: flags</summary>
        public readonly byte flag;
        /// <summary>2: reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] rsvrd;
        /// <summary>1: upper 8 bits of offset in unit of fsblksize</summary>
        public readonly byte off1;
        /// <summary>4: lower 32 bits of offset in unit of fsblksize</summary>
        public readonly uint off2;
        /// <summary>8: length and address in unit of fsblksize</summary>
        public readonly Extent loc;
    }

#endregion

#region Nested type: XTreeHeader

    /// <summary>XTree page/root header. 24 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct XTreeHeader
    {
        /// <summary>8: next sibling</summary>
        public readonly ulong next;
        /// <summary>8: previous sibling</summary>
        public readonly ulong prev;
        /// <summary>1: flags (BT_ROOT, BT_LEAF, BT_INTERNAL, etc.)</summary>
        public readonly byte flag;
        /// <summary>1: reserved</summary>
        public readonly byte rsrvd1;
        /// <summary>2: next index = number of entries</summary>
        public readonly ushort nextindex;
        /// <summary>2: max number of entries</summary>
        public readonly ushort maxentry;
        /// <summary>2: reserved</summary>
        public readonly ushort rsrvd2;
        /// <summary>8: self pxd</summary>
        public readonly Extent self;
    }

#endregion

#region Nested type: DirTableSlot

    /// <summary>Directory table slot for directory traversal during readdir. 8 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirTableSlot
    {
        /// <summary>1: reserved</summary>
        public readonly byte rsrvd;
        /// <summary>1: 0 if free</summary>
        public readonly byte flag;
        /// <summary>1: slot within leaf page of entry</summary>
        public readonly byte slot;
        /// <summary>1: upper 8 bits of leaf page address</summary>
        public readonly byte addr1;
        /// <summary>4: lower 32 bits of leaf page address, or index of next entry when deleted</summary>
        public readonly uint addr2;
    }

#endregion

#region Nested type: DirSlot

    /// <summary>Directory page slot (dtslot). 32 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirSlot
    {
        /// <summary>1: next slot index (-1 = last)</summary>
        public readonly sbyte next;
        /// <summary>1: count of characters in this slot</summary>
        public readonly sbyte cnt;
        /// <summary>30: up to 15 UTF-16LE name characters</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public readonly ushort[] name;
    }

#endregion

#region Nested type: InternalDirEntry

    /// <summary>Internal directory node entry head/only segment (idtentry). 32 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InternalDirEntry
    {
        /// <summary>8: child extent descriptor</summary>
        public readonly Extent xd;
        /// <summary>1: next slot index</summary>
        public readonly sbyte next;
        /// <summary>1: name length</summary>
        public readonly byte namlen;
        /// <summary>22: up to 11 UTF-16LE name characters</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public readonly ushort[] name;
    }

#endregion

#region Nested type: LeafDirEntry

    /// <summary>Leaf directory node entry head/only segment (ldtentry). 32 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LeafDirEntry
    {
        /// <summary>4: inode number</summary>
        public readonly uint inumber;
        /// <summary>1: next slot index</summary>
        public readonly sbyte next;
        /// <summary>1: name length</summary>
        public readonly byte namlen;
        /// <summary>22: up to 11 UTF-16LE name characters</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public readonly ushort[] name;
        /// <summary>4: index into dir_table</summary>
        public readonly uint index;
    }

#endregion

#region Nested type: DirRootHeader

    /// <summary>Directory root page header (in-line in on-disk inode). 32 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirRootHeader
    {
        /// <summary>16: DASD limit/usage info</summary>
        public readonly Dasd DASD;
        /// <summary>1: flags</summary>
        public readonly byte flag;
        /// <summary>1: next free entry in stbl</summary>
        public readonly byte nextindex;
        /// <summary>1: free count</summary>
        public readonly sbyte freecnt;
        /// <summary>1: freelist header</summary>
        public readonly sbyte freelist;
        /// <summary>4: parent inode number</summary>
        public readonly uint idotdot;
        /// <summary>8: sorted entry index table</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly sbyte[] stbl;
    }

#endregion

#region Nested type: DirPageHeader

    /// <summary>Directory regular page header. 32 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirPageHeader
    {
        /// <summary>8: next sibling</summary>
        public readonly ulong next;
        /// <summary>8: previous sibling</summary>
        public readonly ulong prev;
        /// <summary>1: flags</summary>
        public readonly byte flag;
        /// <summary>1: next entry index in stbl</summary>
        public readonly byte nextindex;
        /// <summary>1: free count</summary>
        public readonly sbyte freecnt;
        /// <summary>1: slot index of head of freelist</summary>
        public readonly sbyte freelist;
        /// <summary>1: number of slots in page slot[]</summary>
        public readonly byte maxslot;
        /// <summary>1: slot index of start of stbl</summary>
        public readonly byte stblindex;
        /// <summary>2: reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] rsrvd;
        /// <summary>8: self pxd</summary>
        public readonly Extent self;
    }

#endregion

#region Nested type: Inode

    /// <summary>On-disk inode (dinode). 512 bytes.</summary>
    /// <remarks>
    ///     The inode is divided into a 128-byte base area followed by a 384-byte extension area.
    ///     The extension area contains either directory (dtroot) or file (xtroot) specific data,
    ///     represented here as a raw byte array since C# cannot directly represent C unions for marshaling.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Inode
    {
        /*
         * Base area (128 bytes) - generic/POSIX attributes
         */

        /// <summary>4: stamp to show inode belongs to fileset</summary>
        public readonly uint di_inostamp;
        /// <summary>4: fileset number</summary>
        public readonly uint di_fileset;
        /// <summary>4: inode number, aka file serial number</summary>
        public readonly uint di_number;
        /// <summary>4: inode generation number</summary>
        public readonly uint di_gen;

        /// <summary>8: inode extent descriptor</summary>
        public readonly Extent di_ixpxd;

        /// <summary>8: size</summary>
        public readonly ulong di_size;
        /// <summary>8: number of blocks allocated</summary>
        public readonly ulong di_nblocks;

        /// <summary>4: number of links to the object</summary>
        public readonly uint di_nlink;

        /// <summary>4: user id of owner</summary>
        public readonly uint di_uid;
        /// <summary>4: group id of owner</summary>
        public readonly uint di_gid;

        /// <summary>4: attribute, format and permission</summary>
        public readonly uint di_mode;

        /// <summary>8: time last data accessed</summary>
        public readonly TimeStruct di_atime;
        /// <summary>8: time last status changed</summary>
        public readonly TimeStruct di_ctime;
        /// <summary>8: time last data modified</summary>
        public readonly TimeStruct di_mtime;
        /// <summary>8: time created</summary>
        public readonly TimeStruct di_otime;

        /// <summary>16: ACL descriptor</summary>
        public readonly DataExtent di_acl;

        /// <summary>16: EA descriptor</summary>
        public readonly DataExtent di_ea;

        /// <summary>4: next available dir_table index</summary>
        public readonly uint di_next_index;

        /// <summary>4: type of ACL</summary>
        public readonly uint di_acltype;

        /*
         * Extension area (384 bytes)
         *
         * For directories: contains dir_table_slot[12] (96 bytes) + dtroot (288 bytes)
         * For files: contains 96 bytes (unused or imap gengen) + xtroot or special data (288 bytes)
         *
         * Represented as raw bytes since C# cannot directly marshal C unions.
         * Use appropriate helper methods to interpret this data depending on di_mode.
         */

        /// <summary>384: extension area (union of directory and file specific data)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 384)]
        public readonly byte[] di_u;
    }

#endregion

#region Nested type: InodeAllocationGroup

    /// <summary>Inode allocation group page (IAG). 4096 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InodeAllocationGroup
    {
        /// <summary>8: starting block of AG</summary>
        public readonly ulong agstart;
        /// <summary>4: inode allocation group number</summary>
        public readonly uint iagnum;
        /// <summary>4: AG inode free list forward</summary>
        public readonly uint inofreefwd;
        /// <summary>4: AG inode free list back</summary>
        public readonly uint inofreeback;
        /// <summary>4: AG inode extent free list forward</summary>
        public readonly uint extfreefwd;
        /// <summary>4: AG inode extent free list back</summary>
        public readonly uint extfreeback;
        /// <summary>4: IAG free list</summary>
        public readonly uint iagfree;
        /// <summary>16: summary map of mapwords with free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SMAPSZ)]
        public readonly uint[] inosmap;
        /// <summary>16: summary map of mapwords with free extents</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SMAPSZ)]
        public readonly uint[] extsmap;
        /// <summary>4: number of free inodes</summary>
        public readonly uint nfreeinos;
        /// <summary>4: number of free extents</summary>
        public readonly uint nfreeexts;
        /// <summary>1976: pad to 2048 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1976)]
        public readonly byte[] pad;
        /// <summary>512: working allocation map (1 bit per inode, 0=free, 1=allocated)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EXTSPERIAG)]
        public readonly uint[] wmap;
        /// <summary>512: persistent allocation map</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EXTSPERIAG)]
        public readonly uint[] pmap;
        /// <summary>1024: inode extent addresses</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EXTSPERIAG)]
        public readonly Extent[] inoext;
    }

#endregion

#region Nested type: InodeAllocationGroupControl

    /// <summary>Per AG control information in inode map control page (iagctl_disk). 16 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InodeAllocationGroupControl
    {
        /// <summary>4: free inode list anchor</summary>
        public readonly uint inofree;
        /// <summary>4: free extent list anchor</summary>
        public readonly uint extfree;
        /// <summary>4: number of backed inodes</summary>
        public readonly uint numinos;
        /// <summary>4: number of free inodes</summary>
        public readonly uint numfree;
    }

#endregion

#region Nested type: InodeMapControl

    /// <summary>Per fileset/aggregate inode map control page (dinomap_disk). 4096 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InodeMapControl
    {
        /// <summary>4: free IAG list anchor</summary>
        public readonly uint in_freeiag;
        /// <summary>4: next free IAG number</summary>
        public readonly uint in_nextiag;
        /// <summary>4: number of backed inodes</summary>
        public readonly uint in_numinos;
        /// <summary>4: number of free backed inodes</summary>
        public readonly uint in_numfree;
        /// <summary>4: number of blocks per inode extent</summary>
        public readonly uint in_nbperiext;
        /// <summary>4: log2 of in_nbperiext</summary>
        public readonly uint in_l2nbperiext;
        /// <summary>4: for standalone test driver</summary>
        public readonly uint in_diskblock;
        /// <summary>4: for standalone test driver</summary>
        public readonly uint in_maxag;
        /// <summary>2016: pad to 2048 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2016)]
        public readonly byte[] pad;
        /// <summary>2048: AG control information</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXAG)]
        public readonly InodeAllocationGroupControl[] in_agctl;
    }

#endregion

#region Nested type: DmapTree

    /// <summary>Dmap summary tree (dmaptree). 360 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DmapTree
    {
        /// <summary>4: number of tree leaves</summary>
        public readonly uint nleafs;
        /// <summary>4: log2 number of tree leaves</summary>
        public readonly uint l2nleafs;
        /// <summary>4: index of first tree leaf</summary>
        public readonly uint leafidx;
        /// <summary>4: height of the tree</summary>
        public readonly uint height;
        /// <summary>1: min log2 tree leaf value to combine</summary>
        public readonly sbyte budmin;
        /// <summary>341: tree (TREESIZE = 256+64+16+4+1)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TREESIZE)]
        public readonly sbyte[] stree;
        /// <summary>2: pad to word boundary</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] pad;
    }

#endregion

#region Nested type: Dmap

    /// <summary>Dmap page per 8K blocks bitmap. 4096 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Dmap
    {
        /// <summary>4: number of blocks covered by this dmap</summary>
        public readonly uint nblocks;
        /// <summary>4: number of free blocks in this dmap</summary>
        public readonly uint nfree;
        /// <summary>8: starting block number for this dmap</summary>
        public readonly ulong start;
        /// <summary>360: dmap tree</summary>
        public readonly DmapTree tree;
        /// <summary>1672: pad to 2048 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1672)]
        public readonly byte[] pad;
        /// <summary>1024: bits of the working map</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LPERDMAP)]
        public readonly uint[] wmap;
        /// <summary>1024: bits of the persistent map</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LPERDMAP)]
        public readonly uint[] pmap;
    }

#endregion

#region Nested type: DmapControl

    /// <summary>Disk map control page per level (dmapctl). 4096 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DmapControl
    {
        /// <summary>4: number of tree leaves</summary>
        public readonly uint nleafs;
        /// <summary>4: log2 number of tree leaves</summary>
        public readonly uint l2nleafs;
        /// <summary>4: index of the first tree leaf</summary>
        public readonly uint leafidx;
        /// <summary>4: height of tree</summary>
        public readonly uint height;
        /// <summary>1: minimum log2 tree leaf value</summary>
        public readonly sbyte budmin;
        /// <summary>1365: dmapctl tree (CTLTREESIZE = 1024+256+64+16+4+1)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CTLTREESIZE)]
        public readonly sbyte[] stree;
        /// <summary>2714: pad to 4096 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2714)]
        public readonly byte[] pad;
    }

#endregion

#region Nested type: BlockAllocationMap

    /// <summary>On-disk aggregate disk allocation map descriptor (dbmap_disk). 4096 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BlockAllocationMap
    {
        /// <summary>8: number of blocks in aggregate</summary>
        public readonly ulong dn_mapsize;
        /// <summary>8: number of free blocks in aggregate map</summary>
        public readonly ulong dn_nfree;
        /// <summary>4: number of blocks per page</summary>
        public readonly uint dn_l2nbperpage;
        /// <summary>4: total number of AGs</summary>
        public readonly uint dn_numag;
        /// <summary>4: number of active AGs</summary>
        public readonly uint dn_maxlevel;
        /// <summary>4: max active alloc group number</summary>
        public readonly uint dn_maxag;
        /// <summary>4: preferred alloc group (hint)</summary>
        public readonly uint dn_agpref;
        /// <summary>4: dmapctl level holding the AG</summary>
        public readonly uint dn_aglevel;
        /// <summary>4: height in dmapctl of the AG</summary>
        public readonly uint dn_agheight;
        /// <summary>4: width in dmapctl of the AG</summary>
        public readonly uint dn_agwidth;
        /// <summary>4: start tree index at AG height</summary>
        public readonly uint dn_agstart;
        /// <summary>4: log2 number of blocks per alloc group</summary>
        public readonly uint dn_agl2size;
        /// <summary>1024: per AG free count (8 bytes * 128 AGs)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXAG)]
        public readonly ulong[] dn_agfree;
        /// <summary>8: number of blocks per alloc group</summary>
        public readonly ulong dn_agsize;
        /// <summary>1: max free buddy system</summary>
        public readonly sbyte dn_maxfreebud;
        /// <summary>3007: pad to 4096 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3007)]
        public readonly byte[] pad;
    }

#endregion

#region Nested type: LogSuperBlock

    /// <summary>Log superblock (block 1 of log logical volume). logsuper.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogSuperBlock
    {
        /// <summary>4: log lv identifier (LOGMAGIC = 0x87654321)</summary>
        public readonly uint magic;
        /// <summary>4: version number</summary>
        public readonly uint version;
        /// <summary>4: log open/mount counter</summary>
        public readonly uint serial;
        /// <summary>4: size in number of LOGPSIZE blocks</summary>
        public readonly uint size;
        /// <summary>4: logical block size in bytes</summary>
        public readonly uint bsize;
        /// <summary>4: log2 of bsize</summary>
        public readonly uint l2bsize;
        /// <summary>4: option flags</summary>
        public readonly uint flag;
        /// <summary>4: state (LOGMOUNT=0, LOGREDONE=1, LOGWRAP=2, LOGREADERR=3)</summary>
        public readonly uint state;
        /// <summary>4: address of last log record set by logredo</summary>
        public readonly uint end;
        /// <summary>16: 128-bit journal UUID</summary>
        public readonly Guid uuid;
        /// <summary>16: journal label</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] label;
        /// <summary>2048: active file systems list (128 entries of 16-byte UUIDs)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ACTIVE)]
        public readonly Guid[] active;
    }

#endregion

#region Nested type: LogPageHeader

    /// <summary>Log page header. 8 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogPageHeader
    {
        /// <summary>4: log sequence page number</summary>
        public readonly uint page;
        /// <summary>2: reserved</summary>
        public readonly ushort rsrvd;
        /// <summary>2: end-of-log offset of last record write</summary>
        public readonly ushort eor;
    }

#endregion

#region Nested type: LogPage

    /// <summary>Log logical page. 4096 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogPage
    {
        /// <summary>8: page header</summary>
        public readonly LogPageHeader h;
        /// <summary>4080: log record area ((LOGPSIZE / 4 - 4) * 4 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LOGPSIZE / 4 - 4)]
        public readonly uint[] data;
        /// <summary>8: page trailer (normally same as header)</summary>
        public readonly LogPageHeader t;
    }

#endregion

#region Nested type: LogRecordDescriptor

    /// <summary>Log record descriptor (lrd). 36 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogRecordDescriptor
    {
        /// <summary>4: log transaction identifier</summary>
        public readonly uint logtid;
        /// <summary>4: pointer to previous record of same transaction</summary>
        public readonly uint backchain;
        /// <summary>2: record type</summary>
        public readonly ushort type;
        /// <summary>2: length of data in record (in bytes)</summary>
        public readonly ushort length;
        /// <summary>4: file system lv/aggregate</summary>
        public readonly uint aggregate;
        /// <summary>20: type-dependent area (raw bytes, interpret based on type)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly byte[] log;
    }

#endregion

#region Nested type: LineVectorDescriptor

    /// <summary>Line vector descriptor (lvd). 4 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LineVectorDescriptor
    {
        /// <summary>2: offset</summary>
        public readonly ushort offset;
        /// <summary>2: length</summary>
        public readonly ushort length;
    }

#endregion

#region Nested type: ExtendedAttributeHeader

    /// <summary>Extended attribute header (jfs_ea). Variable length.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedAttributeHeader
    {
        /// <summary>1: flags (unused)</summary>
        public readonly byte flag;
        /// <summary>1: length of name</summary>
        public readonly byte namelen;
        /// <summary>2: length of value</summary>
        public readonly ushort valuelen;

        // Followed by: name (namelen + 1 bytes including null terminator), then value (valuelen bytes)
    }

#endregion

#region Nested type: ExtendedAttributeList

    /// <summary>Extended attribute list header (jfs_ea_list). Variable length.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedAttributeList
    {
        /// <summary>4: overall size of the EA list</summary>
        public readonly uint size;

        // Followed by: variable-length list of ExtendedAttributeHeader entries
    }

#endregion

#region Nested type: SuperBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SuperBlock
    {
        public readonly uint       s_magic;
        public readonly uint       s_version;
        public readonly ulong      s_size;
        public readonly uint       s_bsize;
        public readonly ushort     s_l2bsize;
        public readonly ushort     s_l2bfactor;
        public readonly uint       s_pbsize;
        public readonly ushort     s_l1pbsize;
        public readonly ushort     pad;
        public readonly uint       s_agsize;
        public readonly Flags      s_flags;
        public readonly State      s_state;
        public readonly uint       s_compress;
        public readonly Extent     s_ait2;
        public readonly Extent     s_aim2;
        public readonly uint       s_logdev;
        public readonly uint       s_logserial;
        public readonly Extent     s_logpxd;
        public readonly Extent     s_fsckpxd;
        public readonly TimeStruct s_time;
        public readonly uint       s_fsckloglen;
        public readonly sbyte      s_fscklog;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public readonly byte[] s_fpack;
        public readonly ulong  s_xsize;
        public readonly Extent s_xfsckpxd;
        public readonly Extent s_xlogpxd;
        public readonly Guid   s_uuid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] s_label;
        public readonly Guid s_loguuid;
    }

#endregion

#region Nested type: TimeStruct

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct TimeStruct
    {
        public readonly uint tv_sec;
        public readonly uint tv_nsec;
    }

#endregion
}