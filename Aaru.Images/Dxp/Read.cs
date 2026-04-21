// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Read.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Reads Disk eXPress (DXP) disk images.
//
//     Based on the work of Michal Necasek (fdimg).
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
using System.IO;
using System.Linq;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class Dxp
{
    /// <summary>Decompress all tracks and compute the data CRC-32 in the process.</summary>
    ErrorNumber LoadTracks(Stream stream, int tracksInImage, int trackLen, byte spt, byte heads)
    {
        stream.Seek(_dataOffset, SeekOrigin.Begin);

        uint crc      = _crcSeed != 0 ? _crcSeed : DXP_CRC32_SEED;
        var  trackBuf = new byte[trackLen];
        Lh1  lh1      = null;
        Lh5  lh5      = null;

        for(var trkIdx = 0; trkIdx < tracksInImage; trkIdx++)
        {
            if(_compression == DXP_COMPR_NONE)
            {
                if(stream.EnsureRead(trackBuf, 0, trackLen) != trackLen) return ErrorNumber.InvalidArgument;

                crc = DxpCrc32(trackBuf, crc);
            }
            else
            {
                var cmprLenBuf = new byte[2];

                if(stream.EnsureRead(cmprLenBuf, 0, 2) != 2) return ErrorNumber.InvalidArgument;

                var cmprLen = (short)(cmprLenBuf[0] | cmprLenBuf[1] << 8);

                if(cmprLen == 1)
                {
                    int rep = stream.ReadByte();

                    if(rep < 0) return ErrorNumber.InvalidArgument;

                    Array.Fill(trackBuf, (byte)rep);

                    if(_compression == DXP_COMPR_LH1)
                    {
                        Span<byte> one = stackalloc byte[1];
                        one[0] = (byte)rep;
                        crc    = DxpCrc32(one, crc);
                    }
                    else
                        crc = DxpCrc32(trackBuf, crc);
                }
                else if(cmprLen == trackLen)
                {
                    if(stream.EnsureRead(trackBuf, 0, trackLen) != trackLen) return ErrorNumber.InvalidArgument;

                    crc = DxpCrc32(trackBuf, crc);
                }
                else if(cmprLen > 1 && cmprLen < trackLen)
                {
                    var compressed = new byte[cmprLen];

                    if(stream.EnsureRead(compressed, 0, cmprLen) != cmprLen) return ErrorNumber.InvalidArgument;

                    int produced;

                    if(_compression == DXP_COMPR_LH5)
                    {
                        lh5      ??= new Lh5();
                        produced =   lh5.Decode(compressed, cmprLen, trackBuf, trackLen);
                    }
                    else
                    {
                        lh1      ??= new Lh1();
                        produced =   lh1.Decode(compressed, cmprLen, trackBuf, trackLen);
                    }

                    if(produced != trackLen)
                    {
                        AaruLogging.Error(Localization.Dxp_track_decompression_failed_0, trkIdx);

                        return ErrorNumber.InvalidArgument;
                    }

                    // DXP 1.x CRCs the compressed bytes; DXP 2.x CRCs the uncompressed data.
                    crc = _compression == DXP_COMPR_LH1 ? DxpCrc32(compressed, crc) : DxpCrc32(trackBuf, crc);
                }
                else
                {
                    AaruLogging.Error(Localization.Dxp_invalid_compressed_track_length_0, cmprLen);

                    return ErrorNumber.InvalidArgument;
                }
            }

            // Place the decompressed track into the flat disk buffer in sequential order.
            long dstOfs = (long)trkIdx * trackLen;
            Buffer.BlockCopy(trackBuf, 0, _decodedDisk, (int)dstOfs, trackLen);
        }

        // Any tracks beyond tracksInImage are left as the freshly-formatted fill byte (0xF6).
        _dataCrcOk = _crcSeed != 0 && crc == _header.crc_data;

        // Silence analyzers about unused parameters in debug-only paths.
        _ = spt;
        _ = heads;

        return ErrorNumber.NoError;
    }

    void ExtractComment()
    {
        if(_header.desc is not { Length: 200 }) return;

        // An empty comment is all-spaces (or, defensively, all-identical bytes). Preserve exactly when non-empty.
        byte first = _header.desc[0];
        var  empty = true;

        for(var i = 1; i < _header.desc.Length; i++)
        {
            if(_header.desc[i] == first) continue;

            empty = false;

            break;
        }

        if(empty) return;

        // 5 lines of 40 chars, joined with '\n' as in fdimg.
        StringBuilder sb = new(5 * 41);

        for(var ln = 0; ln < 5; ln++)
        {
            sb.Append(Encoding.GetEncoding("ibm437").GetString(_header.desc, ln * 40, 40));
            sb.Append('\n');
        }

        _imageInfo.Comments = sb.ToString();
    }

#region IMediaImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();

        long hdrOfs = FindHeaderOffset(stream);

        if(hdrOfs < 0) return ErrorNumber.InvalidArgument;

        _headerOffset = hdrOfs;

        var rawHeader = new byte[DXP_HEADER_SIZE];
        stream.Seek(hdrOfs, SeekOrigin.Begin);

        if(stream.EnsureRead(rawHeader, 0, DXP_HEADER_SIZE) != DXP_HEADER_SIZE) return ErrorNumber.InvalidArgument;

        _header = Marshal.ByteArrayToStructureLittleEndian<Header>(rawHeader);

        AaruLogging.Debug(MODULE_NAME, "header.signature = 0x{0:X4}", _header.signature);
        AaruLogging.Debug(MODULE_NAME, "header.version = {0}.{1}",    _header.major, _header.minor);
        AaruLogging.Debug(MODULE_NAME, "header.release = 0x{0:X2}",   _header.release);
        AaruLogging.Debug(MODULE_NAME, "header.dsk_typ = {0}",        _header.dsk_typ);
        AaruLogging.Debug(MODULE_NAME, "header.crc_data = 0x{0:X8}",  _header.crc_data);
        AaruLogging.Debug(MODULE_NAME, "header.compr_typ = {0}",      _header.compr_typ);
        AaruLogging.Debug(MODULE_NAME, "header.last_cyl = {0}",       _header.last_cyl);
        AaruLogging.Debug(MODULE_NAME, "header.last_head = {0}",      _header.last_head);
        AaruLogging.Debug(MODULE_NAME, "header.flags = 0x{0:X2}",     _header.flags);
        AaruLogging.Debug(MODULE_NAME, "header.pass_hash = 0x{0:X8}", _header.pass_hash);
        AaruLogging.Debug(MODULE_NAME, "header.crc_hdr = 0x{0:X8}",   _header.crc_hdr);

        _crcSeed = FindCrcSeed(rawHeader);

        if(_crcSeed == 0) AaruLogging.Debug(MODULE_NAME, Localization.Dxp_header_CRC_does_not_match_any_known_seed);

        // Refuse encrypted images; DXP encryption is not implemented.
        if(_header.pass_hash != 0)
        {
            AaruLogging.Error(Localization.Dxp_image_is_password_protected_and_unsupported);

            return ErrorNumber.NotSupported;
        }

        if(_header.dsk_typ >= _formatTable.Length) return ErrorNumber.InvalidArgument;

        (byte cyls, byte heads, byte spt) = _formatTable[_header.dsk_typ];

        // Clamp last_head (DXP 1.04 IBM VP162FIX.EXE records 2 here).
        byte lastHead = _header.last_head > 1 ? (byte)1 : _header.last_head;

        // Total number of tracks stored in the image. Matches fdimg's dxp_open logic.
        int tracksInImage = (_header.last_cyl + 1) * heads - (1 - lastHead);

        if(tracksInImage <= 0 || tracksInImage > cyls * heads)
        {
            AaruLogging.Error(Localization.Dxp_invalid_track_count);

            return ErrorNumber.InvalidArgument;
        }

        _compression = _header.compr_typ;

        if(_compression is not (DXP_COMPR_NONE or DXP_COMPR_LH1 or DXP_COMPR_LH5))
        {
            AaruLogging.Error(Localization.Dxp_unknown_compression_type_0, _compression);

            return ErrorNumber.NotSupported;
        }

        _dataOffset = _headerOffset + DXP_HEADER_SIZE;
        int trackLen = spt * SECTOR_SIZE;

        // Allocate a flat representation of the whole disk and pre-fill missing tracks.
        long totalSectors = (long)cyls * heads * spt;
        _decodedDisk = new byte[totalSectors * SECTOR_SIZE];
        Array.Fill(_decodedDisk, FMT_BYTE);

        ErrorNumber loadErr = LoadTracks(stream, tracksInImage, trackLen, spt, heads);

        if(loadErr != ErrorNumber.NoError) return loadErr;

        // Parse the comment (5 lines of 40 chars, space-padded). Preserve bytes verbatim.
        ExtractComment();

        _imageInfo.Application          = "Disk eXPress";
        _imageInfo.ApplicationVersion   = $"{_header.major}.{_header.minor:D2}";
        _imageInfo.CreationTime         = imageFilter.CreationTime;
        _imageInfo.LastModificationTime = imageFilter.LastWriteTime;
        _imageInfo.MediaTitle           = imageFilter.Filename;
        _imageInfo.ImageSize            = (ulong)(stream.Length - _dataOffset);
        _imageInfo.Sectors              = (ulong)totalSectors;
        _imageInfo.SectorSize           = SECTOR_SIZE;
        _imageInfo.Cylinders            = cyls;
        _imageInfo.Heads                = heads;
        _imageInfo.SectorsPerTrack      = spt;
        _imageInfo.MetadataMediaType    = MetadataMediaType.BlockMedia;

        _imageInfo.MediaType = Geometry.GetMediaType((cyls, heads, spt, SECTOR_SIZE, MediaEncoding.MFM, false));

        AaruLogging.Verbose(Localization.Dxp_image_contains_a_disk_of_type_0, _imageInfo.MediaType);

        AaruLogging.Debug(MODULE_NAME, Localization.Calculated_data_CRC_equals_0_X8_1, _header.crc_data, _dataCrcOk);

        if(!string.IsNullOrEmpty(_imageInfo.Comments))
            AaruLogging.Verbose(Localization.Dxp_comments_0, _imageInfo.Comments);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus)
    {
        sectorStatus = SectorStatus.Dumped;

        return ReadSectors(sectorAddress, negative, 1, out buffer, out _);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, bool negative, uint length, out byte[] buffer,
                                   out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        if(negative) return ErrorNumber.NotSupported;

        if(sectorAddress > _imageInfo.Sectors - 1) return ErrorNumber.OutOfRange;

        if(sectorAddress + length > _imageInfo.Sectors) return ErrorNumber.OutOfRange;

        buffer       = new byte[length * _imageInfo.SectorSize];
        sectorStatus = Enumerable.Repeat(SectorStatus.Dumped, (int)length).ToArray();

        Array.Copy(_decodedDisk,
                   (long)sectorAddress * _imageInfo.SectorSize,
                   buffer,
                   0,
                   length * _imageInfo.SectorSize);

        return ErrorNumber.NoError;
    }

#endregion
}