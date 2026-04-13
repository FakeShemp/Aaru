// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : EWF logical evidence plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for Expert Witness Format logical evidence files.
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

namespace Aaru.Archives;

public sealed partial class EwfArchive
{
#region Internal types

    /// <summary>Parsed file entry from the ltree</summary>
    sealed class EwfFileEntry
    {
        public long     Id           { get; set; }
        public string   Name         { get; set; }
        public string   FullPath     { get; set; }
        public bool     IsDirectory  { get; set; }
        public long     Size         { get; set; }
        public long     DataOffset   { get; set; }
        public long     DataSize     { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ModifyTime   { get; set; }
        public DateTime AccessTime   { get; set; }
        public string   Md5Hash      { get; set; }
        public string   Sha1Hash     { get; set; }
        public uint     Flags        { get; set; }
    }

#endregion

#region On-disk structures

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfFileHeaderV1
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] signature;
        public byte   fields_start;
        public ushort segment_number;
        public ushort fields_end;
    }

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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfTableHeaderV1
    {
        public uint  number_of_entries;
        public uint  padding1;
        public ulong base_offset;
        public uint  padding2;
        public uint  checksum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfTableEntryV2
    {
        public ulong chunk_data_offset;
        public uint  chunk_data_size;
        public uint  chunk_data_flags;
    }

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

    /// <summary>Ltree section header, 48 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EwfLtreeHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] integrity_hash;
        public ulong data_size;
        public uint  checksum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] unknown1;
    }

#endregion
}