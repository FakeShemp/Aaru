using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;
using Humanizer;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    ErrorNumber CopyMediaTags(IMediaImage primaryImage, IMediaImage secondaryImage, IWritableImage outputFormat)
    {
        if(_aborted) return ErrorNumber.NoError;

        InitProgress?.Invoke();

        foreach(MediaTagType mediaTag in primaryImage.Info.ReadableMediaTags)
        {
            PulseProgress?.Invoke(string.Format(UI.Copying_media_tag_0_from_primary_image, mediaTag.Humanize()));

            ErrorNumber errno = primaryImage.ReadMediaTag(mediaTag, out byte[] tag);

            if(errno != ErrorNumber.NoError)
            {
                StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_media_tag_not_continuing, errno));

                return errno;
            }

            if(outputFormat?.WriteMediaTag(tag, mediaTag) == true) continue;

            StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_media_tag_not_continuing,
                                                       outputFormat?.ErrorMessage));

            return ErrorNumber.WriteError;
        }

        foreach(MediaTagType mediaTag in secondaryImage.Info.ReadableMediaTags)
        {
            if(!useSecondaryTags && primaryImage.Info.ReadableMediaTags.Contains(mediaTag)) continue;

            PulseProgress?.Invoke(string.Format(UI.Copying_media_tag_0_from_secondary_image, mediaTag.Humanize()));

            ErrorNumber errno = secondaryImage.ReadMediaTag(mediaTag, out byte[] tag);

            if(errno != ErrorNumber.NoError)
            {
                StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_media_tag_not_continuing, errno));

                return errno;
            }

            if(outputFormat?.WriteMediaTag(tag, mediaTag) == true) continue;

            StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_media_tag_not_continuing,
                                                       outputFormat?.ErrorMessage));

            return ErrorNumber.WriteError;
        }

        EndProgress?.Invoke();
        return ErrorNumber.NoError;
    }
}