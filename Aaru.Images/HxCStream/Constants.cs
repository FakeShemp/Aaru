// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Rebecca Wallander <sakcheen+github@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for HxC Stream flux images.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class HxCStream
{
    const ushort DEFAULT_RESOLUTION = 40000;

    readonly byte[] _hxcStreamSignature =
    [
        0x43, 0x48, 0x4b, 0x48 // CHKH
    ];

    readonly uint _metadataId = 0x0;
    readonly uint _ioStreamId = 0x1;
    readonly uint _streamId   = 0x2;
}