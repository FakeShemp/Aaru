// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AmigaDOS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Amiga Fast File System plugin.
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
/// <summary>Implements Amiga Fast File System (AFFS)</summary>
public sealed partial class AmigaDOSPlugin : IReadOnlyFilesystem
{
    const string MODULE_NAME = "AmigaDOS plugin";

    /// <summary>Block size in bytes</summary>
    uint _blockSize;

    /// <summary>The boot block</summary>
    BootBlock _bootBlock;

    /// <summary>Indicates if the boot block checksum is valid</summary>
    bool _bootBlockValid;

    /// <summary>The encoding to use for text data</summary>
    Encoding _encoding;

    /// <summary>Indicates if directory cache is enabled</summary>
    bool _hasDirCache;

    /// <summary>Indicates if long filenames are supported (OFS2/FFS2)</summary>
    bool _hasLongNames;

    /// <summary>The media image plugin used to read from the device</summary>
    IMediaImage _imagePlugin;

    /// <summary>Indicates if this is a Fast File System (vs OFS)</summary>
    bool _isFfs;

    /// <summary>Indicates if international mode is enabled</summary>
    bool _isIntl;

    /// <summary>Indicates if this is a multi-user filesystem (MuFS)</summary>
    bool _isMuFs;

    /// <summary>Indicates if the filesystem is currently mounted</summary>
    bool _mounted;

    /// <summary>The partition being mounted</summary>
    Partition _partition;

    /// <summary>The root block</summary>
    RootBlock _rootBlock;

    /// <summary>Root block sector number (relative to partition start)</summary>
    uint _rootBlockSector;

    /// <summary>Cache of root directory entries mapped from filename to block number</summary>
    Dictionary<string, uint> _rootDirectoryCache;

    /// <summary>Sectors per block</summary>
    uint _sectorsPerBlock;

    /// <summary>Total blocks in filesystem</summary>
    uint _totalBlocks;

#region IFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

    /// <inheritdoc />
    public string Name => Localization.AmigaDOSPlugin_Name;

    /// <inheritdoc />
    public Guid Id => new("3c882400-208c-427d-a086-9119852a1bc7");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}