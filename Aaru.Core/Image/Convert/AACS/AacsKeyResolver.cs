#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core.Devices.Dumping;
using Aaru.Decryption.Aacs;
using System.Linq;
using MediaType = Aaru.CommonTypes.MediaType;

namespace Aaru.Core.Image;

/// <summary>Loads VUK and decrypts CPS unit keys for Blu-ray and HD DVD AACS conversion.</summary>
public static class AacsKeyResolver
{
    const int MaxAacsAuxFileSize = 10 * 1024 * 1024;

    static readonly string[] UnitKeyPaths =
    [
        "AACS/Unit_Key_RO.inf",
        "AACS\\Unit_Key_RO.inf"
    ];

    static readonly string[] HddvdTkfPaths =
    [
        "AACS/VTKF000.AACS",
        "AACS\\VTKF000.AACS",
        "AACS/VTKF.AACS",
        "AACS\\VTKF.AACS",
        "AACS/ATKF000.AACS",
        "AACS\\ATKF000.AACS"
    ];

    /// <summary>Checks if every byte in the buffer is zero.</summary>
    /// <param name="buffer">Buffer to check.</param>
    /// <returns>True if every byte in the buffer is zero.</returns>
    public static bool IsAllZero(ReadOnlySpan<byte> buffer)
    {
        for(int i = 0; i < buffer.Length; i++)
        {
            if(buffer[i] != 0)
                return false;
        }

        return true;
    }

    internal static bool TryGetAacsMediaKindForFullMkb(MediaType mediaType, out AacsMediaKind kind)
    {
        if(IsHdDvdAacsMedia(mediaType))
        {
            kind = AacsMediaKind.HdDvd;

            return true;
        }

        if(IsBluRayAacsMedia(mediaType))
        {
            kind = AacsMediaKind.BluRay;

            return true;
        }

        kind = default;

        return false;
    }

    internal static bool IsBluRayAacsMedia(MediaType mediaType) =>
        mediaType is MediaType.BDROM
                  or MediaType.BDR
                  or MediaType.BDRE
                  or MediaType.BDRXL
                  or MediaType.BDREXL
                  or MediaType.UHDBD
                  or MediaType.PS3BD
                  or MediaType.PS4BD
                  or MediaType.PS5BD;

    internal static bool IsHdDvdAacsMedia(MediaType mediaType) =>
        mediaType is MediaType.HDDVDROM
                  or MediaType.HDDVDRAM
                  or MediaType.HDDVDR
                  or MediaType.HDDVDRW
                  or MediaType.HDDVDRDL
                  or MediaType.HDDVDRWDL;

    /// <summary>
    ///     Loads the full MKB from the input image (tag or UDF paths), derives MK with <paramref name="deviceKeysFile" />,
    ///     writes <see cref="MediaTagType.AacsMediaKey" /> to <paramref name="outputImage" />, and derives VUK when
    ///     <see cref="MediaTagType.AACS_VolumeIdentifier" /> is present.
    /// </summary>
    public static bool TryDeriveAndStoreKeysFromMkb(IMediaImage       inputImage,
                                                    IWritableImage    outputImage,
                                                    PluginRegister    plugins,
                                                    MediaType         mediaType,
                                                    string            deviceKeysFile,
                                                    out byte[]?       derivedMk,
                                                    out byte[]?       derivedVuk,
                                                    Action<string>?   updateStatus,
                                                    Action<string>?   errorMessage)
    {
        derivedMk  = null;
        derivedVuk = null;

        if(string.IsNullOrEmpty(deviceKeysFile)) return false;

        if(!TryGetAacsMediaKindForFullMkb(mediaType, out AacsMediaKind kind)) return false;

        byte[]? fullMkb = null;

        if(!AacsFullMkbReader.TryLoad(inputImage, plugins, kind, out byte[]? fsMkb, out string? loadErr) ||
            fsMkb is null)
        {
            errorMessage?.Invoke(string.Format(Aaru.Localization.Core.AACS_full_mkb_load_failed_0, loadErr ?? ""));

            return false;
        }

        fullMkb = fsMkb;
        updateStatus?.Invoke(Aaru.Localization.Core.AACS_full_mkb_loaded);

        if(!AacsDeviceKeys.TryLoad(deviceKeysFile, out IReadOnlyList<AacsDeviceKey>? keys, out string? dkErr))
        {
            errorMessage?.Invoke(string.Format(Aaru.Localization.Core.AACS_device_keys_file_invalid_0, dkErr ?? ""));

            return false;
        }

        if(keys is null) return false;

        if(!AacsMkbProcessor.TryDeriveMediaKey(fullMkb, keys, out byte[]? mk, out string? mkErr))
        {
            errorMessage?.Invoke(string.Format(Aaru.Localization.Core.AACS_media_key_derivation_failed_0,
                                               mkErr ?? ""));

            return false;
        }

        byte[] mediaKey = mk!;
        derivedMk = mediaKey;

        if(!outputImage.WriteMediaTag(mediaKey, MediaTagType.AacsMediaKey))
        {
            errorMessage?.Invoke(string.Format(Aaru.Localization.Core.AACS_convert_failed_writing_mk_tag_0,
                                               outputImage.ErrorMessage ?? ""));

            return false;
        }

        updateStatus?.Invoke(Aaru.Localization.Core.AACS_media_key_derived);

        if(inputImage.ReadMediaTag(MediaTagType.AACS_VolumeIdentifier, out byte[]? vid) != ErrorNumber.NoError ||
           vid is not { Length: 16 } ||
           IsAllZero(vid))
        {
            updateStatus?.Invoke(Aaru.Localization.Core.AACS_convert_volume_identifier_missing_skipping_vuk);

            return true;
        }

        byte[] vukBuf = new byte[16];
        AacsCrypto.DeriveVolumeUniqueKey(mk, vid, vukBuf);
        derivedVuk = vukBuf;

        if(!outputImage.WriteMediaTag(vukBuf, MediaTagType.AacsVolumeUniqueKey))
        {
            errorMessage?.Invoke(string.Format(Aaru.Localization.Core.AACS_convert_failed_writing_vuk_tag_0,
                                               outputImage.ErrorMessage ?? ""));

            return false;
        }

        updateStatus?.Invoke(Aaru.Localization.Core.AACS_volume_unique_key_derived);

        return true;
    }

    /// <summary>
    ///     Obtains decrypted 16-byte CPS unit keys in disc order, or <see langword="null"/> if keys cannot be loaded.
    /// </summary>
    /// <param name="image">Input optical media image.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="encoding">Encoding.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="convertDerivedMk">Optional Media Key from the same convert run (device-keys derivation).</param>
    /// <param name="convertDerivedVuk">Optional Volume Unique Key from the same convert run.</param>
    /// <returns>Decrypted CPS unit keys.</returns>
    public static byte[][]? TryGetDecryptedCpsUnitKeys(IOpticalMediaImage image, PluginRegister plugins,
                                                       Encoding? encoding, out string? errorMessage,
                                                       byte[]? convertDerivedMk = null,
                                                       byte[]? convertDerivedVuk = null)
    {
        errorMessage = null;
        encoding ??= Encoding.UTF8;

        if(!TryResolveVolumeUniqueKey(image,
                                    out byte[] vuk,
                                    out errorMessage,
                                    convertDerivedMk,
                                    convertDerivedVuk))
            return null;

        if(!TryLoadEncryptedCpsUnitKeys(image, plugins, encoding, out byte[][]? encKeys, out errorMessage) ||
           encKeys is null)
            return null;

        byte[][] decrypted = new byte[encKeys.Length][];

        for(int i = 0; i < encKeys.Length; i++)
        {
            if(encKeys[i].Length != 16)
            {
                errorMessage = Aaru.Localization.Core.Aacs_encrypted_unit_key_invalid_length;

                return null;
            }

            decrypted[i] = new byte[16];
            AacsCrypto.DecryptCpsUnitKey(vuk, encKeys[i], decrypted[i]);
        }

        return decrypted;
    }

    /// <summary>
    ///     Obtains decrypted 16-byte CPS unit keys for HD DVD (VTKF/VTUF <c>.AACS</c> or <see cref="MediaTagType.AACS_DataKeys"/>),
    ///     or <see langword="null"/> if keys cannot be loaded.
    /// </summary>
    /// <param name="image">Input optical media image.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="encoding">Encoding.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="convertDerivedMk">Optional Media Key from the same convert run (device-keys derivation).</param>
    /// <param name="convertDerivedVuk">Optional Volume Unique Key from the same convert run.</param>
    /// <returns>Decrypted CPS unit keys.</returns>
    public static byte[][]? TryGetDecryptedCpsUnitKeysHddvd(IOpticalMediaImage image, PluginRegister plugins,
                                                            Encoding? encoding, out string? errorMessage,
                                                            byte[]? convertDerivedMk = null,
                                                            byte[]? convertDerivedVuk = null)
    {
        errorMessage = null;
        encoding ??= Encoding.UTF8;

        if(!TryResolveVolumeUniqueKey(image,
                                    out byte[] vuk,
                                    out errorMessage,
                                    convertDerivedMk,
                                    convertDerivedVuk))
            return null;

        if(!TryLoadEncryptedCpsUnitKeysHddvd(image, plugins, encoding, out byte[][]? encKeys, out errorMessage) ||
           encKeys is null)
            return null;

        byte[][] decrypted = new byte[encKeys.Length][];

        for(int i = 0; i < encKeys.Length; i++)
        {
            if(encKeys[i].Length != 16)
            {
                errorMessage = Aaru.Localization.Core.Aacs_encrypted_unit_key_invalid_length;

                return null;
            }

            decrypted[i] = new byte[16];

            if(IsAllZero(encKeys[i]))
                continue;

            AacsCrypto.DecryptCpsUnitKey(vuk, encKeys[i], decrypted[i]);
        }

        return decrypted;
    }

    /// <summary>Tries to resolve the volume unique key from the media tags.</summary>
    /// <param name="image">Input optical media image.</param>
    /// <param name="vuk">Volume unique key.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="convertDerivedMk">Optional Media Key from the same convert run (device-keys derivation).</param>
    /// <param name="convertDerivedVuk">Optional Volume Unique Key from the same convert run.</param>
    /// <returns>True if the volume unique key was resolved successfully.</returns>
    internal static bool TryResolveVolumeUniqueKey(IOpticalMediaImage image, out byte[] vuk, out string? errorMessage,
                                                   byte[]? convertDerivedMk = null,
                                                   byte[]? convertDerivedVuk = null)
    {
        vuk          = new byte[16];
        errorMessage = null;

        if(convertDerivedVuk is { Length: 16 } &&
           !IsAllZero(convertDerivedVuk))
        {
            Buffer.BlockCopy(convertDerivedVuk, 0, vuk, 0, 16);

            return true;
        }

        if(image.ReadMediaTag(MediaTagType.AacsVolumeUniqueKey, out byte[]? tagVuk) == ErrorNumber.NoError &&
           tagVuk is { Length: 16 } &&
           !IsAllZero(tagVuk))
        {
            Buffer.BlockCopy(tagVuk, 0, vuk, 0, 16);

            return true;
        }

        byte[]? mk = null;

        if(convertDerivedMk is { Length: 16 } &&
           !IsAllZero(convertDerivedMk))
            mk = convertDerivedMk;
        else if(image.ReadMediaTag(MediaTagType.AacsMediaKey, out byte[]? mkTag) == ErrorNumber.NoError &&
                mkTag is { Length: 16 } &&
                !IsAllZero(mkTag))
            mk = mkTag;

        if(mk is null)
        {
            errorMessage = Aaru.Localization.Core.Aacs_missing_volume_unique_key;

            return false;
        }

        if(image.ReadMediaTag(MediaTagType.AACS_VolumeIdentifier, out byte[]? vid) != ErrorNumber.NoError ||
           vid is not { Length: 16 } ||
           IsAllZero(vid))
        {
            errorMessage = Aaru.Localization.Core.Aacs_missing_volume_unique_key;

            return false;
        }

        AacsCrypto.DeriveVolumeUniqueKey(mk, vid, vuk);

        return true;
    }

    /// <summary>Tries to load the encrypted CPS unit keys from the media tags or filesystem.</summary>
    /// <param name="image">Input optical media image.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="encoding">Encoding.</param>
    /// <param name="encKeys">Encrypted CPS unit keys.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <returns>True if the encrypted CPS unit keys were loaded successfully.</returns>
    static bool TryLoadEncryptedCpsUnitKeys(IOpticalMediaImage image, PluginRegister plugins, Encoding encoding,
                                            out byte[][]? encKeys, out string? errorMessage)
    {
        encKeys      = null;
        errorMessage = null;

        if(image.ReadMediaTag(MediaTagType.AACS_DataKeys, out byte[]? dataKeys) == ErrorNumber.NoError &&
           dataKeys is { Length: > 0 })
        {
            UnitKeyRoParseResult? parsed = UnitKeyRoParseResult.TryParse(dataKeys);

            if(parsed != null)
            {
                if(parsed.IsAacs2Layout)
                {
                    errorMessage = Aaru.Localization.Core.Aacs2_unit_keys_not_supported;

                    return false;
                }

                encKeys = parsed.EncryptedCpsUnitKeys;

                if(encKeys.Length > 0)
                    return true;
            }

            UnitKeyRoParseResult? raw = UnitKeyRoParseResult.TryParseRawEncryptedKeys(dataKeys);

            if(raw != null)
            {
                encKeys = raw.EncryptedCpsUnitKeys;

                if(encKeys.Length > 0)
                    return true;
            }
        }

        if(TryReadUnitKeyInfFromFilesystem(image, plugins, encoding, out byte[]? fileBytes) &&
           fileBytes is { Length: > 0 })
        {
            UnitKeyRoParseResult? parsed = UnitKeyRoParseResult.TryParse(fileBytes);

            if(parsed == null)
            {
                errorMessage = Aaru.Localization.Core.Aacs_unit_key_inf_invalid;

                return false;
            }

            if(parsed.IsAacs2Layout)
            {
                errorMessage = Aaru.Localization.Core.Aacs2_unit_keys_not_supported;

                return false;
            }

            encKeys = parsed.EncryptedCpsUnitKeys;

            return encKeys.Length > 0;
        }

        errorMessage = Aaru.Localization.Core.Aacs_missing_unit_keys;

        return false;
    }

    /// <summary>Tries to load encrypted CPS unit keys for HD DVD from tags or <c>AACS/VTKF*.AACS</c> / <c>VTUF*.AACS</c>.</summary>
    static bool TryLoadEncryptedCpsUnitKeysHddvd(IOpticalMediaImage image, PluginRegister plugins, Encoding encoding,
                                                 out byte[][]? encKeys, out string? errorMessage)
    {
        encKeys      = null;
        errorMessage = null;

        if(image.ReadMediaTag(MediaTagType.AACS_DataKeys, out byte[]? dataKeys) == ErrorNumber.NoError &&
           dataKeys is { Length: > 0 })
        {
            if(TryParseEncryptedKeysForHddvd(dataKeys, out encKeys, out errorMessage) && encKeys is { Length: > 0 })
                return true;

            if(errorMessage != null)
                return false;
        }

        if(TryReadHddvdTkfFromFilesystem(image, plugins, encoding, out byte[]? fileBytes) &&
           fileBytes is { Length: > 0 })
        {
            if(!TryParseEncryptedKeysForHddvd(fileBytes, out encKeys, out errorMessage) || encKeys is null ||
               encKeys.Length == 0)
            {
                errorMessage ??= Aaru.Localization.Core.Aacs_hddvd_unit_keys_invalid;

                return false;
            }

            return true;
        }

        errorMessage = Aaru.Localization.Core.Aacs_hddvd_missing_unit_keys;

        return false;
    }

    /// <summary>Parses HD DVD Title Key File or falls back to BD / raw key blobs.</summary>
    static bool TryParseEncryptedKeysForHddvd(ReadOnlySpan<byte> data, out byte[][]? encKeys, out string? errorMessage)
    {
        encKeys      = null;
        errorMessage = null;

        UnitKeyRoParseResult? parsed = UnitKeyRoParseResult.TryParseHddvdTkf(data);

        parsed ??= UnitKeyRoParseResult.TryParse(data);

        if(parsed != null)
        {
            if(parsed.IsAacs2Layout)
            {
                errorMessage = Aaru.Localization.Core.Aacs2_unit_keys_not_supported;

                return false;
            }

            encKeys = parsed.EncryptedCpsUnitKeys;

            return encKeys.Length > 0;
        }

        UnitKeyRoParseResult? raw = UnitKeyRoParseResult.TryParseRawEncryptedKeys(data);

        if(raw != null)
        {
            encKeys = raw.EncryptedCpsUnitKeys;

            return encKeys.Length > 0;
        }

        return false;
    }

    /// <summary>Tries to read an HD DVD Title Key File (<c>VTKF*.AACS</c> / <c>ATKF*.AACS</c>) from the filesystem.</summary>
    static bool TryReadHddvdTkfFromFilesystem(IOpticalMediaImage image, PluginRegister plugins, Encoding encoding,
                                              out byte[]? fileBytes)
    {
        fileBytes = null;

        foreach(Partition partition in Partitions.GetAll(image))
        {
            foreach(IReadOnlyFilesystem rofs in plugins.Filesystems.Values.OfType<IReadOnlyFilesystem>())
            {
                if(!rofs.Identify(image, partition))
                    continue;

                if(rofs.Mount(image, partition, encoding, null, null) != ErrorNumber.NoError)
                    continue;

                try
                {
                    foreach(string path in HddvdTkfPaths)
                    {
                        if(TryReadFileFromFs(rofs, path, out fileBytes))
                            return true;
                    }

                    if(rofs.OpenDir("AACS", out IDirNode? dirNode) == ErrorNumber.NoError && dirNode is not null)
                    {
                        try
                        {
                            while(true)
                            {
                                ErrorNumber errno = rofs.ReadDir(dirNode, out string? filename);

                                if(errno != ErrorNumber.NoError || filename is null)
                                    break;

                                if(!IsTkfFilename(filename))
                                    continue;

                                if(TryReadFileFromFs(rofs, "AACS/" + filename, out fileBytes))
                                    return true;
                            }
                        }
                        finally
                        {
                            rofs.CloseDir(dirNode);
                        }
                    }
                }
                finally
                {
                    rofs.Unmount();
                }
            }
        }

        return false;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="filename"/> matches an HD DVD TKF naming pattern.</summary>
    static bool IsTkfFilename(string filename) =>
        filename.EndsWith(".AACS", StringComparison.OrdinalIgnoreCase) &&
        (filename.StartsWith("VTKF", StringComparison.OrdinalIgnoreCase) ||
         filename.StartsWith("ATKF", StringComparison.OrdinalIgnoreCase));

    /// <summary>Reads a single file from an already-mounted read-only filesystem.</summary>
    static bool TryReadFileFromFs(IReadOnlyFilesystem rofs, string path, out byte[]? fileBytes)
    {
        fileBytes = null;

        if(rofs.OpenFile(path, out IFileNode? node) != ErrorNumber.NoError || node is null)
            return false;

        try
        {
            if(node.Length <= 0 || node.Length > MaxAacsAuxFileSize)
                return false;

            byte[] buf = new byte[node.Length];

            if(rofs.ReadFile(node, node.Length, buf, out long read) != ErrorNumber.NoError ||
               read != node.Length)
                return false;

            fileBytes = buf;

            return true;
        }
        finally
        {
            rofs.CloseFile(node);
        }
    }

    /// <summary>Tries to read the <c>Unit_Key_RO.inf</c> file from the filesystem.</summary>
    /// <param name="image">Input optical media image.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="encoding">Encoding.</param>
    /// <param name="fileBytes">File bytes.</param>
    /// <returns>True if the <c>Unit_Key_RO.inf</c> file was read successfully.</returns>
    static bool TryReadUnitKeyInfFromFilesystem(IOpticalMediaImage image, PluginRegister plugins, Encoding encoding,
                                                out byte[]? fileBytes)
    {
        fileBytes = null;

        foreach(Partition partition in Partitions.GetAll(image))
        {
            foreach(IReadOnlyFilesystem rofs in plugins.Filesystems.Values.OfType<IReadOnlyFilesystem>())
            {
                if(!rofs.Identify(image, partition))
                    continue;

                if(rofs.Mount(image, partition, encoding, null, null) != ErrorNumber.NoError)
                    continue;

                try
                {
                    foreach(string path in UnitKeyPaths)
                    {
                        if(TryReadFileFromFs(rofs, path, out fileBytes))
                            return true;
                    }
                }
                finally
                {
                    rofs.Unmount();
                }
            }
        }

        return false;
    }
}
