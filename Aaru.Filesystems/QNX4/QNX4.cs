// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : QNX4.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the QNX 4 filesystem</summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class QNX4 : IReadOnlyFilesystem
{
    // ReSharper disable once UnusedMember.Local
    const string MODULE_NAME = "QNX4 plugin";

#region IFilesystem Members

    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions { get; } = [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces { get; } = [];

    /// <inheritdoc />
    public string Name => Localization.QNX4_Name;

    /// <inheritdoc />
    public Guid Id => new("E73A63FA-B5B0-48BF-BF82-DA5F0A8170D2");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}