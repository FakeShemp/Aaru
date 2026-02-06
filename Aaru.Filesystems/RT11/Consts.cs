// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : RT-11 file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the RT-11 file system and shows information.
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

namespace Aaru.Filesystems;

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
public sealed partial class RT11
{
    const string FS_TYPE = "rt11";

    // Block numbers
    /// <summary>Home block location</summary>
    const int HOME_BLOCK = 1;

    /// <summary>Blocks reserved for system use (blocks 0-5)</summary>
    const int RESERVED_BLOCKS = 6;

    /// <summary>Block size in words (16-bit units)</summary>
    const int BLOCK_SIZE_WORDS = 256;

    /// <summary>Block size in bytes</summary>
    const int BLOCK_SIZE_BYTES = 512;

    /// <summary>Directory segment size in words</summary>
    const int SEGMENT_SIZE_WORDS = 512;

    /// <summary>Directory segment header size in words</summary>
    const int SEGMENT_HEADER_WORDS = 5;

    /// <summary>Directory entry size in words (without extra bytes)</summary>
    const int DIRECTORY_ENTRY_WORDS = 7;

    /// <summary>Maximum number of directory segments</summary>
    const int MAX_DIRECTORY_SEGMENTS = 31;

    // Directory entry status word values (high byte)
    /// <summary>Tentative file entry</summary>
    const ushort E_TENT = 0x0400;

    /// <summary>Empty area entry</summary>
    const ushort E_MPTY = 0x1000;

    /// <summary>Permanent file entry</summary>
    const ushort E_PERM = 0x2000;

    /// <summary>End-of-segment marker</summary>
    const ushort E_EOS = 0x4000;

    /// <summary>Read-protected file</summary>
    const ushort E_READ = 0x4000;

    /// <summary>Write-protected file</summary>
    const ushort E_PROT = 0x8000;

    // Directory entry status word values (low byte)
    /// <summary>Prefix block indicator</summary>
    const ushort E_PRE = 0x0020;

    // Date word format constants
    /// <summary>Mask for day field (bits 0-4)</summary>
    const ushort DATE_DAY_MASK = 0x001F;

    /// <summary>Mask for month field (bits 5-8)</summary>
    const ushort DATE_MONTH_MASK = 0x01E0;

    /// <summary>Shift for month field</summary>
    const int DATE_MONTH_SHIFT = 5;

    /// <summary>Mask for year field (bits 9-13)</summary>
    const ushort DATE_YEAR_MASK = 0x3E00;

    /// <summary>Shift for year field</summary>
    const int DATE_YEAR_SHIFT = 9;

    /// <summary>Mask for age bits (bits 14-15)</summary>
    const ushort DATE_AGE_MASK = 0xC000;

    /// <summary>Shift for age bits</summary>
    const int DATE_AGE_SHIFT = 14;

    /// <summary>Base year for age 0</summary>
    const int BASE_YEAR_AGE0 = 1972;

    /// <summary>Base year for age 1</summary>
    const int BASE_YEAR_AGE1 = 2004;

    /// <summary>Base year for age 2</summary>
    const int BASE_YEAR_AGE2 = 2036;

    /// <summary>Base year for age 3</summary>
    const int BASE_YEAR_AGE3 = 2068;

    /// <summary>Years per age increment</summary>
    const int YEARS_PER_AGE = 32;
}