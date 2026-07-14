// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsCrypto.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     AES primitives for Blu-ray AACS
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Security.Cryptography;

namespace Aaru.Decryption.Aacs;

/// <summary>Low-level AES operations used by Blu-ray AACS stream decryption.</summary>
public static class AacsCrypto
{
    /// <summary>AES-128 CBC IV.</summary>
    static readonly byte[] AacsCbcIv =
    [
        0x0b, 0xa0, 0xf8, 0xdd, 0xfe, 0xa6, 0x1f, 0xb3, 0xd8, 0xdf, 0x9f, 0x56, 0x6a, 0x05, 0x0f, 0x78
    ];

    /// <summary>AES-128 ECB encrypt one block.</summary>
    /// <param name="key">AES-128 key (16 bytes).</param>
    /// <param name="plaintext">Plaintext to encrypt (16 bytes).</param>
    /// <param name="ciphertext">Encrypted output (16 bytes).</param>
    public static void Aes128EcbEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
    {
        if(key.Length != 16 || plaintext.Length != 16 || ciphertext.Length != 16)
            throw new ArgumentException("AES-128 block requires 16-byte key, plaintext, and output.");

        byte[] keyArr = key.ToArray();
        byte[] ptArr  = plaintext.ToArray();

        using(Aes aes = Aes.Create())
        {
            aes.KeySize = 128;
            aes.Mode    = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key     = keyArr;

            using ICryptoTransform enc = aes.CreateEncryptor();
            byte[] ctArr = enc.TransformFinalBlock(ptArr, 0, 16);
            ctArr.AsSpan(0, 16).CopyTo(ciphertext);
        }
    }

    /// <summary>AES-128 ECB decrypt one block.</summary>
    /// <param name="key">AES-128 key (16 bytes).</param>
    /// <param name="ciphertext">Ciphertext to decrypt (16 bytes).</param>
    /// <param name="plaintext">Decrypted output (16 bytes).</param>
    public static void Aes128EcbDecrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
    {
        if(key.Length != 16 || ciphertext.Length != 16 || plaintext.Length != 16)
            throw new ArgumentException("AES-128 block requires 16-byte key, ciphertext, and output.");

        byte[] keyArr = key.ToArray();
        byte[] ctArr  = ciphertext.ToArray();

        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = keyArr;

        using ICryptoTransform dec = aes.CreateDecryptor();
        byte[] ptArr = dec.TransformFinalBlock(ctArr, 0, 16);
        ptArr.AsSpan(0, 16).CopyTo(plaintext);
    }

    /// <summary>AES-128 CBC decrypt.</summary>
    /// <param name="key">AES-128 key (16 bytes).</param>
    /// <param name="ciphertext">Ciphertext to decrypt (16 bytes).</param>
    /// <param name="plaintext">Decrypted output (16 bytes).</param>
    public static void AacsCbcDecrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
    {
        if(key.Length != 16)
            throw new ArgumentException("AES-128 key must be 16 bytes.");

        if(ciphertext.Length != plaintext.Length || ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext and plaintext spans must have the same non-zero length.");

        if((ciphertext.Length & 15) != 0)
            throw new ArgumentException("Ciphertext length must be a multiple of 16.");

        byte[] keyArr = key.ToArray();
        byte[] ctArr  = ciphertext.ToArray();
        byte[] ivArr  = (byte[])AacsCbcIv.Clone();

        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key = keyArr;
        aes.IV = ivArr;

        using ICryptoTransform dec = aes.CreateDecryptor();
        byte[] ptArr = dec.TransformFinalBlock(ctArr, 0, ctArr.Length);

        if(ptArr.Length != plaintext.Length)
            throw new InvalidOperationException("AACS CBC decrypt length mismatch.");

        ptArr.AsSpan().CopyTo(plaintext);
    }

    /// <summary>
    ///     Blu-ray CPS unit content key derivation: <c>K = AES-128E(K, X) XOR X</c>.
    ///     This is the encrypt-based one-way function used by the BD AACS Pre-recorded Video Book
    ///     for deriving the per-aligned-unit content key. It is <b>not</b> the same as the spec's
    ///     <c>AES-G</c> primitive (which is decrypt-based; see <see cref="AesH"/>).
    /// </summary>
    /// <param name="key">AES-128 key (16 bytes).</param>
    /// <param name="data">Input block (16 bytes).</param>
    /// <param name="result">Output block (16 bytes).</param>
    public static void AesG(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, Span<byte> result)
    {
        if(key.Length != 16 || data.Length != 16 || result.Length != 16)
            throw new ArgumentException("AES-G requires 16-byte key, data, and result.");

        Aes128EcbEncrypt(key, data, result);

        for(int i = 0; i < 16; i++)
            result[i] ^= data[i];
    }

    /// <summary>
    ///     AACS one-way function as defined in the AACS Introduction and Common Cryptographic
    ///     Elements: <c>AES-G(K, X) = AES-128D(K, X) XOR X</c>. Used for HD DVD pack content key
    ///     derivation (<c>Kc = AES-G(Kt, Dtk || CPIlsb_96)</c>, see HD DVD/DVD Pre-recorded Book
    ///     section 4.3.2). Despite the misleading name <see cref="AesG"/>, that function actually
    ///     uses AES-128 ENCRYPT and is only correct for BD content; HD DVD uses this DECRYPT
    ///     based variant.
    /// </summary>
    /// <param name="key">AES-128 key (16 bytes).</param>
    /// <param name="data">Input block (16 bytes).</param>
    /// <param name="result">Output block (16 bytes).</param>
    public static void AesH(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, Span<byte> result)
    {
        if(key.Length != 16 || data.Length != 16 || result.Length != 16)
            throw new ArgumentException("AES-H requires 16-byte key, data, and result.");

        Aes128EcbDecrypt(key, data, result);

        for(int i = 0; i < 16; i++)
            result[i] ^= data[i];
    }

    /// <summary>AES-128 CBC decrypt with IV = all zeros.</summary>
    /// <param name="key">AES-128 key (16 bytes).</param>
    /// <param name="ciphertext">Ciphertext (multiple of 16 bytes).</param>
    /// <param name="plaintext">Decrypted output (same length as ciphertext).</param>
    public static void AesCbcDecryptZeroIv(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
    {
        if(key.Length != 16)
            throw new ArgumentException("AES-128 key must be 16 bytes.");

        if(ciphertext.Length != plaintext.Length || ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext and plaintext spans must have the same non-zero length.");

        if((ciphertext.Length & 15) != 0)
            throw new ArgumentException("Ciphertext length must be a multiple of 16.");

        byte[] keyArr = key.ToArray();
        byte[] ctArr  = ciphertext.ToArray();
        byte[] ivArr  = new byte[16];

        using Aes aes = Aes.Create();
        aes.KeySize = 128;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key     = keyArr;
        aes.IV      = ivArr;

        using ICryptoTransform dec = aes.CreateDecryptor();
        byte[] ptArr = dec.TransformFinalBlock(ctArr, 0, ctArr.Length);

        if(ptArr.Length != plaintext.Length)
            throw new InvalidOperationException("CBC decrypt length mismatch.");

        ptArr.AsSpan().CopyTo(plaintext);
    }

    /// <summary>
    ///     Volume unique key from media key and volume ID.
    /// </summary>
    /// <param name="mediaKey">Media key (16 bytes).</param>
    /// <param name="volumeId">Volume ID (16 bytes).</param>
    /// <param name="volumeUniqueKey">Volume unique key (16 bytes).</param>
    public static void DeriveVolumeUniqueKey(ReadOnlySpan<byte> mediaKey, ReadOnlySpan<byte> volumeId,
                                            Span<byte>          volumeUniqueKey)
    {
        if(mediaKey.Length != 16 || volumeId.Length != 16 || volumeUniqueKey.Length != 16)
            throw new ArgumentException("MK, VID, and VUK must be 16 bytes.");

        Span<byte> tmp = stackalloc byte[16];
        Aes128EcbDecrypt(mediaKey, volumeId, tmp);

        for(int a = 0; a < 16; a++)
            volumeUniqueKey[a] = (byte)(tmp[a] ^ volumeId[a]);
    }

    /// <summary>Decrypt one CPS unit key from encrypted form.</summary>
    /// <param name="volumeUniqueKey">Volume unique key (16 bytes).</param>
    /// <param name="encryptedUnitKey">Encrypted CPS unit key (16 bytes).</param>
    /// <param name="decryptedUnitKey">Decrypted CPS unit key (16 bytes).</param>
    public static void DecryptCpsUnitKey(ReadOnlySpan<byte> volumeUniqueKey, ReadOnlySpan<byte> encryptedUnitKey,
                                         Span<byte>         decryptedUnitKey)
    {
        if(volumeUniqueKey.Length != 16 || encryptedUnitKey.Length != 16 || decryptedUnitKey.Length != 16)
            throw new ArgumentException("VUK and CPS unit keys must be 16 bytes.");

        Aes128EcbDecrypt(volumeUniqueKey, encryptedUnitKey, decryptedUnitKey);
    }
}
