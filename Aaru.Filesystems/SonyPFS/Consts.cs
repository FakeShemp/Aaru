// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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

public partial class SonyPFS
{
    /// <summary>"PFS\0" — PFS superblock magic.</summary>
    const uint PFS_SUPER_MAGIC = 0x50465300;
    /// <summary>"PFSL" — PFS journal/log magic.</summary>
    const uint PFS_JOURNAL_MAGIC = 0x5046534C;
    /// <summary>"SEGD" — Segment descriptor direct (inode) magic.</summary>
    const uint PFS_SEGD_MAGIC = 0x53454744;
    /// <summary>"SEGI" — Segment descriptor indirect magic.</summary>
    const uint PFS_SEGI_MAGIC = 0x53454749;

    /// <summary>PFS block size in bytes (8 KiB).</summary>
    const uint PFS_BLOCK_SIZE = 0x2000;
    /// <summary>PFS metadata structure size in bytes.</summary>
    const uint PFS_META_SIZE = 1024;
    /// <summary>Maximum number of block info entries in an inode data array.</summary>
    const int PFS_INODE_MAX_BLOCKS = 114;
    /// <summary>Maximum file name length.</summary>
    const int PFS_NAME_LEN = 255;
    /// <summary>Maximum number of sub-partitions.</summary>
    const int PFS_MAX_SUBPARTS = 64;
    /// <summary>Current PFS format version.</summary>
    const uint PFS_FORMAT_VERSION = 3;

    /// <summary>Superblock sector offset from partition data start.</summary>
    const uint PFS_SUPER_SECTOR = 0;
    /// <summary>Backup superblock sector (1 sector after superblock).</summary>
    const uint PFS_SUPER_BACKUP_SECTOR = 1;

    /// <summary>Filesystem type identifier string.</summary>
    const string FS_TYPE = "pfs";

    /// <summary>Number of bitmap bits per chunk (1024-byte bitmap block).</summary>
    const uint PFS_BITS_PER_BITMAP_CHUNK = 8192;

    /// <summary>Maximum number of journal log entries.</summary>
    const int PFS_JOURNAL_MAX_ENTRIES = 127;

    /// <summary>
    ///     Number of 512-byte sectors reserved at the start of an APA main partition.
    ///     PFS block numbers are relative to the raw partition start, but partition.Start
    ///     already skips this reserved area, so we subtract it when converting PFS addresses.
    /// </summary>
    const ulong PFS_APA_RESERVED_SECTORS = 8192;
}