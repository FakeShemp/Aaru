// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : RBF.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Random Block File filesystem</summary>
public sealed partial class RBF : IReadOnlyFilesystem
{
    const string MODULE_NAME = "RBF plugin";
    uint         _bitmapLsn; // LSN of allocation bitmap
    Encoding     _encoding;

    // Cached superblock data
    IdSector _idSector;
    ulong    _idSectorLocation; // Location of ID sector (0, 4, or 15)

    // Instance fields for mounted filesystem state
    IMediaImage _imagePlugin;

    // Filesystem type flags
    bool _isOs9000;
    bool _littleEndian;

    // Calculated filesystem parameters
    uint        _lsnSize; // Logical sector size in bytes
    bool        _mounted;
    NewIdSector _newIdSector;
    ulong       _partitionStart;

    // Root directory cache
    Dictionary<string, CachedDirectoryEntry> _rootDirectoryCache;
    FileDescriptor                           _rootDirectoryFd;
    uint                                     _rootDirLsn; // LSN of root directory FD
    uint                                     _sectorSize;
    uint                                     _sectorsPerCluster;
    uint                                     _totalSectors; // Total sectors on disk

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.RBF_Name;

    /// <inheritdoc />
    public Guid Id => new("E864E45B-0B52-4D29-A858-7BDFA9199FB2");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}