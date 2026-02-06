// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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

using Aaru.Helpers;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class ProDOSPlugin
{
    /// <summary>Parses a directory entry from a block using marshalling</summary>
    CachedEntry ParseDirectoryEntry(byte[] block, int offset)
    {
        // Marshal the entry structure
        Entry entry =
            Marshal.ByteArrayToStructureLittleEndian<Entry>(block,
                                                            offset,
                                                            System.Runtime.InteropServices.Marshal.SizeOf<Entry>());

        var storageType = (byte)((entry.storage_type_name_length & STORAGE_TYPE_MASK) >> 4);
        var nameLength  = (byte)(entry.storage_type_name_length & NAME_LENGTH_MASK);

        if(nameLength == 0) return null;

        // Extract filename
        string fileName = _encoding.GetString(entry.file_name, 0, nameLength);

        // Apply case bits if present (GS/OS extension)
        if((entry.case_bits & 0x8000) != 0) fileName = ApplyCaseBits(fileName, entry.case_bits);

        // Parse EOF (3 bytes little-endian)
        var eof = (uint)(entry.eof[0] | entry.eof[1] << 8 | entry.eof[2] << 16);

        return new CachedEntry
        {
            Name             = fileName,
            StorageType      = storageType,
            FileType         = entry.file_type,
            KeyBlock         = entry.key_pointer,
            BlocksUsed       = entry.blocks_used,
            Eof              = eof,
            CreationTime     = DateHandlers.ProDosToDateTime(entry.creation_date,     entry.creation_time),
            ModificationTime = DateHandlers.ProDosToDateTime(entry.modification_date, entry.modification_time),
            Access           = entry.access,
            AuxType          = entry.aux_type,
            HeaderPointer    = entry.header_pointer,
            CaseBits         = entry.case_bits
        };
    }
}