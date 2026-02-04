// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class AtheOS
{
#region Nested type: SuperBlock

    /// <summary>Be superblock</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SuperBlock
    {
        /// <summary>0x000, Volume name, 32 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] name;
        /// <summary>0x020, "AFS1", 0x41465331</summary>
        public readonly uint magic1;
        /// <summary>0x024, "BIGE", 0x42494745</summary>
        public readonly uint fs_byte_order;
        /// <summary>0x028, Bytes per block</summary>
        public readonly uint block_size;
        /// <summary>0x02C, 1 &lt;&lt; block_shift == block_size</summary>
        public readonly uint block_shift;
        /// <summary>0x030, Blocks in volume</summary>
        public readonly long num_blocks;
        /// <summary>0x038, Used blocks in volume</summary>
        public readonly long used_blocks;
        /// <summary>0x040, Bytes per inode</summary>
        public readonly int inode_size;
        /// <summary>0x044, 0xDD121031</summary>
        public readonly uint magic2;
        /// <summary>0x048, Blocks per allocation group</summary>
        public readonly int blocks_per_ag;
        /// <summary>0x04C, 1 &lt;&lt; ag_shift == blocks_per_ag * block_size (byte size of AG)</summary>
        public readonly int ag_shift;
        /// <summary>0x050, Allocation groups in volume</summary>
        public readonly int num_ags;
        /// <summary>0x054, 0x434c454e if clean, 0x44495254 if dirty</summary>
        public readonly uint flags;
        /// <summary>0x058, Allocation group of journal</summary>
        public readonly int log_blocks_ag;
        /// <summary>0x05C, Start block of journal, inside ag</summary>
        public readonly ushort log_blocks_start;
        /// <summary>0x05E, Length in blocks of journal, inside ag</summary>
        public readonly ushort log_blocks_len;
        /// <summary>0x060, Start of journal</summary>
        public readonly long log_start;
        /// <summary>0x068, Valid block logs</summary>
        public readonly int log_valid_blocks;
        /// <summary>0x06C, Log size</summary>
        public readonly int log_size;
        /// <summary>0x070, 0x15B6830E</summary>
        public readonly uint magic3;
        /// <summary>0x074, Allocation group where root folder's i-node resides</summary>
        public readonly int root_dir_ag;
        /// <summary>0x078, Start in ag of root folder's i-node</summary>
        public readonly ushort root_dir_start;
        /// <summary>0x07A, As this is part of inode_addr, this is 1</summary>
        public readonly ushort root_dir_len;
        /// <summary>0x07C, Allocation group where pending-delete-files' i-node resides</summary>
        public readonly int deleted_ag;
        /// <summary>0x080, Start in ag of pending-delete-files' i-node</summary>
        public readonly ushort deleted_start;
        /// <summary>0x082, As this is part of inode_addr, this is 1</summary>
        public readonly ushort deleted_len;
        /// <summary>0x084, Allocation group where indices' i-node resides</summary>
        public readonly int indices_ag;
        /// <summary>0x088, Start in ag of indices' i-node</summary>
        public readonly ushort indices_start;
        /// <summary>0x08A, As this is part of inode_addr, this is 1</summary>
        public readonly ushort indices_len;
        /// <summary>0x08C, Size of bootloader</summary>
        public readonly int boot_size;
    }

#endregion

#region Nested type: BlockRun

    /// <summary>Block run - a pointer to a contiguous range of blocks</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BlockRun
    {
        /// <summary>Allocation group number</summary>
        public readonly int group;
        /// <summary>Start block within the allocation group</summary>
        public readonly ushort start;
        /// <summary>Number of contiguous blocks</summary>
        public readonly ushort len;
    }

#endregion

#region Nested type: DataStream

    /// <summary>Data stream of an AFS file</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DataStream
    {
        /// <summary>Direct block runs (12 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly BlockRun[] direct;
        /// <summary>Maximum number of bytes in direct range</summary>
        public readonly long max_direct_range;
        /// <summary>Indirect block run</summary>
        public readonly BlockRun indirect;
        /// <summary>Maximum number of bytes in direct and indirect ranges</summary>
        public readonly long max_indirect_range;
        /// <summary>Double indirect block run</summary>
        public readonly BlockRun double_indirect;
        /// <summary>Maximum number of bytes in all ranges</summary>
        public readonly long max_double_indirect_range;
        /// <summary>Size of the data stream in bytes</summary>
        public readonly long size;
    }

#endregion

#region Nested type: Inode

    /// <summary>AFS inode structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Inode
    {
        /// <summary>0x000, Magic number (0x64358428)</summary>
        public readonly uint magic1;
        /// <summary>0x004, Block run pointing to this inode</summary>
        public readonly BlockRun inode_num;
        /// <summary>0x00C, User ID of owner</summary>
        public readonly int uid;
        /// <summary>0x010, Group ID of owner</summary>
        public readonly int gid;
        /// <summary>0x014, File mode (permissions and type)</summary>
        public readonly int mode;
        /// <summary>0x018, Inode flags</summary>
        public readonly int flags;
        /// <summary>0x01C, Number of hard links</summary>
        public readonly int link_count;
        /// <summary>0x020, Creation time in microseconds since epoch</summary>
        public readonly long create_time;
        /// <summary>0x028, Last modification time in microseconds since epoch</summary>
        public readonly long modified_time;
        /// <summary>0x030, Block run of parent directory</summary>
        public readonly BlockRun parent;
        /// <summary>0x038, Block run of attribute directory</summary>
        public readonly BlockRun attrib_dir;
        /// <summary>0x040, Index type (for index files only)</summary>
        public readonly uint index_type;
        /// <summary>0x044, Size of this inode structure</summary>
        public readonly int inode_size;
        /// <summary>0x048, Pointer to VNode (in-memory only, not on disk)</summary>
        public readonly long vnode_ptr;
        /// <summary>0x050, Data stream</summary>
        public readonly DataStream data;
        /// <summary>Padding</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly int[] pad;
        /// <summary>Start of small data (extended attributes stored in inode)</summary>
        public readonly int small_data_start;
    }

#endregion

#region Nested type: SmallData

    /// <summary>Small data entry (extended attribute stored in inode)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SmallData
    {
        /// <summary>Attribute type</summary>
        public readonly uint type;
        /// <summary>Size of the attribute name</summary>
        public readonly ushort name_size;
        /// <summary>Size of the attribute data</summary>
        public readonly ushort data_size;
    }

#endregion

#region Nested type: BTreeHeader

    /// <summary>B+tree header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BTreeHeader
    {
        /// <summary>Magic number (0x65768995)</summary>
        public readonly uint magic;
        /// <summary>Block number of root node</summary>
        public readonly long root;
        /// <summary>Depth of the tree</summary>
        public readonly int tree_depth;
        /// <summary>Block number of last node</summary>
        public readonly long last_node;
        /// <summary>Block number of first free node in freelist</summary>
        public readonly long first_free;
    }

#endregion

#region Nested type: BNode

    /// <summary>B+tree node</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BNode
    {
        /// <summary>Block number of left sibling</summary>
        public readonly long left;
        /// <summary>Block number of right sibling</summary>
        public readonly long right;
        /// <summary>Block number of overflow node (for keys >= last key)</summary>
        public readonly long overflow;
        /// <summary>Number of keys in this node</summary>
        public readonly int key_count;
        /// <summary>Total size of all keys in bytes</summary>
        public readonly int total_key_size;

        // Followed by variable-length key data, key indices, and values
    }

#endregion
}