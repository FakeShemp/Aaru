using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.Localization;
using Aaru.Logging;
using Humanizer;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>
    ///     Converts media tags (TOC, lead-in, etc.) from input to output format
    ///     Handles force mode to skip unsupported tags or fail on data loss
    ///     Shows progress for each tag being converted
    /// </summary>
    /// <returns>Status code</returns>
    ErrorNumber ConvertMediaTags()
    {
        InitProgress?.Invoke();
        ErrorNumber errorNumber = ErrorNumber.NoError;

        foreach(MediaTagType mediaTag in _inputImage.Info.ReadableMediaTags.Where(mediaTag => !_force ||
                    _outputImage.SupportedMediaTags.Contains(mediaTag)))
        {
            PulseProgress?.Invoke(string.Format(UI.Converting_media_tag_0, mediaTag.Humanize()));
            ErrorNumber errno = _inputImage.ReadMediaTag(mediaTag, out byte[] tag);

            if(errno != ErrorNumber.NoError)
            {
                if(_force)
                    ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_media_tag, errno));
                else
                {
                    AaruLogging.Error(UI.Error_0_reading_media_tag_not_continuing, errno);

                    errorNumber = errno;

                    break;
                }

                continue;
            }

            if(_outputImage?.WriteMediaTag(tag, mediaTag) == true) continue;

            if(_force)
                ErrorMessage?.Invoke(string.Format(UI.Error_0_writing_media_tag, _outputImage?.ErrorMessage));
            else
            {
                StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_media_tag_not_continuing,
                                                           _outputImage?.ErrorMessage));

                errorNumber = ErrorNumber.WriteError;

                break;
            }
        }

        EndProgress?.Invoke();

        return errorNumber;
    }
}