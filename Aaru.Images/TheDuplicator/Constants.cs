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
//     Contains constants for The Duplicator disk images.
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

namespace Aaru.Images;

public sealed partial class TheDuplicator
{
    /// <summary>Size (in bytes) of a single sector.</summary>
    const int SECTOR_SIZE = 512;

    /// <summary>Offset of the image header.</summary>
    const int HDR_OFFSET = 0x40;

    /// <summary>Offset of the cylinder map.</summary>
    const int CYLMAP_OFFSET = 0x60;

    /// <summary>Size of the image magic signature, in bytes.</summary>
    const int MAGIC_SIZE = 48;

    /// <summary>Size of a single cylinder map entry, in bytes.</summary>
    const int CYLINFO_SIZE = 8;

    /// <summary>Cylinder is stored in the image file.</summary>
    const ushort CYLFLG_IMGDATA = 0x0000;

    /// <summary>Cylinder is not stored, filled with a filler byte.</summary>
    const ushort CYLFLG_FILLER = 0x0002;

    /// <summary>The file signature identifying a The Duplicator image (48 bytes).</summary>
    static readonly byte[] _magic = "Image file of a diskette by THE DUPLICATOR\0\0\0\0\0"u8.ToArray();
}