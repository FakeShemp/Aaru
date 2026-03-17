using System;
using System.Security.Cryptography;

namespace Aaru.Core.Image.WiiU;

/// <summary>Wii U disc encryption helpers.</summary>
static class WiiuCrypto
{
    /// <summary>Wii U common key — publicly known constant.</summary>
    public static readonly byte[] WIIU_COMMON_KEY =
    [
        0xD7, 0xB0, 0x04, 0x02, 0x65, 0x9B, 0xA2, 0xAB, 0xD2, 0xCB, 0x0D, 0xB2, 0x7F, 0xA2, 0xB6, 0x56
    ];

    /// <summary>Wii U physical sector size (32 KiB).</summary>
    public const int SECTOR_SIZE = 0x8000;

    /// <summary>Logical sector size used in AaruFormat (2048 bytes).</summary>
    public const int LOGICAL_SECTOR_SIZE = 2048;

    /// <summary>Number of logical sectors per physical sector.</summary>
    public const int LOGICAL_PER_PHYSICAL = SECTOR_SIZE / LOGICAL_SECTOR_SIZE;

    /// <summary>Number of plaintext physical sectors at the start of the disc (header).</summary>
    public const int HEADER_PHYSICAL_SECTORS = 3;

    /// <summary>Byte offset where the encrypted region begins.</summary>
    public const uint ENCRYPTED_OFFSET = 0x18000U;

    /// <summary>Decrypted TOC signature (big-endian at offset 0).</summary>
    public const uint TOC_SIGNATURE = 0xCCA6E67BU;

    /// <summary>
    ///     Decrypt a full physical sector (0x8000 bytes) in-place using AES-128-CBC with IV=0.
    /// </summary>
    /// <param name="key">16-byte AES-128 key.</param>
    /// <param name="data">Buffer to decrypt (must be a multiple of 16 bytes).</param>
    /// <returns>Decrypted data.</returns>
    public static byte[] DecryptSector(byte[] key, byte[] data)
    {
        using Aes aes = Aes.Create();
        aes.Key     = key;
        aes.IV      = new byte[16];
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform decryptor = aes.CreateDecryptor();

        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    /// <summary>
    ///     Decrypt a title key using the Wii U common key.
    ///     AES-128-CBC with IV = titleId (8 bytes) + 8 zero bytes.
    /// </summary>
    /// <param name="encryptedTitleKey">16-byte encrypted title key from TITLE.TIK.</param>
    /// <param name="titleId">8-byte title ID from TITLE.TIK.</param>
    /// <returns>16-byte decrypted title key.</returns>
    public static byte[] DecryptTitleKey(byte[] encryptedTitleKey, byte[] titleId)
    {
        var iv = new byte[16];
        Array.Copy(titleId, 0, iv, 0, 8);

        using Aes aes = Aes.Create();
        aes.Key     = WIIU_COMMON_KEY;
        aes.IV      = iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform decryptor = aes.CreateDecryptor();

        return decryptor.TransformFinalBlock(encryptedTitleKey, 0, 16);
    }
}