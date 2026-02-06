// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : RT11.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : RT-11 file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the RT-11 file system and shows information.
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

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
/// <summary>Implements the DEC RT-11 filesystem</summary>
public sealed partial class RT11 : IReadOnlyFilesystem
{
    const string MODULE_NAME = "RT-11 plugin";

    /// <summary>The encoding to use for text data</summary>
    Encoding _encoding;

    /// <summary>First directory segment block number</summary>
    ushort _firstDirectoryBlock;

    /// <summary>The home block</summary>
    HomeBlock _homeBlock;

    /// <summary>The media image plugin used to read from the device</summary>
    IMediaImage _imagePlugin;

    /// <summary>Indicates if the filesystem is currently mounted</summary>
    bool _mounted;

    /// <summary>The partition being mounted</summary>
    Partition _partition;

    /// <summary>Cache of root directory entries mapped from filename to starting block number</summary>
    Dictionary<string, uint> _rootDirectoryCache;

    /// <summary>Total number of directory segments</summary>
    ushort _totalSegments;

#region IFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }

    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];

    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

    /// <inheritdoc />
    public string Name => Localization.RT11_Name;

    /// <inheritdoc />
    public Guid Id => new("DB3E2F98-8F98-463C-8126-E937843DA024");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}