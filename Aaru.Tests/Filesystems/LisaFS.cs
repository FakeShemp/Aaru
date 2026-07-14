// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LisaFS.cs
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core;
using Aaru.Filesystems;
using FluentAssertions;
using NUnit.Framework;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Tests.Filesystems;

[TestFixture]
public class LisaFs() : ReadOnlyFilesystemTest("lisafs")
{
    static readonly string OfficeSystem12Fixture =
        Path.Combine(Consts.TestFilesRoot, "Filesystems", "Apple Lisa filesystem");

    public override string DataFolder => Path.Combine(Consts.TestFilesRoot, "Filesystems", "Apple Lisa filesystem");
    public override IFilesystem Plugin => new LisaFS();
    public override bool Partitions => false;

    public override FileSystemTest[] Tests =>
    [
        new()
        {
            TestFile     = "166files.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "166Files",
            VolumeSerial = "A23703A202010663"
        },
        new()
        {
            TestFile     = "222files.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "222Files",
            VolumeSerial = "A23703A201010663"
        },
        new()
        {
            TestFile     = "blank2.0.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 792,
            ClusterSize  = 512,
            VolumeName   = "AOS  4:59 pm 10/02/87",
            VolumeSerial = "A32D261301010663"
        },
        new()
        {
            TestFile     = "blank-disk.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D01010663"
        },
        new()
        {
            TestFile     = "file-with-a-password.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CC3A702010663"
        },
        new()
        {
            TestFile     = "tfwdndrc-has-been-erased.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D14010663"
        },
        new()
        {
            TestFile     = "tfwdndrc-has-been-restored.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D14010663"
        },
        new()
        {
            TestFile     = "three-empty-folders.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D01010663"
        },
        new()
        {
            TestFile     = "three-folders-with-differently-named-docs.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D01010663"
        },
        new()
        {
            TestFile     = "three-folders-with-differently-named-docs-root-alphabetical.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D01010663"
        },
        new()
        {
            TestFile     = "three-folders-with-differently-named-docs-root-chronological.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D01010663"
        },
        new()
        {
            TestFile     = "three-folders-with-identically-named-docs.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A22CB48D01010663"
        },
        new()
        {
            TestFile     = "lisafs1.dc42.lz",
            MediaType    = MediaType.AppleFileWare,
            Sectors      = 1702,
            SectorSize   = 512,
            Clusters     = 1684,
            ClusterSize  = 512,
            VolumeName   = "AOS 4:15 pm 5/06/1983",
            VolumeSerial = "9924151E190001E1"
        },
        new()
        {
            TestFile     = "lisafs2.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 792,
            ClusterSize  = 512,
            VolumeName   = "Office System 1 2.0",
            VolumeSerial = "9497F10016010D10"
        },
        new()
        {
            TestFile     = "lisafs3.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "Office System 1 3.0",
            VolumeSerial = "9CF9CF89070100A8"
        },
        new()
        {
            TestFile     = "lisafs3_with_desktop.dc42.lz",
            MediaType    = MediaType.AppleSonySS,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 800,
            ClusterSize  = 512,
            VolumeName   = "AOS 3.0",
            VolumeSerial = "A4FE1A191F011652"
        }
    ];

    [Test]
    public void OfficeSystem12TwiggyContents()
    {
        string disk1 = Path.Combine(OfficeSystem12Fixture, "LOS1.2i.dc42");
        string disk2 = Path.Combine(OfficeSystem12Fixture, "LOS1.2ii.dc42");

        File.Exists(disk1).Should().BeTrue(disk1);
        File.Exists(disk2).Should().BeTrue(disk2);

        List<string> disk1Entries = ReadRootEntries(disk1);
        List<string> disk2Entries = ReadRootEntries(disk2);

        disk1Entries.Should().HaveCount(31, disk1);
        disk1Entries.Should().Contain("system.shell",        disk1);
        disk1Entries.Should().Contain("SYSTEM.UNPACK",       disk1);
        disk1Entries.Should().Contain("SYSTEM.OS",           disk1);
        disk1Entries.Should().Contain("SYSTEM.BT_PROF",      disk1);
        disk1Entries.Should().Contain("SYSTEM.BT_TWIG",      disk1);

        disk2Entries.Should().HaveCount(27, disk2);
        disk2Entries.Should().Contain("SELECTOR.SHELL",      disk2);
        disk2Entries.Should().Contain("SHELL.OFFICE SYSTEM", disk2);
        disk2Entries.Should().Contain("SYSTEM.PRINT",        disk2);
    }

    static List<string> ReadRootEntries(string imagePath)
    {
        IFilter inputFilter = PluginRegister.Singleton.GetFilter(imagePath);

        inputFilter.Should().NotBeNull(imagePath);

        IMediaImage image = ImageFormat.Detect(inputFilter) as IMediaImage;

        image.Should().NotBeNull(imagePath);

        image.Open(inputFilter).Should().Be(ErrorNumber.NoError, imagePath);

        Partition partition = new()
        {
            Name   = "Whole device",
            Length = image.Info.Sectors,
            Size   = image.Info.Sectors * image.Info.SectorSize
        };

        LisaFS fs = new();

        fs.Mount(image, partition, null, null, null).Should().Be(ErrorNumber.NoError, imagePath);

        fs.OpenDir("/", out IDirNode node).Should().Be(ErrorNumber.NoError, imagePath);

        List<string> entries = [];

        while(fs.ReadDir(node, out string entry) == ErrorNumber.NoError && entry is not null)
            entries.Add(entry);

        fs.CloseDir(node).Should().Be(ErrorNumber.NoError, imagePath);

        return entries.OrderBy(static entry => entry).ToList();
    }
}
