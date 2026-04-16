// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Tar.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Tar plugin.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Tar : IArchive
{
    const string            MODULE_NAME = "tar Archive Plugin";
    Encoding                _encoding;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "tar";
    /// <inheritdoc />
    public Guid Id => new("B3E78AE1-7B49-4E2A-9C6F-5A8D1E4F7C32");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; private set; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => !Opened
                                                          ? ArchiveSupportedFeature.SupportsFilenames      |
                                                            ArchiveSupportedFeature.SupportsSubdirectories |
                                                            ArchiveSupportedFeature.HasExplicitDirectories |
                                                            ArchiveSupportedFeature.HasEntryTimestamp
                                                          : _features;

    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _entries.Count : -1;

#endregion
}