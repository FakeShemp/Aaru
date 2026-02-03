// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BOFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS old filesystem plugin.
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
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class BOFS : IReadOnlyFilesystem
{
    const string FS_TYPE = "beofs";

    /// <summary>Cache of root directory entries (filenames and their metadata)</summary>
    private readonly Dictionary<string, FileEntry> _rootDirectoryCache = [];

    /// <summary>The encoding to use for text data</summary>
    private Encoding _encoding;

    /// <summary>The media image plugin used to read from the device</summary>
    private IMediaImage _imagePlugin;

    /// <summary>The partition being mounted</summary>
    private Partition _partition;

    /// <summary>The filesystem superblock containing metadata about the volume</summary>
    private Track0 _track0;

    /// <inheritdoc />
    public string Name => Localization.Be_old_filesystem;

    /// <inheritdoc />
    public Guid Id => new("0841FD46-2C3C-4C96-8524-315909E4C652");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
}