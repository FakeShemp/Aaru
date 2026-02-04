// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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

/// <inheritdoc />
public sealed partial class AtheOS
{
    /// <summary>Parses a directory's B+tree and returns all entries</summary>
    /// <param name="dataStream">The data stream of the directory inode</param>
    /// <param name="entries">Output dictionary of filename to inode number mappings</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ParseDirectoryBTree(DataStream dataStream, out Dictionary<string, long> entries)
    {
        entries = new Dictionary<string, long>();

        // Read B+tree header from the start of the data stream
        ErrorNumber errno = ReadFromDataStream(dataStream, 0, 32, out byte[] headerData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading B+tree header: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "B+tree header raw bytes: {0}",
                          BitConverter.ToString(headerData, 0, Math.Min(32, headerData.Length)));

        BTreeHeader btHeader = Marshal.ByteArrayToStructureLittleEndian<BTreeHeader>(headerData);

        AaruLogging.Debug(MODULE_NAME,
                          "B+tree header: magic=0x{0:X8}, root={1}, depth={2}, last_node={3}, first_free={4}",
                          btHeader.magic,
                          btHeader.root,
                          btHeader.tree_depth,
                          btHeader.last_node,
                          btHeader.first_free);

        if(btHeader.magic != BTREE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid B+tree magic! Expected 0x{0:X8}, got 0x{1:X8}",
                              BTREE_MAGIC,
                              btHeader.magic);

            return ErrorNumber.InvalidArgument;
        }

        // Traverse the B+tree to collect all entries
        // Start from the root and descend to the leftmost leaf node
        long nodeOffset = btHeader.root;

        AaruLogging.Debug(MODULE_NAME, "Starting B+tree traversal from root at offset {0}", nodeOffset);

        // First, descend to the leftmost leaf node
        for(var depth = 0; depth < btHeader.tree_depth - 1; depth++)
        {
            errno = ReadFromDataStream(dataStream, nodeOffset, B_NODE_SIZE, out byte[] nodeData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading B+tree node at depth {0}: {1}", depth, errno);

                return errno;
            }

            BNode node = Marshal.ByteArrayToStructureLittleEndian<BNode>(nodeData);

            AaruLogging.Debug(MODULE_NAME,
                              "Interior node at depth {0}: key_count={1}, left={2}, right={3}, overflow={4}",
                              depth,
                              node.key_count,
                              node.left,
                              node.right,
                              node.overflow);

            if(node.key_count == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Interior node has no keys");

                return ErrorNumber.InvalidArgument;
            }

            // Get the first value (pointer to leftmost child)
            // Values are stored after the header, keys, and key indices
            // Header size = 32 bytes (BNode structure: 8+8+8+4+4 = 32)
            const int headerSize    = 32;
            int       keyDataEnd    = headerSize + node.total_key_size;
            int       keyIndexStart = keyDataEnd + 3 & ~3; // Align to 4 bytes
            int       valueStart    = keyIndexStart + node.key_count * 2;

            // Get first child pointer
            var firstChild = BitConverter.ToInt64(nodeData, valueStart);

            AaruLogging.Debug(MODULE_NAME, "Descending to child node at offset {0}", firstChild);
            nodeOffset = firstChild;
        }

        // Now we're at a leaf node level - traverse all leaf nodes via right sibling links
        while(nodeOffset != 0 && nodeOffset != NULL_VAL)
        {
            errno = ReadFromDataStream(dataStream, nodeOffset, B_NODE_SIZE, out byte[] nodeData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading leaf node: {0}", errno);

                return errno;
            }

            BNode node = Marshal.ByteArrayToStructureLittleEndian<BNode>(nodeData);

            AaruLogging.Debug(MODULE_NAME,
                              "Leaf node: key_count={0}, total_key_size={1}, left={2}, right={3}, overflow={4}",
                              node.key_count,
                              node.total_key_size,
                              node.left,
                              node.right,
                              node.overflow);

            // Parse all entries in this leaf node
            // Node layout:
            // - Header (32 bytes): left, right, overflow, key_count, total_key_size
            // - Key data (variable)
            // - Key indices (array of int16, padded to 4-byte boundary)
            // - Values (array of int64)

            const int headerSize    = 32;
            int       keyDataEnd    = headerSize + node.total_key_size;
            int       keyIndexStart = keyDataEnd + 3 & ~3; // Align to 4 bytes
            int       valueStart    = keyIndexStart + node.key_count * 2;

            AaruLogging.Debug(MODULE_NAME,
                              "Leaf node layout: keyDataStart={0}, keyDataEnd={1}, keyIndexStart={2}, valueStart={3}",
                              headerSize,
                              keyDataEnd,
                              keyIndexStart,
                              valueStart);

            var keyOffset = 0;

            for(var i = 0; i < node.key_count; i++)
            {
                // Read key end offset from index array
                int keyIndexOffset = keyIndexStart + i * 2;
                var keyEndOffset   = BitConverter.ToUInt16(nodeData, keyIndexOffset);

                int keyStart  = headerSize   + keyOffset;
                int keyLength = keyEndOffset - keyOffset;

                if(keyLength <= 0 || keyLength > B_MAX_KEY_SIZE)
                {
                    AaruLogging.Debug(MODULE_NAME, "Invalid key length at index {0}: {1}", i, keyLength);

                    break;
                }

                // Read value (inode number)
                int valueOffset = valueStart + i * 8;
                var inodeNumber = BitConverter.ToInt64(nodeData, valueOffset);

                // Extract key name
                string keyName = _encoding.GetString(nodeData, keyStart, keyLength);

                AaruLogging.Debug(MODULE_NAME,
                                  "Entry {0}: name='{1}' ({2} bytes), inode={3}",
                                  i,
                                  keyName,
                                  keyLength,
                                  inodeNumber);

                // Skip "." and ".." entries
                if(keyName != "." && keyName != ".." && !entries.ContainsKey(keyName)) entries[keyName] = inodeNumber;

                keyOffset = keyEndOffset;
            }

            // Move to right sibling
            if(node.right != 0 && node.right != NULL_VAL)
                nodeOffset = node.right;
            else
                break;
        }

        return ErrorNumber.NoError;
    }
}