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
//     Contains constants for MagicISO UIF disc images.
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

public sealed partial class MagicIso
{
    const string MODULE_NAME    = "MagicISO plugin";
    const uint   MAX_CACHE_SIZE = 16777216;

    // Descriptor signatures, stored little-endian in the UIF file.
    // ASCII "bbis", "blhr", "bsdr", "blms", "blss" as 32-bit little-endian integers.
    const uint BBIS_SIGNATURE = 0x73696262;
    const uint BLHR_SIGNATURE = 0x72686C62;
    const uint BSDR_SIGNATURE = 0x72647362;
    const uint BLMS_SIGNATURE = 0x736D6C62;
    const uint BLSS_SIGNATURE = 0x73736C62;

    // Image variants recorded in BBIS.imageType
    const ushort IMAGE_TYPE_ISO   = 8;
    const ushort IMAGE_TYPE_MIXED = 9;

    // Block entry compression types
    const uint BLOCK_TYPE_RAW  = 1;
    const uint BLOCK_TYPE_ZERO = 3;
    const uint BLOCK_TYPE_ZLIB = 5;

    // NRG trailer chunk tags, stored big-endian. Equal to the ASCII four-byte id.
    const uint NRG_ANCHOR_NERO = 0x4E45524F; // "NERO"
    const uint NRG_ANCHOR_NER5 = 0x4E455235; // "NER5"
    const uint NRG_CHUNK_DAOI  = 0x44414F49; // "DAOI"
    const uint NRG_CHUNK_DAOX  = 0x44414F58; // "DAOX"
    const uint NRG_CHUNK_ETNF  = 0x45544E46; // "ETNF"
    const uint NRG_CHUNK_ETN2  = 0x45544E32; // "ETN2"
    const uint NRG_CHUNK_CDTX  = 0x43445458; // "CDTX"
    const uint NRG_CHUNK_SINF  = 0x53494E46; // "SINF"
    const uint NRG_CHUNK_END   = 0x454E4421; // "END!"

    // Nero DAO mode byte values
    const byte NRG_MODE_MODE1       = 0x00;
    const byte NRG_MODE_MODE2_FORM1 = 0x02;
    const byte NRG_MODE_MODE2_RAW   = 0x03;
    const byte NRG_MODE_AUDIO       = 0x07;
}