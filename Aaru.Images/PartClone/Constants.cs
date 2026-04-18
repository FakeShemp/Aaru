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
//     Contains constants for partclone disk images.
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

public sealed partial class PartClone
{
    /// <summary>Regular size of the per-block CRC32 trailer in partclone v0001.</summary>
    const int CRC_SIZE_NORMAL = 4;
    /// <summary>
    ///     Size of the per-block CRC32 trailer in partclone v0001 images affected by the legacy 64-bit platform bug
    ///     (the CRC was written using <c>sizeof(unsigned long)</c>, producing 8 bytes on x86_64 instead of 4).
    /// </summary>
    const int CRC_SIZE_X64_BUG = 8;
    /// <summary>Polynomial used by partclone's reflected CRC-32 (Ethernet / ISO).</summary>
    const uint CRC32_POLYNOMIAL = 0xEDB88320;
    /// <summary>Initial seed used by partclone's <c>init_crc32()</c>.</summary>
    const uint CRC32_SEED = 0xFFFFFFFF;
    /// <summary>Only partclone image format 0001 is supported.</summary>
    const string SUPPORTED_VERSION = "0001";

    const    uint   MAX_CACHE_SIZE     = 16777216;
    const    uint   MAX_CACHED_SECTORS = MAX_CACHE_SIZE / 512;
    readonly byte[] _biTmAgIc          = "BiTmAgIc"u8.ToArray();
    readonly byte[] _partCloneMagic    = "partclone-image"u8.ToArray();
}