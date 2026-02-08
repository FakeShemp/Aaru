// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Acorn.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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
/// <summary>Implements Acorn's Advanced Data Filing System (ADFS)</summary>
public sealed partial class AcornADFS : IReadOnlyFilesystem
{
    const string MODULE_NAME = "ADFS Plugin";

    /// <summary>Cached root directory entries (filename -> DirectoryEntryInfo)</summary>
    Dictionary<string, DirectoryEntryInfo> _rootDirectoryCache;

    /// <summary>Encoding used for filenames</summary>
    Encoding _encoding;

    /// <summary>Image plugin being accessed</summary>
    IMediaImage _imagePlugin;

    /// <summary>Whether filesystem is mounted</summary>
    bool _mounted;

    /// <summary>Partition being mounted</summary>
    Partition _partition;

    /// <summary>Whether this is an old map format (ADFS-S, ADFS-M, ADFS-L, ADFS-D)</summary>
    bool _isOldMap;

    /// <summary>Disc record (for new formats)</summary>
    DiscRecord _discRecord;

    /// <summary>Old map sector 0 (for old formats)</summary>
    OldMapSector0 _oldMap0;

    /// <summary>Old map sector 1 (for old formats)</summary>
    OldMapSector1 _oldMap1;

    /// <summary>Root directory indirect disc address</summary>
    uint _rootDirectoryAddress;

    /// <summary>Root directory size in bytes</summary>
    uint _rootDirectorySize;

    /// <summary>Whether this is a big directory format (F+)</summary>
    bool _isBigDirectory;

    /// <summary>Block size in bytes</summary>
    int _blockSize;

    /// <summary>Log2 of bytes per map bit</summary>
    int _log2BytesPerMapBit;

    /// <summary>Maximum filename length</summary>
    int _maxNameLen;

    /// <summary>Total disc size in bytes</summary>
    ulong _discSize;

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.AcornADFS_Name;

    /// <inheritdoc />
    public Guid Id => new("BAFC1E50-9C64-4CD3-8400-80628CC27AFA");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}