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

#region Nested type: DirectExtent

    /// <summary>Direct extent entry used in ext4 organisation</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirectExtent
    {
        /// <summary>Extent number</summary>
        public uint extent;
        /// <summary>Size of extent</summary>
        public uint size;
    }

#endregion

#region Nested type: Ext4

    /// <summary>Ext4 inode organisation</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Ext4
    {
        /// <summary>Spare</summary>
        public uint ve4_spare;
        /// <summary>Indirect extent size</summary>
        public uint ve4_indsize;
        /// <summary>Indirect extents</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] ve4_indir;
        /// <summary>Direct extents</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] ve4_direct;
    }

#endregion

#region Nested type: TypedExtent

    /// <summary>Typed extent descriptor</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct TypedExtent
    {
        /// <summary>Header, 0xTTOOOOOOOOOOOOOO; T=type, O=offset</summary>
        public ulong vt_hdr;
        /// <summary>Extent block</summary>
        public uint vt_block;
        /// <summary>Size in blocks</summary>
        public uint vt_size;
    }

#endregion

#region Nested type: TypedExtentDev4

    /// <summary>Typed extent descriptor for dev4</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct TypedExtentDev4
    {
        /// <summary>Header, 0xTTOOOOOOOOOOOOOO; T=type, O=offset</summary>
        public ulong vd4_hdr;
        /// <summary>Extent block</summary>
        public ulong vd4_block;
        /// <summary>Size in blocks</summary>
        public ulong vd4_size;
        /// <summary>Device ID</summary>
        public uint vd4_dev;
        /// <summary>Padding</summary>
        public byte __pad1;
    }

#endregion

#region Nested type: DiskInode

    /// <summary>On-disk inode structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DiskInode
    {
        /// <summary>File mode and type</summary>
        public uint vdi_mode;
        /// <summary>Link count</summary>
        public uint vdi_nlink;
        /// <summary>User ID</summary>
        public uint vdi_uid;
        /// <summary>Group ID</summary>
        public uint vdi_gid;
        /// <summary>Inode size in bytes</summary>
        public ulong vdi_size;
        /// <summary>Last time accessed - sec</summary>
        public uint vdi_atime;
        /// <summary>Last time accessed - usec</summary>
        public uint vdi_autime;
        /// <summary>Last modify time - sec</summary>
        public uint vdi_mtime;
        /// <summary>Last modify time - usec</summary>
        public uint vdi_mutime;
        /// <summary>Create time - sec</summary>
        public uint vdi_ctime;
        /// <summary>Create time - usec</summary>
        public uint vdi_cutime;
        /// <summary>Allocation flags</summary>
        public byte vdi_aflags;
        /// <summary>Organisation type</summary>
        public byte vdi_orgtype;
        /// <summary>Extended operation flags</summary>
        public ushort vdi_eopflags;
        /// <summary>Extended operation data</summary>
        public uint vdi_eopdata;
        /// <summary>File type area (union: rdev, dotdot, regular, vxspec) - 8 bytes</summary>
        public ulong vdi_ftarea;
        /// <summary>How many blocks does inode occupy</summary>
        public uint vdi_blocks;
        /// <summary>Inode generation</summary>
        public uint vdi_gen;
        /// <summary>Version</summary>
        public ulong vdi_version;
        /// <summary>Organisation data (union: immed[96], ext4, typed[6]) - 96 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public byte[] vdi_org;
        /// <summary>Indirect attribute inode</summary>
        public uint vdi_iattrino;
    }

#endregion

#region Nested type: OltHeader

    /// <summary>Object Location Table header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltHeader
    {
        /// <summary>Magic number</summary>
        public uint olt_magic;
        /// <summary>Size of this entry</summary>
        public uint olt_size;
        /// <summary>Checksum of extent</summary>
        public uint olt_checksum;
        /// <summary>Unused</summary>
        public uint __unused1;
        /// <summary>Time of last modification (sec)</summary>
        public uint olt_mtime;
        /// <summary>Time of last modification (usec)</summary>
        public uint olt_mutime;
        /// <summary>Free space in OLT extent</summary>
        public uint olt_totfree;
        /// <summary>Address of this extent and replica</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] olt_extents;
        /// <summary>Size of this extent</summary>
        public uint olt_esize;
        /// <summary>Address of next extent and replica</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] olt_next;
        /// <summary>Size of next extent</summary>
        public uint olt_nsize;
        /// <summary>Align to 8 byte boundary</summary>
        public uint __unused2;
    }

#endregion

#region Nested type: OltCommon

    /// <summary>Common OLT entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltCommon
    {
        /// <summary>Type of this record</summary>
        public uint olt_type;
        /// <summary>Size of this record</summary>
        public uint olt_size;
    }

#endregion

#region Nested type: OltFree

    /// <summary>Free OLT entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltFree
    {
        /// <summary>Type of this record</summary>
        public uint olt_type;
        /// <summary>Size of this free record</summary>
        public uint olt_fsize;
    }

#endregion

#region Nested type: OltIlist

    /// <summary>Initial inode list OLT entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltIlist
    {
        /// <summary>Type of this record</summary>
        public uint olt_type;
        /// <summary>Size of this record</summary>
        public uint olt_size;
        /// <summary>Initial inode list and replica</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] olt_iext;
    }

#endregion

#region Nested type: OltCut

    /// <summary>Current Usage Table OLT entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltCut
    {
        /// <summary>Type of this record</summary>
        public uint olt_type;
        /// <summary>Size of this record</summary>
        public uint olt_size;
        /// <summary>Inode of current usage table</summary>
        public uint olt_cutino;
        /// <summary>Unused, 8 byte align</summary>
        public byte __pad;
    }

#endregion

#region Nested type: OltSuperBlock

    /// <summary>Inodes containing Superblock, Intent log and OLTs</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltSuperBlock
    {
        /// <summary>Type of this record</summary>
        public uint olt_type;
        /// <summary>Size of this record</summary>
        public uint olt_size;
        /// <summary>Inode of superblock file</summary>
        public uint olt_sbino;
        /// <summary>Unused</summary>
        public uint __unused1;
        /// <summary>Inode of log file and replica</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] olt_logino;
        /// <summary>Inode of OLT and replica</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] olt_oltino;
    }

#endregion

#region Nested type: OltDev

    /// <summary>Device configuration OLT entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltDev
    {
        /// <summary>Type of this record</summary>
        public uint olt_type;
        /// <summary>Size of this record</summary>
        public uint olt_size;
        /// <summary>Inode of device config files</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] olt_devino;
    }

#endregion

#region Nested type: OltFsHead

    /// <summary>Fileset header OLT entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct OltFsHead
    {
        /// <summary>Type number</summary>
        public uint olt_type;
        /// <summary>Size of this record</summary>
        public uint olt_size;
        /// <summary>Inodes of fileset header</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] olt_fsino;
    }

#endregion

#region Nested type: DirectoryBlock

    /// <summary>VxFS directory block header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirectoryBlock
    {
        /// <summary>Free space in directory block</summary>
        public ushort d_free;
        /// <summary>Number of hash chains</summary>
        public ushort d_nhash;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>VxFS directory entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirectoryEntry
    {
        /// <summary>Inode number</summary>
        public uint d_ino;
        /// <summary>Record length</summary>
        public ushort d_reclen;
        /// <summary>Name length</summary>
        public ushort d_namelen;
        /// <summary>Next hash entry</summary>
        public ushort d_hashnext;
        /// <summary>Name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] d_name;
    }

#endregion

#region Nested type: FilesetHeader

    /// <summary>VxFS fileset header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct FilesetHeader
    {
        /// <summary>Fileset header version</summary>
        public uint fsh_version;
        /// <summary>Fileset index</summary>
        public uint fsh_fsindex;
        /// <summary>Modification time - sec</summary>
        public uint fsh_time;
        /// <summary>Modification time - usec</summary>
        public uint fsh_utime;
        /// <summary>Extop flags</summary>
        public uint fsh_extop;
        /// <summary>Allocated inodes</summary>
        public uint fsh_ninodes;
        /// <summary>Number of IAUs</summary>
        public uint fsh_nau;
        /// <summary>Old size of ilist</summary>
        public uint fsh_old_ilesize;
        /// <summary>Flags</summary>
        public uint fsh_dflags;
        /// <summary>Quota limit</summary>
        public uint fsh_quota;
        /// <summary>Maximum inode number</summary>
        public uint fsh_maxinode;
        /// <summary>IAU inode</summary>
        public uint fsh_iauino;
        /// <summary>Ilist inodes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] fsh_ilistino;
        /// <summary>Link count table inode</summary>
        public uint fsh_lctino;
    }

#endregion
}