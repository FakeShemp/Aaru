// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : UNIXBFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
/// <summary>Implements the UNIX boot filesystem</summary>
public sealed partial class BFS : IReadOnlyFilesystem
{
    const string MODULE_NAME = "BFS plugin";

    /// <summary>Maximum last inode number</summary>
    const int BFS_MAX_LASTI = 513;

    /// <summary>Cached inodes (inode number -> inode)</summary>
    readonly Dictionary<uint, Inode> _inodeCache = new();

    /// <summary>Cached root directory entries (filename -> inode number)</summary>
    readonly Dictionary<string, ushort> _rootDirectoryCache = new();

    /// <summary>Encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Last valid inode number</summary>
    uint _lastInode;

    /// <summary>Whether the filesystem is little-endian</summary>
    bool _littleEndian = true;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <summary>Cached superblock</summary>
    SuperBlock _superblock;

#region IFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];
    /// <inheritdoc />
    public string Name => Localization.BFS_Name;

    /// <inheritdoc />
    public Guid Id => new("1E6E0DA6-F7E4-494C-80C6-CB5929E96155");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}