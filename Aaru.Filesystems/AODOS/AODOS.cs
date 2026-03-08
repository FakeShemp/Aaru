// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AODOS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AO-DOS file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the AO-DOS file system and shows information.
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

// Information has been extracted looking at available disk images
/// <inheritdoc />
/// <summary>Implements the AO-DOS filesystem</summary>
public sealed partial class AODOS : IReadOnlyFilesystem
{
    const string MODULE_NAME = "AO-DOS plugin";

    /// <summary>Cached directory entries (all entries from the directory area)</summary>
    readonly List<DirectoryEntry> _directoryCache = [];

    /// <summary>Cached boot block</summary>
    BootBlock _bootBlock;

    /// <summary>Encoding used for filenames (KOI8-R)</summary>
    Encoding _encoding;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.AODOS_Name;

    /// <inheritdoc />
    public Guid Id => new("668E5039-9DDD-442A-BE1B-A315D6E38E26");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}