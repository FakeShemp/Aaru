// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsFullMkbReaderTests.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Unit tests for the AACS full Media Key Block loader. Exercises
//     the candidate-path table per media kind, the primary/backup fallback,
//     and the 16 MiB read cap. The same paths are used when converting an
//     image with --aacs-keydb-file.
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
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core.Devices.Dumping;
using Aaru.Decryption.Aacs;
using FluentAssertions;
using NUnit.Framework;
using FileSystem = Aaru.CommonTypes.AaruMetadata.FileSystem;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;

namespace Aaru.Tests.Decryption;

[TestFixture]
public sealed class AacsFullMkbReaderTests
{
    [Test]
    public void GetCandidatePaths_HdDvd_ReturnsRomThenBackup()
    {
        string[] paths = AacsFullMkbReader.GetCandidatePaths(AacsMediaKind.HdDvd);

        paths.Should().Equal("/AACS/MKBROM.AACS", "/AACS_BAK/MKBROM.AACS");
    }

    [Test]
    public void GetCandidatePaths_BluRay_ReturnsRoInfThenDuplicate()
    {
        string[] paths = AacsFullMkbReader.GetCandidatePaths(AacsMediaKind.BluRay);

        paths.Should().Equal("/AACS/MKB_RO.inf", "/AACS/DUPLICATE/MKB_RO.inf");
    }

    [Test]
    public void TryReadFirstAvailable_PicksPrimary_WhenPresent()
    {
        byte[] primary = [0x10, 0x00, 0x00, 0x0C, 0x00, 0x03, 0x10, 0x03, 0x00, 0x00, 0x00, 0x01];
        byte[] backup  = [0x10, 0x00, 0x00, 0x0C, 0x00, 0x03, 0x10, 0x03, 0x00, 0x00, 0x00, 0x02];

        FakeReadOnlyFilesystem fs = new();
        fs.AddFile("/AACS/MKBROM.AACS",     primary);
        fs.AddFile("/AACS_BAK/MKBROM.AACS", backup);

        AacsFullMkbReader.TryReadFirstAvailable(fs,
                                                AacsFullMkbReader.GetCandidatePaths(AacsMediaKind.HdDvd),
                                                out byte[]? data)
                         .Should()
                         .BeTrue();

        data.Should().Equal(primary);
    }

    [Test]
    public void TryReadFirstAvailable_FallsBackToBackup_WhenPrimaryMissing()
    {
        byte[] backup = [0x10, 0x00, 0x00, 0x0C, 0x00, 0x03, 0x10, 0x03, 0x00, 0x00, 0x00, 0x02];

        FakeReadOnlyFilesystem fs = new();
        fs.AddFile("/AACS_BAK/MKBROM.AACS", backup);

        AacsFullMkbReader.TryReadFirstAvailable(fs,
                                                AacsFullMkbReader.GetCandidatePaths(AacsMediaKind.HdDvd),
                                                out byte[]? data)
                         .Should()
                         .BeTrue();

        data.Should().Equal(backup);
    }

    [Test]
    public void TryReadFirstAvailable_FailsWhenNeitherPresent()
    {
        FakeReadOnlyFilesystem fs = new();

        AacsFullMkbReader.TryReadFirstAvailable(fs,
                                                AacsFullMkbReader.GetCandidatePaths(AacsMediaKind.BluRay),
                                                out byte[]? data)
                         .Should()
                         .BeFalse();

        data.Should().BeNull();
    }

    [Test]
    public void TryReadFile_RejectsZeroLengthFile()
    {
        FakeReadOnlyFilesystem fs = new();
        fs.AddFile("/AACS/MKBROM.AACS", []);

        AacsFullMkbReader.TryReadFile(fs, "/AACS/MKBROM.AACS", out byte[]? data).Should().BeFalse();
        data.Should().BeNull();
    }

    [Test]
    public void TryReadFile_RejectsOversizedFile()
    {
        FakeReadOnlyFilesystem fs = new();
        // Pretend the file is 17 MiB (above the 16 MiB cap), but don't actually allocate.
        fs.AddFakeLargeFile("/AACS/MKBROM.AACS", AacsFullMkbReader.MAX_MKB_SIZE + 1);

        AacsFullMkbReader.TryReadFile(fs, "/AACS/MKBROM.AACS", out byte[]? data).Should().BeFalse();
        data.Should().BeNull();
    }

    /// <summary>
    ///     Minimal <see cref="IReadOnlyFilesystem" /> that backs a few in-memory files. Only the
    ///     methods used by <see cref="AacsFullMkbReader" /> (<c>OpenFile</c>, <c>ReadFile</c>,
    ///     <c>CloseFile</c>) do anything; the rest throw, which is acceptable for the
    ///     loader's narrow contract.
    /// </summary>
    sealed class FakeReadOnlyFilesystem : IReadOnlyFilesystem
    {
        readonly Dictionary<string, FakeFileEntry> _files = new(StringComparer.Ordinal);

        public void AddFile(string path, byte[] data) => _files[path] = new FakeFileEntry(data, data.LongLength);

        public void AddFakeLargeFile(string path, long fakeLength) =>
            _files[path] = new FakeFileEntry([], fakeLength);

        public string Name   => "Fake";
        public Guid   Id     => Guid.Empty;
        public string Author => "Test";

        public FileSystem                                         Metadata         => new();
        public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
        public Dictionary<string, string>                         Namespaces       => new();

        public bool Identify(IMediaImage imagePlugin, Partition partition) => true;

        public void GetInformation(IMediaImage    imagePlugin, Partition partition, Encoding encoding,
                                   out string     information,
                                   out FileSystem metadata)
        {
            information = "";
            metadata    = new FileSystem();
        }

        public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                                 Dictionary<string, string> options,
                                 string                     @namespace) =>
            ErrorNumber.NoError;

        public ErrorNumber Unmount() => ErrorNumber.NoError;

        public ErrorNumber ListXAttr(string path, out List<string> xattrs)
        {
            xattrs = [];

            return ErrorNumber.NotSupported;
        }

        public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf) => ErrorNumber.NotSupported;

        public ErrorNumber StatFs(out FileSystemInfo stat)
        {
            stat = new FileSystemInfo();

            return ErrorNumber.NotSupported;
        }

        public ErrorNumber Stat(string path, out FileEntryInfo stat)
        {
            stat = null;

            return ErrorNumber.NotSupported;
        }

        public ErrorNumber ReadLink(string path, out string dest)
        {
            dest = null;

            return ErrorNumber.NotSupported;
        }

        public ErrorNumber OpenFile(string path, out IFileNode node)
        {
            if(_files.TryGetValue(path, out FakeFileEntry entry))
            {
                node = new FakeFileNode(path, entry.Length);

                return ErrorNumber.NoError;
            }

            node = null;

            return ErrorNumber.NoSuchFile;
        }

        public ErrorNumber CloseFile(IFileNode node) => ErrorNumber.NoError;

        public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
        {
            read = 0;

            if(node is null) return ErrorNumber.InvalidArgument;

            if(!_files.TryGetValue(node.Path, out FakeFileEntry entry)) return ErrorNumber.NoSuchFile;

            if(entry.Data.LongLength < length) return ErrorNumber.InvalidArgument;

            Buffer.BlockCopy(entry.Data, 0, buffer, 0, (int)length);
            read = length;

            return ErrorNumber.NoError;
        }

        public ErrorNumber OpenDir(string path, out IDirNode node)
        {
            node = null;

            return ErrorNumber.NotSupported;
        }

        public ErrorNumber CloseDir(IDirNode node) => ErrorNumber.NoError;

        public ErrorNumber ReadDir(IDirNode node, out string filename)
        {
            filename = null;

            return ErrorNumber.NoSuchFile;
        }

        readonly struct FakeFileEntry(byte[] data, long length)
        {
            public byte[] Data   { get; } = data;
            public long   Length { get; } = length;
        }

        sealed class FakeFileNode(string path, long length) : IFileNode
        {
            public string Path   { get; }      = path;
            public long   Length { get; }      = length;
            public long   Offset { get; set; } = 0;
        }
    }
}