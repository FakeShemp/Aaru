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
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class extFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new ExtDirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory traversal
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "Traversing path with {0} components", pathComponents.Length);

        // Start from root directory
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        foreach(string component in pathComponents)
        {
            AaruLogging.Debug(MODULE_NAME, "Navigating to component '{0}'", component);

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out uint childInodeNum))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' not found in directory", component);

                return ErrorNumber.NoSuchFile;
            }

            // Read the child inode
            ErrorNumber errno = ReadInode(childInodeNum, out ext_inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child inode: {0}", errno);

                return errno;
            }

            // Check if it's a directory (S_IFDIR = 0x4000)
            if((childInode.i_mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory entries from child inode
            errno = ReadDirectoryEntries(childInode, out Dictionary<string, uint> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child directory entries: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Create directory node with found entries
        node = new ExtDirNode
        {
            Path     = normalizedPath,
            Position = 0,
            Entries  = currentEntries.Keys.ToArray()
        };

        AaruLogging.Debug(MODULE_NAME,
                          "Successfully opened directory '{0}' with {1} entries",
                          normalizedPath,
                          currentEntries.Count);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not ExtDirNode extDirNode) return ErrorNumber.InvalidArgument;

        extDirNode.Position = -1;
        extDirNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not ExtDirNode extDirNode) return ErrorNumber.InvalidArgument;

        if(extDirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(extDirNode.Position >= extDirNode.Entries.Length) return ErrorNumber.NoError;

        filename = extDirNode.Entries[extDirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory entries from an inode's data blocks</summary>
    /// <param name="inode">The directory inode</param>
    /// <param name="entries">Dictionary of filename to inode number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(ext_inode inode, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>();

        if(inode.i_size == 0) return ErrorNumber.NoError;

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
                bytesRead += EXT_BLOCK_SIZE;

                continue;
            }

            // Sparse block
            if(physicalBlock == 0)
            {
                blockNum++;
                bytesRead += EXT_BLOCK_SIZE;

                continue;
            }

            // Read the block
            errno = ReadBlock(physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading block {0}: {1}", physicalBlock, errno);
                blockNum++;
                bytesRead += EXT_BLOCK_SIZE;

                continue;
            }

            // Parse directory entries in this block
            uint validBytes = Math.Min(EXT_BLOCK_SIZE, inode.i_size - bytesRead);
            ParseDirectoryBlock(blockData, validBytes, entries);

            blockNum++;
            bytesRead += EXT_BLOCK_SIZE;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses directory entries from a block's data</summary>
    /// <param name="blockData">The block data</param>
    /// <param name="validBytes">Number of valid bytes in the block</param>
    /// <param name="entries">Dictionary to add entries to</param>
    void ParseDirectoryBlock(byte[] blockData, uint validBytes, Dictionary<string, uint> entries)
    {
        // Minimum directory entry is 8 bytes (inode + rec_len + name_len) + at least 1 byte name
        const int minEntrySize = 8;
        uint offset = 0;

        while(offset < validBytes)
        {
            if(offset + minEntrySize > validBytes)
                break;

            // Read the fixed header fields manually since entries are variable-length on disk
            uint   inoNum  = BitConverter.ToUInt32(blockData, (int)offset);
            ushort recLen  = BitConverter.ToUInt16(blockData, (int)(offset + 4));
            ushort nameLen = BitConverter.ToUInt16(blockData, (int)(offset + 6));

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
                // Extract filename (starts at offset + 8)
                var nameBytes = new byte[nameLen];
                Array.Copy(blockData, (int)(offset + 8), nameBytes, 0, nameLen);
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