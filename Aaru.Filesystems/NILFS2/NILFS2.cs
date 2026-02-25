// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : NILFS2.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2 : IReadOnlyFilesystem
{
    const string MODULE_NAME = "NILFS2 plugin";

    /// <summary>The filesystem block size in bytes</summary>
    uint _blockSize;

    /// <summary>Cached DAT inode from the super root, used for virtual-to-physical block translation</summary>
    Inode _datInode;

    /// <summary>The encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>The image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether the filesystem is mounted</summary>
    bool _mounted;

    /// <summary>The partition being mounted</summary>
    Partition _partition;

    /// <summary>Cached root directory entries (filename to directory entry info)</summary>
    Dictionary<string, DirectoryEntryInfo> _rootDirectoryCache;

    /// <summary>The cached superblock</summary>
    Superblock _superblock;

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.NILFS2_Name;

    /// <inheritdoc />
    public Guid Id => new("35224226-C5CC-48B5-8FFD-3781E91E86B6");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}