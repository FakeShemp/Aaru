// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Enumerations for WinOnCD disc images.
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

namespace Aaru.Images;

public sealed partial class WinOnCD
{
    /// <summary>C2D track mode values.</summary>
    enum C2dMode : byte
    {
        /// <summary>Audio track.</summary>
        Audio = 0x00,
        /// <summary>Mode 1 data track.</summary>
        Mode1 = 0x01,
        /// <summary>Mode 2 data track.</summary>
        Mode2 = 0x02,
        /// <summary>Audio track (alternative value).</summary>
        Audio2 = 0xFF
    }

    /// <summary>C2D track flag values.</summary>
    [Flags]
    enum C2dFlag : byte
    {
        /// <summary>No flags set.</summary>
        None = 0x00,
        /// <summary>Copyright flag.</summary>
        Copyright = 0x01,
        /// <summary>Pre-emphasis flag.</summary>
        PreEmphasis = 0x02,
        /// <summary>Data track flag.</summary>
        Data = 0x04,
        /// <summary>Unknown flag.</summary>
        Unknown = 0x08,
        /// <summary>O flag (WinOnCD specific).</summary>
        O = 0x10
    }
}