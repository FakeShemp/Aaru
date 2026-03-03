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
//     Contains structures for UltraISO disc images.
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

public sealed partial class UltraISO
{
#region Nested type: IszHeader

    /// <summary>ISZ file header. 48 bytes (base) or 64 bytes (extended), little-endian.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IszHeader
    {
        /// <summary>Signature: "IsZ!" (0x215A7349)</summary>
        public uint signature;
        /// <summary>Header size in bytes</summary>
        public byte headerSize;
        /// <summary>Format version number</summary>
        public byte version;
        /// <summary>Volume serial number</summary>
        public uint volumeSerialNumber;
        /// <summary>Sector size in bytes</summary>
        public ushort sectorSize;
        /// <summary>Total number of sectors in the image</summary>
        public uint totalSectors;
        /// <summary>Encryption type</summary>
        public IszEncryption encryptionType;
        /// <summary>Size of each segment file in bytes (0 = single file)</summary>
        public ulong segmentSize;
        /// <summary>Total number of chunks in the image</summary>
        public uint numChunks;
        /// <summary>Chunk size in bytes (must be a multiple of sector size)</summary>
        public uint chunkSize;
        /// <summary>Chunk pointer length in bytes (size of each chunk table entry)</summary>
        public byte pointerLength;
        /// <summary>Segment number of this file (1-based, max 99)</summary>
        public byte segmentNumber;
        /// <summary>Offset to chunk pointer table (0 = none)</summary>
        public uint chunkTableOffset;
        /// <summary>Offset to segment pointer table (0 = none)</summary>
        public uint segmentTableOffset;
        /// <summary>Offset to compressed data</summary>
        public uint dataOffset;
        /// <summary>Reserved</summary>
        public byte reserved;
        // --- Extended fields (when headerSize >= 64) ---
        /// <summary>CRC32 of uncompressed data (bitwise NOT applied)</summary>
        public uint checksum1;
        /// <summary>Total input data size</summary>
        public uint dataSize;
        /// <summary>Unknown field</summary>
        public uint unknown;
        /// <summary>CRC32 of compressed data (bitwise NOT applied)</summary>
        public uint checksum2;
    }

#endregion

#region Nested type: IszSegment

    /// <summary>Segment table entry. 24 bytes, little-endian.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IszSegment
    {
        /// <summary>Segment size in bytes</summary>
        public ulong size;
        /// <summary>Number of chunks in this segment</summary>
        public uint numChunks;
        /// <summary>First chunk number in this segment</summary>
        public uint firstChunkNumber;
        /// <summary>Offset to first chunk data in this segment file</summary>
        public uint chunkOffset;
        /// <summary>Incomplete chunk data bytes carried over to next segment</summary>
        public uint leftSize;
    }

#endregion

#region Nested type: IszChunk

    /// <summary>In-memory representation of a chunk table entry</summary>
    struct IszChunk
    {
        /// <summary>Chunk compression/data type</summary>
        public IszChunkType type;
        /// <summary>Compressed length in bytes (0 for zero-fill chunks)</summary>
        public uint length;
        /// <summary>Segment index that contains (the start of) this chunk</summary>
        public byte segment;
        /// <summary>Byte offset to the chunk data within the concatenated stream</summary>
        public ulong offset;
        /// <summary>Offset adjusted relative to the start of segment data</summary>
        public ulong adjustedOffset;
    }

#endregion

#region Nested type: IszPart

    /// <summary>In-memory representation of a segment/part file</summary>
    struct IszPart
    {
        /// <summary>Stream for reading from this segment file</summary>
        public Stream stream;
        /// <summary>Byte offset within the segment file where chunk data begins</summary>
        public ulong offset;
        /// <summary>Start offset within the concatenated compressed stream</summary>
        public ulong start;
        /// <summary>End offset (exclusive) within the concatenated compressed stream</summary>
        public ulong end;
    }

#endregion
}