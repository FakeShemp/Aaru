// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
{
    /// <summary>Reads and parses directory contents</summary>
    /// <param name="startBlock">Start block of the directory data</param>
    /// <param name="offset">Offset within the block</param>
    /// <param name="size">Total size of the directory data</param>
    /// <param name="cache">Dictionary to cache the entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(uint                                   startBlock, uint offset, uint size,
                                      Dictionary<string, DirectoryEntryInfo> cache)
    {
        if(size <= 3) // Empty directory (size includes "." and ".." overhead)
        {
            AaruLogging.Debug(MODULE_NAME, "Empty directory");

            return ErrorNumber.NoError;
        }

        // Calculate absolute position
        ulong blockPosition = _superBlock.directory_table_start + startBlock;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading directory: block=0x{0:X16}, offset={1}, size={2}",
                          blockPosition,
                          offset,
                          size);

        // Read the metadata block
        ErrorNumber errno = ReadMetadataBlock(blockPosition, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory metadata block: {0}", errno);

            return errno;
        }

        if(dirData == null || dirData.Length <= offset)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid directory data");

            return ErrorNumber.InvalidArgument;
        }

        // Parse directory entries
        uint currentOffset = offset;
        uint bytesRead     = 0;

        // The size field in directory inodes is 3 greater than the real size
        // to account for the "." and ".." entries that are not stored
        uint realSize = size - 3;

        while(bytesRead < realSize && currentOffset < dirData.Length)
        {
            // Read directory header
            if(currentOffset + Marshal.SizeOf<DirHeader>() > dirData.Length) break;

            var headerData = new byte[Marshal.SizeOf<DirHeader>()];
            Array.Copy(dirData, currentOffset, headerData, 0, headerData.Length);

            DirHeader header = _littleEndian
                                   ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DirHeader>(headerData)
                                   : Helpers.Marshal.ByteArrayToStructureBigEndian<DirHeader>(headerData);

            currentOffset += (uint)headerData.Length;
            bytesRead     += (uint)headerData.Length;

            // count is stored as count-1
            uint entryCount = header.count + 1;

            if(entryCount > SQUASHFS_DIR_COUNT)
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid directory entry count: {0}", entryCount);

                break;
            }

            AaruLogging.Debug(MODULE_NAME,
                              "Directory header: count={0}, start_block={1}, inode_number={2}",
                              entryCount,
                              header.start_block,
                              header.inode_number);

            // Read entries for this header
            for(uint i = 0; i < entryCount && bytesRead < realSize; i++)
            {
                if(currentOffset + Marshal.SizeOf<DirEntry>() > dirData.Length) break;

                var entryData = new byte[Marshal.SizeOf<DirEntry>()];
                Array.Copy(dirData, currentOffset, entryData, 0, entryData.Length);

                DirEntry entry = _littleEndian
                                     ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DirEntry>(entryData)
                                     : Helpers.Marshal.ByteArrayToStructureBigEndian<DirEntry>(entryData);

                currentOffset += (uint)entryData.Length;
                bytesRead     += (uint)entryData.Length;

                // size is stored as size-1
                var nameSize = (uint)(entry.size + 1);

                if(nameSize > SQUASHFS_NAME_LEN || currentOffset + nameSize > dirData.Length)
                {
                    AaruLogging.Debug(MODULE_NAME, "Invalid entry name size: {0}", nameSize);

                    break;
                }

                var nameData = new byte[nameSize];
                Array.Copy(dirData, currentOffset, nameData, 0, nameSize);
                string name = _encoding.GetString(nameData).TrimEnd('\0');

                currentOffset += nameSize;
                bytesRead     += nameSize;

                // Calculate the actual inode number
                // The entry's inode_number is a signed offset from the header's inode_number
                var inodeNumber = (uint)((int)header.inode_number + entry.inode_number);

                var entryInfo = new DirectoryEntryInfo
                {
                    Name        = name,
                    InodeNumber = inodeNumber,
                    Type        = (SquashInodeType)entry.type,
                    InodeBlock  = header.start_block,
                    InodeOffset = entry.offset
                };

                if(cache.TryAdd(name, entryInfo))
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Cached entry: {0} -> inode {1}, type {2}",
                                      name,
                                      inodeNumber,
                                      (SquashInodeType)entry.type);
                }
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Directory read complete, {0} entries cached", cache.Count);

        return ErrorNumber.NoError;
    }
}