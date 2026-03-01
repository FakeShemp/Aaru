// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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

using System;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

// Information from Inside Windows NT and the Linux kernel NTFS driver (fs/ntfs)
/// <inheritdoc />
/// <summary>Implements detection of the New Technology File System (NTFS)</summary>
public sealed partial class NTFS
{
#region Nested type: BiosParameterBlock

    /// <summary>NTFS $BOOT</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BiosParameterBlock
    {
        // Start of BIOS Parameter Block
        /// <summary>0x000, Jump to boot code</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] jump;
        /// <summary>0x003, OEM Name, 8 bytes, space-padded, must be "NTFS    "</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] oem_name;
        /// <summary>0x00B, Bytes per sector</summary>
        public readonly ushort bps;
        /// <summary>0x00D, Sectors per cluster</summary>
        public readonly byte spc;
        /// <summary>0x00E, Reserved sectors, seems 0</summary>
        public readonly ushort rsectors;
        /// <summary>0x010, Number of FATs... obviously, 0</summary>
        public readonly byte fats_no;
        /// <summary>0x011, Number of entries on root directory... 0</summary>
        public readonly ushort root_ent;
        /// <summary>0x013, Sectors in volume... 0</summary>
        public readonly ushort sml_sectors;
        /// <summary>0x015, Media descriptor</summary>
        public readonly byte media;
        /// <summary>0x016, Sectors per FAT... 0</summary>
        public readonly ushort spfat;
        /// <summary>0x018, Sectors per track, required to boot</summary>
        public readonly ushort sptrk;
        /// <summary>0x01A, Heads... required to boot</summary>
        public readonly ushort heads;
        /// <summary>0x01C, Hidden sectors before BPB</summary>
        public readonly uint hsectors;
        /// <summary>0x020, Sectors in volume if &gt; 65535... 0</summary>
        public readonly uint big_sectors;
        /// <summary>0x024, Drive number</summary>
        public readonly byte drive_no;
        /// <summary>0x025, 0</summary>
        public readonly byte nt_flags;
        /// <summary>0x026, EPB signature, 0x80</summary>
        public readonly byte signature1;
        /// <summary>0x027, Alignment</summary>
        public readonly byte dummy;

        // End of BIOS Parameter Block

        // Start of NTFS real superblock
        /// <summary>0x028, Sectors on volume</summary>
        public readonly long sectors;
        /// <summary>0x030, LSN of $MFT</summary>
        public readonly long mft_lsn;
        /// <summary>0x038, LSN of $MFTMirror</summary>
        public readonly long mftmirror_lsn;
        /// <summary>0x040, Clusters per MFT record</summary>
        public readonly sbyte mft_rc_clusters;
        /// <summary>0x041, Alignment</summary>
        public readonly byte dummy2;
        /// <summary>0x042, Alignment</summary>
        public readonly ushort dummy3;
        /// <summary>0x044, Clusters per index block</summary>
        public readonly sbyte index_blk_cts;
        /// <summary>0x045, Alignment</summary>
        public readonly byte dummy4;
        /// <summary>0x046, Alignment</summary>
        public readonly ushort dummy5;
        /// <summary>0x048, Volume serial number</summary>
        public readonly ulong serial_no;
        /// <summary>Boot code.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 430)]
        public readonly byte[] boot_code;
        /// <summary>0x1FE, 0xAA55</summary>
        public readonly ushort signature2;
    }

#endregion

#region Nested type: NtfsRecordHeader

    /// <summary>Common header for all multi-sector protected NTFS records</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct NtfsRecordHeader
    {
        /// <summary>0x000, Magic identifier (FILE, INDX, RSTR, RCRD, etc.)</summary>
        public readonly NtfsRecordMagic magic;
        /// <summary>0x004, Offset to Update Sequence Array</summary>
        public readonly ushort usa_ofs;
        /// <summary>0x006, Number of USA entries (including USN)</summary>
        public readonly ushort usa_count;
    }

#endregion

#region Nested type: MftRecord

    /// <summary>MFT record header (NTFS 3.1+, 48 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct MftRecord
    {
        /// <summary>0x000, "FILE" magic</summary>
        public readonly NtfsRecordMagic magic;
        /// <summary>0x004, Offset to Update Sequence Array</summary>
        public readonly ushort usa_ofs;
        /// <summary>0x006, Number of USA entries</summary>
        public readonly ushort usa_count;
        /// <summary>0x008, LogFile sequence number</summary>
        public readonly ulong lsn;
        /// <summary>0x010, Reuse count for this MFT record slot</summary>
        public readonly ushort sequence_number;
        /// <summary>0x012, Number of hard links</summary>
        public readonly ushort link_count;
        /// <summary>0x014, Byte offset to first attribute</summary>
        public readonly ushort attrs_offset;
        /// <summary>0x016, MFT record flags</summary>
        public readonly MftRecordFlags flags;
        /// <summary>0x018, Bytes used in this MFT record</summary>
        public readonly uint bytes_in_use;
        /// <summary>0x01C, Total allocated size of this MFT record</summary>
        public readonly uint bytes_allocated;
        /// <summary>0x020, MFT reference to base record (0 for base records)</summary>
        public readonly ulong base_mft_record;
        /// <summary>0x028, Next attribute instance number</summary>
        public readonly ushort next_attr_instance;
        /// <summary>0x02A, Reserved (NTFS 3.1+)</summary>
        public readonly ushort reserved;
        /// <summary>0x02C, This record's index in $MFT (NTFS 3.1+)</summary>
        public readonly uint mft_record_number;
    }

#endregion

#region Nested type: MftRecordOld

    /// <summary>MFT record header (pre-NTFS 3.1, 42 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct MftRecordOld
    {
        /// <summary>0x000, "FILE" magic</summary>
        public readonly NtfsRecordMagic magic;
        /// <summary>0x004, Offset to Update Sequence Array</summary>
        public readonly ushort usa_ofs;
        /// <summary>0x006, Number of USA entries</summary>
        public readonly ushort usa_count;
        /// <summary>0x008, LogFile sequence number</summary>
        public readonly ulong lsn;
        /// <summary>0x010, Reuse count for this MFT record slot</summary>
        public readonly ushort sequence_number;
        /// <summary>0x012, Number of hard links</summary>
        public readonly ushort link_count;
        /// <summary>0x014, Byte offset to first attribute</summary>
        public readonly ushort attrs_offset;
        /// <summary>0x016, MFT record flags</summary>
        public readonly MftRecordFlags flags;
        /// <summary>0x018, Bytes used in this MFT record</summary>
        public readonly uint bytes_in_use;
        /// <summary>0x01C, Total allocated size of this MFT record</summary>
        public readonly uint bytes_allocated;
        /// <summary>0x020, MFT reference to base record (0 for base records)</summary>
        public readonly ulong base_mft_record;
        /// <summary>0x028, Next attribute instance number</summary>
        public readonly ushort next_attr_instance;
    }

#endregion

#region Nested type: AttrDef

    /// <summary>Attribute definition entry ($AttrDef), 160 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct AttrDef
    {
        /// <summary>0x000, Unicode attribute name (UTF-16LE, zero-terminated)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        public readonly ushort[] name;
        /// <summary>0x080, Attribute type code</summary>
        public readonly AttributeType type;
        /// <summary>0x084, Default display rule</summary>
        public readonly uint display_rule;
        /// <summary>0x088, Default collation rule</summary>
        public readonly CollationRule collation_rule;
        /// <summary>0x08C, ATTR_DEF_* flags</summary>
        public readonly AttrDefFlags flags;
        /// <summary>0x090, Minimum attribute value size</summary>
        public readonly ulong min_size;
        /// <summary>0x098, Maximum attribute value size</summary>
        public readonly ulong max_size;
    }

#endregion

#region Nested type: ResidentAttributeRecord

    /// <summary>Resident attribute record header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ResidentAttributeRecord
    {
        /// <summary>0x000, Attribute type</summary>
        public readonly AttributeType type;
        /// <summary>0x004, Total record length</summary>
        public readonly uint length;
        /// <summary>0x008, Always 0 for resident</summary>
        public readonly byte non_resident;
        /// <summary>0x009, Name length in Unicode characters</summary>
        public readonly byte name_length;
        /// <summary>0x00A, Offset to attribute name</summary>
        public readonly ushort name_offset;
        /// <summary>0x00C, Attribute flags</summary>
        public readonly AttributeFlags flags;
        /// <summary>0x00E, Unique instance number</summary>
        public readonly ushort instance;
        /// <summary>0x010, Attribute value size in bytes</summary>
        public readonly uint value_length;
        /// <summary>0x014, Offset to value data</summary>
        public readonly ushort value_offset;
        /// <summary>0x016, Resident attribute flags</summary>
        public readonly ResidentAttributeFlags resident_flags;
        /// <summary>0x017, Reserved</summary>
        public readonly sbyte reserved;
    }

#endregion

#region Nested type: NonResidentAttributeRecord

    /// <summary>Non-resident attribute record header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct NonResidentAttributeRecord
    {
        /// <summary>0x000, Attribute type</summary>
        public readonly AttributeType type;
        /// <summary>0x004, Total record length</summary>
        public readonly uint length;
        /// <summary>0x008, Always 1 for non-resident</summary>
        public readonly byte non_resident;
        /// <summary>0x009, Name length in Unicode characters</summary>
        public readonly byte name_length;
        /// <summary>0x00A, Offset to attribute name</summary>
        public readonly ushort name_offset;
        /// <summary>0x00C, Attribute flags</summary>
        public readonly AttributeFlags flags;
        /// <summary>0x00E, Unique instance number</summary>
        public readonly ushort instance;
        /// <summary>0x010, Lowest VCN</summary>
        public readonly ulong lowest_vcn;
        /// <summary>0x018, Highest VCN</summary>
        public readonly ulong highest_vcn;
        /// <summary>0x020, Offset to mapping pairs array</summary>
        public readonly ushort mapping_pairs_offset;
        /// <summary>0x022, Log2(clusters per compression unit), 0 if uncompressed</summary>
        public readonly byte compression_unit;
        /// <summary>0x023, Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] reserved;
        /// <summary>0x028, Allocated disk space in bytes</summary>
        public readonly ulong allocated_size;
        /// <summary>0x030, Logical attribute value size in bytes</summary>
        public readonly ulong data_size;
        /// <summary>0x038, Initialized portion size in bytes</summary>
        public readonly ulong initialized_size;
        /// <summary>0x040, Compressed on-disk size (only if compressed/sparse)</summary>
        public readonly ulong compressed_size;
    }

#endregion

#region Nested type: StandardInformationV1

    /// <summary>$STANDARD_INFORMATION attribute (NTFS 1.2)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct StandardInformationV1
    {
        /// <summary>0x000, File creation time</summary>
        public readonly long creation_time;
        /// <summary>0x008, Last data modification time</summary>
        public readonly long last_data_change_time;
        /// <summary>0x010, Last MFT record change time</summary>
        public readonly long last_mft_change_time;
        /// <summary>0x018, Last access time</summary>
        public readonly long last_access_time;
        /// <summary>0x020, File attribute flags</summary>
        public readonly FileAttributeFlags file_attributes;
        /// <summary>0x024, Reserved (12 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] reserved;
    }

#endregion

#region Nested type: StandardInformationV3

    /// <summary>$STANDARD_INFORMATION attribute (NTFS 3.0+)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct StandardInformationV3
    {
        /// <summary>0x000, File creation time</summary>
        public readonly long creation_time;
        /// <summary>0x008, Last data modification time</summary>
        public readonly long last_data_change_time;
        /// <summary>0x010, Last MFT record change time</summary>
        public readonly long last_mft_change_time;
        /// <summary>0x018, Last access time</summary>
        public readonly long last_access_time;
        /// <summary>0x020, File attribute flags</summary>
        public readonly FileAttributeFlags file_attributes;
        /// <summary>0x024, Maximum allowed file versions (0 = versioning disabled)</summary>
        public readonly uint maximum_versions;
        /// <summary>0x028, Current version number</summary>
        public readonly uint version_number;
        /// <summary>0x02C, Class ID</summary>
        public readonly uint class_id;
        /// <summary>0x030, Owner ID (maps to $Quota via $Q index)</summary>
        public readonly uint owner_id;
        /// <summary>0x034, Security ID (maps to $Secure $SII/$SDS)</summary>
        public readonly uint security_id;
        /// <summary>0x038, Quota charge in bytes</summary>
        public readonly ulong quota_charged;
        /// <summary>0x040, Last USN from $UsnJrnl</summary>
        public readonly ulong usn;
    }

#endregion

#region Nested type: AttributeListEntry

    /// <summary>$ATTRIBUTE_LIST entry</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct AttributeListEntry
    {
        /// <summary>0x000, Attribute type code</summary>
        public readonly AttributeType type;
        /// <summary>0x004, Entry length (8-byte aligned)</summary>
        public readonly ushort length;
        /// <summary>0x006, Attribute name length in Unicode characters</summary>
        public readonly byte name_length;
        /// <summary>0x007, Offset to attribute name</summary>
        public readonly byte name_offset;
        /// <summary>0x008, Lowest VCN of this attribute extent</summary>
        public readonly ulong lowest_vcn;
        /// <summary>0x010, MFT reference to record holding this attribute</summary>
        public readonly ulong mft_reference;
        /// <summary>0x018, Attribute instance number</summary>
        public readonly ushort instance;
    }

#endregion

#region Nested type: FileNameAttribute

    /// <summary>$FILE_NAME attribute (type 0x30), always resident</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileNameAttribute
    {
        /// <summary>0x000, MFT reference to parent directory</summary>
        public readonly ulong parent_directory;
        /// <summary>0x008, File creation time</summary>
        public readonly long creation_time;
        /// <summary>0x010, Last data modification time</summary>
        public readonly long last_data_change_time;
        /// <summary>0x018, Last MFT record change time</summary>
        public readonly long last_mft_change_time;
        /// <summary>0x020, Last access time</summary>
        public readonly long last_access_time;
        /// <summary>0x028, Allocated size of unnamed $DATA</summary>
        public readonly ulong allocated_size;
        /// <summary>0x030, Logical size of unnamed $DATA</summary>
        public readonly ulong data_size;
        /// <summary>0x038, File attribute flags</summary>
        public readonly FileAttributeFlags file_attributes;
        /// <summary>0x03C, EA packed size or reparse point tag</summary>
        public readonly uint ea_reparse;
        /// <summary>0x040, File name length in Unicode characters</summary>
        public readonly byte file_name_length;
        /// <summary>0x041, File name namespace</summary>
        public readonly FileNameNamespace file_name_type;
    }

#endregion

#region Nested type: ObjectIdAttribute

    /// <summary>$OBJECT_ID attribute (NTFS 3.0+), always resident, 16–64 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ObjectIdAttribute
    {
        /// <summary>0x000, Unique object GUID</summary>
        public readonly Guid object_id;
        /// <summary>0x010, Birth volume GUID (optional)</summary>
        public readonly Guid birth_volume_id;
        /// <summary>0x020, Birth object GUID (optional)</summary>
        public readonly Guid birth_object_id;
        /// <summary>0x030, Domain GUID (optional, usually zero)</summary>
        public readonly Guid domain_id;
    }

#endregion

#region Nested type: NtfsSid

    /// <summary>Security Identifier (SID) fixed header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct NtfsSid
    {
        /// <summary>0x000, SID revision level (usually 1)</summary>
        public readonly byte revision;
        /// <summary>0x001, Number of sub-authorities</summary>
        public readonly byte sub_authority_count;
        /// <summary>0x002, 6-byte identifier authority</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] identifier_authority;
    }

#endregion

#region Nested type: NtfsAce

    /// <summary>Access Control Entry (ACE) fixed header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct NtfsAce
    {
        /// <summary>0x000, ACE type</summary>
        public readonly AceType type;
        /// <summary>0x001, Inheritance and audit flags</summary>
        public readonly AceFlags flags;
        /// <summary>0x002, Total ACE size in bytes</summary>
        public readonly ushort size;
        /// <summary>0x004, Access rights mask</summary>
        public readonly AccessRights mask;
    }

#endregion

#region Nested type: NtfsAcl

    /// <summary>Access Control List (ACL) header, 8 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct NtfsAcl
    {
        /// <summary>0x000, ACL revision (2 or 4)</summary>
        public readonly byte revision;
        /// <summary>0x001, Padding</summary>
        public readonly byte alignment1;
        /// <summary>0x002, Total ACL size in bytes (header + ACEs)</summary>
        public readonly ushort size;
        /// <summary>0x004, Number of ACEs</summary>
        public readonly ushort ace_count;
        /// <summary>0x006, Padding</summary>
        public readonly ushort alignment2;
    }

#endregion

#region Nested type: SecurityDescriptorRelative

    /// <summary>Self-relative security descriptor, 20 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SecurityDescriptorRelative
    {
        /// <summary>0x000, Revision (usually 1)</summary>
        public readonly byte revision;
        /// <summary>0x001, Padding</summary>
        public readonly byte alignment;
        /// <summary>0x002, Control flags</summary>
        public readonly SecurityDescriptorControl control;
        /// <summary>0x004, Offset to owner SID</summary>
        public readonly uint owner;
        /// <summary>0x008, Offset to group SID</summary>
        public readonly uint group;
        /// <summary>0x00C, Offset to SACL</summary>
        public readonly uint sacl;
        /// <summary>0x010, Offset to DACL</summary>
        public readonly uint dacl;
    }

#endregion

#region Nested type: SiiIndexKey

    /// <summary>Key for $SII index in $Secure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SiiIndexKey
    {
        /// <summary>0x000, Security identifier</summary>
        public readonly uint security_id;
    }

#endregion

#region Nested type: SdhIndexKey

    /// <summary>Key for $SDH index in $Secure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SdhIndexKey
    {
        /// <summary>0x000, Hash of security descriptor</summary>
        public readonly uint hash;
        /// <summary>0x004, Security identifier</summary>
        public readonly uint security_id;
    }

#endregion

#region Nested type: VolumeInformation

    /// <summary>$VOLUME_INFORMATION attribute (type 0x70), always resident</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VolumeInformation
    {
        /// <summary>0x000, Reserved</summary>
        public readonly ulong reserved;
        /// <summary>0x008, NTFS major version number</summary>
        public readonly byte major_ver;
        /// <summary>0x009, NTFS minor version number</summary>
        public readonly byte minor_ver;
        /// <summary>0x00A, Volume flags</summary>
        public readonly VolumeFlags flags;
    }

#endregion

#region Nested type: IndexHeader

    /// <summary>Common header for index entries in index root and index blocks</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct IndexHeader
    {
        /// <summary>0x000, Byte offset to first INDEX_ENTRY (relative to this header)</summary>
        public readonly uint entries_offset;
        /// <summary>0x004, Bytes used by index entries</summary>
        public readonly uint index_length;
        /// <summary>0x008, Total allocated bytes for this index</summary>
        public readonly uint allocated_size;
        /// <summary>0x00C, Index flags (SMALL_INDEX / LARGE_INDEX)</summary>
        public readonly IndexHeaderFlags flags;
        /// <summary>0x00D, Reserved (3 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
    }

#endregion

#region Nested type: IndexRoot

    /// <summary>$INDEX_ROOT attribute (type 0x90), always resident</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct IndexRoot
    {
        /// <summary>0x000, Attribute type being indexed ($FILE_NAME for directories, 0 for view indexes)</summary>
        public readonly AttributeType type;
        /// <summary>0x004, Collation rule for sorting entries</summary>
        public readonly CollationRule collation_rule;
        /// <summary>0x008, Size of each index block in bytes</summary>
        public readonly uint index_block_size;
        /// <summary>0x00C, Clusters per index block</summary>
        public readonly byte clusters_per_index_block;
        /// <summary>0x00D, Reserved (3 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        /// <summary>0x010, Index header</summary>
        public readonly IndexHeader index;
    }

#endregion

#region Nested type: IndexBlock

    /// <summary>Index allocation block ($INDEX_ALLOCATION, type 0xa0), 40-byte header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct IndexBlock
    {
        /// <summary>0x000, "INDX" magic</summary>
        public readonly NtfsRecordMagic magic;
        /// <summary>0x004, Offset to Update Sequence Array</summary>
        public readonly ushort usa_ofs;
        /// <summary>0x006, Number of USA entries</summary>
        public readonly ushort usa_count;
        /// <summary>0x008, Log sequence number of last modification</summary>
        public readonly ulong lsn;
        /// <summary>0x010, VCN of this index block</summary>
        public readonly ulong index_block_vcn;
        /// <summary>0x018, Index header</summary>
        public readonly IndexHeader index;
    }

#endregion

#region Nested type: IndexEntryHeader

    /// <summary>Index entry header (fixed 16-byte part of every index entry)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct IndexEntryHeader
    {
        /// <summary>0x000, MFT reference of indexed file (directory index) or data offset/length (view index)</summary>
        public readonly ulong indexed_file_or_data;
        /// <summary>0x008, Total entry size in bytes</summary>
        public readonly ushort length;
        /// <summary>0x00A, Key size in bytes</summary>
        public readonly ushort key_length;
        /// <summary>0x00C, Index entry flags</summary>
        public readonly IndexEntryFlags flags;
        /// <summary>0x00E, Reserved</summary>
        public readonly ushort reserved;
    }

#endregion

#region Nested type: ReparseIndexKey

    /// <summary>Key for $R index in $Extend/$Reparse</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ReparseIndexKey
    {
        /// <summary>0x000, Reparse point tag</summary>
        public readonly ReparseTag reparse_tag;
        /// <summary>0x004, MFT record number of the file</summary>
        public readonly ulong file_id;
    }

#endregion

#region Nested type: ReparsePointAttribute

    /// <summary>$REPARSE_POINT attribute (type 0xc0)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ReparsePointAttribute
    {
        /// <summary>0x000, Reparse point type and flags</summary>
        public readonly ReparseTag reparse_tag;
        /// <summary>0x004, Size of reparse data in bytes</summary>
        public readonly ushort reparse_data_length;
        /// <summary>0x006, Reserved</summary>
        public readonly ushort reserved;
    }

#endregion

#region Nested type: EaInformation

    /// <summary>$EA_INFORMATION attribute (type 0xd0), always resident</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct EaInformation
    {
        /// <summary>0x000, Packed EA size in bytes</summary>
        public readonly ushort ea_length;
        /// <summary>0x002, Number of EAs with NEED_EA flag set</summary>
        public readonly ushort need_ea_count;
        /// <summary>0x004, Unpacked EA query size in bytes</summary>
        public readonly uint ea_query_length;
    }

#endregion

#region Nested type: EaAttribute

    /// <summary>Extended attribute entry ($EA, type 0xe0) fixed header</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct EaAttribute
    {
        /// <summary>0x000, Offset to next EA entry</summary>
        public readonly uint next_entry_offset;
        /// <summary>0x004, EA flags (NEED_EA = 0x80)</summary>
        public readonly EaFlags flags;
        /// <summary>0x005, EA name length in bytes (excluding NUL terminator)</summary>
        public readonly byte ea_name_length;
        /// <summary>0x006, EA value length in bytes</summary>
        public readonly ushort ea_value_length;
    }

#endregion

#region Nested type: QuotaControlEntry

    /// <summary>Quota control entry in $Quota/$Q (fixed part)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct QuotaControlEntry
    {
        /// <summary>0x000, Version (currently 2)</summary>
        public readonly uint version;
        /// <summary>0x004, Quota flags</summary>
        public readonly QuotaFlags flags;
        /// <summary>0x008, Current quota usage in bytes</summary>
        public readonly ulong bytes_used;
        /// <summary>0x010, Last modification time</summary>
        public readonly long change_time;
        /// <summary>0x018, Soft quota limit (-1 = unlimited)</summary>
        public readonly long threshold;
        /// <summary>0x020, Hard quota limit (-1 = unlimited)</summary>
        public readonly long limit;
        /// <summary>0x028, Time when soft quota was exceeded</summary>
        public readonly long exceeded_time;
    }

#endregion

#region Nested type: RestartPageHeader

    /// <summary>LogFile restart page header ($LogFile, magic "RSTR")</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct RestartPageHeader
    {
        /// <summary>0x000, "RSTR" magic</summary>
        public readonly NtfsRecordMagic magic;
        /// <summary>0x004, Offset to Update Sequence Array</summary>
        public readonly ushort usa_ofs;
        /// <summary>0x006, Number of USA entries</summary>
        public readonly ushort usa_count;
        /// <summary>0x008, Last LSN found by chkdsk</summary>
        public readonly ulong chkdsk_lsn;
        /// <summary>0x010, System page size in bytes</summary>
        public readonly uint system_page_size;
        /// <summary>0x014, Log page size in bytes</summary>
        public readonly uint log_page_size;
        /// <summary>0x018, Byte offset to restart area</summary>
        public readonly ushort restart_area_offset;
        /// <summary>0x01A, Log file minor version</summary>
        public readonly ushort minor_ver;
        /// <summary>0x01C, Log file major version</summary>
        public readonly ushort major_ver;
    }

#endregion

#region Nested type: RestartArea

    /// <summary>LogFile restart area record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct RestartArea
    {
        /// <summary>0x000, Current/last LSN</summary>
        public readonly ulong current_lsn;
        /// <summary>0x008, Number of log client records</summary>
        public readonly ushort log_clients;
        /// <summary>0x00A, Index of first free client record</summary>
        public readonly ushort client_free_list;
        /// <summary>0x00C, Index of first in-use client record</summary>
        public readonly ushort client_in_use_list;
        /// <summary>0x00E, Restart area flags</summary>
        public readonly RestartFlags flags;
        /// <summary>0x010, Bits used for sequence number</summary>
        public readonly uint seq_number_bits;
        /// <summary>0x014, Length of restart area including client array</summary>
        public readonly ushort restart_area_length;
        /// <summary>0x016, Offset to first log client record</summary>
        public readonly ushort client_array_offset;
        /// <summary>0x018, Usable log file size in bytes</summary>
        public readonly ulong file_size;
        /// <summary>0x020, Last LSN data length</summary>
        public readonly uint last_lsn_data_length;
        /// <summary>0x024, Log record header size</summary>
        public readonly ushort log_record_header_length;
        /// <summary>0x026, Offset to data in log page</summary>
        public readonly ushort log_page_data_offset;
        /// <summary>0x028, Log open count (incremented each mount)</summary>
        public readonly uint restart_log_open_count;
        /// <summary>0x02C, Reserved</summary>
        public readonly uint reserved;
    }

#endregion

#region Nested type: LogClientRecord

    /// <summary>LogFile client record</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogClientRecord
    {
        /// <summary>0x000, Oldest LSN needed by this client</summary>
        public readonly ulong oldest_lsn;
        /// <summary>0x008, Client restart position LSN</summary>
        public readonly ulong client_restart_lsn;
        /// <summary>0x010, Previous client record index</summary>
        public readonly ushort prev_client;
        /// <summary>0x012, Next client record index</summary>
        public readonly ushort next_client;
        /// <summary>0x014, Sequence number</summary>
        public readonly ushort seq_number;
        /// <summary>0x016, Reserved (6 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] reserved;
        /// <summary>0x01C, Client name length in bytes</summary>
        public readonly uint client_name_length;
        /// <summary>0x020, Client name in Unicode (usually "NTFS")</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly ushort[] client_name;
    }

#endregion
}