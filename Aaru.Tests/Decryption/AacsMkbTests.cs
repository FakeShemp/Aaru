// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsMkbTests.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Synthetic-MKB roundtrip tests for the AACS MKB record walker,
//     device-key file parser, and MKB processor. We hand-construct a minimal
//     MKB containing exactly the records the processor needs (Type/Version,
//     Subset-Difference, Media Key Data, Verify Media Key Data) and then
//     check that <see cref="AacsMkbProcessor.TryDeriveMediaKey" /> recovers
//     the chosen Media Key using the pruned subset-difference walk with full
//     DK metadata (KEYDB-style entries).
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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Aaru.Decryption.Aacs;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Decryption;

[TestFixture]
public sealed class AacsMkbTests
{
    [Test]
    public void CalcVMask_KnownAnswers()
    {
        AacsMkbProcessor.CalcVMask(0x00000004u).Should().Be(0xFFFFFFF8u);
        AacsMkbProcessor.CalcVMask(0x00000040u).Should().Be(0xFFFFFF80u);
        AacsMkbProcessor.CalcVMask(0x00000001u).Should().Be(0xFFFFFFFEu);
        AacsMkbProcessor.CalcVMask(0xDEADBEEFu).Should().Be(0xFFFFFFFEu);
    }

    [Test]
    public void CalcUMask_KnownAnswers()
    {
        AacsMkbProcessor.CalcUMask(0).Should().Be(0xFFFFFFFFu);
        AacsMkbProcessor.CalcUMask(8).Should().Be(0xFFFFFF00u);
        AacsMkbProcessor.CalcUMask(31).Should().Be(0x80000000u);
        AacsMkbProcessor.CalcUMask(32).Should().Be(0u);
        AacsMkbProcessor.CalcUMask(40).Should().Be(0u);
    }

    [Test]
    public void TryFindRecord_FindsAllAndStopsOnTruncation()
    {
        byte[] mkb =
        [
            0x10, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x10, 0x03, 0x00, 0x00, 0x00, 0x01,
            0x04, 0x00, 0x00, 0x09, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE,
            0x05, 0x00, 0x00, 0x14, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                                    0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x02, 0x00, 0x00, 0x00, 0xFF
        ];

        AacsMkb.TryFindRecord(mkb, 0x10, out ReadOnlySpan<byte> tv).Should().BeTrue();
        tv.Length.Should().Be(12);

        AacsMkb.TryFindRecord(mkb, 0x04, out ReadOnlySpan<byte> sd).Should().BeTrue();
        sd.Length.Should().Be(9);

        AacsMkb.TryFindRecord(mkb, 0x05, out ReadOnlySpan<byte> cv).Should().BeTrue();
        cv.Length.Should().Be(20);

        AacsMkb.TryFindRecord(mkb, 0x99, out _).Should().BeFalse();
    }

    [Test]
    public void TryGetTypeAndVersion_ReadsBigEndian()
    {
        byte[] mkb =
        [
            0x10, 0x00, 0x00, 0x0C, 0x00, 0x03, 0x10, 0x03, 0x00, 0x00, 0x12, 0x34
        ];

        AacsMkb.TryGetTypeAndVersion(mkb, out uint mkbType, out uint version).Should().BeTrue();
        mkbType.Should().Be(0x00031003u);
        version.Should().Be(0x00001234u);
    }

    [Test]
    public void DeviceKeys_ParsesKeydbDkEntries()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path,
                              """
                              ; this is a comment
                              # another comment
                              | DK | DEVICE_KEY 0x404142434445464748494A4B4C4D4E4F | DEVICE_NODE 0x22 | KEY_UV 0x8 | KEY_U_MASK 0x1A

                              | DK | DEVICE_KEY 0x202122232425262728292A2B2C2D2E2F | DEVICE_NODE 0x1 | KEY_UV 0x2 | KEY_U_MASK_SHIFT 0x3 ; trailing
                              """);

            AacsDeviceKeys.TryLoad(path, out IReadOnlyList<AacsDeviceKey>? keys, out string? error)
                          .Should()
                          .BeTrue();
            error.Should().BeNull();
            keys.Should().NotBeNull();
            keys!.Count.Should().Be(2);

            keys[0].HasMetadata.Should().BeTrue();
            keys[0].Node!.Value.Should().Be(0x22u);
            keys[0].Uv!.Value.Should().Be(0x8u);
            keys[0].UMaskShift!.Value.Should().Be(0x1A);

            keys[1].HasMetadata.Should().BeTrue();
            keys[1].UMaskShift!.Value.Should().Be(0x3);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DeviceKeys_RejectsIncompleteDkMetadata()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path,
                              "| DK | DEVICE_KEY 0x303132333435363738393A3B3C3D3E3F | DEVICE_NODE 0xABCD\n");

            AacsDeviceKeys.TryLoad(path, out IReadOnlyList<AacsDeviceKey>? keys, out string? error)
                          .Should()
                          .BeFalse();
            keys.Should().BeNull();
            error.Should().Contain("DEVICE_NODE");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DeviceKeys_RejectsLegacySimpleFormat()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "000102030405060708090A0B0C0D0E0F\n");

            AacsDeviceKeys.TryLoad(path, out IReadOnlyList<AacsDeviceKey>? keys, out string? error)
                          .Should()
                          .BeFalse();
            keys.Should().BeNull();
            error.Should().Contain("no DK entries");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DeviceKeys_RejectsMalformedHex()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "DEADBEEFNOTHEX12345678901234567890\n");

            AacsDeviceKeys.TryLoad(path, out IReadOnlyList<AacsDeviceKey>? keys, out string? error)
                          .Should()
                          .BeFalse();
            keys.Should().BeNull();
            error.Should().NotBeNullOrEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DeriveMediaKey_PrunedPath_RecoversMk()
    {
        SyntheticMkb fixture = BuildFixture();

        AacsDeviceKey rich = new(fixture.DeviceKey,
                                 node: fixture.Node,
                                 uv: fixture.DevKeyUv,
                                 uMaskShift: fixture.UMaskShift);

        AacsMkbProcessor.TryDeriveMediaKey(fixture.Mkb, [rich], out byte[]? mk, out string? err).Should().BeTrue();
        err.Should().BeNull();
        mk.Should().NotBeNull();
        mk!.Should().Equal(fixture.MediaKey);
    }

    [Test]
    public void DeriveMediaKey_NoMatchingKey_Fails()
    {
        SyntheticMkb fixture = BuildFixture();

        byte[] random = new byte[16];
        RandomNumberGenerator.Fill(random);

        AacsDeviceKey junk = new(random, null, null, null);

        AacsMkbProcessor.TryDeriveMediaKey(fixture.Mkb, [junk], out byte[]? mk, out string? err).Should().BeFalse();
        mk.Should().BeNull();
        err.Should().NotBeNullOrEmpty();
    }

    /// <summary>Construct a synthetic MKB with a single subset-difference row that we control entirely.</summary>
    static SyntheticMkb BuildFixture()
    {
        // Row metadata. uv = 0x4 → vMask = 0xFFFFFFF8 (depth 3). u_mask_shift = 5 → uMask = 0xFFFFFFE0
        // (depth 5). The subset-difference row covers nodes 0x00..0x1F minus 0x04..0x07.
        //
        // We pick node = 0x10 (in the larger subset, not in the v subset) and a synthetic device key
        // sitting at devKeyUv = 0x8 → devKeyVMask = 0xFFFFFFF0 (depth 4), matching libaacs-style DK.
        const uint  rowUv       = 0x00000004u;
        const byte  uMaskShift  = 0x05;
        const uint  node        = 0x00000010u;
        const uint  devKeyUv    = 0x00000008u;

        byte[] uvBytes = [0x00, 0x00, 0x00, (byte)(rowUv & 0xFF)];

        byte[] deviceKey = new byte[16];

        // Deterministic but arbitrary device key to keep the fixture reproducible.
        for(int i = 0; i < 16; i++) deviceKey[i] = (byte)(0xA0 + i);

        // Walk the AES-G3 tree exactly the same way AacsMkbProcessor.CalcPk does, so we know the
        // processing key the processor will recover.
        byte[]   processingKey = new byte[16];
        uint     vMask         = AacsMkbProcessor.CalcVMask(rowUv);
        uint     devKeyVMask   = AacsMkbProcessor.CalcVMask(devKeyUv);
        AacsMkbProcessor.CalcPk(deviceKey, rowUv, vMask, devKeyVMask, processingKey);

        // Pick an arbitrary Media Key.
        byte[] mediaKey =
        [
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
            0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00
        ];

        // The Media Key Data ciphertext is built so that AES-128-D(pk, c) XOR (uvBytes appended at [12..15])
        // == mediaKey. Since the validation does mk[12..15] ^= uv[0..3] AFTER decryption, we need to
        // pre-XOR the plaintext so the XOR cancels out. So plaintext = mediaKey with mediaKey[12..15]
        // XORed by uvBytes.
        byte[] plaintext = (byte[])mediaKey.Clone();

        for(int i = 0; i < 4; i++) plaintext[12 + i] ^= uvBytes[i];

        byte[] cValue = AesEcbEncrypt(processingKey, plaintext);

        // Verify Media Key Data: dv = AES-128-E(mediaKey, "01 23 45 67 89 AB CD EF" || 8 bytes).
        byte[] dvPlaintext =
        [
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
            // Trailing bytes are arbitrary; the validator only checks the first 8 bytes.
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F
        ];

        byte[] dv = AesEcbEncrypt(mediaKey, dvPlaintext);

        // Build the MKB as a sequence of records.
        using MemoryStream ms = new();

        WriteRecord(ms, 0x10, BuildTypeAndVersion(mkbType: 0x00031003u, version: 1u));
        WriteRecord(ms, 0x04, BuildSubsetDifferencePayload(uMaskShift, uvBytes));
        WriteRecord(ms, 0x05, cValue);
        WriteRecord(ms, 0x81, dv);

        return new SyntheticMkb(ms.ToArray(), deviceKey, node, devKeyUv, uMaskShift, processingKey, mediaKey);
    }

    static byte[] BuildTypeAndVersion(uint mkbType, uint version)
    {
        byte[] payload = new byte[8];
        payload[0] = (byte)(mkbType >> 24);
        payload[1] = (byte)(mkbType >> 16);
        payload[2] = (byte)(mkbType >> 8);
        payload[3] = (byte)mkbType;
        payload[4] = (byte)(version >> 24);
        payload[5] = (byte)(version >> 16);
        payload[6] = (byte)(version >> 8);
        payload[7] = (byte)version;

        return payload;
    }

    static byte[] BuildSubsetDifferencePayload(byte uMaskShift, ReadOnlySpan<byte> uvBytes)
    {
        byte[] payload = new byte[5];
        payload[0] = uMaskShift;
        uvBytes.CopyTo(payload.AsSpan(1));

        return payload;
    }

    static void WriteRecord(MemoryStream ms, byte type, ReadOnlySpan<byte> payload)
    {
        int total = payload.Length + 4;
        ms.WriteByte(type);
        ms.WriteByte((byte)((total >> 16) & 0xFF));
        ms.WriteByte((byte)((total >> 8)  & 0xFF));
        ms.WriteByte((byte)(total & 0xFF));
        ms.Write(payload);
    }

    static byte[] AesEcbEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
    {
        byte[] result = new byte[plaintext.Length];
        AacsCrypto.Aes128EcbEncrypt(key, plaintext, result);

        return result;
    }

    readonly struct SyntheticMkb
    {
        public SyntheticMkb(byte[] mkb,
                            byte[] deviceKey,
                            uint   node,
                            uint   devKeyUv,
                            byte   uMaskShift,
                            byte[] processingKey,
                            byte[] mediaKey)
        {
            Mkb           = mkb;
            DeviceKey     = deviceKey;
            Node          = node;
            DevKeyUv      = devKeyUv;
            UMaskShift    = uMaskShift;
            ProcessingKey = processingKey;
            MediaKey      = mediaKey;
        }

        public byte[] Mkb           { get; }
        public byte[] DeviceKey     { get; }
        public uint   Node          { get; }
        public uint   DevKeyUv      { get; }
        public byte   UMaskShift    { get; }
        public byte[] ProcessingKey { get; }
        public byte[] MediaKey      { get; }
    }
}