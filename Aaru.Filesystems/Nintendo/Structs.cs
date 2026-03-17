// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

public sealed partial class NintendoPlugin
{
#region Nested type: FstEntry

    /// <summary>
    ///     File System Table (FST) entry for Nintendo Gamecube and Wii optical discs.
    ///     Each entry is 12 bytes, big-endian.
    ///     The root entry (index 0) is always a directory whose <see cref="SizeOrNext" /> field
    ///     contains the total number of entries in the FST. The string table follows
    ///     immediately after the last entry.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct FstEntry
    {
        /// <summary>
        ///     Bits 31-24: Type (0 = file, 1 = directory).
        ///     Bits 23-0: Offset into the string table for the entry name.
        /// </summary>
        public uint TypeAndNameOffset;
        /// <summary>
        ///     For files: offset to file data (multiply by 4 on Wii partitions).
        ///     For directories: index of the parent directory entry.
        /// </summary>
        public uint OffsetOrParent;
        /// <summary>
        ///     For files: size of the file data in bytes.
        ///     For directories: index of the next entry not belonging to this directory.
        /// </summary>
        public uint SizeOrNext;
    }

#endregion

#region Nested type: DiscHeader

    /// <summary>
    ///     On-disk disc header for Nintendo GameCube and Wii optical discs.
    ///     Located at the very beginning of the disc (offset 0x000), 0x440 bytes, big-endian.
    ///     On Wii discs, fields <see cref="DolOff" />, <see cref="FstOff" />, <see cref="FstSize" />,
    ///     and <see cref="FstMax" /> must be shifted left by 2 (multiplied by 4) to obtain actual byte offsets.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DiscHeader
    {
        /// <summary>0x000, Console/disc type identifier character</summary>
        public byte DiscType;
        /// <summary>0x001, Game code (2 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] GameCode;
        /// <summary>0x003, Region code character</summary>
        public byte RegionCode;
        /// <summary>0x004, Publisher/maker code (2 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] PublisherCode;
        /// <summary>0x006, Disc number (for multi-disc sets)</summary>
        public byte DiscNumber;
        /// <summary>0x007, Disc version</summary>
        public byte DiscVersion;
        /// <summary>0x008, Audio streaming flag</summary>
        public byte Streaming;
        /// <summary>0x009, Audio streaming buffer size</summary>
        public byte StreamBufferSize;
        /// <summary>0x00A, Unused (14 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public byte[] Unused1;
        /// <summary>0x018, Wii magic number (0x5D1C9EA3)</summary>
        public uint WiiMagic;
        /// <summary>0x01C, GameCube magic number (0xC2339F3D)</summary>
        public uint GcMagic;
        /// <summary>0x020, Game title (96 bytes, null-terminated)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public byte[] Title;
        /// <summary>0x080, Unused (896 bytes, padding to 0x400)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 896)]
        public byte[] Unused2;
        /// <summary>0x400, Debug monitor offset (GameCube only)</summary>
        public uint DebugOff;
        /// <summary>0x404, Debug monitor load address (GameCube only)</summary>
        public uint DebugAddr;
        /// <summary>0x408, Unused (24 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] Unused3;
        /// <summary>0x420, Main executable (DOL) offset (shift left 2 on Wii)</summary>
        public uint DolOff;
        /// <summary>0x424, FST offset (shift left 2 on Wii)</summary>
        public uint FstOff;
        /// <summary>0x428, FST size in bytes (shift left 2 on Wii)</summary>
        public uint FstSize;
        /// <summary>0x42C, FST maximum size in bytes (shift left 2 on Wii)</summary>
        public uint FstMax;
        /// <summary>0x430, Unused (16 bytes, to reach 0x440)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Unused4;
    }

#endregion

#region Nested type: WiiPartitionTableEntry

    /// <summary>
    ///     On-disk partition table entry for Wii optical discs.
    ///     Each entry is 8 bytes, big-endian.
    ///     The offset value must be shifted left by 2 (multiplied by 4) to obtain the actual byte offset.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct WiiPartitionTableEntry
    {
        /// <summary>Partition offset (shift left 2 for actual byte offset)</summary>
        public uint Offset;
        /// <summary>Partition type (0 = data, 1 = update, 2 = channel)</summary>
        public uint Type;
    }

#endregion

#region Nested type: WiiRegionSettings

    /// <summary>
    ///     On-disk region and age rating settings for Wii optical discs.
    ///     Located at offset 0x4E000 on the disc, big-endian.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct WiiRegionSettings
    {
        /// <summary>0x4E000, Region byte</summary>
        public uint Region;
        /// <summary>0x4E004, Unused (12 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] Unused;
        /// <summary>0x4E010, Japan (CERO) age rating</summary>
        public byte JapanAge;
        /// <summary>0x4E011, USA (ESRB) age rating</summary>
        public byte UsaAge;
        /// <summary>0x4E012, Reserved</summary>
        public byte Reserved;
        /// <summary>0x4E013, Germany (USK) age rating</summary>
        public byte GermanAge;
        /// <summary>0x4E014, Europe (PEGI) age rating</summary>
        public byte PegiAge;
        /// <summary>0x4E015, Finland (MEKU/PEGI-FI) age rating</summary>
        public byte FinlandAge;
        /// <summary>0x4E016, Portugal (PEGI-PT) age rating</summary>
        public byte PortugalAge;
        /// <summary>0x4E017, UK (BBFC) age rating</summary>
        public byte UkAge;
        /// <summary>0x4E018, Australia (OFLC/ACB) age rating</summary>
        public byte AustraliaAge;
        /// <summary>0x4E019, Korea (GRB) age rating</summary>
        public byte KoreaAge;
    }

#endregion

#region Nested type: WiiuTocPartition

    /// <summary>
    ///     Parsed Wii U TOC partition entry (in-memory representation).
    ///     Parsed from decrypted TOC sector at WIIU_TOC_ENTRIES_OFFSET + i * WIIU_TOC_ENTRY_SIZE.
    /// </summary>
    struct WiiuTocPartition
    {
        /// <summary>Partition identifier string (e.g., "SI", "GI", "GM...")</summary>
        public string Identifier;
        /// <summary>Start physical sector (0x8000-byte units, BE u32 at entry + 0x20)</summary>
        public uint StartSector;
        /// <summary>Decrypted partition key (disc key or derived title key)</summary>
        public byte[] Key;
        /// <summary>Whether a title key was found for this partition</summary>
        public bool HasTitleKey;
    }

#endregion

#region Nested type: WiiuFstEntry

    /// <summary>
    ///     Wii U FST entry (16 bytes each, big-endian).
    ///     Different from GC/Wii FST entries (which are 12 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct WiiuFstEntry
    {
        /// <summary>Bits 31-24: Type (0 = file, 1 = directory). Bits 23-0: Name offset</summary>
        public uint TypeAndNameOffset;
        /// <summary>For files: data offset (shift left 5). For directories: parent index</summary>
        public uint OffsetOrParent;
        /// <summary>For files: file size. For directories: next entry index</summary>
        public uint SizeOrNext;
        /// <summary>Flags/permissions word</summary>
        public ushort Flags;
        /// <summary>Cluster index for this file's data</summary>
        public ushort ClusterIndex;
    }

#endregion
}