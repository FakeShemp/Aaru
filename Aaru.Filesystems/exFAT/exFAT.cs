// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : exFAT.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft exFAT filesystem plugin.
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
using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
/// <summary>Implements detection of the exFAT filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]

// ReSharper disable once InconsistentNaming
public sealed partial class exFAT : IReadOnlyFilesystem
{
    const string MODULE_NAME = "exFAT plugin";

    // Cached data
    uint[]                                                         _fatEntries;
    Dictionary<string, CompleteDirectoryEntry>                     _rootDirectoryCache;
    Dictionary<string, Dictionary<string, CompleteDirectoryEntry>> _directoryCache;

    // File system parameters
    IMediaImage    _image;
    bool           _mounted;
    uint           _bytesPerSector;
    uint           _sectorsPerCluster;
    uint           _bytesPerCluster;
    ulong          _fatFirstSector;
    ulong          _clusterHeapOffset;
    uint           _clusterCount;
    uint           _firstClusterOfRootDirectory;
    FileSystemInfo _statfs;
    bool           _useFirstFat;
    bool           _debug;

    static Dictionary<string, string> GetDefaultOptions() => new()
    {
        {
            "debug", false.ToString()
        }
    };

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }

    /// <inheritdoc />
    public string Name => Localization.exFAT_Name;

    /// <inheritdoc />
    public Guid Id => new("8271D088-1533-4CB3-AC28-D802B68BB95C");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];

    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => new()
    {
        {
            "exfat", "exFAT default namespace"
        }
    };

#endregion
}