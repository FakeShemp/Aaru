// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
{
    /// <summary>Identifier for Squash (little-endian)</summary>
    const uint SQUASH_MAGIC = 0x73717368;
    /// <summary>Identifier for Squash (big-endian)</summary>
    const uint SQUASH_CIGAM = 0x68737173;

    const string FS_TYPE = "squashfs";

    /// <summary>Major version number</summary>
    const ushort SQUASHFS_MAJOR = 4;
    /// <summary>Minor version number</summary>
    const ushort SQUASHFS_MINOR = 0;
    /// <summary>Start offset of the filesystem</summary>
    const int SQUASHFS_START = 0;

    /// <summary>Size of metadata (inode and directory) blocks</summary>
    const int SQUASHFS_METADATA_SIZE = 8192;
    /// <summary>Block offset</summary>
    const int SQUASHFS_BLOCK_OFFSET = 2;

    /// <summary>Maximum file size</summary>
    const int SQUASHFS_FILE_MAX_SIZE = 1048576;
    /// <summary>Maximum file size log</summary>
    const int SQUASHFS_FILE_MAX_LOG = 20;

    /// <summary>Maximum length of filename</summary>
    const int SQUASHFS_NAME_LEN = 256;

    /// <summary>Maximum value for directory header count</summary>
    const int SQUASHFS_DIR_COUNT = 256;

    /// <summary>Invalid fragment indicator</summary>
    const uint SQUASHFS_INVALID_FRAG = 0xFFFFFFFF;
    /// <summary>Invalid xattr indicator</summary>
    const uint SQUASHFS_INVALID_XATTR = 0xFFFFFFFF;
    /// <summary>Invalid block indicator</summary>
    const long SQUASHFS_INVALID_BLK = -1;

    /// <summary>Bit indicating block is compressed (metadata)</summary>
    const int SQUASHFS_COMPRESSED_BIT = 1 << 15;
    /// <summary>Bit indicating block is compressed (data block)</summary>
    const int SQUASHFS_COMPRESSED_BIT_BLOCK = 1 << 24;

    /// <summary>Maximum directory type value stored in directory entry</summary>
    const int SQUASHFS_MAX_DIR_TYPE = 7;

    /// <summary>Cached data blocks constant</summary>
    const int SQUASHFS_CACHED_BLKS = 8;

    /// <summary>Meta index entries</summary>
    const int SQUASHFS_META_ENTRIES = 127;
    /// <summary>Meta index slots</summary>
    const int SQUASHFS_META_SLOTS = 8;
    /// <summary>Scan indexes</summary>
    const int SQUASHFS_SCAN_INDEXES = 1024;
}