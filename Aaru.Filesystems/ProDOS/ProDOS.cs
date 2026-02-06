// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ProDOS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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

// ReSharper disable NotAccessedField.Local

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Apple ProDOS 8 Technical Reference
/// <inheritdoc />
/// <summary>Implements the Apple ProDOS filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class ProDOSPlugin : IReadOnlyFilesystem
{
    const string MODULE_NAME = "ProDOS plugin";

    // Instance fields for mounted filesystem state
    IMediaImage _imagePlugin;
    Partition   _partition;
    Encoding    _encoding;
    bool        _mounted;

    // Volume directory header (superblock equivalent)
    DirectoryBlockHeader  _rootDirBlockHeader;
    VolumeDirectoryHeader _volumeHeader;
    string                _volumeName;
    DateTime              _creationTime;

    // Filesystem parameters
    uint   _multiplier;  // 2 for 256-byte sectors, 1 for 512-byte sectors
    ushort _bitmapBlock; // Start of volume bitmap
    ushort _totalBlocks; // Total blocks on volume

    // Root directory cache
    Dictionary<string, CachedEntry> _rootDirectoryCache;

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.ProDOSPlugin_Name;

    /// <inheritdoc />
    public Guid Id => new("43874265-7B8A-4739-BCF7-07F80D5932BF");

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