// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Nintendo.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the filesystem used by Nintendo Gamecube and Wii discs</summary>
public sealed partial class NintendoPlugin : IReadOnlyFilesystem
{
    const string MODULE_NAME = "Nintendo plugin";

    /// <summary>Cache of subdirectory entries: path → (filename → FST index)</summary>
    readonly Dictionary<string, Dictionary<string, int>> _directoryCache = new();

    /// <summary>Cache of root directory entries: filename → FST index</summary>
    readonly Dictionary<string, int> _rootDirectoryCache = new();
    DiscHeader  _discHeader;
    uint        _dolOffset;
    uint        _dolSize;
    Encoding    _encoding;
    FstEntry[]  _fstEntries;
    string[]    _fstNames;
    IMediaImage _imagePlugin;
    bool        _isWii;

    bool           _mounted;
    Aes            _partitionAes;
    ulong          _partitionDataOffset;
    ulong          _partitionOffset;
    FileSystemInfo _statfs;

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }

    /// <inheritdoc />
    public string Name => Localization.NintendoPlugin_Name;

    /// <inheritdoc />
    public Guid Id => new("4675fcb4-4418-4288-9e4a-33d6a4ac1126");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];

    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => null;

#endregion
}