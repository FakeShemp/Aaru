// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SonyPFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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
using Aaru.CommonTypes.Structs;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;

namespace Aaru.Filesystems;

public partial class SonyPFS : IReadOnlyFilesystem
{
    bool           _mounted;
    IMediaImage    _image;
    Encoding       _encoding;
    SuperBlock     _superBlock;
    uint           _sectorSize;
    uint           _sectorsPerZone;
    uint           _inodeScale;
    ulong          _partitionStart;
    FileSystemInfo _statfs;

    Dictionary<string, Dictionary<string, DirEntry>> _directoryCache;
    Dictionary<string, DirEntry>                     _rootDirectoryCache;

    /// <inheritdoc />
    public string Name => Localization.PlayStation_FileSystem;
    /// <inheritdoc />
    public Guid Id => new("A68A4B2D-BF28-4D32-BDDB-638C03507A5F");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => [];
}