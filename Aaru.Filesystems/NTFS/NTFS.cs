// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : NTFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

// Information from Inside Windows NT and the Linux kernel NTFS driver (fs/ntfs)
/// <inheritdoc />
/// <summary>Implements the New Technology File System (NTFS)</summary>
public sealed partial class NTFS : IReadOnlyFilesystem
{
    /// <inheritdoc />
    public FileSystem Metadata { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string> Namespaces => [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.NTFS_Name;

    /// <inheritdoc />
    public Guid Id => new("33513B2C-1e6d-4d21-a660-0bbc789c3871");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}