using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Images;
using Aaru.Localization;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    private ErrorNumber CreateOutputImage(IMediaImage primaryImage,    MediaType mediaType, IWritableImage outputImage,
                                          uint        negativeSectors, uint      overflowSectors)
    {
        if(_aborted) return ErrorNumber.NoError;

        InitProgress?.Invoke();
        PulseProgress?.Invoke(UI.Invoke_Opening_image_file);

        bool created = outputImage.Create(outputImagePath,
                                          mediaType,
                                          options,
                                          primaryImage.Info.Sectors,
                                          negativeSectors,
                                          overflowSectors,
                                          primaryImage.Info.SectorSize);

        if(outputImage is AaruFormat aif && errorRecovery > 0) aif.SetErasureCodingAuto((byte)errorRecovery);

        EndProgress?.Invoke();

        if(created) return ErrorNumber.NoError;

        StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_creating_output_image, outputImage.ErrorMessage));

        return ErrorNumber.CannotCreateFormat;
    }
}