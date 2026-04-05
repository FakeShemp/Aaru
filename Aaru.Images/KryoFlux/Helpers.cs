// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains helpers for KryoFlux STREAM images.
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

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class KryoFlux
{
    /// <summary>
    ///     Converts a uint32 cell value to Aaru's flux representation format.
    ///     Format: byte array where 255 = overflow, remainder = value
    /// </summary>
    /// <param name="ticks">The cell value in clock cycles</param>
    /// <returns>Flux representation as byte array</returns>
    static byte[] UInt32ToFluxRepresentation(uint ticks)
    {
        uint over = ticks / 255;

        if(over == 0) return [(byte)ticks];

        var expanded = new byte[over + 1];
        Array.Fill(expanded, (byte)255, 0, (int)over);
        expanded[^1] = (byte)(ticks % 255);

        return expanded;
    }

    /// <summary>
    ///     Calculates resolution in picoseconds from sample clock frequency.
    ///     Resolution = (1 / sck) * 1e12 picoseconds
    /// </summary>
    /// <param name="sck">Sample clock frequency in Hz</param>
    /// <returns>Resolution in picoseconds</returns>
    static ulong CalculateResolution(double sck)
    {
        if(sck <= 0) return 0;

        double periodSeconds = 1.0 / sck;
        double periodPicoseconds = periodSeconds * 1e12;

        return (ulong)periodPicoseconds;
    }
}

