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
//     Reads IBM SaveDskF disk images.
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
// Copyright © 2011-2025 Natalia Portillo
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

public sealed partial class SaveDskF
{
#region IWritableImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();
        stream.Seek(0, SeekOrigin.Begin);

        var hdr = new byte[40];

        stream.EnsureRead(hdr, 0, 40);
        _header = Marshal.ByteArrayToStructureLittleEndian<Header>(hdr);

        AaruLogging.Debug(MODULE_NAME, "header.magic = 0x{0:X4}",      _header.magic);
        AaruLogging.Debug(MODULE_NAME, "header.mediaType = 0x{0:X2}",  _header.mediaType);
        AaruLogging.Debug(MODULE_NAME, "header.sectorSize = {0}",      _header.sectorSize);
        AaruLogging.Debug(MODULE_NAME, "header.clusterMask = {0}",     _header.clusterMask);
        AaruLogging.Debug(MODULE_NAME, "header.clusterShift = {0}",    _header.clusterShift);
        AaruLogging.Debug(MODULE_NAME, "header.reservedSectors = {0}", _header.reservedSectors);
        AaruLogging.Debug(MODULE_NAME, "header.fatCopies = {0}",       _header.fatCopies);
        AaruLogging.Debug(MODULE_NAME, "header.rootEntries = {0}",     _header.rootEntries);
        AaruLogging.Debug(MODULE_NAME, "header.firstCluster = {0}",    _header.firstCluster);
        AaruLogging.Debug(MODULE_NAME, "header.clustersCopied = {0}",  _header.clustersCopied);
        AaruLogging.Debug(MODULE_NAME, "header.sectorsPerFat = {0}",   _header.sectorsPerFat);
        AaruLogging.Debug(MODULE_NAME, "header.checksum = 0x{0:X8}",   _header.checksum);
        AaruLogging.Debug(MODULE_NAME, "header.cylinders = {0}",       _header.cylinders);
        AaruLogging.Debug(MODULE_NAME, "header.heads = {0}",           _header.heads);
        AaruLogging.Debug(MODULE_NAME, "header.sectorsPerTrack = {0}", _header.sectorsPerTrack);
        AaruLogging.Debug(MODULE_NAME, "header.padding = {0}",         _header.padding);
        AaruLogging.Debug(MODULE_NAME, "header.sectorsCopied = {0}",   _header.sectorsCopied);
        AaruLogging.Debug(MODULE_NAME, "header.commentOffset = {0}",   _header.commentOffset);
        AaruLogging.Debug(MODULE_NAME, "header.dataOffset = {0}",      _header.dataOffset);

        if(_header is { dataOffset: 0, magic: SDF_MAGIC_OLD }) _header.dataOffset = 512;

        var cmt = new byte[_header.dataOffset - _header.commentOffset];
        stream.Seek(_header.commentOffset, SeekOrigin.Begin);
        stream.EnsureRead(cmt, 0, cmt.Length);

        if(cmt.Length > 1) _imageInfo.Comments = StringHandlers.CToString(cmt, Encoding.GetEncoding("ibm437"));

        _calculatedChk = 0;
        stream.Seek(0, SeekOrigin.Begin);

        int b;

        do
        {
            b = stream.ReadByte();

            if(b >= 0) _calculatedChk += (uint)b;
        } while(b >= 0);

        AaruLogging.Debug(MODULE_NAME,
                          Localization.Calculated_checksum_equals_0_X8_1,
                          _calculatedChk,
                          _calculatedChk == _header.checksum);

        _imageInfo.Application          = "SaveDskF";
        _imageInfo.CreationTime         = imageFilter.CreationTime;
        _imageInfo.LastModificationTime = imageFilter.LastWriteTime;
        _imageInfo.MediaTitle           = imageFilter.Filename;
        _imageInfo.ImageSize            = (ulong)(stream.Length - _header.dataOffset);
        _imageInfo.Sectors              = (ulong)(_header.sectorsPerTrack * _header.heads * _header.cylinders);
        _imageInfo.SectorSize           = _header.sectorSize;

        _imageInfo.MediaType = Geometry.GetMediaType((_header.cylinders, (byte)_header.heads, _header.sectorsPerTrack,
                                                      _header.sectorSize, MediaEncoding.MFM, false));

        _imageInfo.MetadataMediaType = MetadataMediaType.BlockMedia;

        AaruLogging.Verbose(Localization.SaveDskF_image_contains_a_disk_of_type_0, _imageInfo.MediaType);

        if(!string.IsNullOrEmpty(_imageInfo.Comments))
            AaruLogging.Verbose(Localization.SaveDskF_comments_0, _imageInfo.Comments);

        // TODO: Support compressed images
        if(_header.magic == SDF_MAGIC_COMPRESSED)
        {
            AaruLogging.Error(Localization.Compressed_SaveDskF_images_are_not_supported);

            return ErrorNumber.NotSupported;
        }

        // SaveDskF only omits ending clusters, leaving no gaps behind, so reading all data we have...
        stream.Seek(_header.dataOffset, SeekOrigin.Begin);
        _decodedDisk = new byte[_imageInfo.Sectors * _imageInfo.SectorSize];
        stream.EnsureRead(_decodedDisk, 0, (int)(stream.Length - _header.dataOffset));

        _imageInfo.Cylinders       = _header.cylinders;
        _imageInfo.Heads           = _header.heads;
        _imageInfo.SectorsPerTrack = _header.sectorsPerTrack;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, out byte[] buffer, out SectorStatus sectorStatus)
    {
        sectorStatus = SectorStatus.Dumped;

        return ReadSectors(sectorAddress, 1, out buffer, out _);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong sectorAddress, uint length, out byte[] buffer, out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        if(sectorAddress > _imageInfo.Sectors - 1) return ErrorNumber.OutOfRange;

        if(sectorAddress + length > _imageInfo.Sectors) return ErrorNumber.OutOfRange;

        buffer       = new byte[length * _imageInfo.SectorSize];
        sectorStatus = Enumerable.Repeat(SectorStatus.Dumped, (int)length).ToArray();


        Array.Copy(_decodedDisk, (int)sectorAddress * _imageInfo.SectorSize, buffer, 0, length * _imageInfo.SectorSize);

        return ErrorNumber.NoError;
    }

#endregion
}