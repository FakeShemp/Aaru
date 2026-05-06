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
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using BcECPoint = Org.BouncyCastle.Math.EC.ECPoint;

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

    /// <summary>
    ///     The fixed 16-byte seed used by AES-G3, the AACS one-way function for the subset-difference
    ///     tree (AACS Common Cryptographic Elements §3.2.4). The last byte is incremented by 0, 1 or 2
    ///     to derive the left child key, the processing key at the current node, and the right child
    ///     key respectively.
    /// </summary>
    public static readonly byte[] AacsG3Seed =
    [
        0x7B, 0x10, 0x3C, 0x5D, 0xCB, 0x08, 0xC4, 0xE5, 0x1A, 0x27, 0xB0, 0x17, 0x99, 0x05, 0x3B, 0xD9
    ];

    /// <summary>
    ///     One step of the AACS subset-difference AES-G3 derivation:
    ///     <c>AES-G3(K, lmr) = AES-128D(K, seed') XOR seed'</c>, where <c>seed'</c> is
    ///     <see cref="AacsG3Seed" /> with byte 15 replaced by <c>seed[15] + lmr</c>.
    ///     Pass <c>0</c> for the left child, <c>1</c> for the processing key at this node,
    ///     and <c>2</c> for the right child.
    /// </summary>
    /// <param name="parentKey">16-byte parent key in the subset-difference tree.</param>
    /// <param name="lmr">0 = left, 1 = current processing key, 2 = right.</param>
    /// <param name="derived">Receives the 16-byte derived key.</param>
    public static void AesG3Step(ReadOnlySpan<byte> parentKey, byte lmr, Span<byte> derived)
    {
        if(parentKey.Length != 16 || derived.Length != 16)
            throw new ArgumentException("AES-G3 requires 16-byte key and result.");

        Span<byte> seed = stackalloc byte[16];
        AacsG3Seed.AsSpan().CopyTo(seed);
        seed[15] = (byte)(seed[15] + lmr);

        AesH(parentKey, seed, derived);
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

    /// <summary>
    ///     Verify the signature of a 92-byte AACS host or drive certificate against the AACS LA root
    ///     public key. The signed body is bytes 0..51 (length and key fields); bytes 52..91 are the
    ///     ECDSA signature (r || s, each 20 bytes big-endian).
    /// </summary>
    /// <param name="cert">92-byte AACS certificate.</param>
    /// <returns><c>true</c> if the certificate is well-formed and signed by the AACS LA.</returns>
    public static bool VerifyAacsCert(ReadOnlySpan<byte> cert)
    {
        if(cert.Length != AacsCurve.CERT_SIZE) return false;

        if(cert[2] != 0x00 || cert[3] != 0x5C) return false;

        byte[] body = cert.Slice(0, AacsCurve.CERT_BODY_SIZE).ToArray();
        byte[] sig  = cert.Slice(AacsCurve.CERT_SIGNATURE_OFFSET, AacsCurve.SIGNATURE_SIZE).ToArray();

        return VerifyAacsSignature(AacsCurve.AacsLaPublicKey, body, sig);
    }

    /// <summary>
    ///     Verify an AACS ECDSA signature over the SHA-1 hash of <paramref name="message" />.
    ///     The signature is 40 bytes (r || s, each 20 bytes big-endian).
    /// </summary>
    /// <param name="publicKey">Public key on the AACS curve.</param>
    /// <param name="message">Message that was signed (any length).</param>
    /// <param name="signature">40-byte ECDSA signature (r || s).</param>
    /// <returns><c>true</c> if the signature is valid.</returns>
    public static bool VerifyAacsSignature(ECPublicKeyParameters publicKey, byte[] message, byte[] signature)
    {
        if(signature is not { Length: AacsCurve.SIGNATURE_SIZE }) return false;

        byte[] digest = SHA1.HashData(message);

        BigInteger r = new(1, signature, 0,                        AacsCurve.SCALAR_SIZE);
        BigInteger s = new(1, signature, AacsCurve.SCALAR_SIZE, AacsCurve.SCALAR_SIZE);

        ECDsaSigner signer = new();
        signer.Init(false, publicKey);

        return signer.VerifySignature(digest, r, s);
    }

    /// <summary>
    ///     Verify an AACS ECDSA signature given raw 20-byte X and Y coordinates of the public key.
    /// </summary>
    /// <param name="publicX">20-byte big-endian public key X coordinate.</param>
    /// <param name="publicY">20-byte big-endian public key Y coordinate.</param>
    /// <param name="message">Message that was signed.</param>
    /// <param name="signature">40-byte ECDSA signature (r || s).</param>
    /// <returns><c>true</c> if the signature is valid.</returns>
    public static bool VerifyAacsSignature(byte[] publicX, byte[] publicY, byte[] message, byte[] signature)
    {
        if(publicX is not { Length: AacsCurve.SCALAR_SIZE } || publicY is not { Length: AacsCurve.SCALAR_SIZE })
            return false;

        ECPublicKeyParameters key = AacsCurve.CreatePublicKey(publicX, publicY);

        return VerifyAacsSignature(key, message, signature);
    }

    /// <summary>
    ///     Sign <paramref name="message" /> with the host private key. The output is a 40-byte
    ///     ECDSA signature (r || s, each 20 bytes big-endian, zero-padded if shorter).
    /// </summary>
    /// <param name="hostPrivateKey">20-byte big-endian host private key.</param>
    /// <param name="message">Message to sign.</param>
    /// <returns>40-byte ECDSA signature on the AACS curve.</returns>
    public static byte[] SignAacs(byte[] hostPrivateKey, byte[] message)
    {
        if(hostPrivateKey is not { Length: AacsCurve.SCALAR_SIZE })
            throw new ArgumentException("Host private key must be 20 bytes.", nameof(hostPrivateKey));

        ECPrivateKeyParameters key = AacsCurve.CreatePrivateKey(hostPrivateKey);

        byte[] digest = SHA1.HashData(message);

        ECDsaSigner signer = new();
        signer.Init(true, key);

        BigInteger[] rs = signer.GenerateSignature(digest);

        byte[] signature = new byte[AacsCurve.SIGNATURE_SIZE];
        WriteFixedLengthBigEndian(rs[0], signature, 0,                      AacsCurve.SCALAR_SIZE);
        WriteFixedLengthBigEndian(rs[1], signature, AacsCurve.SCALAR_SIZE, AacsCurve.SCALAR_SIZE);

        return signature;
    }

    /// <summary>
    ///     Generate a fresh AACS host key pair: a random 20-byte private scalar and the
    ///     corresponding public point on the AACS curve serialized as 40 bytes (X || Y).
    /// </summary>
    /// <param name="hostPrivate">Receives the 20-byte private scalar.</param>
    /// <param name="hostPoint">Receives the 40-byte public point (X || Y).</param>
    public static void CreateHostKeyPair(out byte[] hostPrivate, out byte[] hostPoint)
    {
        BigInteger n = AacsCurve.Domain.N;

        byte[] scalar;
        BigInteger d;

        do
        {
            scalar = RandomNumberGenerator.GetBytes(AacsCurve.SCALAR_SIZE);
            d      = new BigInteger(1, scalar);
        }
        while(d.SignValue == 0 || d.CompareTo(n) >= 0);

        BcECPoint q = AacsCurve.Domain.G.Multiply(d).Normalize();

        hostPrivate = scalar;
        hostPoint   = new byte[AacsCurve.POINT_SIZE];
        WriteFixedLengthBigEndian(q.AffineXCoord.ToBigInteger(), hostPoint, 0,                      AacsCurve.SCALAR_SIZE);
        WriteFixedLengthBigEndian(q.AffineYCoord.ToBigInteger(), hostPoint, AacsCurve.SCALAR_SIZE, AacsCurve.SCALAR_SIZE);
    }

    /// <summary>
    ///     ECDH bus key derivation: BK = lowest-128-bits of (host_priv * drive_point).x.
    ///     See AACS Common Cryptographic Elements Book §4.3.
    /// </summary>
    /// <param name="hostPrivateKey">20-byte big-endian host private scalar.</param>
    /// <param name="drivePoint">40-byte drive public point (X || Y).</param>
    /// <param name="busKey">16-byte output bus key.</param>
    public static void DeriveBusKey(byte[] hostPrivateKey, byte[] drivePoint, Span<byte> busKey)
    {
        if(hostPrivateKey is not { Length: AacsCurve.SCALAR_SIZE })
            throw new ArgumentException("Host private key must be 20 bytes.", nameof(hostPrivateKey));

        if(drivePoint is not { Length: AacsCurve.POINT_SIZE })
            throw new ArgumentException("Drive point must be 40 bytes.", nameof(drivePoint));

        if(busKey.Length != 16) throw new ArgumentException("Bus key must be 16 bytes.", nameof(busKey));

        byte[] dx = new byte[AacsCurve.SCALAR_SIZE];
        byte[] dy = new byte[AacsCurve.SCALAR_SIZE];
        Buffer.BlockCopy(drivePoint, 0,                     dx, 0, AacsCurve.SCALAR_SIZE);
        Buffer.BlockCopy(drivePoint, AacsCurve.SCALAR_SIZE, dy, 0, AacsCurve.SCALAR_SIZE);

        BcECPoint q = AacsCurve.Curve.CreatePoint(new BigInteger(1, dx), new BigInteger(1, dy));

        BigInteger d = new(1, hostPrivateKey);

        BcECPoint shared = q.Multiply(d).Normalize();

        byte[] xBytes = shared.AffineXCoord.ToBigInteger().ToByteArrayUnsigned();

        if(xBytes.Length >= 16)
            xBytes.AsSpan(xBytes.Length - 16, 16).CopyTo(busKey);
        else
        {
            busKey.Clear();
            xBytes.AsSpan().CopyTo(busKey[(16 - xBytes.Length)..]);
        }
    }

    /// <summary>
    ///     Compute the 16-byte AES-CMAC of <paramref name="payload" /> under <paramref name="key" />.
    /// </summary>
    /// <param name="key">16-byte AES-128 key.</param>
    /// <param name="payload">Data to MAC.</param>
    /// <param name="mac">16-byte output buffer for the MAC.</param>
    public static void AesCmac(ReadOnlySpan<byte> key, ReadOnlySpan<byte> payload, Span<byte> mac)
    {
        if(key.Length != 16) throw new ArgumentException("AES-CMAC key must be 16 bytes.", nameof(key));

        if(mac.Length != 16) throw new ArgumentException("AES-CMAC output must be 16 bytes.", nameof(mac));

        CMac cmac = new(new AesEngine(), 128);
        cmac.Init(new KeyParameter(key.ToArray()));

        byte[] data = payload.ToArray();
        cmac.BlockUpdate(data, 0, data.Length);

        byte[] result = new byte[16];
        cmac.DoFinal(result, 0);
        result.AsSpan().CopyTo(mac);
    }

    /// <summary>
    ///     Verify the 16-byte AES-CMAC of a 16-byte payload (Volume ID, PMSN) under the bus key.
    /// </summary>
    /// <param name="busKey">16-byte AES key.</param>
    /// <param name="payload">16-byte payload that was MAC'd.</param>
    /// <param name="mac">16-byte expected MAC.</param>
    /// <returns><c>true</c> if the MAC matches.</returns>
    public static bool VerifyAacsMac(byte[] busKey, byte[] payload, byte[] mac)
    {
        if(busKey is not { Length: 16 } || payload is not { Length: 16 } || mac is not { Length: 16 })
            return false;

        Span<byte> computed = stackalloc byte[16];
        AesCmac(busKey, payload, computed);

        return CryptographicOperations.FixedTimeEquals(computed, mac);
    }

    static void WriteFixedLengthBigEndian(BigInteger value, byte[] dest, int offset, int length)
    {
        byte[] raw = value.ToByteArrayUnsigned();

        if(raw.Length > length) throw new InvalidOperationException("Value exceeds requested fixed length.");

        Array.Clear(dest, offset, length);
        Buffer.BlockCopy(raw, 0, dest, offset + (length - raw.Length), raw.Length);
    }
}
