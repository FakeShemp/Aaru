// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
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

// ReSharper disable NotAccessedField.Local

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

// ReSharper disable InheritdocConsiderUsage

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
/// <summary>Implements detection of the UNIX System V filesystem</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class SysVfs
{
#region Nested type: CoherentSuperBlock

    /// <summary>
    ///     Superblock for COHERENT UNIX filesystem
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapPdpEndian]
    partial struct CoherentSuperBlock
    {
        /// <summary>0x000, index of first data zone</summary>
        public ushort s_isize;
        /// <summary>0x002, total number of zones of this volume</summary>
        public int s_fsize;

        // the start of the free block list:
        /// <summary>0x006, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x008, 64 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = COH_NICFREE)]
        public int[] s_free;

        // the cache of free inodes:
        /// <summary>0x108, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0x10A, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x1D2, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x1D3, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x1D4, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x1D5, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x1D6, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x1DE, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x1E2, total number of free inodes</summary>
        public ushort s_tinode;
        /// <summary>0x1E4, interleave factor</summary>
        public short s_m;
        /// <summary>0x1E6, interleave factor</summary>
        public short s_n;
        /// <summary>0x1E8, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x1EE, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x1F4, zero-filled</summary>
        public int s_unique;
    }

#endregion

#region Nested type: SystemVRelease2SuperBlock

    /// <summary>
    ///     Superblock for System V Release 2 and derivates
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SystemVRelease2SuperBlock
    {
        /// <summary>0x000, size in blocks of i-list</summary>
        public ushort s_isize;
        /// <summary>0x002, total number of zones of this volume</summary>
        public int s_fsize;

        // the start of the free block list:
        /// <summary>0x006, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x008, 50 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE)]
        public int[] s_free;

        // the cache of free inodes:
        /// <summary>0x0D0, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0x0D2, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x19A, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x19B, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x19C, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x19D, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x19E, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x1A2, blocks per cylinder</summary>
        public short s_cylblks;
        /// <summary>0x1A4, blocks per gap</summary>
        public short s_gapblks;
        /// <summary>0x1A6, device information ??</summary>
        public short s_dinfo0;
        /// <summary>0x1A8, device information ??</summary>
        public short s_dinfo1;
        /// <summary>0x1AA, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x1AE, total number of free inodes</summary>
        public ushort s_tinode;
        /// <summary>0x1B0, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x1B6, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x1BC, 56 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public int[] s_fill;
        /// <summary>0x1F4, if s_state == (0x7C269D38 - s_time) then filesystem is clean</summary>
        public int s_state;
        /// <summary>0x1F8, magic</summary>
        public uint s_magic;
        /// <summary>0x1FC, filesystem type (1 = 512 bytes/blk, 2 = 1024 bytes/blk)</summary>
        public FsType s_type;
    }

#endregion

#region Nested type: SystemVRelease4SuperBlock

    /// <summary>
    ///     Superblock for System V Release 4 and derivates
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SystemVRelease4SuperBlock
    {
        /// <summary>0x000, size in blocks of i-list</summary>
        public ushort s_isize;
        /// <summary>0x002, padding</summary>
        public short s_pad0;
        /// <summary>0x004, total number of zones of this volume</summary>
        public int s_fsize;

        // the start of the free block list:
        /// <summary>0x008, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x00A, padding</summary>
        public short s_pad1;
        /// <summary>0x00C, 50 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE)]
        public uint[] s_free;

        // the cache of free inodes:
        /// <summary>0x0D4, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0x0D6, padding</summary>
        public short s_pad2;
        /// <summary>0x0D8, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x1A0, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x1A1, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x1A2, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x1A3, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x1A4, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x1A8, blocks per cylinder</summary>
        public short s_cylblks;
        /// <summary>0x1AA, blocks per gap</summary>
        public short s_gapblks;
        /// <summary>0x1AC, device information ??</summary>
        public short s_dinfo0;
        /// <summary>0x1AE, device information ??</summary>
        public short s_dinfo1;
        /// <summary>0x1B0, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x1B4, total number of free inodes</summary>
        public short s_tinode;
        /// <summary>0x1B6, padding</summary>
        public short s_pad3;
        /// <summary>0x1B8, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x1BE, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x1C4, 48 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public int[] s_fill;
        /// <summary>0x1F4, if s_state == (0x7C269D38 - s_time) then filesystem is clean</summary>
        public int s_state;
        /// <summary>0x1F8, magic</summary>
        public uint s_magic;
        /// <summary>0x1FC, filesystem type (1 = 512 bytes/blk, 2 = 1024 bytes/blk)</summary>
        public FsType s_type;
    }

#endregion

#region Nested type: UNIX7thEditionSuperBlock

    /// <summary>
    ///     Superblock for 512 bytes per block UNIX 7th Edition filesystem
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    [SwapPdpEndian]
    partial struct UNIX7thEditionSuperBlock
    {
        /// <summary>0x000, index of first data zone</summary>
        public ushort s_isize;
        /// <summary>0x002, total number of zones of this volume</summary>
        public uint s_fsize;

        // the start of the free block list:
        /// <summary>0x006, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x008, 50 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE)]
        public int[] s_free;

        // the cache of free inodes:
        /// <summary>0x0D0, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0x0D2, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x19A, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x19B, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x19C, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x19D, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x19E, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x1A2, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x1A6, total number of free inodes</summary>
        public ushort s_tinode;
        /// <summary>0x1A8, interleave factor</summary>
        public short s_m;
        /// <summary>0x1AA, interleave factor</summary>
        public short s_n;
        /// <summary>0x1AC, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x1B2, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x1B8, start place for circular search</summary>
        public ushort s_lasti;
        /// <summary>0x1BE, est # free inodes before s_lasti</summary>
        public ushort s_nbehind;
    }

#endregion

#region Nested type: UNIX7thEditionSuperBlock_CL2

    /// <summary>
    ///     Superblock for 1024 bytes per block UNIX 7th Edition filesystem
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    [SwapPdpEndian]
    partial struct UNIX7thEditionSuperBlock_CL2
    {
        /// <summary>0x000, index of first data zone</summary>
        public ushort s_isize;
        /// <summary>0x002, total number of zones of this volume</summary>
        public uint s_fsize;

        // the start of the free block list:
        /// <summary>0x006, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x008, 178 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE_CL2)]
        public int[] s_free;

        // the cache of free inodes:
        /// <summary>0x2D0, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0x2D2, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x39A, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x39B, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x39C, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x39D, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x39E, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x3A2, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x3A6, total number of free inodes</summary>
        public ushort s_tinode;
        /// <summary>0x3A8, interleave factor</summary>
        public short s_m;
        /// <summary>0x3AA, interleave factor</summary>
        public short s_n;
        /// <summary>0x3AC, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x3B2, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x3B8, start place for circular search</summary>
        public ushort s_lasti;
        /// <summary>0x3BE, est # free inodes before s_lasti</summary>
        public ushort s_nbehind;
    }

#endregion

#region Nested type: UNIX7thEditionSuperBlock_CL4

    /// <summary>
    ///     Superblock for 2048 bytes per block UNIX 7th Edition filesystem
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    [SwapPdpEndian]
    partial struct UNIX7thEditionSuperBlock_CL4
    {
        /// <summary>0x000, index of first data zone</summary>
        public ushort s_isize;
        /// <summary>0x002, total number of zones of this volume</summary>
        public uint s_fsize;

        // the start of the free block list:
        /// <summary>0x006, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x008, 434 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE_CL4)]
        public int[] s_free;

        // the cache of free inodes:
        /// <summary>0x6D0, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0x6D2, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x79A, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x79B, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x79C, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x79D, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x79E, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x7A2, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x7A6, total number of free inodes</summary>
        public ushort s_tinode;
        /// <summary>0x7A8, interleave factor</summary>
        public short s_m;
        /// <summary>0x7AA, interleave factor</summary>
        public short s_n;
        /// <summary>0x7AC, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x7B2, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x7B8, start place for circular search</summary>
        public ushort s_lasti;
        /// <summary>0x7BE, est # free inodes before s_lasti</summary>
        public ushort s_nbehind;
    }

#endregion

#region Nested type: XenixSuperBlock

    /// <summary>
    ///     Superblock for XENIX and UNIX System III
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct XenixSuperBlock
    {
        /// <summary>0x000, size in blocks of i-list</summary>
        public ushort s_isize;
        /// <summary>0x002, total number of zones of this volume</summary>
        public int s_fsize;

        // the start of the free block list:
        /// <summary>0x006, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x008, 100 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XNX_NICFREE)]
        public int[] s_free;

        // the cache of free inodes:
        /// <summary>0x198, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0x19A, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x262, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x263, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x264, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x265, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x266, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x26A, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x26E, total number of free inodes</summary>
        public ushort s_tinode;
        /// <summary>0x270, blocks per cylinder</summary>
        public short s_cylblks;
        /// <summary>0x272, blocks per gap</summary>
        public short s_gapblks;
        /// <summary>0x274, device information ??</summary>
        public short s_dinfo0;
        /// <summary>0x276, device information ??</summary>
        public short s_dinfo1;
        /// <summary>0x278, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x27E, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x284, 0x46 if volume is clean</summary>
        public byte s_clean;
        /// <summary>0x285, 371 bytes, 51 bytes for Xenix 3</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NSBFILL)]
        public byte[] s_fill;
        /// <summary>0x3F8, magic</summary>
        public uint s_magic;
        /// <summary>0x3FC, filesystem type (1 = 512 bytes/blk, 2 = 1024 bytes/blk, 3 = 2048 bytes/blk)</summary>
        public FsType s_type;
    }

#endregion

#region Nested type: Xenix3SuperBlock

    /// <summary>
    ///     Superblock for XENIX 3
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Xenix3SuperBlock
    {
        /// <summary>0x000, size in blocks of i-list</summary>
        public ushort s_isize;
        /// <summary>0x002, total number of zones of this volume</summary>
        public int s_fsize;

        // the start of the free block list:
        /// <summary>0x006, blocks in s_free, &lt;=100</summary>
        public short s_nfree;
        /// <summary>0x008, 50 entries, first free block list chunk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE)]
        public int[] s_free;

        // the cache of free inodes:
        /// <summary>0xD0, number of inodes in s_inode, &lt;= 100</summary>
        public short s_ninode;
        /// <summary>0xD2, 100 entries, some free inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ushort[] s_inode;
        /// <summary>0x19A, free block list manipulation lock</summary>
        public sbyte s_flock;
        /// <summary>0x19B, inode cache manipulation lock</summary>
        public sbyte s_ilock;
        /// <summary>0x19C, superblock modification flag</summary>
        public sbyte s_fmod;
        /// <summary>0x19D, read-only mounted flag</summary>
        public sbyte s_ronly;
        /// <summary>0x19E, time of last superblock update</summary>
        public int s_time;
        /// <summary>0x1A2, total number of free zones</summary>
        public int s_tfree;
        /// <summary>0x1A6, total number of free inodes</summary>
        public short s_tinode;
        /// <summary>0x1A8, blocks per cylinder</summary>
        public short s_cylblks;
        /// <summary>0x1AA, blocks per gap</summary>
        public short s_gapblks;
        /// <summary>0x1AC, device information ??</summary>
        public short s_dinfo0;
        /// <summary>0x1AE, device information ??</summary>
        public short s_dinfo1;
        /// <summary>0x1B0, 6 bytes, volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fname;
        /// <summary>0x1B6, 6 bytes, pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fpack;
        /// <summary>0x1BC, 0x46 if volume is clean</summary>
        public sbyte s_clean;
        /// <summary>0x1BD, 371 bytes, 51 bytes for Xenix 3</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XNX3_NSBFILL)]
        public byte[] s_fill;
        /// <summary>0x1F0, magic</summary>
        public uint s_magic;
        /// <summary>0x1F4, filesystem type (1 = 512 bytes/blk, 2 = 1024 bytes/blk, 3 = 2048 bytes/blk)</summary>
        public FsType s_type;
    }

#endregion

    /// <summary>Directory entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirectoryEntry
    {
        /// <summary>Inode number</summary>
        public ushort d_ino;
        /// <summary>File name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DIRSIZE)]
        public byte[] d_name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    [SwapPdpEndian]
    partial struct Inode
    {
        ///<summary>mode and type of file</summary>
        public ushort di_mode;
        ///<summary>number of links to file</summary>
        public short di_nlink;
        ///<summary>owner's user id</summary>
        public short di_uid;
        ///<summary>owner's group id</summary>
        public short di_gid;
        ///<summary>number of bytes in file</summary>
        public int di_size;
        ///<summary>disk block addresses</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 39)]
        public byte[] di_addr;
        ///<summary>file generation number</summary>
        public sbyte di_gen;
        ///<summary>time last accessed</summary>
        public int di_atime;
        ///<summary>time last modified</summary>
        public int di_mtime;
        ///<summary>time created</summary>
        public int di_ctime;
    }
}