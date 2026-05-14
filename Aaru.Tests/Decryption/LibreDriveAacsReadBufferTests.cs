// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LibreDriveAacsReadBufferTests.cs
//
// --[ Description ] ----------------------------------------------------------
//
//     Tests for LibreDrive READ BUFFER Volume Identifier parsing and probe heuristics.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
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
public sealed class LibreDriveAacsReadBufferTests
{
    [Test]
    public void TryParseLibreDriveReadBufferVid_RejectsShortBuffer()
    {
        byte[] buf = new byte[19];

        bool ok = LibreDriveAacsReadBuffer.TryParseLibreDriveReadBufferVid(buf, out byte[]? vid);

        ok.Should().BeFalse();
        vid.Should().BeNull();
    }

    [Test]
    public void TryParseLibreDriveReadBufferVid_ExtractsSixteenBytesAtOffsetFour_FromThirtySixByteCapture()
    {
        // 36-byte READ BUFFER payload: 4-byte prefix (e.g. 00 22 00 50) then 16-byte VID, remainder ignored.
        byte[] buf =
        [
            0x00, 0x22, 0x00, 0x50, 0xCD, 0xDB, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A,
            0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22, 0x33, 0x44,
            0x55, 0x66, 0x77, 0x88
        ];

        bool ok = LibreDriveAacsReadBuffer.TryParseLibreDriveReadBufferVid(buf, out byte[]? vid);

        ok.Should().BeTrue();
        vid.Should().NotBeNull();
        vid!.Length.Should().Be(16);
        vid[0].Should().Be(0xCD);
        vid[1].Should().Be(0xDB);
        vid[15].Should().Be(0x10);
    }

    [Test]
    public void ResponseLooksLikeLibreDrivePatchedProbe_TrueWhenMmkvAndLbDrPresent()
    {
        byte[] buf = new byte[64];
        "prefixMMkvtrailingLbDrmore"u8.CopyTo(buf);

        bool ok = LibreDriveAacsReadBuffer.ResponseLooksLikeLibreDrivePatchedProbe(buf);

        ok.Should().BeTrue();
    }

    [Test]
    public void ResponseLooksLikeLibreDrivePatchedProbe_FalseWhenLbDrMissing()
    {
        byte[] buf = new byte[64];
        Array.Fill(buf, (byte)0x20);
        "MMkv"u8.CopyTo(buf.AsSpan(10));

        bool ok = LibreDriveAacsReadBuffer.ResponseLooksLikeLibreDrivePatchedProbe(buf);

        ok.Should().BeFalse();
    }

    [Test]
    public void ResponseLooksLikeLibreDrivePatchedProbe_FalseWhenMmkvMissing()
    {
        byte[] buf = new byte[64];
        "LbDr"u8.CopyTo(buf.AsSpan(20));

        bool ok = LibreDriveAacsReadBuffer.ResponseLooksLikeLibreDrivePatchedProbe(buf);

        ok.Should().BeFalse();
    }
}