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
// Copyright © 2011-2025 Natalia Portillo
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
        public int log_blocks_ag;
        /// <summary>0x05C, Start block of journal, inside ag</summary>
        public ushort log_blocks_start;
        /// <summary>0x05E, Length in blocks of journal, inside ag</summary>
        public ushort log_blocks_len;
        /// <summary>0x060, Start of journal</summary>
        public long log_start;
        /// <summary>0x068, End of journal</summary>
        public long log_end;
        /// <summary>0x070, 0x15B6830E</summary>
        public uint magic3;
        /// <summary>0x074, Allocation group where root folder's i-node resides</summary>
        public int root_dir_ag;
        /// <summary>0x078, Start in ag of root folder's i-node</summary>
        public ushort root_dir_start;
        /// <summary>0x07A, As this is part of inode_addr, this is 1</summary>
        public ushort root_dir_len;
        /// <summary>0x07C, Allocation group where indices' i-node resides</summary>
        public int indices_ag;
        /// <summary>0x080, Start in ag of indices' i-node</summary>
        public ushort indices_start;
        /// <summary>0x082, As this is part of inode_addr, this is 1</summary>
        public ushort indices_len;
    }

#endregion
}