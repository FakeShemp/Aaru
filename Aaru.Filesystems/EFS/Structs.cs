// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class EFS
{
#region Nested type: Superblock

    /// <summary>EFS Superblock structure, 92 bytes. Located at basic block 1.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct Superblock
    {
        /// <summary>0x00: Filesystem size including bb 0, in basic blocks.</summary>
        public int sb_size;
        /// <summary>0x04: First cylinder group offset, in basic blocks.</summary>
        public int sb_firstcg;
        /// <summary>0x08: Cylinder group size, in basic blocks.</summary>
        public int sb_cgfsize;
        /// <summary>0x0C: Inodes per cylinder group, in basic blocks.</summary>
        public short sb_cgisize;
        /// <summary>0x0E: Geometry: sectors per track.</summary>
        public short sb_sectors;
        /// <summary>0x10: Geometry: heads per cylinder (unused).</summary>
        public short sb_heads;
        /// <summary>0x12: Number of cylinder groups in filesystem.</summary>
        public short sb_ncg;
        /// <summary>0x14: Non-zero indicates fsck required.</summary>
        public short sb_dirty;
        /// <summary>0x16: Padding.</summary>
        public short sb_pad0;
        /// <summary>0x18: Superblock creation/modification time.</summary>
        public int sb_time;
        /// <summary>0x1C: Magic number (EFS_MAGIC or EFS_MAGIC_NEW).</summary>
        public uint sb_magic;
        /// <summary>0x20: Filesystem name, 6 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] sb_fname;
        /// <summary>0x26: Filesystem pack name, 6 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] sb_fpack;
        /// <summary>0x2C: Bitmap size in bytes.</summary>
        public int sb_bmsize;
        /// <summary>0x30: Total free data blocks.</summary>
        public int sb_tfree;
        /// <summary>0x34: Total free inodes.</summary>
        public int sb_tinode;
        /// <summary>0x38: Bitmap location (for grown filesystems).</summary>
        public int sb_bmblock;
        /// <summary>0x3C: Replicated superblock location.</summary>
        public int sb_replsb;
        /// <summary>0x40: Last allocated inode.</summary>
        public int sb_lastinode;
        /// <summary>0x44: Spare/unused, 20 bytes (must be zero).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] sb_spare;
        /// <summary>0x58: Checksum of all above fields.</summary>
        public uint sb_checksum;
    }

#endregion

#region Nested type: Extent

    /// <summary>
    ///     EFS Extent structure, 8 bytes. Describes a contiguous range of blocks. The structure uses bit fields packed
    ///     into two 32-bit words.
    /// </summary>
    /// <remarks>
    ///     Layout (big-endian):
    ///     <list type="bullet">
    ///         <item>Bits 63-56: Magic (must be 0)</item>
    ///         <item>Bits 55-32: Block number (24 bits)</item>
    ///         <item>Bits 31-24: Length in basic blocks (8 bits)</item>
    ///         <item>Bits 23-0: Logical offset into file (24 bits)</item>
    ///     </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct Extent
    {
        /// <summary>First 32 bits: magic (8 bits, must be 0) + block number (24 bits).</summary>
        public uint ex_magic_bn;

        /// <summary>Second 32 bits: length (8 bits) + logical offset (24 bits).</summary>
        public uint ex_length_offset;

        /// <summary>Gets the magic number (must be 0 for valid extent).</summary>
        public byte Magic => (byte)(ex_magic_bn >> 24 & 0xFF);

        /// <summary>Gets the starting block number (24 bits).</summary>
        public uint BlockNumber => ex_magic_bn & 0x00FFFFFF;

        /// <summary>Gets the extent length in basic blocks (8 bits, max 248).</summary>
        public byte Length => (byte)(ex_length_offset >> 24 & 0xFF);

        /// <summary>Gets the logical block offset into file (24 bits).</summary>
        public uint Offset => ex_length_offset & 0x00FFFFFF;
    }

#endregion

#region Nested type: DeviceNumbers

    /// <summary>Device numbers for character/block special files. Used in inode di_u union.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct DeviceNumbers
    {
        /// <summary>Old-style device number (16 bits).</summary>
        public ushort odev;
        /// <summary>Padding.</summary>
        public ushort pad;
        /// <summary>New-style (extended) device number (32 bits).</summary>
        public uint ndev;
    }

#endregion

#region Nested type: Inode

    /// <summary>EFS on-disk inode structure, exactly 128 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct Inode
    {
        /// <summary>0x00: Mode and type of file.</summary>
        public ushort di_mode;
        /// <summary>0x02: Number of links to file.</summary>
        public short di_nlink;
        /// <summary>0x04: Owner's user ID.</summary>
        public ushort di_uid;
        /// <summary>0x06: Owner's group ID.</summary>
        public ushort di_gid;
        /// <summary>0x08: Number of bytes in file.</summary>
        public int di_size;
        /// <summary>0x0C: Time last accessed (Unix timestamp).</summary>
        public int di_atime;
        /// <summary>0x10: Time last modified (Unix timestamp).</summary>
        public int di_mtime;
        /// <summary>0x14: Time created/changed (Unix timestamp).</summary>
        public int di_ctime;
        /// <summary>0x18: Generation number.</summary>
        public uint di_gen;
        /// <summary>0x1C: Number of extents.</summary>
        public short di_numextents;
        /// <summary>0x1E: Version of inode (0=EFS, 1=AFS special, 2=AFS normal).</summary>
        public byte di_version;
        /// <summary>0x1F: Spare byte (used by AFS).</summary>
        public byte di_spare;
        /// <summary>0x20: Direct extents array, 12 extents * 8 bytes = 96 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EFS_DIRECTEXTENTS)]
        public Extent[] di_extents;
    }

#endregion

#region Nested type: DirectoryBlock

    /// <summary>EFS directory block header, 4 bytes. The rest of the block contains directory entries.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct DirectoryBlock
    {
        /// <summary>0x00: Magic number (0xBEEF).</summary>
        public ushort magic;
        /// <summary>0x02: Offset to first used directory entry byte.</summary>
        public byte firstused;
        /// <summary>0x03: Number of offset slots in directory block.</summary>
        public byte slots;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>
    ///     EFS directory entry structure. Variable length due to name. The inode number is stored as two 16-bit values
    ///     for alignment reasons.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SwapEndian]
    partial struct DirectoryEntry
    {
        /// <summary>0x00: Inode number high word (big-endian: high 16 bits).</summary>
        public ushort d_inum_high;
        /// <summary>0x02: Inode number low word (big-endian: low 16 bits).</summary>
        public ushort d_inum_low;
        /// <summary>0x04: Length of name string.</summary>
        public byte d_namelen;

        // Followed by d_name[d_namelen] - variable length name

        /// <summary>Gets the full inode number (big-endian format).</summary>
        public uint InodeNumber => (uint)(d_inum_high << 16 | d_inum_low);
    }

#endregion
}