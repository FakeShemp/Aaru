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
//     Contains structures for QRST disk images.
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Qrst
{
#region Nested type: QrstHeader

    /// <summary>QRST image header. 796 bytes on disk.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct QrstHeader
    {
        /// <summary>'QRST' signature.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>Version number.</summary>
        public uint version;
        /// <summary>Checksum (pre-V5 only, algorithm unknown).</summary>
        public uint cksum;
        /// <summary>Image format specifier (index into the disk format table).</summary>
        public byte disk_fmt;
        /// <summary>Volume in set.</summary>
        public byte vol_set;
        /// <summary>Number of volumes in set.</summary>
        public byte set_cnt;
        /// <summary>Disk description.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public byte[] desc;
        /// <summary>Disk label.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 720)]
        public byte[] label;
        /// <summary>Contains 2 for V5, 0 for pre-V5.</summary>
        public byte type;
    }

#endregion

#region Nested type: QrstTrackHeader

    /// <summary>QRST per-track header.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct QrstTrackHeader
    {
        /// <summary>Cylinder number.</summary>
        public byte cyl;
        /// <summary>Head number.</summary>
        public byte head;
        /// <summary>Track type (<see cref="TRK_NORMAL" />, <see cref="TRK_BLANK" /> or <see cref="TRK_CMPRSD" />).</summary>
        public byte type;
    }

#endregion
}