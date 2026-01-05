// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Write.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Writes A2R flux images.
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class A2R
{
#region IWritableFluxImage Members

    /// <inheritdoc />
    public ErrorNumber WriteFluxCapture(ulong  indexResolution, ulong dataResolution, byte[] indexBuffer,
                                        byte[] dataBuffer, uint head, ushort track, byte subTrack, uint captureIndex)
    {
        if(!IsWriting)
        {
            ErrorMessage = Localization.Tried_to_write_on_a_non_writable_image;

            return ErrorNumber.WriteError;
        }

        // Per A2R 3.x spec: An RWCP chunk can only have one capture resolution per chunk.
        // If the resolution changes, we need to create a new RWCP chunk.

        if(_currentResolution != dataResolution)
        {
            if(IsWritingRwcps)
            {
                CloseRwcpChunk();

                _writingStream.Seek(_currentRwcpStart, SeekOrigin.Begin);
                WriteRwcpHeader();

                _currentRwcpStart     = _writingStream.Length;
                _currentCaptureOffset = 16;
            }

            IsWritingRwcps = true;

            _currentResolution = (uint)dataResolution;
        }

        _writingStream.Seek(_currentRwcpStart + _currentCaptureOffset + Marshal.SizeOf<ChunkHeader>(),
                            SeekOrigin.Begin);

        // Per A2R 3.x spec: RWCP chunk captures use mark 0x43
        _writingStream.WriteByte(0x43);

        // Per A2R 3.x spec: Capture type 1 = timing (~1.25 revolutions), 3 = xtiming (2.25+ revolutions)
        // Type 2 = bits (legacy, deprecated)
        _writingStream.WriteByte(IsCaptureTypeTiming(dataResolution, dataBuffer) ? (byte)1 : (byte)3);

        // Per A2R 3.x spec: Location uses formula ((cylinder << 1) + side) for most drive types
        // For quarter-step drives (SS 5.25 @ 0.25 step), location is in halfphases
        _writingStream.Write(BitConverter.GetBytes((ushort)HeadTrackSubToA2RLocation(head,
                                                                                     track,
                                                                                     subTrack,
                                                                                     _infoChunkV3.driveType)),
                                                                                     0,
                                                                                     2);

        // Per A2R 3.x spec: Index signals are absolute timings from start of track
        // If capture starts at index signal, that signal should not be included in the array
        List<uint> a2RIndices = FluxRepresentationsToUInt32List(indexBuffer);

        // Per A2R 3.x spec: synchronized indicates if cross-track sync/index was used during imaging
        // If the first index signal is 0, the capture started at index, so synchronized should be 1
        if(!_firstCaptureProcessed && a2RIndices.Count > 0)
        {
            _infoChunkV3.synchronized = a2RIndices[0] == 0 ? (byte)1 : (byte)0;
            _firstCaptureProcessed = true;
        }

        if(a2RIndices.Count > 0 && a2RIndices[0] == 0) a2RIndices.RemoveAt(0);

        _writingStream.WriteByte((byte)a2RIndices.Count);

        long previousIndex = 0;

        foreach(uint index in a2RIndices)
        {
            _writingStream.Write(BitConverter.GetBytes(index + previousIndex), 0, 4);
            previousIndex += index;
        }

        _writingStream.Write(BitConverter.GetBytes(dataBuffer.Length), 0, 4);
        _writingStream.Write(dataBuffer,                               0, dataBuffer.Length);

        _currentCaptureOffset += (uint)(9 + a2RIndices.Count * 4 + dataBuffer.Length);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber WriteFluxIndexCapture(ulong resolution, byte[] index, uint head, ushort track, byte subTrack,
                                             uint  captureIndex) => ErrorNumber.NoError;

    /// <inheritdoc />
    public ErrorNumber WriteFluxDataCapture(ulong resolution, byte[] data, uint head, ushort track, byte subTrack,
                                            uint  captureIndex) => ErrorNumber.NoError;

    /// <inheritdoc />
    public bool Create(string path,            MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                       uint   negativeSectors, uint      overflowSectors, uint sectorSize)
    {
        try
        {
            _writingStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch(IOException ex)
        {
            ErrorMessage = string.Format(Localization.Could_not_create_new_image_file_exception_0, ex.Message);
            AaruLogging.Exception(ex, Localization.Could_not_create_new_image_file_exception_0, ex.Message);

            return false;
        }

        IsWriting    = true;
        ErrorMessage = null;
        _firstCaptureProcessed = false;

        // Per A2R 3.x spec: File header is 8 bytes
        // Bytes 0-3: "A2R3" (0x33523241 little-endian) - version 3.x
        // Byte 4: 0xFF (high bit test - ensures no 7-bit data transmission)
        // Bytes 5-7: 0x0A 0x0D 0x0A (LF CR LF - file translator test)
        _header.signature   = "A2R"u8.ToArray();
        _header.version     = 0x33; // A2R 3.x format
        _header.highBitTest = 0xFF;
        _header.lineTest    = "\n\r\n"u8.ToArray();

        _infoChunkV3.driveType = mediaType switch
                                 {
                                     MediaType.DOS_525_DS_DD_9 => A2rDriveType.DS_525_40trk,
                                     MediaType.DOS_35_HD       => A2rDriveType.DS_35_80trk,
                                     MediaType.DOS_525_HD      => A2rDriveType.DS_525_80trk,
                                     MediaType.AppleSonyDS     => A2rDriveType.DS_35_80trk_appleCLV,
                                     MediaType.Apple32SS       => A2rDriveType.SS_525_40trk_quarterStep,
                                     MediaType.Unknown         => A2rDriveType.DS_35_80trk,
                                     _                         => A2rDriveType.DS_35_80trk
                                 };

        return true;
    }

    /// <inheritdoc />
    public bool Close()
    {
        if(!IsWriting)
        {
            ErrorMessage = Localization.Image_is_not_opened_for_writing;

            return false;
        }

        _writingStream.Seek(0, SeekOrigin.Begin);

        _writingStream.Write(_header.signature, 0, 3);
        _writingStream.WriteByte(_header.version);
        _writingStream.WriteByte(_header.highBitTest);
        _writingStream.Write(_header.lineTest, 0, 3);

        // Per A2R 3.x spec: First chunk must be an INFO chunk (at byte 8, immediately after header)
        WriteInfoChunk();

        _writingStream.Seek(_currentRwcpStart, SeekOrigin.Begin);

        WriteRwcpHeader();

        _writingStream.Seek(0, SeekOrigin.End);
        CloseRwcpChunk();

        WriteMetaChunk();

        _writingStream.Flush();
        _writingStream.Close();

        IsWriting    = false;
        ErrorMessage = "";

        return true;
    }

    /// <inheritdoc />
    public bool SetImageInfo(ImageInfo imageInfo)
    {
        _meta = new Dictionary<string, string>();

        _infoChunkV3.header.chunkId   = _infoChunkSignature;
        _infoChunkV3.header.chunkSize = 37;
        _infoChunkV3.version          = 1;

        _infoChunkV3.creator =
            Encoding.UTF8.GetBytes($"Aaru v{typeof(A2R).Assembly.GetName().Version?.ToString()}".PadRight(32, ' '));

        // Per A2R 3.x spec: writeProtected indicates if floppy is write protected (1 = protected)
        // Check if Floppy_WriteProtection media tag is available, otherwise default to 1 (write protected)
        // as a safe default for archival images
        // Initialize _mediaTags if not already initialized
        _mediaTags ??= [];

        if(_mediaTags.TryGetValue(MediaTagType.Floppy_WriteProtection, out byte[] writeProtectionTag))
        {
            // Boolean value: non-zero byte = true (write protected), 0 = false (not write protected)
            _infoChunkV3.writeProtected = writeProtectionTag is { Length: > 0 } && writeProtectionTag[0] != 0 ? (byte)1 : (byte)0;
        }
        else
        {
            _infoChunkV3.writeProtected = 1; // Default to write protected
        }

        // Per A2R 3.x spec: synchronized indicates if cross-track sync/index was used during imaging (1 = synchronized)
        // Will be set based on first capture's index signals in WriteFluxCapture
        // Default to 0 (will be updated when first capture is written)
        _infoChunkV3.synchronized = 0;
        _firstCaptureProcessed = false;

        // Per A2R 3.x spec: hardSectorCount indicates number of hard sectors (0 = soft sectored)
        // Default to 0 (soft sectored) as most floppies are soft sectored
        _infoChunkV3.hardSectorCount = 0;

        // Per A2R 3.x spec: META chunk contains tab-delimited UTF-8 key-value pairs
        // Standard metadata keys: title, subtitle, publisher, developer, copyright, version, language,
        // requires_platform, requires_machine, requires_ram, notes, side, side_name, contributor, image_date
        _meta.Add("image_date", DateTime.Now.ToString("O"));

        if(!string.IsNullOrEmpty(imageInfo.MediaTitle))
            _meta.Add("title", imageInfo.MediaTitle);

        if(!string.IsNullOrEmpty(imageInfo.Version))
            _meta.Add("version", imageInfo.Version);

        if(!string.IsNullOrEmpty(imageInfo.Comments))
            _meta.Add("notes", imageInfo.Comments);

        if(!string.IsNullOrEmpty(imageInfo.Creator))
            _meta.Add("contributor", imageInfo.Creator);

        return true;
    }

    /// <inheritdoc />
    public bool SetGeometry(uint cylinders, uint heads, uint sectorsPerTrack) => true;

    /// <inheritdoc />
    public bool WriteSectorTag(byte[] data, ulong sectorAddress, bool negative, SectorTagType tag) => false;

    /// <inheritdoc />
    public bool WriteSectorsTag(byte[] data, ulong sectorAddress, bool negative, uint length, SectorTagType tag) =>
        false;

    /// <inheritdoc />
    public bool SetDumpHardware(List<DumpHardware> dumpHardware) => false;

    /// <inheritdoc />
    public bool SetMetadata(Metadata metadata) => false;

    /// <inheritdoc />
    public bool WriteMediaTag(byte[] data, MediaTagType tag)
    {
        if(!SupportedMediaTags.Contains(tag))
        {
            ErrorMessage = $"Tried to write unsupported media tag {tag}.";

            return false;
        }

        _mediaTags ??= [];

        if(_mediaTags.ContainsKey(tag)) _mediaTags.Remove(tag);

        _mediaTags.Add(tag, data);

        // If this is the write protection tag, update the INFO chunk value
        if(tag == MediaTagType.Floppy_WriteProtection)
        {
            // Boolean value: non-zero byte = true (write protected), 0 = false (not write protected)
            _infoChunkV3.writeProtected = data is { Length: > 0 } && data[0] != 0 ? (byte)1 : (byte)0;
        }

        return true;
    }

    /// <inheritdoc />
    public bool WriteSector(byte[] data, ulong sectorAddress, bool negative, SectorStatus sectorStatus) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectorLong(byte[] data, ulong sectorAddress, bool negative, SectorStatus sectorStatus) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectors(byte[]         data, ulong sectorAddress, bool negative, uint length,
                             SectorStatus[] sectorStatus) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectorsLong(byte[]         data, ulong sectorAddress, bool negative, uint length,
                                 SectorStatus[] sectorStatus) => throw new NotImplementedException();

#endregion

    /// <summary>
    ///     Writes the header to an RWCP chunk, up to and including the reserved bytes, to stream.
    ///     Per A2R 3.x spec: RWCP (Raw Captures) chunk contains raw flux data streams.
    ///     Chunk structure: ChunkHeader (8 bytes) + RWCP Version (1 byte) + Resolution (4 bytes) + Reserved (11 bytes)
    /// </summary>
    /// <returns>Error number</returns>
    ErrorNumber WriteRwcpHeader()
    {
        if(!IsWriting)
        {
            ErrorMessage = Localization.Tried_to_write_on_a_non_writable_image;

            return ErrorNumber.WriteError;
        }

        _writingStream.Write(_rwcpChunkSignature,                              0, 4);
        _writingStream.Write(BitConverter.GetBytes(_currentCaptureOffset + 1), 0, 4);
        _writingStream.WriteByte(1);
        _writingStream.Write(BitConverter.GetBytes(_currentResolution), 0, 4);

        var reserved = new byte[11];

        _writingStream.Write(reserved, 0, 11);

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Writes the entire INFO chunk to stream.
    ///     Per A2R 3.x spec: INFO chunk must be the first chunk in the file (after the 8-byte header).
    ///     Contains fundamental information about the image: creator, drive type, write protection, etc.
    /// </summary>
    /// <returns>Error number</returns>
    ErrorNumber WriteInfoChunk()
    {
        if(!IsWriting)
        {
            ErrorMessage = Localization.Tried_to_write_on_a_non_writable_image;

            return ErrorNumber.WriteError;
        }

        _writingStream.Write(_infoChunkV3.header.chunkId,                          0, 4);
        _writingStream.Write(BitConverter.GetBytes(_infoChunkV3.header.chunkSize), 0, 4);
        _writingStream.WriteByte(_infoChunkV3.version);
        _writingStream.Write(_infoChunkV3.creator, 0, 32);
        _writingStream.WriteByte((byte)_infoChunkV3.driveType);
        _writingStream.WriteByte(_infoChunkV3.writeProtected);
        _writingStream.WriteByte(_infoChunkV3.synchronized);
        _writingStream.WriteByte(_infoChunkV3.hardSectorCount);

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Writes the entire META chunk to stream.
    ///     Per A2R 3.x spec: META chunk contains tab-delimited UTF-8 metadata (key-value pairs).
    ///     Each row ends with linefeed (\n), columns separated by tab (\t).
    ///     Standard keys include: title, publisher, developer, copyright, version, language, etc.
    /// </summary>
    /// <returns>Error number</returns>
    ErrorNumber WriteMetaChunk()
    {
        if(!IsWriting)
        {
            ErrorMessage = Localization.Tried_to_write_on_a_non_writable_image;

            return ErrorNumber.WriteError;
        }

        _writingStream.Write(_metaChunkSignature, 0, 4);

        // Per A2R 3.x spec: Metadata is tab-delimited UTF-8, no BOM
        // Format: key\tvalue\n for each entry, final \n at end
        // Values cannot contain tab, linefeed, or pipe characters (except as delimiters)
        byte[] metaString = Encoding.UTF8.GetBytes(_meta.Select(static m => $"{m.Key}\t{m.Value}")
                                                        .Aggregate(static (concat, str) => $"{concat}\n{str}") +
                                                   '\n');

        _writingStream.Write(BitConverter.GetBytes((uint)metaString.Length), 0, 4);
        _writingStream.Write(metaString,                                     0, metaString.Length);

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Writes the closing byte to an RWCP chunk signaling its end, to stream.
    ///     Per A2R 3.x spec: RWCP chunk ends with 0x58 byte (mark value for end of tracks).
    /// </summary>
    /// <returns>Error number</returns>
    ErrorNumber CloseRwcpChunk()
    {
        if(!IsWriting)
        {
            ErrorMessage = Localization.Tried_to_write_on_a_non_writable_image;

            return ErrorNumber.WriteError;
        }

        _writingStream.WriteByte(0x58);

        return ErrorNumber.NoError;
    }
}