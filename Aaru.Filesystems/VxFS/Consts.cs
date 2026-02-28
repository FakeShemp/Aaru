// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Veritas filesystem</summary>
public sealed partial class VxFS
{
    /// <summary>Identifier for VxFS</summary>
    const uint VXFS_MAGIC = 0xA501FCF5;
    /// <summary>Identifier for VxFS, big-endian</summary>
    const uint VXFS_MAGIC_BE = 0xF5FC01A5;
    /// <summary>OLT magic number</summary>
    const uint VXFS_OLT_MAGIC = 0xA504FCF5;
    /// <summary>Superblock offset for Unixware/x86 (block 1, little-endian)</summary>
    const uint VXFS_BASE = 0x400;
    /// <summary>Superblock offset for HP-UX/parisc (block 8, big-endian)</summary>
    const uint VXFS_BASE_BE = 0x2000;
    /// <summary>The root inode</summary>
    const uint VXFS_ROOT_INO = 2;
    /// <summary>Number of direct addresses in inode</summary>
    const int VXFS_NDADDR = 10;
    /// <summary>Number of indirect addresses in inode</summary>
    const int VXFS_NIADDR = 2;
    /// <summary>Size of immediate data in inode</summary>
    const int VXFS_NIMMED = 96;
    /// <summary>Number of typed extents</summary>
    const int VXFS_NTYPED = 6;
    /// <summary>Number of entries in free extent array</summary>
    const int VXFS_NEFREE = 32;
    /// <summary>Maximum length of directory entry name</summary>
    const int VXFS_NAMELEN = 256;
    /// <summary>Inode size</summary>
    const int VXFS_ISIZE = 0x100;
    /// <summary>Typed extent offset mask</summary>
    const ulong VXFS_TYPED_OFFSETMASK = 0x00FFFFFFFFFFFFFFUL;
    /// <summary>Typed extent type mask</summary>
    const ulong VXFS_TYPED_TYPEMASK = 0xFF00000000000000UL;
    /// <summary>Typed extent type shift</summary>
    const int VXFS_TYPED_TYPESHIFT = 56;
    /// <summary>VxFS type mask for mode field</summary>
    const uint VXFS_TYPE_MASK = 0xFFFFF000;

    const string FS_TYPE = "vxfs";

    /// <summary>VxFS OLT entry types</summary>
    enum OltEntryType : uint
    {
        /// <summary>Free OLT entry</summary>
        Free = 1,
        /// <summary>Fileset header</summary>
        FsHead = 2,
        /// <summary>Current usage table</summary>
        Cut = 3,
        /// <summary>Initial inode list</summary>
        Ilist = 4,
        /// <summary>Device configuration</summary>
        Dev = 5,
        /// <summary>Superblock</summary>
        Sb = 6
    }

    /// <summary>Typed extent descriptor types</summary>
    enum TypedExtentType : byte
    {
        /// <summary>Indirect extent</summary>
        Indirect = 1,
        /// <summary>Data extent</summary>
        Data = 2,
        /// <summary>Indirect extent (dev4)</summary>
        IndirectDev4 = 3,
        /// <summary>Data extent (dev4)</summary>
        DataDev4 = 4
    }

    /// <summary>Inode organisation types</summary>
    enum InodeOrgType : byte
    {
        /// <summary>Inode has no format</summary>
        None = 0,
        /// <summary>Ext4 organisation</summary>
        Ext4 = 1,
        /// <summary>All data stored in inode</summary>
        Immed = 2,
        /// <summary>Typed extents</summary>
        Typed = 3
    }

    /// <summary>VxFS file modes and types</summary>
    enum VxfsFileType : uint
    {
        /// <summary>Named pipe</summary>
        Fifo = 0x00001000,
        /// <summary>Character device</summary>
        Chr = 0x00002000,
        /// <summary>Directory</summary>
        Dir = 0x00004000,
        /// <summary>Xenix device</summary>
        Nam = 0x00005000,
        /// <summary>Block device</summary>
        Blk = 0x00006000,
        /// <summary>Regular file</summary>
        Reg = 0x00008000,
        /// <summary>Compressed file</summary>
        Cmp = 0x00009000,
        /// <summary>Symlink</summary>
        Lnk = 0x0000A000,
        /// <summary>Socket</summary>
        Soc = 0x0000C000,
        /// <summary>Fileset header</summary>
        Fsh = 0x10000000,
        /// <summary>Inode list</summary>
        Ilt = 0x20000000,
        /// <summary>Inode allocation unit</summary>
        Iau = 0x30000000,
        /// <summary>Current usage table</summary>
        Cut = 0x40000000,
        /// <summary>Attribute inode</summary>
        Att = 0x50000000,
        /// <summary>Link count table</summary>
        Lct = 0x60000000,
        /// <summary>Indirect attribute file</summary>
        Iat = 0x70000000,
        /// <summary>Extent map reorg file</summary>
        Emr = 0x80000000,
        /// <summary>BSD quota file</summary>
        Quo = 0x90000000,
        /// <summary>Pass through inode</summary>
        Pti = 0xA0000000,
        /// <summary>Device label file</summary>
        Lab = 0x11000000,
        /// <summary>OLT file</summary>
        Olt = 0x12000000,
        /// <summary>Log file</summary>
        Log = 0x13000000,
        /// <summary>Extent map file</summary>
        Emp = 0x14000000,
        /// <summary>Extent AU file</summary>
        Eau = 0x15000000,
        /// <summary>Extent AU summary file</summary>
        Aus = 0x16000000,
        /// <summary>Device config file</summary>
        Dev = 0x17000000
    }
}