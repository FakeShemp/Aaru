// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for A2R flux images.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class A2R
{
#region Nested type: A2RHeader

    /// <summary>
    ///     Per A2R spec: File header structure (8 bytes total).
    ///     Bytes 0-2: Signature ("A2R")
    ///     Byte 3: Version (0x32 = 2.x, 0x33 = 3.x)
    ///     Byte 4: High bit test (0xFF - ensures no 7-bit data transmission)
    ///     Bytes 5-7: Line test (0x0A 0x0D 0x0A - LF CR LF, file translator test)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct A2RHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] signature;
        public byte version;
        public byte highBitTest; // Should always be 0xFF
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] lineTest; // Should always be 0x0A 0x0D 0x0A
    }

#endregion

#region Nested type: ChunkHeader

    /// <summary>
    ///     Per A2R spec: Chunk header structure (8 bytes total).
    ///     All chunks start with this header: Chunk ID (4 ASCII bytes) + Chunk Size (4 bytes, little-endian).
    ///     Unknown chunks can be safely skipped by reading chunk size and seeking past them.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ChunkHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] chunkId;
        public uint chunkSize;
    }

#endregion

#region Nested type: InfoChunkV2

    /// <summary>
    ///     Per A2R 2.x spec: INFO chunk structure (36 bytes data + 8 bytes header = 44 bytes total).
    ///     Contains fundamental image information. Must be the first chunk in the file.
    ///     Structure: ChunkHeader (8) + Version (1) + Creator (32) + Disk Type (1) + Write Protected (1) + Synchronized (1)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InfoChunkV2
    {
        public ChunkHeader header;
        public byte        version;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] creator;
        public A2RDiskType diskType;
        public byte        writeProtected;
        public byte        synchronized;
    }

#endregion

#region Nested type: InfoChunkV3

    /// <summary>
    ///     Per A2R 3.x spec: INFO chunk structure (37 bytes data + 8 bytes header = 45 bytes total).
    ///     Contains fundamental image information. Must be the first chunk in the file.
    ///     Structure: ChunkHeader (8) + Version (1) + Creator (32) + Drive Type (1) + Write Protected (1) +
    ///     Synchronized (1) + Hard Sector Count (1)
    ///     Version 3.x adds hardSectorCount field and uses driveType instead of diskType.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InfoChunkV3
    {
        public ChunkHeader header;
        public byte        version;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] creator;
        public A2rDriveType driveType;
        public byte         writeProtected;
        public byte         synchronized;
        public byte         hardSectorCount;
    }

#endregion

#region Nested type: RwcpChunkHeader

    /// <summary>
    ///     Per A2R 3.x spec: RWCP (Raw Captures) chunk header structure.
    ///     Structure: ChunkHeader (8) + RWCP Version (1) + Resolution (4) + Reserved (11) = 24 bytes header.
    ///     Resolution is in picoseconds per tick (default 62,500 = 62.5 nanoseconds).
    ///     Each RWCP chunk can only have one resolution value - if resolution changes, a new chunk is needed.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RwcpChunkHeader
    {
        public ChunkHeader header;
        public byte        version;
        public uint        resolution;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public byte[] reserved;
    }

#endregion

#region Nested type: SlvdChunkHeader

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SlvdChunkHeader
    {
        public ChunkHeader header;
        public byte        version;
        public uint        resolution;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public byte[] reserved;
    }

#endregion

#region Nested type: StreamCapture

    /// <summary>
    ///     Internal structure representing a flux capture from A2R file.
    ///     Used for both A2R 2.x (STRM) and 3.x (RWCP) formats.
    ///     For A2R 3.x RWCP: mark = 0x43, captureType = 1 (timing) or 3 (xtiming), includes indexSignals.
    ///     For A2R 2.x STRM: mark not used, captureType = 1 (timing), 2 (bits), or 3 (xtiming), no indexSignals.
    ///     A2R 2.x uses fixed 125ns resolution, A2R 3.x uses configurable resolution per chunk.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StreamCapture
    {
        public byte   mark;
        public byte   captureType;
        public ushort location;
        public byte   numberOfIndexSignals;
        public uint[] indexSignals;
        public uint   captureDataSize;
        public long   dataOffset;
        public uint   resolution;
        public uint   head;
        public ushort track;
        public byte   subTrack;
    }

#endregion

#region Nested type: TrackHeader

    /// <summary>
    ///     Per A2R 3.x spec: Track Entry structure for SLVD (Solved) chunks.
    ///     SLVD chunks contain solved flux data organized by track entries.
    ///     Structure: Mark (1 byte) + Location (2 bytes) + Mirror Distance Outward (1 byte) +
    ///     Mirror Distance Inward (1 byte) + Reserved (6 bytes) + Number of Index Signals (1 byte) +
    ///     Array of Index Signals (variable) + Flux Data Size (4 bytes) + Flux Data (variable)
    ///     Note: SLVD chunks are not yet supported in Aaru, but this structure is defined for future use.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TrackHeader
    {
        /// <summary>
        ///     Per A2R 3.x spec: Mark field indicates entry type.
        ///     0x54 ('T') = Track entry, 0x58 ('X') = End of tracks marker.
        ///     SLVD chunks use 'T' mark, unlike RWCP chunks which use 0x43.
        /// </summary>
        public byte mark;

        /// <summary>
        ///     Per A2R 3.x spec: Location indicates track position.
        ///     For Drive Type 1 (SS 5.25 @ 0.25 step): value is in halfphases/quarter tracks.
        ///     For all other Drive Types: uses formula (cylinder shifted left by 1 bit, plus side).
        ///     Example: 0 = Track 0 Side 0, 1 = Track 0 Side 1, 2 = Track 1 Side 0.
        /// </summary>
        public ushort location;

        /// <summary>
        ///     Per A2R 3.x spec: Mirror Distance Outward indicates how far identical flux data extends
        ///     to neighboring Locations in the outward direction (lower Location numbers).
        ///     Typically only used for Drive Type 1 (quarter-step) or special fat track copy protections.
        ///     Value should be 0 if flux is not mirrored to neighboring Locations.
        /// </summary>
        public byte mirrorDistanceOutward;

        /// <summary>
        ///     Per A2R 3.x spec: Mirror Distance Inward indicates how far identical flux data extends
        ///     to neighboring Locations in the inward direction (higher Location numbers).
        ///     Typically only used for Drive Type 1 (quarter-step) or special fat track copy protections.
        ///     Value should be 0 if flux is not mirrored to neighboring Locations.
        /// </summary>
        public byte mirrorDistanceInward;

        /// <summary>
        ///     Per A2R 3.x spec: Reserved bytes for future use (6 bytes, should be zeroed).
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] reserved;

        /// <summary>
        ///     Per A2R 3.x spec: Number of Index Signals in the following array.
        ///     Can be zero for soft sectored disks that don't use index sensors.
        ///     Hard sectored disks will have multiple signals per disk rotation.
        /// </summary>
        public byte numberOfIndexSignals;

        /// <summary>
        ///     Per A2R 3.x spec: Array of Index Signals.
        ///     Each entry is an absolute timing (in ticks) from the start of the track
        ///     to when an index signal should be triggered.
        ///     If Number of Index Signals is 0, this array doesn't exist.
        ///     If capture starts at index signal, that signal should not be included in the array.
        /// </summary>
        public uint[] indexSignals;

        /// <summary>
        ///     Per A2R 3.x spec: Flux Data Size indicates the number of bytes of flux data
        ///     that follow this track header.
        /// </summary>
        public uint fluxDataSize;
    }

#endregion
}