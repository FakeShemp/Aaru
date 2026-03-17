// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for Nintendo Wii U compressed disc images (WUX format).
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

using System.Runtime.InteropServices;

namespace Aaru.Images;

public sealed partial class Wux
{
#region Nested type: WuxHeader

    /// <summary>WUX file header, 32 bytes, all fields little-endian</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct WuxHeader
    {
        /// <summary>Offset 0x00, "WUX0" magic (0x30585557 LE)</summary>
        public uint Magic;
        /// <summary>Offset 0x04, reserved / version</summary>
        public uint Reserved;
        /// <summary>Offset 0x08, sector size (must be 0x8000)</summary>
        public uint SectorSize;
        /// <summary>Offset 0x0C, must be 0</summary>
        public uint Reserved2;
        /// <summary>Offset 0x10, uncompressed disc size in bytes</summary>
        public ulong UncompressedSize;
        /// <summary>Offset 0x18, must be 0</summary>
        public ulong Reserved3;
    }

#endregion
}