// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for UltraISO disc images.
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

public sealed partial class UltraISO
{
    /// <summary>ISZ signature: "IsZ!"</summary>
    const uint ISZ_SIGNATURE = 0x215A7349;
    /// <summary>Size of the base ISZ header (without extended fields)</summary>
    const int ISZ_HEADER_BASE_SIZE = 48;
    /// <summary>Size of the extended ISZ header (with checksums)</summary>
    const int ISZ_HEADER_EXTENDED_SIZE = 64;
    /// <summary>Size of each segment table entry</summary>
    const int ISZ_SEGMENT_SIZE = 24;
    /// <summary>Maximum decompressed chunk cache size in bytes</summary>
    const uint MAX_CACHE_SIZE = 16777216;
    /// <summary>Sector size for ISO images</summary>
    const uint SECTOR_SIZE = 2048;
    /// <summary>Maximum number of cached sectors</summary>
    const uint MAX_CACHED_SECTORS = MAX_CACHE_SIZE / SECTOR_SIZE;
}