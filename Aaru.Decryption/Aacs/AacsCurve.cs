// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsCurve.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     The fixed 160-bit prime elliptic curve used by AACS for ECDSA signing
//     and ECDH bus key derivation, plus the AACS Licensing Authority root
//     public key used to verify host and drive certificates.
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

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace Aaru.Decryption.Aacs;

/// <summary>
///     The AACS elliptic curve domain parameters and AACS Licensing Authority root public key.
///     Source: AACS Common Cryptographic Elements Book, Annex A; cross-checked against
///     <c>libaacs</c> <c>crypto.c</c> and <c>aacskeys</c> <c>aacs_ecdsa.cpp</c>.
/// </summary>
public static class AacsCurve
{
    /// <summary>Prime modulus p of GF(p).</summary>
    public const string P_HEX = "9DC9D81355ECCEB560BDB09EF9EAE7C479A7D7DF";

    /// <summary>Curve parameter a (= p - 3 mod p).</summary>
    public const string A_HEX = "9DC9D81355ECCEB560BDB09EF9EAE7C479A7D7DC";

    /// <summary>Curve parameter b.</summary>
    public const string B_HEX = "402DAD3EC1CBCD165248D68E1245E0C4DAACB1D8";

    /// <summary>Order n of the generator G.</summary>
    public const string N_HEX = "9DC9D81355ECCEB560BDC44F54817B2C7F5AB017";

    /// <summary>x-coordinate of the generator point G.</summary>
    public const string G_X_HEX = "2E64FC22578351E6F4CCA7EB81D0A4BDC54CCEC6";

    /// <summary>y-coordinate of the generator point G.</summary>
    public const string G_Y_HEX = "0914A25DD05442889DB455C7F23C9A0707F5CBB9";

    /// <summary>x-coordinate of the AACS Licensing Authority root public key.</summary>
    public const string AACS_LA_PUBKEY_X_HEX = "63C21DFFB2B2798A13B58D61166C4E4AAC8A0772";

    /// <summary>y-coordinate of the AACS Licensing Authority root public key.</summary>
    public const string AACS_LA_PUBKEY_Y_HEX = "137EC638818FD98FA4C30B996728BF4B917F6A27";

    /// <summary>Cofactor h. Always 1 for the AACS curve.</summary>
    public const int H = 1;

    /// <summary>Length in bytes of an AACS scalar (private key, signature half).</summary>
    public const int SCALAR_SIZE = 20;

    /// <summary>Length in bytes of an uncompressed AACS point (x || y).</summary>
    public const int POINT_SIZE = 40;

    /// <summary>Length in bytes of an AACS ECDSA signature (r || s).</summary>
    public const int SIGNATURE_SIZE = 40;

    /// <summary>Length in bytes of an AACS host or drive certificate.</summary>
    public const int CERT_SIZE = 92;

    /// <summary>Length in bytes of the certificate body that is signed by the AACS LA.</summary>
    public const int CERT_BODY_SIZE = 52;

    /// <summary>Offset of the public key x-coordinate inside an AACS certificate.</summary>
    public const int CERT_PK_X_OFFSET = 12;

    /// <summary>Offset of the public key y-coordinate inside an AACS certificate.</summary>
    public const int CERT_PK_Y_OFFSET = 32;

    /// <summary>Offset of the AACS LA signature inside an AACS certificate.</summary>
    public const int CERT_SIGNATURE_OFFSET = 52;

    static readonly object               _curveLock = new();
    static          ECDomainParameters?  _domain;
    static          FpCurve?             _curve;
    static          ECPublicKeyParameters? _aacsLaPublicKey;

    /// <summary>The AACS curve as a BouncyCastle <see cref="ECDomainParameters" />.</summary>
    public static ECDomainParameters Domain
    {
        get
        {
            EnsureInitialized();

            return _domain!;
        }
    }

    /// <summary>The AACS curve as a BouncyCastle <see cref="FpCurve" />.</summary>
    public static FpCurve Curve
    {
        get
        {
            EnsureInitialized();

            return _curve!;
        }
    }

    /// <summary>The AACS LA root public key on the AACS curve, used to verify certificates.</summary>
    public static ECPublicKeyParameters AacsLaPublicKey
    {
        get
        {
            EnsureInitialized();

            return _aacsLaPublicKey!;
        }
    }

    /// <summary>Build an <see cref="ECPublicKeyParameters" /> from raw 20-byte X and Y coordinates.</summary>
    /// <param name="x">20-byte big-endian X coordinate.</param>
    /// <param name="y">20-byte big-endian Y coordinate.</param>
    /// <returns>The corresponding public key parameters on the AACS curve.</returns>
    public static ECPublicKeyParameters CreatePublicKey(byte[] x, byte[] y)
    {
        EnsureInitialized();

        ECPoint point = _curve!.CreatePoint(new BigInteger(1, x), new BigInteger(1, y));

        return new ECPublicKeyParameters(point, _domain);
    }

    /// <summary>Build an <see cref="ECPrivateKeyParameters" /> from a raw 20-byte big-endian scalar.</summary>
    /// <param name="d">20-byte big-endian private scalar.</param>
    /// <returns>The corresponding private key parameters on the AACS curve.</returns>
    public static ECPrivateKeyParameters CreatePrivateKey(byte[] d)
    {
        EnsureInitialized();

        return new ECPrivateKeyParameters(new BigInteger(1, d), _domain);
    }

    static void EnsureInitialized()
    {
        if(_domain != null) return;

        lock(_curveLock)
        {
            if(_domain != null) return;

            BigInteger p = new(P_HEX, 16);
            BigInteger a = new(A_HEX, 16);
            BigInteger b = new(B_HEX, 16);
            BigInteger n = new(N_HEX, 16);

            FpCurve curve = new(p, a, b, n, BigInteger.One);

            ECPoint g = curve.CreatePoint(new BigInteger(G_X_HEX, 16), new BigInteger(G_Y_HEX, 16));

            ECDomainParameters domain = new(curve, g, n, BigInteger.One);

            ECPoint laPoint = curve.CreatePoint(new BigInteger(AACS_LA_PUBKEY_X_HEX, 16),
                                                new BigInteger(AACS_LA_PUBKEY_Y_HEX, 16));

            _curve            = curve;
            _domain           = domain;
            _aacsLaPublicKey  = new ECPublicKeyParameters(laPoint, domain);
        }
    }
}