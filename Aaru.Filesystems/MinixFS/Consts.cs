// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class MinixFS
{
#region Module name

    const string MODULE_NAME = "MinixFS";

#endregion

#region Magic numbers

    /// <summary>Minix v1, 14 char filenames</summary>
    const ushort MINIX_MAGIC = 0x137F;

    /// <summary>Minix v1, 30 char filenames</summary>
    const ushort MINIX_MAGIC2 = 0x138F;

    /// <summary>Minix v2, 14 char filenames</summary>
    const ushort MINIX2_MAGIC = 0x2468;

    /// <summary>Minix v2, 30 char filenames</summary>
    const ushort MINIX2_MAGIC2 = 0x2478;

    /// <summary>Minix v3, 60 char filenames</summary>
    const ushort MINIX3_MAGIC = 0x4D5A;

    // Byteswapped magic numbers
    /// <summary>Minix v1, 14 char filenames (byteswapped)</summary>
    const ushort MINIX_CIGAM = 0x7F13;

    /// <summary>Minix v1, 30 char filenames (byteswapped)</summary>
    const ushort MINIX_CIGAM2 = 0x8F13;

    /// <summary>Minix v2, 14 char filenames (byteswapped)</summary>
    const ushort MINIX2_CIGAM = 0x6824;

    /// <summary>Minix v2, 30 char filenames (byteswapped)</summary>
    const ushort MINIX2_CIGAM2 = 0x7824;

    /// <summary>Minix v3, 60 char filenames (byteswapped)</summary>
    const ushort MINIX3_CIGAM = 0x5A4D;

#endregion

#region Filesystem type identifiers

    const string FS_TYPE_V1 = "minix";
    const string FS_TYPE_V2 = "minix2";
    const string FS_TYPE_V3 = "minix3";

#endregion

#region Filename sizes

    /// <summary>Maximum filename length for Minix v1 (short names)</summary>
    const int V1_NAME_MAX = 14;

    /// <summary>Maximum filename length for Minix v1 (long names)</summary>
    const int V1_NAME_MAX_LONG = 30;

    /// <summary>Maximum filename length for Minix v3</summary>
    const int V3_NAME_MAX = 60;

#endregion

#region Inode zone counts

    /// <summary>Number of direct zone numbers in a V1 inode</summary>
    const int V1_NR_DZONES = 7;

    /// <summary>Total number of zone numbers in a V1 inode</summary>
    const int V1_NR_TZONES = 9;

    /// <summary>Number of direct zone numbers in a V2 inode</summary>
    const int V2_NR_DZONES = 7;

    /// <summary>Total number of zone numbers in a V2 inode</summary>
    const int V2_NR_TZONES = 10;

#endregion

#region Block and offset constants

    /// <summary>Boot block number (block 0)</summary>
    const int BOOT_BLOCK = 0;

    /// <summary>Superblock offset in bytes from start of partition</summary>
    const int SUPER_BLOCK_BYTES = 1024;

    /// <summary>First block of filesystem data (after superblock)</summary>
    const int START_BLOCK = 2;

    /// <summary>Default block size for V1/V2 filesystems</summary>
    const int V1_V2_BLOCK_SIZE = 1024;

    /// <summary>Minimum block size for V3 filesystems</summary>
    const int V3_MIN_BLOCK_SIZE = 1024;

    /// <summary>Maximum block size for V3 filesystems</summary>
    const int V3_MAX_BLOCK_SIZE = 4096;

#endregion

#region Inode constants

    /// <summary>Root directory inode number</summary>
    const int ROOT_INODE = 1;

    /// <summary>Size of a V1 disk inode in bytes</summary>
    const int V1_INODE_SIZE = 32;

    /// <summary>Size of a V2/V3 disk inode in bytes</summary>
    const int V2_INODE_SIZE = 64;

#endregion

#region Directory entry sizes

    /// <summary>Size of V1 directory entry with 14-char names (2 + 14 = 16 bytes)</summary>
    const int V1_DIR_ENTRY_SIZE = 16;

    /// <summary>Size of V1 directory entry with 30-char names (2 + 30 = 32 bytes)</summary>
    const int V1_DIR_ENTRY_SIZE_LONG = 32;

    /// <summary>Size of V2 directory entry with 14-char names (2 + 14 = 16 bytes)</summary>
    const int V2_DIR_ENTRY_SIZE = 16;

    /// <summary>Size of V2 directory entry with 30-char names (2 + 30 = 32 bytes)</summary>
    const int V2_DIR_ENTRY_SIZE_LONG = 32;

    /// <summary>Size of V3 directory entry (4 + 60 = 64 bytes)</summary>
    const int V3_DIR_ENTRY_SIZE = 64;

#endregion

#region Bitmap constants

    /// <summary>Operating on the inode bit map</summary>
    const int IMAP = 0;

    /// <summary>Operating on the zone bit map</summary>
    const int ZMAP = 1;

#endregion
}