// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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

// Information from Practical Filesystem Design, ISBN 1-55860-497-9
/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class BeFS
{
#region Nested type: SuperBlock

    /// <summary>Be superblock</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>0x000, Volume name, 32 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] name;
        /// <summary>0x020, "BFS1", 0x42465331</summary>
        public uint magic1;
        /// <summary>0x024, "BIGE", 0x42494745</summary>
        public uint fs_byte_order;
        /// <summary>0x028, Bytes per block</summary>
        public uint block_size;
        /// <summary>0x02C, 1 &lt;&lt; block_shift == block_size</summary>
        public uint block_shift;
        /// <summary>0x030, Blocks in volume</summary>
        public long num_blocks;
        /// <summary>0x038, Used blocks in volume</summary>
        public long used_blocks;
        /// <summary>0x040, Bytes per inode</summary>
        public int inode_size;
        /// <summary>0x044, 0xDD121031</summary>
        public uint magic2;
        /// <summary>0x048, Blocks per allocation group</summary>
        public int blocks_per_ag;
        /// <summary>0x04C, 1 &lt;&lt; ag_shift == blocks_per_ag</summary>
        public int ag_shift;
        /// <summary>0x050, Allocation groups in volume</summary>
        public int num_ags;
        /// <summary>0x054, 0x434c454e if clean, 0x44495254 if dirty</summary>
        public uint flags;
        /// <summary>0x058, Allocation group of journal</summary>
        public block_run log_blocks;
        /// <summary>0x060, Start of journal</summary>
        public long log_start;
        /// <summary>0x068, End of journal</summary>
        public long log_end;
        /// <summary>0x070, 0x15B6830E</summary>
        public uint magic3;
        /// <summary>0x074, Allocation group where root folder's i-node resides</summary>
        public block_run root_dir;
        /// <summary>0x07C, Allocation group where indices' i-node resides</summary>
        public block_run indices;
    }

#endregion

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct block_run
    {
        /// <summary>Starting allocation group</summary>
        public uint allocation_group;
        /// <summary>Starting block inside allocation group</summary>
        public ushort start;
        /// <summary>Number of blocks</summary>
        public ushort len;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct bfs_inode
    {
        public int         magic1;
        public block_run   inode_num;
        public int         uid;
        public int         gid;
        public int         mode;
        public InodeFlags  flags;
        public ulong       create_time;
        public ulong       last_modified_time;
        public block_run   parent;
        public block_run   attributes;
        public uint        type;
        public int         node_size;
        public ulong       etc;
        public data_stream data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] pad;
        public small_data small_data;
    }

    enum InodeFlags
    {
        INODE_IN_USE      = 0x00000001,
        ATTR_INODE        = 0x00000004,
        INODE_LOGGED      = 0x00000008,
        INODE_DELETED     = 0x00000010,
        PERMANENT_FLAGS   = 0x0000ffff,
        INODE_NO_CACHE    = 0x00010000,
        INODE_WAS_WRITTEN = 0x00020000,
        NO_TRANSACTION    = 0x00040000
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct data_stream
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NUM_DIRECT_BLOCKS)]
        public block_run[] direct;
        public long      max_direct_range;
        public block_run indirect;
        public long      max_indirect_range;
        public block_run double_indirect;
        public long      max_double_indirect_range;
        public long      size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct small_data
    {
        public uint   type;
        public ushort name_size;
        public ushort data_size;

        // Followed by name[name_size] and data[data_size]
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct bt_header
    {
        public long magic;
        public int  node_size;
        public int  max_number_of_levels;
        public int  data_type;
        public long node_root_pointer;
        public long free_node_pointer;
        public long maximum_size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct bt_node_hdr
    {
        public long  left_link;
        public long  right_link;
        public long  overflow_link;
        public short node_keys;
        public short keys_length;
    }
}