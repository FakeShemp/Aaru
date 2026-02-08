// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin
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
//     License aint with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

// Commit count

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;
using commitcnt_t = int;

// Disk address
using daddr_t = int;

// Fstore
using fstore_t = int;

// Global File System number
using gfs_t = int;

// Inode number
using ino_t = int;

// Filesystem pack number
using pckno_t = short;

// Timestamp
using time_t = int;

// Link count
using nlink_t = short;

// Disk flags
using dflag_t = short;

// Inode unique identifier
using ino_uniqid_t = int;

// Old short inode number
using s_ino_t = short;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Locus filesystem</summary>
public sealed partial class Locus
{
#region Nested type: FsGeneration

    /// <summary>Filesystem generation structure for replicated filesystems</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FsGeneration
    {
        /// <summary>Low water mark commit count</summary>
        public readonly commitcnt_t fsg_lwm;
        /// <summary>Generation time</summary>
        public readonly time_t fsg_time;
    }

#endregion

#region Nested type: OldSuperblock

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OldSuperblock
    {
        public readonly uint s_magic; /* identifies this as a locus filesystem */
        /* defined as a constant below */
        public readonly gfs_t   s_gfs;   /* global filesystem number */
        public readonly daddr_t s_fsize; /* size in blocks of entire volume */
        /* several ints for replicated filsystems */
        public readonly commitcnt_t s_lwm; /* all prior commits propagated */
        public readonly commitcnt_t s_hwm; /* highest commit propagated */
        /* oldest committed version in the list.
         * llst mod NCMTLST is the offset of commit #llst in the list,
         * which wraps around from there.
         */
        public readonly commitcnt_t s_llst;
        public readonly fstore_t s_fstore; /* filesystem storage bit mask; if the
                   filsys is replicated and this is not a
                   primary or backbone copy, this bit mask
                   determines which files are stored */

        public readonly time_t  s_time;  /* last super block update */
        public readonly daddr_t s_tfree; /* total free blocks*/

        public readonly ino_t   s_isize;   /* size in blocks of i-list */
        public readonly short   s_nfree;   /* number of addresses in s_free */
        public readonly Flags   s_flags;   /* filsys flags, defined below */
        public readonly ino_t   s_tinode;  /* total free inodes */
        public readonly ino_t   s_lasti;   /* start place for circular search */
        public readonly ino_t   s_nbehind; /* est # free inodes before s_lasti */
        public readonly pckno_t s_gfspack; /* global filesystem pack number */
        public readonly short   s_ninode;  /* number of i-nodes in s_inode */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly short[] s_dinfo; /* interleave stuff */

        //#define s_m s_dinfo[0]
        //#define s_skip  s_dinfo[0]      /* AIX defines  */
        //#define s_n s_dinfo[1]
        //#define s_cyl   s_dinfo[1]      /* AIX defines  */
        public readonly byte    s_flock;   /* lock during free list manipulation */
        public readonly byte    s_ilock;   /* lock during i-list manipulation */
        public readonly byte    s_fmod;    /* super block modified flag */
        public readonly Version s_version; /* version of the data format in fs. */
        /*  defined below. */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] s_fsmnt; /* name of this file system */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] s_fpack; /* name of this physical volume */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = OLDNICINOD)]
        public readonly ino_t[] s_inode; /* free i-node list */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = OLDNICFREE)]
        public readonly daddr_t[] su_free; /* free block list for non-replicated filsys */
        public readonly byte s_byteorder;  /* byte order of integers */
    }

#endregion

#region Nested type: Superblock

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Superblock
    {
        public uint s_magic; /* identifies this as a locus filesystem */
        /* defined as a constant below */
        public gfs_t   s_gfs;   /* global filesystem number */
        public daddr_t s_fsize; /* size in blocks of entire volume */
        /* several ints for replicated filesystems */
        public commitcnt_t s_lwm; /* all prior commits propagated */
        public commitcnt_t s_hwm; /* highest commit propagated */
        /* oldest committed version in the list.
         * llst mod NCMTLST is the offset of commit #llst in the list,
         * which wraps around from there.
         */
        public commitcnt_t s_llst;
        public fstore_t s_fstore; /* filesystem storage bit mask; if the
                   filsys is replicated and this is not a
                   primary or backbone copy, this bit mask
                   determines which files are stored */

        public time_t  s_time;  /* last super block update */
        public daddr_t s_tfree; /* total free blocks*/

        public ino_t   s_isize;   /* size in blocks of i-list */
        public short   s_nfree;   /* number of addresses in s_free */
        public Flags   s_flags;   /* filsys flags, defined below */
        public ino_t   s_tinode;  /* total free inodes */
        public ino_t   s_lasti;   /* start place for circular search */
        public ino_t   s_nbehind; /* est # free inodes before s_lasti */
        public pckno_t s_gfspack; /* global filesystem pack number */
        public short   s_ninode;  /* number of i-nodes in s_inode */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public short[] s_dinfo; /* interleave stuff */

        //#define s_m s_dinfo[0]
        //#define s_skip  s_dinfo[0]      /* AIX defines  */
        //#define s_n s_dinfo[1]
        //#define s_cyl   s_dinfo[1]      /* AIX defines  */
        public byte    s_flock;   /* lock during free list manipulation */
        public byte    s_ilock;   /* lock during i-list manipulation */
        public byte    s_fmod;    /* super block modified flag */
        public Version s_version; /* version of the data format in fs. */
        /*  defined below. */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] s_fsmnt; /* name of this file system */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] s_fpack; /* name of this physical volume */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICINOD)]
        public ino_t[] s_inode; /* free i-node list */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE)]
        public daddr_t[] su_free; /* free block list for non-replicated filsys */
        public byte s_byteorder;  /* byte order of integers */
    }

#endregion

#region Nested type: Dinode

    /// <summary>Disk inode structure as it appears on disk</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Dinode
    {
        /// <summary>Mode and type of file</summary>
        public readonly ushort di_mode;
        /// <summary>Number of links to file</summary>
        public readonly nlink_t di_nlink;
        /// <summary>Owner's user id</summary>
        public readonly short di_uid;
        /// <summary>Owner's group id</summary>
        public readonly short di_gid;
        /// <summary>Unique identifier</summary>
        public readonly ino_uniqid_t di_uniqid;
        /// <summary>Filler</summary>
        public readonly short di_filler;
        /// <summary>Disk flags</summary>
        public readonly dflag_t di_dflag;
        /// <summary>Number of bytes in file</summary>
        public readonly int di_size;
        /// <summary>Time last modified</summary>
        public readonly time_t di_mtime;
        /// <summary>Time last accessed</summary>
        public readonly time_t di_atime;
        /// <summary>Time changed</summary>
        public readonly time_t di_ctime;
        /// <summary>GFS commit sequence number</summary>
        public readonly commitcnt_t di_cmtcnt;
        /// <summary>File propagation attributes</summary>
        public readonly fstore_t di_fstore;
        /// <summary>Version number of this copy of the data</summary>
        public readonly int di_version;
        /// <summary>Actual number of blocks used</summary>
        public readonly daddr_t di_blocks;
        /// <summary>Padding for future growth</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public readonly byte[] di_pad;
        /// <summary>Disk block addresses</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NADDR)]
        public readonly daddr_t[] di_addr;
    }

#endregion

#region Nested type: DinodeSmallBlock

    /// <summary>Disk inode structure with small block support</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DinodeSmallBlock
    {
        /// <summary>Mode and type of file</summary>
        public readonly ushort di_mode;
        /// <summary>Number of links to file</summary>
        public readonly nlink_t di_nlink;
        /// <summary>Owner's user id</summary>
        public readonly short di_uid;
        /// <summary>Owner's group id</summary>
        public readonly short di_gid;
        /// <summary>Unique identifier</summary>
        public readonly ino_uniqid_t di_uniqid;
        /// <summary>Filler</summary>
        public readonly short di_filler;
        /// <summary>Disk flags</summary>
        public readonly dflag_t di_dflag;
        /// <summary>Number of bytes in file</summary>
        public readonly int di_size;
        /// <summary>Time last modified</summary>
        public readonly time_t di_mtime;
        /// <summary>Time last accessed</summary>
        public readonly time_t di_atime;
        /// <summary>Time changed</summary>
        public readonly time_t di_ctime;
        /// <summary>GFS commit sequence number</summary>
        public readonly commitcnt_t di_cmtcnt;
        /// <summary>File propagation attributes</summary>
        public readonly fstore_t di_fstore;
        /// <summary>Version number of this copy of the data</summary>
        public readonly int di_version;
        /// <summary>Actual number of blocks used</summary>
        public readonly daddr_t di_blocks;
        /// <summary>Padding for future growth</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 27)]
        public readonly byte[] di_pad;
        /// <summary>Flags for small blocks</summary>
        public readonly byte di_sbflag;
        /// <summary>Disk block addresses</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NADDR)]
        public readonly daddr_t[] di_addr;
        /// <summary>Small block buffer (384 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SMBLKSZ)]
        public readonly byte[] di_sbbuf;
    }

#endregion

#region Nested type: Direct

    /// <summary>Old System V format directory entry</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Direct
    {
        /// <summary>Inode number</summary>
        public readonly s_ino_t d_ino;
        /// <summary>File name (14 bytes max)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DIRSIZ)]
        public readonly byte[] d_name;
    }

#endregion

#region Nested type: Dirent

    /// <summary>POSIX directory entry structure</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Dirent
    {
        /// <summary>Inode number of file</summary>
        public readonly ino_t d_ino;
        /// <summary>Offset to next dir entry or past end of file</summary>
        public readonly ushort d_reclen;
        /// <summary>Length of the name field</summary>
        public readonly ushort d_namlen;
        /// <summary>Null-terminated file name (variable length, max NAME_MAX+1)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAME_MAX + 1)]
        public readonly byte[] d_name;
    }

#endregion

#region Nested type: Fblk

    /// <summary>Free block list structure</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Fblk
    {
        /// <summary>Number of free blocks in list</summary>
        public readonly int df_nfree;
        /// <summary>Array of free block addresses</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NICFREE)]
        public readonly daddr_t[] df_free;
    }

#endregion
}