// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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
public sealed partial class Squash
{
#region Nested type: SuperBlock

    /// <summary>Squashfs superblock</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>Magic number (0x73717368)</summary>
        public uint magic;
        /// <summary>Number of inodes in the filesystem</summary>
        public uint inodes;
        /// <summary>Filesystem creation time (Unix timestamp)</summary>
        public uint mkfs_time;
        /// <summary>Block size in bytes</summary>
        public uint block_size;
        /// <summary>Number of fragments</summary>
        public uint fragments;
        /// <summary>Compression algorithm used</summary>
        public ushort compression;
        /// <summary>Log2 of block size</summary>
        public ushort block_log;
        /// <summary>Filesystem flags</summary>
        public ushort flags;
        /// <summary>Number of uid/gid entries</summary>
        public ushort no_ids;
        /// <summary>Major version number</summary>
        public ushort s_major;
        /// <summary>Minor version number</summary>
        public ushort s_minor;
        /// <summary>Reference to the root inode</summary>
        public ulong root_inode;
        /// <summary>Total bytes used by the filesystem</summary>
        public ulong bytes_used;
        /// <summary>Offset to id table</summary>
        public ulong id_table_start;
        /// <summary>Offset to xattr id table</summary>
        public ulong xattr_id_table_start;
        /// <summary>Offset to inode table</summary>
        public ulong inode_table_start;
        /// <summary>Offset to directory table</summary>
        public ulong directory_table_start;
        /// <summary>Offset to fragment table</summary>
        public ulong fragment_table_start;
        /// <summary>Offset to lookup table</summary>
        public ulong lookup_table_start;
    }

#endregion

#region Nested type: BaseInode

    /// <summary>Base inode structure common to all inode types</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BaseInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
    }

#endregion

#region Nested type: IpcInode

    /// <summary>IPC (FIFO/socket) inode structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct IpcInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Number of hard links</summary>
        public uint nlink;
    }

#endregion

#region Nested type: ExtendedIpcInode

    /// <summary>Extended IPC (FIFO/socket) inode structure with xattr support</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct ExtendedIpcInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Number of hard links</summary>
        public uint nlink;
        /// <summary>Extended attributes index</summary>
        public uint xattr;
    }

#endregion

#region Nested type: DevInode

    /// <summary>Device inode structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DevInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Number of hard links</summary>
        public uint nlink;
        /// <summary>Device number (major/minor)</summary>
        public uint rdev;
    }

#endregion

#region Nested type: ExtendedDevInode

    /// <summary>Extended device inode structure with xattr support</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct ExtendedDevInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Number of hard links</summary>
        public uint nlink;
        /// <summary>Device number (major/minor)</summary>
        public uint rdev;
        /// <summary>Extended attributes index</summary>
        public uint xattr;
    }

#endregion

#region Nested type: SymlinkInode

    /// <summary>Symbolic link inode structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SymlinkInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Number of hard links</summary>
        public uint nlink;
        /// <summary>Length of the symbolic link target</summary>
        public uint symlink_size;

        // Followed by symlink target (char symlink[])
    }

#endregion

#region Nested type: RegInode

    /// <summary>Regular file inode structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RegInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Start block of the file data</summary>
        public uint start_block;
        /// <summary>Fragment index (or SQUASHFS_INVALID_FRAG if no fragment)</summary>
        public uint fragment;
        /// <summary>Offset within the fragment</summary>
        public uint offset;
        /// <summary>File size in bytes</summary>
        public uint file_size;

        // Followed by block list (ushort block_list[])
    }

#endregion

#region Nested type: ExtendedRegInode

    /// <summary>Extended regular file inode structure for large files with xattr support</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct ExtendedRegInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Start block of the file data</summary>
        public ulong start_block;
        /// <summary>File size in bytes</summary>
        public ulong file_size;
        /// <summary>Sparse file indicator</summary>
        public ulong sparse;
        /// <summary>Number of hard links</summary>
        public uint nlink;
        /// <summary>Fragment index (or SQUASHFS_INVALID_FRAG if no fragment)</summary>
        public uint fragment;
        /// <summary>Offset within the fragment</summary>
        public uint offset;
        /// <summary>Extended attributes index</summary>
        public uint xattr;

        // Followed by block list (ushort block_list[])
    }

#endregion

#region Nested type: DirInode

    /// <summary>Directory inode structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Start block of the directory entries</summary>
        public uint start_block;
        /// <summary>Number of hard links</summary>
        public uint nlink;
        /// <summary>Directory size</summary>
        public ushort file_size;
        /// <summary>Offset within the block</summary>
        public ushort offset;
        /// <summary>Parent inode number</summary>
        public uint parent_inode;
    }

#endregion

#region Nested type: ExtendedDirInode

    /// <summary>Extended directory inode structure with index and xattr support</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct ExtendedDirInode
    {
        /// <summary>Inode type</summary>
        public ushort inode_type;
        /// <summary>File mode (permissions)</summary>
        public ushort mode;
        /// <summary>User ID index</summary>
        public ushort uid;
        /// <summary>Group ID index</summary>
        public ushort guid;
        /// <summary>Modification time (Unix timestamp)</summary>
        public uint mtime;
        /// <summary>Inode number</summary>
        public uint inode_number;
        /// <summary>Number of hard links</summary>
        public uint nlink;
        /// <summary>Directory size</summary>
        public uint file_size;
        /// <summary>Start block of the directory entries</summary>
        public uint start_block;
        /// <summary>Parent inode number</summary>
        public uint parent_inode;
        /// <summary>Number of directory index entries</summary>
        public ushort i_count;
        /// <summary>Offset within the block</summary>
        public ushort offset;
        /// <summary>Extended attributes index</summary>
        public uint xattr;

        // Followed by directory index entries (DirIndex index[])
    }

#endregion

#region Nested type: DirIndex

    /// <summary>Directory index entry for fast directory lookup</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirIndex
    {
        /// <summary>Index within the directory</summary>
        public uint index;
        /// <summary>Start block</summary>
        public uint start_block;
        /// <summary>Size of the name</summary>
        public uint size;

        // Followed by name (char name[])
    }

#endregion

#region Nested type: DirEntry

    /// <summary>Directory entry structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirEntry
    {
        /// <summary>Offset to the inode</summary>
        public ushort offset;
        /// <summary>Inode number offset from header</summary>
        public short inode_number;
        /// <summary>Entry type</summary>
        public ushort type;
        /// <summary>Name length minus 1</summary>
        public ushort size;

        // Followed by name (char name[])
    }

#endregion

#region Nested type: DirHeader

    /// <summary>Directory header structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirHeader
    {
        /// <summary>Number of entries minus 1</summary>
        public uint count;
        /// <summary>Start block containing the inodes</summary>
        public uint start_block;
        /// <summary>Base inode number</summary>
        public uint inode_number;
    }

#endregion

#region Nested type: FragmentEntry

    /// <summary>Fragment table entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct FragmentEntry
    {
        /// <summary>Start block of the fragment</summary>
        public ulong start_block;
        /// <summary>Size of the fragment (with compression flag)</summary>
        public uint size;
        /// <summary>Unused padding</summary>
        public uint unused;
    }

#endregion

#region Nested type: XattrEntry

    /// <summary>Extended attribute entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct XattrEntry
    {
        /// <summary>Attribute type (namespace)</summary>
        public ushort type;
        /// <summary>Attribute name size</summary>
        public ushort size;

        // Followed by attribute name (char data[])
    }

#endregion

#region Nested type: XattrVal

    /// <summary>Extended attribute value</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct XattrVal
    {
        /// <summary>Value size</summary>
        public uint vsize;

        // Followed by value (char value[])
    }

#endregion

#region Nested type: XattrId

    /// <summary>Extended attribute ID entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct XattrId
    {
        /// <summary>Reference to the xattr data</summary>
        public ulong xattr;
        /// <summary>Number of xattr entries</summary>
        public uint count;
        /// <summary>Total size of the xattr data</summary>
        public uint size;
    }

#endregion

#region Nested type: XattrIdTable

    /// <summary>Extended attribute ID table header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct XattrIdTable
    {
        /// <summary>Start of the xattr table</summary>
        public ulong xattr_table_start;
        /// <summary>Number of xattr IDs</summary>
        public uint xattr_ids;
        /// <summary>Unused padding</summary>
        public uint unused;
    }

#endregion
}