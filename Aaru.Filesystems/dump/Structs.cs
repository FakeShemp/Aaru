// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : dump(8) file system plugin
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies backups created with dump(8) shows information.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements identification of a dump(8) image (virtual filesystem on a file)</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class Dump
{
#region Nested type: DInode

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DInode
    {
        public ushort di_mode;      /*   0: IFMT, permissions; see below. */
        public short  di_nlink;     /*   2: File link count. */
        public int    inumber;      /*   4: Lfs: inode number. */
        public ulong  di_size;      /*   8: File byte count. */
        public int    di_atime;     /*  16: Last access time. */
        public int    di_atimensec; /*  20: Last access time. */
        public int    di_mtime;     /*  24: Last modified time. */
        public int    di_mtimensec; /*  28: Last modified time. */
        public int    di_ctime;     /*  32: Last inode change time. */
        public int    di_ctimensec; /*  36: Last inode change time. */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NDADDR)]
        public int[] di_db; /*  40: Direct disk blocks. */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NIADDR)]
        public int[] di_ib;    /*  88: Indirect disk blocks. */
        public uint di_flags;  /* 100: Status flags (chflags). */
        public uint di_blocks; /* 104: Blocks actually held. */
        public int  di_gen;    /* 108: Generation number. */
        public uint di_uid;    /* 112: File owner. */
        public uint di_gid;    /* 116: File group. */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] di_spare; /* 120: Reserved; currently unused */
    }

#endregion

#region Nested type: s_spcl

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct s_spcl
    {
        public int    c_type;     /* record type (see below) */
        public int    c_date;     /* date of this dump */
        public int    c_ddate;    /* date of previous dump */
        public int    c_volume;   /* dump volume number */
        public int    c_tapea;    /* logical block of this record */
        public uint   c_inumber;  /* number of inode */
        public int    c_magic;    /* magic number (see above) */
        public int    c_checksum; /* record checksum */
        public DInode c_dinode;   /* ownership and mode of inode */
        public int    c_count;    /* number of valid c_addr entries */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TP_NINDIR)]
        public byte[] c_addr; /* 1 => data; 0 => hole in inode */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LBLSIZE)]
        public byte[] c_label; /* dump label */
        public int c_level;    /* level of this dump */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAMELEN)]
        public byte[] c_filesys; /* name of dumpped file system */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAMELEN)]
        public byte[] c_dev; /* name of dumpped device */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NAMELEN)]
        public byte[] c_host;    /* name of dumpped host */
        public int  c_flags;     /* additional information */
        public int  c_firstrec;  /* first record on volume */
        public long c_ndate;     /* date of this dump */
        public long c_nddate;    /* date of previous dump */
        public long c_ntapea;    /* logical block of this record */
        public long c_nfirstrec; /* first record on volume */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public int[] c_spare; /* reserved for future uses */
    }

#endregion

#region Nested type: spcl_aix

    // 32-bit AIX format record
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct spcl_aix
    {
        /// <summary>Record type</summary>
        public int c_type;
        /// <summary>Dump date</summary>
        public int c_date;
        /// <summary>Previous dump date</summary>
        public int c_ddate;
        /// <summary>Dump volume number</summary>
        public int c_volume;
        /// <summary>Logical block of this record</summary>
        public int c_tapea;
        public uint c_inumber;
        public uint c_magic;
        public int  c_checksum;

        // Unneeded for now
        /*
        public bsd_dinode  bsd_c_dinode;
        public int c_count;
        public char c_addr[TP_NINDIR];
        public int xix_flag;
        public dinode xix_dinode;
        */
    }

#endregion

#region Nested type: spcl16

    // Old 16-bit format record
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct spcl16
    {
        /// <summary>Record type</summary>
        public readonly short c_type;
        /// <summary>Dump date</summary>
        public int c_date;
        /// <summary>Previous dump date</summary>
        public int c_ddate;
        /// <summary>Dump volume number</summary>
        public readonly short c_volume;
        /// <summary>Logical block of this record</summary>
        public readonly int c_tapea;
        /// <summary>Inode number</summary>
        public readonly ushort c_inumber;
        /// <summary>Magic number</summary>
        public readonly ushort c_magic;
        /// <summary>Record checksum</summary>
        public readonly int c_checksum;

        // Unneeded for now
        /*
        struct dinode  c_dinode;
        int c_count;
        char c_addr[BSIZE];
        */
    }

#endregion
}