// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     On-disk structures for the OS/2 High Performance File System.
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

using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of IBM's High Performance File System (HPFS)</summary>
public sealed partial class HPFS
{
#region Nested type: BiosParameterBlock

    /// <summary>BIOS Parameter Block, at sector 0</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BiosParameterBlock
    {
        /// <summary>0x000, Jump to boot code</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] jump;
        /// <summary>0x003, OEM Name, 8 bytes, space-padded</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] oem_name;
        /// <summary>0x00B, Bytes per sector</summary>
        public readonly ushort bps;
        /// <summary>0x00D, Sectors per cluster</summary>
        public readonly byte spc;
        /// <summary>0x00E, Reserved sectors between BPB and... does it have sense in HPFS?</summary>
        public readonly ushort rsectors;
        /// <summary>0x010, Number of FATs... seriously?</summary>
        public readonly byte fats_no;
        /// <summary>0x011, Number of entries on root directory... ok</summary>
        public readonly ushort root_ent;
        /// <summary>0x013, Sectors in volume... doubt it</summary>
        public readonly ushort sectors;
        /// <summary>0x015, Media descriptor</summary>
        public readonly byte media;
        /// <summary>0x016, Sectors per FAT... again</summary>
        public readonly ushort spfat;
        /// <summary>0x018, Sectors per track... you're kidding</summary>
        public readonly ushort sptrk;
        /// <summary>0x01A, Heads... stop!</summary>
        public readonly ushort heads;
        /// <summary>0x01C, Hidden sectors before BPB</summary>
        public readonly uint hsectors;
        /// <summary>0x024, Sectors in volume if &gt; 65535...</summary>
        public readonly uint big_sectors;
        /// <summary>0x028, Drive number</summary>
        public readonly byte drive_no;
        /// <summary>0x029, Volume flags?</summary>
        public readonly byte nt_flags;
        /// <summary>0x02A, EPB signature, 0x28 for HPFS (0x29 for FAT)</summary>
        public readonly byte signature;
        /// <summary>0x02B, Volume serial number</summary>
        public readonly uint serial_no;
        /// <summary>0x02F, Volume label, 11 bytes, space-padded</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public readonly byte[] volume_label;
        /// <summary>0x03A, Filesystem type, 8 bytes, space-padded ("HPFS    ")</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] fs_type;
        /// <summary>Boot code.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 448)]
        public readonly byte[] boot_code;
        /// <summary>0x1FE, 0xAA55</summary>
        public readonly ushort signature2;
    }

#endregion

#region Nested type: SuperBlock

    /// <summary>HPFS superblock at sector 16</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SuperBlock
    {
        /// <summary>0x000, 0xF995E849</summary>
        public readonly uint magic1;
        /// <summary>0x004, 0xFA53E9C5</summary>
        public readonly uint magic2;
        /// <summary>0x008, HPFS version</summary>
        public readonly byte version;
        /// <summary>0x009, 2 if &lt;= 4 GiB, 3 if &gt; 4 GiB</summary>
        public readonly byte func_version;
        /// <summary>0x00A, Alignment</summary>
        public readonly ushort dummy;
        /// <summary>0x00C, LSN pointer to root fnode</summary>
        public readonly uint root_fnode;
        /// <summary>0x010, Sectors on volume</summary>
        public readonly uint sectors;
        /// <summary>0x014, Bad blocks on volume</summary>
        public readonly uint badblocks;
        /// <summary>0x018, LSN pointer to volume bitmap</summary>
        public readonly uint bitmap_lsn;
        /// <summary>0x01C, 0</summary>
        public readonly uint zero1;
        /// <summary>0x020, LSN pointer to badblock directory</summary>
        public readonly uint badblock_lsn;
        /// <summary>0x024, 0</summary>
        public readonly uint zero2;
        /// <summary>0x028, Time of last CHKDSK</summary>
        public readonly int last_chkdsk;
        /// <summary>0x02C, Time of last optimization</summary>
        public readonly int last_optim;
        /// <summary>0x030, Sectors of dir band</summary>
        public readonly uint dband_sectors;
        /// <summary>0x034, Start sector of dir band</summary>
        public readonly uint dband_start;
        /// <summary>0x038, Last sector of dir band</summary>
        public readonly uint dband_last;
        /// <summary>0x03C, LSN of free space bitmap</summary>
        public readonly uint dband_bitmap;
        /// <summary>0x040, Volume name (32 bytes, can be used for volume name)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] volume_name;
        /// <summary>0x060, LSN pointer to user ID table (8 preallocated sectors, HPFS386)</summary>
        public readonly uint user_id_table;
        /// <summary>0x064, Reserved (103 dwords)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 103)]
        public readonly uint[] reserved;
    }

#endregion

#region Nested type: SpareBlock

    /// <summary>HPFS spareblock at sector 17</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SpareBlock
    {
        /// <summary>0x000, 0xF9911849</summary>
        public readonly uint magic1;
        /// <summary>0x004, 0xFA5229C5</summary>
        public readonly uint magic2;
        /// <summary>0x008, HPFS flags</summary>
        public readonly SpareBlockFlags flags1;
        /// <summary>0x009, HPFS386 flags</summary>
        public readonly SpareBlockFlags386 flags2;
        /// <summary>0x00A, MM contiguity</summary>
        public readonly byte mm_contiguity;
        /// <summary>0x00B, Unused</summary>
        public readonly byte unused;
        /// <summary>0x00C, LSN of hotfix directory</summary>
        public readonly uint hotfix_start;
        /// <summary>0x010, Used hotfixes</summary>
        public readonly uint hotfix_used;
        /// <summary>0x014, Total hotfixes available</summary>
        public readonly uint hotfix_entries;
        /// <summary>0x018, Unused spare dnodes</summary>
        public readonly uint spare_dnodes_free;
        /// <summary>0x01C, Length of spare dnodes list</summary>
        public readonly uint spare_dnodes;
        /// <summary>0x020, LSN of codepage directory</summary>
        public readonly uint codepage_lsn;
        /// <summary>0x024, Number of codepages used</summary>
        public readonly uint codepages;
        /// <summary>0x028, SuperBlock CRC32 (only HPFS386)</summary>
        public readonly uint sb_crc32;
        /// <summary>0x02C, SpareBlock CRC32 (only HPFS386)</summary>
        public readonly uint sp_crc32;
        /// <summary>0x030, Reserved (15 dwords)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public readonly uint[] zero1;
        /// <summary>0x06C, Emergency free dnode list (100 entries)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public readonly uint[] spare_dnodes_list;
        /// <summary>0x1FC, Room for more?</summary>
        public readonly uint zero2;
    }

#endregion

#region Nested type: DNode

    /// <summary>Directory node (dnode), 4 sectors (2048 bytes) long</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DNode
    {
        /// <summary>0x000, 0x77E40AAE</summary>
        public readonly uint magic;
        /// <summary>0x004, Offset from start of dnode to first free dir entry</summary>
        public readonly uint first_free;
        /// <summary>0x008, Flags: bit 0 = root dnode, bits 1-7 = activity counter</summary>
        public readonly byte flags;
        /// <summary>0x009, Activity counter (3 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] increment_me;
        /// <summary>0x00C, Parent: directory's fnode (if root) or parent dnode</summary>
        public readonly uint up;
        /// <summary>0x010, Pointer to this dnode (self-reference)</summary>
        public readonly uint self;
        /// <summary>0x014, Directory entries (2028 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2028)]
        public readonly byte[] dirent;

        /// <summary>Returns true if this is the root dnode of a directory</summary>
        public bool IsRoot => (flags & 0x01) != 0;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>Directory entry within a dnode</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectoryEntry
    {
        /// <summary>0x00, Offset to next dirent (length of this entry)</summary>
        public readonly ushort length;
        /// <summary>0x02, Entry flags</summary>
        public readonly DirectoryEntryFlags flags;
        /// <summary>0x03, DOS attributes</summary>
        public readonly DosAttributes attributes;
        /// <summary>0x04, FNode giving allocation info</summary>
        public readonly uint fnode;
        /// <summary>0x08, Modification time (seconds since 1970)</summary>
        public readonly uint write_date;
        /// <summary>0x0C, File length in bytes</summary>
        public readonly uint file_size;
        /// <summary>0x10, Access time (seconds since 1970)</summary>
        public readonly uint read_date;
        /// <summary>0x14, Creation time (seconds since 1970)</summary>
        public readonly uint creation_date;
        /// <summary>0x18, Total EA length in bytes</summary>
        public readonly uint ea_size;
        /// <summary>0x1C, Number of ACL's (low 3 bits)</summary>
        public readonly byte no_of_acls;
        /// <summary>0x1D, Code page index (of filename)</summary>
        public readonly byte code_page_index;
        /// <summary>0x1E, File name length</summary>
        public readonly byte namelen;

        // Followed by: name[namelen] and optionally down pointer (dnode_secno)
    }

#endregion

#region Nested type: FNode

    /// <summary>File node (fnode), root of allocation B+ tree</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FNode
    {
        /// <summary>0x000, 0xF7E40AAE</summary>
        public readonly uint magic;
        /// <summary>0x004, Read history (unused)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly uint[] zero1;
        /// <summary>0x00C, True length of filename</summary>
        public readonly byte name_len;
        /// <summary>0x00D, Truncated name (15 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public readonly byte[] name;
        /// <summary>0x01C, Pointer to file's directory fnode</summary>
        public readonly uint up;
        /// <summary>0x020, ACL size (large)</summary>
        public readonly uint acl_size_l;
        /// <summary>0x024, First sector of disk-resident ACL</summary>
        public readonly uint acl_secno;
        /// <summary>0x028, ACL size (small, fnode-resident)</summary>
        public readonly ushort acl_size_s;
        /// <summary>0x02A, ACL is in anode</summary>
        public readonly byte acl_anode;
        /// <summary>0x02B, History bit count (unused)</summary>
        public readonly byte zero2;
        /// <summary>0x02C, Length of disk-resident EA's</summary>
        public readonly uint ea_size_l;
        /// <summary>0x030, First sector of disk-resident EA's</summary>
        public readonly uint ea_secno;
        /// <summary>0x034, Length of fnode-resident EA's</summary>
        public readonly ushort ea_size_s;
        /// <summary>0x036, Flags: bit 1 = ea_secno is an anode, bit 8 = directory</summary>
        public readonly FNodeFlags flags;
        /// <summary>0x038, B+ tree header (8 bytes)</summary>
        public readonly BPlusHeader btree;
        /// <summary>0x040, B+ tree data: 8 leaf nodes or 12 internal nodes (96 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public readonly byte[] btree_data;
        /// <summary>0x0A0, File length in bytes</summary>
        public readonly uint file_size;
        /// <summary>0x0A4, Number of EA's with NEEDEA set</summary>
        public readonly uint n_needea;
        /// <summary>0x0A8, User ID (unused, 16 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] user_id;
        /// <summary>0x0B8, Offset from start of fnode to first fnode-resident EA</summary>
        public readonly ushort ea_offs;
        /// <summary>0x0BA, DASD limit threshold</summary>
        public readonly byte dasd_limit_threshold;
        /// <summary>0x0BB, DASD limit delta</summary>
        public readonly byte dasd_limit_delta;
        /// <summary>0x0BC, DASD limit</summary>
        public readonly uint dasd_limit;
        /// <summary>0x0C0, DASD usage</summary>
        public readonly uint dasd_usage;
        /// <summary>0x0C4, Fnode-resident EA's (316 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 316)]
        public readonly byte[] ea;

        /// <summary>Returns true if EA sector number points to an anode</summary>
        public bool EaInAnode => (flags & FNodeFlags.EaAnode) != 0;

        /// <summary>Returns true if this fnode belongs to a directory</summary>
        public bool IsDirectory => (flags & FNodeFlags.Directory) != 0;
    }

#endregion

#region Nested type: ANode

    /// <summary>Allocation node (anode), for B+ tree allocation</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ANode
    {
        /// <summary>0x000, 0x37E40AAE</summary>
        public readonly uint magic;
        /// <summary>0x004, Pointer to this anode (self-reference)</summary>
        public readonly uint self;
        /// <summary>0x008, Parent anode or fnode</summary>
        public readonly uint up;
        /// <summary>0x00C, B+ tree header (8 bytes)</summary>
        public readonly BPlusHeader btree;
        /// <summary>0x014, B+ tree data: 40 leaf nodes or 60 internal nodes (480 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 480)]
        public readonly byte[] btree_data;
        /// <summary>0x1F4, Unused (3 dwords)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly uint[] fill;
    }

#endregion

#region Nested type: BPlusHeader

    /// <summary>B+ tree header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BPlusHeader
    {
        /// <summary>
        ///     0x00, Flags:
        ///     bit 0 = high bit of first free entry offset,
        ///     bit 5 = pointed to by fnode/data btree/EA,
        ///     bit 6 = suggest binary search (unused),
        ///     bit 7 = 1 for internal tree of anodes, 0 for leaf list of extents
        /// </summary>
        public readonly BPlusFlags flags;
        /// <summary>0x01, Fill/padding (3 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] fill;
        /// <summary>0x04, Number of free nodes in array</summary>
        public readonly byte n_free_nodes;
        /// <summary>0x05, Number of used nodes in array</summary>
        public readonly byte n_used_nodes;
        /// <summary>0x06, Offset from start of header to first free node</summary>
        public readonly ushort first_free;

        /// <summary>Returns true if this is an internal node (contains anode pointers)</summary>
        public bool IsInternal => (flags & BPlusFlags.Internal) != 0;

        /// <summary>Returns true if this is a leaf node (contains extent runs)</summary>
        public bool IsLeaf => !IsInternal;
    }

#endregion

#region Nested type: BPlusLeafNode

    /// <summary>B+ tree leaf node (extent run)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BPlusLeafNode
    {
        /// <summary>First file sector in extent</summary>
        public readonly uint file_secno;
        /// <summary>Length in sectors</summary>
        public readonly uint length;
        /// <summary>First corresponding disk sector</summary>
        public readonly uint disk_secno;
    }

#endregion

#region Nested type: BPlusInternalNode

    /// <summary>B+ tree internal node (subtree pointer)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BPlusInternalNode
    {
        /// <summary>Subtree maps sectors less than this value</summary>
        public readonly uint file_secno;
        /// <summary>Pointer to subtree (anode sector number)</summary>
        public readonly uint down;
    }

#endregion

#region Nested type: CodePageDirectory

    /// <summary>Code page directory, pointed to by spare block</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CodePageDirectory
    {
        /// <summary>0x00, 0x494521F7</summary>
        public readonly uint magic;
        /// <summary>0x04, Number of code page entries</summary>
        public readonly uint n_code_pages;
        /// <summary>0x08, Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly uint[] zero1;

        // Followed by CodePageDirectoryEntry[n_code_pages] (max 31)
    }

#endregion

#region Nested type: CodePageDirectoryEntry

    /// <summary>Code page directory entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CodePageDirectoryEntry
    {
        /// <summary>0x00, Index</summary>
        public readonly ushort ix;
        /// <summary>0x02, Code page number</summary>
        public readonly ushort code_page_number;
        /// <summary>0x04, Bounds (matches corresponding word in data block)</summary>
        public readonly uint bounds;
        /// <summary>0x08, Sector number of code_page_data containing this code page</summary>
        public readonly uint code_page_data;
        /// <summary>0x0C, Index in code page array in that sector</summary>
        public readonly ushort index;
        /// <summary>0x0E, Unknown value (usually 0, 2 in Japanese)</summary>
        public readonly ushort unknown;
    }

#endregion

#region Nested type: CodePageData

    /// <summary>Code page data block</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CodePageData
    {
        /// <summary>0x00, 0x894521F7</summary>
        public readonly uint magic;
        /// <summary>0x04, Number of elements used in code page array</summary>
        public readonly uint n_used;
        /// <summary>0x08, Bounds (looks like (beg1,end1), (beg2,end2), one byte each)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly uint[] bounds;
        /// <summary>0x14, Offsets from start of sector to start of code page entry</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly ushort[] offs;

        // Followed by CodePageDataEntry[3]
    }

#endregion

#region Nested type: CodePageDataEntry

    /// <summary>Code page data entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CodePageDataEntry
    {
        /// <summary>0x00, Index</summary>
        public readonly ushort ix;
        /// <summary>0x02, Code page number</summary>
        public readonly ushort code_page_number;
        /// <summary>0x04, Unknown (same as in cp directory)</summary>
        public readonly ushort unknown;
        /// <summary>0x06, Upcase table for chars 0x80-0xFF (128 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] map;
        /// <summary>0x86, Padding</summary>
        public readonly ushort zero2;
    }

#endregion

#region Nested type: ExtendedAttribute

    /// <summary>Extended attribute header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedAttribute
    {
        /// <summary>
        ///     0x00, Flags:
        ///     bit 0 = value is indirect (sector number),
        ///     bit 1 = sector is an anode,
        ///     bit 7 = required EA (NEEDEA)
        /// </summary>
        public readonly ExtendedAttributeFlags flags;
        /// <summary>0x01, Length of name in bytes</summary>
        public readonly byte namelen;
        /// <summary>0x02, Length of value (low byte)</summary>
        public readonly byte valuelen_lo;
        /// <summary>0x03, Length of value (high byte)</summary>
        public readonly byte valuelen_hi;

        // Followed by: name[namelen], nul terminator, value[valuelen]
        // If flags.Indirect is set, valuelen is 8 and value is:
        //   uint length (real length of value)
        //   uint secno (sector address where value starts)

        /// <summary>Gets the full value length</summary>
        public ushort ValueLength => (ushort)(valuelen_lo | valuelen_hi << 8);

        /// <summary>Returns true if the value is stored indirectly (in sectors)</summary>
        public bool IsIndirect => (flags & ExtendedAttributeFlags.Indirect) != 0;

        /// <summary>Returns true if the indirect sector is an anode</summary>
        public bool InAnode => (flags & ExtendedAttributeFlags.Anode) != 0;

        /// <summary>Returns true if this EA is required</summary>
        public bool NeedEa => (flags & ExtendedAttributeFlags.NeedEa) != 0;
    }

#endregion

#region Nested type: ExtendedAttributeIndirectValue

    /// <summary>Extended attribute indirect value (when EA_indirect flag is set)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedAttributeIndirectValue
    {
        /// <summary>Real length of value in bytes</summary>
        public readonly uint length;
        /// <summary>Sector address where value starts</summary>
        public readonly uint secno;
    }

#endregion
}