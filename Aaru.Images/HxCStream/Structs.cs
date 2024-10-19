// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Rebecca Wallander <sakcheen+github@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for HxC Stream flux images.
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class HxCStream
{
#region Nested type: HxCStreamChunkHeader

    /// <summary>
    ///     Represents a chunk header in an HxCStream file. A chunk is a container that holds
    ///     multiple packet blocks (metadata, IO stream, flux stream) and ends with a CRC32 checksum.
    ///     Each track file can contain multiple chunks, allowing data to be split across chunks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HxCStreamChunkHeader
    {
        /// <summary>Chunk signature, always "CHKH" (0x43, 0x48, 0x4B, 0x48)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>Total size of the chunk including header, all packet blocks, and CRC32 (4 bytes)</summary>
        public uint size;
        /// <summary>Packet number, used for sequencing chunks</summary>
        public uint packetNumber;
    }

#endregion

#region Nested type: HxCStreamChunkBlockHeader

    /// <summary>
    ///     Base header structure for packet blocks within a chunk. This is the common header
    ///     that all packet types share. The actual packet headers extend this with additional fields.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HxCStreamChunkBlockHeader
    {
        /// <summary>Packet type identifier (0x0 = metadata, 0x1 = IO stream, 0x2 = flux stream)</summary>
        public uint type;
        /// <summary>Size of the packet payload data (excluding this header)</summary>
        public uint payloadSize;
    }

#endregion

#region Nested type: HxCStreamMetadataHeader

    /// <summary>
    ///     Header for a metadata packet block (type 0x0). Contains text-based metadata information
    ///     such as sample rate, IO channel names, etc. The payload is UTF-8 encoded text.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HxCStreamMetadataHeader
    {
        /// <summary>Packet type, always 0x0 for metadata</summary>
        public uint type;
        /// <summary>Size of the metadata text payload in bytes</summary>
        public uint payloadSize;
    }

#endregion

#region Nested type: HxCStreamPackedIoHeader

    /// <summary>
    ///     Header for a packed IO stream packet block (type 0x1). Contains LZ4-compressed
    ///     16-bit IO values representing index signals, write protect status, and other
    ///     IO channel states. Also, see <see cref="IoStreamState" /> for more information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HxCStreamPackedIoHeader
    {
        /// <summary>Packet type, always 0x1 for packed IO stream</summary>
        public uint type;
        /// <summary>Total size of the packet including this header and packed data</summary>
        public uint payloadSize;
        /// <summary>Size of the LZ4-compressed data in bytes</summary>
        public uint packedSize;
        /// <summary>Size of the uncompressed data in bytes (should be even, as it's 16-bit values)</summary>
        public uint unpackedSize;
    }

#endregion

#region Nested type: HxCStreamPackedStreamHeader

    /// <summary>
    ///     Header for a packed flux stream packet block (type 0x2). Contains LZ4-compressed
    ///     variable-length encoded flux pulse data. The pulses represent time intervals between
    ///     flux reversals, encoded using a variable-length encoding scheme to save space.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HxCStreamPackedStreamHeader
    {
        /// <summary>Packet type, always 0x2 for packed flux stream</summary>
        public uint type;
        /// <summary>Total size of the packet including this header and packed data</summary>
        public uint payloadSize;
        /// <summary>Size of the LZ4-compressed data in bytes</summary>
        public uint packedSize;
        /// <summary>Size of the uncompressed variable-length encoded pulse data in bytes</summary>
        public uint unpackedSize;
        /// <summary>Number of flux pulses in this packet (may be updated during decoding)</summary>
        public uint numberOfPulses;
    }

#endregion

#region Nested type: IoStreamState

    /// <summary>
    ///     Represents the decoded state of a 16-bit IO stream value from an HxCStream file.
    ///     The IO stream contains signals sampled at regular intervals. Currently, only the
    ///     index signal (bit 0) and write protect (bit 5) are used. The raw value is preserved
    ///     for future extensions and can be accessed to check other bits if needed.
    /// </summary>
    public struct IoStreamState
    {
        /// <summary>
        ///     Raw 16-bit IO value. Use this to access other bits that may be defined in the future.
        ///     Bits can be checked using: (RawValue &amp; bitMask) != 0
        /// </summary>
        public ushort RawValue { get; set; }

        /// <summary>Index signal state (bit 0). True when index signal is active/high.</summary>
        public bool IndexSignal => (RawValue & 0x01) != 0;

        /// <summary>Write protect state (bit 5). True when write protect is active.</summary>
        public bool WriteProtect => (RawValue & 0x20) != 0;

        /// <summary>
        ///     Creates an IoStreamState from a raw 16-bit value
        /// </summary>
        /// <param name="rawValue">The raw 16-bit IO stream value</param>
        /// <returns>Decoded IO stream state</returns>
        public static IoStreamState FromRawValue(ushort rawValue) => new() { RawValue = rawValue };
    }

#endregion

#region Nested type: TrackCapture

    /// <summary>
    ///     Represents a complete flux capture for a single track. Contains the decoded flux pulse
    ///     data, index signal positions, and resolution information. This is the internal representation
    ///     used to store parsed track data from HxCStream files.
    /// </summary>
    public class TrackCapture
    {
        public uint   head;
        public ushort track;
        /// <summary>
        ///     Resolution (sample rate) of the flux capture in picoseconds.
        ///     Default is 40,000 picoseconds (40 nanoseconds = 25 MHz sample rate).
        ///     Can be 20,000 picoseconds (20 nanoseconds = 50 MHz sample rate) if metadata indicates 50 MHz.
        /// </summary>
        public uint   resolution;
        /// <summary>
        ///     Array of flux pulse intervals in ticks. Each value represents the time interval
        ///     between flux reversals, measured in resolution units (picoseconds).
        /// </summary>
        public uint[] fluxPulses;
        /// <summary>
        ///     Array of index positions. Each value is an index into the fluxPulses array
        ///     indicating where an index signal occurs. These positions are extracted from
        ///     the IO stream (bit 0 transitions) and mapped to flux stream positions.
        /// </summary>
        public uint[] indexPositions;
    }

#endregion
}