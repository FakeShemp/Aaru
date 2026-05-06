// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsKeyResolverConvertTests.cs
//
// --[ Description ] ----------------------------------------------------------
//
//     Unit tests for convert-time AACS full-MKB media kind mapping used when
//     deriving MK/VUK with --aacs-keydb-file.
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
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT ANY KIND OF WARRANTY.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using Aaru.CommonTypes;
using Aaru.Core.Image;
using Aaru.Decryption.Aacs;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Decryption;

[TestFixture]
public sealed class AacsKeyResolverConvertTests
{
    [Test]
    public void TryGetAacsMediaKindForFullMkb_BdRom_IsBluRay()
    {
        bool ok = AacsKeyResolver.TryGetAacsMediaKindForFullMkb(MediaType.BDROM, out AacsMediaKind kind);

        ok.Should().BeTrue();
        kind.Should().Be(AacsMediaKind.BluRay);
    }

    [Test]
    public void TryGetAacsMediaKindForFullMkb_PlayStationBd_IsBluRay()
    {
        bool ok = AacsKeyResolver.TryGetAacsMediaKindForFullMkb(MediaType.PS3BD, out AacsMediaKind kind);

        ok.Should().BeTrue();
        kind.Should().Be(AacsMediaKind.BluRay);
    }

    [Test]
    public void TryGetAacsMediaKindForFullMkb_HdDvdRom_IsHdDvd()
    {
        bool ok = AacsKeyResolver.TryGetAacsMediaKindForFullMkb(MediaType.HDDVDROM, out AacsMediaKind kind);

        ok.Should().BeTrue();
        kind.Should().Be(AacsMediaKind.HdDvd);
    }

    [Test]
    public void TryGetAacsMediaKindForFullMkb_CdRom_IsUnsupported()
    {
        bool ok = AacsKeyResolver.TryGetAacsMediaKindForFullMkb(MediaType.CDROM, out AacsMediaKind kind);

        ok.Should().BeFalse();
        kind.Should().Be(default(AacsMediaKind));
    }
}
