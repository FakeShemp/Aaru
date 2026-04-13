// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for Expert Witness Format disk images.
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

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class Ewf
{
    /// <summary>EWF v1 disk image signature: "EVF\x09\x0D\x0A\xFF\x00"</summary>
    static readonly byte[] EVF_SIGNATURE = [0x45, 0x56, 0x46, 0x09, 0x0D, 0x0A, 0xFF, 0x00];

    /// <summary>EWF v1 logical evidence signature: "LVF\x09\x0D\x0A\xFF\x00"</summary>
    static readonly byte[] LVF_SIGNATURE = [0x4C, 0x56, 0x46, 0x09, 0x0D, 0x0A, 0xFF, 0x00];

    /// <summary>EWF v2 disk image signature: "EVF2\x0D\x0A\x81\x00"</summary>
    static readonly byte[] EVF2_SIGNATURE = [0x45, 0x56, 0x46, 0x32, 0x0D, 0x0A, 0x81, 0x00];

    /// <summary>EWF v2 logical evidence signature: "LEF2\x0D\x0A\x81\x00"</summary>
    static readonly byte[] LEF2_SIGNATURE = [0x4C, 0x45, 0x46, 0x32, 0x0D, 0x0A, 0x81, 0x00];

    /// <summary>SMART volume section signature</summary>
    static readonly byte[] SMART_SIGNATURE = [0x53, 0x4D, 0x41, 0x52, 0x54];

    const int SIGNATURE_LENGTH = 8;

    /// <summary>Section type strings for EWF v1</summary>
    const string SECTION_TYPE_HEADER = "header";
    const string SECTION_TYPE_HEADER2 = "header2";
    const string SECTION_TYPE_VOLUME  = "volume";
    const string SECTION_TYPE_DATA    = "data";
    const string SECTION_TYPE_TABLE   = "table";
    const string SECTION_TYPE_TABLE2  = "table2";
    const string SECTION_TYPE_SECTORS = "sectors";
    const string SECTION_TYPE_HASH    = "hash";
    const string SECTION_TYPE_DIGEST  = "digest";
    const string SECTION_TYPE_ERROR2  = "error2";
    const string SECTION_TYPE_SESSION = "session";
    const string SECTION_TYPE_DONE    = "done";
    const string SECTION_TYPE_NEXT    = "next";

    /// <summary>EWF v1 section descriptor size</summary>
    const int SECTION_DESCRIPTOR_V1_SIZE = 76;

    /// <summary>Default sectors per chunk</summary>
    const uint DEFAULT_SECTORS_PER_CHUNK = 64;

    /// <summary>Default bytes per sector</summary>
    const uint DEFAULT_BYTES_PER_SECTOR = 512;

    /// <summary>Default chunk size (64 * 512 = 32768)</summary>
    const uint DEFAULT_CHUNK_SIZE = DEFAULT_SECTORS_PER_CHUNK * DEFAULT_BYTES_PER_SECTOR;

    /// <summary>Compression flag in EWF v1 table entry (most significant bit)</summary>
    const uint TABLE_ENTRY_V1_COMPRESSED_FLAG = 0x80000000;

    /// <summary>Offset mask for EWF v1 table entry (lower 31 bits)</summary>
    const uint TABLE_ENTRY_V1_OFFSET_MASK = 0x7FFFFFFF;

    /// <summary>Maximum cached chunks</summary>
    const int MAX_CACHE_SIZE = 16777216;

    /// <summary>Maximum cached sectors</summary>
    const int MAX_CACHED_SECTORS = MAX_CACHE_SIZE / 512;

    /// <summary>EWF v1 file header size</summary>
    const int FILE_HEADER_V1_SIZE = 13;

    /// <summary>EWF v2 file header size</summary>
    const int FILE_HEADER_V2_SIZE = 32;

    /// <summary>EWF v1 EnCase 5+ volume section data size</summary>
    const int VOLUME_SECTION_SIZE_ENCASE = 1052;

    /// <summary>EWF v1 SMART/old EnCase volume section data size</summary>
    const int VOLUME_SECTION_SIZE_SMART = 94;

    /// <summary>Session entry flag: audio track</summary>
    const uint SESSION_ENTRY_FLAG_IS_AUDIO = 0x00000001;
}