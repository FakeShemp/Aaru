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
//     Contains structures for Software Pirates SNATCH-IT disk images.
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

using System.Runtime.InteropServices;

namespace Aaru.Images;

public sealed partial class SnatchIt
{
#region Nested type: Cp2Header

    /// <summary>On-disk SNATCH-IT image header (30 bytes).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Cp2Header
    {
        /// <summary>'SOFTWARE PIRATES' signature.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] pirates;
        /// <summary>Release number string.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] version;
        /// <summary>'$' literal.</summary>
        public readonly byte dollar;
        /// <summary>ASCII digit, volume number.</summary>
        public readonly byte vol_no;
    }

#endregion

#region Nested type: Cp2SectorHeader

    /// <summary>On-disk SNATCH-IT sector header (16 bytes).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Cp2SectorHeader
    {
        /// <summary>FDC read result (normally 0).</summary>
        public readonly byte result;
        /// <summary>Status ST0.</summary>
        public readonly byte st0;
        /// <summary>Status ST1.</summary>
        public readonly byte st1;
        /// <summary>Status ST2.</summary>
        public readonly byte st2;
        /// <summary>Cylinder number from ID.</summary>
        public readonly byte hdr_cyl;
        /// <summary>Head number from ID.</summary>
        public readonly byte hdr_head;
        /// <summary>Sector number from ID.</summary>
        public readonly byte hdr_sec;
        /// <summary>Sector size code.</summary>
        public readonly byte size_code;
        /// <summary>Sector offset low byte.</summary>
        public readonly byte sofs_lo;
        /// <summary>Sector offset high byte.</summary>
        public readonly byte sofs_hi;
        /// <summary>Unknown/reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] unk;
    }

#endregion

#region Nested type: Cp2TrackHeader

    /// <summary>On-disk SNATCH-IT track header (3 + 24 * 16 = 387 bytes).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Cp2TrackHeader
    {
        /// <summary>Physical cylinder (zero-based).</summary>
        public readonly byte cyl;
        /// <summary>Physical head number.</summary>
        public readonly byte head;
        /// <summary>Number of sectors on this track.</summary>
        public readonly byte num_sect;
        /// <summary>Sector headers.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CP2_MAX_SPT)]
        public readonly Cp2SectorHeader[] sct_hdr;
    }

#endregion

#region Nested type: SectorDesc

    /// <summary>In-memory descriptor for a single recorded sector.</summary>
    sealed class SectorDesc
    {
        /// <summary>True if the sector was recorded with a Deleted Data Address Mark.</summary>
        public bool Deleted;
        /// <summary>True if the sector was recorded with any uPD765 ST0/ST1/ST2 error bits set.</summary>
        public bool Errored;
        /// <summary>Offset of sector data in the image file.</summary>
        public long FileOffset;
        /// <summary>Logical sector ID from the ID AM.</summary>
        public byte SectorId;
        /// <summary>Sector size in bytes (may differ from siblings in the same track).</summary>
        public uint Size;
    }

#endregion

#region Nested type: TrackDesc

    /// <summary>In-memory descriptor for a single track.</summary>
    sealed class TrackDesc
    {
        /// <summary>Recorded sectors for this track, in physical order.</summary>
        public SectorDesc[] Sectors;
    }

#endregion

#region Nested type: SectorLoc

    /// <summary>Per-LBA map entry used by the reader.</summary>
    struct SectorLoc
    {
        /// <summary>Offset of sector data in the image file.</summary>
        public long FileOffset;
        /// <summary>Native sector size in bytes.</summary>
        public uint Size;
        /// <summary>Whether the track containing this LBA was present in the image.</summary>
        public bool TrackPresent;
        /// <summary>Whether this LBA's sector ID was found within its track.</summary>
        public bool SectorPresent;
        /// <summary>Whether the sector was recorded with a Deleted Data Address Mark.</summary>
        public bool Deleted;
        /// <summary>Whether the sector was recorded with any uPD765 ST0/ST1/ST2 error bits set.</summary>
        public bool Errored;
    }

#endregion
}