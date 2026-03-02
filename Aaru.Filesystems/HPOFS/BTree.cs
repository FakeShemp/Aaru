// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     B-tree directory node parsing for the High Performance Optical File System.
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
using Aaru.Helpers;

namespace Aaru.Filesystems;

public sealed partial class HPOFS
{
    /// <summary>Parses directory entries from a leaf INDX node's raw sector data</summary>
    /// <remarks>
    ///     Each INDX entry has the format:
    ///     [4] sectorAddress (uint32 big-endian)
    ///     [2] entryDataSize (uint16 big-endian, bytes of entry data NOT including this or sectorAddr or reserved)
    ///     [2] reserved (zeros, NOT counted in entryDataSize)
    ///     [4] timestamp (uint32 big-endian)
    ///     [N] name (entryDataSize - 4 bytes, null-padded)
    ///     Stride per entry = 8 + entryDataSize
    /// </remarks>
    List<(string fullPath, uint sectorAddr, uint timestamp)> ParseLeafEntries(byte[] nodeData)
    {
        List<(string fullPath, uint sectorAddr, uint timestamp)> entries = new();
        var                                                      offset  = 0x24;

        while(offset + 8 <= nodeData.Length)
        {
            // Read sector address (4 bytes, big-endian)
            var sectorAddr = BigEndianBitConverter.ToUInt32(nodeData, offset);

            // Read entry data size (2 bytes, big-endian)
            // Counts bytes of entry data AFTER the 2-byte reserved field
            var entryDataSize = BigEndianBitConverter.ToUInt16(nodeData, offset + 4);

            // End of entries: zero size or too small to hold timestamp
            if(entryDataSize < 4) break;

            // Bounds check: full entry must fit within the buffer
            // Entry = 4 (sectorAddr) + 2 (entryDataSize) + 2 (reserved) + entryDataSize (data)
            if(offset + 8 + entryDataSize > nodeData.Length) break;

            // Skip reserved (2 bytes at offset+6) and read timestamp (4 bytes at offset+8)
            var timestamp = BigEndianBitConverter.ToUInt32(nodeData, offset + 8);

            // Read name: entryDataSize - 4 bytes starting at offset + 12
            // (entryDataSize includes: 4 timestamp + N name)
            int nameLength = entryDataSize - 4;

            if(nameLength > 0 && nameLength <= MAX_FILENAME_LENGTH)
            {
                string name = _encoding.GetString(nodeData, offset + 12, nameLength).TrimEnd('\0');

                if(!string.IsNullOrWhiteSpace(name) && sectorAddr != 0) entries.Add((name, sectorAddr, timestamp));
            }

            // Advance to next entry: stride = 4 + 2 + 2 + entryDataSize = 8 + entryDataSize
            offset += 8 + entryDataSize;
        }

        return entries;
    }
}