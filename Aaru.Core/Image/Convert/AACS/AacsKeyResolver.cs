using System;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decryption.Aacs;

namespace Aaru.Core.Image;

/// <summary>Loads VUK and decrypts CPS unit keys for Blu-ray AACS conversion.</summary>
public static class AacsKeyResolver
{
    static readonly string[] UnitKeyPaths =
    [
        "AACS/Unit_Key_RO.inf",
        "AACS\\Unit_Key_RO.inf"
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

    /// <summary>
    ///     Obtains decrypted 16-byte CPS unit keys in disc order, or <see langword="null"/> if keys cannot be loaded.
    /// </summary>
    /// <param name="image">Input optical media image.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="encoding">Encoding.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <returns>Decrypted CPS unit keys.</returns>
    public static byte[][]? TryGetDecryptedCpsUnitKeys(IOpticalMediaImage image, PluginRegister plugins,
                                                       Encoding? encoding, out string? errorMessage)
    {
        errorMessage = null;
        encoding ??= Encoding.UTF8;

        if(!TryResolveVolumeUniqueKey(image, out byte[] vuk, out errorMessage))
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

    /// <summary>Tries to resolve the volume unique key from the media tags.</summary>
    /// <param name="image">Input optical media image.</param>
    /// <param name="vuk">Volume unique key.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <returns>True if the volume unique key was resolved successfully.</returns>
    static bool TryResolveVolumeUniqueKey(IOpticalMediaImage image, out byte[] vuk, out string? errorMessage)
    {
        vuk          = new byte[16];
        errorMessage = null;

        if(image.ReadMediaTag(MediaTagType.AacsVolumeUniqueKey, out byte[]? tagVuk) == ErrorNumber.NoError &&
           tagVuk is { Length: 16 } &&
           !IsAllZero(tagVuk))
        {
            Buffer.BlockCopy(tagVuk, 0, vuk, 0, 16);

            return true;
        }

        if(image.ReadMediaTag(MediaTagType.AacsMediaKey, out byte[]? mk) != ErrorNumber.NoError ||
           mk is not { Length: 16 } ||
           IsAllZero(mk))
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
            foreach(IFilesystem fs in plugins.Filesystems.Values)
            {
                if(fs is not IReadOnlyFilesystem rofs)
                    continue;

                if(!fs.Identify(image, partition))
                    continue;

                if(rofs.Mount(image, partition, encoding, null, null) != ErrorNumber.NoError)
                    continue;

                try
                {
                    foreach(string path in UnitKeyPaths)
                    {
                        if(rofs.OpenFile(path, out IFileNode? node) != ErrorNumber.NoError || node is null)
                            continue;

                        try
                        {
                            if(node.Length <= 0 || node.Length > 10 * 1024 * 1024)
                                continue;

                            byte[] buf = new byte[node.Length];

                            if(rofs.ReadFile(node, node.Length, buf, out long read) != ErrorNumber.NoError ||
                               read != node.Length)
                                continue;

                            fileBytes = buf;

                            return true;
                        }
                        finally
                        {
                            rofs.CloseFile(node);
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
}
