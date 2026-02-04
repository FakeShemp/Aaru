// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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
public sealed partial class QNX6
{
    /// <summary>Superblock size (always 512 bytes)</summary>
    const uint QNX6_SUPERBLOCK_SIZE = 0x200;
    /// <summary>Superblock area reserved size</summary>
    const uint QNX6_SUPERBLOCK_AREA = 0x1000;
    /// <summary>Boot block size</summary>
    const uint QNX6_BOOT_BLOCKS_SIZE = 0x2000;
    /// <summary>For Info.cs compatibility</summary>
    const uint QNX6_SUPER_BLOCK_SIZE = 0x1000;
    /// <summary>QNX6 magic number</summary>
    const uint QNX6_MAGIC = 0x68191122;
    /// <summary>Root inode number</summary>
    const uint QNX6_ROOT_INO = 1;
    /// <summary>Directory entry size</summary>
    const int QNX6_DIR_ENTRY_SIZE = 0x20;
    /// <summary>Inode size</summary>
    const int QNX6_INODE_SIZE = 0x80;
    /// <summary>Inode size bits</summary>
    const int QNX6_INODE_SIZE_BITS = 7;
    /// <summary>Number of direct pointers in inode/superblock</summary>
    const int QNX6_NO_DIRECT_POINTERS = 16;
    /// <summary>Maximum indirect pointer levels</summary>
    const int QNX6_PTR_MAX_LEVELS = 5;
    /// <summary>Short filename max length</summary>
    const int QNX6_SHORT_NAME_MAX = 27;
    /// <summary>Long filename max length</summary>
    const int QNX6_LONG_NAME_MAX = 510;

    /// <summary>File status: directory</summary>
    const byte QNX6_FILE_DIRECTORY = 0x01;
    /// <summary>File status: deleted</summary>
    const byte QNX6_FILE_DELETED = 0x02;
    /// <summary>File status: normal file</summary>
    const byte QNX6_FILE_NORMAL = 0x03;

    const string FS_TYPE = "qnx6";
}