// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsCryptoTests.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Sanity tests for the AACS host-side crypto primitives used to drive the
//     drive authentication, bus key derivation, and CMAC verification.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System;
using Aaru.Decryption.Aacs;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Decryption;

[TestFixture]
public sealed class AacsCryptoTests
{
    [Test]
    public void AesCmac_NistKnownAnswer_Empty()
    {
        // NIST SP 800-38B AES-CMAC test vector with key 2B7E1516..., empty input.
        byte[] key      = HexToBytes("2B7E151628AED2A6ABF7158809CF4F3C");
        byte[] expected = HexToBytes("BB1D6929E95937287FA37D129B756746");

        Span<byte> mac = stackalloc byte[16];
        AacsCrypto.AesCmac(key, ReadOnlySpan<byte>.Empty, mac);

        mac.ToArray().Should().Equal(expected);
    }

    [Test]
    public void AesCmac_NistKnownAnswer_OneBlock()
    {
        // NIST SP 800-38B AES-CMAC test vector, 16-byte input.
        byte[] key      = HexToBytes("2B7E151628AED2A6ABF7158809CF4F3C");
        byte[] message  = HexToBytes("6BC1BEE22E409F96E93D7E117393172A");
        byte[] expected = HexToBytes("070A16B46B4D4144F79BDD9DD04A287C");

        Span<byte> mac = stackalloc byte[16];
        AacsCrypto.AesCmac(key, message, mac);

        mac.ToArray().Should().Equal(expected);
    }

    [Test]
    public void Curve_GeneratorOrder_ReturnsIdentity()
    {
        // n * G should equal the point at infinity on the AACS curve.
        Org.BouncyCastle.Math.EC.ECPoint nG =
            AacsCurve.Domain.G.Multiply(AacsCurve.Domain.N).Normalize();

        nG.IsInfinity.Should().BeTrue();
    }

    [Test]
    public void HostKeyPair_RoundtripsSignatureWithSelf()
    {
        AacsCrypto.CreateHostKeyPair(out byte[] hostPrivate, out byte[] hostPoint);

        hostPrivate.Should().HaveCount(20);
        hostPoint.Should().HaveCount(40);

        byte[] message   = "AACS Phase 1 sanity check"u8.ToArray();
        byte[] signature = AacsCrypto.SignAacs(hostPrivate, message);

        signature.Should().HaveCount(40);

        byte[] x = new byte[20];
        byte[] y = new byte[20];
        Buffer.BlockCopy(hostPoint, 0,  x, 0, 20);
        Buffer.BlockCopy(hostPoint, 20, y, 0, 20);

        AacsCrypto.VerifyAacsSignature(x, y, message, signature).Should().BeTrue();

        byte[] tamperedMessage = (byte[])message.Clone();
        tamperedMessage[0] ^= 0x01;
        AacsCrypto.VerifyAacsSignature(x, y, tamperedMessage, signature).Should().BeFalse();
    }

    [Test]
    public void DeriveBusKey_IsCommutative()
    {
        // ECDH: bk(a, B) == bk(b, A) where A = a*G, B = b*G.
        AacsCrypto.CreateHostKeyPair(out byte[] aliceScalar, out byte[] alicePoint);
        AacsCrypto.CreateHostKeyPair(out byte[] bobScalar,   out byte[] bobPoint);

        byte[] busAtAlice = new byte[16];
        byte[] busAtBob   = new byte[16];

        AacsCrypto.DeriveBusKey(aliceScalar, bobPoint,   busAtAlice);
        AacsCrypto.DeriveBusKey(bobScalar,   alicePoint, busAtBob);

        busAtAlice.Should().Equal(busAtBob);
    }

    [Test]
    public void DeriveBusKey_RejectsBadLengths()
    {
        byte[] busKey = new byte[16];

        Action badPriv = () => AacsCrypto.DeriveBusKey(new byte[10], new byte[40], busKey);
        badPriv.Should().Throw<ArgumentException>();

        Action badPoint = () => AacsCrypto.DeriveBusKey(new byte[20], new byte[10], busKey);
        badPoint.Should().Throw<ArgumentException>();
    }

    [Test]
    public void VerifyAacsMac_RejectsAlteredMac()
    {
        byte[] busKey  = HexToBytes("000102030405060708090A0B0C0D0E0F");
        byte[] payload = HexToBytes("00112233445566778899AABBCCDDEEFF");

        Span<byte> mac = stackalloc byte[16];
        AacsCrypto.AesCmac(busKey, payload, mac);
        byte[] macArr = mac.ToArray();

        AacsCrypto.VerifyAacsMac(busKey, payload, macArr).Should().BeTrue();

        macArr[0] ^= 0x01;
        AacsCrypto.VerifyAacsMac(busKey, payload, macArr).Should().BeFalse();
    }

    static byte[] HexToBytes(string hex)
    {
        if(hex.Length % 2 != 0) throw new ArgumentException("Odd-length hex.", nameof(hex));

        byte[] data = new byte[hex.Length / 2];

        for(int i = 0; i < data.Length; i++)
            data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

        return data;
    }
}