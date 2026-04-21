// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Open.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Opens Sydex CopyQM+ Self-eXtracting Disk (SXD) images.
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
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class SXD
{
#region IMediaImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();

        long hdrOfs = FindHeaderOffset(stream);

        if(hdrOfs < 0) return ErrorNumber.InvalidArgument;

        _headerOffset = hdrOfs;

        var rawHeader = new byte[SXD_HEADER_SIZE];
        stream.Seek(hdrOfs, SeekOrigin.Begin);

        if(stream.EnsureRead(rawHeader, 0, SXD_HEADER_SIZE) != SXD_HEADER_SIZE) return ErrorNumber.InvalidArgument;

        _header = Marshal.ByteArrayToStructureLittleEndian<Header>(rawHeader);

        ushort computedHdrCrc = SxdCrc16(rawHeader.AsSpan(0, SXD_CRC_HDR_COVER));

        AaruLogging.Debug(MODULE_NAME, "header.trk_len = {0}",          _header.trk_len);
        AaruLogging.Debug(MODULE_NAME, "header.n_trk_sec = {0}",        _header.n_trk_sec);
        AaruLogging.Debug(MODULE_NAME, "header.n_heads = {0}",          _header.n_heads);
        AaruLogging.Debug(MODULE_NAME, "header.n_cyl_dsk = {0}",        _header.n_cyl_dsk);
        AaruLogging.Debug(MODULE_NAME, "header.n_cyl_img = {0}",        _header.n_cyl_img);
        AaruLogging.Debug(MODULE_NAME, "header.first_sid = {0}",        _header.first_sid);
        AaruLogging.Debug(MODULE_NAME, "header.interleave = {0}",       _header.interleave);
        AaruLogging.Debug(MODULE_NAME, "header.skew = {0}",             _header.skew);
        AaruLogging.Debug(MODULE_NAME, "header.sec_sz_code = {0}",      _header.sec_sz_code);
        AaruLogging.Debug(MODULE_NAME, "header.drv_typ = {0} ({1})",    _header.drv_typ, (SxdDriveType)_header.drv_typ);
        AaruLogging.Debug(MODULE_NAME, "header.density = {0} ({1})",    _header.density, DensityName(_header.density));
        AaruLogging.Debug(MODULE_NAME, "header.pwd_crc = 0x{0:X4}",     _header.pwd_crc);
        AaruLogging.Debug(MODULE_NAME, "header.pwd_hash = 0x{0:X4}",    _header.pwd_hash);
        AaruLogging.Debug(MODULE_NAME, "header.comment_len = {0}",      _header.comment_len);
        AaruLogging.Debug(MODULE_NAME, "header.crc_comment = 0x{0:X4}", _header.crc_comment);

        AaruLogging.Debug(MODULE_NAME,
                          "header.unk0 = {0:X2} {1:X2} {2:X2} {3:X2}",
                          _header.unk0[0],
                          _header.unk0[1],
                          _header.unk0[2],
                          _header.unk0[3]);

        AaruLogging.Debug(MODULE_NAME, "header.dos_time = 0x{0:X4}", _header.dos_time);
        AaruLogging.Debug(MODULE_NAME, "header.dos_date = 0x{0:X4}", _header.dos_date);

        AaruLogging.Debug(MODULE_NAME,
                          "header.crc_hdr = 0x{0:X4} (computed 0x{1:X4})",
                          _header.crc_hdr,
                          computedHdrCrc);

        if(computedHdrCrc != _header.crc_hdr)
        {
            AaruLogging.Error(Localization.Sxd_header_CRC_mismatch);

            return ErrorNumber.InvalidArgument;
        }

        // Password-protected images cannot be decoded; the scheme is not documented.
        if(_header.pwd_crc != 0 || _header.pwd_hash != 0)
        {
            AaruLogging.Error(Localization.Sxd_image_is_password_protected_and_unsupported);

            return ErrorNumber.NotSupported;
        }

        byte spt     = _header.n_trk_sec;
        byte heads   = _header.n_heads;
        byte cylsDsk = _header.n_cyl_dsk;
        byte cylsImg = _header.n_cyl_img;

        // Basic sanity checks matching fdimg's sxd_open().
        if(spt == 0 || heads is 0 or > 2 || cylsDsk == 0 || cylsImg == 0 || cylsDsk > NTRK_MAX || cylsImg > cylsDsk)
        {
            AaruLogging.Error(Localization.Sxd_image_has_invalid_geometry);

            return ErrorNumber.InvalidArgument;
        }

        if(_header.trk_len != spt * SECTOR_SIZE)
        {
            AaruLogging.Error(Localization.Sxd_inconsistent_track_length);

            return ErrorNumber.InvalidArgument;
        }

        int trackLen = spt * SECTOR_SIZE;

        // Read optional comment following the header.
        long dataOffset = _headerOffset + SXD_HEADER_SIZE + _header.comment_len;

        if(_header.comment_len > 0)
        {
            var commentBytes = new byte[_header.comment_len];
            stream.Seek(_headerOffset + SXD_HEADER_SIZE, SeekOrigin.Begin);

            if(stream.EnsureRead(commentBytes, 0, _header.comment_len) != _header.comment_len)
                return ErrorNumber.InvalidArgument;

            ushort commentCrc = SxdCrc16(commentBytes);

            if(commentCrc == _header.crc_comment)
            {
                // Replace embedded nulls with newlines, mirroring fdimg.
                for(var i = 0; i < commentBytes.Length; i++)
                {
                    if(commentBytes[i] == 0) commentBytes[i] = (byte)'\n';
                }

                _imageInfo.Comments = Encoding.GetEncoding("ibm437").GetString(commentBytes);
            }
            else
                AaruLogging.Debug(MODULE_NAME, Localization.Sxd_comment_CRC_mismatch_discarded);
        }

        // Allocate flat disk buffer sized to the full physical disk and pre-fill with the freshly-formatted byte.
        long totalSectors = (long)cylsDsk * heads * spt;
        _decodedDisk = new byte[totalSectors * SECTOR_SIZE];
        Array.Fill(_decodedDisk, FMT_BYTE);

        // Decompress every track stored in the image into its slot in the flat buffer.
        int tracksInImage = cylsImg * heads;
        Lzh lzh           = null;
        var cmprBuf       = new byte[trackLen + 128];
        var allCrcsOk     = true;

        stream.Seek(dataOffset, SeekOrigin.Begin);

        for(var trkIdx = 0; trkIdx < tracksInImage; trkIdx++)
        {
            long dstOfs = (long)trkIdx * trackLen;

            ErrorNumber err = DecompressTrack(stream,
                                              _decodedDisk,
                                              (int)dstOfs,
                                              trackLen,
                                              cmprBuf,
                                              ref lzh,
                                              out bool trackCrcOk);

            if(err != ErrorNumber.NoError)
            {
                AaruLogging.Error(Localization.Sxd_track_decompression_failed_0, trkIdx);

                return err;
            }

            if(!trackCrcOk) allCrcsOk = false;
        }

        _dataCrcOk = allCrcsOk;

        // Populate ImageInfo.
        _imageInfo.Application = "CopyQM+ / MAKESXD";
        _imageInfo.CreationTime = DosDateTimeToUtc(_header.dos_date, _header.dos_time, imageFilter.CreationTime);
        _imageInfo.LastModificationTime = _imageInfo.CreationTime;
        _imageInfo.MediaTitle = imageFilter.Filename;
        _imageInfo.ImageSize = (ulong)(stream.Length - dataOffset);
        _imageInfo.Sectors = (ulong)totalSectors;
        _imageInfo.SectorSize = SECTOR_SIZE;
        _imageInfo.Cylinders = cylsDsk;
        _imageInfo.Heads = heads;
        _imageInfo.SectorsPerTrack = spt;
        _imageInfo.MetadataMediaType = MetadataMediaType.BlockMedia;
        _imageInfo.MediaType = Geometry.GetMediaType((cylsDsk, heads, spt, SECTOR_SIZE, MediaEncoding.MFM, false));

        AaruLogging.Verbose(Localization.Sxd_image_contains_a_disk_of_type_0, _imageInfo.MediaType);

        if(!string.IsNullOrEmpty(_imageInfo.Comments))
            AaruLogging.Verbose(Localization.Sxd_comments_0, _imageInfo.Comments);

        AaruLogging.Debug(MODULE_NAME, Localization.Sxd_all_track_CRCs_match_0, _dataCrcOk);

        return ErrorNumber.NoError;
    }

#endregion

    /// <summary>Read, decompress, and CRC-validate one track from <paramref name="stream" /> into <paramref name="outBuf" />.</summary>
    ErrorNumber DecompressTrack(Stream   stream, byte[] outBuf, int outOfs, int trackLen, byte[] cmprBuf, ref Lzh lzh,
                                out bool crcOk)
    {
        crcOk = false;

        var header = new byte[4];

        if(stream.EnsureRead(header, 0, 4) != 4) return ErrorNumber.InvalidArgument;

        var crcStored = (ushort)(header[0] | header[1] << 8);
        var cmprLen   = (short)(header[2]  | header[3] << 8);

        int cmprLenAbs = cmprLen < 0 ? -cmprLen : cmprLen;

        if(cmprLenAbs > trackLen + 2)
        {
            AaruLogging.Error(Localization.Sxd_track_compressed_length_out_of_range_0, cmprLen);

            return ErrorNumber.InvalidArgument;
        }

        if(stream.EnsureRead(cmprBuf, 0, cmprLenAbs) != cmprLenAbs) return ErrorNumber.InvalidArgument;

        if(cmprLen < 0)
        {
            // CopyQM-style signed-RLE (inline).
            var cmprPos      = 0;
            var bytesWritten = 0;

            while(bytesWritten < trackLen)
            {
                if(cmprPos + 2 > cmprLenAbs) return ErrorNumber.InvalidArgument;

                var repCnt = (short)(cmprBuf[cmprPos] | cmprBuf[cmprPos + 1] << 8);
                cmprPos += 2;

                if(repCnt > 0)
                {
                    if(bytesWritten + repCnt > trackLen) return ErrorNumber.InvalidArgument;
                    if(cmprPos      + repCnt > cmprLenAbs) return ErrorNumber.InvalidArgument;

                    Buffer.BlockCopy(cmprBuf, cmprPos, outBuf, outOfs + bytesWritten, repCnt);
                    cmprPos      += repCnt;
                    bytesWritten += repCnt;
                }
                else if(repCnt < 0)
                {
                    int count = -repCnt;

                    if(bytesWritten + count > trackLen) return ErrorNumber.InvalidArgument;
                    if(cmprPos      + 1     > cmprLenAbs) return ErrorNumber.InvalidArgument;

                    byte b = cmprBuf[cmprPos++];

                    for(var i = 0; i < count; i++) outBuf[outOfs + bytesWritten + i] = b;

                    bytesWritten += count;
                }
                else
                {
                    // Zero run-count is invalid per fdimg.
                    return ErrorNumber.InvalidArgument;
                }
            }
        }
        else
        {
            // LH1-variant LZHUF.
            lzh ??= new Lzh();

            int produced = lzh.Decode(cmprBuf, cmprLenAbs, outBuf, outOfs, trackLen);

            if(produced != trackLen) return ErrorNumber.InvalidArgument;
        }

        ushort crcComputed = SxdCrc16(outBuf.AsSpan(outOfs, trackLen));

        crcOk = crcComputed == crcStored;

        return ErrorNumber.NoError;
    }

    static string DensityName(byte density) => density switch
                                               {
                                                   0 => "DD",
                                                   1 => "HD",
                                                   2 => "ED",
                                                   _ => "unknown"
                                               };

    static DateTime DosDateTimeToUtc(ushort dosDate, ushort dosTime, DateTime fallback)
    {
        if(dosDate == 0 && dosTime == 0) return fallback;

        int year   = (dosDate >> 9 & 0x7F) + 1980;
        int month  = dosDate >> 5  & 0x0F;
        int day    = dosDate       & 0x1F;
        int hour   = dosTime >> 11 & 0x1F;
        int minute = dosTime >> 5  & 0x3F;
        int second = (dosTime & 0x1F) * 2;

        try
        {
            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
        }
        catch(ArgumentOutOfRangeException)
        {
            return fallback;
        }
    }
}