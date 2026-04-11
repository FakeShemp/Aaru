using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Localization;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>Converts CSS-encrypted DVD optical sectors.</summary>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="useLong">True to use long sector reads.</param>
    /// <returns>Error number.</returns>
    ErrorNumber ConvertCssDvdOpticalSectors(IOpticalMediaImage inputOptical, IWritableOpticalImage outputOptical,
                                            bool               useLong)
    {
        if(_aborted) return ErrorNumber.NoError;
        InitProgress?.Invoke();
        InitProgress2?.Invoke();
        byte[] generatedTitleKeys = null;
        int    currentTrack       = 0;

        foreach(Track track in inputOptical.Tracks)
        {
            if(_aborted) break;

            UpdateProgress?.Invoke(string.Format(UI.Converting_sectors_in_track_0_of_1,
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

                if(trackSectors - doneSectors >= _count)
                    sectorsToDo = _count;
                else
                    sectorsToDo = (uint)(trackSectors - doneSectors);

                UpdateProgress2?.Invoke(string.Format(UI.Converting_sectors_0_to_1_in_track_2,
                                                      doneSectors + track.StartSector,
                                                      doneSectors + sectorsToDo + track.StartSector,
                                                      track.Sequence),
                                        (long)doneSectors,
                                        (long)trackSectors);

                bool         useNotLong        = false;
                bool         result            = false;
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
                        result = true;

                        if(_force)
                        {
                            ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_continuing,
                                                               errno,
                                                               doneSectors + track.StartSector));
                        }
                        else
                        {
                            StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                                       errno,
                                                                       doneSectors + track.StartSector));

                            return ErrorNumber.WriteError;
                        }
                    }

                    if(!result && sector.Length % 2352 != 0)
                    {
                        if(!_force)
                        {
                            StoppingErrorMessage
                              ?.Invoke(UI.Input_image_is_not_returning_raw_sectors_use_force_if_you_want_to_continue);

                            return ErrorNumber.InOutError;
                        }

                        useNotLong = true;
                    }
                }

                if(!useLong || useNotLong)
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
                        result = true;

                        if(_force)
                        {
                            ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_continuing,
                                                               errno,
                                                               doneSectors + track.StartSector));
                        }
                        else
                        {
                            StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                                       errno,
                                                                       doneSectors + track.StartSector));

                            return ErrorNumber.WriteError;
                        }
                    }
                }

                if(!result)
                {
                    if(_force)
                    {
                        ErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_continuing,
                                                           outputOptical.ErrorMessage,
                                                           doneSectors + track.StartSector));
                    }
                    else
                    {
                        StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_not_continuing,
                                                                   outputOptical.ErrorMessage,
                                                                   doneSectors + track.StartSector));

                        return ErrorNumber.WriteError;
                    }
                }

                doneSectors += sectorsToDo;
            }

            currentTrack++;
        }

        EndProgress2?.Invoke();
        EndProgress?.Invoke();

        return ErrorNumber.NoError;
    }
}
