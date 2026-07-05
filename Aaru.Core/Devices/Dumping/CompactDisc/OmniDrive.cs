// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : OmniDrive.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : CompactDisc dumping with OmniDrive.
//
// --[ Description ] ----------------------------------------------------------
//
//     Dumps user data part using an OmniDrive.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aaru.Checksums;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Extents;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core.Logging;
using Aaru.Decoders.CD;
using Aaru.Decoders.SCSI;
using Aaru.Devices;
using Aaru.Logging;
using Humanizer;
using Track = Aaru.CommonTypes.Structs.Track;
using TrackType = Aaru.CommonTypes.Enums.TrackType;

namespace Aaru.Core.Devices.Dumping;

partial class Dump
{
    // TODO: Data mode
    /// <summary>Reads all CD user data</summary>
    /// <param name="audioExtents">Extents with audio sectors</param>
    /// <param name="blocks">Total number of positive sectors</param>
    /// <param name="blockSize">Size of the read sector in bytes</param>
    /// <param name="currentSpeed">Current read speed</param>
    /// <param name="currentTry">Current dump hardware try</param>
    /// <param name="extents">Extents</param>
    /// <param name="ibgLog">IMGBurn log</param>
    /// <param name="imageWriteDuration">Duration of image write</param>
    /// <param name="lastSector">Last sector number</param>
    /// <param name="leadOutExtents">Lead-out extents</param>
    /// <param name="maxSpeed">Maximum speed</param>
    /// <param name="mhddLog">MHDD log</param>
    /// <param name="minSpeed">Minimum speed</param>
    /// <param name="newTrim">Is trim a new one?</param>
    /// <param name="offsetBytes">Read offset</param>
    /// <param name="sectorsForOffset">Sectors needed to fix offset</param>
    /// <param name="subSize">Subchannel size in bytes</param>
    /// <param name="supportedSubchannel">Drive's maximum supported subchannel</param>
    /// <param name="supportsLongSectors">Supports reading EDC and ECC</param>
    /// <param name="totalDuration">Total commands duration</param>
    /// <param name="tracks">Disc tracks</param>
    /// <param name="subLog">Subchannel log</param>
    /// <param name="desiredSubchannel">Subchannel desired to save</param>
    /// <param name="isrcs">List of disc ISRCs</param>
    /// <param name="mcn">Disc media catalogue number</param>
    /// <param name="subchannelExtents">List of subchannels not yet dumped correctly</param>
    /// <param name="smallestPregapLbaPerTrack">List of smallest pregap relative address per track</param>
    void ReadCdDataOmniDrive(ExtentsULong audioExtents, ulong blocks, uint blockSize, ref double currentSpeed,
                             DumpHardware currentTry, ExtentsULong extents, IbgLog ibgLog,
                             ref double imageWriteDuration, long lastSector, ExtentsULong leadOutExtents,
                             ref double maxSpeed, MhddLog mhddLog, ref double minSpeed, out bool newTrim,
                             int offsetBytes, int sectorsForOffset, uint subSize, MmcSubchannel supportedSubchannel,
                             bool supportsLongSectors, ref double totalDuration, Track[] tracks, SubchannelLog subLog,
                             MmcSubchannel desiredSubchannel, Dictionary<byte, string> isrcs, ref string mcn,
                             HashSet<int> subchannelExtents, Dictionary<byte, int> smallestPregapLbaPerTrack)
    {
        ulong              sectorSpeedStart = 0; // Used to calculate correct speed
        uint               blocksToRead;         // How many sectors to read at once
        bool               sense;                // Sense indicator
        byte[]             cmdBuf     = null;    // Data buffer
        ReadOnlySpan<byte> senseBuf   = null;    // Sense buffer
        const uint         sectorSize = 2352;    // Full sector size
        newTrim = false;
        var outputFormat = _outputPlugin as IWritableImage;

        InitProgress?.Invoke();

        int    currentReadSpeed      = _speed;
        var    crossingLeadOut       = false;
        var    failedCrossingLeadOut = false;
        var    skippingLead          = false;
        double elapsed               = 0;
        var    speedSectorCounter    = 0;

        UpdateStatus?.Invoke("[slateblue1]Setting speed to [teal]8x[/] for scrambled reading.[/]");

        _dev.SetCdSpeed(out _, RotationalControl.ClvAndImpureCav, 1416, 0, _dev.Timeout, out _);

        for(ulong i = _resume.NextBlock; (long)i <= lastSector; i += blocksToRead)
        {
            _speedStopwatch.Reset();

            if(_aborted)
            {
                currentTry.Extents = ExtentsConverter.ToMetadata(extents);
                UpdateStatus?.Invoke(Localization.Core.Aborted);

                break;
            }

            while(leadOutExtents.Contains(i))
            {
                skippingLead = true;
                i++;
            }

            if((long)i > lastSector) break;

            var firstSectorToRead = (uint)i;

            Track track = tracks.OrderBy(static t => t.StartSector).LastOrDefault(t => i >= t.StartSector);

            blocksToRead = _maximumReadable;

            if(blocksToRead == 1) blocksToRead += (uint)sectorsForOffset;

            if(blocksToRead == 0)
            {
                if(!skippingLead) i += (ulong)sectorsForOffset;

                skippingLead = false;

                continue;
            }

            if(offsetBytes < 0)
            {
                if(i == 0)
                    firstSectorToRead = uint.MaxValue - (uint)(sectorsForOffset - 1); // -1
                else
                    firstSectorToRead -= (uint)sectorsForOffset;

                if(blocksToRead <= sectorsForOffset) blocksToRead += (uint)sectorsForOffset;
            }

            if(speedSectorCounter > 1000)
            {
                _dev.SetCdSpeed(out _, RotationalControl.ClvAndImpureCav, 1416, 0, _dev.Timeout, out _);
                speedSectorCounter = 0;
            }
            else
                speedSectorCounter += (int)blocksToRead;

            if(currentSpeed > maxSpeed && currentSpeed > 0) maxSpeed = currentSpeed;

            if(currentSpeed < minSpeed && currentSpeed > 0) minSpeed = currentSpeed;

            UpdateProgress?.Invoke(string.Format(Localization.Core.Reading_sector_0_of_1_2,
                                                 i,
                                                 blocks,
                                                 ByteSize.FromMegabytes(currentSpeed).Per(_oneSecond).Humanize()),
                                   (long)i,
                                   (long)blocks);


            _speedStopwatch.Start();

            sense = _dev.OmniDriveReadCd(out cmdBuf,
                                         out senseBuf,
                                         firstSectorToRead,
                                         blocksToRead,
                                         _dev.Timeout,
                                         out _);

            _speedStopwatch.Stop();

            if(!sense && !_dev.Error)
            {
                // Because one block has been partially used to fix the offset
                if(offsetBytes != 0)
                {
                    FixOffsetData(offsetBytes,
                                  sectorSize,
                                  sectorsForOffset,
                                  supportedSubchannel,
                                  ref blocksToRead,
                                  subSize,
                                  ref cmdBuf,
                                  blockSize,
                                  failedCrossingLeadOut);
                }

                mhddLog.Write(i, _speedStopwatch.Elapsed.TotalMilliseconds, blocksToRead);
                ibgLog.Write(i, currentSpeed * 1024);
                extents.Add(i, blocksToRead, true);
                _writeStopwatch.Restart();

                var         data         = new byte[sectorSize * blocksToRead];
                var         sub          = new byte[subSize    * blocksToRead];
                var         sectorStatus = new SectorStatus[blocksToRead];
                var         sector       = new byte[sectorSize];
                List<ulong> paintBad     = [];

                for(var b = 0; b < blocksToRead; b++)
                {
                    Array.Copy(cmdBuf, (int)(0 + b * blockSize), sector, 0, sectorSize);

                    if(IsScrambledData(sector, (int)(i + (ulong)b), out _))
                    {
                        sector = Sector.Scramble(sector);
                        SectorFixResult fixStatus = CdChecksums.FixSector(sector);
                        if(fixStatus == SectorFixResult.Correct) _correctSectors++;
                        if(fixStatus == SectorFixResult.Fixed) _fixedSectors++;

                        if(fixStatus == SectorFixResult.CouldNotFix)
                        {
                            sectorStatus[b] = SectorStatus.Errored;
                            _resume.BadBlocks.Add(i + (ulong)b);
                            paintBad.Add(i          + (ulong)b);
                        }

                        Array.Copy(sector, 0, cmdBuf, (int)(0 + b * blockSize), sectorSize);
                    }

                    Array.Copy(cmdBuf, (int)(0 + b * blockSize), data, sectorSize * b, sectorSize);

                    Array.Copy(cmdBuf, (int)(sectorSize + b * blockSize), sub, subSize * b, subSize);

                    // Not already set
                    if(sectorStatus[b] != SectorStatus.Errored) sectorStatus[b] = SectorStatus.Dumped;
                }

                if(supportsLongSectors)
                    outputFormat.WriteSectorsLong(data, i, false, blocksToRead, sectorStatus);
                else
                {
                    var cooked = new MemoryStream();

                    for(var b = 0; b < blocksToRead; b++)
                    {
                        Array.Copy(cmdBuf, (int)(0 + b * blockSize), sector, 0, sectorSize);
                        byte[] cookedSector = Sector.GetUserData(sector);
                        cooked.Write(cookedSector, 0, cookedSector.Length);
                    }

                    outputFormat.WriteSectors(cooked.ToArray(), i, false, blocksToRead, sectorStatus);
                }

                bool indexesChanged = Media.CompactDisc.WriteSubchannelToImage(supportedSubchannel,
                                                                               desiredSubchannel,
                                                                               sub,
                                                                               i,
                                                                               blocksToRead,
                                                                               subLog,
                                                                               isrcs,
                                                                               (byte)track.Sequence,
                                                                               ref mcn,
                                                                               tracks,
                                                                               subchannelExtents,
                                                                               _fixSubchannelPosition,
                                                                               outputFormat as IWritableOpticalImage,
                                                                               _fixSubchannel,
                                                                               _fixSubchannelCrc,
                                                                               UpdateStatus,
                                                                               smallestPregapLbaPerTrack,
                                                                               true,
                                                                               out List<ulong> newPregapSectors);

                // Set tracks and go back
                if(indexesChanged)
                {
                    (outputFormat as IWritableOpticalImage).SetTracks([.. tracks]);

                    foreach(ulong newPregapSector in newPregapSectors)
                    {
                        if(newPregapSector == 0) continue;

                        Track sectorTrack =
                            tracks.FirstOrDefault(t => t.StartSector <= newPregapSector &&
                                                       t.EndSector   >= newPregapSector);

                        if(sectorTrack.Sequence == 1) continue;

                        Track prevTrack = tracks.FirstOrDefault(t => t.Sequence == sectorTrack.Sequence - 1);

                        if(sectorTrack.Session != prevTrack.Session) continue;

                        if(sectorTrack.Type != prevTrack.Type) _resume.BadBlocks.Add(newPregapSector);
                    }

                    if(i >= blocksToRead)
                        i -= blocksToRead;
                    else
                        i = 0;

                    if(i > 0) i--;

                    foreach(Track aTrack in tracks.Where(static aTrack => aTrack.Type == TrackType.Audio))
                        audioExtents.Add(aTrack.StartSector, aTrack.EndSector);

                    continue;
                }

                _mediaGraph?.PaintSectorsGood(i, blocksToRead);
                foreach(ulong b in paintBad) _mediaGraph?.PaintSectorsBad(b, 1);

                imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;
            }
            else
            {
                if(crossingLeadOut && Sense.Decode(senseBuf)?.ASC == 0x21)
                {
                    if(failedCrossingLeadOut) break;

                    failedCrossingLeadOut = true;
                    blocksToRead          = 0;

                    continue;
                }

                _errorLog?.WriteLine(firstSectorToRead, _dev.Error, _dev.LastError, senseBuf.ToArray());

                // TODO: Reset device after X errors
                if(_stopOnError) return; // TODO: Return more cleanly

                if(i + _skip > blocks) _skip = (uint)(blocks - i);

                // Write empty data
                _writeStopwatch.Restart();

                if(supportedSubchannel != MmcSubchannel.None)
                {
                    outputFormat.WriteSectorsLong(new byte[sectorSize * _skip],
                                                  i,
                                                  false,
                                                  _skip,
                                                  [.. Enumerable.Repeat(SectorStatus.NotDumped, (int)_skip)]);

                    if(desiredSubchannel != MmcSubchannel.None)
                    {
                        outputFormat.WriteSectorsTag(new byte[subSize * _skip],
                                                     i,
                                                     false,
                                                     _skip,
                                                     SectorTagType.CdSectorSubchannel);
                    }
                }
                else
                {
                    if(supportsLongSectors)
                    {
                        outputFormat.WriteSectorsLong(new byte[blockSize * _skip],
                                                      i,
                                                      false,
                                                      _skip,
                                                      [.. Enumerable.Repeat(SectorStatus.NotDumped, (int)_skip)]);
                    }
                    else
                    {
                        if(cmdBuf.Length % sectorSize == 0)
                        {
                            outputFormat.WriteSectors(new byte[2048 * _skip],
                                                      i,
                                                      false,
                                                      _skip,
                                                      [.. Enumerable.Repeat(SectorStatus.NotDumped, (int)_skip)]);
                        }
                        else
                        {
                            outputFormat.WriteSectorsLong(new byte[blockSize * _skip],
                                                          i,
                                                          false,
                                                          _skip,
                                                          Enumerable.Repeat(SectorStatus.NotDumped, (int)_skip)
                                                                    .ToArray());
                        }
                    }
                }

                imageWriteDuration += _writeStopwatch.Elapsed.TotalSeconds;

                for(ulong b = i; b < i + _skip; b++) _resume.BadBlocks.Add(b);

                AaruLogging.Debug(MODULE_NAME, Localization.Core.READ_error_0, Sense.PrettifySense(senseBuf));

                mhddLog.Write(i,
                              _speedStopwatch.Elapsed.TotalMilliseconds < 500
                                  ? 65535
                                  : _speedStopwatch.Elapsed.TotalMilliseconds,
                              _skip);

                AaruLogging.WriteLine(Localization.Core.Skipping_0_blocks_from_errored_block_1, _skip, i);
                i       += _skip - blocksToRead;
                newTrim =  true;
            }

            totalDuration += _speedStopwatch.Elapsed.TotalMilliseconds;

            _writeStopwatch.Stop();

            sectorSpeedStart += blocksToRead;

            _resume.NextBlock = i + blocksToRead;

            elapsed += _speedStopwatch.Elapsed.TotalMilliseconds;

            if(elapsed < 100) continue;

            currentSpeed = sectorSpeedStart * blockSize / (1048576 * elapsed / 1000);
            ibgLog.Write(i, currentSpeed                                     * 1024);
            sectorSpeedStart = 0;
            elapsed          = 0;
        }

        _speedStopwatch.Stop();
        EndProgress?.Invoke();

        _resume.BadBlocks = [.. _resume.BadBlocks.Distinct()];
    }
}