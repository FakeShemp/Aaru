// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SpiralTests.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Graphics unit tests.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System.IO;
using Aaru.CommonTypes;
using Aaru.Core.Graphics;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Graphics;

/// <summary>
///     Generates representative spiral PNGs to <c>/tmp/aaru-spiral-*.png</c> for manual visual
///     validation. Each test also asserts that the file was written and is non-empty. Tests cover
///     single-layer media (CD/DVD/BD/UMD/GD-ROM) and dual-layer OTP and PTP configurations.
/// </summary>
[TestFixture]
public class SpiralTests
{
    const int DIMENSIONS = 1000;

    static string OutPath(string id) => Path.Combine(Path.GetTempPath(), $"aaru-spiral-{id}.png");

    static void AssertWritten(string path)
    {
        File.Exists(path).Should().BeTrue($"Expected PNG at {path}");
        new FileInfo(path).Length.Should().BeGreaterThan(0L, $"PNG {path} is empty");
    }

    static void PaintSingleLayerPattern(Spiral spiral, ulong lastSector)
    {
        // Green: first 25%
        spiral.PaintSectorsGood(0, (uint)(lastSector / 4));

        // Red: a ring near middle (3072 sectors, emulating ring protection)
        spiral.PaintSectorsBad(lastSector / 2, 3072);

        // Yellow: 10% after the red ring
        spiral.PaintSectorsUnknown(lastSector / 2 + 3072, (uint)(lastSector / 10));

        // Undumped (gray is background so nothing to do)
    }

    static void PaintDualLayerPattern(Spiral spiral, ulong layerBreak, ulong lastSector)
    {
        // Layer 0: good on first half, bad ring in middle, unknown near the break
        spiral.PaintSectorsGood(0, (uint)(layerBreak / 3));
        spiral.PaintSectorsBad(layerBreak / 2, 2048);
        spiral.PaintSectorsUnknown(layerBreak - 5000, 5000);

        // Layer 1: good near start (outer radius for OTP), bad ring in middle, unknown near end
        ulong l1Start = layerBreak + 1;
        ulong l1Size  = lastSector - layerBreak;
        spiral.PaintSectorsGood(l1Start, (uint)(l1Size / 3));
        spiral.PaintSectorsBad(l1Start        + l1Size / 2, 2048);
        spiral.PaintSectorsUnknown(lastSector - 5000, 5000);
    }

    [Test]
    public void CdRom()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.CDROM);
        const ulong           lastSector = 333000UL;
        var                   spiral     = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector);
        PaintSingleLayerPattern(spiral, lastSector);
        string path = OutPath("cdrom");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void Cdr()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.CDR);
        const ulong           lastSector = 359849UL;
        var                   spiral     = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector);
        spiral.PaintRecordableInformationGood();
        PaintSingleLayerPattern(spiral, lastSector);
        string path = OutPath("cdr");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void DvdRomSingleLayer()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.DVDROM);
        const ulong           lastSector = 2294921UL;
        var                   spiral     = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector);
        PaintSingleLayerPattern(spiral, lastSector);
        string path = OutPath("dvdrom-sl");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void DvdRomDualLayerOtp()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.DVDROM);
        const ulong           lastSector = 4173821UL;
        const ulong           layerBreak = 2086911UL;

        var spiral = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector, layerBreak);
        PaintDualLayerPattern(spiral, layerBreak, lastSector);
        string path = OutPath("dvdrom-dl-otp");
        spiral.WriteTo(path);
        AssertWritten(path);

        spiral.Bitmap.Width.Should().Be(DIMENSIONS * 2);
        spiral.Bitmap.Height.Should().Be(DIMENSIONS);
    }

    [Test]
    public void DvdRomDualLayerPtp()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.DVDROM);
        const ulong           lastSector = 4173821UL;
        const ulong           layerBreak = 2086911UL;

        var spiral = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector, layerBreak, false);
        PaintDualLayerPattern(spiral, layerBreak, lastSector);
        string path = OutPath("dvdrom-dl-ptp");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void DvdPlusRDualLayer()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.DVDPRDL);
        const ulong           lastSector = 4173821UL;
        const ulong           layerBreak = 2086911UL;

        var spiral = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector, layerBreak);
        spiral.PaintRecordableInformationGood();
        PaintDualLayerPattern(spiral, layerBreak, lastSector);
        string path = OutPath("dvdprdl");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void BdRomSingleLayer()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.BDROM);
        const ulong           lastSector = 12219391UL;
        var                   spiral     = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector);
        PaintSingleLayerPattern(spiral, lastSector);
        string path = OutPath("bdrom-sl");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void BdRomDualLayer()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.BDROM);
        const ulong           lastSector = 24438783UL;
        const ulong           layerBreak = 12219391UL;

        var spiral = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector, layerBreak);
        PaintDualLayerPattern(spiral, layerBreak, lastSector);
        string path = OutPath("bdrom-dl");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void BdRxl()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.BDRXL);
        const ulong           lastSector = 48878591UL;
        var                   spiral     = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector);
        spiral.PaintRecordableInformationGood();
        PaintSingleLayerPattern(spiral, lastSector);
        string path = OutPath("bdrxl");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void Umd()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.UMD);
        const ulong           lastSector = 471871UL;
        var                   spiral     = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector);
        PaintSingleLayerPattern(spiral, lastSector);
        string path = OutPath("umd");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void GdRom()
    {
        Spiral.DiscParameters p          = Spiral.DiscParametersFromMediaType(MediaType.GDROM);
        const ulong           lastSector = 549999UL;
        var                   spiral     = new Spiral(DIMENSIONS, DIMENSIONS, p, lastSector);

        // Low-density area (sectors 0..45000) plus high-density
        spiral.PaintSectorsGood(0,     45000);
        spiral.PaintSectorsGood(45000, 100000);
        spiral.PaintSectorsBad(200000, 2048);
        string path = OutPath("gdrom");
        spiral.WriteTo(path);
        AssertWritten(path);
    }

    [Test]
    public void GdRomDualLayerRequestFallsBackToSingleLayer()
    {
        // GD-ROM physically single-layer; Spiral should ignore layerBreak and render a single disc.
        Spiral.DiscParameters p      = Spiral.DiscParametersFromMediaType(MediaType.GDROM);
        var                   spiral = new Spiral(DIMENSIONS, DIMENSIONS, p, 549999UL, 250000UL);
        spiral.Bitmap.Width.Should().Be(DIMENSIONS);
    }
}