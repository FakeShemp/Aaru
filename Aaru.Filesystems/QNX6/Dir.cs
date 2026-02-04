// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX6
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

            node = new QNX6DirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Resolve the path to get the target entry
        ErrorNumber errno = ResolvePath(normalizedPath, out qnx6_inode_entry targetEntry, out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Check if it's a directory (S_IFDIR = 0x4000)
        if((targetEntry.di_mode & 0xF000) != 0x4000)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenDir: Path is not a directory");

            return ErrorNumber.NotDirectory;
        }

        // Read directory entries from target inode
        errno = ReadDirectoryEntries(targetEntry, out Dictionary<string, qnx6_inode_entry> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading directory entries: {0}", errno);

            return errno;
        }

        // Create directory node with found entries
        node = new QNX6DirNode
        {
            Path     = normalizedPath,
            Position = 0,
            Entries  = entries.Keys.ToArray()
        };

        AaruLogging.Debug(MODULE_NAME,
                          "OpenDir: Successfully opened directory '{0}' with {1} entries",
                          normalizedPath,
                          entries.Count);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not QNX6DirNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not QNX6DirNode qnx6DirNode) return ErrorNumber.InvalidArgument;

        if(qnx6DirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(qnx6DirNode.Position >= qnx6DirNode.Entries.Length) return ErrorNumber.NoError;

        filename = qnx6DirNode.Entries[qnx6DirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a path to its target inode entry</summary>
    /// <param name="path">The path to resolve (must be normalized, not root)</param>
    /// <param name="entry">The resolved inode entry</param>
    /// <param name="parentEntries">The directory entries of the parent directory containing the target</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ResolvePath(string                                   path, out qnx6_inode_entry entry,
                            out Dictionary<string, qnx6_inode_entry> parentEntries)
    {
        entry         = default(qnx6_inode_entry);
        parentEntries = null;

        string pathWithoutLeadingSlash = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "ResolvePath: Traversing path with {0} components", pathComponents.Length);

        // Start from root directory
        Dictionary<string, qnx6_inode_entry> currentEntries = _rootDirectoryCache;
        string                               targetName     = pathComponents[^1];

        // Traverse all but the last component
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            AaruLogging.Debug(MODULE_NAME, "ResolvePath: Navigating to component '{0}'", component);

            if(!currentEntries.TryGetValue(component, out qnx6_inode_entry childEntry))
            {
                AaruLogging.Debug(MODULE_NAME, "ResolvePath: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Check if it's a directory (S_IFDIR = 0x4000)
            if((childEntry.di_mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME, "ResolvePath: Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read the child directory entries
            ErrorNumber errno = ReadDirectoryEntries(childEntry, out Dictionary<string, qnx6_inode_entry> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ResolvePath: Error reading directory entries: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Now look up the target in the current entries
        parentEntries = currentEntries;

        if(!currentEntries.TryGetValue(targetName, out entry))
        {
            AaruLogging.Debug(MODULE_NAME, "ResolvePath: Target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "ResolvePath: Found target '{0}' (mode={1:X4}, size={2})",
                          targetName,
                          entry.di_mode,
                          entry.di_size);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory entries from an inode's data blocks</summary>
    /// <param name="inode">The directory inode entry</param>
    /// <param name="entries">Dictionary of filename to inode entry</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(qnx6_inode_entry inode, out Dictionary<string, qnx6_inode_entry> entries)
    {
        entries = new Dictionary<string, qnx6_inode_entry>();

        if(inode.di_size == 0) return ErrorNumber.NoError;

        // Calculate number of blocks to read
        var  blocksToRead    = (uint)((inode.di_size + _blockSize - 1) / _blockSize);
        uint entriesPerBlock = _blockSize / QNX6_DIR_ENTRY_SIZE;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadDirectoryEntries: Reading {0} blocks ({1} bytes)",
                          blocksToRead,
                          inode.di_size);

        ulong bytesRead = 0;

        for(uint blockOffset = 0; blockOffset < blocksToRead; blockOffset++)
        {
            // Map logical block to physical block using the inode's block pointers
            ErrorNumber errno = MapBlock(inode, blockOffset, out uint physicalBlock);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadDirectoryEntries: Error mapping block {0}: {1}",
                                  blockOffset,
                                  errno);

                bytesRead += _blockSize;

                continue;
            }

            AaruLogging.Debug(MODULE_NAME,
                              "ReadDirectoryEntries: Block {0} maps to physical {1}",
                              blockOffset,
                              physicalBlock);

            // Read the block
            errno = ReadBlock(physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadDirectoryEntries: Error reading block {0}: {1}",
                                  physicalBlock,
                                  errno);

                bytesRead += _blockSize;

                continue;
            }

            // Parse directory entries in this block
            AaruLogging.Debug(MODULE_NAME,
                              "ReadDirectoryEntries: Block data first 64 bytes: {0}",
                              BitConverter.ToString(blockData, 0, Math.Min(64, blockData.Length)));

            for(uint i = 0; i < entriesPerBlock; i++)
            {
                if(bytesRead + i * QNX6_DIR_ENTRY_SIZE >= inode.di_size) break;

                var offset = (int)(i * QNX6_DIR_ENTRY_SIZE);

                // Read the directory entry
                qnx6_dir_entry dirEntry = _littleEndian
                                              ? Marshal.ByteArrayToStructureLittleEndian<qnx6_dir_entry>(blockData,
                                                  offset,
                                                  QNX6_DIR_ENTRY_SIZE)
                                              : Marshal.ByteArrayToStructureBigEndian<qnx6_dir_entry>(blockData,
                                                  offset,
                                                  QNX6_DIR_ENTRY_SIZE);

                AaruLogging.Debug(MODULE_NAME,
                                  "ReadDirectoryEntries: Entry {0} at offset {1}: de_inode={2}, de_size={3}",
                                  i,
                                  offset,
                                  dirEntry.de_inode,
                                  dirEntry.de_size);

                // Check if entry is empty (inode 0 or size 0)
                if(dirEntry.de_inode == 0 || dirEntry.de_size == 0) continue;

                string filename;

                // Check if this is a long filename entry (size > QNX6_SHORT_NAME_MAX)
                if(dirEntry.de_size > QNX6_SHORT_NAME_MAX)
                {
                    // Long filename entry
                    qnx6_long_dir_entry longDirEntry = _littleEndian
                                                           ? Marshal
                                                              .ByteArrayToStructureLittleEndian<
                                                                   qnx6_long_dir_entry>(blockData,
                                                                   offset,
                                                                   QNX6_DIR_ENTRY_SIZE)
                                                           : Marshal
                                                              .ByteArrayToStructureBigEndian<
                                                                   qnx6_long_dir_entry>(blockData,
                                                                   offset,
                                                                   QNX6_DIR_ENTRY_SIZE);

                    // Read the long filename from the longfile tree
                    errno = ReadLongFilename(longDirEntry.de_long_inode, out filename);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "ReadDirectoryEntries: Error reading long filename for inode {0}",
                                          longDirEntry.de_inode);

                        continue;
                    }
                }
                else
                {
                    // Short filename - extract from entry
                    int nameLen = dirEntry.de_size;

                    if(nameLen > QNX6_SHORT_NAME_MAX) nameLen = QNX6_SHORT_NAME_MAX;

                    // Always manually extract filename from block data to avoid marshalling issues
                    // de_fname starts at offset 5 in the directory entry (after 4 bytes inode + 1 byte size)
                    var fnameBytes = new byte[nameLen];
                    Array.Copy(blockData, offset + 5, fnameBytes, 0, nameLen);
                    filename = _encoding.GetString(fnameBytes);

                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadDirectoryEntries: Entry {0} filename='{1}' (bytes: {2})",
                                      i,
                                      filename,
                                      BitConverter.ToString(fnameBytes));
                }

                // Skip "." and ".." entries
                if(string.IsNullOrWhiteSpace(filename) || filename == "." || filename == "..") continue;

                // Read the inode for this entry
                errno = ReadInode(dirEntry.de_inode, out qnx6_inode_entry entryInode);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadDirectoryEntries: Error reading inode {0} for '{1}'",
                                      dirEntry.de_inode,
                                      filename);

                    continue;
                }

                if(!entries.ContainsKey(filename))
                {
                    entries[filename] = entryInode;

                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadDirectoryEntries: Found '{0}' (inode {1})",
                                      filename,
                                      dirEntry.de_inode);
                }
            }

            bytesRead += _blockSize;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a long filename from the longfile tree</summary>
    /// <param name="longInodeNum">The block-level offset into the longfile tree</param>
    /// <param name="filename">The read filename</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadLongFilename(uint longInodeNum, out string filename)
    {
        filename = null;

        // de_long_inode is in block units within the longfile tree data
        // We need to map this to a physical block and offset
        // longInodeNum * blocksize gives us the byte offset into the longfile data
        // The long filename structure is 512 bytes

        // Calculate the logical block number in the longfile tree
        // longInodeNum is the offset in blocks, so we need to find which block contains it
        uint blockIndex    = longInodeNum;
        uint offsetInBlock = 0;

        // Map the logical block to physical block using the longfile tree
        ErrorNumber errno = MapBlock(_superblock.Longfile, blockIndex, out uint physicalBlock);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLongFilename: Error mapping block {0}", blockIndex);

            return errno;
        }

        // Read the block
        errno = ReadBlock(physicalBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLongFilename: Error reading block {0}", physicalBlock);

            return errno;
        }

        // Parse the long filename structure at the beginning of the block
        // The long filename structure starts at the block boundary
        qnx6_long_filename longFilename = _littleEndian
                                              ? Marshal.ByteArrayToStructureLittleEndian<qnx6_long_filename>(blockData,
                                                  (int)offsetInBlock,
                                                  512)
                                              : Marshal.ByteArrayToStructureBigEndian<qnx6_long_filename>(blockData,
                                                  (int)offsetInBlock,
                                                  512);

        int nameLen = longFilename.lf_size;

        if(nameLen > QNX6_LONG_NAME_MAX) nameLen = QNX6_LONG_NAME_MAX;

        // Always manually extract filename from block data to avoid marshalling issues
        // lf_size is 2 bytes at offset 0, lf_fname starts at offset 2
        var fnameBytes = new byte[nameLen];
        Array.Copy(blockData, (int)offsetInBlock + 2, fnameBytes, 0, nameLen);
        filename = _encoding.GetString(fnameBytes);

        return ErrorNumber.NoError;
    }
}