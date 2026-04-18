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
//     Reads partclone disk images.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Extents;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class PartClone
{
#region IMediaImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();
        stream.Seek(0, SeekOrigin.Begin);

        if(stream.Length < 512) return ErrorNumber.InvalidArgument;

        // Peek the version field (bytes 30..33), shared by both v0001 and v0002 layouts.
        var versionBytes = new byte[4];
        stream.Seek(30, SeekOrigin.Begin);

        if(stream.EnsureRead(versionBytes, 0, 4) != 4) return ErrorNumber.InvalidArgument;

        string version = Encoding.ASCII.GetString(versionBytes);

        AaruLogging.Debug(MODULE_NAME, "Detected partclone image version '{0}'", version);

        stream.Seek(0, SeekOrigin.Begin);

        switch(version)
        {
            case VERSION_0001:
                return OpenV1(imageFilter, stream);
            case VERSION_0002:
                return OpenV2(imageFilter, stream);
            default:
                AaruLogging.Error(MODULE_NAME,
                                  "Unsupported partclone image version '{0}', only '0001' and '0002' are supported",
                                  version);

                return ErrorNumber.NotSupported;
        }
    }

    ErrorNumber OpenV1(IFilter imageFilter, Stream stream)
    {
        _imageVersion = 1;

        var pHdrB = new byte[Marshal.SizeOf<Header>()];
        stream.EnsureRead(pHdrB, 0, Marshal.SizeOf<Header>());
        _pHdr = Marshal.ByteArrayToStructureLittleEndian<Header>(pHdrB);

        AaruLogging.Debug(MODULE_NAME, "pHdr.magic = {0}",       StringHandlers.CToString(_pHdr.magic));
        AaruLogging.Debug(MODULE_NAME, "pHdr.filesystem = {0}",  StringHandlers.CToString(_pHdr.filesystem));
        AaruLogging.Debug(MODULE_NAME, "pHdr.version = {0}",     StringHandlers.CToString(_pHdr.version));
        AaruLogging.Debug(MODULE_NAME, "pHdr.blockSize = {0}",   _pHdr.blockSize);
        AaruLogging.Debug(MODULE_NAME, "pHdr.deviceSize = {0}",  _pHdr.deviceSize);
        AaruLogging.Debug(MODULE_NAME, "pHdr.totalBlocks = {0}", _pHdr.totalBlocks);
        AaruLogging.Debug(MODULE_NAME, "pHdr.usedBlocks = {0}",  _pHdr.usedBlocks);

        _byteMap = new byte[_pHdr.totalBlocks];
        AaruLogging.Debug(MODULE_NAME, Localization.Reading_bytemap_0_bytes, _byteMap.Length);
        stream.EnsureRead(_byteMap, 0, _byteMap.Length);

        var bitmagic = new byte[8];
        stream.EnsureRead(bitmagic, 0, 8);

        AaruLogging.Debug(MODULE_NAME, "pHdr.bitmagic = {0}", StringHandlers.CToString(bitmagic));

        if(!_biTmAgIc.SequenceEqual(bitmagic))
        {
            AaruLogging.Error(Localization.Could_not_find_partclone_BiTmAgIc_not_continuing);

            return ErrorNumber.InvalidArgument;
        }

        _dataOff = stream.Position;
        AaruLogging.Debug(MODULE_NAME, "pHdr.dataOff = {0}", _dataOff);

        // Autodetect the legacy x64 CRC bug: on 64-bit platforms old partclone versions serialized
        // the 4-byte CRC using sizeof(unsigned long), writing 8 bytes per block instead of 4.
        // Compare the trailing data region length against both possibilities to pick the right stride.
        long trailing       = stream.Length - _dataOff;
        var  expectedNormal = (long)(_pHdr.usedBlocks * (_pHdr.blockSize + CRC_SIZE_NORMAL));
        var  expectedX64    = (long)(_pHdr.usedBlocks * (_pHdr.blockSize + CRC_SIZE_X64_BUG));

        if(trailing == expectedX64 && trailing != expectedNormal)
        {
            _crcSize = CRC_SIZE_X64_BUG;

            AaruLogging.Debug(MODULE_NAME,
                              "Detected partclone v0001 x64 CRC bug: using 8-byte CRC stride (trailing={0})",
                              trailing);
        }
        else
        {
            _crcSize = CRC_SIZE_NORMAL;

            if(trailing != expectedNormal && _pHdr.usedBlocks > 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Partclone data region size {0} does not match either layout (normal={1}, x64={2}); assuming 4-byte CRC stride",
                                  trailing,
                                  expectedNormal,
                                  expectedX64);
            }
        }

        BuildExtentsFromByteMap(_pHdr.totalBlocks);

        _sectorCache = new Dictionary<ulong, byte[]>();

        _imageInfo.CreationTime         = imageFilter.CreationTime;
        _imageInfo.LastModificationTime = imageFilter.LastWriteTime;
        _imageInfo.MediaTitle           = Path.GetFileNameWithoutExtension(imageFilter.Filename);
        _imageInfo.Sectors              = _pHdr.totalBlocks;
        _imageInfo.SectorSize           = _pHdr.blockSize;
        _imageInfo.MetadataMediaType    = MetadataMediaType.BlockMedia;
        _imageInfo.MediaType            = MediaType.GENERIC_HDD;
        _imageInfo.ImageSize            = (ulong)(stream.Length - (4096 + 0x40 + (long)_pHdr.totalBlocks));
        _imageStream                    = stream;

        return ErrorNumber.NoError;
    }

    ErrorNumber OpenV2(IFilter imageFilter, Stream stream)
    {
        _imageVersion = 2;

        int hdrSize = Marshal.SizeOf<HeaderV2>();
        var hdrB    = new byte[hdrSize];

        if(stream.EnsureRead(hdrB, 0, hdrSize) != hdrSize) return ErrorNumber.InvalidArgument;

        // Validate the header CRC32 over everything but the trailing crc field.
        uint computed = UpdateCrc32(CRC32_SEED, hdrB, 0, hdrSize - V2_HEADER_CRC_SIZE);
        var  stored   = BitConverter.ToUInt32(hdrB, hdrSize      - V2_HEADER_CRC_SIZE);

        if(computed != stored)
        {
            AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_invalid_header_checksum_0_1, stored, computed);

            return ErrorNumber.InvalidArgument;
        }

        HeaderV2 hdr = Marshal.ByteArrayToStructureLittleEndian<HeaderV2>(hdrB);

        if(hdr.endianess != ENDIAN_MAGIC)
        {
            AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_unsupported_endianness_0, hdr.endianess);

            return ErrorNumber.NotSupported;
        }

        string ptcVersion = StringHandlers.CToString(hdr.ptcVersion);
        string fsName     = StringHandlers.CToString(hdr.filesystem);

        AaruLogging.Debug(MODULE_NAME, "v2.ptcVersion          = {0}",      ptcVersion);
        AaruLogging.Debug(MODULE_NAME, "v2.filesystem          = {0}",      fsName);
        AaruLogging.Debug(MODULE_NAME, "v2.deviceSize          = {0}",      hdr.deviceSize);
        AaruLogging.Debug(MODULE_NAME, "v2.totalBlocks         = {0}",      hdr.totalBlocks);
        AaruLogging.Debug(MODULE_NAME, "v2.usedBlocks          = {0}",      hdr.usedBlocks);
        AaruLogging.Debug(MODULE_NAME, "v2.superBlockUsed      = {0}",      hdr.superBlockUsedBlocks);
        AaruLogging.Debug(MODULE_NAME, "v2.blockSize           = {0}",      hdr.blockSize);
        AaruLogging.Debug(MODULE_NAME, "v2.featureSize         = {0}",      hdr.featureSize);
        AaruLogging.Debug(MODULE_NAME, "v2.imageVersion        = 0x{0:X4}", hdr.imageVersion);
        AaruLogging.Debug(MODULE_NAME, "v2.cpuBits             = {0}",      hdr.cpuBits);
        AaruLogging.Debug(MODULE_NAME, "v2.checksumMode        = 0x{0:X2}", hdr.checksumMode);
        AaruLogging.Debug(MODULE_NAME, "v2.checksumSize        = {0}",      hdr.checksumSize);
        AaruLogging.Debug(MODULE_NAME, "v2.blocksPerChecksum   = {0}",      hdr.blocksPerChecksum);
        AaruLogging.Debug(MODULE_NAME, "v2.reseedChecksum      = {0}",      hdr.reseedChecksum);
        AaruLogging.Debug(MODULE_NAME, "v2.bitmapMode          = 0x{0:X2}", hdr.bitmapMode);

        if(hdr.cpuBits != 32 && hdr.cpuBits != 64)
        {
            AaruLogging.Debug(MODULE_NAME, Localization.PartClone_v2_unexpected_cpu_bits_0, hdr.cpuBits);
        }

        if(hdr.blockSize == 0)
        {
            AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_invalid_block_size_0, hdr.blockSize);

            return ErrorNumber.InvalidArgument;
        }

        // Validate checksum mode + checksum size pair.
        ushort expectedCsSize = hdr.checksumMode switch
                                {
                                    CSM_NONE       => 0,
                                    CSM_CRC32      => 4,
                                    CSM_CRC32_0001 => 4,
                                    CSM_XXH64      => 8,
                                    CSM_XXH128     => 16,
                                    _              => ushort.MaxValue
                                };

        if(expectedCsSize == ushort.MaxValue)
        {
            AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_unknown_checksum_mode_0, hdr.checksumMode);

            return ErrorNumber.NotSupported;
        }

        if(expectedCsSize != hdr.checksumSize)
        {
            AaruLogging.Error(MODULE_NAME,
                              Localization.PartClone_v2_checksum_size_mismatch_0_1,
                              hdr.checksumSize,
                              hdr.checksumMode);

            return ErrorNumber.InvalidArgument;
        }

        if(hdr.checksumMode != CSM_NONE && hdr.blocksPerChecksum == 0)
        {
            AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_blocks_per_checksum_zero);

            return ErrorNumber.InvalidArgument;
        }

        _v2TotalBlocks       = hdr.totalBlocks;
        _v2UsedBlocks        = hdr.usedBlocks;
        _v2BlockSize         = hdr.blockSize;
        _v2DeviceSize        = hdr.deviceSize;
        _v2ChecksumMode      = hdr.checksumMode;
        _v2ChecksumSize      = hdr.checksumSize;
        _v2BlocksPerChecksum = hdr.blocksPerChecksum;
        _v2ReseedChecksum    = hdr.reseedChecksum != 0;
        _v2BitmapMode        = hdr.bitmapMode;

        // Read and validate the bitmap.
        _byteMap = new byte[hdr.totalBlocks];

        switch(hdr.bitmapMode)
        {
            case BM_NONE:
                // All blocks present and contiguous.
                for(ulong i = 0; i < hdr.totalBlocks; i++) _byteMap[i] = 1;

                break;

            case BM_BIT:
            {
                ulong bitmapBytes = (hdr.totalBlocks + 7) / 8;

                if(bitmapBytes > int.MaxValue)
                {
                    AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_bitmap_too_large_0, bitmapBytes);

                    return ErrorNumber.NotSupported;
                }

                var bitmap = new byte[bitmapBytes];

                if(stream.EnsureRead(bitmap, 0, (int)bitmapBytes) != (int)bitmapBytes)
                {
                    AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_short_read_bitmap);

                    return ErrorNumber.InvalidArgument;
                }

                var bitmapCrcBuf = new byte[V2_BITMAP_CRC_SIZE];

                if(stream.EnsureRead(bitmapCrcBuf, 0, V2_BITMAP_CRC_SIZE) != V2_BITMAP_CRC_SIZE)
                {
                    AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_short_read_bitmap_crc);

                    return ErrorNumber.InvalidArgument;
                }

                uint computedBitmapCrc = UpdateCrc32(CRC32_SEED, bitmap, 0, (int)bitmapBytes);
                var  storedBitmapCrc   = BitConverter.ToUInt32(bitmapCrcBuf, 0);

                if(computedBitmapCrc != storedBitmapCrc)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      Localization.PartClone_v2_bitmap_crc_mismatch_0_1,
                                      storedBitmapCrc,
                                      computedBitmapCrc);

                    return ErrorNumber.InvalidArgument;
                }

                for(ulong i = 0; i < hdr.totalBlocks; i++)
                {
                    byte b = bitmap[i >> 3];

                    if((b & 1 << (int)(i & 7)) != 0) _byteMap[i] = 1;
                }

                break;
            }

            case BM_BYTE:
            {
                if(hdr.totalBlocks > int.MaxValue)
                {
                    AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_bitmap_too_large_0, hdr.totalBlocks);

                    return ErrorNumber.NotSupported;
                }

                if(stream.EnsureRead(_byteMap, 0, (int)hdr.totalBlocks) != (int)hdr.totalBlocks)
                {
                    AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_short_read_bitmap);

                    return ErrorNumber.InvalidArgument;
                }

                var bitmagic = new byte[8];
                stream.EnsureRead(bitmagic, 0, 8);

                if(!_biTmAgIc.SequenceEqual(bitmagic))
                {
                    AaruLogging.Error(Localization.Could_not_find_partclone_BiTmAgIc_not_continuing);

                    return ErrorNumber.InvalidArgument;
                }

                break;
            }

            default:
                AaruLogging.Error(MODULE_NAME, Localization.PartClone_v2_unknown_bitmap_mode_0, hdr.bitmapMode);

                return ErrorNumber.NotSupported;
        }

        _dataOff = stream.Position;
        AaruLogging.Debug(MODULE_NAME, "v2.dataOff             = {0}", _dataOff);

        BuildExtentsFromByteMap(hdr.totalBlocks);

        _sectorCache = new Dictionary<ulong, byte[]>();

        _imageInfo.CreationTime         = imageFilter.CreationTime;
        _imageInfo.LastModificationTime = imageFilter.LastWriteTime;
        _imageInfo.MediaTitle           = Path.GetFileNameWithoutExtension(imageFilter.Filename);
        _imageInfo.Sectors              = hdr.totalBlocks;
        _imageInfo.SectorSize           = hdr.blockSize;
        _imageInfo.MetadataMediaType    = MetadataMediaType.BlockMedia;
        _imageInfo.MediaType            = MediaType.GENERIC_HDD;
        _imageInfo.ImageSize            = hdr.usedBlocks * hdr.blockSize;
        _imageInfo.ApplicationVersion   = ptcVersion;
        _imageStream                    = stream;

        return ErrorNumber.NoError;
    }

    void BuildExtentsFromByteMap(ulong totalBlocks)
    {
        AaruLogging.Debug(MODULE_NAME, Localization.Filling_extents);
        var extentFillStopwatch = new Stopwatch();
        extentFillStopwatch.Start();
        _extents    = new ExtentsULong();
        _extentsOff = new Dictionary<ulong, ulong>();

        if(totalBlocks == 0)
        {
            extentFillStopwatch.Stop();

            return;
        }

        bool  current     = _byteMap[0] > 0;
        ulong blockOff    = 0;
        ulong extentStart = 0;

        if(current) _extentsOff.Add(0, 0);

        for(ulong i = 1; i < totalBlocks; i++)
        {
            bool next = _byteMap[i] > 0;

            // Flux
            if(next != current)
            {
                if(next)
                {
                    extentStart = i;
                    _extentsOff.Add(i, ++blockOff);
                }
                else
                    _extents.Add(extentStart, i);
            }

            if(next && current) blockOff++;

            current = next;
        }

        if(current) _extents.Add(extentStart, totalBlocks);

        extentFillStopwatch.Stop();

        AaruLogging.Debug(MODULE_NAME,
                          Localization.Took_0_seconds_to_fill_extents,
                          extentFillStopwatch.Elapsed.TotalSeconds);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer       = null;
        sectorStatus = SectorStatus.NotDumped;

        if(negative) return ErrorNumber.NotSupported;

        if(sectorAddress > _imageInfo.Sectors - 1) return ErrorNumber.OutOfRange;

        sectorStatus = SectorStatus.Dumped;

        uint blockSize = _imageVersion == 2 ? _v2BlockSize : _pHdr.blockSize;

        if(_byteMap[sectorAddress] == 0)
        {
            buffer = new byte[blockSize];

            return ErrorNumber.NoError;
        }

        if(_sectorCache.TryGetValue(sectorAddress, out buffer)) return ErrorNumber.NoError;

        long imageOff = BlockByteOffset(sectorAddress);

        buffer = new byte[blockSize];
        _imageStream.Seek(imageOff, SeekOrigin.Begin);
        _imageStream.EnsureRead(buffer, 0, (int)blockSize);

        if(_sectorCache.Count > MAX_CACHED_SECTORS) _sectorCache.Clear();

        _sectorCache.Add(sectorAddress, buffer);

        return ErrorNumber.NoError;
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

        uint blockSize = _imageVersion == 2 ? _v2BlockSize : _pHdr.blockSize;

        var ms = new MemoryStream();
        sectorStatus = Enumerable.Repeat(SectorStatus.Dumped, (int)length).ToArray();

        var allEmpty = true;

        for(uint i = 0; i < length; i++)
        {
            if(_byteMap[sectorAddress + i] == 0) continue;

            allEmpty = false;

            break;
        }

        if(allEmpty)
        {
            buffer = new byte[blockSize * length];

            return ErrorNumber.NoError;
        }

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