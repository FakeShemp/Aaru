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
/// <summary>Implements detection of the squash filesystem</summary>
public sealed partial class Squash
{
#region Nested type: SuperBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        public uint   magic;
        public uint   inodes;
        public uint   mkfs_time;
        public uint   block_size;
        public uint   fragments;
        public ushort compression;
        public ushort block_log;
        public ushort flags;
        public ushort no_ids;
        public ushort s_major;
        public ushort s_minor;
        public ulong  root_inode;
        public ulong  bytes_used;
        public ulong  id_table_start;
        public ulong  xattr_id_table_start;
        public ulong  inode_table_start;
        public ulong  directory_table_start;
        public ulong  fragment_table_start;
        public ulong  lookup_table_start;
    }

#endregion
}