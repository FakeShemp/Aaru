// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : partclone2.cs
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
using Aaru.Images;
using NUnit.Framework;

namespace Aaru.Tests.Images;

[TestFixture]
public class Partclone2 : BlockMediaImageTest
{
    public override string DataFolder => Path.Combine(Consts.TestFilesRoot, "Media image formats", "partclone2");

    public override IMediaImage Plugin => new PartClone();

    public override BlockImageTestExpected[] Tests =>
    [
        new()
        {
            TestFile   = "ext2.partclone2.lz",
            MediaType  = MediaType.GENERIC_HDD,
            Sectors    = 127882,
            SectorSize = 4096,
            Md5        = "ff239c91166b6b13fa826dd258b40666"
        },
        new()
        {
            TestFile   = "fat16.partclone2.lz",
            MediaType  = MediaType.GENERIC_HDD,
            Sectors    = 1012032,
            SectorSize = 512,
            Md5        = "f98b1a51ca2e7bf047d84969a2392a3d"
        },
        new()
        {
            TestFile   = "fat32.partclone2.lz",
            MediaType  = MediaType.GENERIC_HDD,
            Sectors    = 1023057,
            SectorSize = 512,
            Md5        = "aea38a2aecc9186f298d7526471dfa03"
        },
        new()
        {
            TestFile   = "ntfs.partclone2.lz",
            MediaType  = MediaType.GENERIC_HDD,
            Sectors    = 1023056,
            SectorSize = 512,
            Md5        = "61cc3faa286364e7ad5bab18120c1151"
        }
    ];
}