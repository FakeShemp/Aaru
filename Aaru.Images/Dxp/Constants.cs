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
//     Contains constants for Disk eXPress (DXP) disk images.
//
//     Based on the work of Michal Necasek (fdimg).
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

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class Dxp
{
    /// <summary>DXP signature: "AS" for Albert Shan.</summary>
    const ushort DXP_SIG = 0x5341; // 'A' | ('S' << 8)

    /// <summary>Standard DXP CRC-32 seed.</summary>
    const uint DXP_CRC32_SEED = 0x59D;

    /// <summary>CRC-32 seed used by the IBM OEM version of DXP.</summary>
    const uint DXP_CRC32_SEED_IBM = 0x31E;

    /// <summary>Size of the DXP image header, in bytes.</summary>
    const int DXP_HEADER_SIZE = 512;

    /// <summary>Offset of <c>crc_hdr</c> inside the header (bytes covered by the header CRC).</summary>
    const int DXP_CRC_HDR_OFFSET = 304;

    /// <summary>Maximum number of cylinders the DXP format supports.</summary>
    const int NTRK_MAX = 80;

    /// <summary>Sector size used by all DXP images.</summary>
    const int SECTOR_SIZE = 512;

    /// <summary>Fill byte used for tracks not present in the image (freshly formatted).</summary>
    const byte FMT_BYTE = 0xF6;

    /// <summary>Uncompressed image marker (compr_typ).</summary>
    const byte DXP_COMPR_NONE = 0;
    /// <summary>LH1 (LZHUF, DXP 1.x) compression.</summary>
    const byte DXP_COMPR_LH1 = 1;
    /// <summary>LH5 (LHA 2.x) compression.</summary>
    const byte DXP_COMPR_LH5 = 2;

    /// <summary>DXP floppy formats indexed by <c>dsk_typ</c>.</summary>
    static readonly (byte Cylinders, byte Heads, byte SectorsPerTrack)[] _formatTable =
    [
        (40, 1, 8),  // 5.25" 160K
        (40, 1, 9),  // 5.25" 180K
        (40, 2, 8),  // 5.25" 320K
        (40, 2, 9),  // 5.25" 360K
        (80, 2, 9),  // 3.5"  720K
        (80, 2, 15), // 5.25" 1.2M
        (80, 2, 18), // 3.5"  1.44M
        (80, 2, 36)  // 3.5"  2.88M
    ];
}