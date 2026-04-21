// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for QRST disk images.
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

/* Based on the work of Michal Necasek (www.os2museum.com). */

namespace Aaru.Images;

public sealed partial class Qrst
{
    /// <summary>Sector size for all supported QRST formats.</summary>
    const int SECTOR_SIZE = 512;

    /// <summary>Regular, uncompressed track.</summary>
    const byte TRK_NORMAL = 0;

    /// <summary>Blank track filled with a single repeating byte.</summary>
    const byte TRK_BLANK = 1;

    /// <summary>RLE-compressed track.</summary>
    const byte TRK_CMPRSD = 2;

    /// <summary>QRST image signature.</summary>
    static readonly byte[] _signature = "QRST"u8.ToArray();

    /// <summary>Disk format descriptor: cylinders, heads, sectors-per-track. Index 0 is reserved (unknown).</summary>
    static readonly (byte Cyls, byte Heads, byte Spt)[] _dskDesc =
    [
        (0, 0, 0),   // 0: unknown
        (40, 2, 9),  // 1: 360K
        (80, 2, 15), // 2: 1.2M
        (80, 2, 9),  // 3: 720K
        (80, 2, 18), // 4: 1.44M
        (40, 1, 8),  // 5: 160K
        (40, 1, 9),  // 6: 180K
        (40, 2, 8)   // 7: 320K
    ];
}