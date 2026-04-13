// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

public partial class SonyPFS
{
    /// <summary>Block number/count pair, used in inodes to describe contiguous zone runs.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct BlockInfo
    {
        /// <summary>0x00, Block/zone number.</summary>
        public uint number;
        /// <summary>0x04, Sub-partition index (0 = main).</summary>
        public ushort subpart;
        /// <summary>0x06, Number of contiguous zones.</summary>
        public ushort count;
    }

    /// <summary>Date/time descriptor, 8 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct DateTime
    {
        /// <summary>0x00, Unused.</summary>
        public byte unused;
        /// <summary>0x01, Seconds.</summary>
        public byte sec;
        /// <summary>0x02, Minutes.</summary>
        public byte min;
        /// <summary>0x03, Hours.</summary>
        public byte hour;
        /// <summary>0x04, Day of month.</summary>
        public byte day;
        /// <summary>0x05, Month.</summary>
        public byte month;
        /// <summary>0x06, Year.</summary>
        public ushort year;
    }

    /// <summary>
    ///     PFS superblock structure. Located at sector 0 relative to partition data start (sector 8192 from APA partition
    ///     start).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct SuperBlock
    {
        /// <summary>0x00, Magic number, <see cref="PFS_SUPER_MAGIC" />.</summary>
        public uint magic;
        /// <summary>0x04, Format version.</summary>
        public uint version;
        /// <summary>0x08, Module version that last modified the filesystem.</summary>
        public uint modver;
        /// <summary>0x0C, Filesystem check status flags.</summary>
        public uint pfsFsckStat;
        /// <summary>0x10, Zone size in bytes (power of 2, 2048..131072).</summary>
        public uint zone_size;
        /// <summary>0x14, Number of sub-partitions attached to the filesystem.</summary>
        public uint num_subs;
        /// <summary>0x18, Block info for the metadata journal/log.</summary>
        public BlockInfo log;
        /// <summary>0x20, Block info for the root directory inode.</summary>
        public BlockInfo root;
    }

    /// <summary>PFS inode structure, 1024 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct Inode
    {
        /// <summary>0x000, Checksum: sum of all other 32-bit words in the inode.</summary>
        public uint checksum;
        /// <summary>0x004, Magic number (<see cref="PFS_SEGD_MAGIC" /> or <see cref="PFS_SEGI_MAGIC" />).</summary>
        public uint magic;
        /// <summary>0x008, Start block of this inode.</summary>
        public BlockInfo inode_block;
        /// <summary>0x010, Next segment descriptor inode.</summary>
        public BlockInfo next_segment;
        /// <summary>0x018, Last segment descriptor inode.</summary>
        public BlockInfo last_segment;
        /// <summary>0x020, Unused.</summary>
        public BlockInfo unused;
        /// <summary>0x028, Data block info array, 114 entries.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = PFS_INODE_MAX_BLOCKS)]
        public BlockInfo[] data;
        /// <summary>0x3B8, File mode (type and permissions).</summary>
        public ushort mode;
        /// <summary>0x3BA, File attributes.</summary>
        public ushort attr;
        /// <summary>0x3BC, User ID.</summary>
        public ushort uid;
        /// <summary>0x3BE, Group ID.</summary>
        public ushort gid;
        /// <summary>0x3C0, Access time.</summary>
        public DateTime atime;
        /// <summary>0x3C8, Creation time.</summary>
        public DateTime ctime;
        /// <summary>0x3D0, Modification time.</summary>
        public DateTime mtime;
        /// <summary>0x3D8, File size in bytes.</summary>
        public ulong size;
        /// <summary>0x3E0, Number of blocks/zones used by file.</summary>
        public uint number_blocks;
        /// <summary>0x3E4, Number of used entries in data array.</summary>
        public uint number_data;
        /// <summary>0x3E8, Number of indirect segment descriptors.</summary>
        public uint number_segdesg;
        /// <summary>0x3EC, Sub-partition of this inode.</summary>
        public uint subpart;
        /// <summary>0x3F0, Reserved, 16 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] reserved;
    }

    /// <summary>PFS directory entry.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct DirectoryEntry
    {
        /// <summary>0x00, Inode number.</summary>
        public uint inode;
        /// <summary>0x04, Sub-partition index.</summary>
        public byte sub;
        /// <summary>0x05, Path/name length.</summary>
        public byte pLen;
        /// <summary>0x06, Allocated length (lower 12 bits) and file type (upper 4 bits from FIO_S_IFMT).</summary>
        public ushort aLen;
        /// <summary>0x08, File/directory name, up to 504 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 504)]
        public byte[] path;
    }

    /// <summary>PFS attribute entry, stored in inode data blocks.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct AttributeEntry
    {
        /// <summary>0x00, Key length / offset into str for value.</summary>
        public byte kLen;
        /// <summary>0x01, Value length.</summary>
        public byte vLen;
        /// <summary>0x02, Allocated length == ((kLen + vLen + 7) &amp; ~3).</summary>
        public ushort aLen;
    }

    /// <summary>PFS journal/log header.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct JournalHeader
    {
        /// <summary>0x00, Magic number, <see cref="PFS_JOURNAL_MAGIC" />.</summary>
        public uint magic;
        /// <summary>0x04, Number of log entries.</summary>
        public ushort num;
        /// <summary>0x06, Checksum.</summary>
        public ushort checksum;
    }

    /// <summary>PFS journal log entry.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct JournalEntry
    {
        /// <summary>0x00, Block/sector for partition.</summary>
        public uint sector;
        /// <summary>0x04, Main (0) or sub (+1) partition.</summary>
        public ushort sub;
        /// <summary>0x06, Block/sector offset in journal area.</summary>
        public ushort logSector;
    }
}