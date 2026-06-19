// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LIF.cs
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
using Aaru.Checksums;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core;
using Aaru.Filesystems;
using FluentAssertions;
using NUnit.Framework;

namespace Aaru.Tests.Filesystems;

[TestFixture]
public class Lif() : FilesystemTest("hplif")
{
    const string TEST_FILE = "HP-UX 9.0 Install S300,400.iso";

    public override string DataFolder =>
        Path.Combine(Consts.TestFilesRoot, "Filesystems", "Logical Interchange Format");
    public override IFilesystem Plugin     => new LIF();
    public override bool        Partitions => false;

    public override FileSystemTest[] Tests =>
    [
        new()
        {
            TestFile    = TEST_FILE,
            MediaType   = MediaType.CD,
            Sectors     = 4036,
            SectorSize  = 2048,
            Clusters    = 32288,
            ClusterSize = 256,
            VolumeName  = "BOOT  "
        }
    ];

    [Test]
    public void RootDirectory()
    {
        IReadOnlyFilesystem filesystem = MountFilesystem();

        filesystem.OpenDir("/", out IDirNode node).Should().Be(ErrorNumber.NoError);

        List<string> entries = [];
        string       filename;

        while(filesystem.ReadDir(node, out filename) == ErrorNumber.NoError && filename is not null)
            entries.Add(filename);

        filesystem.CloseDir(node).Should().Be(ErrorNumber.NoError);

        entries.Should().Equal("SYSINSTALL", "SYSDEBUG", "SYSBCKUP", "SYSTEST", "SYSXDB");
    }

    [Test]
    public void ReadKnownFile()
    {
        IReadOnlyFilesystem filesystem = MountFilesystem();

        filesystem.Stat("/SYSINSTALL", out FileEntryInfo stat).Should().Be(ErrorNumber.NoError);
        stat.Length.Should().Be(6912);
        stat.BlockSize.Should().Be(256);
        stat.Blocks.Should().Be(27);

        filesystem.OpenFile("/SYSINSTALL", out IFileNode node).Should().Be(ErrorNumber.NoError);

        var buffer = new byte[stat.Length];

        filesystem.ReadFile(node, stat.Length, buffer, out long read).Should().Be(ErrorNumber.NoError);
        read.Should().Be(stat.Length);
        Md5Context.Data(buffer, out _).Should().Be("7d77836613a402369163240b99912d17");

        filesystem.CloseFile(node).Should().Be(ErrorNumber.NoError);
    }

    static IReadOnlyFilesystem MountFilesystem()
    {
        IFilter inputFilter = PluginRegister.Singleton.GetFilter(Path.Combine(Consts.TestFilesRoot,
                                                                              "Filesystems",
                                                                              "Logical Interchange Format",
                                                                              TEST_FILE));

        inputFilter.Should().NotBeNull();

        var image = ImageFormat.Detect(inputFilter) as IMediaImage;
        image.Should().NotBeNull();
        image.Open(inputFilter).Should().Be(ErrorNumber.NoError);

        Partition partition = new()
        {
            Name   = "Whole device",
            Length = image.Info.Sectors,
            Size   = image.Info.Sectors * image.Info.SectorSize
        };

        IReadOnlyFilesystem filesystem = new LIF();

        filesystem.Mount(image, partition, null, null, null).Should().Be(ErrorNumber.NoError);

        return filesystem;
    }
}