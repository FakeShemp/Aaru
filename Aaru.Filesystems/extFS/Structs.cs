// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
/// <summary>Implements detection of the Linux extended filesystem</summary>

// ReSharper disable once InconsistentNaming
public sealed partial class extFS
{
#region Nested type: SuperBlock

    /// <summary>ext superblock</summary>
#pragma warning disable CS0649
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ext_super_block
    {
        /// <summary>0x000, inodes on volume</summary>
        public uint s_ninodes;
        /// <summary>0x004, zones on volume</summary>
        public uint s_nzones;
        /// <summary>0x008, first free block</summary>
        public uint s_firstfreeblock;
        /// <summary>0x00C, free blocks count</summary>
        public uint s_freeblockscount;
        /// <summary>0x010, first free inode</summary>
        public uint s_firstfreeinode;
        /// <summary>0x014, free inodes count</summary>
        public uint s_freeinodescount;
        /// <summary>0x018, first data zone</summary>
        public uint s_firstdatazone;
        /// <summary>0x01C, log zone size</summary>
        public uint s_log_zone_size;
        /// <summary>0x020, max zone size</summary>
        public uint s_max_size;
        /// <summary>0x024, reserved</summary>
        public uint s_reserved1;
        /// <summary>0x028, reserved</summary>
        public uint s_reserved2;
        /// <summary>0x02C, reserved</summary>
        public uint s_reserved3;
        /// <summary>0x030, reserved</summary>
        public uint s_reserved4;
        /// <summary>0x034, reserved</summary>
        public uint s_reserved5;
        /// <summary>0x038, 0x137D (little endian)</summary>
        public ushort s_magic;
    }
#pragma warning restore CS0649

#endregion

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ext_inode {
        public ushort i_mode;
        public ushort i_uid;
        public uint  i_size;
        public uint  i_time;
        public ushort i_gid;
        public ushort i_nlinks;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public uint[]  i_zone;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ext_free_inode {
        public uint count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public uint[] free;
        public uint next;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ext_free_block {
        public uint count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 254)]
        public uint[] free;
        public uint next;
    }

    struct ext_dir_entry {
        public uint  inode;
        public ushort rec_len;
        public ushort name_len;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EXT_NAME_LEN)]
        public byte[] name;
    }
}