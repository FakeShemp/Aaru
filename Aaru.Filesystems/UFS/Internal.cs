// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class UFSPlugin
{
    /// <summary>Represents an opened directory for iteration</summary>
    sealed class UfsDirNode : IDirNode
    {
        public string[] Entries  { get; init; }
        public int      Position { get; set; }
        public string   Path     { get; init; }
    }

    /// <summary>Represents an opened file for reading</summary>
    sealed class UfsFileNode : IFileNode
    {
        public uint       InodeNumber { get; init; }
        public List<long> BlockList   { get; init; }
        public string     Path        { get; init; }
        public long       Length      { get; init; }
        public long       Offset      { get; set; }
    }

    /// <summary>Normalized superblock combining fields from all UFS variants</summary>
    sealed class UfsSuperBlock
    {
        // Shift/mask values for fast computation
        /// <summary>``blkoff'' calc of blk offsets</summary>
        public int fs_bmask;
        /// <summary>``lblkno'' calc of logical blkno</summary>
        public int fs_bshift;
        /// <summary>size of basic blocks in fs</summary>
        public int fs_bsize;
        /// <summary>offset of cyl-block in filesys</summary>
        public int fs_cblkno;
        /// <summary>used to calc mod fs_ntrak</summary>
        public int fs_cgmask;

        // Cylinder group layout (UFS1 only; UFS2 uses cgbase = fpg * cg)
        /// <summary>cylinder group offset in cylinder</summary>
        public int fs_cgoffset;
        /// <summary>cylinder group size</summary>
        public int fs_cgsize;

        // State
        /// <summary>filesystem is clean flag</summary>
        public int fs_clean;
        /// <summary>blk addr of cyl grp summary area</summary>
        public long fs_csaddr;
        /// <summary>size of cyl grp summary area</summary>
        public int fs_cssize;
        /// <summary>number of free blocks</summary>
        public long fs_cstotal_nbfree;

        // Summary information
        /// <summary>number of directories</summary>
        public long fs_cstotal_ndir;
        /// <summary>number of free frags</summary>
        public long fs_cstotal_nffree;
        /// <summary>number of free inodes</summary>
        public long fs_cstotal_nifree;
        /// <summary>offset of first data after cg</summary>
        public int fs_dblkno;
        /// <summary>number of data blocks in fs</summary>
        public long fs_dsize;
        /// <summary>FS_ flags</summary>
        public int fs_flags;
        /// <summary>``fragoff'' calc of frag offsets</summary>
        public int fs_fmask;
        /// <summary>blocks per group * fs_frag (frags per group)</summary>
        public int fs_fpg;
        /// <summary>number of frags in a block in fs</summary>
        public int fs_frag;
        /// <summary>block to frag shift</summary>
        public int fs_fragshift;
        /// <summary>fsbtodb and dbtofsb shift constant</summary>
        public int fs_fsbtodb;
        /// <summary>``numfrags'' calc number of frags</summary>
        public int fs_fshift;
        /// <summary>size of frag blocks in fs</summary>
        public int fs_fsize;

        // Volume names
        /// <summary>name mounted on</summary>
        public string fs_fsmnt;
        /// <summary>offset of inode-blocks in filesys</summary>
        public int fs_iblkno;
        /// <summary>unique filesystem id</summary>
        public int fs_id_1;
        /// <summary>unique filesystem id</summary>
        public int fs_id_2;
        /// <summary>format of on-disk inodes (FS_42INODEFMT or FS_44INODEFMT)</summary>
        public int fs_inodefmt;
        /// <summary>value of INOPB (number of inodes per block)</summary>
        public uint fs_inopb;
        /// <summary>inodes per group</summary>
        public int fs_ipg;

        // UFS2 specific
        /// <summary>true if this is a UFS2 filesystem</summary>
        public bool fs_isUfs2;

        // Identity
        /// <summary>magic number</summary>
        public uint fs_magic;

        // Symlink and inode format
        /// <summary>max length of an internal symlink</summary>
        public int fs_maxsymlinklen;
        /// <summary>size of area reserved for metadata (UFS2)</summary>
        public long fs_metaspace;

        // Sizes
        /// <summary>number of cylinder groups</summary>
        public int fs_ncg;

        // Inode parameters
        /// <summary>value of NINDIR (number of indirect pointers per block)</summary>
        public int fs_nindir;
        /// <summary>cylinders per group</summary>
        public int fs_old_cpg;
        /// <summary>cylinders in filesystem</summary>
        public int fs_old_ncyl;

        // Old geometry (UFS1)
        /// <summary>sectors per cylinder</summary>
        public int fs_old_spc;

        // Filesystem geometry
        /// <summary>addr of super-block in filesys (fragment offset)</summary>
        public int fs_sblkno;
        /// <summary>byte offset of standard superblock (UFS2)</summary>
        public long fs_sblockloc;
        /// <summary>actual size of super block</summary>
        public int fs_sbsize;

        // Total sizes (64-bit for UFS2 compat)
        /// <summary>number of blocks (frags) in fs</summary>
        public long fs_size;

        // Timestamps
        /// <summary>last time written</summary>
        public long fs_time;
        /// <summary>volume name</summary>
        public string fs_volname;
    }
}