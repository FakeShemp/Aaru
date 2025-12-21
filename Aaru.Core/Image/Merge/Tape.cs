using System;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    ErrorNumber ValidateTapeImage(ITapeImage primaryTape, ITapeImage secondaryTape, IWritableTapeImage outputTape)
    {
        if(_aborted || primaryTape?.IsTape != true && secondaryTape?.IsTape != true || outputTape is not null)
            return ErrorNumber.NoError;

        StoppingErrorMessage?.Invoke(UI.Input_format_contains_a_tape_image_and_is_not_supported_by_output_format);

        return ErrorNumber.UnsupportedMedia;
    }

    ErrorNumber SetupTapeImage(ITapeImage primaryTape, ITapeImage secondaryTape, IWritableTapeImage outputTape)
    {
        if(_aborted || primaryTape?.IsTape != true && secondaryTape?.IsTape != true || outputTape == null)
            return ErrorNumber.NoError;

        bool ret = outputTape.SetTape();

        // Cannot set image to tape mode
        if(ret) return ErrorNumber.NoError;

        StoppingErrorMessage?.Invoke(UI.Error_setting_output_image_in_tape_mode +
                                     Environment.NewLine                        +
                                     outputTape.ErrorMessage);

        return ErrorNumber.WriteError;
    }
}