// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of QNX 6 filesystem</summary>
public sealed partial class QNX6
{
#region Nested type: qnx6_root_node

    /// <summary>QNX6 root node structure (used in superblock for inode, bitmap, longfile trees)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx6_root_node
    {
        /// <summary>Size of the tree</summary>
        public readonly ulong size;
        /// <summary>Block pointers (16 direct pointers)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly uint[] ptr;
        /// <summary>Number of indirect levels</summary>
        public readonly byte levels;
        /// <summary>Mode</summary>
        public readonly byte mode;
        /// <summary>Spare bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] spare;
    }

#endregion

#region Nested type: qnx6_super_block

    /// <summary>QNX6 superblock structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx6_super_block
    {
        /// <summary>Magic number (0x68191122)</summary>
        public readonly uint sb_magic;
        /// <summary>Superblock checksum</summary>
        public readonly uint sb_checksum;
        /// <summary>Volume serial number</summary>
        public readonly ulong sb_serial;
        /// <summary>Creation time</summary>
        public readonly uint sb_ctime;
        /// <summary>Last access time</summary>
        public readonly uint sb_atime;
        /// <summary>Flags</summary>
        public readonly uint sb_flags;
        /// <summary>Filesystem version 1</summary>
        public readonly ushort sb_version1;
        /// <summary>Filesystem version 2</summary>
        public readonly ushort sb_version2;
        /// <summary>Volume ID (16 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] sb_volumeid;
        /// <summary>Block size in bytes</summary>
        public readonly uint sb_blocksize;
        /// <summary>Total number of inodes</summary>
        public readonly uint sb_num_inodes;
        /// <summary>Number of free inodes</summary>
        public readonly uint sb_free_inodes;
        /// <summary>Total number of blocks</summary>
        public readonly uint sb_num_blocks;
        /// <summary>Number of free blocks</summary>
        public readonly uint sb_free_blocks;
        /// <summary>Allocation group</summary>
        public readonly uint sb_allocgroup;
        /// <summary>Inode tree root</summary>
        public readonly qnx6_root_node Inode;
        /// <summary>Bitmap tree root</summary>
        public readonly qnx6_root_node Bitmap;
        /// <summary>Long filename tree root</summary>
        public readonly qnx6_root_node Longfile;
        /// <summary>Unknown tree root</summary>
        public readonly qnx6_root_node Unknown;
    }

#endregion

#region Nested type: qnx6_mmi_super_block

    /// <summary>Audi MMI 3G superblock structure (different layout from plain QNX6)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx6_mmi_super_block
    {
        /// <summary>Magic number</summary>
        public readonly uint sb_magic;
        /// <summary>Superblock checksum</summary>
        public readonly uint sb_checksum;
        /// <summary>Volume serial number</summary>
        public readonly ulong sb_serial;
        /// <summary>Spare bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] sb_spare0;
        /// <summary>ID (12 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] sb_id;
        /// <summary>Block size in bytes</summary>
        public readonly uint sb_blocksize;
        /// <summary>Total number of inodes</summary>
        public readonly uint sb_num_inodes;
        /// <summary>Number of free inodes</summary>
        public readonly uint sb_free_inodes;
        /// <summary>Total number of blocks</summary>
        public readonly uint sb_num_blocks;
        /// <summary>Number of free blocks</summary>
        public readonly uint sb_free_blocks;
        /// <summary>Spare bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] sb_spare1;
        /// <summary>Inode tree root</summary>
        public readonly qnx6_root_node Inode;
        /// <summary>Bitmap tree root</summary>
        public readonly qnx6_root_node Bitmap;
        /// <summary>Long filename tree root</summary>
        public readonly qnx6_root_node Longfile;
        /// <summary>Unknown tree root</summary>
        public readonly qnx6_root_node Unknown;
    }

#endregion

#region Nested type: qnx6_inode_entry

    /// <summary>QNX6 inode entry (128 bytes each)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx6_inode_entry
    {
        /// <summary>File size</summary>
        public readonly ulong di_size;
        /// <summary>User ID</summary>
        public readonly uint di_uid;
        /// <summary>Group ID</summary>
        public readonly uint di_gid;
        /// <summary>File time (creation)</summary>
        public readonly uint di_ftime;
        /// <summary>Modification time</summary>
        public readonly uint di_mtime;
        /// <summary>Access time</summary>
        public readonly uint di_atime;
        /// <summary>Change time</summary>
        public readonly uint di_ctime;
        /// <summary>File mode (permissions)</summary>
        public readonly ushort di_mode;
        /// <summary>Extended mode</summary>
        public readonly ushort di_ext_mode;
        /// <summary>Block pointers (16 direct pointers)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly uint[] di_block_ptr;
        /// <summary>Number of indirect levels</summary>
        public readonly byte di_filelevels;
        /// <summary>File status (0x01=directory, 0x02=deleted, 0x03=normal)</summary>
        public readonly byte di_status;
        /// <summary>Unknown bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] di_unknown2;
        /// <summary>Zero padding</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly uint[] di_zero2;
    }

#endregion

#region Nested type: qnx6_dir_entry

    /// <summary>QNX6 directory entry (32 bytes max, short filename)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx6_dir_entry
    {
        /// <summary>Inode number</summary>
        public readonly uint de_inode;
        /// <summary>Filename length</summary>
        public readonly byte de_size;
        /// <summary>Filename (max 27 characters)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 27)]
        public readonly byte[] de_fname;
    }

#endregion

#region Nested type: qnx6_long_dir_entry

    /// <summary>QNX6 long directory entry (for filenames longer than 27 chars)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx6_long_dir_entry
    {
        /// <summary>Inode number</summary>
        public readonly uint de_inode;
        /// <summary>Size indicator (0xFF for long entries)</summary>
        public readonly byte de_size;
        /// <summary>Unknown bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] de_unknown;
        /// <summary>Long filename inode number (in longfile tree)</summary>
        public readonly uint de_long_inode;
        /// <summary>Checksum</summary>
        public readonly uint de_checksum;
    }

#endregion

#region Nested type: qnx6_long_filename

    /// <summary>QNX6 long filename structure (stored in longfile tree)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx6_long_filename
    {
        /// <summary>Filename length</summary>
        public readonly ushort lf_size;
        /// <summary>Filename (max 510 characters)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 510)]
        public readonly byte[] lf_fname;
    }

#endregion
}