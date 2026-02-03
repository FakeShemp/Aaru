// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : extFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />

// ReSharper disable once InconsistentNaming
public sealed partial class extFS : IReadOnlyFilesystem
{
    const string MODULE_NAME = "extFS plugin";

    /// <summary>Cached root directory entries (filename to inode number)</summary>
    readonly Dictionary<string, uint> _rootDirectoryCache = new();

    /// <summary>The encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>The image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether the filesystem is mounted</summary>
    bool _mounted;

    /// <summary>The partition being mounted</summary>
    Partition _partition;

    /// <summary>The cached superblock</summary>
    ext_super_block _superblock;

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.extFS_Name;

    /// <inheritdoc />
    public Guid Id => new("076CB3A2-08C2-4D69-BC8A-FCAA2E502BE2");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}