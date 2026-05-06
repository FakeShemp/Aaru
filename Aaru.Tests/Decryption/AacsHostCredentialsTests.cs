// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsHostCredentialsTests.cs
//
// --[ Description ] ----------------------------------------------------------
//
//     Tests for KEYDB.cfg AACS host credential parsing.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System.IO;
using Aaru.Decryption.Aacs;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Decryption;

[TestFixture]
public sealed class AacsHostCredentialsTests
{
    [Test]
    public void HostCredentials_RejectsLegacyTwoLineFormat()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path,
                              """
                              00112233445566778899AABBCCDDEEFF00112233
                              0200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
                              """);

            AacsHostCredentials.TryLoad(path, out AacsHostCredentials? credentials, out string? errorMessage)
                               .Should()
                               .BeFalse();
            credentials.Should().BeNull();
            errorMessage.Should().Contain("no HC entries");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void HostCredentials_RejectsFileWithoutHcEntries()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path,
                              """
                              ; KEYDB without HC
                              | DK | DEVICE_KEY 0x00112233445566778899AABBCCDDEEFF | DEVICE_NODE 0x1
                              """);

            AacsHostCredentials.TryLoad(path, out AacsHostCredentials? credentials, out string? errorMessage)
                               .Should()
                               .BeFalse();
            credentials.Should().BeNull();
            errorMessage.Should().Contain("no HC entries");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void HostCredentials_ScansMultipleHcAndUsesFirstValidCandidate()
    {
        string path = Path.GetTempFileName();

        try
        {
            string certType12 = "12" + new string('0', 182);

            File.WriteAllText(path,
                              $"""
                               | HC | HOST_PRIV_KEY 0xBADHEX | HOST_CERT 0x{certType12}
                               | HC | HOST_PRIV_KEY 0x00112233445566778899AABBCCDDEEFF00112233 | HOST_CERT 0x{certType12}
                               """);

            AacsHostCredentials.TryLoad(path, out AacsHostCredentials? credentials, out string? errorMessage)
                               .Should()
                               .BeFalse();
            credentials.Should().BeNull();
            errorMessage.Should().Contain("Unsupported host certificate type");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
