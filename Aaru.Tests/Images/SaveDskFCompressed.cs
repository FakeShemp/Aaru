// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SaveDskFCompressed.cs
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

using System.IO;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using NUnit.Framework;

namespace Aaru.Tests.Images;

[TestFixture]
public class SaveDskFCompressed : BlockMediaImageTest
{
    public override string DataFolder =>
        Path.Combine(Consts.TestFilesRoot, "Media image formats", "SaveDskF", "compressed");

    public override IMediaImage Plugin => new Aaru.Images.SaveDskF();

    public override BlockImageTestExpected[] Tests =>
    [
        new()
        {
            TestFile   = "5dd8_a.dsk",
            MediaType  = MediaType.DOS_525_DS_DD_8,
            Sectors    = 640,
            SectorSize = 512,
            Md5        = "4989762c82f173f9b52e0bdb8cf5becb"
        },
        new()
        {
            TestFile   = "5dd8_ak.dsk",
            MediaType  = MediaType.DOS_525_DS_DD_8,
            Sectors    = 640,
            SectorSize = 512,
            Md5        = "4989762c82f173f9b52e0bdb8cf5becb"
        },
        new()
        {
            TestFile   = "5dd8defs.dsk",
            MediaType  = MediaType.DOS_525_DS_DD_8,
            Sectors    = 640,
            SectorSize = 512,
            Md5        = "5a1e0a75d31d88c1ce7429fd333c268f"
        },
        new()
        {
            TestFile   = "5dd_a.dsk",
            MediaType  = MediaType.DOS_525_DS_DD_9,
            Sectors    = 720,
            SectorSize = 512,
            Md5        = "8a4d35dd0d97e6bca8b000170a43a56f"
        },
        new()
        {
            TestFile   = "5dd_ak.dsk",
            MediaType  = MediaType.DOS_525_DS_DD_9,
            Sectors    = 720,
            SectorSize = 512,
            Md5        = "8a4d35dd0d97e6bca8b000170a43a56f"
        },
        new()
        {
            TestFile   = "5dddefs.dsk",
            MediaType  = MediaType.DOS_525_DS_DD_9,
            Sectors    = 720,
            SectorSize = 512,
            Md5        = "c1a67b27bc76b64d0845965501b24120"
        },
        new()
        {
            TestFile   = "5hd_a.dsk",
            MediaType  = MediaType.DOS_525_HD,
            Sectors    = 2400,
            SectorSize = 512,
            Md5        = "2ce745ac23712d3eb03d7a11ba933b12"
        },
        new()
        {
            TestFile   = "5hd_ak.dsk",
            MediaType  = MediaType.DOS_525_HD,
            Sectors    = 2400,
            SectorSize = 512,
            Md5        = "2ce745ac23712d3eb03d7a11ba933b12"
        },
        new()
        {
            TestFile   = "5hddefs.dsk",
            MediaType  = MediaType.DOS_525_HD,
            Sectors    = 2400,
            SectorSize = 512,
            Md5        = "1c28b4c3cdc1dbf19c24a5eca3891a87"
        },
        new()
        {
            TestFile   = "5sd8_a.dsk",
            MediaType  = MediaType.DOS_525_SS_DD_8,
            Sectors    = 320,
            SectorSize = 512,
            Md5        = "6f5d09c13a7b481bad9ea78042e61e00"
        },
        new()
        {
            TestFile   = "5sd8_ak.dsk",
            MediaType  = MediaType.DOS_525_SS_DD_8,
            Sectors    = 320,
            SectorSize = 512,
            Md5        = "6f5d09c13a7b481bad9ea78042e61e00"
        },
        new()
        {
            TestFile   = "5sd8defs.dsk",
            MediaType  = MediaType.DOS_525_SS_DD_8,
            Sectors    = 320,
            SectorSize = 512,
            Md5        = "65ce0cd08d90c882df12637c9c72c1ba"
        },
        new()
        {
            TestFile   = "5sd_a.dsk",
            MediaType  = MediaType.DOS_525_SS_DD_9,
            Sectors    = 360,
            SectorSize = 512,
            Md5        = "fd81fceb26bda5b02053c5c729a6f67f"
        },
        new()
        {
            TestFile   = "5sd_ak.dsk",
            MediaType  = MediaType.DOS_525_SS_DD_9,
            Sectors    = 360,
            SectorSize = 512,
            Md5        = "fd81fceb26bda5b02053c5c729a6f67f"
        },
        new()
        {
            TestFile   = "5sddefs.dsk",
            MediaType  = MediaType.DOS_525_SS_DD_9,
            Sectors    = 360,
            SectorSize = 512,
            Md5        = "412fdc582506c0d7e76735d403b30759"
        },
        new()
        {
            TestFile   = "mf2dd_a.dsk",
            MediaType  = MediaType.DOS_35_DS_DD_9,
            Sectors    = 1440,
            SectorSize = 512,
            Md5        = "e574be0d057f2ef775dfb685561d27cf"
        },
        new()
        {
            TestFile   = "mf2dd_ak.dsk",
            MediaType  = MediaType.DOS_35_DS_DD_9,
            Sectors    = 1440,
            SectorSize = 512,
            Md5        = "e574be0d057f2ef775dfb685561d27cf"
        },
        new()
        {
            TestFile   = "mf2dd_defs.dsk",
            MediaType  = MediaType.DOS_35_DS_DD_9,
            Sectors    = 1440,
            SectorSize = 512,
            Md5        = "2aefc1e97f29bf9982e0fd7091dfb9f5"
        },
        new()
        {
            TestFile   = "mf2ed_a.dsk",
            MediaType  = MediaType.ECMA_147,
            Sectors    = 5760,
            SectorSize = 512,
            Md5        = "42e73287b23ac985c9825466cae26859"
        },
        new()
        {
            TestFile   = "mf2ed_ak.dsk",
            MediaType  = MediaType.ECMA_147,
            Sectors    = 5760,
            SectorSize = 512,
            Md5        = "42e73287b23ac985c9825466cae26859"
        },
        new()
        {
            TestFile   = "mf2ed_defs.dsk",
            MediaType  = MediaType.ECMA_147,
            Sectors    = 5760,
            SectorSize = 512,
            Md5        = "e4746aa9629a2325c520db1c8a641ac6"
        },
        new()
        {
            TestFile   = "mf2hd_a.dsk",
            MediaType  = MediaType.DOS_35_HD,
            Sectors    = 2880,
            SectorSize = 512,
            Md5        = "009cc68e28b2b13814d3afbec9d9e59f"
        },
        new()
        {
            TestFile   = "mf2hd_ak.dsk",
            MediaType  = MediaType.DOS_35_HD,
            Sectors    = 2880,
            SectorSize = 512,
            Md5        = "009cc68e28b2b13814d3afbec9d9e59f"
        },
        new()
        {
            TestFile   = "mf2hd_defs.dsk",
            MediaType  = MediaType.DOS_35_HD,
            Sectors    = 2880,
            SectorSize = 512,
            Md5        = "003e9130d83a23018f488f9fa89cae5e"
        },
        new()
        {
            TestFile   = "mf2hd_xdf_a.dsk",
            MediaType  = MediaType.XDF_35,
            Sectors    = 3680,
            SectorSize = 512,
            Md5        = "34b4bdab5fcc17076cceb7c1a39ea430"
        },
        new()
        {
            TestFile   = "mf2hd_xdf_ak.dsk",
            MediaType  = MediaType.XDF_35,
            Sectors    = 3680,
            SectorSize = 512,
            Md5        = "34b4bdab5fcc17076cceb7c1a39ea430"
        },
        new()
        {
            TestFile   = "mf2hd_xdf_defs.dsk",
            MediaType  = MediaType.XDF_35,
            Sectors    = 3680,
            SectorSize = 512,
            Md5        = "2770e5b1b7935ca6e9695a32008b936a"
        }
    ];
}