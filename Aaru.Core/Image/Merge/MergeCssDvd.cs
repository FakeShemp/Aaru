// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// --[ Description ] ----------------------------------------------------------
//
//     DVD CSS optical sector merge pipeline.
//
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Localization;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    /// <summary>Merges CSS-encrypted DVD optical sectors.</summary>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="useLong">True to use long sector reads.</param>
    /// <returns>Error number.</returns>
    ErrorNumber CopyCssDvdOpticalSectorsPrimary(IOpticalMediaImage inputOptical, IWritableOpticalImage outputOptical,
                                              bool useLong)
    {
        if(_aborted) return ErrorNumber.NoError;

        InitProgress?.Invoke();
        InitProgress2?.Invoke();
        byte[] generatedTitleKeys = null;
        int currentTrack = 0;

        foreach(Track track in inputOptical.Tracks)
        {
            if(_aborted) break;

            UpdateProgress?.Invoke(string.Format(UI.Copying_sectors_in_track_0_of_1,
                                                 currentTrack + 1,
                                                 inputOptical.Tracks.Count),
                                   currentTrack,
                                   inputOptical.Tracks.Count);

            ulong doneSectors  = 0;
            ulong trackSectors = track.EndSector - track.StartSector + 1;

            while(doneSectors < trackSectors)
            {
                if(_aborted) break;

                byte[] sector;

                uint sectorsToDo;

                if(trackSectors - doneSectors >= (ulong)count)
                    sectorsToDo = (uint)count;
                else
                    sectorsToDo = (uint)(trackSectors - doneSectors);

                UpdateProgress2?.Invoke(string.Format(UI.Copying_sectors_0_to_1_in_track_2,
                                                      doneSectors + track.StartSector,
                                                      doneSectors + sectorsToDo + track.StartSector,
                                                      track.Sequence),
                                        (long)doneSectors,
                                        (long)trackSectors);

                bool         result;
                SectorStatus sectorStatus      = SectorStatus.NotDumped;
                SectorStatus[] sectorStatusArray = new SectorStatus[1];
                ErrorNumber  errno;

                if(useLong)
                {
                    errno = sectorsToDo == 1
                                ? inputOptical.ReadSectorLong(doneSectors + track.StartSector,
                                                              false,
                                                              out sector,
                                                              out sectorStatus)
                                : inputOptical.ReadSectorsLong(doneSectors + track.StartSector,
                                                               false,
                                                               sectorsToDo,
                                                               out sector,
                                                               out sectorStatusArray);

                    if(errno == ErrorNumber.NoError)
                    {
                        CssDvdSectorDecrypt.ApplyCssAfterReadLong(ref sector,
                                                                    ref sectorStatus,
                                                                    sectorStatusArray,
                                                                    sectorsToDo,
                                                                    sectorsToDo == 1,
                                                                    inputOptical,
                                                                    doneSectors + track.StartSector,
                                                                    _plugins,
                                                                    ref generatedTitleKeys,
                                                                    () => _aborted,
                                                                    MODULE_NAME);

                        result = sectorsToDo == 1
                                     ? outputOptical.WriteSectorLong(sector,
                                                                     doneSectors + track.StartSector,
                                                                     false,
                                                                     sectorStatus)
                                     : outputOptical.WriteSectorsLong(sector,
                                                                      doneSectors + track.StartSector,
                                                                      false,
                                                                      sectorsToDo,
                                                                      sectorStatusArray);
                    }
                    else
                    {
                        StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                                   errno,
                                                                   doneSectors + track.StartSector));

                        return ErrorNumber.WriteError;
                    }

                    if(!result && sector.Length % 2352 != 0)
                    {
                        StoppingErrorMessage?.Invoke(UI.Input_image_is_not_returning_long_sectors_not_continuing);

                        return ErrorNumber.InOutError;
                    }
                }
                else
                {
                    errno = sectorsToDo == 1
                                ? inputOptical.ReadSector(doneSectors + track.StartSector,
                                                          false,
                                                          out sector,
                                                          out sectorStatus)
                                : inputOptical.ReadSectors(doneSectors + track.StartSector,
                                                           false,
                                                           sectorsToDo,
                                                           out sector,
                                                           out sectorStatusArray);

                    if(errno == ErrorNumber.NoError)
                    {
                        CssDvdSectorDecrypt.ApplyCssAfterRead(ref sector,
                                                                     ref sectorStatus,
                                                                     sectorStatusArray,
                                                                     sectorsToDo,
                                                                     sectorsToDo == 1,
                                                                     inputOptical,
                                                                     doneSectors + track.StartSector,
                                                                     _plugins,
                                                                     ref generatedTitleKeys,
                                                                     () => _aborted,
                                                                     MODULE_NAME);

                        result = sectorsToDo == 1
                                     ? outputOptical.WriteSector(sector,
                                                                 doneSectors + track.StartSector,
                                                                 false,
                                                                 sectorStatus)
                                     : outputOptical.WriteSectors(sector,
                                                                  doneSectors + track.StartSector,
                                                                  false,
                                                                  sectorsToDo,
                                                                  sectorStatusArray);
                    }
                    else
                    {
                        StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                                   errno,
                                                                   doneSectors + track.StartSector));

                        return ErrorNumber.WriteError;
                    }
                }

                if(!result)
                {
                    StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_not_continuing,
                                                               outputOptical.ErrorMessage,
                                                               doneSectors + track.StartSector));

                    return ErrorNumber.WriteError;
                }

                doneSectors += sectorsToDo;
            }

            currentTrack++;
        }

        EndProgress2?.Invoke();
        EndProgress?.Invoke();

        return ErrorNumber.NoError;
    }

    /// <summary>Merges CSS-encrypted DVD optical sectors from a secondary image.</summary>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="useLong">True to use long sector reads.</param>
    /// <param name="sectorsToCopy">Sectors to copy.</param>
    /// <returns>Error number.</returns>
    ErrorNumber CopyCssDvdOpticalSectorsSecondary(IOpticalMediaImage inputOptical, IWritableOpticalImage outputOptical,
                                                  bool useLong, List<ulong> sectorsToCopy)
    {
        if(_aborted) return ErrorNumber.NoError;

        InitProgress?.Invoke();
        byte[] generatedTitleKeys   = null;
        int    howManySectorsToCopy = sectorsToCopy.Count(t => t < inputOptical.Info.Sectors);
        int howManySectorsCopied = 0;

        foreach(ulong sectorAddress in sectorsToCopy.Where(t => t < inputOptical.Info.Sectors)
                                                    .TakeWhile(_ => !_aborted))
        {
            UpdateProgress?.Invoke(string.Format(UI.Copying_sector_0, sectorAddress),
                                   howManySectorsCopied,
                                   howManySectorsToCopy);

            if(_aborted) break;

            byte[]       sector;
            bool         result;
            SectorStatus sectorStatus;
            ErrorNumber  errno;

            if(useLong)
            {
                errno = inputOptical.ReadSectorLong(sectorAddress, false, out sector, out sectorStatus);

                if(errno == ErrorNumber.NoError)
                {
                    SectorStatus[] sectorStatusArray = new SectorStatus[1];
                    CssDvdSectorDecrypt.ApplyCssAfterReadLong(ref sector,
                                                              ref sectorStatus,
                                                              sectorStatusArray,
                                                              1,
                                                              true,
                                                              inputOptical,
                                                              sectorAddress,
                                                              _plugins,
                                                              ref generatedTitleKeys,
                                                              () => _aborted,
                                                              MODULE_NAME);

                    result = outputOptical.WriteSectorLong(sector, sectorAddress, false, sectorStatus);
                }
                else
                {
                    StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                               errno,
                                                               sectorAddress));

                    return ErrorNumber.WriteError;
                }

                if(!result && sector.Length % 2352 != 0)
                {
                    StoppingErrorMessage?.Invoke(UI.Input_image_is_not_returning_long_sectors_not_continuing);

                    return ErrorNumber.InOutError;
                }
            }
            else
            {
                errno = inputOptical.ReadSector(sectorAddress, false, out sector, out sectorStatus);

                if(errno == ErrorNumber.NoError)
                {
                    SectorStatus[] sectorStatusArray = new SectorStatus[1];
                    CssDvdSectorDecrypt.ApplyCssAfterRead(ref sector,
                                                               ref sectorStatus,
                                                               sectorStatusArray,
                                                               1,
                                                               true,
                                                               inputOptical,
                                                               sectorAddress,
                                                               _plugins,
                                                               ref generatedTitleKeys,
                                                               () => _aborted,
                                                               MODULE_NAME);

                    result = outputOptical.WriteSector(sector, sectorAddress, false, sectorStatus);
                }
                else
                {
                    StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                               errno,
                                                               sectorAddress));

                    return ErrorNumber.WriteError;
                }
            }

            if(!result)
            {
                StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_not_continuing,
                                                           outputOptical.ErrorMessage,
                                                           sectorAddress));

                return ErrorNumber.WriteError;
            }

            howManySectorsCopied++;
        }

        EndProgress?.Invoke();

        return ErrorNumber.NoError;
    }
}
