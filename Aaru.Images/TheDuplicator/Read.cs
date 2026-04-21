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
//     Reads The Duplicator disk images.
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
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class TheDuplicator
{
#region IMediaImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();

        TdupHeader    hdr    = new();
        TdupCylInfo[] cylMap = null;

        if(!TryReadImage(stream, ref hdr, ref cylMap)) return ErrorNumber.InvalidArgument;

        _header      = hdr;
        _cylMap      = cylMap;
        _imageFilter = imageFilter;

        _imageInfo.Cylinders       = hdr.numCyls;
        _imageInfo.Heads           = (byte)hdr.numHeads;
        _imageInfo.SectorsPerTrack = hdr.numSec;
        _imageInfo.SectorSize      = SECTOR_SIZE;
        _imageInfo.Sectors         = (ulong)hdr.numCyls * hdr.numHeads * hdr.numSec;
        _imageInfo.ImageSize       = _imageInfo.Sectors * _imageInfo.SectorSize;

        _imageInfo.MetadataMediaType = MetadataMediaType.BlockMedia;

        _imageInfo.CreationTime         = imageFilter.CreationTime;
        _imageInfo.LastModificationTime = imageFilter.LastWriteTime;
        _imageInfo.MediaTitle           = Path.GetFileNameWithoutExtension(imageFilter.Filename);

        _imageInfo.MediaType = Geometry.GetMediaType(((ushort)_imageInfo.Cylinders, (byte)_imageInfo.Heads,
                                                      (ushort)_imageInfo.SectorsPerTrack, SECTOR_SIZE,
                                                      MediaEncoding.MFM, false));

        AaruLogging.Debug(MODULE_NAME,
                          Localization.Detected_TheDuplicator_image_with_CHS_equals_0_1_2,
                          _imageInfo.Cylinders,
                          _imageInfo.Heads,
                          _imageInfo.SectorsPerTrack);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer       = null;
        sectorStatus = SectorStatus.NotDumped;

        if(negative) return ErrorNumber.NotSupported;

        if(sectorAddress >= _imageInfo.Sectors) return ErrorNumber.OutOfRange;

        var spc         = _imageInfo.SectorsPerTrack * _imageInfo.Heads;
        var cylNum      = (uint)(sectorAddress / spc);
        var sectorInCyl = (uint)(sectorAddress % spc);

        if(cylNum >= _cylMap.Length) return ErrorNumber.SectorNotFound;

        buffer = new byte[SECTOR_SIZE];

        switch(_cylMap[cylNum].flags)
        {
            case CYLFLG_IMGDATA:
                Stream stream = _imageFilter.GetDataForkStream();
                long   cylOfs = (long)_cylMap[cylNum].start * SECTOR_SIZE;
                stream.Seek(cylOfs + sectorInCyl * SECTOR_SIZE, SeekOrigin.Begin);
                stream.EnsureRead(buffer, 0, SECTOR_SIZE);

                break;
            case CYLFLG_FILLER:
                Array.Fill(buffer, _cylMap[cylNum].filler);

                break;
            default:
                return ErrorNumber.InvalidArgument;
        }

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