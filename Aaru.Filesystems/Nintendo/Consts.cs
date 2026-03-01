// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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

public sealed partial class NintendoPlugin
{
    const string FS_TYPE_NGC = "ngcfs";
    const string FS_TYPE_WII = "wiifs";

    const uint GC_MAGIC  = 0xC2339F3D;
    const uint WII_MAGIC = 0x5D1C9EA3;

    /// <summary>Virtual FST index for the main DOL executable (not in the real FST)</summary>
    const int DOL_VIRTUAL_INDEX = -1;

    /// <summary>Size of the DOL header in bytes</summary>
    const int DOL_HEADER_SIZE = 0x100;

    /// <summary>Number of code (text) segments in a DOL file</summary>
    const int DOL_CODE_SEGMENTS = 7;

    /// <summary>Number of data segments in a DOL file</summary>
    const int DOL_DATA_SEGMENTS = 11;

    /// <summary>Size of a Wii data cluster on disk (hash + data)</summary>
    const int WII_CLUSTER_SIZE = 0x8000;

    /// <summary>Size of the hash/metadata portion at the start of each Wii cluster</summary>
    const int WII_CLUSTER_HASH_SIZE = 0x400;

    /// <summary>Size of the decrypted data portion of each Wii cluster</summary>
    const int WII_CLUSTER_DATA_SIZE = 0x7C00;

    /// <summary>Wii common key used to decrypt partition title keys</summary>
    static readonly byte[] WII_COMMON_KEY =
    [
        0xEB, 0xE4, 0x2A, 0x22, 0x5E, 0x85, 0x93, 0xE4, 0x48, 0xD9, 0xC5, 0x45, 0x73, 0x81, 0xAA, 0xF7
    ];

    /// <summary>Wii Korean common key</summary>
    static readonly byte[] WII_KOREAN_KEY =
    [
        0x63, 0xB8, 0x2B, 0xB4, 0xF4, 0x61, 0x4E, 0x2E, 0x13, 0xF2, 0xFE, 0xFB, 0xBA, 0x4C, 0x9B, 0x7E
    ];
}