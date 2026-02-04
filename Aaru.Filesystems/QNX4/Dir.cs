// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX4
{
    /// <summary>Reads directory entries from an inode's data blocks</summary>
    /// <param name="inode">The directory inode entry</param>
    /// <param name="entries">Dictionary of filename to inode entry</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(qnx4_inode_entry inode, out Dictionary<string, qnx4_inode_entry> entries)
    {
        entries = new Dictionary<string, qnx4_inode_entry>();

        if(inode.di_size == 0) return ErrorNumber.NoError;

        // Calculate number of blocks to read
        uint blocksToRead = (inode.di_size + QNX4_BLOCK_SIZE - 1) / QNX4_BLOCK_SIZE;
        uint bytesRead    = 0;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadDirectoryEntries: Reading {0} blocks ({1} bytes)",
                          blocksToRead,
                          inode.di_size);

        for(uint blockOffset = 0; blockOffset < blocksToRead; blockOffset++)
        {
            // Map logical block to physical block
            ErrorNumber errno = MapBlock(inode, blockOffset, out uint physicalBlock);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadDirectoryEntries: Error mapping block {0}", blockOffset);
                bytesRead += QNX4_BLOCK_SIZE;

                continue;
            }

            if(physicalBlock == 0)
            {
                // Sparse block
                bytesRead += QNX4_BLOCK_SIZE;

                continue;
            }

            // Read the block
            errno = ReadBlock(physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadDirectoryEntries: Error reading block {0}", physicalBlock);
                bytesRead += QNX4_BLOCK_SIZE;

                continue;
            }

            // Parse directory entries in this block (8 entries per block)
            for(var i = 0; i < QNX4_INODES_PER_BLOCK; i++)
            {
                if(bytesRead + i * QNX4_DIR_ENTRY_SIZE >= inode.di_size) break;

                int offset = i * QNX4_DIR_ENTRY_SIZE;

                // Check if entry is empty (first byte of name is 0)
                if(blockData[offset] == 0) continue;

                // Get the status byte (last byte of entry)
                byte status = blockData[offset + QNX4_DIR_ENTRY_SIZE - 1];

                // Check if entry is in use
                if((status & QNX4_FILE_USED) == 0 && (status & QNX4_FILE_LINK) == 0) continue;

                string           filename;
                qnx4_inode_entry entry;

                if((status & QNX4_FILE_LINK) != 0)
                {
                    // This is a link entry - has longer filename (48 bytes)
                    qnx4_link_info linkInfo =
                        Marshal.ByteArrayToStructureLittleEndian<qnx4_link_info>(blockData,
                            offset,
                            QNX4_DIR_ENTRY_SIZE);

                    filename = StringHandlers.CToString(linkInfo.dl_fname, _encoding);

                    // Skip "." and ".." entries
                    if(string.IsNullOrWhiteSpace(filename) || filename == "." || filename == "..") continue;

                    // For links, we need to read the actual inode from the referenced location
                    errno = ReadInodeEntry(linkInfo.dl_inode_blk, linkInfo.dl_inode_ndx, out entry);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "ReadDirectoryEntries: Error reading linked inode for '{0}'",
                                          filename);

                        continue;
                    }
                }
                else
                {
                    // Regular inode entry - has shorter filename (16 bytes)
                    entry = Marshal.ByteArrayToStructureLittleEndian<qnx4_inode_entry>(blockData,
                        offset,
                        QNX4_DIR_ENTRY_SIZE);

                    filename = StringHandlers.CToString(entry.di_fname, _encoding);

                    // Skip "." and ".." entries
                    if(string.IsNullOrWhiteSpace(filename) || filename == "." || filename == "..") continue;
                }

                if(!string.IsNullOrWhiteSpace(filename) && !entries.ContainsKey(filename))
                {
                    entries[filename] = entry;
                    AaruLogging.Debug(MODULE_NAME, "ReadDirectoryEntries: Found '{0}'", filename);
                }
            }

            bytesRead += QNX4_BLOCK_SIZE;
        }


        return ErrorNumber.NoError;
    }
}