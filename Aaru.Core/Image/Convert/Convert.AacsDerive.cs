#nullable enable
using System;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Core.Image;

public partial class Convert
{
    void TryDeriveAndPersistAacsMediaKeysFromDeviceKeys()
    {
        _derivedAacsMediaKeyForDecrypt         = null;
        _derivedAacsVolumeUniqueKeyForDecrypt  = null;

        if(string.IsNullOrEmpty(_aacsDeviceKeysFile)) return;

        if(_inputImage is not IMediaImage mediaImage) return;

        bool completed = AacsKeyResolver.TryDeriveAndStoreKeysFromMkb(mediaImage,
                                                                       _outputImage,
                                                                       _plugins,
                                                                       _mediaType,
                                                                       _aacsDeviceKeysFile,
                                                                       out byte[]? derivedMk,
                                                                       out byte[]? derivedVuk,
                                                                       msg => UpdateStatus?.Invoke(msg),
                                                                       msg => ErrorMessage?.Invoke(msg));

        if(completed && derivedMk is { Length: 16 }) _derivedAacsMediaKeyForDecrypt = derivedMk;

        if(derivedVuk is { Length: 16 }) _derivedAacsVolumeUniqueKeyForDecrypt = derivedVuk;
    }
}