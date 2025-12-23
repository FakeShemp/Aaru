// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser filesystem plugin
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Reiser v3 filesystem</summary>
public sealed partial class Reiser
{
    const uint REISER_SUPER_OFFSET = 0x10000;

    const string FS_TYPE = "reiserfs";

    readonly byte[] _magic35 = "ReIsErFs\0\0"u8.ToArray();
    readonly byte[] _magic36 = "ReIsEr2Fs\0"u8.ToArray();
    readonly byte[] _magicJr = "ReIsEr3Fs\0"u8.ToArray();
}