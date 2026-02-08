// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Cram.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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

// ReSharper disable UnusedMember.Local

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the CRAM filesystem</summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Cram : IReadOnlyFilesystem
{
    const string MODULE_NAME = "CramFS plugin";

    /// <summary>Cached root directory entries</summary>
    Dictionary<string, DirectoryEntryInfo> _rootDirectoryCache;

    /// <summary>Encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <summary>The superblock</summary>
    SuperBlock _superBlock;

    /// <summary>Whether the filesystem is little-endian</summary>
    bool _littleEndian;

    /// <summary>Base offset for the filesystem (0 or 512 for shifted superblock)</summary>
    uint _baseOffset;

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.Cram_Name;

    /// <inheritdoc />
    public Guid Id => new("F8F6E46F-7A2A-48E3-9C0A-46AF4DC29E09");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];
}