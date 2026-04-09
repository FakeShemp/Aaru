// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ReedSolomonTests.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru unit testing.
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using Aaru.Checksums;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Checksums;

[TestFixture]
public class ReedSolomonTests
{
    [Test]
    public void Mode1SectorEccPRowCanBeEncodedAndCorrected()
    {
        ReedSolomon reedSolomon = CreateCdReedSolomon();
        int[]       row         = ExtractCdRow(CdChecksumsFix.Mode1Sector, 0, 86, 24, 2, 86, false);

        VerifyCdRowRoundTrip(reedSolomon, row, 252, 0x55);
    }

    [Test]
    public void Mode1SectorEccQRowCanBeEncodedAndCorrected()
    {
        ReedSolomon reedSolomon = CreateCdReedSolomon();
        int[]       row         = ExtractCdRow(CdChecksumsFix.Mode1Sector, 0, 52, 43, 86, 88, false);

        VerifyCdRowRoundTrip(reedSolomon, row, 251, 0xA5);
    }

    [Test]
    public void Mode2Form1SectorEccPRowCanBeEncodedAndCorrected()
    {
        ReedSolomon reedSolomon = CreateCdReedSolomon();
        int[]       row         = ExtractCdRow(CdChecksumsFix.Mode2Form1Sector, 0, 86, 24, 2, 86, true);

        VerifyCdRowRoundTrip(reedSolomon, row, 252, 0x33);
    }

    [Test]
    public void Mode2Form1SectorEccQRowCanBeEncodedAndCorrected()
    {
        ReedSolomon reedSolomon = CreateCdReedSolomon();
        int[]       row         = ExtractCdRow(CdChecksumsFix.Mode2Form1Sector, 0, 52, 43, 86, 88, true);

        VerifyCdRowRoundTrip(reedSolomon, row, 251, 0x77);
    }

    static ReedSolomon CreateCdReedSolomon()
    {
        ReedSolomon reedSolomon = new();
        reedSolomon.InitRs(255, 253, 8);

        return reedSolomon;
    }

    static void VerifyCdRowRoundTrip(ReedSolomon reedSolomon, int[] row, int errorIndex, int errorMask)
    {
        int[] data   = CreateShortenedData(row);
        int   result = reedSolomon.encode_rs(data, out int[] parity);

        result.Should().Be(0);

        int[] codeword = BuildCodeword(data, parity);
        var   expected = (int[])codeword.Clone();

        int verifiedSymbols = reedSolomon.eras_dec_rs(ref codeword, out int[] verifiedErasures, 0);

        verifiedSymbols.Should().Be(0);
        codeword.Should().Equal(expected);
        verifiedErasures.Should().NotBeNull();

        var corruptedCodeword = (int[])expected.Clone();
        corruptedCodeword[errorIndex] ^= errorMask;

        int correctedSymbols = reedSolomon.eras_dec_rs(ref corruptedCodeword, out int[] erasPos, 0);

        correctedSymbols.Should().Be(1);
        corruptedCodeword.Should().Equal(expected);
        erasPos.Should().NotBeNull();
    }

    static int[] ExtractCdRow(byte[] sector, int major, int majorCount, int minorCount, int majorMult, int minorInc,
                              bool   zeroAddress)
    {
        var row   = new int[minorCount];
        int size  = majorCount * minorCount;
        int index = (major >> 1) * majorMult + (major & 1);

        for(var minor = 0; minor < minorCount; minor++)
        {
            row[minor] = index < 4 && !zeroAddress
                             ? sector[0x0C + index]
                             : index < 4
                                 ? 0
                                 : sector[0x10 + index - 4];

            index += minorInc;

            if(index >= size) index -= size;
        }

        return row;
    }

    static int[] CreateShortenedData(int[] row)
    {
        var data = new int[253];
        Array.Copy(row, 0, data, data.Length - row.Length, row.Length);

        return data;
    }

    static int[] BuildCodeword(int[] data, int[] parity)
    {
        var codeword = new int[255];
        Array.Copy(data,   0, codeword, 0,           data.Length);
        Array.Copy(parity, 0, codeword, data.Length, parity.Length);

        return codeword;
    }
}