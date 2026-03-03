// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for PowerISO disc images.
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

using System.IO;
using System.Runtime.InteropServices;

namespace Aaru.Images;

public sealed partial class PowerISO
{
#region Nested type: DaaFormat2Header

    /// <summary>Additional header data used in format version 2 (0x110). 16 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DaaFormat2Header
    {
        /// <summary>PowerISO compression profile: 1 = "better", 2 = "best"</summary>
        public byte profile;
        /// <summary>Chunk table compressed flag</summary>
        public uint chunkTableCompressed;
        /// <summary>Bit sizes for chunk table entries (lower 3 bits = type bit size, upper bits >> 3 = length bit size)</summary>
        public byte chunkTableBitSettings;
        /// <summary>LZMA filter type: 0 = no filter, 1 = BCJ x86</summary>
        public byte lzmaFilter;
        /// <summary>LZMA properties (5 bytes: 4-byte dictionary size + 1-byte lc/lp/pb)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] lzmaProps;
        /// <summary>Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] reserved;
    }

#endregion

#region Nested type: DaaMainHeader

    /// <summary>Main DAA file header. 76 bytes, little-endian. CRC32 over first 72 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DaaMainHeader
    {
        /// <summary>Signature: "DAA" or "GBI" (16-byte null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] signature;
        /// <summary>Offset of chunk table within the main file</summary>
        public uint chunkTableOffset;
        /// <summary>Format version: 0x100 (v1) or 0x110 (v2)</summary>
        public uint formatVersion;
        /// <summary>Offset of chunk data within the main file</summary>
        public uint chunkDataOffset;
        /// <summary>Unknown, always 0x00000001</summary>
        public uint dummy1;
        /// <summary>Unknown, always 0x00000000</summary>
        public uint dummy2;
        /// <summary>Uncompressed size of each chunk (v2: encoded with flags)</summary>
        public uint chunkSize;
        /// <summary>Total uncompressed ISO size in bytes</summary>
        public ulong isoSize;
        /// <summary>Total DAA file size in bytes</summary>
        public ulong daaSize;
        /// <summary>Additional format version 2 data</summary>
        public DaaFormat2Header format2;
        /// <summary>CRC32 over the first 72 bytes of the header</summary>
        public uint crc;
    }

#endregion

#region Nested type: DaaPartHeader

    /// <summary>Part (volume) file header. 40 bytes, little-endian. CRC32 over first 36 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DaaPartHeader
    {
        /// <summary>Signature: "DAA VOL" or "GBI VOL" (16-byte null-padded)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] signature;
        /// <summary>Offset of chunk data within this part file</summary>
        public uint chunkDataOffset;
        /// <summary>Additional format version 2 data</summary>
        public DaaFormat2Header format2;
        /// <summary>CRC32 over the first 36 bytes of the header</summary>
        public uint crc;
    }

#endregion

#region Nested type: DaaDescriptorHeader

    /// <summary>Descriptor block header. 8 bytes, little-endian.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DaaDescriptorHeader
    {
        /// <summary>Descriptor type</summary>
        public uint type;
        /// <summary>Descriptor length (includes type and length fields themselves)</summary>
        public uint length;
    }

#endregion

#region Nested type: DaaChunk

    /// <summary>In-memory representation of a chunk table entry</summary>
    struct DaaChunk
    {
        /// <summary>Byte offset of this chunk's compressed data within the concatenated part streams</summary>
        public ulong offset;
        /// <summary>Compressed length in bytes</summary>
        public uint length;
        /// <summary>Compression method used for this chunk</summary>
        public DaaCompressionType compression;
    }

#endregion

#region Nested type: DaaPart

    /// <summary>In-memory representation of a part file entry</summary>
    struct DaaPart
    {
        /// <summary>Stream for reading from this part file</summary>
        public Stream stream;
        /// <summary>Byte offset within the part file where chunk data begins</summary>
        public ulong offset;
        /// <summary>Start offset within the concatenated compressed stream</summary>
        public ulong start;
        /// <summary>End offset (exclusive) within the concatenated compressed stream</summary>
        public ulong end;
    }

#endregion
}