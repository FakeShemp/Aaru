using Aaru.CommonTypes.Enums;
using Aaru.Localization;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>
    ///     Creates output image file with specified parameters
    ///     Calls the output format plugin's Create() method with sector count and format options
    ///     Shows progress indicator during file creation
    /// </summary>
    /// <returns>Error code if creation fails</returns>
    private ErrorNumber CreateOutputImage()
    {
        InitProgress?.Invoke();
        PulseProgress?.Invoke(UI.Invoke_Opening_image_file);

        bool created = _outputImage.Create(_outputPath,
                                           _mediaType,
                                           _parsedOptions,
                                           _inputImage.Info.Sectors,
                                           _negativeSectors,
                                           _overflowSectors,
                                           _inputImage.Info.SectorSize);

        EndProgress?.Invoke();

        if(created) return ErrorNumber.NoError;

        StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_creating_output_image, _outputImage.ErrorMessage));

        return ErrorNumber.CannotCreateFormat;
    }
}