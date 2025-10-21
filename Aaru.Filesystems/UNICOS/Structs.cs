// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNICOS filesystem plugin.
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

// UNICOS is ILP64 so let's think everything is 64-bit

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;
using blkno_t = long;
using daddr_t = long;
using dev_t = long;
using extent_t = long;
using ino_t = long;
using time_t = long;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection for the Cray UNICOS filesystem</summary>
public sealed partial class UNICOS
{
#region Nested type: nc1fdev_sb

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct nc1fdev_sb
    {
        public long fd_name; /* Physical device name */
        public uint fd_sblk; /* Start block number */
        public uint fd_nblk; /* Number of blocks */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NC1_MAXIREG)]
        public nc1ireg_sb[] fd_ireg; /* Inode regions */
    }

#endregion

#region Nested type: nc1ireg_sb

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct nc1ireg_sb
    {
        public ushort i_unused; /* reserved */
        public ushort i_nblk;   /* number of blocks */
        public uint   i_sblk;   /* start block number */
    }

#endregion

#region Nested type: Superblock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [SwapEndian]
    partial struct Superblock
    {
        public ulong s_magic; /* magic number to indicate file system type */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] s_fname; /* file system name */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] s_fpack; /* file system pack name */
        public dev_t s_dev;    /* major/minor device, for verification */

        public daddr_t  s_fsize;   /* size in blocks of entire volume */
        public long     s_isize;   /* Number of total inodes */
        public long     s_bigfile; /* number of bytes at which a file is big */
        public long     s_bigunit; /* minimum number of blocks allocated for big files */
        public ulong    s_secure;  /* security: secure FS label */
        public long     s_maxlvl;  /* security: maximum security level */
        public long     s_minlvl;  /* security: minimum security level */
        public long     s_valcmp;  /* security: valid security compartments */
        public time_t   s_time;    /* last super block update */
        public blkno_t  s_dboff;   /* Dynamic block number */
        public ino_t    s_root;    /* root inode */
        public long     s_error;   /* Type of file system error detected */
        public blkno_t  s_mapoff;  /* Start map block number */
        public long     s_mapblks; /* Last map block number */
        public long     s_nscpys;  /* Number of copies of s.b per partition */
        public long     s_npart;   /* Number of partitions */
        public long     s_ifract;  /* Ratio of inodes to blocks */
        public extent_t s_sfs;     /* SFS only blocks */
        public long     s_flag;    /* Flag word */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NC1_MAXPART)]
        public nc1fdev_sb[] s_part; /* Partition descriptors */
        public long s_iounit;       /* Physical block size */
        public long s_numiresblks;  /* number of inode reservation blocks */
        /* per region (currently 1) */
        /* 0 = 1*(AU) words, n = (n+1)*(AU) words */
        public long s_priparts; /* bitmap of primary partitions */
        public long s_priblock; /* block size of primary partition(s) */
        /* 0 = 1*512 words, n = (n+1)*512 words */
        public long s_prinblks; /* number of 512 wds blocks in primary */
        public long s_secparts; /* bitmap of secondary partitions */
        public long s_secblock; /* block size of secondary partition(s) */
        /* 0 = 1*512 words, n = (n+1)*512 words */
        public long s_secnblks;  /* number of 512 wds blocks in secondary */
        public long s_sbdbparts; /* bitmap of partitions with file system data */
        /* including super blocks, dynamic block */
        /* and free block bitmaps (only primary */
        /* partitions may contain these) */
        public long s_rootdparts; /* bitmap of partitions with root directory */
        /* (only primary partitions) */
        public long s_nudparts; /* bitmap of no-user-data partitions */
        /* (only primary partitions) */
        public long s_nsema;     /* SFS: # fs semaphores to allocate */
        public long s_priactive; /* bitmap of primary partitions which contain */
        /* active (up to date) dynamic blocks and */
        /* free block bitmaps. All bits set indicate */
        /* that all primary partitions are active, */
        /* and no kernel manipulation of active flag */
        /* is allowed. */
        public long s_sfs_arbiterid; /* SFS Arbiter ID */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 91)]
        public long[] s_fill; /* reserved */
    }

#endregion
}