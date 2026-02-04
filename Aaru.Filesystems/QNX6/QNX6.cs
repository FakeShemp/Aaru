// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : QNX6.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements QNX 6 filesystem</summary>
public sealed partial class QNX6 : IReadOnlyFilesystem
{
    const string MODULE_NAME = "QNX6 plugin";

    /// <summary>Block offset from partition start (includes boot blocks and superblock area)</summary>
    uint _blockOffset;

    /// <summary>Block size in bytes</summary>
    uint _blockSize;

    /// <summary>Encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether this is an Audi MMI filesystem</summary>
    bool _isAudiMmi;

    /// <summary>Whether the filesystem is little-endian (false = big-endian)</summary>
    bool _littleEndian;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <summary>Cached root directory entries (filename -> inode entry)</summary>
    readonly Dictionary<string, qnx6_inode_entry> _rootDirectoryCache = new();

    /// <summary>Cached superblock</summary>
    qnx6_super_block _superblock;

#region IFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }

    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];

    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

    /// <inheritdoc />
    public string Name => Localization.QNX6_Name;

    /// <inheritdoc />
    public Guid Id => new("3E610EA2-4D08-4D70-8947-830CD4C74FC0");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}