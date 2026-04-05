// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for KryoFlux STREAM images.
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

public sealed partial class KryoFlux
{
#region Nested type: OobBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OobBlock
    {
        public readonly BlockIds blockId;
        public readonly OobTypes blockType;
        public readonly ushort   length;
    }

#endregion

#region Nested type: OobStreamRead

    /// <summary>
    ///     Per KryoFlux spec: OOB Stream Read block structure (4 bytes after OOB header).
    ///     Contains stream position and elapsed time since last transfer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OobStreamRead
    {
        public readonly uint streamPosition;
        public readonly uint trTime;
    }

#endregion

#region Nested type: OobIndex

    /// <summary>
    ///     Per KryoFlux spec: OOB Index block structure (12 bytes after OOB header).
    ///     Contains stream position, timer value, and system time when index was detected.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OobIndex
    {
        public readonly uint streamPosition;
        public readonly uint timer;
        public readonly uint sysTime;
    }

#endregion

#region Nested type: OobStreamEnd

    /// <summary>
    ///     Per KryoFlux spec: OOB Stream End block structure (12 bytes after OOB header).
    ///     Contains stream position and result code.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OobStreamEnd
    {
        public readonly uint streamPosition;
        // The specification says it's a ulong, but the actual data is a uint
        public readonly uint result;
    }

#endregion

#region Nested type: TrackCapture

    /// <summary>
    ///     Internal structure representing a flux capture for a single track.
    ///     Contains decoded flux pulse data, index signal positions, and resolution information.
    /// </summary>
    public class TrackCapture
    {
        public uint   head;
        public ushort track;
        /// <summary>
        ///     Resolution (sample rate) of the flux capture in picoseconds.
        ///     Calculated from KryoFlux clock frequencies (sck).
        /// </summary>
        public ulong  resolution;
        /// <summary>
        ///     Array of flux pulse intervals in clock cycles. Each value represents the time interval
        ///     between flux reversals, measured in KryoFlux clock cycles.
        /// </summary>
        public uint[] fluxPulses;
        /// <summary>
        ///     Array of index positions. Each value is an index into the fluxPulses array
        ///     indicating where an index signal occurs.
        /// </summary>
        public uint[] indexPositions;
    }

#endregion
}