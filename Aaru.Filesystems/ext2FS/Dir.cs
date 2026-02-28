// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
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

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    /// <summary>Reads directory entries from an inode's data blocks</summary>
    /// <param name="inode">The directory inode</param>
    /// <param name="size">The directory size in bytes</param>
    /// <param name="entries">Dictionary of filename to inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(Inode inode, ulong size, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>(StringComparer.Ordinal);

        // Get all data blocks for the inode
        ErrorNumber errno = GetInodeDataBlocks(inode, out List<(ulong physicalBlock, uint length)> blockList);

        if(errno != ErrorNumber.NoError) return errno;

        ulong bytesRead = 0;

        foreach((ulong physicalBlock, uint length) in blockList)
        {
            if(bytesRead >= size) break;

            // Read the data blocks
            var bytesToRead = (uint)Math.Min(length * _blockSize, size - bytesRead);

            errno = ReadBytes(physicalBlock * _blockSize, bytesToRead, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading directory block at {0}: {1}", physicalBlock, errno);

                bytesRead += length * _blockSize;

                continue;
            }

            // Parse directory entries within this data
            ParseDirectoryBlock(blockData, bytesToRead, entries);

            bytesRead += length * _blockSize;
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

        while(offset + 8 <= validBytes && offset + 8 <= blockData.Length)
        {
            // Read the fixed header fields manually since entries are variable-length
            var inodeNum = BitConverter.ToUInt32(blockData, (int)offset);
            var recLen   = BitConverter.ToUInt16(blockData, (int)(offset + 4));

            // Validate record length
            // rec_len must be at least 12 (8 bytes header + minimum 4 bytes for alignment)
            // and be a multiple of 4
            if(recLen < 12 || recLen % 4 != 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid record length {0} at offset {1}", recLen, offset);

                break;
            }

            if(offset + recLen > validBytes || offset + recLen > blockData.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Record extends beyond valid data at offset {0}", offset);

                break;
            }

            // Handle ext4_rec_len_from_disk encoding for large blocks:
            // If recLen == 0xFFFF or recLen == 0, it means "rest of block"
            uint actualRecLen = recLen;

            if(recLen == 0xFFFF || recLen == 0)
                actualRecLen = _blockSize - offset % _blockSize;
            else if(_blockSize > 65536)
            {
                // Low 2 bits encode high 2 bits of length
                actualRecLen = (uint)(recLen & 0xFFFC | (recLen & 3) << 16);
            }

            byte nameLen;

            if(_hasFileType)
            {
                // DirectoryEntry2: name_len is 1 byte at offset 6, file_type is 1 byte at offset 7
                nameLen = blockData[offset + 6];
            }
            else
            {
                // DirectoryEntry: name_len is 2 bytes at offset 6
                nameLen = (byte)Math.Min(BitConverter.ToUInt16(blockData, (int)(offset + 6)), (ushort)255);
            }

            // Validate name length
            if(nameLen > 0 && nameLen + 8 <= actualRecLen && inodeNum != 0)
            {
                if(offset + 8 + nameLen <= blockData.Length)
                {
                    var nameBytes = new byte[nameLen];
                    Array.Copy(blockData, (int)(offset + 8), nameBytes, 0, nameLen);
                    string filename = StringHandlers.CToString(nameBytes, _encoding);

                    if(!string.IsNullOrWhiteSpace(filename)) entries[filename] = inodeNum;
                }
            }

            offset += actualRecLen;
        }
    }
}