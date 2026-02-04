// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MicroDOS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MicroDOS filesystem plugin
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

// ReSharper disable UnusedType.Local
// ReSharper disable UnusedMember.Local

using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>
///     Implements the MicroDOS filesystem. Information from http://www.owg.ru/mkt/BK/MKDOS.TXT Thanks
///     to tarlabnor for translating it
/// </summary>
public sealed partial class MicroDOS : IReadOnlyFilesystem
{
    const string MODULE_NAME = "MicroDOS plugin";

    /// <summary>Block size in bytes</summary>
    const int BLOCK_SIZE = 512;

    /// <summary>Directory entry size in bytes</summary>
    const int DIR_ENTRY_SIZE = 24;

    /// <summary>Offset of first directory entry in block 0</summary>
    const int DIR_START_OFFSET = 320;

    /// <summary>Subdirectory marker (first byte of filename = 0x7F)</summary>
    const byte SUBDIR_MARKER = 0x7F;

    /// <summary>Cached root directory entries (filename -> DirectoryEntry)</summary>
    readonly Dictionary<string, DirectoryEntry> _rootDirectoryCache = new();

    /// <summary>Cached superblock (block 0)</summary>
    Block0 _block0;

    /// <summary>Encoding used for filenames (KOI8-R)</summary>
    Encoding _encoding;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

#region IFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

    /// <inheritdoc />
    public string Name => Localization.MicroDOS_Name;

    /// <inheritdoc />
    public Guid Id => new("9F9A364A-1A27-48A3-B730-7A7122000324");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}