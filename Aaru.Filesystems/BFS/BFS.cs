// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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

// Information from Practical Filesystem Design, ISBN 1-55860-497-9
/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class BeFS : IReadOnlyFilesystem
{
    /// <summary>The media image plugin used to read from the device</summary>
    private IMediaImage _imagePlugin;

    /// <summary>The partition being mounted</summary>
    private Partition _partition;

    /// <summary>The encoding to use for text data</summary>
    private Encoding _encoding;

    /// <summary>Indicates if the filesystem uses little-endian byte order</summary>
    private bool _littleEndian;

    /// <summary>The filesystem superblock containing metadata about the volume</summary>
    private SuperBlock _superblock;

    /// <summary>Cache of root directory entries mapped from filename to i-node address</summary>
    private readonly Dictionary<string, long> _rootDirectoryCache = new();

    /// <inheritdoc />
    public FileSystem Metadata { get; set; } = new();
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.BeFS_Name;

    /// <inheritdoc />
    public Guid Id => new("dc8572b3-b6ad-46e4-8de9-cbe123ff6672");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}