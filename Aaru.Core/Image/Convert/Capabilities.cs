using System;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.Localization;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>
    ///     Validates media type and media tag support in output format
    ///     Checks if output format supports the media type being converted
    ///     Validates all readable media tags are supported by output (unless force mode enabled)
    /// </summary>
    /// <returns>Error if required features not supported and data would be lost</returns>
    ErrorNumber ValidateMediaCapabilities()
    {
        if(!_outputImage.SupportedMediaTypes.Contains(_mediaType))
        {
            StoppingErrorMessage?.Invoke(UI.Output_format_does_not_support_media_type);

            return ErrorNumber.UnsupportedMedia;
        }

        foreach(MediaTagType mediaTag in _inputImage.Info.ReadableMediaTags.Where(mediaTag =>
                    !_outputImage.SupportedMediaTags.Contains(mediaTag) && !_force))
        {
            StoppingErrorMessage?.Invoke(string.Format(UI.Converting_image_will_lose_media_tag_0 +
                                                       Environment.NewLine                       +
                                                       UI.If_you_dont_care_use_force_option,
                                                       mediaTag));

            return ErrorNumber.DataWillBeLost;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber ValidateSectorTags(out bool useLong)
    {
        // Validates sector tag compatibility between formats
        // Sets useLong flag based on sector tag support to determine sector size (512 vs 2352 bytes)
        // Some tags like CD flags/ISRC don't require long sectors; subchannel data does
        // In force mode, skips unsupported tags; otherwise reports error if data would be lost

        useLong = _inputImage.Info.ReadableSectorTags.Count != 0;

        foreach(SectorTagType sectorTag in _inputImage.Info.ReadableSectorTags.Where(sectorTag =>
                    !_outputImage.SupportedSectorTags.Contains(sectorTag)))
        {
            if(_force)
            {
                if(sectorTag != SectorTagType.CdTrackFlags &&
                   sectorTag != SectorTagType.CdTrackIsrc  &&
                   sectorTag != SectorTagType.CdSectorSubchannel)
                    useLong = false;

                continue;
            }

            StoppingErrorMessage.Invoke(string.Format(UI.Converting_image_will_lose_sector_tag_0 +
                                                      Environment.NewLine                        +
                                                      UI
                                                         .If_you_dont_care_use_force_option_This_will_skip_all_sector_tags_converting_only_user_data,
                                                      sectorTag));

            return ErrorNumber.DataWillBeLost;
        }

        return ErrorNumber.NoError;
    }
}