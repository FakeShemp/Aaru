// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : VxFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Veritas File System plugin.
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
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS : IReadOnlyFilesystem
{
    const string MODULE_NAME = "VxFS plugin";

    /// <summary>Cached inodes (inode number -> DiskInode)</summary>
    readonly Dictionary<uint, DiskInode> _inodeCache = new();

    /// <summary>Cached root directory entries (filename -> inode number)</summary>
    readonly Dictionary<string, uint> _rootDirectoryCache = new();

    /// <summary>Whether the filesystem is big-endian (HP-UX/parisc)</summary>
    bool _bigEndian;

    /// <summary>Encoding for filenames</summary>
    Encoding _encoding;

    /// <summary>Fileset header inode number from OLT</summary>
    uint _fsHeadIno;

    /// <summary>Initial inode list extent (block number)</summary>
    uint _ilistExtent;

    /// <summary>Primary inode list inode (file/directory inodes)</summary>
    DiskInode _ilistInode;

    /// <summary>Image being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <summary>Structural inode list inode (system metadata)</summary>
    DiskInode _stilistInode;

    /// <summary>Cached superblock</summary>
    SuperBlock _superblock;
    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.VxFS_Name;

    /// <inheritdoc />
    public Guid Id => new("EC372605-7687-453C-8BEA-7E0DFF79CB03");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}