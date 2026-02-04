// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of QNX 4 filesystem</summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class QNX4
{
#region Nested type: qnx4_xtnt_t

    /// <summary>QNX4 extent structure</summary>
    /// <remarks>From qnxtypes.h: typedef struct { __le32 xtnt_blk; __le32 xtnt_size; } qnx4_xtnt_t;</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx4_xtnt_t
    {
        /// <summary>Starting block of extent</summary>
        public readonly uint xtnt_blk;
        /// <summary>Size of extent in blocks</summary>
        public readonly uint xtnt_size;
    }

#endregion

#region Nested type: qnx4_inode_entry

    /// <summary>QNX4 inode entry (directory entry for regular files/directories)</summary>
    /// <remarks>
    ///     From qnx4_fs.h: struct qnx4_inode_entry
    ///     Size: 64 bytes (QNX4_DIR_ENTRY_SIZE)
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx4_inode_entry
    {
        /// <summary>Filename (max 16 chars for short names)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = QNX4_SHORT_NAME_MAX)]
        public readonly byte[] di_fname;
        /// <summary>File size in bytes</summary>
        public readonly uint di_size;
        /// <summary>First extent</summary>
        public readonly qnx4_xtnt_t di_first_xtnt;
        /// <summary>Block number of extent block (xblk)</summary>
        public readonly uint di_xblk;
        /// <summary>File creation time</summary>
        public readonly uint di_ftime;
        /// <summary>Last modification time</summary>
        public readonly uint di_mtime;
        /// <summary>Last access time</summary>
        public readonly uint di_atime;
        /// <summary>Last change time</summary>
        public readonly uint di_ctime;
        /// <summary>Number of extents</summary>
        public readonly ushort di_num_xtnts;
        /// <summary>File mode/permissions</summary>
        public readonly ushort di_mode;
        /// <summary>User ID</summary>
        public readonly ushort di_uid;
        /// <summary>Group ID</summary>
        public readonly ushort di_gid;
        /// <summary>Number of links</summary>
        public readonly ushort di_nlink;
        /// <summary>Reserved/zero</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] di_zero;
        /// <summary>File type</summary>
        public readonly byte di_type;
        /// <summary>File status flags</summary>
        public readonly byte di_status;
    }

#endregion

#region Nested type: qnx4_link_info

    /// <summary>QNX4 link info (directory entry for links)</summary>
    /// <remarks>
    ///     From qnx4_fs.h: struct qnx4_link_info
    ///     Size: 64 bytes (QNX4_DIR_ENTRY_SIZE)
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx4_link_info
    {
        /// <summary>Filename (max 48 chars for long names/links)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = QNX4_NAME_MAX)]
        public readonly byte[] dl_fname;
        /// <summary>Block number containing the inode</summary>
        public readonly uint dl_inode_blk;
        /// <summary>Index of inode within the block</summary>
        public readonly byte dl_inode_ndx;
        /// <summary>Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly byte[] dl_spare;
        /// <summary>Link status flags</summary>
        public readonly byte dl_status;
    }

#endregion

#region Nested type: qnx4_xblk

    /// <summary>QNX4 extent block (contains additional extents for large files)</summary>
    /// <remarks>
    ///     From qnx4_fs.h: struct qnx4_xblk
    ///     Size: 512 bytes (QNX4_XBLK_ENTRY_SIZE)
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx4_xblk
    {
        /// <summary>Next extent block number</summary>
        public readonly uint xblk_next_xblk;
        /// <summary>Previous extent block number</summary>
        public readonly uint xblk_prev_xblk;
        /// <summary>Number of extents in this block</summary>
        public readonly byte xblk_num_xtnts;
        /// <summary>Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] xblk_spare;
        /// <summary>Total number of blocks in all extents</summary>
        public readonly uint xblk_num_blocks;
        /// <summary>Array of extents (max 60)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = QNX4_MAX_XTNTS_PER_XBLK)]
        public readonly qnx4_xtnt_t[] xblk_xtnts;
        /// <summary>Signature</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] xblk_signature;
        /// <summary>First extent (copy of inode's first extent)</summary>
        public readonly qnx4_xtnt_t xblk_first_xtnt;
    }

#endregion

#region Nested type: qnx4_super_block

    /// <summary>QNX4 superblock</summary>
    /// <remarks>
    ///     From qnx4_fs.h: struct qnx4_super_block
    ///     Located at block 1 (offset 512 bytes)
    ///     Size: 256 bytes (4 * 64 byte inodes)
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct qnx4_super_block
    {
        /// <summary>Root directory inode</summary>
        public readonly qnx4_inode_entry RootDir;
        /// <summary>Inode file (bitmap) inode</summary>
        public readonly qnx4_inode_entry Inode;
        /// <summary>Boot file inode</summary>
        public readonly qnx4_inode_entry Boot;
        /// <summary>Alternate boot file inode</summary>
        public readonly qnx4_inode_entry AltBoot;
    }

#endregion
}