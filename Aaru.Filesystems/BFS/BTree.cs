// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Directory.cs
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
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
public sealed partial class BeFS
{
    /// <summary>Parses a directory's B+tree and returns all entries</summary>
    private ErrorNumber ParseDirectoryBTree(data_stream dataStream, out Dictionary<string, long> entries)
    {
        entries = [];

        // Traverse B+tree to collect all entries
        const long BEFS_BT_INVAL = unchecked((long)0xFFFFFFFFFFFFFFFF);

        // Read B+tree header (should be at the start of the first direct block)
        ErrorNumber errno = ReadFromDataStream(dataStream, 0, 32, out byte[] headerData);

        if(errno != ErrorNumber.NoError) return errno;

        AaruLogging.Debug(MODULE_NAME,
                          "B+tree header raw bytes: {0}",
                          BitConverter.ToString(headerData, 0, Math.Min(32, headerData.Length)));

        bt_header btHeader = _littleEndian
                                 ? Marshal.ByteArrayToStructureLittleEndian<bt_header>(headerData)
                                 : Marshal.ByteArrayToStructureBigEndian<bt_header>(headerData);

        AaruLogging.Debug(MODULE_NAME,
                          "B+tree header: magic=0x{0:X16}, node_size={1}, root_ptr={2}, data_type={3}",
                          btHeader.magic,
                          btHeader.node_size,
                          btHeader.node_root_pointer,
                          btHeader.data_type);

        if(btHeader.magic != BTREE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid B+tree magic! Expected 0x{0:X16}, got 0x{1:X16}",
                              BTREE_MAGIC,
                              btHeader.magic);

            return ErrorNumber.InvalidArgument;
        }

        // Traverse B+tree to collect all entries
        long nodeOffset = btHeader.node_root_pointer;

        AaruLogging.Debug(MODULE_NAME,
                          "ParseDirectoryBTree: Starting traversal from root node at offset {0}",
                          nodeOffset);

        while(nodeOffset != 0 && nodeOffset != BEFS_BT_INVAL)
        {
            errno = ReadFromDataStream(dataStream, nodeOffset, btHeader.node_size, out byte[] nodeData);

            if(errno != ErrorNumber.NoError) return errno;

            AaruLogging.Debug(MODULE_NAME,
                              "ParseDirectoryBTree: Read node at offset {0}, got {1} bytes",
                              nodeOffset,
                              nodeData.Length);

            bt_node_hdr nodeHeader = _littleEndian
                                         ? Marshal.ByteArrayToStructureLittleEndian<bt_node_hdr>(nodeData)
                                         : Marshal.ByteArrayToStructureBigEndian<bt_node_hdr>(nodeData);

            AaruLogging.Debug(MODULE_NAME,
                              "ParseDirectoryBTree: Node header: keys_length={0}, node_keys={1}, left_link={2}, right_link={3}, overflow_link=0x{4:X16}",
                              nodeHeader.keys_length,
                              nodeHeader.node_keys,
                              nodeHeader.left_link,
                              nodeHeader.right_link,
                              nodeHeader.overflow_link);

            bool isLeafNode = nodeHeader.overflow_link == BEFS_BT_INVAL;

            AaruLogging.Debug(MODULE_NAME, "ParseDirectoryBTree: Node type: {0}", isLeafNode ? "LEAF" : "INTERIOR");

            if(isLeafNode)
            {
                // Parse leaf node entries
                // According to Linux code: "Except that rounding up to 8 works, and rounding up to 4 doesn't."
                // Header is 28 bytes (left, right, overflow = 3 off_t = 24 bytes + count + length = 4 bytes)
                const int headerSize   = 28;
                int       keyDataStart = headerSize;
                int       keyDataEnd   = keyDataStart + nodeHeader.keys_length;

                // Align to 8-byte boundary, not 4-byte!
                int keyLengthIndexStart = (keyDataEnd + 7)                           / 8 * 8;
                int valueDataStart      = keyLengthIndexStart + nodeHeader.node_keys * 2;

                AaruLogging.Debug(MODULE_NAME,
                                  "Leaf node layout: headerSize={0}, keyDataStart={1}, keyDataEnd={2}, keyLengthIndexStart={3}, valueDataStart={4}",
                                  headerSize,
                                  keyDataStart,
                                  keyDataEnd,
                                  keyLengthIndexStart,
                                  valueDataStart);

                AaruLogging.Debug(MODULE_NAME,
                                  "Node data first 128 bytes: {0}",
                                  BitConverter.ToString(nodeData, 0, Math.Min(128, nodeData.Length)));

                var keyOffset = 0;

                for(var i = 0; i < nodeHeader.node_keys; i++)
                {
                    int offsetIndex = keyLengthIndexStart + i * 2;

                    // Bounds check before reading
                    if(offsetIndex + 2 > nodeData.Length)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "Offset index {0} extends beyond node data length {1}",
                                          offsetIndex + 2,
                                          nodeData.Length);

                        break;
                    }

                    ushort keyEndOffset = _littleEndian
                                              ? BitConverter.ToUInt16(nodeData, offsetIndex)
                                              : BigEndianBitConverter.ToUInt16(nodeData, offsetIndex);

                    AaruLogging.Debug(MODULE_NAME,
                                      "Key {0}: offsetIndex={1}, keyOffset={2}, keyEndOffset={3}",
                                      i,
                                      offsetIndex,
                                      keyOffset,
                                      keyEndOffset);

                    int keyStart  = keyDataStart + keyOffset;
                    int keyLength = keyEndOffset - keyOffset;

                    // Validate key length - must be positive and reasonable
                    if(keyLength <= 0 || keyLength > 256)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "Invalid key length at index {0}: keyOffset={1}, keyEndOffset={2}, calculated length={3}",
                                          i,
                                          keyOffset,
                                          keyEndOffset,
                                          keyLength);

                        break;
                    }

                    // Validate key data is within the key data section
                    if(keyStart < keyDataStart || keyStart + keyLength > keyDataEnd)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "Key data out of bounds at index {0}: start={1}, length={2}, range=[{3},{4}]",
                                          i,
                                          keyStart,
                                          keyLength,
                                          keyDataStart,
                                          keyDataEnd);

                        break;
                    }

                    int valueIndex = valueDataStart + i * 8;

                    // Bounds check for value data
                    if(valueIndex + 8 > nodeData.Length)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "Value index {0} extends beyond node data length {1}",
                                          valueIndex + 8,
                                          nodeData.Length);

                        break;
                    }

                    long inodeNumber = _littleEndian
                                           ? BitConverter.ToInt64(nodeData, valueIndex)
                                           : BigEndianBitConverter.ToInt64(nodeData, valueIndex);

                    string keyName = _encoding.GetString(nodeData, keyStart, keyLength);

                    // Filter out "." (current directory) and ".." (parent directory)
                    // These should not be cached as they cause infinite loops during path traversal
                    if(keyName != "." && keyName != ".." && !entries.ContainsKey(keyName))
                        entries[keyName] = inodeNumber;

                    keyOffset = keyEndOffset;
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

    /// <summary>Recursively traverses a B+tree node and its siblings</summary>
    /// <remarks>
    ///     Processes both interior and leaf nodes. For interior nodes, follows the overflow link
    ///     to child nodes. For leaf nodes, parses the entries. Also processes right sibling links
    ///     to continue traversal across nodes at the same level.
    /// </remarks>
    /// <param name="dataStream">The data stream containing the B+tree nodes</param>
    /// <param name="btHeader">The B+tree header with metadata and node size</param>
    /// <param name="nodeOffset">The byte offset of the current node in the data stream</param>
    /// <param name="nodeData">The raw node data</param>
    /// <returns>Error code indicating success or failure</returns>
    private ErrorNumber TraverseBTree(data_stream dataStream, bt_header btHeader, long nodeOffset, byte[] nodeData)
    {
        const long BEFS_BT_INVAL = unchecked((long)0xFFFFFFFFFFFFFFFF);

        bt_node_hdr nodeHeader = _littleEndian
                                     ? Marshal.ByteArrayToStructureLittleEndian<bt_node_hdr>(nodeData)
                                     : Marshal.ByteArrayToStructureBigEndian<bt_node_hdr>(nodeData);

        AaruLogging.Debug(MODULE_NAME, "B+tree node: keys={0}", nodeHeader.node_keys);

        bool isLeafNode = nodeHeader.overflow_link == BEFS_BT_INVAL;

        if(isLeafNode)
            ParseLeafNode(nodeData, nodeHeader);
        else
        {
            // Interior node - read overflow node
            if(nodeHeader.overflow_link != 0 && nodeHeader.overflow_link != BEFS_BT_INVAL)
            {
                ErrorNumber errno = ReadFromDataStream(dataStream,
                                                       nodeHeader.overflow_link,
                                                       btHeader.node_size,
                                                       out byte[] overflowNodeData);

                if(errno != ErrorNumber.NoError) return errno;

                errno = TraverseBTree(dataStream, btHeader, nodeHeader.overflow_link, overflowNodeData);

                if(errno != ErrorNumber.NoError) return errno;
            }
        }

        // Read right sibling
        if(nodeHeader.right_link != 0 && nodeHeader.right_link != BEFS_BT_INVAL)
        {
            ErrorNumber errno = ReadFromDataStream(dataStream,
                                                   nodeHeader.right_link,
                                                   btHeader.node_size,
                                                   out byte[] siblingData);

            if(errno != ErrorNumber.NoError) return errno;

            errno = TraverseBTree(dataStream, btHeader, nodeHeader.right_link, siblingData);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a B+tree leaf node and caches the directory entries</summary>
    /// <remarks>
    ///     Extracts the key/value pairs from a leaf node. Keys are filenames and values are
    ///     i-node addresses. The node layout consists of key data, a key length index, and
    ///     a value array. All directory entries are cached in _rootDirectoryCache for retrieval.
    /// </remarks>
    /// <param name="nodeData">The raw leaf node data</param>
    /// <param name="nodeHeader">The parsed node header containing metadata</param>
    private void ParseLeafNode(byte[] nodeData, bt_node_hdr nodeHeader)
    {
        AaruLogging.Debug(MODULE_NAME,
                          "Parsing leaf node with {0} keys, {1} bytes of key data",
                          nodeHeader.node_keys,
                          nodeHeader.keys_length);

        // Leaf node structure:
        // - Header (28 bytes)
        // - Key data
        // - Key length index (array of shorts)
        // - Value data (array of long)

        var headerSize   = 28; // bt_node_hdr size
        int keyDataStart = headerSize;
        int keyDataEnd   = keyDataStart + nodeHeader.keys_length;

        // The key length index is right after the key data, rounded up to 4-byte boundary
        int keyLengthIndexStart = keyDataEnd + 3 & ~3;
        int valueDataStart      = keyLengthIndexStart + nodeHeader.node_keys * 2; // 2 bytes per short

        AaruLogging.Debug(MODULE_NAME,
                          "Node layout: keyData=[{0}-{1}], keyLengthIdx={2}, valueData={3}",
                          keyDataStart,
                          keyDataEnd,
                          keyLengthIndexStart,
                          valueDataStart);

        // Parse each entry
        var keyOffset = 0;

        for(var i = 0; i < nodeHeader.node_keys; i++)
        {
            // Read the ending offset of this key from the length index
            int offsetIndex = keyLengthIndexStart + i * 2;

            ushort keyEndOffset = _littleEndian
                                      ? BitConverter.ToUInt16(nodeData, offsetIndex)
                                      : (ushort)(nodeData[offsetIndex] << 8 | nodeData[offsetIndex + 1]);

            int keyStart  = keyDataStart + keyOffset;
            int keyLength = keyEndOffset - keyOffset;

            // Read the value (i-node number) for this key
            int valueIndex = valueDataStart + i * 8;

            long inodeNumber = _littleEndian
                                   ? BitConverter.ToInt64(nodeData, valueIndex)
                                   : (long)nodeData[valueIndex]     << 56 |
                                     (long)nodeData[valueIndex + 1] << 48 |
                                     (long)nodeData[valueIndex + 2] << 40 |
                                     (long)nodeData[valueIndex + 3] << 32 |
                                     (long)nodeData[valueIndex + 4] << 24 |
                                     (long)nodeData[valueIndex + 5] << 16 |
                                     (long)nodeData[valueIndex + 6] << 8  |
                                     nodeData[valueIndex + 7];

            // Extract the key name (fixed-length string, not null-terminated)
            string keyName = _encoding.GetString(nodeData, keyStart, keyLength);

            AaruLogging.Debug(MODULE_NAME,
                              "Entry {0}: name='{1}' ({2} bytes), i-node={3}",
                              i,
                              keyName,
                              keyLength,
                              inodeNumber);

            // Cache this entry - store the i-node address for later retrieval
            if(!_rootDirectoryCache.ContainsKey(keyName)) _rootDirectoryCache[keyName] = inodeNumber;

            keyOffset = keyEndOffset;
        }
    }
}