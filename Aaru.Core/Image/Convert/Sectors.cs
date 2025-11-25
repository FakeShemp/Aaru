using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.Localization;
using Aaru.Logging;

namespace Aaru.Core.Image;

public partial class Convert
{
    ErrorNumber ConvertSectors(bool useLong, bool isTape)
    {
        InitProgress?.Invoke();
        ErrorNumber errno       = ErrorNumber.NoError;
        ulong       doneSectors = 0;

        while(doneSectors < _inputImage.Info.Sectors)
        {
            byte[] sector;

            uint sectorsToDo;

            if(isTape)
                sectorsToDo = 1;
            else if(_inputImage.Info.Sectors - doneSectors >= _count)
                sectorsToDo = _count;
            else
                sectorsToDo = (uint)(_inputImage.Info.Sectors - doneSectors);

            UpdateProgress?.Invoke(string.Format(UI.Converting_sectors_0_to_1, doneSectors, doneSectors + sectorsToDo),
                                   (long)doneSectors,
                                   (long)_inputImage.Info.Sectors);

            bool         result;
            SectorStatus sectorStatus      = SectorStatus.NotDumped;
            var          sectorStatusArray = new SectorStatus[1];

            if(useLong)
            {
                errno = sectorsToDo == 1
                            ? _inputImage.ReadSectorLong(doneSectors, false, out sector, out sectorStatus)
                            : _inputImage.ReadSectorsLong(doneSectors,
                                                          false,
                                                          sectorsToDo,
                                                          out sector,
                                                          out sectorStatusArray);

                if(errno == ErrorNumber.NoError)
                {
                    result = sectorsToDo == 1
                                 ? _outputImage.WriteSectorLong(sector, doneSectors, false, sectorStatus)
                                 : _outputImage.WriteSectorsLong(sector,
                                                                 doneSectors,
                                                                 false,
                                                                 sectorsToDo,
                                                                 sectorStatusArray);
                }
                else
                {
                    result = true;

                    if(_force)
                        ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_continuing, errno, doneSectors));
                    else
                    {
                        StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                                   errno,
                                                                   doneSectors));

                        return errno;
                    }
                }
            }
            else
            {
                errno = sectorsToDo == 1
                            ? _inputImage.ReadSector(doneSectors, false, out sector, out sectorStatus)
                            : _inputImage.ReadSectors(doneSectors,
                                                      false,
                                                      sectorsToDo,
                                                      out sector,
                                                      out sectorStatusArray);

                if(errno == ErrorNumber.NoError)
                {
                    result = sectorsToDo == 1
                                 ? _outputImage.WriteSector(sector, doneSectors, false, sectorStatus)
                                 : _outputImage.WriteSectors(sector,
                                                             doneSectors,
                                                             false,
                                                             sectorsToDo,
                                                             sectorStatusArray);
                }
                else
                {
                    result = true;

                    if(_force)
                        ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_continuing, errno, doneSectors));
                    else
                    {
                        StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                                   errno,
                                                                   doneSectors));

                        return errno;
                    }
                }
            }

            if(!result)
            {
                if(_force)
                {
                    ErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_continuing,
                                                       _outputImage.ErrorMessage,
                                                       doneSectors));
                }
                else
                {
                    StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_not_continuing,
                                                               _outputImage.ErrorMessage,
                                                               doneSectors));

                    return ErrorNumber.WriteError;
                }
            }

            doneSectors += sectorsToDo;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber ConvertSectorsTags(bool useLong)
    {
        foreach(SectorTagType tag in _inputImage.Info.ReadableSectorTags.TakeWhile(_ => useLong))
        {
            switch(tag)
            {
                case SectorTagType.AppleSonyTag:
                case SectorTagType.AppleProfileTag:
                case SectorTagType.PriamDataTowerTag:
                case SectorTagType.CdSectorSync:
                case SectorTagType.CdSectorHeader:
                case SectorTagType.CdSectorSubHeader:
                case SectorTagType.CdSectorEdc:
                case SectorTagType.CdSectorEccP:
                case SectorTagType.CdSectorEccQ:
                case SectorTagType.CdSectorEcc:
                    // These tags are inline in long sector
                    continue;
            }

            if(_force && !_outputImage.SupportedSectorTags.Contains(tag)) continue;

            ulong       doneSectors = 0;
            ErrorNumber errno;

            InitProgress?.Invoke();

            while(doneSectors < _inputImage.Info.Sectors)
            {
                uint sectorsToDo;

                if(_inputImage.Info.Sectors - doneSectors >= _count)
                    sectorsToDo = _count;
                else
                    sectorsToDo = (uint)(_inputImage.Info.Sectors - doneSectors);

                UpdateProgress?.Invoke(string.Format(UI.Converting_tag_2_for_sectors_0_to_1,
                                                     doneSectors,
                                                     doneSectors + sectorsToDo,
                                                     tag),
                                       (long)doneSectors,
                                       (long)_inputImage.Info.Sectors);

                bool result;

                errno = sectorsToDo == 1
                            ? _inputImage.ReadSectorTag(doneSectors, false, tag, out byte[] sector)
                            : _inputImage.ReadSectorsTag(doneSectors, false, sectorsToDo, tag, out sector);

                if(errno == ErrorNumber.NoError)
                {
                    result = sectorsToDo == 1
                                 ? _outputImage.WriteSectorTag(sector, doneSectors, false, tag)
                                 : _outputImage.WriteSectorsTag(sector, doneSectors, false, sectorsToDo, tag);
                }
                else
                {
                    result = true;

                    if(_force)
                        AaruLogging.Error(UI.Error_0_reading_sector_1_continuing, errno, doneSectors);
                    else
                    {
                        AaruLogging.Error(UI.Error_0_reading_sector_1_not_continuing, errno, doneSectors);

                        return errno;
                    }
                }

                if(!result)
                {
                    if(_force)
                    {
                        AaruLogging.Error(UI.Error_0_writing_sector_1_continuing,
                                          _outputImage.ErrorMessage,
                                          doneSectors);
                    }
                    else
                    {
                        AaruLogging.Error(UI.Error_0_writing_sector_1_not_continuing,
                                          _outputImage.ErrorMessage,
                                          doneSectors);

                        return ErrorNumber.WriteError;
                    }
                }

                doneSectors += sectorsToDo;
            }

            EndProgress?.Invoke();
        }

        return ErrorNumber.NoError;
    }
}