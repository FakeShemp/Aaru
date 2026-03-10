// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX FIle System plugin.
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

// Using information from Linux kernel headers
/// <inheritdoc />
/// <summary>Implements detection of BSD Fast File System (FFS, aka UNIX File System)</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class UFSPlugin
{
#region Nested type: Checksum

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct CylinderSummary
    {
        /// <summary>number of directories</summary>
        public int cs_ndir;
        /// <summary>number of free blocks</summary>
        public int cs_nbfree;
        /// <summary>number of free inodes</summary>
        public int cs_nifree;
        /// <summary>number of free frags</summary>
        public int cs_nffree;
    }

#endregion

#region Nested type: csum_total

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct csum_total
    {
        /// <summary>number of directories</summary>
        public long cs_ndir;
        /// <summary>number of free blocks</summary>
        public long cs_nbfree;
        /// <summary>number of free inodes</summary>
        public long cs_nifree;
        /// <summary>number of free frags</summary>
        public long cs_nffree;
        /// <summary>number of free clusters</summary>
        public long cs_numclusters;
        /// <summary>future expansion</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public long[] cs_spare;
    }

#endregion

#region Nested type: SuperBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>linked list of file systems</summary>
        public uint fs_link;
        /// <summary>used for incore super blocks on Sun: uint fs_rolled; // logging only: fs fully rolled</summary>
        public uint fs_rlink;
        /// <summary>addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>cylinder group offset in cylinder</summary>
        public int fs_old_cgoffset;
        /// <summary>used to calc mod fs_ntrak</summary>
        public int fs_old_cgmask;
        /// <summary>last time written</summary>
        public int fs_old_time;
        /// <summary>number of blocks in fs</summary>
        public int fs_old_size;
        /// <summary>number of data blocks in fs</summary>
        public int fs_old_dsize;
        /// <summary>number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>number of frags in a block in fs</summary>
        public int fs_frag;
        /* these are configuration parameters */
        /// <summary>minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>num of ms for optimal next block</summary>
        public int fs_old_rotdelay;
        /// <summary>disk revolutions per second</summary>
        public int fs_old_rps;
        /* these fields can be computed from the others */
        /// <summary>``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>``numfrags'' calc number of frags</summary>
        public int fs_fshift;
        /* these are configuration parameters */
        /// <summary>max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>max number of blks per cyl group</summary>
        public int fs_maxbpg;
        /* these fields can be computed from the others */
        /// <summary>block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>csum block offset</summary>
        public int fs_csmask;
        /// <summary>csum block number</summary>
        public int fs_csshift;
        /// <summary>value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>value of INOPB</summary>
        public uint fs_inopb;
        /// <summary>value of NSPF</summary>
        public int fs_old_nspf;
        /* yet another configuration parameter */
        /// <summary>optimization preference, see below On SVR: int fs_state; // file system state</summary>
        public int fs_optim;
        /// <summary># sectors/track including spares</summary>
        public int fs_old_npsect;
        /// <summary>hardware sector interleave</summary>
        public int fs_old_interleave;
        /// <summary>sector 0 skew, per track On A/UX: int fs_state; // file system state</summary>
        public int fs_old_trackskew;
        /// <summary>unique filesystem id On old: int fs_headswitch; // head switch time, usec</summary>
        public int fs_id_1;
        /// <summary>unique filesystem id On old: int fs_trkseek; // track-to-track seek, usec</summary>
        public int fs_id_2;
        /* sizes determined by number of cylinder groups and their sizes */
        /// <summary>blk addr of cyl grp summary area</summary>
        public int fs_old_csaddr;
        /// <summary>size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>cylinder group size</summary>
        public int fs_cgsize;
        /* these fields are derived from the hardware */
        /// <summary>tracks per cylinder</summary>
        public int fs_old_ntrak;
        /// <summary>sectors per track</summary>
        public int fs_old_nsect;
        /// <summary>sectors per cylinder</summary>
        public int fs_old_spc;
        /* this comes from the disk driver partitioning */
        /// <summary>cylinders in filesystem</summary>
        public int fs_old_ncyl;
        /* these fields can be computed from the others */
        /// <summary>cylinders per group</summary>
        public int fs_old_cpg;
        /// <summary>inodes per group</summary>
        public int fs_ipg;
        /// <summary>blocks per group * fs_frag</summary>
        public int fs_fpg;
        /* this data must be re-computed after crashes */
        /// <summary>cylinder summary information</summary>
        public CylinderSummary fs_old_cstotal;
        /* these fields are cleared at mount time */
        /// <summary>super block modified flag</summary>
        public sbyte fs_fmod;
        /// <summary>filesystem is clean flag</summary>
        public sbyte fs_clean;
        /// <summary>mounted read-only flag</summary>
        public sbyte fs_ronly;
        /// <summary>old FS_ flags</summary>
        public sbyte fs_old_flags;
        /// <summary>name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 468)]
        public byte[] fs_fsmnt;
        /// <summary>volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] fs_volname;
        /// <summary>system-wide uid</summary>
        public ulong fs_swuid;
        /// <summary>due to alignment of fs_swuid</summary>
        public int fs_pad;
        /* these fields retain the current block allocation info */
        /// <summary>last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>padding; was list of fs_cs buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public uint[] fs_ocsp;
        /// <summary>(u) # of contig. allocated dirs</summary>
        public uint fs_contigdirs;
        /// <summary>(u) cg summary info buffer</summary>
        public uint fs_csp;
        /// <summary>(u) max cluster in each cyl group</summary>
        public uint fs_maxcluster;
        /// <summary>(u) used by snapshots to track fs</summary>
        public uint fs_active;
        /// <summary>cyl per cycle in postbl</summary>
        public int fs_old_cpc;
        /// <summary>maximum blocking factor permitted</summary>
        public int fs_maxbsize;
        /// <summary>number of unreferenced inodes</summary>
        public long fs_unrefs;
        /// <summary>size of underlying GEOM provider</summary>
        public long fs_providersize;
        /// <summary>size of area reserved for metadata</summary>
        public long fs_metaspace;
        /// <summary>old rotation block list head</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public long[] fs_sparecon64;
        /// <summary>byte offset of standard superblock</summary>
        public long fs_sblockloc;
        /// <summary>(u) cylinder summary information</summary>
        public csum_total fs_cstotal;
        /// <summary>last time written</summary>
        public long fs_time;
        /// <summary>number of blocks in fs</summary>
        public long fs_size;
        /// <summary>number of data blocks in fs</summary>
        public long fs_dsize;
        /// <summary>blk addr of cyl grp summary area</summary>
        public long fs_csaddr;
        /// <summary>(u) blocks being freed</summary>
        public long fs_pendingblocks;
        /// <summary>(u) inodes being freed</summary>
        public uint fs_pendinginodes;
        /// <summary>list of snapshot inode numbers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public uint[] fs_snapinum;
        /// <summary>expected average file size</summary>
        public uint fs_avgfilesize;
        /// <summary>expected # of files per directory</summary>
        public uint fs_avgfpdir;
        /// <summary>save real cg size to use fs_bsize</summary>
        public int fs_save_cgsize;
        /// <summary>Last mount or fsck time.</summary>
        public long fs_mtime;
        /// <summary>SUJ free list</summary>
        public int fs_sujfree;
        /// <summary>reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 23)]
        public int[] fs_sparecon32;
        /// <summary>see FS_ flags below</summary>
        public int fs_flags;
        /// <summary>size of cluster summary array</summary>
        public int fs_contigsumsize;
        /// <summary>max length of an internal symlink</summary>
        public int fs_maxsymlinklen;
        /// <summary>format of on-disk inodes</summary>
        public int fs_old_inodefmt;
        /// <summary>maximum representable file size</summary>
        public ulong fs_maxfilesize;
        /// <summary>~fs_bmask for use with 64-bit size</summary>
        public long fs_qbmask;
        /// <summary>~fs_fmask for use with 64-bit size</summary>
        public long fs_qfmask;
        /// <summary>validate fs_clean field</summary>
        public int fs_state;
        /// <summary>format of positional layout tables</summary>
        public int fs_old_postblformat;
        /// <summary>number of rotational positions</summary>
        public int fs_old_nrpos;
        /// <summary>(short) rotation block list head</summary>
        public int fs_old_postbloff;
        /// <summary>(uchar_t) blocks for each rotation</summary>
        public int fs_old_rotbloff;
        /// <summary>magic number</summary>
        public uint fs_magic;
        /// <summary>list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_rotbl;
    }

#endregion

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HeadBlock
    {
        /// <summary>0 head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCPG)]
        public short[] fs_postbl;
    }

    /// <summary>
    ///     A/UX superblock
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlockAux
    {
        /// <summary>0x000 linked list of file systems</summary>
        public int fs_link;
        /// <summary>0x004 used for incore super blocks</summary>
        public int fs_rlink;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
        /// <summary>0x080 optimization preference, see below</summary>
        public FsOptim fs_optim;
        /// <summary>0x084 reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fs_sparecon;
        /// <summary>0x08C file system state</summary>
        public int fs_state;
        /// <summary>0x090 file system id</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fs_id;
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 currently unused flag</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN_SHORT)]
        public byte[] fs_fsmnt;
        /// <summary>0x2C8 file system name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] fs_fname;
        /// <summary>0x2CE file system pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] fs_fpack;
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NRPOS)]
        public HeadBlock[] fs_postbl;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_rotbl;
    }

    /// <summary>4.1c BSD</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlockOldBSD
    {
        /// <summary>0x00 linked list of file systems</summary>
        public int fs_link;
        /// <summary>0x04 used for incore super blocks</summary>
        public int fs_rlink;
        /// <summary>0x08 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x0c offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x10 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x14 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x18 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x1c used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x20 last time written</summary>
        public int fs_time;
        /// <summary>0x24 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x28 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x2c number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x30 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x34 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x38 number of frags in a block in fs</summary>
        public int fs_frag;
        /// <summary>0x3c minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x40 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x44 disk revolutions per second</summary>
        public int fs_rps;
        /// <summary>0x48 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x4c ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x50 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x54 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
        /// <summary>0x58 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x5c max number of blks per cyl group</summary>
        public int fs_maxbpg;
        /// <summary>0x60 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x64 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x68 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x6c csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x70 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x74 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x78 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x7c value of NSPF</summary>
        public int fs_nspf;
        /// <summary>0x80 reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public int[] fs_sparecon;
        /// <summary>0x98 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x9c size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0xa0 cylinder group size</summary>
        public int fs_cgsize;
        /// <summary>0xa4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0xa8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0xac sectors per cylinder</summary>
        public int fs_spc;
        /// <summary>0xb0 cylinders in file system</summary>
        public int fs_ncyl;
        /// <summary>0xb4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0xb8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0xbc blocks per group * fs_frag</summary>
        public int fs_fpg;
        /// <summary>0xc0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
        /// <summary>0xd0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0xd1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0xd2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0xd3 currently unused flag</summary>
        public byte fs_flags;
        /// <summary>0xd4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte fs_fsmnt;
        /// <summary>0x2d4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2d8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35c head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NRPOS)]
        public HeadBlock[] fs_postbl;
        /// <summary>0x55c magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_rotbl;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HeadBlockShort
    {
        /// <summary>0 head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public short[] fs_opostbl;
    }

    /// <summary>4.4 BSD, NeXTStep, Rhapsody, Mac OS X</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock44BSD
    {
        /// <summary>0x000 historic file system linked list</summary>
        public int fs_firstfield;
        /// <summary>0x004 used for incore super blocks</summary>
        public int fs_unused_1;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
/* yet another configuration parameter */
        /// <summary>0x080 optimization preference, see below</summary>
        public FsOptim fs_optim;
/* these fields are derived from the hardware */
        /// <summary>0x084 # sectors/track including spares</summary>
        public int fs_npsect;
        /// <summary>0x088 hardware sector interleave</summary>
        public int fs_interleave;
        /// <summary>0x08C sector 0 skew, per track</summary>
        public int fs_trackskew;
        /// <summary>0x090 head switch time, usec</summary>
        public int fs_headswitch;
        /// <summary>0x094 track-to-track seek, usec</summary>
        public int fs_trkseek;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields are derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 currently unused flag</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte[] fs_fsmnt;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 31)]
        public int[] fs_csp;
        /// <summary>0x354 max cluster in each cyl group</summary>
        public int fs_maxcluster;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C old rotation block list head</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HeadBlockShort[] fs_opostbl;
        /// <summary>0x45C reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public int[] fs_sparecon;
        /// <summary>0x524 size of cluster summary array</summary>
        public int fs_contigsumsize;
        /// <summary>0x528 max length of an internal symlink</summary>
        public int fs_maxsymlinklen;
        /// <summary>0x52C format of on-disk inodes</summary>
        public InodeFormat fs_inodefmt;
        /// <summary>0x530 maximum representable file size</summary>
        public ulong fs_maxfilesize;
        /// <summary>0x538 ~fs_bmask for use with 64-bit size</summary>
        public long fs_qbmask;
        /// <summary>0x540 ~fs_fmask for use with 64-bit size</summary>
        public long fs_qfmask;
        /// <summary>0x548 validate fs_clean field</summary>
        public int fs_state;
        /// <summary>0x54C format of positional layout tables</summary>
        public RotationalFormat fs_postblformat;
        /// <summary>0x550 number of rotational positions</summary>
        public int fs_nrpos;
        /// <summary>0x554 (u_int16) rotation block list head</summary>
        public int fs_postbloff;
        /// <summary>0x558 (u_int8) blocks for each rotation</summary>
        public int fs_rotbloff;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_space;
/* actually longer */
    }

    /// <summary>Ultrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperblockUltrix
    {
        /// <summary>0x000 linked list of file systems</summary>
        public int fs_link;
        /// <summary>0x004 used for incore super blocks</summary>
        public int fs_rlink;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
        /// <summary>0x080 optimization preference, see below</summary>
        public FsOptim fs_optim;
        /// <summary>0x084 reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] fs_sparecon;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields should be derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 currently unused flags</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN_SHORT)]
        public byte[] fs_fsmnt;
/* Space for next three fields taken from fs_fsmnt (12 bytes) */
        /// <summary>0x2C8 Default value for fs_cleantimer</summary>
        public byte fs_deftimer;
        /// <summary>0x2C9 Currently unused</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] fs_extra;
        /// <summary>0x2CC Time of last fsck</summary>
        public int fs_lastfsck;
        /// <summary>0x2D0 Unique file system id</summary>
        public uint fs_gennum;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NRPOS)]
        public HeadBlock[] fs_postbl;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_rotbl;
/* actually longer */
    }

    /// <summary>UNIX System V Release 4</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperblockSVR4
    {
        /// <summary>0x000 linked list of file systems</summary>
        public int fs_link;
        /// <summary>0x004 used for incore super blocks</summary>
        public int fs_rlink;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
        /// <summary>0x080 optimization preference, see below</summary>
        public FsOptim fs_optim;
        /// <summary>0x084 file system state</summary>
        public int fs_state;
        /// <summary>0x088 reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fs_sparecon;
/* a unique id for this filesystem (currently unused and unmaintained) */
        /// <summary>0x090 file system id</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fs_id;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields should be derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 currently unused flag</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte[] fs_fsmnt;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NRPOS)]
        public HeadBlock[] fs_postbl;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_rotbl;
/* actually longer */
    }

    /// <summary>386BSD</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Superblock386BSD
    {
        /// <summary>0x000 linked list of file systems</summary>
        public int fs_link;
        /// <summary>0x004 used for incore super blocks</summary>
        public int fs_rlink;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
/* yet another configuration parameter */
        /// <summary>0x080 optimization preference, see below</summary>
        public FsOptim fs_optim;
/* these fields are derived from the hardware */
        /// <summary>0x084 # sectors/track including spares</summary>
        public int fs_npsect;
        /// <summary>0x088 hardware sector interleave</summary>
        public int fs_interleave;
        /// <summary>0x08C sector 0 skew, per track</summary>
        public int fs_trackskew;
        /// <summary>0x090 head switch time, usec</summary>
        public int fs_headswitch;
        /// <summary>0x094 track-to-track seek, usec</summary>
        public int fs_trkseek;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields are derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 currently unused flag</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte[] fs_fsmnt;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C old rotation block list head</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HeadBlockShort[] fs_opostbl;
        /// <summary>0x45C reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 55)]
        public int[] fs_sparecon;
        /// <summary>0x538 validate fs_clean field</summary>
        public int fs_state;
        /// <summary>0x53C ~fs_bmask - for use with quad size</summary>
        public long fs_qbmask;
        /// <summary>0x544 ~fs_fmask - for use with quad size</summary>
        public long fs_qfmask;
        /// <summary>0x54C format of positional layout tables</summary>
        public int fs_postblformat;
        /// <summary>0x550 number of rotational positions</summary>
        public int fs_nrpos;
        /// <summary>0x554 (short) rotation block list head</summary>
        public int fs_postbloff;
        /// <summary>0x558 (u_char) blocks for each rotation</summary>
        public int fs_rotbloff;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_space;
/* actually longer */
    }

    /// <summary>RISC/os</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperblockRISCos
    {
        /// <summary>0x000 linked list of file systems</summary>
        public int fs_link;
        /// <summary>0x004 used for incore super blocks</summary>
        public int fs_rlink;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
        /// <summary>0x080 optimization preference, see below</summary>
        public FsOptim fs_optim;
        /// <summary>0x084 reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public int[] fs_sparecon;
/* a unique id for this filesystem (currently unused and unmaintained) */
        /// <summary>0x090 file system id</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fs_id;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields should be derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 currently unused flag</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte[] fs_fsmnt;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NRPOS)]
        public HeadBlock[] fs_postbl;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_rotbl;
/* actually longer */
    }

    /// <summary>Sun Solaris</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperblockSun
    {
        /// <summary>0x000 linked list of file systems</summary>
        public uint fs_link;
        /// <summary>0x004 logging only: fs fully rolled</summary>
        public uint fs_rolled;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
/* yet another configuration parameter */
        /// <summary>0x080 optimization preference, see below</summary>
        public int fs_optim;
/* these fields are derived from the hardware */
        /// <summary>0x084 # sectors/track including spares</summary>
        public int fs_npsect;
        /// <summary>0x088 summary info state - lufs only</summary>
        public int fs_si;
        /// <summary>0x08C sector 0 skew, per track</summary>
        public int fs_trackskew;
/* a unique id for this filesystem (currently unused and unmaintained) */
/* In 4.3 Tahoe this space is used by fs_headswitch and fs_trkseek */
/* Neither of those fields is used in the Tahoe code right now but */
/* there could be problems if they are.                            */
        /// <summary>0x090 file system id</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fs_id;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields are derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system state flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 largefiles flag, etc.</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte[] fs_fsmnt;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /*
         * The following used to be fs_csp[MAXCSBUFS]. It was not
         * used anywhere except in old utilities.  We removed this
         * in 5.6 and expect fs_u.fs_csp to be used instead.
         * We no longer limit fs_cssize based on MAXCSBUFS.
         */
        /// <summary>0x2D8 fs_cs (csum) info</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public uint[] fs_csp_pad;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C old rotation block list head</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HeadBlockShort[] fs_opostbl;
        /// <summary>0x45C reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 51)]
        public int[] fs_sparecon;
        /// <summary>0x528 minor version of ufs</summary>
        public int fs_version;
        /// <summary>0x52C block # of embedded log</summary>
        public int fs_logbno;
        /// <summary>0x530 reclaim open, deleted files</summary>
        public int fs_reclaim;
        /// <summary>0x534 reserved for future constant</summary>
        public int fs_sparecon2;
        /// <summary>0x538 file system state time stamp</summary>
        public int fs_state;
        /// <summary>0x53C ~fs_bmask - for use with quad size</summary>
        public long fs_qbmask;
        /// <summary>0x544 ~fs_fmask - for use with quad size</summary>
        public long fs_qfmask;
        /// <summary>0x54C format of positional layout tables</summary>
        public int fs_postblformat;
        /// <summary>0x550 number of rotational positions</summary>
        public int fs_nrpos;
        /// <summary>0x554 (short) rotation block list head</summary>
        public int fs_postbloff;
        /// <summary>0x558 (uchar_t) blocks for each rotation</summary>
        public int fs_rotbloff;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_space;
/* actually longer */
    }

    /// <summary>OSF/1</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperblockOSF1
    {
        /// <summary>0x000 list pointers unused on BSD systems</summary>
        public int fs_link;
        /// <summary>0x004 list pointers unused on BSD systems</summary>
        public int fs_rlink;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
/* yet another configuration parameter */
        /// <summary>0x080 optimization preference, see below</summary>
        public int fs_optim;
/* these fields are derived from the hardware */
        /// <summary>0x084 # sectors/track including spares</summary>
        public int fs_npsect;
        /// <summary>0x088 hardware sector interleave</summary>
        public int fs_interleave;
        /// <summary>0x08C sector 0 skew, per track</summary>
        public int fs_trackskew;
        /// <summary>0x090 head switch time, usec</summary>
        public int fs_headswitch;
        /// <summary>0x094 track-to-track seek, usec</summary>
        public int fs_trkseek;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public int fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields are derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 fs_clean save area</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte[] fs_fsmnt;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 Unused in alpha</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_blank;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C old rotation block list head</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HeadBlockShort[] fs_opostbl;
        /// <summary>0x45C reserved for future constants</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 56)]
        public int[] fsu_sparecon;
        /// <summary>0x53C ~fs_bmask, for use with quad size</summary>
        public long fs_qbmask;
        /// <summary>0x544 ~fs_fmask, for use with quad size</summary>
        public long fs_qfmask;
        /// <summary>0x54C format of positional layout tables</summary>
        public int fs_postblformat;
        /// <summary>0x550 number of rotational positions</summary>
        public int fs_nrpos;
        /// <summary>0x554 (short) rotation block list head</summary>
        public int fs_postbloff;
        /// <summary>0x558 (u_char) blocks for each rotation</summary>
        public int fs_rotbloff;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_space;
        /// <summary>0x561 used for rotation, etc. info</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ALPHA_PAD)]
        public byte[] fs_xxx;
        /// <summary>list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
/* actually longer */
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct mirinfo
    {
        public uint mirstate; /* mirror states for root and swap */
        public int  mirtime;  /* mirror time stamp */
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperblockHPUX
    {
        /// <summary>0x000 linked list of file systems</summary>
        public int fs_link;
        /// <summary>0x004 used for incore super blocks</summary>
        public int fs_rlink;
        /// <summary>0x008 addr of super-block in filesys</summary>
        public int fs_sblkno;
        /// <summary>0x00C offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>0x010 offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>0x014 offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>0x018 cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>0x01C used to calc mod fs_ntrak</summary>
        public int fs_cgmask;
        /// <summary>0x020 last time written</summary>
        public int fs_time;
        /// <summary>0x024 number of blocks in fs</summary>
        public int fs_size;
        /// <summary>0x028 number of data blocks in fs</summary>
        public int fs_dsize;
        /// <summary>0x02C number of cylinder groups</summary>
        public int fs_ncg;
        /// <summary>0x030 size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>0x034 size of frag blocks in fs</summary>
        public int fs_fsize;
        /// <summary>0x038 number of frags in a block in fs</summary>
        public int fs_frag;
/* these are configuration parameters */
        /// <summary>0x03C minimum percentage of free blocks</summary>
        public int fs_minfree;
        /// <summary>0x040 num of ms for optimal next block</summary>
        public int fs_rotdelay;
        /// <summary>0x044 disk revolutions per second</summary>
        public int fs_rps;
/* these fields can be computed from the others */
        /// <summary>0x048 ``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>0x04C ``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>0x050 ``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>0x054 ``numfrags'' calc number of frags</summary>
        public int fs_fshift;
/* these are configuration parameters */
        /// <summary>0x058 max number of contiguous blks</summary>
        public int fs_maxcontig;
        /// <summary>0x05C max number of blks per cyl group</summary>
        public int fs_maxbpg;
/* these fields can be computed from the others */
        /// <summary>0x060 block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>0x064 fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>0x068 actual size of super block</summary>
        public int fs_sbsize;
        /// <summary>0x06C csum block offset</summary>
        public int fs_csmask;
        /// <summary>0x070 csum block number</summary>
        public int fs_csshift;
        /// <summary>0x074 value of NINDIR</summary>
        public int fs_nindir;
        /// <summary>0x078 value of INOPB</summary>
        public int fs_inopb;
        /// <summary>0x07C value of NSPF</summary>
        public int fs_nspf;
        /// <summary>0x080 file system id</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fs_id;
        /// <summary>0x088 mirror states of root/swap</summary>
        public mirinfo fs_mirror;
        /// <summary>0x090 feature bit flags</summary>
        public int fs_featurebits;
        /// <summary>0x094 optimization preference - see below</summary>
        public int fs_optim;
/* sizes determined by number of cylinder groups and their sizes */
        /// <summary>0x098 blk addr of cyl grp summary area</summary>
        public byte fs_csaddr;
        /// <summary>0x09C size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>0x0A0 cylinder group size</summary>
        public int fs_cgsize;
/* these fields should be derived from the hardware */
        /// <summary>0x0A4 tracks per cylinder</summary>
        public int fs_ntrak;
        /// <summary>0x0A8 sectors per track</summary>
        public int fs_nsect;
        /// <summary>0x0AC sectors per cylinder</summary>
        public int fs_spc;
/* this comes from the disk driver partitioning */
        /// <summary>0x0B0 cylinders in file system</summary>
        public int fs_ncyl;
/* these fields can be computed from the others */
        /// <summary>0x0B4 cylinders per group</summary>
        public int fs_cpg;
        /// <summary>0x0B8 inodes per group</summary>
        public int fs_ipg;
        /// <summary>0x0BC blocks per group * fs_frag</summary>
        public int fs_fpg;
/* this data must be re-computed after crashes */
        /// <summary>0x0C0 cylinder summary information</summary>
        public CylinderSummary fs_cstotal;
/* these fields are cleared at mount time */
        /// <summary>0x0D0 super block modified flag</summary>
        public byte fs_fmod;
        /// <summary>0x0D1 file system is clean flag</summary>
        public byte fs_clean;
        /// <summary>0x0D2 mounted read-only flag</summary>
        public byte fs_ronly;
        /// <summary>0x0D3 currently unused flag</summary>
        public byte fs_flags;
        /// <summary>0x0D4 name mounted on</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXMNTLEN)]
        public byte[] fs_fsmnt;
/* these fields retain the current block allocation info */
        /// <summary>0x2D4 last cg searched</summary>
        public int fs_cgrotor;
        /// <summary>0x2D8 list of fs_cs info buffers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXCSBUFS)]
        public int[] fs_csp;
        /// <summary>0x358 cyl per cycle in postbl</summary>
        public int fs_cpc;
        /// <summary>0x35C head of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NRPOS)]
        public HeadBlock[] fs_postbl;
        /// <summary>0x55C magic number</summary>
        public int fs_magic;
        /// <summary>0x560 file system name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] fs_fname;
        /// <summary>0x566 file system pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] fs_fpack;
        /// <summary>0x56C list of blocks for each rotation</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] fs_rotbl;
/* actually longer */
    }
}