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
//     Contains constants for PowerISO disc images.
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

public sealed partial class PowerISO
{
    const uint MAX_CACHE_SIZE     = 16777216;
    const uint SECTOR_SIZE        = 2048;
    const uint MAX_CACHED_SECTORS = MAX_CACHE_SIZE / SECTOR_SIZE;
    const int  LZMA_PROPS_SIZE    = 5;

    /// <summary>"DAA" in 16-byte null-padded field</summary>
    readonly byte[] _daaMainSignature = [0x44, 0x41, 0x41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    /// <summary>"DAA VOL" in 16-byte null-padded field</summary>
    readonly byte[] _daaPartSignature =
        [0x44, 0x41, 0x41, 0x20, 0x56, 0x4F, 0x4C, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    /// <summary>"GBI" in 16-byte null-padded field</summary>
    readonly byte[] _gbiMainSignature = [0x47, 0x42, 0x49, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    /// <summary>"GBI VOL" in 16-byte null-padded field</summary>
    readonly byte[] _gbiPartSignature =
        [0x47, 0x42, 0x49, 0x20, 0x56, 0x4F, 0x4C, 0, 0, 0, 0, 0, 0, 0, 0, 0];
}