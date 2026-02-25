// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

// ReSharper disable UnusedMember.Local

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    const ushort NILFS2_MAGIC        = 0x3434;
    const uint   NILFS2_SUPER_OFFSET = 1024;

    const string FS_TYPE = "nilfs2";

    /// <summary>Number of entries in inode block mapping array</summary>
    const int NILFS2_INODE_BMAP_SIZE = 7;

    /// <summary>Minimum inode size in bytes</summary>
    const int NILFS2_MIN_INODE_SIZE = 128;

    /// <summary>Default maximum mount count</summary>
    const int NILFS2_DFL_MAX_MNT_COUNT = 50;

    /// <summary>Maximum filename length</summary>
    const int NILFS2_NAME_LEN = 255;

    /// <summary>Minimum block size</summary>
    const int NILFS2_MIN_BLOCK_SIZE = 1024;

    /// <summary>Maximum block size</summary>
    const int NILFS2_MAX_BLOCK_SIZE = 65536;

    /// <summary>Segment summary magic number</summary>
    const uint NILFS2_SEGSUM_MAGIC = 0x1EAFfa11;

    /// <summary>Minimum number of blocks in a full segment</summary>
    const int NILFS2_SEG_MIN_BLOCKS = 16;

    /// <summary>Minimum number of blocks in a partial segment</summary>
    const int NILFS2_PSEG_MIN_BLOCKS = 2;

    /// <summary>Minimum number of reserved segments</summary>
    const int NILFS2_MIN_NRSVSEGS = 8;

    /// <summary>Maximal count of links to a file</summary>
    const int NILFS2_LINK_MAX = 32000;

    /// <summary>Directory entry padding alignment (must be a multiple of 8)</summary>
    const int NILFS2_DIR_PAD = 8;

    /// <summary>Minimum DAT entry size</summary>
    const int NILFS2_MIN_DAT_ENTRY_SIZE = 32;

    /// <summary>Minimum segment usage size</summary>
    const int NILFS2_MIN_SEGMENT_USAGE_SIZE = 16;

    /// <summary>Current major revision</summary>
    const int NILFS2_CURRENT_REV = 2;

    /// <summary>Current minor revision</summary>
    const int NILFS2_MINOR_REV = 0;

    /// <summary>Minimum supported revision</summary>
    const int NILFS2_MIN_SUPP_REV = 2;

    /// <summary>B-tree node root flag</summary>
    const byte NILFS2_BTREE_NODE_ROOT = 0x01;

    /// <summary>B-tree data level</summary>
    const int NILFS2_BTREE_LEVEL_DATA = 0;

    /// <summary>B-tree minimum node level</summary>
    const int NILFS2_BTREE_LEVEL_NODE_MIN = 1;

    /// <summary>B-tree maximum level (exclusive)</summary>
    const int NILFS2_BTREE_LEVEL_MAX = 14;

    /// <summary>Read-only compatible feature: block count</summary>
    const ulong NILFS2_FEATURE_COMPAT_RO_BLOCK_COUNT = 0x00000001UL;

    // Special inode numbers

    /// <summary>Root file inode</summary>
    const int NILFS2_ROOT_INO = 2;

    /// <summary>DAT file inode</summary>
    const int NILFS2_DAT_INO = 3;

    /// <summary>Checkpoint file inode</summary>
    const int NILFS2_CPFILE_INO = 4;

    /// <summary>Segment usage file inode</summary>
    const int NILFS2_SUFILE_INO = 5;

    /// <summary>Inode file inode</summary>
    const int NILFS2_IFILE_INO = 6;

    /// <summary>Atime file (reserved)</summary>
    const int NILFS2_ATIME_INO = 7;

    /// <summary>Extended attribute file (reserved)</summary>
    const int NILFS2_XATTR_INO = 8;

    /// <summary>Sketch file inode</summary>
    const int NILFS2_SKETCH_INO = 10;

    /// <summary>First user's file inode number</summary>
    const int NILFS2_USER_INO = 11;

    // OS codes

    /// <summary>Linux operating system code</summary>
    const int NILFS2_OS_LINUX = 0;
}