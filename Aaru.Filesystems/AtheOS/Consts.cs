// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection for the AtheOS filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class AtheOS
{
    // Little endian constants (that is, as read by .NET :p)
    const uint AFS_MAGIC1 = 0x41465331;
    const uint AFS_MAGIC2 = 0xDD121031;
    const uint AFS_MAGIC3 = 0x15B6830E;

    // Common constants
    const uint AFS_SUPERBLOCK_SIZE = 1024;
    const uint AFS_BOOTBLOCK_SIZE  = AFS_SUPERBLOCK_SIZE;

    // Data stream constants
    const int DIRECT_BLOCK_COUNT = 12;
    const int BLOCKS_PER_DI_RUN  = 4;

    // Maximum run length
    const int AFS_MAX_RUN_LENGTH = 65535;

    // Volume flags
    const uint AFS_FLAG_NONE      = 0;
    const uint AFS_FLAG_READ_ONLY = 1;
    const uint AFS_FLAG_CLEAN     = 0x434C454E; // "CLEN"
    const uint AFS_FLAG_DIRTY     = 0x44495254; // "DIRT"

    // Byte order
    const int BO_LITTLE_ENDIAN = 0;
    const int BO_BIG_ENDIAN    = 1;

    // Inode constants
    const uint INODE_MAGIC = 0x64358428;

    // Inode flags (permanent)
    const uint INF_USED       = 0x00000001;
    const uint INF_ATTRIBUTES = 0x00000002;
    const uint INF_LOGGED     = 0x00000004;

    const uint INF_PERMANENT_MASK = 0x0000FFFF;

    // Inode flags (transient)
    const uint INF_NO_CACHE       = 0x00010000;
    const uint INF_WAS_WRITTEN    = 0x00020000;
    const uint INF_NO_TRANSACTION = 0x00040000;
    const uint INF_NOT_IN_DELME   = 0x00080000;
    const uint INF_STAT_CHANGED   = 0x00100000;

    // B+tree constants
    const uint BTREE_MAGIC    = 0x65768995;
    const int  B_NODE_SIZE    = 1024;
    const int  B_MAX_KEY_SIZE = 256;

    // B+tree key types
    const int KEY_TYPE_NONE   = 0;
    const int KEY_TYPE_INT32  = 1;
    const int KEY_TYPE_INT64  = 2;
    const int KEY_TYPE_FLOAT  = 3;
    const int KEY_TYPE_DOUBLE = 4;
    const int KEY_TYPE_STRING = 5;

    // Null value for B+tree
    const long NULL_VAL = -1;

    const string FS_TYPE = "atheos";
}