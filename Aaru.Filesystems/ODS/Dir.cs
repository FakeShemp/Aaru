// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Directory operations for the Files-11 On-Disk Structure.
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
using Aaru.Helpers;

namespace Aaru.Filesystems;

public sealed partial class ODS
{
    /// <summary>Parses directory entries from a directory block.</summary>
    /// <param name="block">Directory block data.</param>
    void ParseDirectoryBlock(byte[] block)
    {
        var offset = 0;

        while(offset < block.Length - 2)
        {
            // Check for end of records marker
            var size = BitConverter.ToUInt16(block, offset);

            if(size is NO_MORE_RECORDS or 0) break;

            // Ensure we have enough data for the record header
            if(offset + 6 > block.Length) break;

            byte flags     = block[offset + 4];
            byte namecount = block[offset + 5];

            // Extract name type from flags
            var nameType = (DirectoryNameType)(flags >> 3 & 0x07);

            // Read filename
            int nameOffset = offset + 6;

            if(nameOffset + namecount > block.Length) break;

            string filename = nameType == DirectoryNameType.Ucs2
                                  ? Encoding.Unicode.GetString(block, nameOffset, namecount)
                                  : _encoding.GetString(block, nameOffset, namecount);

            // Value field (directory entries) starts after name, word-aligned
            int valueOffset = nameOffset + (namecount + 1 & ~1);

            // Read directory entry (first version)
            if(valueOffset + 8 <= block.Length)
            {
                var entryVersion = BitConverter.ToUInt16(block, valueOffset);

                FileId fid = Marshal.ByteArrayToStructureLittleEndian<FileId>(block, valueOffset + 2, 6);

                // Create filename with version (ODS style: FILENAME.EXT;VERSION)
                var fullName = $"{filename};{entryVersion}";

                // Also store without version for easier lookup
                if(!_rootDirectoryCache.ContainsKey(filename.ToUpperInvariant()))
                {
                    _rootDirectoryCache[filename.ToUpperInvariant()] = new CachedFile
                    {
                        Fid     = fid,
                        Version = entryVersion
                    };
                }

                // Store with version too
                _rootDirectoryCache[fullName.ToUpperInvariant()] = new CachedFile
                {
                    Fid     = fid,
                    Version = entryVersion
                };
            }

            // Move to next record
            offset += size + 2; // size doesn't include the size field itself
        }
    }
}