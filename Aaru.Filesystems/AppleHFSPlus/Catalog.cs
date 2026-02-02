// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Catalog.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System Plus plugin.
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

// ReSharper disable InconsistentNaming

using System;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Aaru.Filesystems;

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <summary>Reads a catalog B-Tree node by node number</summary>
    ErrorNumber ReadCatalogNode(uint nodeNumber, out byte[] nodeData)
    {
        nodeData = null;

        HFSPlusForkData catalogFork = _volumeHeader.catalogFile;

        if(catalogFork.extents.extentDescriptors == null || catalogFork.extents.extentDescriptors.Length == 0)
            return ErrorNumber.InvalidArgument;

        // Calculate byte offset of node within the catalog file
        ulong nodeOffset = (ulong)nodeNumber * _catalogBTreeHeader.nodeSize;

        // Find which extent contains this offset
        ulong currentOffset = 0;

        foreach(HFSPlusExtentDescriptor extent in catalogFork.extents.extentDescriptors)
        {
            if(extent.blockCount == 0) break;

            ulong extentSizeInBytes = (ulong)extent.blockCount * _volumeHeader.blockSize;

            if(nodeOffset < currentOffset + extentSizeInBytes)
            {
                // Found the extent containing this node
                ulong offsetInExtent = nodeOffset                                         - currentOffset;
                ulong blockOffset    = (ulong)extent.startBlock * _volumeHeader.blockSize + offsetInExtent;

                // Convert to device sector address
                ulong deviceSector = (_partitionStart * _sectorSize + blockOffset) / _sectorSize;
                var   byteOffset   = (uint)((_partitionStart * _sectorSize + blockOffset) % _sectorSize);

                uint sectorsToRead = (_catalogBTreeHeader.nodeSize + byteOffset + _sectorSize - 1) / _sectorSize;

                ErrorNumber errno = _imagePlugin.ReadSectors(deviceSector,
                                                             false,
                                                             sectorsToRead,
                                                             out byte[] sectorData,
                                                             out _);

                if(errno != ErrorNumber.NoError) return errno;

                if(sectorData.Length < byteOffset + _catalogBTreeHeader.nodeSize) return ErrorNumber.InvalidArgument;

                nodeData = new byte[_catalogBTreeHeader.nodeSize];
                Array.Copy(sectorData, (int)byteOffset, nodeData, 0, _catalogBTreeHeader.nodeSize);

                return ErrorNumber.NoError;
            }

            currentOffset += extentSizeInBytes;
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Traverses the B-Tree from a given node, applying a predicate function to each leaf node record</summary>
    /// <remarks>
    ///     This is a general-purpose B-Tree traversal method that can be used to search for any catalog entries.
    ///     The predicate function is called for each record in leaf nodes and should return true to stop traversal.
    /// </remarks>
    ErrorNumber TraverseCatalogBTree(uint nodeNumber, Func<byte[], ushort, bool> recordPredicate)
    {
        // Read the node at nodeNumber
        ErrorNumber errno = ReadCatalogNode(nodeNumber, out byte[] nodeData);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse the node descriptor to determine if it's a leaf or index node
        int ndSize = Marshal.SizeOf(typeof(BTNodeDescriptor));

        if(nodeData.Length < ndSize) return ErrorNumber.InvalidArgument;

        BTNodeDescriptor nodeDesc =
            Helpers.Marshal.ByteArrayToStructureBigEndian<BTNodeDescriptor>(nodeData, 0, ndSize);

        if(nodeDesc.kind == BTNodeKind.kBTLeafNode)
        {
            // Process leaf node records with the predicate
            BTNodeDescriptor descriptor = nodeDesc;
            ushort           numRecords = descriptor.numRecords;

            var foundMatch = false;

            for(ushort i = 0; i < numRecords; i++)
            {
                int offsetPointerOffset = _catalogBTreeHeader.nodeSize - 2 * (i + 1);

                if(offsetPointerOffset < 0 || offsetPointerOffset + 2 > nodeData.Length) continue;

                var recordOffset = BigEndianBitConverter.ToUInt16(nodeData, offsetPointerOffset);

                // Call predicate - if it returns true, stop entire traversal
                if(recordPredicate(nodeData, recordOffset)) return ErrorNumber.NoError;

                // Track if we found at least one matching record
                // This is determined by the predicate not returning true but processing the record
            }

            // After processing this leaf node, continue to the next leaf node via fLink
            if(descriptor.fLink != 0)
            {
                // Continue traversing the next leaf node
                return TraverseCatalogBTree(descriptor.fLink, recordPredicate);
            }

            // No more leaf nodes and no match found
            return foundMatch ? ErrorNumber.NoError : ErrorNumber.InvalidArgument;
        }

        if(nodeDesc.kind == BTNodeKind.kBTIndexNode)
        {
            // In index nodes, find the first record which points to a child node
            ushort numRecords = nodeDesc.numRecords;

            if(numRecords == 0) return ErrorNumber.InvalidArgument;

            // Get the first record offset (stored at nodeSize - 2 * 1)
            int firstRecordOffsetPos = _catalogBTreeHeader.nodeSize - 2;

            if(firstRecordOffsetPos < 0 || firstRecordOffsetPos + 2 > nodeData.Length)
                return ErrorNumber.InvalidArgument;

            var firstRecordOffset = BigEndianBitConverter.ToUInt16(nodeData, firstRecordOffsetPos);

            if(firstRecordOffset >= nodeData.Length || firstRecordOffset + 2 > nodeData.Length)
                return ErrorNumber.InvalidArgument;

            var keyLength = BigEndianBitConverter.ToUInt16(nodeData, firstRecordOffset);

            // Child pointer comes after the key
            int childPtrOffset = firstRecordOffset + 2 + keyLength;

            if(childPtrOffset + 4 > nodeData.Length) return ErrorNumber.InvalidArgument;

            var childNode = BigEndianBitConverter.ToUInt32(nodeData, childPtrOffset);

            return TraverseCatalogBTree(childNode, recordPredicate);
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Extracts the filename from a catalog key</summary>
    string ExtractNameFromCatalogKey(byte[] leafNode, ushort keyOffset)
    {
        if(keyOffset + 2 > leafNode.Length) return string.Empty;

        var keyLength = BigEndianBitConverter.ToUInt16(leafNode, keyOffset);

        // The key structure is: keyLength(2) + parentID(4) + nodeName (Unicode string with length prefix)
        // nodeName format: length(2) + UTF-16 data
        int nameOffset = keyOffset + 2 + 4; // Skip keyLength and parentID

        if(nameOffset + 2 > leafNode.Length) return string.Empty;

        var nameLength = BigEndianBitConverter.ToUInt16(leafNode, nameOffset);

        // nameLength is in UTF-16 code units (2 bytes each)
        int nameDataSize = nameLength * 2;

        if(nameOffset + 2 + nameDataSize > leafNode.Length) return string.Empty;

        // Convert UTF-16 big-endian bytes to string
        try
        {
            var nameBytes = new byte[nameDataSize];
            Array.Copy(leafNode, nameOffset + 2, nameBytes, 0, nameDataSize);

            // Decode as UTF-16 big-endian
            string name = Encoding.BigEndianUnicode.GetString(nameBytes);

            return name;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     Compares two filenames according to the volume's case-sensitivity setting.
    ///     For case-sensitive volumes (HFSX with kHFSBinaryCompare), performs binary comparison.
    ///     For case-insensitive volumes (HFS+ or HFSX with kHFSCaseFolding), performs case-insensitive comparison.
    /// </summary>
    /// <param name="name1">First name to compare</param>
    /// <param name="name2">Second name to compare</param>
    /// <returns>True if names match according to volume's comparison rules, false otherwise</returns>
    private bool CompareNames(string name1, string name2)
    {
        if(_isCaseSensitive)
        {
            // Case-sensitive: binary comparison of UTF-16 code units
            // According to TN1150: each character compared as unsigned 16-bit integer
            return string.Equals(name1, name2, StringComparison.Ordinal);
        }

        // Case-insensitive: use Unicode case-folding comparison (same as HFS+)
        return string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
    }
}