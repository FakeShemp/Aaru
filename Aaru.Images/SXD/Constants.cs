// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for Sydex CopyQM+ Self-eXtracting Disk (SXD) images.
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

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class SXD
{
    /// <summary>SXD header signature bytes: 'S','X','D'.</summary>
    static readonly byte[] _sxdSignature = [(byte)'S', (byte)'X', (byte)'D'];

    /// <summary>Optional "WB" block signature bytes preceding the SXD payload.</summary>
    static readonly byte[] _wbSignature = [(byte)'W', (byte)'B'];

    /// <summary>Size of the SXD header, in bytes.</summary>
    const int SXD_HEADER_SIZE = 33;

    /// <summary>Number of header bytes protected by <c>crc_hdr</c>.</summary>
    const int SXD_CRC_HDR_COVER = 31;

    /// <summary>Reversed ANSI CRC-16 polynomial used by the SXD format.</summary>
    const ushort SXD_CRC_POLY = 0xA001;

    /// <summary>Maximum number of cylinders we will accept in a well-formed SXD image.</summary>
    const int NTRK_MAX = 84;

    /// <summary>All SXD tracks use 512-byte sectors.</summary>
    const int SECTOR_SIZE = 512;

    /// <summary>Fill byte used for tracks not present in the image (freshly formatted).</summary>
    const byte FMT_BYTE = 0xF6;

    /// <summary>SXD drive type codes as reported by the <c>drv_typ</c> header field.</summary>
    enum SxdDriveType : byte
    {
        Unused     = 0,
        Floppy360K = 1,
        Floppy12M  = 2,
        Floppy720K = 3,
        Floppy144M = 4,
        EightInch  = 5,
        Floppy288M = 6
    }
}