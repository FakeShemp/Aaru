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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

// Information from the Linux kernel
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
        /// <summary>0x02, zones on volume</summary>
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
        /// <summary>0x12, filesystem state</summary>
        public ushort s_state;
        /// <summary>0x14, number of zones</summary>
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
        /// <summary>0x02, old zones on volume</summary>
        public ushort s_nzones;
        /// <summary>0x06, blocks on inode map</summary>
        public ushort s_imap_blocks;
        /// <summary>0x08, blocks on zone map</summary>
        public ushort s_zmap_blocks;
        /// <summary>0x0A, first data zone</summary>
        public ushort s_firstdatazone;
        /// <summary>0x0C, log2 of blocks/zone</summary>
        public ushort s_log_zone_size;
        /// <summary>0x0E, padding</summary>
        public ushort s_pad1;
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
}