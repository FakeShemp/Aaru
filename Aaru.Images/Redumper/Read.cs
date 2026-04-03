// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Read.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Reads Redumper raw DVD dump images (.sdram + .state).
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
using System.IO;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decoders.DVD;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Track = Aaru.CommonTypes.Structs.Track;
using TrackType = Aaru.CommonTypes.Enums.TrackType;
using Session = Aaru.CommonTypes.Structs.Session;

namespace Aaru.Images;

public sealed partial class Redumper
{
#region IOpticalMediaImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        string filename = imageFilter.Filename;
        _ngcwRegularDataSectors = 0;

        if(string.IsNullOrEmpty(filename)) return ErrorNumber.InvalidArgument;

        string basePath = filename[..^".state".Length];
        string sdramPath = basePath + ".sdram";

        if(!File.Exists(sdramPath)) return ErrorNumber.NoSuchFile;

        long stateLength = imageFilter.DataForkLength;
        long sdramLength = new FileInfo(sdramPath).Length;

        if(sdramLength % RECORDING_FRAME_SIZE != 0) return ErrorNumber.InvalidArgument;

        _totalFrames = sdramLength / RECORDING_FRAME_SIZE;

        if(stateLength != _totalFrames) return ErrorNumber.InvalidArgument;

        _imageFilter = imageFilter;

        // Read entire state file into memory (1 byte per frame, manageable size)
        Stream stateStream = imageFilter.GetDataForkStream();
        _stateData = new byte[stateLength];
        stateStream.Seek(0, SeekOrigin.Begin);
        stateStream.EnsureRead(_stateData, 0, (int)stateLength);

        // Open sdram via filter system
        _sdramFilter = PluginRegister.Singleton.GetFilter(sdramPath);

        if(_sdramFilter is null) return ErrorNumber.NoSuchFile;

        // Compute sector counts
        // Frames map to physical LBAs: frame[i] → LBA (LBA_START + i)
        // Negative LBAs: LBA_START .. -1, count = min(-LBA_START, _totalFrames)
        // Positive LBAs: 0 .. (_totalFrames + LBA_START - 1)
        long negativeLbaCount = Math.Min(-LBA_START, _totalFrames);
        long positiveLbaCount = Math.Max(0, _totalFrames + LBA_START);

        _imageInfo.NegativeSectors = (uint)negativeLbaCount;
        _imageInfo.Sectors         = (ulong)positiveLbaCount;
        _imageInfo.SectorSize      = DVD_USER_DATA_SIZE;
        _imageInfo.ImageSize       = _imageInfo.Sectors * DVD_USER_DATA_SIZE;

        _imageInfo.CreationTime         = imageFilter.CreationTime;
        _imageInfo.LastModificationTime = imageFilter.LastWriteTime;
        _imageInfo.MediaTitle           = Path.GetFileNameWithoutExtension(imageFilter.Filename);
        _imageInfo.MetadataMediaType    = MetadataMediaType.OpticalDisc;
        _imageInfo.HasPartitions        = true;
        _imageInfo.HasSessions          = true;

        // Load media tag sidecars
        _mediaTags = new Dictionary<MediaTagType, byte[]>();
        LoadMediaTagSidecars(basePath);

        // Determine media type from PFI if available
        _imageInfo.MediaType = MediaType.DVDROM;

        if(_mediaTags.TryGetValue(MediaTagType.DVD_PFI, out byte[] pfi))
        {
            PFI.PhysicalFormatInformation? decodedPfi = PFI.Decode(pfi, _imageInfo.MediaType);

            if(decodedPfi.HasValue)
            {
                _imageInfo.MediaType = decodedPfi.Value.DiskCategory switch
                {
                    DiskCategory.DVDPR    => MediaType.DVDPR,
                    DiskCategory.DVDPRDL  => MediaType.DVDPRDL,
                    DiskCategory.DVDPRW   => MediaType.DVDPRW,
                    DiskCategory.DVDPRWDL => MediaType.DVDPRWDL,
                    DiskCategory.DVDR     => decodedPfi.Value.PartVersion >= 6 ? MediaType.DVDRDL : MediaType.DVDR,
                    DiskCategory.DVDRAM   => MediaType.DVDRAM,
                    DiskCategory.DVDRW    => decodedPfi.Value.PartVersion >= 15 ? MediaType.DVDRWDL : MediaType.DVDRW,
                    DiskCategory.Nintendo => decodedPfi.Value.DiscSize == DVDSize.Eighty
                                                 ? MediaType.GOD
                                                 : MediaType.WOD,
                    _                     => MediaType.DVDROM
                };

                if(decodedPfi.Value.DataAreaEndPSN >= decodedPfi.Value.DataAreaStartPSN)
                    _ngcwRegularDataSectors =
                        (ulong)(decodedPfi.Value.DataAreaEndPSN - decodedPfi.Value.DataAreaStartPSN) + 1;
            }
        }

        TryInitializeNgcwAfterOpen();

        _imageInfo.ReadableMediaTags = [.._mediaTags.Keys];

        // Sector tags available from DVD RecordingFrame structure
        _imageInfo.ReadableSectorTags =
        [
            SectorTagType.DvdSectorInformation,
            SectorTagType.DvdSectorNumber,
            SectorTagType.DvdSectorIed,
            SectorTagType.DvdSectorCmi,
            SectorTagType.DvdSectorTitleKey,
            SectorTagType.DvdSectorEdc
        ];

        // Set up single track and session covering positive LBAs
        Tracks =
        [
            new Track
            {
                Sequence          = 1,
                Session           = 1,
                Type              = TrackType.Data,
                StartSector       = 0,
                EndSector         = _imageInfo.Sectors > 0 ? _imageInfo.Sectors - 1 : 0,
                Pregap            = 0,
                FileType          = "BINARY",
                Filter            = _sdramFilter,
                File              = sdramPath,
                BytesPerSector    = DVD_USER_DATA_SIZE,
                RawBytesPerSector = DVD_SECTOR_SIZE
            }
        ];

        Sessions =
        [
            new Session
            {
                Sequence    = 1,
                StartSector = 0,
                EndSector   = _imageInfo.Sectors > 0 ? _imageInfo.Sectors - 1 : 0,
                StartTrack  = 1,
                EndTrack    = 1
            }
        ];

        Partitions =
        [
            new Partition
            {
                Sequence = 0,
                Start    = 0,
                Length   = _imageInfo.Sectors,
                Size     = _imageInfo.Sectors * _imageInfo.SectorSize,
                Offset   = 0,
                Type     = "DVD Data"
            }
        ];

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadMediaTag(MediaTagType tag, out byte[] buffer)
    {
        buffer = null;

        if(!_mediaTags.TryGetValue(tag, out byte[] data)) return ErrorNumber.NoData;

        buffer = new byte[data.Length];
        Array.Copy(data, buffer, data.Length);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer = new byte[DVD_USER_DATA_SIZE];
        ErrorNumber errno = ReadSectorLong(sectorAddress, negative, out byte[] long_buffer, out sectorStatus);
        if(errno != ErrorNumber.NoError) return errno;

        Array.Copy(long_buffer, Aaru.Decoders.Nintendo.Sector.NintendoMainDataOffset, buffer, 0, DVD_USER_DATA_SIZE);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, bool negative, uint length, out byte[] buffer,
                                   out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        buffer       = new byte[length * DVD_USER_DATA_SIZE];
        sectorStatus = new SectorStatus[length];

        for(uint i = 0; i < length; i++)
        {
            ulong addr = negative ? sectorAddress - i : sectorAddress + i;
            ErrorNumber errno = ReadSector(addr, negative, out byte[] sector, out SectorStatus status);

            if(errno != ErrorNumber.NoError) return errno;

            Array.Copy(sector, 0, buffer, i * DVD_USER_DATA_SIZE, DVD_USER_DATA_SIZE);
            sectorStatus[i] = status;
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectorLong(ulong            sectorAddress, bool negative, out byte[] buffer,
                                      out SectorStatus sectorStatus) =>
        ReadSectorLongForNgcw(sectorAddress, negative, out buffer, out sectorStatus);

    /// <inheritdoc />
    public ErrorNumber ReadSectorsLong(ulong              sectorAddress, bool negative, uint length, out byte[] buffer,
                                       out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        buffer       = new byte[length * DVD_SECTOR_SIZE];
        sectorStatus = new SectorStatus[length];

        for(uint i = 0; i < length; i++)
        {
            ErrorNumber errno = ReadSectorLong(sectorAddress + i, negative, out byte[] sector, out SectorStatus status);

            if(errno != ErrorNumber.NoError) return errno;

            Array.Copy(sector, 0, buffer, i * DVD_SECTOR_SIZE, DVD_SECTOR_SIZE);
            sectorStatus[i] = status;
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectorTag(ulong sectorAddress, bool negative, SectorTagType tag, out byte[] buffer)
    {
        buffer = null;

        return ReadSectorsTag(sectorAddress, negative, 1, tag, out buffer);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectorsTag(ulong      sectorAddress, bool negative, uint length, SectorTagType tag,
                                      out byte[] buffer)
    {
        buffer = null;

        uint sectorOffset;
        uint sectorSize;

        switch(tag)
        {
            case SectorTagType.DvdSectorInformation:
                sectorOffset = 0;
                sectorSize   = 1;

                break;
            case SectorTagType.DvdSectorNumber:
                sectorOffset = 1;
                sectorSize   = 3;

                break;
            case SectorTagType.DvdSectorIed:
                sectorOffset = 4;
                sectorSize   = 2;

                break;
            case SectorTagType.DvdSectorCmi:
                sectorOffset = 6;
                sectorSize   = 1;

                break;
            case SectorTagType.DvdSectorTitleKey:
                sectorOffset = 7;
                sectorSize   = 5;

                break;
            case SectorTagType.DvdSectorEdc:
                sectorOffset = 2060;
                sectorSize   = 4;

                break;
            default:
                return ErrorNumber.NotSupported;
        }

        buffer = new byte[sectorSize * length];

        for(uint i = 0; i < length; i++)
        {
            ErrorNumber errno = ReadSectorLong(sectorAddress + i, negative, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) return errno;

            Array.Copy(sector, sectorOffset, buffer, i * sectorSize, sectorSize);
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, uint track, out byte[] buffer, out SectorStatus sectorStatus) =>
        ReadSector(sectorAddress, false, out buffer, out sectorStatus);

    /// <inheritdoc />
    public ErrorNumber ReadSectorLong(ulong            sectorAddress, uint track, out byte[] buffer,
                                      out SectorStatus sectorStatus) =>
        ReadSectorLong(sectorAddress, false, out buffer, out sectorStatus);

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, uint length, uint track, out byte[] buffer,
                                   out SectorStatus[] sectorStatus) =>
        ReadSectors(sectorAddress, false, length, out buffer, out sectorStatus);

    /// <inheritdoc />
    public ErrorNumber ReadSectorsLong(ulong              sectorAddress, uint length, uint track, out byte[] buffer,
                                       out SectorStatus[] sectorStatus) =>
        ReadSectorsLong(sectorAddress, false, length, out buffer, out sectorStatus);

    /// <inheritdoc />
    public ErrorNumber ReadSectorTag(ulong sectorAddress, uint track, SectorTagType tag, out byte[] buffer) =>
        ReadSectorTag(sectorAddress, false, tag, out buffer);

    /// <inheritdoc />
    public ErrorNumber ReadSectorsTag(ulong      sectorAddress, uint length, uint track, SectorTagType tag,
                                      out byte[] buffer) =>
        ReadSectorsTag(sectorAddress, false, length, tag, out buffer);

    /// <inheritdoc />
    public List<Track> GetSessionTracks(Session session) => Tracks;

    /// <inheritdoc />
    public List<Track> GetSessionTracks(ushort session) => Tracks;

#endregion

    /// <summary>
    ///     Reads one RecordingFrame from the .sdram file and flattens it into a 2064-byte DVD sector
    ///     by extracting only the 172-byte main_data portion from each of the 12 rows (discarding PI/PO parity).
    /// </summary>
    byte[] ReadAndFlattenFrame(long frameIndex)
    {
        Stream stream = _sdramFilter.GetDataForkStream();
        long offset = frameIndex * RECORDING_FRAME_SIZE;

        if(offset + RECORDING_FRAME_SIZE > stream.Length) return null;

        var frame = new byte[RECORDING_FRAME_SIZE];
        stream.Seek(offset, SeekOrigin.Begin);
        stream.EnsureRead(frame, 0, RECORDING_FRAME_SIZE);

        // Flatten: copy 172 main-data bytes from each of the 12 rows into a contiguous 2064-byte buffer
        var dvdSector = new byte[DVD_SECTOR_SIZE];
        int rowStride = ROW_MAIN_DATA_SIZE + ROW_PARITY_INNER_SIZE;

        for(int row = 0; row < RECORDING_FRAME_ROWS; row++)
            Array.Copy(frame, row * rowStride, dvdSector, row * ROW_MAIN_DATA_SIZE, ROW_MAIN_DATA_SIZE);

        return dvdSector;
    }

    /// <summary>Maps a Redumper state byte to an Aaru SectorStatus.</summary>
    static SectorStatus MapState(byte state) =>
        state switch
        {
            0 => SectorStatus.NotDumped,  // ERROR_SKIP
            1 => SectorStatus.Errored,    // ERROR_C2
            _ => SectorStatus.Dumped      // SUCCESS_C2_OFF (2), SUCCESS_SCSI_OFF (3), SUCCESS (4)
        };

    /// <summary>Loads Redumper sidecar files (.physical, .manufacturer, .bca) as media tags.</summary>
    void LoadMediaTagSidecars(string basePath)
    {
        // PFI layer 0: prefer unindexed, fall back to .0.physical
        LoadScsiSidecar(basePath, ".physical", ".0.physical", MediaTagType.DVD_PFI);

        // PFI layer 1
        LoadScsiSidecar(basePath, ".1.physical", null, MediaTagType.DVD_PFI_2ndLayer);

        // DMI layer 0: prefer unindexed, fall back to .0.manufacturer
        LoadScsiSidecar(basePath, ".manufacturer", ".0.manufacturer", MediaTagType.DVD_DMI);

        // BCA (no SCSI header stripping — redumper writes raw BCA data)
        string bcaPath = basePath + ".bca";

        if(File.Exists(bcaPath))
        {
            byte[] bcaData = File.ReadAllBytes(bcaPath);

            if(bcaData.Length > 0)
            {
                _mediaTags[MediaTagType.DVD_BCA] = bcaData;
                AaruLogging.Debug(MODULE_NAME, Localization.Found_media_tag_0, MediaTagType.DVD_BCA);
            }
        }
    }

    /// <summary>
    ///     Loads a SCSI READ DVD STRUCTURE response sidecar, strips the 4-byte parameter list header,
    ///     and stores the 2048-byte payload as a media tag.
    /// </summary>
    void LoadScsiSidecar(string basePath, string primarySuffix, string fallbackSuffix, MediaTagType tag)
    {
        string path = basePath + primarySuffix;

        if(!File.Exists(path))
        {
            if(fallbackSuffix is null) return;

            path = basePath + fallbackSuffix;

            if(!File.Exists(path)) return;
        }

        byte[] data = File.ReadAllBytes(path);

        if(data.Length <= SCSI_HEADER_SIZE) return;

        // Strip the 4-byte SCSI parameter list header
        byte[] payload = new byte[data.Length - SCSI_HEADER_SIZE];
        Array.Copy(data, SCSI_HEADER_SIZE, payload, 0, payload.Length);

        _mediaTags[tag] = payload;
        AaruLogging.Debug(MODULE_NAME, Localization.Found_media_tag_0, tag);
    }
}