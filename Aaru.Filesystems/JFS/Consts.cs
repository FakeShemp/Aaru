// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : IBM JFS filesystem plugin
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

public sealed partial class JFS
{
    const uint JFS_BOOT_BLOCKS_SIZE = 0x8000;
    const uint JFS_MAGIC            = 0x3153464A;

    const string FS_TYPE     = "jfs";
    const string MODULE_NAME = "JFS plugin";

    /// <summary>Maximum filename length in JFS</summary>
    const ushort JFS_NAME_MAX = 255;

    /// <summary>Byte offset of the aggregate inode table (AITBL_OFF)</summary>
    const long AITBL_OFF = 0xB000;

    /// <summary>On-disk inode size in bytes (DISIZE)</summary>
    const int DISIZE = 512;

    /// <summary>Page size in bytes</summary>
    const int PSIZE = 4096;

    /// <summary>log2(PSIZE)</summary>
    const int L2PSIZE = 12;

    /// <summary>Number of inodes per 4K page</summary>
    const int INOSPERPAGE = 8;

    /// <summary>log2(INOSPERPAGE)</summary>
    const int L2INOSPERPAGE = 3;

    /// <summary>Number of disk inodes per extent</summary>
    const int INOSPEREXT = 32;

    /// <summary>log2(INOSPEREXT)</summary>
    const int L2INOSPEREXT = 5;

    /// <summary>Number of disk inodes per IAG</summary>
    const int INOSPERIAG = 4096;

    /// <summary>log2(INOSPERIAG)</summary>
    const int L2INOSPERIAG = 12;

    /// <summary>Fileset inode map inode number in aggregate inode table</summary>
    const int FILESYSTEM_I = 16;

    /// <summary>Fileset root inode number</summary>
    const int ROOT_I = 2;

    /// <summary>First xtree data entry index (slots 0-1 are the header)</summary>
    const int XTENTRYSTART = 2;

    /// <summary>Max xad entries in xtree root (in inode)</summary>
    const int XTROOTMAXSLOT = 18;

    /// <summary>Max xad entries in xtree page</summary>
    const int XTPAGEMAXSLOT = 256;

    /// <summary>dtroot max slots</summary>
    const int DTROOTMAXSLOT = 9;

    /// <summary>dtpage max slots for 4K pages</summary>
    const int DTPAGEMAXSLOT = 128;

    /// <summary>dtslot size in bytes</summary>
    const int DTSLOTSIZE = 32;

    /// <summary>Name characters per ldtentry head segment (with dir_index)</summary>
    const int DTLHDRDATALEN = 11;

    /// <summary>Name characters per ldtentry head segment (legacy, without dir_index)</summary>
    const int DTLHDRDATALEN_LEGACY = 13;

    /// <summary>XAD slot size</summary>
    const int XTSLOTSIZE = 16;

    // B+-tree flags
    const byte BT_ROOT     = 0x01;
    const byte BT_LEAF     = 0x02;
    const byte BT_INTERNAL = 0x04;
    const byte BT_RIGHTMOST = 0x10;
    const byte BT_LEFTMOST  = 0x20;
    const byte BT_SWAPPED   = 0x80;

    /// <summary>Maximum B+-tree height (xtree and dtree)</summary>
    const int MAXTREEHEIGHT = 8;

    // XAD (extent allocation descriptor) flags
    /// <summary>XAD flag: new extent</summary>
    const byte XAD_NEW = 0x01;

    /// <summary>XAD flag: extended</summary>
    const byte XAD_EXTENDED = 0x02;

    /// <summary>XAD flag: compressed with recorded length</summary>
    const byte XAD_COMPRESSED = 0x04;

    /// <summary>XAD flag: allocated but not recorded (sparse hole)</summary>
    const byte XAD_NOTRECORDED = 0x08;

    /// <summary>XAD flag: copy-on-write</summary>
    const byte XAD_COW = 0x10;

    /// <summary>Number of disk inode extent per IAG</summary>
    const int EXTSPERIAG = 128;

    /// <summary>Number of words per summary map</summary>
    const int SMAPSZ = 4;

    /// <summary>Maximum number of allocation groups</summary>
    const int MAXAG = 128;

    /// <summary>Size of a dmap tree</summary>
    const int TREESIZE = 256 + 64 + 16 + 4 + 1; // 341

    /// <summary>Size of a dmapctl tree</summary>
    const int CTLTREESIZE = 1024 + 256 + 64 + 16 + 4 + 1; // 1365

    /// <summary>Number of leaves per dmap tree</summary>
    const int LPERDMAP = 256;

    /// <summary>Maximum number of active file systems sharing the log</summary>
    const int MAX_ACTIVE = 128;

    /// <summary>Log page size in bytes</summary>
    const int LOGPSIZE = 4096;

    // DataExtent (dxd_t) flags
    /// <summary>DXD flag: B+-tree index</summary>
    const byte DXD_INDEX = 0x80;

    /// <summary>DXD flag: in-line data extent</summary>
    const byte DXD_INLINE = 0x40;

    /// <summary>DXD flag: out-of-line single extent</summary>
    const byte DXD_EXTENT = 0x20;

    /// <summary>DXD flag: out-of-line file (inode)</summary>
    const byte DXD_FILE = 0x10;

    /// <summary>DXD flag: inconsistency detected</summary>
    const byte DXD_CORRUPT = 0x08;

    /// <summary>Extended mode bit: inline EA area free</summary>
    const uint INLINEEA = 0x00040000;

    /// <summary>
    ///     Offset of inline EA data within inode extension area (di_u).
    ///     Layout: _data[96] + unused[16] + dxd[16] + _fastsymlink/rdev[128] + _inlineea[128]
    /// </summary>
    const int INLINEEA_OFFSET = 256;

    /// <summary>Size of inline EA area in bytes</summary>
    const int INLINEEA_SIZE = 128;

    /// <summary>
    ///     Offset of fast symlink data within inode extension area (di_u).
    ///     Layout: _data[96] + unused[16] + dxd[16] = 128 bytes before _fastsymlink
    /// </summary>
    const int FASTSYMLINK_OFFSET = 128;

    /// <summary>Size of inline data area in the inode (IDATASIZE = _fastsymlink[128] + _inlineea[128])</summary>
    const int IDATASIZE = 256;

    /// <summary>Prefix for unknown-namespace OS/2 attributes</summary>
    const string XATTR_OS2_PREFIX = "os2.";

    /// <summary>
    ///     Offset of di_rdev (device number) within inode extension area (di_u).
    ///     Layout: _data[96] + unused[16] + dxd[16] = 128 bytes before _rdev (4 bytes, le32)
    /// </summary>
    const int RDEV_OFFSET = FASTSYMLINK_OFFSET;

    // Extended mode bits in di_mode (on-disk inode)

    /// <summary>Journalled file</summary>
    const uint IFJOURNAL = 0x00010000;

    /// <summary>Sparse file enabled</summary>
    const uint ISPARSE = 0x00020000;

    /// <summary>File open for pager swap space</summary>
    const uint ISWAPFILE = 0x00800000;

    // OS/2 extended attributes in di_mode

    /// <summary>No write access to file (OS/2)</summary>
    const uint IREADONLY = 0x02000000;

    /// <summary>Hidden file (OS/2)</summary>
    const uint IHIDDEN = 0x04000000;

    /// <summary>System file (OS/2)</summary>
    const uint ISYSTEM = 0x08000000;

    /// <summary>Directory shadow (OS/2)</summary>
    const uint IDIRECTORY = 0x20000000;

    /// <summary>File archive bit (OS/2)</summary>
    const uint IARCHIVE = 0x40000000;

    /// <summary>Non-8.3 filename format (OS/2)</summary>
    const uint INEWNAME = 0x80000000;

    // Linux extended flags in di_mode

    /// <summary>Do not update atime</summary>
    const uint JFS_NOATIME_FL = 0x00080000;

    /// <summary>Dirsync behaviour</summary>
    const uint JFS_DIRSYNC_FL = 0x00100000;

    /// <summary>Synchronous updates</summary>
    const uint JFS_SYNC_FL = 0x00200000;

    /// <summary>Writes to file may only append</summary>
    const uint JFS_APPEND_FL = 0x01000000;

    /// <summary>Immutable file</summary>
    const uint JFS_IMMUTABLE_FL = 0x02000000;
}