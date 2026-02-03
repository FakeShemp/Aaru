// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xia.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Xia filesystem plugin.
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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Xia : IReadOnlyFilesystem
{
    const string MODULE_NAME = "Xia plugin";

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

    /// <summary>Cache of root directory entries (filenames and their inode numbers)</summary>
    readonly Dictionary<string, uint> _rootDirectoryCache = [];

    /// <summary>The encoding to use for text data</summary>
    Encoding _encoding;

    /// <summary>The media image plugin used to read from the device</summary>
    IMediaImage _imagePlugin;

    /// <summary>Indicates if the filesystem is currently mounted</summary>
    bool _mounted;

    /// <summary>The partition being mounted</summary>
    Partition _partition;

    /// <summary>The filesystem superblock</summary>
    SuperBlock _superblock;

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.Xia_Name;

    /// <inheritdoc />
    public Guid Id => new("169E1DE5-24F2-4EF6-A04D-A4B2CA66DE9D");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}