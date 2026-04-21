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
//     Contains structures for The Duplicator disk images.
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
public sealed partial class TheDuplicator
{
#region Nested type: TdupHeader

    /// <summary>The Duplicator image header, located at offset 0x40.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TdupHeader
    {
        /// <summary>Possibly a version field.</summary>
        public ushort version;
        /// <summary>Number of heads (1 or 2).</summary>
        public ushort numHeads;
        /// <summary>Number of sectors per track.</summary>
        public ushort numSec;
        /// <summary>Number of cylinders.</summary>
        public ushort numCyls;
        /// <summary>Unknown field.</summary>
        public ushort unknown;
        /// <summary>Possibly a media type code (1=360K, 2=720K, 3=1.2M, 4=1.44M).</summary>
        public ushort media;
    }

#endregion

#region Nested type: TdupCylInfo

    /// <summary>The Duplicator cylinder map entry, located at offset 0x60.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TdupCylInfo
    {
        /// <summary>Possibly a cylinder checksum (algorithm unknown).</summary>
        public ushort cksum;
        /// <summary>Cylinder flags (<see cref="CYLFLG_IMGDATA" /> or <see cref="CYLFLG_FILLER" />).</summary>
        public ushort flags;
        /// <summary>File offset in 512-byte units where the cylinder data is stored.</summary>
        public ushort start;
        /// <summary>Filler byte used for left-out cylinders.</summary>
        public byte filler;
        /// <summary>Possibly unused/junk byte.</summary>
        public byte junk;
    }

#endregion
}