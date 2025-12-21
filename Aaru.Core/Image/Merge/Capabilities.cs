using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    ErrorNumber ValidateMediaCapabilities(IMediaImage    primaryImage, IMediaImage secondaryImage,
                                          IWritableImage outputImage,  MediaType   mediaType)
    {
        if(_aborted) return ErrorNumber.NoError;

        if(!outputImage.SupportedMediaTypes.Contains(mediaType))
        {
            StoppingErrorMessage?.Invoke(UI.Output_format_does_not_support_media_type);

            return ErrorNumber.UnsupportedMedia;
        }

        foreach(MediaTagType mediaTag in primaryImage.Info.ReadableMediaTags
                                                     .Where(mediaTag =>
                                                                !outputImage.SupportedMediaTags.Contains(mediaTag))
                                                     .TakeWhile(_ => !_aborted))
        {
            StoppingErrorMessage
              ?.Invoke(string.Format(UI.Media_tag_0_present_in_primary_image_will_be_lost_in_output_format, mediaTag));

            return ErrorNumber.DataWillBeLost;
        }

        foreach(MediaTagType mediaTag in secondaryImage.Info.ReadableMediaTags
                                                       .Where(mediaTag =>
                                                                  !primaryImage.Info.ReadableMediaTags
                                                                     .Contains(mediaTag) &&
                                                                  !outputImage.SupportedMediaTags.Contains(mediaTag))
                                                       .TakeWhile(_ => !_aborted))
        {
            StoppingErrorMessage
              ?.Invoke(string.Format(UI.Media_tag_0_present_in_secondary_image_will_be_lost_in_output_format,
                                     mediaTag));

            return ErrorNumber.DataWillBeLost;
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber ValidateSectorTags(IMediaImage primaryImage, IWritableImage outputImage, out bool useLong)
    {
        useLong = primaryImage.Info.ReadableSectorTags.Count != 0;

        if(_aborted) return ErrorNumber.NoError;

        foreach(SectorTagType sectorTag in primaryImage.Info.ReadableSectorTags
                                                       .Where(sectorTag =>
                                                                  !outputImage.SupportedSectorTags.Contains(sectorTag))
                                                       .TakeWhile(_ => !_aborted))
        {
            StoppingErrorMessage?.Invoke(string.Format(UI.Output_image_does_not_support_sector_tag_0_data_will_be_lost,
                                                       sectorTag));

            return ErrorNumber.DataWillBeLost;
        }

        return ErrorNumber.NoError;
    }
}