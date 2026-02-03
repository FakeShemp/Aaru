// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class extFS
{
    /// <summary>Reads directory entries from an inode's data blocks</summary>
    /// <param name="inode">The directory inode</param>
    /// <param name="entries">Dictionary of filename to inode number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(ext_inode inode, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>();

        if(inode.i_size == 0) return ErrorNumber.NoError;

        uint blockSize = 1024u << (int)_superblock.s_log_zone_size;
        uint bytesRead = 0;

        // Process all blocks containing directory data
        uint blockNum = 0;

        while(bytesRead < inode.i_size)
        {
            // Map logical block to physical block
            ErrorNumber errno = MapBlock(inode, blockNum, out uint physicalBlock);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error mapping block {0}: {1}", blockNum, errno);
                blockNum++;
                bytesRead += blockSize;

                continue;
            }

            // Sparse block
            if(physicalBlock == 0)
            {
                blockNum++;
                bytesRead += blockSize;

                continue;
            }

            // Read the block
            errno = ReadBlock(physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading block {0}: {1}", physicalBlock, errno);
                blockNum++;
                bytesRead += blockSize;

                continue;
            }

            // Parse directory entries in this block
            uint validBytes = Math.Min(blockSize, inode.i_size - bytesRead);
            ParseDirectoryBlock(blockData, validBytes, entries);

            blockNum++;
            bytesRead += blockSize;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses directory entries from a block's data</summary>
    /// <param name="blockData">The block data</param>
    /// <param name="validBytes">Number of valid bytes in the block</param>
    /// <param name="entries">Dictionary to add entries to</param>
    void ParseDirectoryBlock(byte[] blockData, uint validBytes, Dictionary<string, uint> entries)
    {
        uint offset = 0;

        while(offset < validBytes)
        {
            // Directory entry structure:
            // - 4 bytes: inode number
            // - 2 bytes: record length (rec_len)
            // - 2 bytes: name length (name_len)
            // - N bytes: name

            if(offset + 8 > validBytes) // Minimum entry size
                break;

            var inoNum  = BitConverter.ToUInt32(blockData, (int)offset);
            var recLen  = BitConverter.ToUInt16(blockData, (int)(offset + 4));
            var nameLen = BitConverter.ToUInt16(blockData, (int)(offset + 6));

            // Validate record length (from Linux kernel validation)
            // rec_len must be at least 8 and be a multiple of 8
            if(recLen < 8 || recLen % 8 != 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid record length {0} at offset {1}", recLen, offset);

                break;
            }

            if(offset + recLen > validBytes)
            {
                AaruLogging.Debug(MODULE_NAME, "Record extends beyond valid data at offset {0}", offset);

                break;
            }

            // Validate name length
            if(nameLen > EXT_NAME_LEN || nameLen + 8 > recLen)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Invalid name length {0} at offset {1} (rec_len={2})",
                                  nameLen,
                                  offset,
                                  recLen);

                offset += recLen;

                continue;
            }

            if(inoNum != 0 && nameLen > 0)
            {
                // Extract filename
                var nameBytes = new byte[nameLen];
                Array.Copy(blockData, offset + 8, nameBytes, 0, nameLen);
                string filename = StringHandlers.CToString(nameBytes, _encoding);

                // Skip "." and ".." entries
                if(!string.IsNullOrWhiteSpace(filename) && filename != "." && filename != "..")
                {
                    entries[filename] = inoNum;

                    AaruLogging.Debug(MODULE_NAME, "Directory entry: '{0}' -> inode {1}", filename, inoNum);
                }
            }

            offset += recLen;
        }
    }
}