// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
/// <summary>Implements detection of the UNIX boot filesystem</summary>
public sealed partial class BFS
{
#region Nested type: SuperBlock

    /// <summary>BFS superblock layout on disk (512 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    partial struct SuperBlock
    {
        /// <summary>0x00, Magic number 0x1BADFACE</summary>
        public uint s_magic;
        /// <summary>0x04, Start offset in bytes of data area</summary>
        public uint s_start;
        /// <summary>0x08, End offset in bytes of data area</summary>
        public uint s_end;
        /// <summary>0x0C, Used for compaction (from inode)</summary>
        public uint s_from;
        /// <summary>0x10, Used for compaction (to inode)</summary>
        public uint s_to;
        /// <summary>0x14, Used for compaction (from block)</summary>
        public int s_bfrom;
        /// <summary>0x18, Used for compaction (to block)</summary>
        public int s_bto;
        /// <summary>0x1C, Filesystem name (6 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_fsname;
        /// <summary>0x22, Volume name (6 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] s_volume;
        /// <summary>0x28, Padding to 512 bytes (118 uint32s = 472 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 118)]
        public uint[] s_padding;
    }

#endregion

#region Nested type: Inode

    /// <summary>BFS inode layout on disk (64 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    partial struct Inode
    {
        /// <summary>0x00, Inode number</summary>
        public ushort i_ino;
        /// <summary>0x02, Unused</summary>
        public ushort i_unused;
        /// <summary>0x04, First data block</summary>
        public uint i_sblock;
        /// <summary>0x08, Last data block</summary>
        public uint i_eblock;
        /// <summary>0x0C, EOF offset in last block</summary>
        public uint i_eoffset;
        /// <summary>0x10, Vnode type (1=regular, 2=directory)</summary>
        public uint i_vtype;
        /// <summary>0x14, File mode/permissions</summary>
        public uint i_mode;
        /// <summary>0x18, Owner user ID</summary>
        public uint i_uid;
        /// <summary>0x1C, Owner group ID</summary>
        public uint i_gid;
        /// <summary>0x20, Number of links</summary>
        public uint i_nlink;
        /// <summary>0x24, Access time</summary>
        public uint i_atime;
        /// <summary>0x28, Modification time</summary>
        public uint i_mtime;
        /// <summary>0x2C, Change time</summary>
        public uint i_ctime;
        /// <summary>0x30, Padding (4 uint32s = 16 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] i_padding;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>BFS directory entry (16 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    partial struct DirectoryEntry
    {
        /// <summary>0x00, Inode number</summary>
        public ushort ino;
        /// <summary>0x02, Filename (14 bytes, null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BFS_NAMELEN)]
        public byte[] name;
    }

#endregion
}