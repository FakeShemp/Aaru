// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : TarV7.cs
// Author(s)      : OpenAI Codex
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
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//     General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 OpenAI
// ****************************************************************************/

using System.IO;
using Aaru.Archives;
using Aaru.CommonTypes.Interfaces;
using NUnit.Framework;

namespace Aaru.Tests.Archives;

[TestFixture]
public class TarV7 : ArchiveTest
{
    public override string   DataFolder => Path.Combine(Consts.TestFilesRoot, "Archive formats", "TAR", "V7");
    public override IArchive Plugin     => new Tar();

    public override ArchiveTestExpected[] Tests =>
    [
        new()
        {
            TestFile   = "Microsoft Word for Unix 5.0 (Disk 1).dsk",
            EntryCount = 171
        }
    ];
}