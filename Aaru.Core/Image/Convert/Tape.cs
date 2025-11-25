using System;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;

namespace Aaru.Core.Image;

public partial class Convert
{
    ErrorNumber ValidateTapeImage(ITapeImage inputTape, IWritableTapeImage outputTape)
    {
        // Validates tape image format compatibility
        // Checks if input is tape-based but output format doesn't support tape images
        // Returns error if unsupported media type combination detected

        if(inputTape?.IsTape != true || outputTape is not null) return ErrorNumber.NoError;

        StoppingErrorMessage?.Invoke(UI.Input_format_contains_a_tape_image_and_is_not_supported_by_output_format);

        return ErrorNumber.UnsupportedMedia;
    }

    ErrorNumber SetupTapeImage(ITapeImage inputTape, IWritableTapeImage outputTape)
    {
        // Configures output format for tape image handling
        // Calls SetTape() on output to initialize tape mode if both input and output support tapes
        // Returns error if tape mode initialization fails

        if(inputTape?.IsTape != true || outputTape == null) return ErrorNumber.NoError;

        bool ret = outputTape.SetTape();

        // Cannot set image to tape mode
        if(ret) return ErrorNumber.NoError;

        StoppingErrorMessage?.Invoke(UI.Error_setting_output_image_in_tape_mode +
                                     Environment.NewLine                        +
                                     _outputImage.ErrorMessage);

        return ErrorNumber.WriteError;
    }
}