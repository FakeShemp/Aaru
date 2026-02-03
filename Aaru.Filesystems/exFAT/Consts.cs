// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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
using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
/// <summary>Implements detection of the exFAT filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]

// ReSharper disable once InconsistentNaming
public sealed partial class exFAT
{
    const string FS_TYPE = "exfat";

    /// <summary>Flash Parameters GUID: {0A0C7E46-3399-4021-90C8-FA6D389C4BA2} (Section 3.3.4.1).</summary>
    static readonly Guid _oemFlashParameterGuid = new("0A0C7E46-3399-4021-90C8-FA6D389C4BA2");

    /// <summary>Null Parameters GUID: {00000000-0000-0000-0000-000000000000} (Section 3.3.3.1).</summary>
    static readonly Guid _oemNullParameterGuid = Guid.Empty;

    /// <summary>File system signature "EXFAT   " (Section 3.1.2).</summary>
    static readonly byte[] _signature = "EXFAT   "u8.ToArray();

    /// <summary>Boot sector signature (0xAA55) (Section 3.1.20).</summary>
    const ushort BOOT_SIGNATURE = 0xAA55;

    /// <summary>Extended boot sector signature (0xAA550000) (Section 3.2.2).</summary>
    const uint EXTENDED_BOOT_SIGNATURE = 0xAA550000;

    /// <summary>Recommended Up-case Table checksum (Section 7.2.5.1).</summary>
    const uint RECOMMENDED_UPCASE_TABLE_CHECKSUM = 0xE619D30D;
}