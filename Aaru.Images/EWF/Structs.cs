// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for Expert Witness Format disk images.
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

namespace Aaru.Images;

public sealed partial class Ewf
{
#region Nested type: EwfFileHeaderV1

    /// <summary>EWF v1 file header, 13 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfFileHeaderV1
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] signature;
        public byte   fields_start;
        public ushort segment_number;
        public ushort fields_end;
    }

#endregion

#region Nested type: EwfFileHeaderV2

    /// <summary>EWF v2 file header, 32 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfFileHeaderV2
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] signature;
        public byte   major_version;
        public byte   minor_version;
        public ushort compression_method;
        public uint   segment_number;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] set_identifier;
    }

#endregion

#region Nested type: EwfSectionDescriptorV1

    /// <summary>EWF v1 section descriptor, 76 bytes. Sections are forward-linked.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfSectionDescriptorV1
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] type_string;
        public ulong next_offset;
        public ulong size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] padding;
        public uint checksum;
    }

#endregion

#region Nested type: EwfSectionDescriptorV2

    /// <summary>EWF v2 section descriptor, 64 bytes. Sections are backward-linked.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfSectionDescriptorV2
    {
        public uint  type;
        public uint  data_flags;
        public ulong previous_offset;
        public ulong data_size;
        public uint  descriptor_size;
        public uint  padding_size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] data_integrity_hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] reserved;
        public uint checksum;
    }

#endregion

#region Nested type: EwfVolumeSection

    /// <summary>EWF v1 EnCase volume section data, 1024 bytes (followed by checksum)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfVolumeSection
    {
        public byte media_type;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] unknown1;
        public uint  number_of_chunks;
        public uint  sectors_per_chunk;
        public uint  bytes_per_sector;
        public ulong number_of_sectors;
        public uint  chs_cylinders;
        public uint  chs_heads;
        public uint  chs_sectors;
        public byte  media_flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] unknown2;
        public uint palm_volume_start_sector;
        public uint unknown3;
        public uint smart_logs_start_sector;
        public byte compression_level;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] unknown4;
        public uint error_granularity;
        public uint unknown5;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] set_identifier;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 963)]
        public byte[] unknown6;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] signature;
        public uint checksum;
    }

#endregion

#region Nested type: EwfVolumeSmartSection

    /// <summary>EWF SMART/old EnCase volume section data, 94 bytes total</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfVolumeSmartSection
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] unknown1;
        public uint number_of_chunks;
        public uint sectors_per_chunk;
        public uint bytes_per_sector;
        public uint number_of_sectors;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] unknown2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)]
        public byte[] unknown3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] signature;
        public uint checksum;
    }

#endregion

#region Nested type: EwfTableHeaderV1

    /// <summary>EWF v1 table section header, 24 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfTableHeaderV1
    {
        public uint  number_of_entries;
        public uint  padding1;
        public ulong base_offset;
        public uint  padding2;
        public uint  checksum;
    }

#endregion

#region Nested type: EwfTableHeaderV2

    /// <summary>EWF v2 table section header, 32 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfTableHeaderV2
    {
        public ulong first_chunk_number;
        public uint  number_of_entries;
        public uint  unknown1;
        public uint  checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] padding;
    }

#endregion

#region Nested type: EwfTableEntryV2

    /// <summary>EWF v2 table entry, 16 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfTableEntryV2
    {
        public ulong chunk_data_offset;
        public uint  chunk_data_size;
        public uint  chunk_data_flags;
    }

#endregion

#region Nested type: EwfHashSection

    /// <summary>EWF v1 hash section, 36 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfHashSection
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] md5_hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] unknown1;
        public uint checksum;
    }

#endregion

#region Nested type: EwfDigestSection

    /// <summary>EWF v1 digest section, 80 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfDigestSection
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] md5_hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] sha1_hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] padding1;
        public uint checksum;
    }

#endregion

#region Nested type: EwfSessionHeaderV1

    /// <summary>EWF v1 session section header, 36 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfSessionHeaderV1
    {
        public uint number_of_entries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] unknown1;
        public uint checksum;
    }

#endregion

#region Nested type: EwfSessionEntryV1

    /// <summary>EWF v1 session entry, 32 bytes. Note: flags first, then start_sector.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfSessionEntryV1
    {
        public uint flags;
        public uint start_sector;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] unknown1;
    }

#endregion

#region Nested type: EwfSessionHeaderV2

    /// <summary>EWF v2 session section header, 32 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfSessionHeaderV2
    {
        public uint number_of_entries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] unknown1;
        public uint checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] padding;
    }

#endregion

#region Nested type: EwfSessionEntryV2

    /// <summary>EWF v2 session entry, 32 bytes. Note: start_sector first, then flags (reversed from v1).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfSessionEntryV2
    {
        public ulong start_sector;
        public uint  flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] unknown1;
    }

#endregion

#region Nested type: EwfErrorHeaderV1

    /// <summary>EWF v1 error2 section header, 520 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfErrorHeaderV1
    {
        public uint number_of_entries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] unknown1;
        public uint checksum;
    }

#endregion

#region Nested type: EwfErrorEntryV1

    /// <summary>EWF v1 error entry, 8 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfErrorEntryV1
    {
        public uint start_sector;
        public uint number_of_sectors;
    }

#endregion

#region Nested type: EwfErrorHeaderV2

    /// <summary>EWF v2 error section header, 32 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfErrorHeaderV2
    {
        public uint number_of_entries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] unknown1;
        public uint checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] padding;
    }

#endregion

#region Nested type: EwfErrorEntryV2

    /// <summary>EWF v2 error entry, 32 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfErrorEntryV2
    {
        public ulong start_sector;
        public uint  number_of_sectors;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] unknown1;
    }

#endregion

#region Nested type: EwfMd5HashV2

    /// <summary>EWF v2 MD5 hash section, 32 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfMd5HashV2
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] md5_hash;
        public uint checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] padding;
    }

#endregion

#region Nested type: EwfSha1HashV2

    /// <summary>EWF v2 SHA1 hash section, 48 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfSha1HashV2
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] sha1_hash;
        public uint checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] padding;
    }

#endregion
}