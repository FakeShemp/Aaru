// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     On-disk structures for the High Performance Optical File System.
//     Reverse-engineered from UHPOFS.DLL and HPOFS20.IFS via Ghidra.
//
//     HPOFS stores all on-disk multi-byte fields in BIG-ENDIAN byte order.
//     The driver and utility swap fields to little-endian on read and back
//     to big-endian on write.
//
//     Naming conventions (from HP's OFS design):
//       VMI  — Volume Management Information  (signature "VMISUBCL")
//       SMI  — System Management Information  (signature "SMISUBCL")
//       DCI  — Disk Configuration Information (signature "MAST")
//
//     Sector map (known fixed positions):
//       Sector 0x00  — Boot sector (primary)
//       Sector 0x0D  — Media Info record ("MEDINFO", mirrors SMI core)
//       Sector 0x0E  — Volume Info record ("VOLINFO", mirrors VMI core)
//       Sector 0x0F  — R/W Statistics ("RWSTATS1")
//       Sector 0x14  — DCI record (primary)
//       Sector 0x7F  — Boot sector (backup)
//       Sector 0x8C  — Media Info record (backup)
//       Sector 0x8D  — Volume Info record (backup)
//       Sector 0x8E  — R/W Statistics (backup)
//       Sector 0x93  — DCI record (backup)
//
//     B-tree record signatures:
//       "MAST" — Master / DCI record
//       "INDX" — Index node (directory master, directory entry)
//       "DATA" — Data node (band header, SFA descriptor)
//       "SUBF" — Subfile extent list
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

[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class HPOFS
{
#region Nested type: BiosParameterBlock

    /// <summary>
    ///     BIOS Parameter Block, at sector 0 (backup at sector 0x7F), little-endian.
    ///     Standard x86 BPB with HPOFS extension markers at the end.
    ///     Written by WriteBootSector (UHPOFS.DLL VA: 1000:e1d0).
    ///     Size: 512 bytes (one sector).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BiosParameterBlock
    {
        /// <summary>0x000, x86 JMP SHORT + NOP {0xEB, 0x00, 0x90}</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] jump;
        /// <summary>0x003, OEM Name, 8 bytes, space-padded (e.g. "IBM 10.2")</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] oem_name;
        /// <summary>0x00B, Bytes per sector (e.g. 512, 1024, 2048)</summary>
        public readonly ushort bps;
        /// <summary>0x00D, Sectors per allocation unit</summary>
        public readonly byte spc;
        /// <summary>0x00E, Reserved sectors, always 0 for HPOFS</summary>
        public readonly ushort rsectors;
        /// <summary>0x010, Number of FATs, always 0 (no FATs in HPOFS)</summary>
        public readonly byte fats_no;
        /// <summary>0x011, Root entry count, 0 or 0xE000 (compatibility field)</summary>
        public readonly ushort root_ent;
        /// <summary>0x013, 16-bit total sector count</summary>
        public readonly ushort sectors;
        /// <summary>0x015, Media descriptor, always 0xF8 (hard disk)</summary>
        public readonly byte media;
        /// <summary>0x016, Sectors per FAT, always 0 (no FATs)</summary>
        public readonly ushort spfat;
        /// <summary>0x018, Disk geometry: sectors per track</summary>
        public readonly ushort sptrk;
        /// <summary>0x01A, Disk geometry: number of heads</summary>
        public readonly ushort heads;
        /// <summary>0x01C, Hidden sectors before this partition</summary>
        public readonly uint hsectors;
        /// <summary>0x020, 32-bit total sector count</summary>
        public readonly uint big_sectors;
        /// <summary>0x024, BIOS physical drive number (always 0)</summary>
        public readonly byte drive_no;
        /// <summary>0x025, Reserved</summary>
        public readonly byte nt_flags;
        /// <summary>0x026, Extended BPB signature (always 0x29)</summary>
        public readonly byte signature;
        /// <summary>0x027, Volume serial number (always 0 in format)</summary>
        public readonly uint serial_no;
        /// <summary>0x02B, Volume label, 11 bytes, space-padded ("NO NAME    ")</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public readonly byte[] volume_label;
        /// <summary>0x036, Filesystem type, 8 bytes ("HPOFS\0\0\0")</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] fs_type;
        /// <summary>0x03E, Boot code / reserved area</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 442)]
        public readonly byte[] boot_code;
        /// <summary>0x1F8, HPOFS marker byte 1 (0x2F = '/') and marker byte 2 (0xF8 = media type echo)</summary>
        public readonly ushort hpofsMarker;
        /// <summary>0x1FA, Reserved (always 0)</summary>
        public readonly ushort hpofsReserved;
        /// <summary>0x1FC, HPOFS version indicator (always 1)</summary>
        public readonly ushort hpofsVersion;
        /// <summary>0x1FE, Standard boot signature (0xAA55)</summary>
        public readonly ushort signature2;
    }

#endregion

#region Nested type: VmiBlock

    /// <summary>
    ///     Volume Management Information block (signature "VMISUBCL").
    ///     Contains volume identification, version info, label, and sector layout pointers.
    ///     A trimmed copy is stored as the VOLINFO record at sector 0x0E.
    ///     Written by WriteSuperblock (UHPOFS.DLL VA: 1000:e54a).
    ///     Endian-swapped by SwapSpareBlockEndian (VA: 1008:053e).
    ///     Core swapped by SwapSpareBlockCoreEndian (VA: 1008:09dc).
    ///     Size: one sector (structure occupies 0x198 = 408 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct VmiBlock
    {
        /// <summary>0x000, Signature "VMISUBCL"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] signature;
        /// <summary>0x008, Record version/type (init=1)</summary>
        public uint recordVersion1;
        /// <summary>0x00C, Record version/type (init=1)</summary>
        public uint recordVersion2;
        /// <summary>0x010, Reserved (init=0)</summary>
        public uint reserved10;
        /// <summary>0x014, Reserved (init=0)</summary>
        public uint reserved14;
        /// <summary>0x018, Reserved (init=0)</summary>
        public uint reserved18;
        /// <summary>0x01C, Reserved (init=0)</summary>
        public uint reserved1C;
        /// <summary>0x020, Status word (init=0)</summary>
        public ushort statusWord;
        /// <summary>0x022, Reserved (init=0)</summary>
        public ushort reserved22;
        /// <summary>0x024, Reserved (init=0)</summary>
        public uint reserved24;
        /// <summary>0x028, OEM volume name, 32 bytes, space-padded (EBCDIC ok)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] volumeOemName;
        /// <summary>0x048, Volume label, 32 bytes (copied from user input)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] volumeLabel;
        /// <summary>0x068, Volume comment, 16 bytes, space-padded (EBCDIC ok)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] volumeComment;
        /// <summary>0x078, Status/flags (init=0)</summary>
        public ushort status78;
        /// <summary>0x07A, Status/flags (init=0)</summary>
        public ushort status7A;
        /// <summary>0x07C, Format version number</summary>
        public ushort formatVersion;
        /// <summary>0x07E, Status/flags (init=0)</summary>
        public ushort status7E;
        /// <summary>0x080, Reserved / unused padding (116 bytes, not endian-swapped)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 116)]
        public byte[] reserved80;
        /// <summary>0x0F4, Primary sector count</summary>
        public uint sectorCountPrimary;
        /// <summary>0x0F8, Reserved gap</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reservedF8;
        /// <summary>0x0FC, Backup sector count (same as primary)</summary>
        public uint sectorCountBackup;
        /// <summary>0x100, Reserved gap</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved100;
        /// <summary>0x104, Alternative sector count 1</summary>
        public uint sectorCountAlt1;
        /// <summary>0x108, Reserved gap</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved108;
        /// <summary>0x10C, Alternative sector count 2</summary>
        public uint sectorCountAlt2;
        /// <summary>0x110, Reserved / unused padding (86 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 86)]
        public byte[] reserved110;
        /// <summary>0x166, Directory range 1 allocation type</summary>
        public ushort dirRange1Type;
        /// <summary>0x168, Directory range 1 start sector</summary>
        public uint dirRange1Sector;
        /// <summary>0x16C, Reserved</summary>
        public ushort reserved16C;
        /// <summary>0x16E, Directory range 1 sector count</summary>
        public ushort dirRange1Count;
        /// <summary>0x170, Directory data block 1 sector</summary>
        public uint dirData1Sector;
        /// <summary>0x174, Reserved</summary>
        public ushort reserved174;
        /// <summary>0x176, Directory range 2 allocation type</summary>
        public ushort dirRange2Type;
        /// <summary>0x178, Directory range 2 start sector</summary>
        public uint dirRange2Sector;
        /// <summary>0x17C, Reserved</summary>
        public ushort reserved17C;
        /// <summary>0x17E, Directory range 2 sector count</summary>
        public ushort dirRange2Count;
        /// <summary>0x180, Directory data block 2 sector</summary>
        public uint dirData2Sector;
        /// <summary>0x184, Reserved gap</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved184;
        /// <summary>0x188, SMI block primary sector (+1 offset)</summary>
        public uint smiPrimarySector;
        /// <summary>0x18C, Reserved gap</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved18C;
        /// <summary>0x190, SMI block backup sector (+1 offset)</summary>
        public uint smiBackupSector;
        /// <summary>0x194, Number of bands currently in use (init=0)</summary>
        public ushort usedBandCount;
        /// <summary>0x196, Total number of bands on volume</summary>
        public ushort totalBandCount;
    }

#endregion

#region Nested type: SmiBlock

    /// <summary>
    ///     System Management Information block (signature "SMISUBCL").
    ///     The main filesystem parameter block, cached in memory by the IFS driver as the "superblock".
    ///     Contains volume serial number, codepage configuration, media geometry, band layout, and
    ///     partition pointers.  A trimmed copy is stored as the MEDINFO record at sector 0x0D.
    ///     Written by WriteSpareBlock (UHPOFS.DLL VA: 1000:e94e).
    ///     Endian-swapped by SwapSuperBlockEndian (VA: 1008:007c).
    ///     Core swapped by SwapSuperBlockCoreEndian (VA: 1008:087e).
    ///     Size: one sector (structure occupies 0x1A2 = 418 bytes).
    ///     NOTE ON NAMING: The code names are historically reversed:
    ///     WriteSuperblock     → writes VMISUBCL (the VMI block)
    ///     WriteSpareBlock     → writes SMISUBCL (the SMI block)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct SmiBlock
    {
        /// <summary>0x000, Signature "SMISUBCL"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] signature;
        /// <summary>0x008, Volume label, 32 bytes, space-padded (EBCDIC ok)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] volumeLabel;
        /// <summary>0x028, Codepage/character mapping tables (160 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
        public byte[] codepageMappingData;
        /// <summary>0x0C8, Volume serial number (from timestamp hash)</summary>
        public uint volumeSerialNumber;
        /// <summary>0x0CC, Format version major</summary>
        public ushort formatVersionMajor;
        /// <summary>0x0CE, Format version minor</summary>
        public ushort formatVersionMinor;
        /// <summary>0x0D0, Codepage type (1=ASCII, 2=EBCDIC)</summary>
        public ushort codepageType;
        /// <summary>0x0D2, Specific codepage identifier</summary>
        public ushort codepageId;
        /// <summary>0x0D4, Total sectors on volume</summary>
        public uint totalSectors;
        /// <summary>0x0D8, Bytes per sector</summary>
        public ushort bytesPerSector;
        /// <summary>0x0DA, Bytes per sector (duplicate)</summary>
        public ushort bytesPerSector2;
        /// <summary>0x0DC, Reserved (init=0)</summary>
        public uint reservedDC;
        /// <summary>0x0E0, Sectors per band (totalSectors / numBands)</summary>
        public uint sectorsPerBand;
        /// <summary>0x0E4, Bytes per sector as uint32 (high word always 0)</summary>
        public uint bytesPerSectorExt;
        /// <summary>0x0E8, Reserved / padding (4 bytes, not endian-swapped)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reservedE8;
        /// <summary>0x0EC, Reserved (init=0)</summary>
        public uint reservedEC;
        /// <summary>0x0F0, Format revision (0x00010000 = v1.0)</summary>
        public uint formatRevision;
        /// <summary>0x0F4, Reserved (updated at runtime)</summary>
        public uint reservedF4;
        /// <summary>0x0F8, Reserved (part of swapped dword at 0xF8)</summary>
        public ushort reservedF8lo;
        /// <summary>
        ///     0x0FA, Flags byte 1 (bit 0x80 always set).
        ///     IFS driver tests word at 0xFA: bit 0x8000 of word = bit 7 of flags2.
        /// </summary>
        public byte flags1;
        /// <summary>
        ///     0x0FB, Flags byte 2.
        ///     bit 0x80 = extended layout (0x8000 as word),
        ///     bit 0x40 = optical media type (MEDINFO),
        ///     bit 0x20 = non-optical media.
        /// </summary>
        public byte flags2;
        /// <summary>0x0FC, Reserved (init=0)</summary>
        public ushort reservedFC;
        /// <summary>0x0FE, Reserved (init=0)</summary>
        public ushort reservedFE;
        /// <summary>0x100, Reserved (init=0)</summary>
        public ushort reserved100;
        /// <summary>0x102, Reserved</summary>
        public ushort reserved102;
        /// <summary>0x104, Reserved / padding (4 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved104;
        /// <summary>0x108, Format indicator (always 1)</summary>
        public byte formatIndicator;
        /// <summary>
        ///     0x109, Directory separator character.
        ///     0x2F = '/' (ASCII codepages), 0x61 = EBCDIC slash equivalent.
        /// </summary>
        public byte dirSeparator;
        /// <summary>0x10A, Reserved / unused padding (92 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 92)]
        public byte[] reserved10A;
        /// <summary>0x166, Directory range 1 allocation type</summary>
        public ushort dirRange1Type;
        /// <summary>0x168, Directory range 1 start sector</summary>
        public uint dirRange1Sector;
        /// <summary>0x16C, Reserved</summary>
        public ushort reserved16C;
        /// <summary>0x16E, Directory range 1 extra field</summary>
        public ushort dirRange1Extra;
        /// <summary>0x170, Directory data block 1 sector</summary>
        public uint dirData1Sector;
        /// <summary>0x174, Reserved</summary>
        public ushort reserved174;
        /// <summary>0x176, Directory range 2 allocation type</summary>
        public ushort dirRange2Type;
        /// <summary>0x178, Directory range 2 start sector</summary>
        public uint dirRange2Sector;
        /// <summary>0x17C, Reserved</summary>
        public ushort reserved17C;
        /// <summary>0x17E, Directory range 2 extra field</summary>
        public ushort dirRange2Extra;
        /// <summary>0x180, Directory data block 2 sector</summary>
        public uint dirData2Sector;
        /// <summary>0x184, Reserved gap</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved184;
        /// <summary>0x188, SMI block primary sector address</summary>
        public uint smiPrimarySector;
        /// <summary>0x18C, Reserved gap</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved18C;
        /// <summary>0x190, SMI block backup sector address</summary>
        public uint smiBackupSector;
        /// <summary>0x194, Bands currently in use</summary>
        public ushort usedBandCount;
        /// <summary>0x196, Total bands on volume (init=0x20)</summary>
        public ushort totalBandCount;
        /// <summary>0x198, Reserved (from global)</summary>
        public ushort reserved198;
        /// <summary>0x19A, Buffer size in sectors (used by IFS driver)</summary>
        public ushort bufferSectorCount;
        /// <summary>0x19C, Spare/additional sector count</summary>
        public ushort spareSectorCount;
        /// <summary>0x19E, Reserved (from global)</summary>
        public ushort reserved19E;
        /// <summary>0x1A0, Tail version indicator (init=1)</summary>
        public ushort tailVersion;
    }

#endregion

#region Nested type: MediaInformationBlock

    /// <summary>
    ///     Media Information Block, at sector 0x0D (backup at 0x8C), big-endian.
    ///     This is a trimmed copy of the SMI block core section (offsets 0xC8–0x102 of SMI).
    ///     Written by WriteMediaInfoRecord (UHPOFS.DLL VA: 1000:2524).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct MediaInformationBlock
    {
        /// <summary>0x000, Block identifier "MEDINFO "</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] blockId;
        /// <summary>0x008, Volume label, 32 bytes, space-padded</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] volumeLabel;
        /// <summary>0x028, Volume comment / codepage mapping data, 160 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 160)]
        public byte[] comment;
        /// <summary>0x0C8, Volume serial number (from timestamp hash, maps to SMI+0xC8)</summary>
        public uint serial;
        /// <summary>0x0CC, Format version major (maps to SMI+0xCC)</summary>
        public ushort creationDate;
        /// <summary>0x0CE, Format version minor (maps to SMI+0xCE)</summary>
        public ushort creationTime;
        /// <summary>0x0D0, Codepage type: 1=ASCII, 2=EBCDIC (maps to SMI+0xD0)</summary>
        public ushort codepageType;
        /// <summary>0x0D2, Codepage identifier (maps to SMI+0xD2)</summary>
        public ushort codepage;
        /// <summary>0x0D4, Total sectors on volume (maps to SMI+0xD4)</summary>
        public uint rps;
        /// <summary>0x0D8, Bytes per sector (maps to SMI+0xD8)</summary>
        public ushort bps;
        /// <summary>0x0DA, Bytes per sector duplicate (maps to SMI+0xDA)</summary>
        public ushort bpc;
        /// <summary>0x0DC, Reserved (init=0, maps to SMI+0xDC)</summary>
        public uint unknown2;
        /// <summary>0x0E0, Sectors per band (totalSectors/numBands, maps to SMI+0xE0)</summary>
        public uint sectors;
        /// <summary>0x0E4, Bytes per sector as uint32 (maps to SMI+0xE4)</summary>
        public uint unknown3;
        /// <summary>0x0E8, Reserved / padding (not endian-swapped, maps to SMI+0xE8–0xF7)</summary>
        public ulong unknown4;
        /// <summary>0x0F0, Format revision high word (maps to SMI+0xF0)</summary>
        public ushort major;
        /// <summary>0x0F2, Format revision low word (maps to SMI+0xF2)</summary>
        public ushort minor;
        /// <summary>0x0F4, Reserved (maps to SMI+0xF4–0xF7)</summary>
        public uint unknown5;
        /// <summary>0x0F8, Flags / reserved (maps to SMI+0xF8–0xFB)</summary>
        public uint unknown6;
        /// <summary>0x0FC, Empty / padding to end of sector</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        public byte[] filler;
    }

#endregion

#region Nested type: VolumeInformationBlock

    /// <summary>
    ///     Volume Information Block, at sector 0x0E (backup at 0x8D), big-endian.
    ///     This is a trimmed copy of the VMI block core section (offsets 0x08–0x7E of VMI).
    ///     Written by WriteVolumeInfoRecord (UHPOFS.DLL VA: 1000:2750).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct VolumeInformationBlock
    {
        /// <summary>0x000, Block identifier "VOLINFO "</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] blockId;
        /// <summary>0x008, Record version/type 1 (init=1, maps to VMI+0x08)</summary>
        public uint unknown;
        /// <summary>0x00C, Record version/type 2 (init=1, maps to VMI+0x0C)</summary>
        public uint unknown2;
        /// <summary>0x010, Directory intent counter (maps to VMI+0x10)</summary>
        public uint dir_intent_cnt;
        /// <summary>0x014, Directory update counter (maps to VMI+0x14)</summary>
        public uint dir_update_cnt;
        /// <summary>0x018, Reserved, 22 bytes (maps to VMI+0x18–0x27)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public byte[] unknown3;
        /// <summary>0x02E, OEM volume name, 32 bytes, space-padded (maps to VMI+0x28)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] unknown4;
        /// <summary>0x04E, Volume label / owner, 32 bytes, space-padded (maps to VMI+0x48)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] owner;
        /// <summary>0x06E, Volume comment, 16 bytes, space-padded (maps to VMI+0x68)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] unknown5;
        /// <summary>0x07E, Status/flags (maps to VMI+0x78–0x7B)</summary>
        public uint unknown6;
        /// <summary>0x082, Format version number (maps to VMI+0x7C)</summary>
        public ushort percentFull;
        /// <summary>0x084, Status/flags (maps to VMI+0x7E)</summary>
        public ushort unknown7;
        /// <summary>0x086, Empty / padding to end of sector</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 384)]
        public byte[] filler;
    }

#endregion

#region Nested type: CodepageEntry

    /// <summary>
    ///     Individual codepage descriptor.
    ///     Part of the codepage info structure within DCI blocks and system file entries.
    ///     4 entries per codepage info block at stride 0x34 (52 bytes).
    ///     Swapped by SwapCodepageInfoEndian (UHPOFS.DLL VA: 1008:02c4).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct CodepageEntry
    {
        /// <summary>0x00, Codepage identifier</summary>
        public ushort codepageId;
        /// <summary>0x02, Codepage type (1=SBCS, 2=DBCS)</summary>
        public ushort codepageType;
        /// <summary>0x04, Offset to codepage data</summary>
        public uint dataOffset;
        /// <summary>0x08, Length of codepage data</summary>
        public ushort dataLength;
        /// <summary>0x0A, Reserved</summary>
        public ushort reserved0A;
        /// <summary>0x0C, Codepage field</summary>
        public uint field0C;
        /// <summary>0x10, Codepage field</summary>
        public uint field10;
        /// <summary>0x14, Codepage field</summary>
        public uint field14;
        /// <summary>0x18, Codepage field</summary>
        public uint field18;
        /// <summary>0x1C, Codepage field</summary>
        public uint field1C;
        /// <summary>0x20, Codepage field</summary>
        public uint field20;
        /// <summary>0x24, Codepage field</summary>
        public uint field24;
        /// <summary>0x28, Codepage field</summary>
        public uint field28;
        /// <summary>0x2C, Codepage field</summary>
        public uint field2C;
        /// <summary>0x30, Padding to stride boundary</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved30;
    }

#endregion

#region Nested type: CodepageInfo

    /// <summary>
    ///     Codepage information header + 4 codepage entries.
    ///     Used as the payload of DCI blocks (at DCI offset +4) and as the layout
    ///     of System File Entries.
    ///     Swapped by SwapCodepageInfoEndian (UHPOFS.DLL VA: 1008:02c4).
    ///     Also swapped by SwapSystemFileEntryFields (VA: 1000:2084).
    ///     Size: 0x110 = 272 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct CodepageInfo
    {
        /// <summary>0x00, File type or record index</summary>
        public uint field00;
        /// <summary>0x04, File type or record index (duplicate)</summary>
        public uint field04;
        /// <summary>0x08, Initial value / version (init=1)</summary>
        public uint field08;
        /// <summary>0x0C, Flags (bit 13 set = 0x2000)</summary>
        public ushort flags;
        /// <summary>0x0E, Reserved (not endian-swapped)</summary>
        public ushort reserved0E;
        /// <summary>0x10, Field</summary>
        public uint field10;
        /// <summary>0x14, Field</summary>
        public ushort field14;
        /// <summary>0x16, Field</summary>
        public ushort field16;
        /// <summary>0x18, Field</summary>
        public uint field18;
        /// <summary>0x1C, Version/date field</summary>
        public ushort field1C;
        /// <summary>0x1E, Version/date field</summary>
        public ushort field1E;
        /// <summary>0x20, Version/date field</summary>
        public ushort field20;
        /// <summary>0x22, Version/date field</summary>
        public ushort field22;
        /// <summary>0x24, Field</summary>
        public ushort field24;
        /// <summary>0x26, Field</summary>
        public ushort field26;
        /// <summary>0x28, Field</summary>
        public ushort field28;
        /// <summary>0x2A, Field</summary>
        public ushort field2A;
        /// <summary>0x2C, Reserved / padding, 20 bytes (not endian-swapped)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] reserved2C;
        /// <summary>0x40, First codepage entry</summary>
        public CodepageEntry entry0;
        /// <summary>0x74, Second codepage entry</summary>
        public CodepageEntry entry1;
        /// <summary>0xA8, Third codepage entry</summary>
        public CodepageEntry entry2;
        /// <summary>0xDC, Fourth codepage entry</summary>
        public CodepageEntry entry3;
    }

#endregion

#region Nested type: SystemFileEntry

    /// <summary>
    ///     System file descriptor.
    ///     Describes a system file (band metadata, directory, etc.) and its extent allocation.
    ///     Embedded within band headers.
    ///     Initialized by InitializeSystemFileEntry (UHPOFS.DLL VA: 1000:291a).
    ///     Swapped by SwapSystemFileEntryFields (VA: 1000:2084).
    ///     Header layout is identical to CodepageInfo header.
    ///     Size: 0x2C = 44 bytes (header only, followed by extent and file-specific data).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct SystemFileEntry
    {
        /// <summary>0x00, File type index (low word = type, hi = 0)</summary>
        public uint fileType;
        /// <summary>0x04, File type index (duplicate of fileType)</summary>
        public uint fileType2;
        /// <summary>0x08, Initial version/count (init=1)</summary>
        public uint initialVersion;
        /// <summary>
        ///     0x0C, Flags (bit 13 always set = 0x2000, bits in high byte: |= 0x60 conditionally)
        /// </summary>
        public ushort flags;
        /// <summary>0x0E, Sector size type (6 or 0x10, NOT endian-swapped)</summary>
        public ushort sectorSizeType;
        /// <summary>0x10, Field</summary>
        public uint field10;
        /// <summary>0x14, Format version major</summary>
        public ushort versionMajor;
        /// <summary>0x16, Format version minor</summary>
        public ushort versionMinor;
        /// <summary>0x18, Field</summary>
        public uint field18;
        /// <summary>0x1C, Creation version major</summary>
        public ushort creationMajor;
        /// <summary>0x1E, Creation version minor</summary>
        public ushort creationMinor;
        /// <summary>0x20, Creation version major (duplicate)</summary>
        public ushort creationMajor2;
        /// <summary>0x22, Creation version minor (duplicate)</summary>
        public ushort creationMinor2;
        /// <summary>0x24, Field</summary>
        public ushort field24;
        /// <summary>0x26, Field</summary>
        public ushort field26;
        /// <summary>0x28, Field</summary>
        public ushort field28;
        /// <summary>0x2A, Field</summary>
        public ushort field2A;
    }

#endregion

#region Nested type: ExtentEntry

    /// <summary>
    ///     Extent descriptor for allocation maps.
    ///     Describes a contiguous run of sectors.  Used in arrays, e.g., in the DCI extent map.
    ///     Swapped by SwapExtentArrayEndian (UHPOFS.DLL VA: 1008:0708) per entry.
    ///     Size: 0x08 = 8 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct ExtentEntry
    {
        /// <summary>0x00, Extent type / allocation flags</summary>
        public ushort extentType;
        /// <summary>0x02, Reserved / padding</summary>
        public ushort reserved02;
        /// <summary>0x04, Starting sector address of the extent</summary>
        public uint sectorAddress;
    }

#endregion

#region Nested type: AllocNode

    /// <summary>
    ///     B-tree allocation node.
    ///     Used within band headers for tracking sector allocation.
    ///     Each node has a variable-size key area (keyLength bytes from start) followed by
    ///     a 4-byte sector address (uint32).
    ///     Two variants exist:
    ///     Standard (keyLength=0x14): name[12] at +0x08, sectorAddress at +0x14, total 0x18 = 24 bytes
    ///     Small    (keyLength=0x10): name[8]  at +0x08, sectorAddress at +0x10, total 0x14 = 20 bytes
    ///     Each node is followed by a record of recordDescriptor bytes (typically 0x74 or 0xDC for directories).
    ///     Standard stride = 0x8C (0x18 + 0x74), small stride = 0x88 (0x14 + 0x74).
    ///     Swapped by SwapAllocNodeEndian (UHPOFS.DLL VA: 1008:07a6).
    ///     This struct represents the standard (keyLength=0x14) layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct AllocNode
    {
        /// <summary>0x00, Key area size (0x14=standard, 0x10=small)</summary>
        public ushort keyLength;
        /// <summary>0x02, Record descriptor / record size (0x74 or 0xDC for directory nodes)</summary>
        public ushort recordDescriptor;
        /// <summary>0x04, B-tree entry size (varies: 0x06–0x10)</summary>
        public ushort entrySize;
        /// <summary>0x06, Checksum of the entry name</summary>
        public ushort nameChecksum;
        /// <summary>
        ///     0x08, Null-padded entry name (e.g. "almostfree", "root").
        ///     For standard nodes: 12 bytes.
        ///     For small nodes (keyLength=0x10): only 8 bytes, sectorAddress shifts to +0x10.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] name;
        /// <summary>
        ///     0x14, Sector address / B-tree pointer.
        ///     At offset = keyLength from node start.
        ///     For small nodes (keyLength=0x10): overlaps name[8..11].
        /// </summary>
        public uint sectorAddress;
    }

#endregion

#region Nested type: BandHeader

    /// <summary>
    ///     Data band descriptor ("DATA" signature).
    ///     Describes a band (allocation group) including its alloc nodes and system file entries.
    ///     Contains embedded sub-structures.
    ///     Written by WriteBandHeaders (UHPOFS.DLL VA: 1000:2a30).
    ///     Header swapped by SwapBandHeaderEndian (VA: 1008:0756).
    ///     Embedded alloc nodes swapped by SwapAllocNodeEndian.
    ///     Embedded system file entries swapped by InitializeSystemFileEntry.
    ///     Minimum header size: 0x24 = 36 bytes.
    ///     Band layout for alloc nodes (from WriteBandHeaders):
    ///     Node 0 at 0x024: (unnamed), fileType=0
    ///     Node 1 at 0x0B0: almostfree, fileType=1
    ///     Node 2 at 0x13C: badspots, fileType=2
    ///     Node 3 at 0x1C8: directory, fileType=3
    ///     Node 4 at 0x2BC: freefile, fileType=4
    ///     Node 5 at 0x348: reserved, fileType=5
    ///     Node 6 at 0x3D4: root (small, keyLength=0x10), fileType=7
    ///     Node 7 at 0x45C: token (small, keyLength=0x10), fileType=6
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct BandHeader
    {
        /// <summary>0x00, Signature "DATA"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>0x04, Reserved (=0)</summary>
        public ushort reserved04;
        /// <summary>0x06, Reserved (=0)</summary>
        public ushort reserved06;
        /// <summary>0x08, Entry version (init=1)</summary>
        public ushort entryVersion;
        /// <summary>0x0A, Reserved (=0)</summary>
        public ushort reserved0A;
        /// <summary>0x0C, Band type indicator (0=standard, 2=directory band)</summary>
        public uint bandType;
        /// <summary>0x10, Band flags (0=default, 3=directory primary)</summary>
        public ushort bandFlags;
        /// <summary>0x12, Reserved (=0, swapped by BandHeaderEndian)</summary>
        public ushort reserved12;
        /// <summary>0x14, Reserved (=0)</summary>
        public ushort reserved14;
        /// <summary>0x16, Number of system file entries (3, 5, or 8)</summary>
        public ushort systemFileCount;
        /// <summary>
        ///     0x18, Band data area size in bytes.
        ///     count=3 → 0x01C0, count=5 → 0x0348, count=8 → 0x04E4.
        /// </summary>
        public ushort bandDataSize;
        /// <summary>0x1A, Reserved (=0)</summary>
        public ushort reserved1A;
        /// <summary>0x1C, Reserved (=0)</summary>
        public uint reserved1C;
        /// <summary>0x20, Reserved (=0)</summary>
        public ushort reserved20;
        /// <summary>0x22, Reserved (=0)</summary>
        public ushort reserved22;
    }

#endregion

#region Nested type: DciBlock

    /// <summary>
    ///     Disk Configuration Information block.
    ///     Encoded on-disk with XOR checksum.  First 4 bytes are the checksum
    ///     (XORed with 0xFFFF per word), remaining bytes are a codepage info structure.
    ///     Decoded/swapped by DecodeAndSwapDciBlock (UHPOFS.DLL VA: 1008:0838).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DciBlock
    {
        /// <summary>0x00, Checksum (XOR 0xFFFF each word, then swap)</summary>
        public readonly uint encodedChecksum;
        /// <summary>0x04, Codepage information structure</summary>
        public readonly CodepageInfo codepageInfo;
    }

#endregion

#region Nested type: DciRecord

    /// <summary>
    ///     Master allocation record ("MAST" signature).
    ///     Maps the overall volume allocation using a 32-entry extent table.
    ///     Written at sectors 0x14 (primary) and 0x93 (backup).
    ///     Written by WriteDciRecord (UHPOFS.DLL VA: 1000:2784).
    ///     Swapped by SwapDciRecordFields (VA: 1000:1ee4).
    ///     Size: 0x9C = 156 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DciRecord
    {
        /// <summary>0x00, Signature "MAST"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>0x04, Reserved (=0)</summary>
        public ushort reserved04;
        /// <summary>0x06, Reserved (=0)</summary>
        public ushort reserved06;
        /// <summary>0x08, Record checksum / validation (init=0x00000001)</summary>
        public uint recordChecksum;
        /// <summary>0x0C, Entry type (3 for &gt;= 0x800 sectors, else 4)</summary>
        public ushort entryType;
        /// <summary>0x0E, Reserved (=0, part of dword write at 0x0C)</summary>
        public ushort reserved0E;
        /// <summary>0x10, Number of bands on the volume</summary>
        public ushort bandCount;
        /// <summary>0x12, Record type identifier (=0x0109)</summary>
        public ushort recordType;
        /// <summary>0x14, Record size (=0x00DC = 220 bytes)</summary>
        public ushort recordSize;
        /// <summary>0x16, Flags (=0x8000)</summary>
        public ushort flags;
        /// <summary>0x18, Reserved (=0, NOT endian-swapped)</summary>
        public ushort reserved18;
        /// <summary>0x1A, Chain link / sequence number (=1)</summary>
        public ushort chainLink;
        /// <summary>
        ///     0x1C, Extent map array (32 entries).
        ///     extentMap[30] (0x94) = 1 (primary band),
        ///     extentMap[31] (0x98) = 2 (secondary).
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] extentMap;
    }

#endregion

#region Nested type: DirectoryMasterRecord

    /// <summary>
    ///     Root of a directory B-tree ("INDX" signature).
    ///     Contains type info, codepage, and embedded directory entry descriptors.
    ///     Written by WriteDirectoryMasterRecord (UHPOFS.DLL VA: 1000:2872).
    ///     Swapped by SwapDirMasterRecordFields (VA: 1000:1f88).
    ///     Minimum header size: 0x24 = 36 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DirectoryMasterRecord
    {
        /// <summary>0x00, Signature "INDX"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>0x04, Reserved (=0)</summary>
        public ushort reserved04;
        /// <summary>0x06, Reserved (=0)</summary>
        public ushort reserved06;
        /// <summary>0x08, Field (init=0, endian-swapped)</summary>
        public uint field08;
        /// <summary>0x0C, Field (init=0, endian-swapped)</summary>
        public uint field0C;
        /// <summary>0x10, Field (init=0, endian-swapped)</summary>
        public uint field10;
        /// <summary>0x14, Entry count (init=1)</summary>
        public ushort entryCount;
        /// <summary>0x16, Codepage type for this directory (1=ASCII, 2=EBCDIC)</summary>
        public ushort codepageType;
        /// <summary>0x18, Key separator char (0x40='@' for ASCII, 0x5C='\' for EBCDIC)</summary>
        public ushort keySeparator;
        /// <summary>0x1A, Reserved (=0)</summary>
        public ushort reserved1A;
        /// <summary>0x1C, Field (init=0, endian-swapped)</summary>
        public uint field1C;
        /// <summary>0x20, Reserved (=0, NOT endian-swapped)</summary>
        public ushort reserved20;
        /// <summary>0x22, Field (=0, endian-swapped)</summary>
        public ushort field22;
    }

#endregion

#region Nested type: DirectoryEntryHeader

    /// <summary>
    ///     Header for directory entry records within B-tree index nodes.
    ///     Swapped by SwapDirEntryHeaderFields (UHPOFS.DLL VA: 1000:2038).
    ///     NOTE: This structure contains a uint32 at the UNALIGNED byte offset 0x07.
    ///     Size: 0x10 = 16 bytes.
    ///     Maximum filename length: 256 bytes (0x100).
    ///     Full entry layout within INDX data area (variable-length):
    ///     +0x00: entryLength (uint16) — total size of this entry; 0 = corrupt
    ///     +0x02: entryType   (uint16) — entry type / flags
    ///     +0x04: entryFlags  (uint8)  — additional flags byte
    ///     +0x05: reserved    (uint16) — NOT endian-swapped
    ///     +0x07: sectorOrTimestamp (uint32) — sector address or timestamp (UNALIGNED!)
    ///     +0x0B: reserved    (uint8)
    ///     +0x0C: parentOrSize (uint32) — parent reference or file size
    ///     +0x16: nameLength  (uint16) — length of filename string
    ///     +0x18: fileType    (uint16) — file type / attribute code
    ///     +0x1A: nameData[nameLength] — filename characters (variable length)
    ///     After name + 4-byte alignment padding: sub-structure with extended attributes.
    ///     Last 2 bytes of record: end_marker (uint16) — 0xFFFF = deleted/invalid entry.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DirectoryEntryHeader
    {
        /// <summary>0x00, Total entry length in bytes</summary>
        public ushort entryLength;
        /// <summary>0x02, Entry type / flags</summary>
        public ushort entryType;
        /// <summary>0x04, Additional flags byte</summary>
        public byte entryFlags;
        /// <summary>0x05, Reserved (NOT endian-swapped)</summary>
        public ushort reserved05;
        /// <summary>0x07, Sector address or timestamp (UNALIGNED uint32!)</summary>
        public uint sectorOrTimestamp;
        /// <summary>0x0B, Reserved</summary>
        public byte reserved0B;
        /// <summary>0x0C, Parent reference or file size</summary>
        public uint parentOrSize;
    }

#endregion

#region Nested type: DirectoryEntryRecord

    /// <summary>
    ///     Full directory entry record used in B-tree nodes ("INDX" signature).
    ///     Written by WriteDirectoryEntryRecord (UHPOFS.DLL VA: 1000:3694).
    ///     Minimum header size: 0x1A = 26 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct DirectoryEntryRecord
    {
        /// <summary>0x00, Signature "INDX"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>0x04, Reserved (=0)</summary>
        public ushort reserved04;
        /// <summary>0x06, Reserved (=0)</summary>
        public ushort reserved06;
        /// <summary>0x08, Field (init=0)</summary>
        public uint field08;
        /// <summary>0x0C, Field (init=0)</summary>
        public uint field0C;
        /// <summary>0x10, Field (init=0)</summary>
        public uint field10;
        /// <summary>0x14, Entry count (init=1)</summary>
        public ushort entryCount;
        /// <summary>0x16, Codepage type</summary>
        public ushort codepageType;
        /// <summary>0x18, Entry size (=0x34, 52 bytes)</summary>
        public ushort entrySize;
    }

#endregion

#region Nested type: SfaDescriptor

    /// <summary>
    ///     System File Area descriptor ("DATA" signature).
    ///     Tracks free sectors within the System File Area (SFA).
    ///     Contains embedded free sector entries.
    ///     Written by WriteSfaDescriptor (UHPOFS.DLL VA: 1000:38b0).
    ///     Swapped by SwapSfaDescriptorFields (VA: 1000:21b4).
    ///     SwapSfaDescriptorFields swaps: 0x00:word, 0x04:dword, 0x08:word, 0x0C:dword,
    ///     0x10:dword, 0x18:dword, 0x20:dword, 0x28:dword.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct SfaDescriptor
    {
        /// <summary>0x00, Signature "DATA"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>0x04, Reserved</summary>
        public ushort reserved04;
        /// <summary>0x06, Reserved</summary>
        public ushort reserved06;
        /// <summary>0x08, SFA version (init=1)</summary>
        public uint sfaVersion;
    }

#endregion

#region Nested type: FreeSectorEntry

    /// <summary>
    ///     Free sector tracking within SFA descriptors.
    ///     Tracks available sector ranges for allocation.
    ///     Used as embedded entries within SFA descriptors at 20-byte stride.
    ///     Written inline by WriteSfaDescriptor at offsets 0x24, 0x38, 0x4C.
    ///     NOTE: Contains an UNALIGNED uint32 at byte offset 0x07.
    ///     Size: 0x14 = 20 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct FreeSectorEntry
    {
        /// <summary>0x00, Entry type (=7)</summary>
        public ushort entryType;
        /// <summary>0x02, Sub-type (=5)</summary>
        public ushort subType;
        /// <summary>0x04, Flags byte (=0)</summary>
        public byte flags;
        /// <summary>0x05, Reserved padding</summary>
        public ushort reserved05;
        /// <summary>0x07, Start sector (UNALIGNED uint32!)</summary>
        public uint startSector;
        /// <summary>0x0B, Padding</summary>
        public byte reserved0B;
        /// <summary>0x0C, Number of sectors</summary>
        public uint sectorCount;
        /// <summary>0x10, Used flag (0x01 = in use)</summary>
        public byte usedFlag;
        /// <summary>0x11, Padding to 20-byte boundary</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] reserved11;
    }

#endregion

#region Nested type: FreeSectorsRecord

    /// <summary>
    ///     Free sector information record.
    ///     Top-level record for free sector tracking per band or region.
    ///     Swapped by SwapFreeSectorsRecordFields (UHPOFS.DLL VA: 1000:224c).
    ///     Size: 0x1C = 28 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct FreeSectorsRecord
    {
        /// <summary>0x00, Record header (8 bytes, not swapped)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] header;
        /// <summary>0x08, Number of free sector entries</summary>
        public ushort entryCount;
        /// <summary>0x0A, Gap / padding (not swapped)</summary>
        public ushort reserved0A;
        /// <summary>0x0C, Total free sectors in this region</summary>
        public uint totalFreeSectors;
        /// <summary>0x10, Field</summary>
        public ushort field10;
        /// <summary>0x12, Gap / padding (not swapped)</summary>
        public ushort reserved12;
        /// <summary>0x14, Field (sector address or count)</summary>
        public uint field14;
        /// <summary>0x18, Field (sector address or count)</summary>
        public uint field18;
    }

#endregion

#region Nested type: ExtentListHeader

    /// <summary>
    ///     On-disk subfile extent chain ("SUBF" signature).
    ///     Linked chain of extent list blocks.  Each block contains a header followed
    ///     by an array of extent descriptors.  The chain is terminated when nextListLba
    ///     is 0 or the extent count is 0.
    ///     Documented in ValidateSectorAllocationLists (UHPOFS.DLL VA: 1000:1866).
    ///     All multi-byte fields stored BIG-ENDIAN on disk.
    ///     Size: 0x20 = 32 bytes (header only), followed by N × ExtentDescriptor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct ExtentListHeader
    {
        /// <summary>0x00, "SUBF" magic identifier</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>0x04, Reserved / unknown</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] reserved04;
        /// <summary>0x0C, LBA of next extent list block (big-endian)</summary>
        public uint nextListLba;
        /// <summary>0x10, Number of valid extent descriptors</summary>
        public ushort extentCount;
        /// <summary>0x12, Reserved / padding</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public byte[] reserved12;
    }

#endregion

#region Nested type: ExtentDescriptor

    /// <summary>
    ///     On-disk extent within SUBF extent list blocks.
    ///     Describes a contiguous run of sectors in a subfile allocation chain.
    ///     An entry with startLba == 0xFFFFFFFF marks end-of-list.
    ///     All multi-byte fields stored BIG-ENDIAN on disk.
    ///     Byte-swapped by ValidateSectorAllocationLists per entry.
    ///     Size: 0x08 = 8 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    struct ExtentDescriptor
    {
        /// <summary>0x00, Number of sectors in this extent</summary>
        public ushort sectorCount;
        /// <summary>0x02, Reserved / padding</summary>
        public ushort reserved02;
        /// <summary>0x04, Starting LBA (0xFFFFFFFF = end marker)</summary>
        public uint startLba;
    }

#endregion

#region Nested type: Dci

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Dci
    {
        /// <summary>"DATA"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] blockId;
        /// <summary>Unknown</summary>
        public readonly uint unknown;
        /// <summary>Unknown</summary>
        public readonly uint unknown2;
        /// <summary>Unknown</summary>
        public readonly uint unknown3;
        /// <summary>Unknown</summary>
        public readonly uint unknown4;
        /// <summary>Unknown</summary>
        public readonly uint unknown5;
        /// <summary>Unknown</summary>
        public readonly ushort unknown6;
        /// <summary>Unknown</summary>
        public readonly ushort unknown7;
        /// <summary>Unknown</summary>
        public readonly uint unknown8;
        /// <summary>Unknown</summary>
        public readonly uint unknown9;
        /// <summary>Entries, size unknown</summary>
        public readonly DciEntry[] entries;
    }

#endregion

#region Nested type: DciEntry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DciEntry
    {
        /// <summary>Key length</summary>
        public readonly ushort key_len;
        /// <summary>Record length</summary>
        public readonly ushort record_len;
        /// <summary>dci key</summary>
        public readonly DciKey key;
        /// <summary>Padding? Size is key_len - size of DciKey</summary>
        public readonly byte[] padding;
        /// <summary>Direct</summary>
        public readonly Direct dir;
        /// <summary>Padding? Size is record_len - size of Direct</summary>
        public readonly byte[] unknown;
    }

#endregion

#region Nested type: DciKey

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DciKey
    {
        /// <summary>Unknown</summary>
        public readonly byte unknown;
        /// <summary>Name size + 2</summary>
        public readonly byte size;
        /// <summary>Unknown</summary>
        public readonly byte unknown2;
        /// <summary>Unknown</summary>
        public readonly byte unknown3;
        /// <summary>Unknown</summary>
        public readonly byte unknown4;
        /// <summary>Unknown</summary>
        public readonly byte unknown5;
        /// <summary>Name, length = size - 2</summary>
        public readonly byte[] name;
    }

#endregion

#region Nested type: Direct

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Direct
    {
        /// <summary>Unknown</summary>
        public readonly uint unknown;
        /// <summary>Unknown</summary>
        public readonly uint unknown2;
        /// <summary>Unknown</summary>
        public readonly uint unknown3;
        /// <summary>Mask 0x6000</summary>
        public readonly ushort subfiles_no;
        /// <summary>Unknown</summary>
        public readonly ushort unknown4;
        /// <summary>Unknown</summary>
        public readonly uint unknown5;
        /// <summary>Unknown</summary>
        public readonly uint unknown6;
        /// <summary>Unknown</summary>
        public readonly uint unknown7;
        /// <summary>Some date</summary>
        public readonly ushort date1;
        /// <summary>Some time</summary>
        public readonly ushort time1;
        /// <summary>Some date</summary>
        public readonly ushort date2;
        /// <summary>Some time</summary>
        public readonly ushort time2;
        /// <summary>Unknown</summary>
        public readonly uint unknown8;
        /// <summary>Unknown</summary>
        public readonly uint unknown9;
        /// <summary>Unknown</summary>
        public readonly uint unknown10;
        /// <summary>Unknown</summary>
        public readonly uint unknown11;
        /// <summary>Unknown</summary>
        public readonly uint unknown12;
        /// <summary>Unknown</summary>
        public readonly uint unknown13;
        /// <summary>Unknown</summary>
        public readonly uint unknown14;
        /// <summary>Subfiles, length unknown</summary>
        public readonly SubFile[] subfiles;
    }

#endregion

#region Nested type: Extent

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Extent
    {
        /// <summary>Extent length in sectors</summary>
        public readonly ushort length;
        /// <summary>Unknown</summary>
        public readonly short unknown;
        /// <summary>Extent starting sector</summary>
        public readonly int start;
    }

#endregion

#region Nested type: MasterRecord

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct MasterRecord
    {
        /// <summary>"MAST"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] blockId;
        /// <summary>Unknown</summary>
        public readonly uint unknown;
        /// <summary>Unknown</summary>
        public readonly ushort unknown2;
        /// <summary>Unknown</summary>
        public readonly ushort unknown3;
        /// <summary>Unknown</summary>
        public readonly uint unknown4;
        /// <summary>Unknown</summary>
        public readonly ushort unknown5;
        /// <summary>Unknown</summary>
        public readonly ushort unknown6;
        /// <summary>Unknown</summary>
        public readonly ushort unknown7;
        /// <summary>Unknown</summary>
        public readonly ushort unknown8;
        /// <summary>Unknown</summary>
        public readonly uint unknown9;
    }

#endregion

#region Nested type: SubFile

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SubFile
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly Extent[] extents;
        /// <summary>Unknown</summary>
        public readonly uint unknown;
        /// <summary>Unknown</summary>
        public readonly uint unknown2;
        /// <summary>Logical size in bytes</summary>
        public readonly uint logical_size;
        /// <summary>Unknown</summary>
        public readonly uint unknown3;
        /// <summary>Physical size in bytes</summary>
        public readonly uint physical_size;
        /// <summary>Unknown</summary>
        public readonly uint unknown4;
        /// <summary>Physical size in bytes</summary>
        public readonly uint physical_size2;
        /// <summary>Unknown</summary>
        public readonly uint unknown5;
        /// <summary>Unknown</summary>
        public readonly uint unknown6;
    }

#endregion
}