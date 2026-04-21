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
//     Contains structures for Disk eXPress (DXP) disk images.
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

public sealed partial class Dxp
{
#region Nested type: Header

    /// <summary>DXP image header, exactly 512 bytes in size.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Header
    {
        /// <summary>0x00 'AS' signature.</summary>
        public readonly ushort signature;
        /// <summary>0x02 Major DXP version needed to write image.</summary>
        public readonly byte major;
        /// <summary>0x03 Minor DXP version needed to write image.</summary>
        public readonly byte minor;
        /// <summary>0x04 DXP release letter used to create image.</summary>
        public readonly byte release;
        /// <summary>0x05 Disk format, index into the format table.</summary>
        public readonly byte dsk_typ;
        /// <summary>0x06 CRC-32 of disk data.</summary>
        public readonly uint crc_data;
        /// <summary>0x0A Compression type (0 = none, 1 = LH1, 2 = LH5).</summary>
        public readonly byte compr_typ;
        /// <summary>0x0B Last cylinder imaged.</summary>
        public readonly byte last_cyl;
        /// <summary>0x0C Last head imaged.</summary>
        public readonly byte last_head;
        /// <summary>0x0D Should be zero.</summary>
        public readonly byte unused_1;
        /// <summary>0x0E Image flags.</summary>
        public readonly byte flags;
        /// <summary>0x0F Should be zero.</summary>
        public readonly byte unused_2;
        /// <summary>0x10 Password hash (zero if image is not encrypted).</summary>
        public readonly uint pass_hash;
        /// <summary>0x14 Reserved, 284 bytes of zeroes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 284)]
        public readonly byte[] empty;
        /// <summary>0x130 CRC-32 of image header.</summary>
        public readonly uint crc_hdr;
        /// <summary>0x134 Disk image description, 5 lines of 40 chars each, space-padded.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public readonly byte[] desc;
        /// <summary>0x1FC CRC-32 of image description.</summary>
        public readonly uint crc_desc;
    }

#endregion
}