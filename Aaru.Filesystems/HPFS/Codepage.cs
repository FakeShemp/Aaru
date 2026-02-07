// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Codepage.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
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

using System;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class HPFS
{
    /// <summary>Loads the code page table from disk.</summary>
    /// <param name="cpDirSector">Sector number of the code page directory.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber LoadCodePageTable(uint cpDirSector)
    {
        ErrorNumber errno = _image.ReadSector(_partition.Start + cpDirSector, false, out byte[] cpDirSector_, out _);

        if(errno != ErrorNumber.NoError) return errno;

        CodePageDirectory cpDir = Marshal.ByteArrayToStructureLittleEndian<CodePageDirectory>(cpDirSector_);

        if(cpDir.magic != CP_DIR_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid code page directory magic: 0x{0:X8} (expected 0x{1:X8})",
                              cpDir.magic,
                              CP_DIR_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        if(cpDir.n_code_pages == 0) return ErrorNumber.NoError;

        // Get the target code page number from the user's encoding
        int targetCodePage = GetCodePageFromEncoding(_encoding);

        AaruLogging.Debug(MODULE_NAME,
                          "Looking for code page {0} among {1} available code pages",
                          targetCodePage,
                          cpDir.n_code_pages);

        // Search through all code page directory entries to find a matching code page
        const int cpDirEntryOffset = 16; // After magic + n_code_pages + 2 reserved dwords
        const int cpDirEntrySize   = 16; // Size of CodePageDirectoryEntry

        CodePageDirectoryEntry? matchingEntry = null;
        CodePageDirectoryEntry? fallbackEntry = null;

        for(var i = 0; i < cpDir.n_code_pages && i < 31; i++)
        {
            int entryOffset = cpDirEntryOffset + i * cpDirEntrySize;

            if(entryOffset + cpDirEntrySize > cpDirSector_.Length) break;

            CodePageDirectoryEntry cpEntry =
                Marshal.ByteArrayToStructureLittleEndian<CodePageDirectoryEntry>(cpDirSector_,
                    entryOffset,
                    cpDirEntrySize);

            AaruLogging.Debug(MODULE_NAME, "Found code page {0} at index {1}", cpEntry.code_page_number, cpEntry.ix);

            // Check if this matches the target code page
            if(cpEntry.code_page_number == targetCodePage)
            {
                matchingEntry = cpEntry;

                break;
            }

            // Keep the first entry as fallback
            fallbackEntry ??= cpEntry;
        }

        // Use matching entry, or fall back to first entry
        CodePageDirectoryEntry? entryToUse = matchingEntry ?? fallbackEntry;

        if(entryToUse == null) return ErrorNumber.NoError;

        if(matchingEntry != null)
        {
            AaruLogging.Debug(MODULE_NAME, "Using matching code page {0}", entryToUse.Value.code_page_number);
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Code page {0} not found, falling back to code page {1}",
                              targetCodePage,
                              entryToUse.Value.code_page_number);
        }

        // Read the code page data sector
        errno = _image.ReadSector(_partition.Start + entryToUse.Value.code_page_data,
                                  false,
                                  out byte[] cpDataSector,
                                  out _);

        if(errno != ErrorNumber.NoError) return errno;

        CodePageData cpData = Marshal.ByteArrayToStructureLittleEndian<CodePageData>(cpDataSector);

        if(cpData.magic != CP_DATA_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid code page data magic: 0x{0:X8} (expected 0x{1:X8})",
                              cpData.magic,
                              CP_DATA_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        // Find the correct code page data entry using the index from the directory entry
        int cpDataEntryIndex = entryToUse.Value.index;

        if(cpDataEntryIndex >= cpData.n_used || cpData.offs == null || cpDataEntryIndex >= cpData.offs.Length)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Code page data entry index {0} out of range (n_used={1})",
                              cpDataEntryIndex,
                              cpData.n_used);

            return ErrorNumber.InvalidArgument;
        }

        int tableOffset = cpData.offs[cpDataEntryIndex] + 6; // Skip index, code page number, and unknown fields

        if(tableOffset + 128 > cpDataSector.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Code page table offset {0} out of range", tableOffset);

            return ErrorNumber.InvalidArgument;
        }

        _codePageTable = new byte[128];
        Array.Copy(cpDataSector, tableOffset, _codePageTable, 0, 128);

        AaruLogging.Debug(MODULE_NAME, "Successfully loaded code page table");

        return ErrorNumber.NoError;
    }

    /// <summary>Gets the DOS/Windows code page number from an Encoding.</summary>
    /// <param name="encoding">The encoding to get the code page for.</param>
    /// <returns>The code page number, or 850 as default.</returns>
    static int GetCodePageFromEncoding(Encoding encoding)
    {
        // Try to get the code page directly from the encoding
        int codePage = encoding.CodePage;

        // Map common .NET encoding code pages to DOS/OS2 code pages
        return codePage switch
               {
                   // Already DOS/Windows code pages
                   437   => 437, // US
                   850   => 850, // Multilingual Latin I
                   852   => 852, // Latin II (Central/Eastern European)
                   855   => 855, // Cyrillic
                   857   => 857, // Turkish
                   860   => 860, // Portuguese
                   861   => 861, // Icelandic
                   863   => 863, // Canadian French
                   865   => 865, // Nordic
                   866   => 866, // Russian
                   869   => 869, // Modern Greek
                   932   => 932, // Japanese Shift-JIS
                   936   => 936, // Simplified Chinese GBK
                   949   => 949, // Korean
                   950   => 950, // Traditional Chinese Big5
                   1250  => 852, // Windows Central European -> DOS Latin II
                   1251  => 866, // Windows Cyrillic -> DOS Russian
                   1252  => 850, // Windows Western European -> DOS Multilingual
                   1253  => 869, // Windows Greek -> DOS Greek
                   1254  => 857, // Windows Turkish -> DOS Turkish
                   1255  => 862, // Windows Hebrew -> DOS Hebrew
                   1256  => 864, // Windows Arabic -> DOS Arabic
                   1257  => 775, // Windows Baltic -> DOS Baltic
                   28591 => 850, // ISO-8859-1 (Latin 1) -> DOS Multilingual
                   28592 => 852, // ISO-8859-2 (Latin 2) -> DOS Latin II
                   28595 => 866, // ISO-8859-5 (Cyrillic) -> DOS Russian
                   28597 => 869, // ISO-8859-7 (Greek) -> DOS Greek
                   28599 => 857, // ISO-8859-9 (Turkish) -> DOS Turkish
                   65001 => 850, // UTF-8 -> Default to multilingual
                   _     => 850  // Default to code page 850 (Multilingual Latin I)
               };
    }
}