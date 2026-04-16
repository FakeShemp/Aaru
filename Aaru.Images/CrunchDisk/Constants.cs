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
//     Contains constants for CrunchDisk disk images.
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

namespace Aaru.Images;

public sealed partial class CrunchDisk
{
    /// <summary>"CDF0" file header magic number</summary>
    const uint HEADER_MAGIC = 0x43444630;
    /// <summary>"CYL0" uncompressed cylinder marker</summary>
    const uint CYL_UNCOMPRESSED = 0x43594C30;
    /// <summary>"CYL1" compressed/interleaved cylinder marker</summary>
    const uint CYL_COMPRESSED = 0x43594C31;
    /// <summary>Size of the file header in bytes</summary>
    const int HEADER_SIZE = 52;
    /// <summary>Size of the per-cylinder header in bytes</summary>
    const int CYLINDER_HEADER_SIZE = 8;
}