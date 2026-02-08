// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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

// Information from the Minix source code
/// <inheritdoc />
/// <summary>Implements detection of the MINIX filesystem</summary>
public sealed partial class MinixFS
{
#region Nested type: SuperBlock

    /// <summary>Superblock for Minix v1 and V2 filesystems</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>0x00, inodes on volume</summary>
        public ushort s_ninodes;

        /// <summary>0x02, zones on volume (V1 only)</summary>
        public ushort s_nzones;

        /// <summary>0x04, blocks on inode map</summary>
        public short s_imap_blocks;

        /// <summary>0x06, blocks on zone map</summary>
        public short s_zmap_blocks;

        /// <summary>0x08, first data zone</summary>
        public ushort s_firstdatazone;

        /// <summary>0x0A, log2 of blocks/zone</summary>
        public short s_log_zone_size;

        /// <summary>0x0C, max file size</summary>
        public uint s_max_size;

        /// <summary>0x10, magic</summary>
        public ushort s_magic;

        /// <summary>0x12, filesystem state (V1) or flags (V2)</summary>
        public ushort s_state;

        /// <summary>0x14, number of zones (V2, replaces s_nzones)</summary>
        public uint s_zones;
    }

#endregion

#region Nested type: SuperBlock3

    /// <summary>Superblock for Minix v3 filesystems</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock3
    {
        /// <summary>0x00, inodes on volume</summary>
        public uint s_ninodes;

        /// <summary>0x04, old zones on volume (for compatibility)</summary>
        public ushort s_nzones;

        /// <summary>0x06, blocks on inode map</summary>
        public ushort s_imap_blocks;

        /// <summary>0x08, blocks on zone map</summary>
        public ushort s_zmap_blocks;

        /// <summary>0x0A, first data zone (small, for compatibility)</summary>
        public ushort s_firstdatazone;

        /// <summary>0x0C, log2 of blocks/zone</summary>
        public ushort s_log_zone_size;

        /// <summary>0x0E, flags</summary>
        public ushort s_flags;

        /// <summary>0x10, max file size</summary>
        public uint s_max_size;

        /// <summary>0x14, number of zones</summary>
        public uint s_zones;

        /// <summary>0x18, magic</summary>
        public ushort s_magic;

        /// <summary>0x1A, padding</summary>
        public ushort s_pad2;

        /// <summary>0x1C, bytes in a block</summary>
        public ushort s_blocksize;

        /// <summary>0x1E, on-disk structures version</summary>
        public byte s_disk_version;
    }

#endregion

#region Nested type: V1DiskInode

    /// <summary>V1 disk inode structure (32 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct V1DiskInode
    {
        /// <summary>0x00, file type, protection, etc.</summary>
        public ushort d1_mode;

        /// <summary>0x02, user id of the file's owner</summary>
        public ushort d1_uid;

        /// <summary>0x04, current file size in bytes</summary>
        public uint d1_size;

        /// <summary>0x08, when was file data last modified</summary>
        public uint d1_mtime;

        /// <summary>0x0C, group number (lower 8 bits)</summary>
        public byte d1_gid;

        /// <summary>0x0D, how many links to this file</summary>
        public byte d1_nlinks;

        /// <summary>0x0E, zone numbers for direct, indirect, and double indirect (9 zones, 2 bytes each = 18 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public ushort[] d1_zone;
    }

#endregion

#region Nested type: V2DiskInode

    /// <summary>V2/V3 disk inode structure (64 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct V2DiskInode
    {
        /// <summary>0x00, file type, protection, etc.</summary>
        public ushort d2_mode;

        /// <summary>0x02, how many links to this file</summary>
        public ushort d2_nlinks;

        /// <summary>0x04, user id of the file's owner</summary>
        public short d2_uid;

        /// <summary>0x06, group number</summary>
        public ushort d2_gid;

        /// <summary>0x08, current file size in bytes</summary>
        public uint d2_size;

        /// <summary>0x0C, when was file data last accessed</summary>
        public uint d2_atime;

        /// <summary>0x10, when was file data last modified</summary>
        public uint d2_mtime;

        /// <summary>0x14, when was inode data last changed</summary>
        public uint d2_ctime;

        /// <summary>0x18, zone numbers for direct, indirect, double indirect, and triple indirect (10 zones)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public uint[] d2_zone;
    }

#endregion

#region Nested type: V1DirectoryEntry

    /// <summary>V1 directory entry with 14-character filename (16 bytes total)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct V1DirectoryEntry
    {
        /// <summary>0x00, inode number (0 = unused entry)</summary>
        public ushort d_ino;

        /// <summary>0x02, filename (14 characters, null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public byte[] d_name;
    }

#endregion

#region Nested type: V1DirectoryEntryLong

    /// <summary>V1 directory entry with 30-character filename (32 bytes total)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct V1DirectoryEntryLong
    {
        /// <summary>0x00, inode number (0 = unused entry)</summary>
        public ushort d_ino;

        /// <summary>0x02, filename (30 characters, null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] d_name;
    }

#endregion

#region Nested type: V2DirectoryEntry

    /// <summary>V2 directory entry with 14-character filename (16 bytes total)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct V2DirectoryEntry
    {
        /// <summary>0x00, inode number (0 = unused entry)</summary>
        public ushort d_ino;

        /// <summary>0x02, filename (14 characters, null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public byte[] d_name;
    }

#endregion

#region Nested type: V2DirectoryEntryLong

    /// <summary>V2 directory entry with 30-character filename (32 bytes total)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct V2DirectoryEntryLong
    {
        /// <summary>0x00, inode number (0 = unused entry)</summary>
        public ushort d_ino;

        /// <summary>0x02, filename (30 characters, null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] d_name;
    }

#endregion

#region Nested type: V3DirectoryEntry

    /// <summary>V3 directory entry with 60-character filename (64 bytes total)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct V3DirectoryEntry
    {
        /// <summary>0x00, inode number (0 = unused entry)</summary>
        public uint d_ino;

        /// <summary>0x04, filename (60 characters, null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public byte[] d_name;
    }

#endregion
}