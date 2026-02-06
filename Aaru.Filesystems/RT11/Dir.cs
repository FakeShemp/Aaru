// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
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

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
public sealed partial class RT11
{
    /// <summary>Parses a directory segment and extracts file entries</summary>
    /// <param name="segmentData">Directory segment data (1024 bytes)</param>
    /// <param name="header">Directory segment header</param>
    /// <param name="entries">Output list of entries (filename, start block)</param>
    /// <returns>Error code</returns>
    ErrorNumber ParseDirectorySegment(byte[] segmentData, DirectorySegmentHeader header,
                                      out List<(string filename, uint startBlock)> entries)
    {
        entries = [];

        // Directory entries start after the 5-word header (10 bytes)
        var offset          = 10;
        int entrySize       = DIRECTORY_ENTRY_WORDS * 2 + header.extraBytesPerEntry; // 14 bytes + extra
        var currentBlockNum = (uint)header.dataBlockStart;

        while(offset + entrySize <= segmentData.Length)
        {
            // Read directory entry
            DirectoryEntry entry =
                Marshal.PtrToStructure<DirectoryEntry>(Marshal.UnsafeAddrOfPinnedArrayElement(segmentData, offset));

            // Check for end-of-segment marker
            if((entry.status & 0xFF00) == E_EOS)
            {
                AaruLogging.Debug(MODULE_NAME, $"ParseDirectorySegment: End of segment at offset {offset}");

                break;
            }

            switch(entry.status & 0xFF00)
            {
                // Process permanent files only
                case E_PERM:
                {
                    // Decode Radix-50 filename
                    string filename = DecodeRadix50Filename(entry.filename1, entry.filename2, entry.filetype);

                    if(!string.IsNullOrEmpty(filename))
                    {
                        entries.Add((filename, currentBlockNum));

                        AaruLogging.Debug(MODULE_NAME,
                                          $"ParseDirectorySegment: Found file '{filename}' at block {currentBlockNum}, length {entry.length}");
                    }

                    currentBlockNum += entry.length;

                    break;
                }
                case E_TENT:
                    // Tentative file - skip
                    AaruLogging.Debug(MODULE_NAME,
                                      $"ParseDirectorySegment: Skipping tentative file at offset {offset}");

                    currentBlockNum += entry.length;

                    break;
                case E_MPTY:
                    // Empty area - just advance block counter
                    AaruLogging.Debug(MODULE_NAME, $"ParseDirectorySegment: Empty area, length {entry.length}");
                    currentBlockNum += entry.length;

                    break;
            }

            offset += entrySize;
        }

        return ErrorNumber.NoError;
    }
}