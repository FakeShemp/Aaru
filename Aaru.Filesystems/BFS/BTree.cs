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
using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
public sealed partial class BeFS
{
    /// <summary>Parses the directory B+tree structure from a data stream</summary>
    /// <remarks>
    ///     Reads the B+tree header from the start of the data stream, validates it,
    ///     reads the root node, and traverses the tree to extract all directory entries
    ///     which are then cached for later retrieval.
    /// </remarks>
    /// <param name="dataStream">The data stream containing the B+tree data</param>
    /// <returns>Error code indicating success or failure</returns>
    private ErrorNumber ParseDirectoryBTree(data_stream dataStream)
    {
        AaruLogging.Debug(MODULE_NAME, "Parsing directory B+tree from data stream...");
        AaruLogging.Debug(MODULE_NAME, "Filesystem endianness: {0}", _littleEndian ? "Little-endian" : "Big-endian");

        // Read B+tree header from position 0 of the data stream
        ErrorNumber errno = ReadFromDataStream(dataStream, 0, 32, out byte[] headerData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading B+tree header: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "B+tree header raw bytes: {0}",
                          BitConverter.ToString(headerData, 0, Math.Min(32, headerData.Length)));

        bt_header btHeader = _littleEndian
                                 ? Marshal.ByteArrayToStructureLittleEndian<bt_header>(headerData)
                                 : Marshal.ByteArrayToStructureBigEndian<bt_header>(headerData);

        AaruLogging.Debug(MODULE_NAME,
                          "B+tree header: magic=0x{0:X8}, node_size={1}, root_ptr={2}",
                          btHeader.magic,
                          btHeader.node_size,
                          btHeader.node_root_pointer);

        // Validate B+tree header
        if(btHeader.magic != BTREE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid B+tree magic! Expected 0x{0:X8}, got 0x{1:X8}",
                              BTREE_MAGIC,
                              btHeader.magic);

            return ErrorNumber.InvalidArgument;
        }

        if(btHeader.node_size != 1024)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid B+tree node size! Expected 1024, got {0}", btHeader.node_size);

            return ErrorNumber.InvalidArgument;
        }

        // Read root node from data stream
        errno = ReadFromDataStream(dataStream, btHeader.node_root_pointer, btHeader.node_size, out byte[] rootNodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root B+tree node: {0}", errno);

            return errno;
        }

        // Traverse B+tree
        return TraverseBTree(dataStream, btHeader, btHeader.node_root_pointer, rootNodeData);
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