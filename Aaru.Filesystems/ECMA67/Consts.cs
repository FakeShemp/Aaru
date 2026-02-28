// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ECMA-67 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the ECMA-67 file system and shows information.
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

/// <inheritdoc />
/// <summary>Implements detection of the filesystem described in ECMA-67</summary>
public sealed partial class ECMA67
{
    const string FS_TYPE = "ecma67";

    /// <summary>Label Number for VOL1 and HDR1 labels. Shall be digit ONE (section 7.3.3, 7.4.3).</summary>
    const byte LABEL_NUMBER = 0x31; // ASCII '1'

    /// <summary>Label Standard Version for ECMA-67, January 1981 (section 7.3.10).</summary>
    const byte LABEL_STANDARD_VERSION = 0x31; // ASCII '1'

    /// <summary>Physical Record Length Identifier value indicating 256-byte physical records (section 7.3.8).</summary>
    const byte PHYSICAL_RECORD_LENGTH_ID = 0x31; // ASCII '1'

    // Index Cylinder (cylinder 00) sector layout, side 0 — 1-based sector numbers (section 5.6)

    /// <summary>First sector reserved for system use on side 0 of cylinder 00 (section 5.6.1).</summary>
    const int SECTOR_SYSTEM_USE_FIRST = 1;

    /// <summary>Last sector reserved for system use on side 0 of cylinder 00 (section 5.6.1).</summary>
    const int SECTOR_SYSTEM_USE_LAST = 4;

    /// <summary>Sector reserved for the ERMAP label on side 0 of cylinder 00 (section 5.6).</summary>
    const int SECTOR_ERMAP = 5;

    /// <summary>Sector reserved for future standardization on side 0 of cylinder 00 (section 5.6.2).</summary>
    const int SECTOR_RESERVED = 6;

    /// <summary>Sector reserved for the Volume Label (VOL1) on side 0 of cylinder 00 (section 5.6).</summary>
    const int SECTOR_VOL1 = 7;

    /// <summary>First sector reserved for File Labels (HDR1) on side 0 of cylinder 00 (section 5.6).</summary>
    const int SECTOR_HDR1_SIDE0_FIRST = 8;

    /// <summary>Last sector reserved for File Labels (HDR1) on side 0 of cylinder 00 (section 5.6).</summary>
    const int SECTOR_HDR1_SIDE0_LAST = 16;

    /// <summary>First sector reserved for File Labels (HDR1) on side 1 of cylinder 00 (section 5.6).</summary>
    const int SECTOR_HDR1_SIDE1_FIRST = 1;

    /// <summary>Last sector reserved for File Labels (HDR1) on side 1 of cylinder 00 (section 5.6).</summary>
    const int SECTOR_HDR1_SIDE1_LAST = 16;

    // Cylinder layout (section 5.5)

    /// <summary>Index Cylinder number (section 5.6).</summary>
    const int CYLINDER_INDEX = 0;

    /// <summary>First data cylinder number (section 5.7).</summary>
    const int CYLINDER_DATA_FIRST = 1;

    /// <summary>Last data cylinder number (section 5.7).</summary>
    const int CYLINDER_DATA_LAST = 32;

    /// <summary>First alternative cylinder, used to replace defective cylinders (section 5.5).</summary>
    const int CYLINDER_ALTERNATIVE_FIRST = 33;

    /// <summary>Last alternative cylinder, used to replace defective cylinders (section 5.5).</summary>
    const int CYLINDER_ALTERNATIVE_LAST = 34;

    // Physical record sizes (sections 5.6, 7.3.8)

    /// <summary>Physical Record length on side 0 of cylinder 00, in bytes (section 5.6).</summary>
    const int PHYSICAL_RECORD_LENGTH_INDEX_SIDE0 = 128;

    /// <summary>Physical Record length on side 1 of cylinder 00, in bytes (section 5.6).</summary>
    const int PHYSICAL_RECORD_LENGTH_INDEX_SIDE1 = 256;

    /// <summary>Physical Record length on data cylinders, in bytes (section 7.3.8).</summary>
    const int PHYSICAL_RECORD_LENGTH_DATA = 256;

    // Surface Indicator values (section 7.3.7)

    /// <summary>Surface Indicator: only side 0 is formatted according to ECMA-66 (section 7.3.7).</summary>
    const byte SURFACE_SINGLE_SIDE = 0x20; // ASCII SPACE

    /// <summary>Surface Indicator: only side 0 is formatted according to ECMA-66 (section 7.3.7).</summary>
    const byte SURFACE_SINGLE_SIDE_ONE = 0x31; // ASCII '1'

    /// <summary>Surface Indicator: both sides formatted according to ECMA-70 (section 7.3.7).</summary>
    const byte SURFACE_DOUBLE_SIDE = 0x4D; // ASCII 'M'

    // Record Format values (section 7.4.8)

    /// <summary>Record Format: fixed-length records, indicated by SPACE (section 7.4.8).</summary>
    const byte RECORD_FORMAT_FIXED_SPACE = 0x20; // ASCII SPACE

    /// <summary>Record Format: fixed-length records, indicated by 'F' (section 7.4.8).</summary>
    const byte RECORD_FORMAT_FIXED = 0x46; // ASCII 'F'

    /// <summary>Record Format: variable-length records (section 7.4.8).</summary>
    const byte RECORD_FORMAT_VARIABLE = 0x56; // ASCII 'V'

    // Bypass Indicator values (section 7.4.9)

    /// <summary>Bypass Indicator: file is intended for interchange (section 7.4.9).</summary>
    const byte BYPASS_NO = 0x20; // ASCII SPACE

    /// <summary>Bypass Indicator: file is not intended for interchange (section 7.4.9).</summary>
    const byte BYPASS_YES = 0x42; // ASCII 'B'

    // Write Protect values (section 7.4.11)

    /// <summary>Write Protect: no protection (section 7.4.11).</summary>
    const byte WRITE_PROTECT_NONE = 0x20; // ASCII SPACE

    /// <summary>Write Protect: file is protected against alteration (section 7.4.11).</summary>
    const byte WRITE_PROTECT_YES = 0x50; // ASCII 'P'

    // Interchange Type values (section 7.4.12)

    /// <summary>Interchange Type: Basic Interchange file (section 7.4.12).</summary>
    const byte INTERCHANGE_BASIC = 0x20; // ASCII SPACE

    /// <summary>Interchange Type: Extended Interchange Level 1 file (section 7.4.12).</summary>
    const byte INTERCHANGE_E1 = 0x31; // ASCII '1'

    /// <summary>Interchange Type: Extended Interchange Level 2 file (section 7.4.12).</summary>
    const byte INTERCHANGE_E2 = 0x32; // ASCII '2'

    // Multivolume Indicator values (section 7.4.13)

    /// <summary>Multivolume Indicator: file is entirely contained in the volume (section 7.4.13).</summary>
    const byte MULTIVOLUME_NONE = 0x20; // ASCII SPACE

    /// <summary>Multivolume Indicator: file continues on another volume (section 7.4.13).</summary>
    const byte MULTIVOLUME_CONTINUES = 0x43; // ASCII 'C'

    /// <summary>Multivolume Indicator: file ends but does not begin in the volume (section 7.4.13).</summary>
    const byte MULTIVOLUME_LAST = 0x4C; // ASCII 'L'

    // Record Attribute values (section 7.4.18)

    /// <summary>Record Attribute: unblocked records (section 7.4.18).</summary>
    const byte RECORD_ATTR_UNBLOCKED = 0x20; // ASCII SPACE

    /// <summary>Record Attribute: blocked records (section 7.4.18).</summary>
    const byte RECORD_ATTR_BLOCKED = 0x42; // ASCII 'B'

    // File Organization values (section 7.4.19)

    /// <summary>File Organization: sequential, indicated by SPACE (section 7.4.19).</summary>
    const byte FILE_ORG_SEQUENTIAL_SPACE = 0x20; // ASCII SPACE

    /// <summary>File Organization: sequential, indicated by 'S' (section 7.4.19).</summary>
    const byte FILE_ORG_SEQUENTIAL = 0x53; // ASCII 'S'

    // Data Mark bytes (section 9.1.1)

    /// <summary>Data Mark last byte: data is valid, Physical Record can be read (section 9.1.1).</summary>
    const byte DATA_MARK_VALID = 0xFB;

    /// <summary>
    ///     Data Mark last byte: flag byte, first byte of Physical Record is interpreted per sections 9.2 and 9.3 (section
    ///     9.1.1).
    /// </summary>
    const byte DATA_MARK_FLAG = 0xF8;

    // First byte of Physical Record when Data Mark is a flag byte (sections 9.2, 9.3)

    /// <summary>Deleted data marker: first byte of a Physical Record marked as deleted (section 9.2).</summary>
    const byte DELETED_DATA_MARKER = 0x44; // ASCII 'D'

    /// <summary>Defective Physical Record marker: first byte of a defective Physical Record (section 9.3).</summary>
    const byte DEFECTIVE_RECORD_MARKER = 0x46; // ASCII 'F'

    /// <summary>Label Identifier for the Error Map Label (section 7.5.2).</summary>
    readonly byte[] _ermapMagic = "ERMAP"u8.ToArray();

    // Expiration Date special value (section 7.4.20)

    /// <summary>Expiration Date value meaning the file shall not be deleted (section 7.4.20).</summary>
    readonly byte[] _expirationNever = "999999"u8.ToArray();

    /// <summary>Label Identifier for the File Header Label (section 7.4.2).</summary>
    readonly byte[] _hdrMagic = "HDR"u8.ToArray();

    /// <summary>Label Identifier for the Volume Label (section 7.3.2).</summary>
    readonly byte[] _magic = "VOL"u8.ToArray();
}