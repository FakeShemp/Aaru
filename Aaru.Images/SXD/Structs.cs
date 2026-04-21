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
//     Contains structures for Sydex CopyQM+ Self-eXtracting Disk (SXD) images.
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

public sealed partial class SXD
{
#region Nested type: Header

    /// <summary>SXD image header, exactly 33 bytes long. The first 31 bytes are protected by <see cref="crc_hdr" />.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Header
    {
        /// <summary>0x00 'SXD' signature.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] signature;
        /// <summary>0x03 Track length in bytes.</summary>
        public readonly ushort trk_len;
        /// <summary>0x05 Number of sectors per track.</summary>
        public readonly byte n_trk_sec;
        /// <summary>0x06 Number of heads (1 or 2 valid).</summary>
        public readonly byte n_heads;
        /// <summary>0x07 Number of cylinders on disk.</summary>
        public readonly byte n_cyl_dsk;
        /// <summary>0x08 Number of cylinders stored in the image.</summary>
        public readonly byte n_cyl_img;
        /// <summary>0x09 ID of first sector minus 1 (usually 0).</summary>
        public readonly byte first_sid;
        /// <summary>0x0A Interleave factor (usually 1).</summary>
        public readonly byte interleave;
        /// <summary>0x0B Skew factor, probably (usually 0).</summary>
        public readonly byte skew;
        /// <summary>0x0C Sector size code (2 = 512B).</summary>
        public readonly byte sec_sz_code;
        /// <summary>0x0D Drive type, 1-6 (see <see cref="SxdDriveType" />).</summary>
        public readonly byte drv_typ;
        /// <summary>0x0E Density: 0/1/2 for DD/HD/ED.</summary>
        public readonly byte density;
        /// <summary>0x0F Password CRC (nonzero if image is encrypted).</summary>
        public readonly ushort pwd_crc;
        /// <summary>0x11 Password hash.</summary>
        public readonly ushort pwd_hash;
        /// <summary>0x13 Length of optional comment following the header.</summary>
        public readonly ushort comment_len;
        /// <summary>0x15 CRC of the comment block.</summary>
        public readonly ushort crc_comment;
        /// <summary>0x17 Unknown 4 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] unk0;
        /// <summary>0x1B Image timestamp in DOS time format.</summary>
        public readonly ushort dos_time;
        /// <summary>0x1D Image date in DOS date format.</summary>
        public readonly ushort dos_date;
        /// <summary>0x1F CRC of header bytes 0x00..0x1E.</summary>
        public readonly ushort crc_hdr;
    }

#endregion
}