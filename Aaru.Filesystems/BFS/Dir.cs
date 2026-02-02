// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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

// Information from Practical Filesystem Design, ISBN 1-55860-497-9
/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
public sealed partial class BeFS
{
    /// <summary>Opens a directory for enumeration</summary>
    /// <remarks>
    ///     Opens the specified directory path for enumeration. Traverses the directory tree
    ///     by reading i-nodes and B+tree structures to find subdirectories.
    ///     Returns a directory node containing all entries in the target directory.
    /// </remarks>
    /// <param name="path">Directory path to open (e.g., "/", "/home", "/home/user")</param>
    /// <param name="node">Output directory node for enumeration</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize the path
        string normalizedPath                                            = path ?? "/";
        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory - return cached entries
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new BefsDirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory traversal
        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "Traversing path with {0} components", pathComponents.Length);

        // Start from root directory cache
        Dictionary<string, long> currentEntries = _rootDirectoryCache;
        bfs_inode                currentInode   = default;
        var                      useRootInode   = true;

        // Traverse each path component
        foreach(string component in pathComponents)
        {
            AaruLogging.Debug(MODULE_NAME, "Navigating to component '{0}'", component);

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out long childInodeAddr))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' not found in directory", component);

                return ErrorNumber.NoSuchFile;
            }

            // Convert i-node address to block_run
            // I-node address is stored as a block_run where allocation_group and start define the location
            var ag          = (uint)(childInodeAddr >> 32);
            var blockOffset = (uint)(childInodeAddr & 0xFFFFFFFF);

            var childInodeBlockRun = new block_run
            {
                allocation_group = ag,
                start            = (ushort)blockOffset,
                len              = 1
            };

            AaruLogging.Debug(MODULE_NAME, "Reading i-node at AG {0}, block {1}", ag, blockOffset);

            // Read the child i-node
            ErrorNumber errno = ReadInode(childInodeBlockRun, out bfs_inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child i-node: {0}", errno);

                return errno;
            }

            // Check if it's a directory
            if(!IsDirectory(childInode))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Parse the child directory's B+tree
            errno = ParseDirectoryBTree(childInode.data, out Dictionary<string, long> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error parsing child directory B+tree: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
            currentInode   = childInode;
            useRootInode   = false;
        }

        // Create directory node with the entries we found
        var dirNode = new BefsDirNode
        {
            Path     = normalizedPath,
            Position = 0,
            Entries  = currentEntries.Keys.ToArray()
        };

        node = dirNode;

        AaruLogging.Debug(MODULE_NAME,
                          "Successfully opened directory '{0}' with {1} entries",
                          normalizedPath,
                          dirNode.Entries.Length);

        return ErrorNumber.NoError;
    }

    /// <summary>Closes a directory enumeration</summary>
    /// <remarks>
    ///     Closes the directory node and cleans up resources used during enumeration.
    ///     Resets the position to mark the node as closed.
    /// </remarks>
    /// <param name="node">The directory node to close</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not BefsDirNode befsDirNode) return ErrorNumber.InvalidArgument;

        befsDirNode.Position = -1;
        befsDirNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the next entry from a directory enumeration</summary>
    /// <remarks>
    ///     Returns entries from the directory in the order they appear in the B+tree.
    ///     When all entries have been read, returns NoError with filename = null,
    ///     indicating end of directory.
    /// </remarks>
    /// <param name="node">The directory node to read from</param>
    /// <param name="filename">Output filename of the next entry</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not BefsDirNode befsDirNode) return ErrorNumber.InvalidArgument;

        if(befsDirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(befsDirNode.Position >= befsDirNode.Entries.Length) return ErrorNumber.NoError;

        filename = befsDirNode.Entries[befsDirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Checks if an i-node represents a directory</summary>
    private bool IsDirectory(bfs_inode inode) =>

        // In BeFS, the mode field contains Unix-style permission bits
        // S_IFDIR = 0x4000
        (inode.mode & 0xF000) == 0x4000;

    /// <summary>Parses a directory's B+tree and returns all entries</summary>
    private ErrorNumber ParseDirectoryBTree(data_stream dataStream, out Dictionary<string, long> entries)
    {
        entries = [];

        // Read B+tree header
        ErrorNumber errno = ReadFromDataStream(dataStream, 0, 32, out byte[] headerData);

        if(errno != ErrorNumber.NoError) return errno;

        bt_header btHeader = _littleEndian
                                 ? Marshal.ByteArrayToStructureLittleEndian<bt_header>(headerData)
                                 : Marshal.ByteArrayToStructureBigEndian<bt_header>(headerData);

        if(btHeader.magic != BTREE_MAGIC) return ErrorNumber.InvalidArgument;

        // Traverse B+tree to collect all entries
        const long BEFS_BT_INVAL = unchecked((long)0xFFFFFFFFFFFFFFFF);
        long       nodeOffset    = btHeader.node_root_pointer;

        while(nodeOffset != 0 && nodeOffset != BEFS_BT_INVAL)
        {
            errno = ReadFromDataStream(dataStream, nodeOffset, btHeader.node_size, out byte[] nodeData);

            if(errno != ErrorNumber.NoError) return errno;

            bt_node_hdr nodeHeader = _littleEndian
                                         ? Marshal.ByteArrayToStructureLittleEndian<bt_node_hdr>(nodeData)
                                         : Marshal.ByteArrayToStructureBigEndian<bt_node_hdr>(nodeData);

            bool isLeafNode = nodeHeader.overflow_link == BEFS_BT_INVAL;

            if(isLeafNode)
            {
                // Parse leaf node entries
                const int headerSize          = 28;
                int       keyDataStart        = headerSize;
                int       keyDataEnd          = keyDataStart + nodeHeader.keys_length;
                int       keyLengthIndexStart = keyDataEnd + 3 & ~3;
                int       valueDataStart      = keyLengthIndexStart + nodeHeader.node_keys * 2;

                var keyOffset = 0;

                for(var i = 0; i < nodeHeader.node_keys; i++)
                {
                    int offsetIndex = keyLengthIndexStart + i * 2;

                    ushort keyEndOffset = _littleEndian
                                              ? BitConverter.ToUInt16(nodeData, offsetIndex)
                                              : BigEndianBitConverter.ToUInt16(nodeData, offsetIndex);

                    int keyStart  = keyDataStart + keyOffset;
                    int keyLength = keyEndOffset - keyOffset;

                    int valueIndex = valueDataStart + i * 8;

                    long inodeNumber = _littleEndian
                                           ? BitConverter.ToInt64(nodeData, valueIndex)
                                           : BigEndianBitConverter.ToInt64(nodeData, valueIndex);

                    string keyName = _encoding.GetString(nodeData, keyStart, keyLength);

                    if(!entries.ContainsKey(keyName)) keyOffset = keyEndOffset;
                }
            }

            // Move to next sibling
            if(nodeHeader.right_link != 0 && nodeHeader.right_link != BEFS_BT_INVAL)
                nodeOffset = nodeHeader.right_link;
            else
                break;
        }

        return ErrorNumber.NoError;
    }
}