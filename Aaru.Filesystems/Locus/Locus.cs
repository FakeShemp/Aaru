// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Locus.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin
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
//     License aint with this library; if not, see <http://www.gnu.org/licenses/>.
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

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Locus filesystem</summary>
public sealed partial class Locus : IReadOnlyFilesystem
{
    const string MODULE_NAME = "Locus plugin";

    /// <summary>Root inode number (always 2)</summary>
    const int ROOT_INO = 2;

    /// <summary>Size of disk inode structure (128 bytes for standard, 512 for small block)</summary>
    const int DINODE_SIZE = 128;

    /// <summary>Size of disk inode structure for smallblock filesystems</summary>
    const int DINODE_SMALLBLOCK_SIZE = 512;

    /// <summary>Cached inodes (inode number -> inode)</summary>
    readonly Dictionary<int, Dinode> _inodeCache = new();

    /// <summary>Cached root directory entries (filename -> inode number)</summary>
    readonly Dictionary<string, int> _rootDirectoryCache = new();

    /// <summary>Whether the filesystem uses big-endian byte order</summary>
    bool _bigEndian;

    /// <summary>Block size in bytes (1024 or 4096)</summary>
    int _blockSize;

    /// <summary>Encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Calculated inodes per block</summary>
    int _inodesPerBlock;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <summary>Whether this is a smallblock filesystem</summary>
    bool _smallBlocks;

    /// <summary>Cached superblock</summary>
    Superblock _superblock;

    /// <summary>Location of superblock in sectors from partition start</summary>
    ulong _superblockLocation;

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.Locus_Name;

    /// <inheritdoc />
    public Guid Id => new("1A70B30A-437D-479A-88E1-D0C9C1797FF4");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}