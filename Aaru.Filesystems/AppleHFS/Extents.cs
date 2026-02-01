// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Extents.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System plugin.
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

//
// EXTENTS OVERFLOW FILE - HFS FILE EXTENT MANAGEMENT
// ====================================================
//
// Overview:
// The HFS File Manager tracks which allocation blocks belong to a file by maintaining
// a list of extents (contiguous ranges of allocation blocks). Each extent is represented
// by a pair of values: the starting allocation block and the number of allocation blocks.
//
// Extent Storage Strategy:
// 1. SMALL FILES (up to 3 extents):
//    The first 3 extents are stored directly in the catalog file record:
//    - Data fork: stored in CdrFilRec.filExtRec (ExtDataRec)
//    - Resource fork: stored in CdrFilRec.filRExtRec (ExtDataRec)
//    Each ExtDataRec contains 3 ExtDescriptor structures (8 bytes each = 12 bytes total)
//    If a file has fewer than 3 extents, unused extents have xdrNumABlks = 0
//
// 2. LARGE FILES (more than 3 extents):
//    Additional extents are stored in the Extents Overflow File, organized as a B-Tree.
//    The File Manager only needs to read the extents overflow file for extents beyond
//    the first three; this minimizes disk I/O for small files.
//
// Extents Overflow File B-Tree Structure:
// ========================================
//
// EXTENT KEY FORMAT (used in both index and leaf nodes):
// - keyLen (1 byte): Length of key data after this field (value = 7, indicating 7 bytes)
// - forkType (1 byte): 0x00 for data fork, 0xFF for resource fork
// - fileNumber (4 bytes): File ID (CNID) of the file
// - startFileABN (2 bytes): Starting file allocation block number (FAB) within the file
//   This represents the allocation block offset within the file where this extent record starts.
//   Total key size: 1 + 1 + 4 + 2 = 8 bytes (includes keyLen)
//
// EXTENT DATA RECORD (data portion of leaf node records):
// Contains 3 ExtDescriptor structures for the file starting at the specified FAB:
// - xdrStABN (2 bytes): First allocation block on disk for this extent
// - xdrNumABlks (2 bytes): Number of allocation blocks in this extent
// - (repeated 3 times = 12 bytes total)
//
// Traversing the Extents Overflow B-Tree:
// =======================================
//
// 1. START: Read MDB for extents B-Tree location and initial extent records
// 2. INDEX NODES: Navigate using extent keys (fileId + startBlock as search key)
// 3. LEAF NODES: Extract extent records matching the target file ID and fork type
// 4. FORWARD LINK: Use ndFLink to traverse sibling leaf nodes for additional extents
//
// Key Characteristics:
// - Extent records are ordered by (fileNumber, forkType, startFileABN)
// - All extents for a single file/fork are stored consecutively in the B-Tree
// - Each leaf record contains up to 3 extents for a given (file, fork, startBlock)
// - Leaf node forward links allow efficient iteration through all extents
//
// Example - Reconstructing All File Extents:
// ==========================================
//
// For a file with CNID=100 and 5 data fork extents:
//
// Step 1: From catalog file, read CdrFilRec.filExtRec (ExtDataRec)
//         This contains the first 3 extents
//
// Step 2: Search extents overflow B-Tree for key (fileId=100, fork=DATA, startBlock=3)
//         This retrieves the 4th extent (stored in next ExtDataRec in overflow)
//
// Step 3: Search extents overflow B-Tree for key (fileId=100, fork=DATA, startBlock=4)
//         This retrieves the 5th extent
//
// Step 4: Search for key (fileId=100, fork=DATA, startBlock=5)
//         Returns NoSuchFile when no more extents exist
//
// This approach ensures:
// - Fast access for small files (no B-Tree lookup needed)
// - Efficient iteration through large file extents
// - Minimal memory footprint (extent records are read on-demand)
// - Proper handling of fragmented files
//

using System;
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from Inside Macintosh
// https://developer.apple.com/legacy/library/documentation/mac/pdf/Files/File_Manager.pdf
public sealed partial class AppleHFS
{
    /// <summary>Cache of extent records by file ID and fork type</summary>
    Dictionary<(uint fileId, ForkType forkType), List<ExtDescriptor>> _extentCache;

    /// <summary>Safely validates and initializes an ExtDataRec structure</summary>
    private static ExtDataRec EnsureValidExtDataRec(ExtDataRec extents)
    {
        // If the xdr array is null, create a new one with all zeros
        if(extents.xdr == null)
        {
            extents.xdr = new ExtDescriptor[3];

            for(var i = 0; i < 3; i++)
            {
                extents.xdr[i].xdrStABN    = 0;
                extents.xdr[i].xdrNumABlks = 0;
            }
        }

        return extents;
    }

    /// <summary>Initializes the extents overflow file B-Tree header information</summary>
    ErrorNumber InitializeExtentsOverflowBTree()
    {
        // Get the extents B-Tree header information from the MDB
        // The extents B-Tree extent records are stored in drXTExtRec
        // The extents B-Tree total size is stored in drXTFlSize
        // We need to read the B-Tree header node (node 0) to get tree information

        ErrorNumber errno = ReadExtentsHeader(out BTHdrRed bthdr);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read extents B-Tree header");

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          $"Extents B-Tree: depth={bthdr.bthDepth}, rootNode={bthdr.bthRoot}, " +
                          $"nodeSize={bthdr.bthNodeSize}");

        _extentsBTreeHeader = bthdr;
        _extentCache        = [];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the extents B-Tree header node</summary>
    ErrorNumber ReadExtentsHeader(out BTHdrRed bthdr)
    {
        bthdr = default(BTHdrRed);


        // The extents B-Tree is stored in the extents file, which is identified by CNID 3
        // The first 3 extents of the extents file itself are in the MDB
        // Read node 0 (the header node) using the extent information from MDB

        ErrorNumber errno = ReadNode(0, _mdb.drXTExtRec, _mdb.drXTFlSize, out byte[] nodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read extents B-Tree header node");

            return errno;
        }

        // Skip the node descriptor and get to the B-Tree header record
        // Node descriptor is 14 bytes, then 2 bytes for record offset, then B-Tree header
        int headerOffset = 14 + 2;

        if(nodeData.Length < headerOffset + Marshal.SizeOf<BTHdrRed>()) return ErrorNumber.InvalidArgument;

        bthdr = Marshal.ByteArrayToStructureBigEndian<BTHdrRed>(nodeData, headerOffset, Marshal.SizeOf<BTHdrRed>());

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a node using extents from an extent descriptor record</summary>
    ErrorNumber ReadNode(uint nodeNumber, ExtDataRec extents, uint totalSize, out byte[] nodeData)
    {
        nodeData = null;

        // Ensure the extents struct is properly initialized
        extents = EnsureValidExtDataRec(extents);

        // Calculate which extent contains this node
        uint nodeSize     = 512; // Default HFS node size
        uint nodeSizeBlks = nodeSize / _mdb.drAlBlkSiz;

        if(nodeSizeBlks == 0) nodeSizeBlks = 1;

        uint nodeBlockOffset = nodeNumber * nodeSizeBlks;
        uint blockCount      = 0;
        uint foundBlock      = 0;
        var  foundExtent     = false;

        // Search through the 3 extents for the one containing this node
        for(var i = 0; i < 3; i++)
        {
            if(extents.xdr[i].xdrNumABlks == 0) break;

            uint extentStart  = extents.xdr[i].xdrStABN;
            uint extentLength = extents.xdr[i].xdrNumABlks;

            if(nodeBlockOffset >= blockCount && nodeBlockOffset < blockCount + extentLength)
            {
                // Found the extent containing this node
                uint offsetInExtent = nodeBlockOffset - blockCount;
                foundBlock  = extentStart + offsetInExtent;
                foundExtent = true;

                break;
            }

            blockCount += extentLength;
        }

        if(!foundExtent) return ErrorNumber.InvalidArgument;

        // Read the node from disk
        // HFS allocation blocks are at offsets expressed in 512-byte sectors
        ulong extentOffsetSector512 = (ulong)foundBlock * _mdb.drAlBlkSiz / 512;

        // Convert to device sector address
        HfsOffsetToDeviceSector(extentOffsetSector512, out ulong deviceSector, out uint byteOffset);
        uint sectorCount = (nodeSizeBlks * _mdb.drAlBlkSiz + byteOffset + _sectorSize - 1) / _sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(deviceSector, false, sectorCount, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to read node {nodeNumber}");

            return errno;
        }

        // Extract node data from the appropriate offset
        if(sectorData == null || sectorData.Length < (int)byteOffset + nodeSize) return ErrorNumber.InvalidArgument;

        nodeData = new byte[nodeSize];
        Array.Copy(sectorData, (int)byteOffset, nodeData, 0, nodeSize);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets all extents for a file by searching the extents B-Tree</summary>
    ErrorNumber GetFileExtents(uint                    fileId, ForkType forkType, ExtDataRec firstExtents,
                               out List<ExtDescriptor> allExtents)
    {
        allExtents = new List<ExtDescriptor>();

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Check cache first
        (uint fileId, ForkType forkType) cacheKey = (fileId, forkType);

        if(_extentCache.TryGetValue(cacheKey, out List<ExtDescriptor> cached))
        {
            allExtents = cached;

            return ErrorNumber.NoError;
        }

        // First, add the three extents that are stored in the catalog record
        // Ensure xdr array is properly initialized
        if(firstExtents.xdr != null)
        {
            for(var i = 0; i < 3; i++)
            {
                if(i >= firstExtents.xdr.Length || firstExtents.xdr[i].xdrNumABlks == 0) break;

                allExtents.Add(firstExtents.xdr[i]);

                AaruLogging.Debug(MODULE_NAME,
                                  "Adding extent from catalog: startBlock={0}, numBlocks={1}",
                                  firstExtents.xdr[i].xdrStABN,
                                  firstExtents.xdr[i].xdrNumABlks);
            }
        }

        // If all three initial extents are full (or we have 3), we might have overflow extents
        // Check if we need to search the extents B-Tree
        bool hasThreeExtents = firstExtents.xdr                != null &&
                               firstExtents.xdr.Length         >= 3    &&
                               firstExtents.xdr[2].xdrNumABlks != 0;

        if(hasThreeExtents)
        {
            // Need to search the extents B-Tree for overflow extents
            ErrorNumber errno = SearchExtentsOverflowBTree(fileId, forkType, out List<ExtDescriptor> overflowExtents);

            if(errno != ErrorNumber.NoError)
            {
                // If no overflow extents found, it's ok - file just has 3 extents
                if(errno != ErrorNumber.NoSuchFile) return errno;
            }
            else if(overflowExtents != null && overflowExtents.Count > 0)
            {
                // Combine initial and overflow extents
                allExtents.AddRange(overflowExtents);

                AaruLogging.Debug(MODULE_NAME,
                                  "Added {0} overflow extents for file {1}",
                                  overflowExtents.Count,
                                  fileId);
            }
        }

        if(allExtents.Count == 0) return ErrorNumber.NoSuchFile;

        // Cache the result
        _extentCache[cacheKey] = allExtents;

        return ErrorNumber.NoError;
    }

    /// <summary>Searches the extents overflow B-Tree for extent records of a specific file</summary>
    ErrorNumber SearchExtentsOverflowBTree(uint fileId, ForkType forkType, out List<ExtDescriptor> extents)
    {
        extents = new List<ExtDescriptor>();

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(_extentsBTreeHeader.bthRoot == 0) return ErrorNumber.NoSuchFile;

        // Validate that MDB and its extents are properly initialized
        if(_mdb.drXTExtRec.xdr == null || _mdb.drXTExtRec.xdr[0].xdrNumABlks == 0) return ErrorNumber.NoSuchFile;

        // Start from the root node
        uint currentNode = _extentsBTreeHeader.bthRoot;

        // Traverse the B-Tree to find leaf nodes containing extent records for this file
        for(var level = 0; level < _extentsBTreeHeader.bthDepth; level++)
        {
            ErrorNumber errno = ReadNode(currentNode, _mdb.drXTExtRec, _mdb.drXTFlSize, out byte[] nodeData);

            if(errno != ErrorNumber.NoError) return errno;

            if(nodeData == null || nodeData.Length < Marshal.SizeOf<NodeDescriptor>())
                return ErrorNumber.InvalidArgument;

            // Parse node descriptor
            NodeDescriptor nodeDesc =
                Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData, 0, Marshal.SizeOf<NodeDescriptor>());

            // If leaf node, extract extent records
            if(nodeDesc.ndType == NodeType.ndLeafNode)
            {
                errno = ExtractExtentRecordsFromLeaf(nodeData,
                                                     _mdb.drXTExtRec.xdr[0].xdrNumABlks * _mdb.drAlBlkSiz / 2,
                                                     fileId,
                                                     forkType,
                                                     out List<ExtDescriptor> records);

                if(errno != ErrorNumber.NoError)
                {
                    // If no records found, return NoSuchFile
                    if(extents.Count == 0) return ErrorNumber.NoSuchFile;
                }

                if(records != null) extents.AddRange(records);

                // Continue searching through linked leaf nodes
                if(nodeDesc.ndFLink != 0)
                {
                    currentNode = nodeDesc.ndFLink;

                    continue;
                }

                // No more leaf nodes
                break;
            }

            // Index node - find the next node to traverse
            errno = FindNextNodeInExtentsTree(nodeData, fileId, out currentNode);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(extents.Count == 0) return ErrorNumber.NoSuchFile;

        return ErrorNumber.NoError;
    }

    /// <summary>Finds the next node to traverse in the extents B-Tree</summary>
    ErrorNumber FindNextNodeInExtentsTree(byte[] nodeData, uint targetFileId, out uint nextNode)
    {
        nextNode = 0;

        // Parse node descriptor
        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData, 0, Marshal.SizeOf<NodeDescriptor>());

        if(nodeDesc.ndType != NodeType.ndIndxNode) return ErrorNumber.InvalidArgument;

        // Index nodes contain keys and child pointers
        // For extents, keys are: keyLen(1) + forkType(1) + fileNumber(4) + startBlock(2) = 8 bytes key
        // Each record is: key + pointer(4)

        // Get record offsets
        var dataStart   = 14;            // Node descriptor size
        int recordStart = dataStart + 2; // Skip free space offset

        // Find the first record with fileID >= targetFileId
        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            // Each record in index nodes has a variable-length key followed by a 4-byte pointer
            // Record offsets are stored at the end of the node

            int offsetTableStart = nodeData.Length - (nodeDesc.ndNRecs - i) * 2;
            var recordOffset     = BigEndianBitConverter.ToUInt16(nodeData, offsetTableStart);

            if(recordOffset + 7 > nodeData.Length) return ErrorNumber.InvalidArgument;

            // Parse extent key
            // keyLen(1) + forkType(1) + fileNumber(4) + startBlock(2)
            byte keyLen = nodeData[recordOffset];

            if(keyLen < 7) return ErrorNumber.InvalidArgument;

            var keyFileId = BigEndianBitConverter.ToUInt32(nodeData, recordOffset + 2);

            if(keyFileId >= targetFileId)
            {
                // Get the child pointer (4 bytes after the key)
                nextNode = BigEndianBitConverter.ToUInt32(nodeData, recordOffset + 1 + keyLen);

                return ErrorNumber.NoError;
            }
        }

        // If no record found, use the last pointer
        int lastOffsetTableStart = nodeData.Length - 2;
        var lastRecordOffset     = BigEndianBitConverter.ToUInt16(nodeData, lastOffsetTableStart);

        if(lastRecordOffset + 7 > nodeData.Length) return ErrorNumber.InvalidArgument;

        byte lastKeyLen = nodeData[lastRecordOffset];

        nextNode = BigEndianBitConverter.ToUInt32(nodeData, lastRecordOffset + 1 + lastKeyLen);

        return ErrorNumber.NoError;
    }

    /// <summary>Extracts extent records from a leaf node</summary>
    ErrorNumber ExtractExtentRecordsFromLeaf(byte[] nodeData, uint nodeSize, uint targetFileId, ForkType targetForkType,
                                             out List<ExtDescriptor> records)
    {
        records = new List<ExtDescriptor>();

        // Parse node descriptor
        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData, 0, Marshal.SizeOf<NodeDescriptor>());

        if(nodeDesc.ndType != NodeType.ndLeafNode) return ErrorNumber.InvalidArgument;

        // Leaf nodes contain keys followed by extent data records
        // Key format: keyLen(1) + forkType(1) + fileNumber(4) + startBlock(2) = 8 bytes
        // Data: ExtDataRec = 6 extents * 4 bytes each = 24 bytes

        var dataStart = 14; // Node descriptor size

        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            // Get record offset from offset table at end of node
            int offsetTableStart = nodeData.Length - (nodeDesc.ndNRecs - i) * 2;
            var recordOffset     = BigEndianBitConverter.ToUInt16(nodeData, offsetTableStart);

            if(recordOffset + 8 > nodeData.Length) return ErrorNumber.InvalidArgument;

            // Parse extent key
            byte keyLen = nodeData[recordOffset];

            if(keyLen < 7) return ErrorNumber.InvalidArgument;

            var forkType = (ForkType)nodeData[recordOffset + 1];
            var fileId   = BigEndianBitConverter.ToUInt32(nodeData, recordOffset + 2);

            // Only extract records for matching file ID and fork type
            if(fileId == targetFileId && forkType == targetForkType)
            {
                // Extract the ExtDataRec (3 extents of 4 bytes each = 12 bytes)
                int dataOffset = recordOffset + 1 + keyLen;

                for(var j = 0; j < 3; j++)
                {
                    var startBlock = BigEndianBitConverter.ToUInt16(nodeData, dataOffset + j * 4);
                    var numBlocks  = BigEndianBitConverter.ToUInt16(nodeData, dataOffset + j * 4 + 2);

                    if(numBlocks == 0) break;

                    records.Add(new ExtDescriptor
                    {
                        xdrStABN    = startBlock,
                        xdrNumABlks = numBlocks
                    });
                }
            }
        }

        if(records.Count == 0) return ErrorNumber.NoSuchFile;

        return ErrorNumber.NoError;
    }

    /// <summary>Calculates the byte offset for a file block considering all extents</summary>
    ErrorNumber GetFileBlockOffset(uint      fileId, ForkType forkType, ExtDataRec firstExtents, uint blockInFile,
                                   out ulong byteOffset)
    {
        byteOffset = 0;

        // Get all extents for the file
        ErrorNumber errno = GetFileExtents(fileId, forkType, firstExtents, out List<ExtDescriptor> allExtents);

        if(errno != ErrorNumber.NoError) return errno;

        uint currentBlock = 0;

        foreach(ExtDescriptor ext in allExtents)
        {
            if(blockInFile < currentBlock + ext.xdrNumABlks)
            {
                // Found the extent containing the target block
                uint offsetInExtent  = blockInFile  - currentBlock;
                uint allocationBlock = ext.xdrStABN + offsetInExtent;

                byteOffset = (ulong)allocationBlock * _mdb.drAlBlkSiz;

                return ErrorNumber.NoError;
            }

            currentBlock += ext.xdrNumABlks;
        }

        // Block is beyond file size
        return ErrorNumber.InvalidArgument;
    }
}