// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
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

// ReSharper disable UnusedMember.Local

namespace Aaru.Filesystems;

public sealed partial class HPOFS
{
    // Do not translate
    const string FS_TYPE = "hpofs";

    // Sector map (known fixed positions from reverse engineering)
    const int BOOT_SECTOR_PRIMARY    = 0x00;
    const int MEDINFO_SECTOR_PRIMARY = 0x0D;
    const int VOLINFO_SECTOR_PRIMARY = 0x0E;
    const int RWSTATS_SECTOR_PRIMARY = 0x0F;
    const int DCI_SECTOR_PRIMARY     = 0x14;
    const int BOOT_SECTOR_BACKUP     = 0x7F;
    const int MEDINFO_SECTOR_BACKUP  = 0x8C;
    const int VOLINFO_SECTOR_BACKUP  = 0x8D;
    const int RWSTATS_SECTOR_BACKUP  = 0x8E;
    const int DCI_SECTOR_BACKUP      = 0x93;

    // BPB constants
    const byte HPOFS_MEDIA_DESCRIPTOR   = 0xF8;
    const byte HPOFS_EXT_BOOT_SIGNATURE = 0x29;
    const byte HPOFS_MARKER_1           = 0x2F; // '/' ASCII
    const byte HPOFS_MARKER_2           = 0xF8; // media type echo

    // Codepage types
    const ushort CODEPAGE_TYPE_ASCII  = 1;
    const ushort CODEPAGE_TYPE_EBCDIC = 2;

    // Directory separator characters
    const byte DIR_SEPARATOR_ASCII  = 0x2F; // '/'
    const byte DIR_SEPARATOR_EBCDIC = 0x61; // EBCDIC slash equivalent

    // Key separator characters (B-tree key boundary)
    const byte KEY_SEPARATOR_ASCII  = 0x40; // '@'
    const byte KEY_SEPARATOR_EBCDIC = 0x5C; // '\'

    // Band type indicators
    const uint BAND_TYPE_STANDARD  = 0;
    const uint BAND_TYPE_DIRECTORY = 2;

    // Band flags
    const ushort BAND_FLAGS_DEFAULT     = 0;
    const ushort BAND_FLAGS_DIR_PRIMARY = 3;

    // Alloc node key lengths
    const ushort ALLOC_NODE_KEY_STANDARD = 0x14; // 20 bytes, name[12]
    const ushort ALLOC_NODE_KEY_SMALL    = 0x10; // 16 bytes, name[8]

    // DCI record constants
    const ushort DCI_RECORD_TYPE = 0x0109;
    const ushort DCI_RECORD_SIZE = 0x00DC;
    const ushort DCI_FLAGS       = 0x8000;

    // System file types (from WriteBandHeaders alloc node initialization)
    const uint SYSFILE_TYPE_UNNAMED    = 0;
    const uint SYSFILE_TYPE_ALMOSTFREE = 1;
    const uint SYSFILE_TYPE_BADSPOTS   = 2;
    const uint SYSFILE_TYPE_DIRECTORY  = 3;
    const uint SYSFILE_TYPE_FREEFILE   = 4;
    const uint SYSFILE_TYPE_RESERVED   = 5;
    const uint SYSFILE_TYPE_TOKEN      = 6;
    const uint SYSFILE_TYPE_ROOT       = 7;

    // Maximum filename length from enumerate_node_directory_entries
    const int MAX_FILENAME_LENGTH = 256;

    // Directory entry sentinel value (marks deleted/invalid entries)
    const ushort DIR_ENTRY_DELETED = 0xFFFF;

    // Extent end marker in SUBF extent lists
    const    uint   EXTENT_END_MARKER = 0xFFFFFFFF;
    readonly byte[] _dataSignature    = "DATA"u8.ToArray();
    readonly byte[] _indxSignature    = "INDX"u8.ToArray();
    readonly byte[] _mastSignature    = "MAST"u8.ToArray();
    readonly byte[] _medinfoSignature = "MEDINFO "u8.ToArray();
    readonly byte[] _smiSignature     = "SMISUBCL"u8.ToArray();
    readonly byte[] _subfSignature    = "SUBF"u8.ToArray();
    readonly byte[] _type             = "HPOFS\0\0\0"u8.ToArray();

    // Signatures discovered through reverse engineering of UHPOFS.DLL and HPOFS20.IFS
    readonly byte[] _vmiSignature     = "VMISUBCL"u8.ToArray();
    readonly byte[] _volinfoSignature = "VOLINFO "u8.ToArray();
}