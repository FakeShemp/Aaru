// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser4 filesystem plugin
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Reiser v4 filesystem</summary>
public sealed partial class Reiser4
{
    const string MODULE_NAME = "Reiser4";

    const uint   REISER4_SUPER_OFFSET = 0x10000;
    const string FS_TYPE              = "reiser4";

    // Format40 super block magic
    const string FORMAT40_MAGIC = "ReIsEr40FoRmAt";

    // Root directory key components (from disk_format40.c)
    const ulong FORMAT40_ROOT_LOCALITY = 41;
    const ulong FORMAT40_ROOT_OBJECTID = 42;

    // Key minor localities (key_minor_locality enum)
    const ulong KEY_FILE_NAME_MINOR = 0;
    const ulong KEY_SD_MINOR        = 1;
    const ulong KEY_ATTR_NAME_MINOR = 2;
    const ulong KEY_ATTR_BODY_MINOR = 3;
    const ulong KEY_BODY_MINOR      = 4;

    // Key field masks
    const ulong KEY_LOCALITY_MASK = 0xFFFFFFFFFFFFFFF0UL;
    const ulong KEY_TYPE_MASK     = 0x000000000000000FUL;
    const ulong KEY_BAND_MASK     = 0xF000000000000000UL;
    const ulong KEY_OBJECTID_MASK = 0x0FFFFFFFFFFFFFFFUL;
    const ulong KEY_FULLOID_MASK  = 0xFFFFFFFFFFFFFFFFUL;
    const ulong KEY_OFFSET_MASK   = 0xFFFFFFFFFFFFFFFFUL;

    // Key field shifts
    const int KEY_LOCALITY_SHIFT = 4;
    const int KEY_TYPE_SHIFT     = 0;
    const int KEY_BAND_SHIFT     = 60;
    const int KEY_OBJECTID_SHIFT = 0;

    // Item plugin IDs (from plugin/plugin.h and plugin/item/item.h)
    const ushort STATIC_STAT_DATA_ID   = 0;
    const ushort SIMPLE_DIR_ENTRY_ID   = 1;
    const ushort COMPOUND_DIR_ENTRY_ID = 2;
    const ushort NODE_POINTER_ID       = 3;
    const ushort EXTENT_POINTER_ID     = 5;
    const ushort FORMATTING_ID         = 6; // tail item
    const ushort CTAIL_ID              = 7;

    // Node40 magic value: "R4FS" as uint32
    const uint REISER4_NODE40_MAGIC = 0x52344653;

    // Format40 flags
    const ulong FORMAT40_LARGE_KEYS = 1;

    // Maximum tree height
    const int REISER4_MAX_TREE_HEIGHT = 8;

    // Leaf node level
    const byte LEAF_LEVEL = 1;

    // Twig node level (where extent items live)
    const byte TWIG_LEVEL = 2;

    // Size of an on-disk extent descriptor (start + width, each 8 bytes)
    const int EXTENT_SIZE = 16;

    // Longname marker bit (in ordering/objectid field)
    const ulong LONGNAME_MARK  = 0x0100000000000000UL;
    const ulong FIBRATION_MASK = 0xFF00000000000000UL;

    // POSIX inode mode masks
    const ushort S_IFMT   = 0xF000;
    const ushort S_IFSOCK = 0xC000;
    const ushort S_IFLNK  = 0xA000;
    const ushort S_IFREG  = 0x8000;
    const ushort S_IFBLK  = 0x6000;
    const ushort S_IFDIR  = 0x4000;
    const ushort S_IFCHR  = 0x2000;
    const ushort S_IFIFO  = 0x1000;

    // Stat-data extension bits
    const ushort SD_LIGHT_WEIGHT = 1 << 0;
    const ushort SD_UNIX         = 1 << 1;
    const ushort SD_LARGE_TIMES  = 1 << 2;
    const ushort SD_SYMLINK      = 1 << 3;
    const ushort SD_PLUGIN       = 1 << 4;
    const ushort SD_FLAGS        = 1 << 5;

    // Maximum filename length for StatFs
    const int REISER4_MAX_NAME = 255;

    // Master super block magic
    readonly byte[] _magic = "ReIsEr4\0\0\0\0\0\0\0\0\0"u8.ToArray();
}