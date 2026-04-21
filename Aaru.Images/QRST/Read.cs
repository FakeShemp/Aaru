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
//     Reads QRST disk images.
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

/* Based on the work of Michal Necasek (www.os2museum.com). */

using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class Qrst
{
#region IMediaImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        Stream stream     = imageFilter.GetDataForkStream();
        int    headerSize = Marshal.SizeOf<QrstHeader>();

        if(stream.Length < headerSize) return ErrorNumber.InvalidArgument;

        stream.Seek(0, SeekOrigin.Begin);
        var hdrBuf = new byte[headerSize];
        stream.EnsureRead(hdrBuf, 0, headerSize);

        QrstHeader hdr = Marshal.ByteArrayToStructureLittleEndian<QrstHeader>(hdrBuf);

        if(!hdr.signature.SequenceEqual(_signature)) return ErrorNumber.InvalidArgument;

        // V5 (PKWARE-compressed) images are not supported.
        if(hdr.type != 0)
        {
            AaruLogging.Error(MODULE_NAME, Localization.Qrst_V5_images_are_not_supported);

            return ErrorNumber.NotSupported;
        }

        if(hdr.disk_fmt == 0 || hdr.disk_fmt >= _dskDesc.Length) return ErrorNumber.InvalidArgument;

        (byte cyls, byte heads, byte spt) = _dskDesc[hdr.disk_fmt];

        _cyls     = cyls;
        _heads    = heads;
        _spt      = spt;
        _trackLen = spt * SECTOR_SIZE;
        _header   = hdr;

        int totalTracks = _cyls * _heads;

        // Walk the track headers to build a lookup table of file offsets.
        long curOfs    = headerSize;
        var  trkHdrBuf = new byte[Marshal.SizeOf<QrstTrackHeader>()];
        var  blkLenBuf = new byte[sizeof(ushort)];

        for(var i = 0; i < totalTracks; i++)
        {
            stream.Seek(curOfs, SeekOrigin.Begin);

            if(stream.EnsureRead(trkHdrBuf, 0, trkHdrBuf.Length) != trkHdrBuf.Length)
                return ErrorNumber.InvalidArgument;

            QrstTrackHeader trkHdr = Marshal.ByteArrayToStructureLittleEndian<QrstTrackHeader>(trkHdrBuf);

            if(trkHdr.cyl > _cyls || trkHdr.head > _heads || trkHdr.type > TRK_CMPRSD)
                return ErrorNumber.InvalidArgument;

            int trkIdx = trkHdr.cyl * _heads + trkHdr.head;

            // Reject duplicates.
            if(_trackOffset.ContainsKey(trkIdx)) return ErrorNumber.InvalidArgument;

            _trackOffset[trkIdx] =  curOfs;
            curOfs               += trkHdrBuf.Length;

            switch(trkHdr.type)
            {
                case TRK_NORMAL:
                    curOfs += _trackLen;

                    break;
                case TRK_BLANK:
                    curOfs += 1;

                    break;
                case TRK_CMPRSD:
                    if(stream.EnsureRead(blkLenBuf, 0, blkLenBuf.Length) != blkLenBuf.Length)
                        return ErrorNumber.InvalidArgument;

                    var blkLen = BitConverter.ToUInt16(blkLenBuf, 0);
                    curOfs += blkLenBuf.Length + blkLen;

                    break;
                default:
                    return ErrorNumber.InvalidArgument;
            }
        }

        if(stream.Length < curOfs) return ErrorNumber.InvalidArgument;

        if(_trackOffset.Count != totalTracks) return ErrorNumber.InvalidArgument;

        _imageInfo.Cylinders       = _cyls;
        _imageInfo.Heads           = _heads;
        _imageInfo.SectorsPerTrack = _spt;
        _imageInfo.SectorSize      = SECTOR_SIZE;
        _imageInfo.Sectors         = (ulong)totalTracks * _spt;
        _imageInfo.ImageSize       = _imageInfo.Sectors * _imageInfo.SectorSize;

        _imageInfo.MetadataMediaType    = MetadataMediaType.BlockMedia;
        _imageInfo.CreationTime         = imageFilter.CreationTime;
        _imageInfo.LastModificationTime = imageFilter.LastWriteTime;
        _imageInfo.MediaTitle           = Path.GetFileNameWithoutExtension(imageFilter.Filename);

        _imageInfo.MediaType = Geometry.GetMediaType((_cyls, _heads, _spt, SECTOR_SIZE, MediaEncoding.MFM, false));

        string description = Encoding.ASCII.GetString(hdr.desc).TrimEnd('\0', ' ');

        if(!string.IsNullOrWhiteSpace(description)) _imageInfo.Comments = description;

        _imageFilter = imageFilter;

        AaruLogging.Debug(MODULE_NAME, Localization.Detected_QRST_image_with_CHS_equals_0_1_2, _cyls, _heads, _spt);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer       = null;
        sectorStatus = SectorStatus.NotDumped;

        if(negative) return ErrorNumber.NotSupported;

        if(sectorAddress >= _imageInfo.Sectors) return ErrorNumber.OutOfRange;

        var trackNum    = (int)(sectorAddress / _spt);
        var sectorInTrk = (int)(sectorAddress % _spt);

        if(!_trackCache.TryGetValue(trackNum, out byte[] trackData))
        {
            ErrorNumber errno = ReadTrackIntoCache(_imageFilter.GetDataForkStream(), trackNum);

            if(errno != ErrorNumber.NoError) return errno;

            trackData = _trackCache[trackNum];
        }

        buffer = new byte[SECTOR_SIZE];
        Array.Copy(trackData, sectorInTrk * SECTOR_SIZE, buffer, 0, SECTOR_SIZE);

        sectorStatus = SectorStatus.Dumped;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, bool negative, uint length, out byte[] buffer,
                                   out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        if(negative) return ErrorNumber.NotSupported;

        if(sectorAddress >= _imageInfo.Sectors) return ErrorNumber.OutOfRange;

        if(sectorAddress + length > _imageInfo.Sectors) return ErrorNumber.OutOfRange;

        MemoryStream ms = new();
        sectorStatus = new SectorStatus[length];

        for(uint i = 0; i < length; i++)
        {
            ErrorNumber errno = ReadSector(sectorAddress + i, false, out byte[] sector, out SectorStatus status);

            if(errno != ErrorNumber.NoError) return errno;

            ms.Write(sector, 0, sector.Length);
            sectorStatus[i] = status;
        }

        buffer = ms.ToArray();

        return ErrorNumber.NoError;
    }

#endregion
}