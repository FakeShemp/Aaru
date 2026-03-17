using System;
using System.Security.Cryptography;

namespace Aaru.Core.Image.PS3;

/// <summary>PS3 disc encryption key derivation.</summary>
static class Ps3Crypto
{
    /// <summary>PS3 Encryption Round Key — publicly known constant.</summary>
    static readonly byte[] PS3_ERK =
    [
        0x38, 0x0B, 0xCF, 0x0B, 0x53, 0x45, 0x5B, 0x3C, 0x78, 0x17, 0xAB, 0x4F, 0xA3, 0xBA, 0x90, 0xED
    ];

    /// <summary>PS3 ERK IV — publicly known constant.</summary>
    static readonly byte[] PS3_ERK_IV =
    [
        0x69, 0x47, 0x47, 0x72, 0xAF, 0x6F, 0xDA, 0xB3, 0x42, 0x74, 0x3A, 0xEF, 0xAA, 0x18, 0x62, 0x87
    ];

    /// <summary>Derives a PS3 disc key from a data1 key.</summary>
    /// <param name="data1">16-byte data1 key (from disc or IRD).</param>
    /// <returns>16-byte derived disc key.</returns>
    public static byte[] DeriveDiscKey(byte[] data1)
    {
        if(data1 is not { Length: 16 }) throw new ArgumentOutOfRangeException(nameof(data1));

        var discKey = new byte[16];
        Array.Copy(data1, discKey, 16);

        using var aes = Aes.Create();
        aes.Key     = PS3_ERK;
        aes.IV      = PS3_ERK_IV;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[]                 result    = encryptor.TransformFinalBlock(discKey, 0, 16);

        return result;
    }
}