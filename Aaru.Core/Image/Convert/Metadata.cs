using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interop;
using Aaru.Localization;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>
    ///     Builds and applies complete ImageInfo metadata to output image
    ///     Copies input metadata and applies command-line overrides (title, comments, creator, drive info, etc.)
    ///     Sets Aaru application version and applies all metadata fields to output format
    /// </summary>
    /// <returns></returns>
    ErrorNumber SetImageMetadata()
    {
        if(_aborted) return ErrorNumber.NoError;

        var imageInfo = new CommonTypes.Structs.ImageInfo
        {
            Application           = "Aaru",
            ApplicationVersion    = Version.GetInformationalVersion(),
            Comments              = _comments              ?? _inputImage.Info.Comments,
            Creator               = _creator               ?? _inputImage.Info.Creator,
            DriveFirmwareRevision = _driveFirmwareRevision ?? _inputImage.Info.DriveFirmwareRevision,
            DriveManufacturer     = _driveManufacturer     ?? _inputImage.Info.DriveManufacturer,
            DriveModel            = _driveModel            ?? _inputImage.Info.DriveModel,
            DriveSerialNumber     = _driveSerialNumber     ?? _inputImage.Info.DriveSerialNumber,
            LastMediaSequence     = _lastMediaSequence != 0 ? _lastMediaSequence : _inputImage.Info.LastMediaSequence,
            MediaBarcode          = _mediaBarcode      ?? _inputImage.Info.MediaBarcode,
            MediaManufacturer     = _mediaManufacturer ?? _inputImage.Info.MediaManufacturer,
            MediaModel            = _mediaModel        ?? _inputImage.Info.MediaModel,
            MediaPartNumber       = _mediaPartNumber   ?? _inputImage.Info.MediaPartNumber,
            MediaSequence         = _mediaSequence != 0 ? _mediaSequence : _inputImage.Info.MediaSequence,
            MediaSerialNumber     = _mediaSerialNumber ?? _inputImage.Info.MediaSerialNumber,
            MediaTitle            = _mediaTitle        ?? _inputImage.Info.MediaTitle
        };

        if(_outputImage.SetImageInfo(imageInfo)) return ErrorNumber.NoError;

        if(!_force)
        {
            StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_setting_metadata_not_continuing,
                                                       _outputImage.ErrorMessage));

            return ErrorNumber.WriteError;
        }

        ErrorMessage?.Invoke(string.Format(Localization.Core.Error_0_setting_metadata, _outputImage.ErrorMessage));

        return ErrorNumber.NoError;
    }
}