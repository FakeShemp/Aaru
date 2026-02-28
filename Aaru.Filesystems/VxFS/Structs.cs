// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Veritas File System plugin.
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

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Veritas filesystem</summary>
public sealed partial class VxFS
{
#region Nested type: SuperBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>Magic number</summary>
        public uint vs_magic;
        /// <summary>VxFS version</summary>
        public int vs_version;
        /// <summary>create time - secs</summary>
        public uint vs_ctime;
        /// <summary>create time - usecs</summary>
        public uint vs_cutime;
        /// <summary>unused</summary>
        public int __unused1;
        /// <summary>unused</summary>
        public int __unused2;
        /// <summary>obsolete</summary>
        public int vs_old_logstart;
        /// <summary>obsolete</summary>
        public int vs_old_logend;
        /// <summary>block size</summary>
        public int vs_bsize;
        /// <summary>number of blocks</summary>
        public int vs_size;
        /// <summary>number of data blocks</summary>
        public int vs_dsize;
        /// <summary>obsolete</summary>
        public uint vs_old_ninode;
        /// <summary>obsolete</summary>
        public int vs_old_nau;
        /// <summary>unused</summary>
        public int __unused3;
        /// <summary>obsolete</summary>
        public int vs_old_defiextsize;
        /// <summary>obsolete</summary>
        public int vs_old_ilbsize;
        /// <summary>size of immediate data area</summary>
        public int vs_immedlen;
        /// <summary>number of direct extentes</summary>
        public int vs_ndaddr;
        /// <summary>address of first AU</summary>
        public int vs_firstau;
        /// <summary>offset of extent map in AU</summary>
        public int vs_emap;
        /// <summary>offset of inode map in AU</summary>
        public int vs_imap;
        /// <summary>offset of ExtOp. map in AU</summary>
        public int vs_iextop;
        /// <summary>offset of inode list in AU</summary>
        public int vs_istart;
        /// <summary>offset of fdblock in AU</summary>
        public int vs_bstart;
        /// <summary>aufirst + emap</summary>
        public int vs_femap;
        /// <summary>aufirst + imap</summary>
        public int vs_fimap;
        /// <summary>aufirst + iextop</summary>
        public int vs_fiextop;
        /// <summary>aufirst + istart</summary>
        public int vs_fistart;
        /// <summary>aufirst + bstart</summary>
        public int vs_fbstart;
        /// <summary>number of entries in indir</summary>
        public int vs_nindir;
        /// <summary>length of AU in blocks</summary>
        public int vs_aulen;
        /// <summary>length of imap in blocks</summary>
        public int vs_auimlen;
        /// <summary>length of emap in blocks</summary>
        public int vs_auemlen;
        /// <summary>length of ilist in blocks</summary>
        public int vs_auilen;
        /// <summary>length of pad in blocks</summary>
        public int vs_aupad;
        /// <summary>data blocks in AU</summary>
        public int vs_aublocks;
        /// <summary>log base 2 of aublocks</summary>
        public int vs_maxtier;
        /// <summary>number of inodes per blk</summary>
        public int vs_inopb;
        /// <summary>obsolete</summary>
        public int vs_old_inopau;
        /// <summary>obsolete</summary>
        public int vs_old_inopilb;
        /// <summary>obsolete</summary>
        public int vs_old_ndiripau;
        /// <summary>size of indirect addr ext.</summary>
        public int vs_iaddrlen;
        /// <summary>log base 2 of bsize</summary>
        public int vs_bshift;
        /// <summary>log base 2 of inobp</summary>
        public int vs_inoshift;
        /// <summary>~( bsize - 1 )</summary>
        public int vs_bmask;
        /// <summary>bsize - 1</summary>
        public int vs_boffmask;
        /// <summary>old_inopilb - 1</summary>
        public int vs_old_inomask;
        /// <summary>checksum of V1 data</summary>
        public int vs_checksum;
        /// <summary>number of free blocks</summary>
        public int vs_free;
        /// <summary>number of free inodes</summary>
        public int vs_ifree;
        /// <summary>number of free extents by size</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public int[] vs_efree;
        /// <summary>flags ?!?</summary>
        public int vs_flags;
        /// <summary>filesystem has been changed</summary>
        public byte vs_mod;
        /// <summary>clean FS</summary>
        public byte vs_clean;
        /// <summary>unused</summary>
        public ushort __unused4;
        /// <summary>mount time log ID</summary>
        public uint vs_firstlogid;
        /// <summary>last time written - sec</summary>
        public uint vs_wtime;
        /// <summary>last time written - usec</summary>
        public uint vs_wutime;
        /// <summary>FS name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] vs_fname;
        /// <summary>FS pack name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] vs_fpack;
        /// <summary>log format version</summary>
        public int vs_logversion;
        /// <summary>unused</summary>
        public int __unused5;
        /// <summary>OLT extent and replica</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] vs_oltext;
        /// <summary>OLT extent size</summary>
        public int vs_oltsize;
        /// <summary>size of inode map</summary>
        public int vs_iauimlen;
        /// <summary>size of IAU in blocks</summary>
        public int vs_iausize;
        /// <summary>size of inode in bytes</summary>
        public int vs_dinosize;
        /// <summary>indir levels per inode</summary>
        public int vs_old_dniaddr;
        /// <summary>checksum of V2 RO</summary>
        public int vs_checksum2;
    }

#endregion
}