// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class QNX4
{
    const string FS_TYPE = "qnx4";

    /// <summary>Root inode number</summary>
    const int QNX4_ROOT_INO = 1;

    /// <summary>Maximum extents per extent block</summary>
    const int QNX4_MAX_XTNTS_PER_XBLK = 60;

    // di_status flags
    /// <summary>File is in use</summary>
    const byte QNX4_FILE_USED = 0x01;
    /// <summary>File has been modified</summary>
    const byte QNX4_FILE_MODIFIED = 0x02;
    /// <summary>File is busy</summary>
    const byte QNX4_FILE_BUSY = 0x04;
    /// <summary>Entry is a link</summary>
    const byte QNX4_FILE_LINK = 0x08;
    /// <summary>Entry is an inode</summary>
    const byte QNX4_FILE_INODE = 0x10;
    /// <summary>Filesystem is clean</summary>
    const byte QNX4_FILE_FSYSCLEAN = 0x20;

    /// <summary>Inode map slots</summary>
    const int QNX4_I_MAP_SLOTS = 8;
    /// <summary>Zone map slots</summary>
    const int QNX4_Z_MAP_SLOTS = 64;

    /// <summary>Clean filesystem flag</summary>
    const int QNX4_VALID_FS = 0x0001;
    /// <summary>Filesystem has errors flag</summary>
    const int QNX4_ERROR_FS = 0x0002;

    /// <summary>Block size (512 bytes)</summary>
    const int QNX4_BLOCK_SIZE = 0x200;
    /// <summary>Block size shift (9 bits)</summary>
    const int QNX4_BLOCK_SIZE_BITS = 9;

    /// <summary>Directory entry size (64 bytes)</summary>
    const int QNX4_DIR_ENTRY_SIZE = 0x040;
    /// <summary>Directory entry size shift (6 bits)</summary>
    const int QNX4_DIR_ENTRY_SIZE_BITS = 6;

    /// <summary>Extent block size (512 bytes)</summary>
    const int QNX4_XBLK_ENTRY_SIZE = 0x200;

    /// <summary>Inodes per block (512 / 64 = 8)</summary>
    const int QNX4_INODES_PER_BLOCK = 0x08;

    /// <summary>Maximum short filename length</summary>
    const int QNX4_SHORT_NAME_MAX = 16;
    /// <summary>Maximum filename length (for links)</summary>
    const int QNX4_NAME_MAX = 48;

    readonly byte[] _rootDirFname = "/\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"u8.ToArray();
}