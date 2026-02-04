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

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX4
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

            node = new QNX4DirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Resolve the path to get the target entry
        ErrorNumber errno = ResolvePath(normalizedPath, out qnx4_inode_entry targetEntry, out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Check if it's a directory (S_IFDIR = 0x4000)
        if((targetEntry.di_mode & 0xF000) != 0x4000)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenDir: Path is not a directory");

            return ErrorNumber.NotDirectory;
        }

        // Read directory entries from target inode
        errno = ReadDirectoryEntries(targetEntry, out Dictionary<string, qnx4_inode_entry> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading directory entries: {0}", errno);

            return errno;
        }

        // Create directory node with found entries
        node = new QNX4DirNode
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
        if(node is not QNX4DirNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not QNX4DirNode qnx4DirNode) return ErrorNumber.InvalidArgument;

        if(qnx4DirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(qnx4DirNode.Position >= qnx4DirNode.Entries.Length) return ErrorNumber.NoError;

        filename = qnx4DirNode.Entries[qnx4DirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a path to its target inode entry</summary>
    /// <param name="path">The path to resolve (must be normalized, not root)</param>
    /// <param name="entry">The resolved inode entry</param>
    /// <param name="parentEntries">The directory entries of the parent directory containing the target</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ResolvePath(string                                   path, out qnx4_inode_entry entry,
                            out Dictionary<string, qnx4_inode_entry> parentEntries)
    {
        entry         = default(qnx4_inode_entry);
        parentEntries = null;

        string pathWithoutLeadingSlash = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] pathComponents = pathWithoutLeadingSlash.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "ResolvePath: Traversing path with {0} components", pathComponents.Length);

        // Start from root directory
        Dictionary<string, qnx4_inode_entry> currentEntries = _rootDirectoryCache;
        string                               targetName     = pathComponents[^1];

        // Traverse all but the last component
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            AaruLogging.Debug(MODULE_NAME, "ResolvePath: Navigating to component '{0}'", component);

            if(!currentEntries.TryGetValue(component, out qnx4_inode_entry childEntry))
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

            ErrorNumber errno = ReadDirectoryEntries(childEntry, out Dictionary<string, qnx4_inode_entry> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ResolvePath: Error reading child directory: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Find the target in the current directory
        if(!currentEntries.TryGetValue(targetName, out entry))
        {
            AaruLogging.Debug(MODULE_NAME, "ResolvePath: Target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        parentEntries = currentEntries;

        return ErrorNumber.NoError;
    }

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