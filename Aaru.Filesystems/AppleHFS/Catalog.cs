// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Catalog.cs
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

using System;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class AppleHFS
{
    /// <summary>Caches the root directory from the catalog B-Tree</summary>
    ErrorNumber CacheRootDirectory()
    {
        // Read the catalog header node (node 0) to get B-Tree information
        ErrorNumber errno = ReadCatalogHeader(out BTHdrRed bthdr);

        if(errno != ErrorNumber.NoError) return errno;

        AaruLogging.Debug(MODULE_NAME,
                          $"Catalog B-Tree: depth={bthdr.bthDepth}, rootNode={bthdr.bthRoot}, nodeSize={bthdr.bthNodeSize}");

        // The root directory has:
        //   CNID = kRootCnid (2)
        //   parentID = kRootParentCnid (1)
        // We search for records with parentID=kRootParentCnid to navigate the B-Tree,
        // then find the specific record with CNID=kRootCnid in the leaf node.
        uint targetParentID = kRootParentCnid;
        uint currentNode    = bthdr.bthRoot;

        // Traverse through all levels until we reach a leaf node
        for(var level = 0; level < bthdr.bthDepth; level++)
        {
            errno = ReadNode(currentNode, bthdr.bthNodeSize, out byte[] nodeData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, $"Failed to read node {currentNode} at level {level}");

                return errno;
            }

            // Parse node descriptor
            NodeDescriptor nodeDesc =
                Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData,
                                                                      0,
                                                                      System.Runtime.InteropServices.Marshal
                                                                            .SizeOf(typeof(NodeDescriptor)));

            AaruLogging.Debug(MODULE_NAME,
                              $"Level {level}: Node {currentNode}, type={nodeDesc.ndType}, nRecs={nodeDesc.ndNRecs}");

            // If this is a leaf node, search for root directory records (parentID=1, CNID=2)
            if(nodeDesc.ndType == NodeType.ndLeafNode)
            {
                errno = FindRootInLeaf(nodeData, bthdr.bthNodeSize, out CdrDirRec rootRec);

                if(errno == ErrorNumber.NoError)
                {
                    _rootDirectory = rootRec;

                    AaruLogging.Debug(MODULE_NAME,
                                      $"Root directory found: CNID={rootRec.dirDirID}, {rootRec.dirVal} entries");

                    return ErrorNumber.NoError;
                }

                // Root directory not found in this leaf node, try next leaf if available
                AaruLogging.Debug(MODULE_NAME,
                                  $"Root directory not found in leaf node {currentNode}, trying next leaf node");

                // Get next leaf node number from node descriptor
                if(nodeDesc.ndFLink != 0)
                {
                    currentNode = nodeDesc.ndFLink;
                    level--; // Don't increment level, stay at leaf level to traverse sibling nodes

                    continue;
                }

                // No more leaf nodes to check
                AaruLogging.Debug(MODULE_NAME, "No more leaf nodes to check");

                return errno;
            }

            // This is an index node - find the pointer for records with parentID=kRootParentCnid
            errno = FindIndexPointer(nodeData, bthdr.bthNodeSize, targetParentID, out currentNode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Failed to find index pointer");

                return errno;
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Failed to find root directory after traversing all levels");

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Reads the catalog header node (node 0)</summary>
    ErrorNumber ReadCatalogHeader(out BTHdrRed bthdr)
    {
        bthdr = default(BTHdrRed);

        // Read node 0 (header node)
        ErrorNumber errno = ReadNode(0, 512, out byte[] headerNode);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse B-Tree header record (starts at offset 14, after node descriptor)
        int btHdrSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BTHdrRed));

        if(headerNode.Length < 14 + btHdrSize) return ErrorNumber.InvalidArgument;

        bthdr = Marshal.ByteArrayToStructureBigEndian<BTHdrRed>(headerNode, 14, btHdrSize);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a node from the catalog file</summary>
    ErrorNumber ReadNode(uint nodeNumber, ushort nodeSize, out byte[] nodeData)
    {
        nodeData = null;

        // Catalog file extent is in MDB
        ExtDescriptor catalogExtent = _mdb.drCTExtRec.xdr[0];

        if(catalogExtent.xdrNumABlks == 0) return ErrorNumber.InvalidArgument;

        // Calculate node offset: each node is nodeSize bytes
        // But nodes are always stored in 512-byte units on disk
        ulong nodeOffset = nodeNumber * 512;

        // Convert allocation block to sector
        ulong allocBlockSectorSize = _mdb.drAlBlkSiz / 512;
        ulong catalogBaseSector    = _mdb.drAlBlSt + catalogExtent.xdrStABN * allocBlockSectorSize;

        // Add node offset in sectors
        ulong nodeSector = catalogBaseSector + nodeOffset / 512;

        AaruLogging.Debug(MODULE_NAME, $"ReadNode: num={nodeNumber}, sector={nodeSector}");

        // Read 1 sector (512 bytes) for the node
        ErrorNumber errno = _imagePlugin.ReadSector(_partitionStart + nodeSector, false, out nodeData, out _);

        return errno;
    }

    ErrorNumber FindIndexPointer(byte[] indexNode, ushort nodeSize, uint targetParentID, out uint childNode)
    {
        childNode = 0;

        // Index node structure per Inside Macintosh:
        // - Node descriptor (14 bytes) at offset 0
        // - Records follow at offset 14
        // - Offset table at end (2 bytes per record, growing backwards)
        //
        // The offset table contains offsets to the START of each record.
        // Each record is: key + child pointer (4 bytes)
        // The first record's offset points to its key (and implicitly to the child pointer after it)

        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(indexNode,
                                                                  0,
                                                                  System.Runtime.InteropServices.Marshal
                                                                        .SizeOf(typeof(NodeDescriptor)));

        if(nodeDesc.ndType != NodeType.ndIndxNode) return ErrorNumber.InvalidArgument;

        if(nodeDesc.ndNRecs == 0) return ErrorNumber.InvalidArgument;

        uint lastPointer = 0;

        // Search through index records
        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            // Offset table is at end of node, growing backwards
            // Entry 0 is at (nodeSize - 2), entry 1 is at (nodeSize - 4), etc.
            var offsetTablePos = (ushort)(nodeSize - 2 - i * 2);

            if(offsetTablePos < 14 || offsetTablePos >= nodeSize) return ErrorNumber.InvalidArgument;

            // Debug: show the bytes at this position
            byte b0 = indexNode[offsetTablePos];
            byte b1 = indexNode[offsetTablePos + 1];

            AaruLogging.Debug(MODULE_NAME,
                              $"Index record {i}: offsetTablePos={offsetTablePos}, bytes=[{b0:X2} {b1:X2}]");

            // Read offset table entry as big-endian
            var recordOffset = BigEndianBitConverter.ToUInt16(indexNode, offsetTablePos);

            AaruLogging.Debug(MODULE_NAME, $"Index record {i}: recordOffset={recordOffset} (0x{recordOffset:X4})");

            if(recordOffset < 14 || recordOffset >= nodeSize) return ErrorNumber.InvalidArgument;

            // Parse key at record offset
            byte keyLen      = indexNode[recordOffset];
            var  keyParentID = BigEndianBitConverter.ToUInt32(indexNode, recordOffset + 2);

            AaruLogging.Debug(MODULE_NAME, $"Index record {i}: keyLen={keyLen}, parentID={keyParentID}");

            // Key size: keyLen + 1 (for keyLen byte) padded to word boundary
            var keySize = (ushort)(keyLen + 1 + 1 & ~1);

            // Child pointer follows the key - read as big-endian
            int pointerOffset = recordOffset + keySize;

            if(pointerOffset + 4 > nodeSize) return ErrorNumber.InvalidArgument;

            var childPointer = BigEndianBitConverter.ToUInt32(indexNode, pointerOffset);

            AaruLogging.Debug(MODULE_NAME, $"  child pointer at {pointerOffset}: {childPointer}");

            // For B-tree searches, we need the pointer to child nodes that might contain
            // records with parentID = targetParentID. This is the child pointer of the
            // largest key that is <= targetParentID.
            if(keyParentID <= targetParentID)
            {
                // This key might contain our target or keys before it
                lastPointer = childPointer;
            }

            if(keyParentID > targetParentID && lastPointer != 0)
            {
                // We've found a key larger than target, so use the previous pointer
                childNode = lastPointer;
                AaruLogging.Debug(MODULE_NAME, $"Found pointer for parentID={targetParentID}: {childNode}");

                return ErrorNumber.NoError;
            }
        }

        // If no larger key found, use the last pointer (which handles records >= targetParentID)
        if(lastPointer != 0)
        {
            childNode = lastPointer;
            AaruLogging.Debug(MODULE_NAME, $"Using last pointer for parentID={targetParentID}: {childNode}");

            return ErrorNumber.NoError;
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Searches a leaf node for the root directory records</summary>
    ErrorNumber FindRootInLeaf(byte[] leafNode, ushort nodeSize, out CdrDirRec rootRec)
    {
        rootRec = default(CdrDirRec);

        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafNode,
                                                                  0,
                                                                  System.Runtime.InteropServices.Marshal
                                                                        .SizeOf(typeof(NodeDescriptor)));

        if(nodeDesc.ndType != NodeType.ndLeafNode) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, $"Searching leaf node with {nodeDesc.ndNRecs} records");

        // Search for the root directory record:
        //   parentID = kRootParentCnid (1)
        //   CNID = kRootCnid (2)
        //
        // The root directory should have:
        // - Thread record (type 3) with parentID=kRootParentCnid, empty name
        // - Directory record (type 1) with parentID=kRootParentCnid, empty name, CNID=kRootCnid
        //
        // OR just a single directory record with CNID=kRootCnid

        // First pass: look for thread record + directory record pair with empty names
        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            // Offset table at end of node, growing backwards
            var offsetPos = (ushort)(nodeSize - 2 - i * 2);

            if(offsetPos < 14 || offsetPos >= nodeSize) continue;

            // Read offset table as big-endian
            var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPos);

            if(recordOffset < 14 || recordOffset >= nodeSize) continue;

            // Parse the key
            byte keyLen      = leafNode[recordOffset];
            var  keyParentID = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);
            byte nameLen     = leafNode[recordOffset                                 + 6];

            // Calculate key size for reading CNID from data
            var keySize    = (ushort)(keyLen + 1 + 1 & ~1);
            int dataOffset = recordOffset + keySize;

            if(dataOffset + 2 > leafNode.Length) continue;

            byte recordType = leafNode[dataOffset];

            // For directory or thread records, parse CNID
            uint recordCNID = 0;

            if(recordType == kCatalogRecordTypeDirectory && dataOffset + 6 + 4 <= leafNode.Length)
            {
                // Directory record (type 1): CdrDirRec structure
                // Offset 0: CatDataRec (type + reserved) = 2 bytes
                // Offset 2: dirFlags = 2 bytes
                // Offset 4: dirVal = 2 bytes
                // Offset 6: dirDirID = 4 bytes (this is the CNID)
                recordCNID = BigEndianBitConverter.ToUInt32(leafNode, dataOffset + 6);
            }
            else if(recordType == kCatalogRecordTypeDirectoryThread && dataOffset + 6 + 4 <= leafNode.Length)
            {
                // Thread record (type 3): CdrThdRec structure
                // Offset 0: CatDataRec (type + reserved) = 2 bytes
                // Offset 2: thdResrv[0] = 4 bytes
                // Offset 6: thdParID = 4 bytes (for thread records, this is the parent ID, not CNID)
                // Thread records don't have a CNID field, so we skip them
                recordCNID = 0;
            }

            AaruLogging.Debug(MODULE_NAME,
                              $"Record {i}: keyLen={keyLen}, parentID={keyParentID}, nameLen={nameLen}, recordType={recordType}, CNID={recordCNID}, dataOffset={dataOffset}");

            // Only look at parentID=kRootParentCnid (1)
            if(keyParentID != kRootParentCnid) continue;

            // Check if this is a directory record (type 1) with CNID=kRootCnid
            if(recordType == kCatalogRecordTypeDirectory && recordCNID == kRootCnid)
            {
                // Parse the directory record
                int cdrDirRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrDirRec));

                if(dataOffset + cdrDirRecSize > leafNode.Length)
                    AaruLogging.Debug(MODULE_NAME,
                                      $"Record {i}: dataOffset {dataOffset} + {cdrDirRecSize} > leafNode.Length {leafNode.Length}");
                else
                {
                    AaruLogging.Debug(MODULE_NAME, $"Record {i}: Found root directory record with CNID={recordCNID}");

                    rootRec = Marshal.ByteArrayToStructureBigEndian<CdrDirRec>(leafNode, dataOffset, cdrDirRecSize);

                    AaruLogging.Debug(MODULE_NAME,
                                      $"Record {i}: Parsed dirDirID={rootRec.dirDirID}, dirVal={rootRec.dirVal}");

                    return ErrorNumber.NoError;
                }
            }

            // Look for thread record (type 3) first (with empty name)
            if(nameLen != 0) continue;

            if(recordType == kCatalogRecordTypeDirectoryThread)
            {
                // Found the thread record. The next record should be the directory record
                if(i + 1 >= nodeDesc.ndNRecs) continue;

                var nextOffsetPos = (ushort)(nodeSize - 2 - (i + 1) * 2);

                if(nextOffsetPos < 14 || nextOffsetPos >= nodeSize) continue;

                var nextRecordOffset = BigEndianBitConverter.ToUInt16(leafNode, nextOffsetPos);

                if(nextRecordOffset < 14 || nextRecordOffset >= nodeSize) continue;

                // Check the next record's key
                byte nextKeyLen      = leafNode[nextRecordOffset];
                var  nextKeyParentID = BigEndianBitConverter.ToUInt32(leafNode, nextRecordOffset + 2);
                byte nextNameLen     = leafNode[nextRecordOffset                                 + 6];

                // Verify next record has matching parentID and empty name
                if(nextKeyParentID != kRootParentCnid || nextNameLen != 0) continue;

                // Calculate key size for next record
                var nextKeySize    = (ushort)(nextKeyLen + 1 + 1 & ~1);
                int nextDataOffset = nextRecordOffset + nextKeySize;

                if(nextDataOffset + 2 > leafNode.Length) continue;

                byte nextRecordType = leafNode[nextDataOffset];

                AaruLogging.Debug(MODULE_NAME,
                                  $"Record {i + 1}: keyLen={nextKeyLen}, parentID={nextKeyParentID}, nameLen={nextNameLen}, recordType={nextRecordType}");

                // Check if it's a directory record (type 1)
                if(nextRecordType != kCatalogRecordTypeDirectory) continue;

                // Parse the directory record
                int cdrDirRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrDirRec));

                if(nextDataOffset + cdrDirRecSize > leafNode.Length)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      $"Record {i + 1}: dataOffset {nextDataOffset} + {cdrDirRecSize} > leafNode.Length {leafNode.Length}");

                    continue;
                }

                AaruLogging.Debug(MODULE_NAME,
                                  $"Record {i + 1}: Parsing CdrDirRec at offset {nextDataOffset}, size {cdrDirRecSize}");

                rootRec = Marshal.ByteArrayToStructureBigEndian<CdrDirRec>(leafNode, nextDataOffset, cdrDirRecSize);

                AaruLogging.Debug(MODULE_NAME,
                                  $"Record {i + 1}: Parsed dirDirID={rootRec.dirDirID}, dirVal={rootRec.dirVal}");

                // Verify this is the root directory (CNID should be kRootCnid = 2)
                if(rootRec.dirDirID != kRootCnid)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      $"Record {i + 1}: Found directory record with CNID={rootRec.dirDirID}, but expected kRootCnid={kRootCnid}");

                    continue;
                }

                AaruLogging.Debug(MODULE_NAME, $"Found root directory record: CNID={rootRec.dirDirID}");

                return ErrorNumber.NoError;
            }

            // If we find a directory record (type 1) with parentID=kRootParentCnid and empty name directly,
            // parse it to check if CNID=2
            if(recordType == kCatalogRecordTypeDirectory)
            {
                // Parse the directory record
                int cdrDirRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrDirRec));

                if(dataOffset + cdrDirRecSize > leafNode.Length)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      $"Record {i}: dataOffset {dataOffset} + {cdrDirRecSize} > leafNode.Length {leafNode.Length}");

                    continue;
                }

                AaruLogging.Debug(MODULE_NAME,
                                  $"Record {i}: Parsing CdrDirRec at offset {dataOffset}, size {cdrDirRecSize}");

                rootRec = Marshal.ByteArrayToStructureBigEndian<CdrDirRec>(leafNode, dataOffset, cdrDirRecSize);

                AaruLogging.Debug(MODULE_NAME,
                                  $"Record {i}: Parsed dirDirID={rootRec.dirDirID}, dirVal={rootRec.dirVal}");

                AaruLogging.Debug(MODULE_NAME, $"Record {i}: Found directory record with CNID={rootRec.dirDirID}");

                // Verify this is the root directory (CNID should be kRootCnid = 2)
                if(rootRec.dirDirID != kRootCnid)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      $"Record {i}: CNID {rootRec.dirDirID} != expected kRootCnid {kRootCnid}, continuing");

                    continue;
                }

                AaruLogging.Debug(MODULE_NAME,
                                  $"Found root directory record (without thread): CNID={rootRec.dirDirID}");

                return ErrorNumber.NoError;
            }
        }

        // Second pass: look for ANY directory record (type 1) with CNID=kRootCnid, regardless of other constraints
        AaruLogging.Debug(MODULE_NAME, "Second pass: searching for ANY directory record with CNID=kRootCnid");

        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            var offsetPos = (ushort)(nodeSize - 2 - i * 2);

            if(offsetPos < 14 || offsetPos >= nodeSize) continue;

            var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPos);

            if(recordOffset < 14 || recordOffset >= nodeSize) continue;

            // Parse the key
            byte keyLen      = leafNode[recordOffset];
            var  keyParentID = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);

            // Calculate key size
            var keySize    = (ushort)(keyLen + 1 + 1 & ~1);
            int dataOffset = recordOffset + keySize;

            if(dataOffset + 2 > leafNode.Length) continue;

            byte recordType = leafNode[dataOffset];

            // Only look at directory records (type 1)
            if(recordType != kCatalogRecordTypeDirectory) continue;

            // Parse the directory record
            int cdrDirRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrDirRec));

            if(dataOffset + cdrDirRecSize > leafNode.Length) continue;

            rootRec = Marshal.ByteArrayToStructureBigEndian<CdrDirRec>(leafNode, dataOffset, cdrDirRecSize);

            AaruLogging.Debug(MODULE_NAME,
                              $"Record {i} (second pass): keyParentID={keyParentID}, parsed dirDirID={rootRec.dirDirID}, nameLen={leafNode[recordOffset + 6]}, type={recordType}, dataOffset={dataOffset}");

            // Check if this is the root directory (CNID should be kRootCnid = 2)
            if(rootRec.dirDirID == kRootCnid)
            {
                AaruLogging.Debug(MODULE_NAME, $"Found root directory record (second pass): CNID={rootRec.dirDirID}");

                return ErrorNumber.NoError;
            }
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Caches all entries from the root directory by traversing the B-Tree efficiently</summary>
    ErrorNumber CacheRootDirectoryEntries()
    {
        // Initialize the cache dictionary
        _rootDirectoryCache = [];

        // Get the root directory CNID from the cached root directory record
        uint   targetParentID = _rootDirectory.dirDirID;
        ushort expectedCount  = _rootDirectory.dirVal;

        AaruLogging.Debug(MODULE_NAME,
                          $"Caching root directory entries: CNID={targetParentID}, expectedCount={expectedCount}");

        // Read the catalog header to get B-Tree information
        ErrorNumber errno = ReadCatalogHeader(out BTHdrRed bthdr);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read catalog header for caching entries");

            return errno;
        }

        // CRITICAL: The B-Tree stores entries sorted by (parentID, name).
        // Entries with the same parentID are grouped together but may span MULTIPLE leaf nodes.
        //
        // B-Tree Navigation Strategy:
        // 1. Navigate from root to the appropriate leaf node for targetParentID
        // 2. Once at a leaf node, traverse BACKWARD using ndBLink to find the FIRST leaf node
        //    that might contain entries with parentID = targetParentID
        // 3. Then traverse FORWARD through sibling nodes (using ndFLink)
        // 4. Extract ALL records with parentID = targetParentID from each leaf node
        // 5. Stop when we encounter entries with parentID > targetParentID consistently

        uint   currentNode  = bthdr.bthRoot;
        ushort entriesFound = 0;

        // Navigate from root to a leaf node for targetParentID
        for(var level = 0; level < bthdr.bthDepth; level++)
        {
            errno = ReadNode(currentNode, bthdr.bthNodeSize, out byte[] nodeData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, $"Failed to read node {currentNode} at level {level}");

                return errno;
            }

            NodeDescriptor nodeDesc =
                Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData,
                                                                      0,
                                                                      System.Runtime.InteropServices.Marshal
                                                                            .SizeOf(typeof(NodeDescriptor)));

            // If we've reached a leaf node, we'll process from here
            if(nodeDesc.ndType == NodeType.ndLeafNode)
            {
                // We've found a leaf node that may contain targetParentID entries.
                // Now traverse backwards to find the FIRST leaf node that contains targetParentID entries
                uint leafNode = currentNode;

                // Traverse backward to find the start of targetParentID entries
                while(leafNode != 0)
                {
                    errno = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] leafData);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME, $"Failed to read leaf node {leafNode}");

                        return errno;
                    }

                    NodeDescriptor leafDesc =
                        Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafData,
                                                                              0,
                                                                              System.Runtime.InteropServices.Marshal
                                                                                 .SizeOf(typeof(NodeDescriptor)));

                    // Check if this leaf node contains any targetParentID entries
                    if(!LeafContainsTargetParentID(leafData, bthdr.bthNodeSize, targetParentID))
                    {
                        // This leaf doesn't have our target, move to next
                        if(leafDesc.ndFLink == 0) break;

                        leafNode = leafDesc.ndFLink;

                        continue;
                    }

                    // Found a leaf with targetParentID entries. Move back one more to make sure we're at the beginning
                    if(leafDesc.ndBLink != 0)
                        leafNode = leafDesc.ndBLink;
                    else
                        break; // This is the first leaf node
                }

                // Now traverse forward and extract all entries with targetParentID
                while(leafNode != 0)
                {
                    errno = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] leafData);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME, $"Failed to read leaf node {leafNode}");

                        return errno;
                    }

                    NodeDescriptor leafDesc =
                        Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafData,
                                                                              0,
                                                                              System.Runtime.InteropServices.Marshal
                                                                                 .SizeOf(typeof(NodeDescriptor)));

                    // Extract all entries with parentID = targetParentID from this leaf node
                    errno = ExtractDirectoryEntriesFromLeaf(leafData,
                                                            bthdr.bthNodeSize,
                                                            targetParentID,
                                                            ref entriesFound);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME, $"Error extracting entries from leaf node {leafNode}");

                        return errno;
                    }

                    // Check if next leaf node contains entries with targetParentID
                    if(leafDesc.ndFLink == 0) break; // Last leaf node reached

                    uint nextLeafNode = leafDesc.ndFLink;
                    errno = ReadNode(nextLeafNode, bthdr.bthNodeSize, out byte[] nextLeafData);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME, $"Failed to read next leaf node {nextLeafNode}");

                        break;
                    }

                    // If the next leaf doesn't contain targetParentID, we're done
                    if(!LeafContainsTargetParentID(nextLeafData, bthdr.bthNodeSize, targetParentID))
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          $"Next leaf node {nextLeafNode} doesn't contain parentID={targetParentID}, stopping traversal");

                        break;
                    }

                    leafNode = nextLeafNode;
                }

                AaruLogging.Debug(MODULE_NAME, $"Root directory caching complete: {entriesFound} entries found");

                return ErrorNumber.NoError;
            }

            // This is an index node - find the appropriate child pointer for targetParentID
            errno = FindIndexPointer(nodeData, bthdr.bthNodeSize, targetParentID, out currentNode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Could not find appropriate index pointer for target parentID");

                return errno;
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Failed to reach a leaf node");

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Checks if a leaf node contains any entries with a specific parentID</summary>
    bool LeafContainsTargetParentID(byte[] leafNode, ushort nodeSize, uint targetParentID)
    {
        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafNode,
                                                                  0,
                                                                  System.Runtime.InteropServices.Marshal
                                                                        .SizeOf(typeof(NodeDescriptor)));

        if(nodeDesc.ndType != NodeType.ndLeafNode) return false;

        // Quick scan for targetParentID
        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            var offsetPos = (ushort)(nodeSize - 2 - i * 2);

            if(offsetPos < 14 || offsetPos >= nodeSize) continue;

            var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPos);

            if(recordOffset < 14 || recordOffset >= nodeSize) continue;

            var keyParentID = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);

            if(keyParentID == targetParentID) return true;
        }

        return false;
    }


    /// <summary>Extracts all directory and file entries from a leaf node matching a given parent ID</summary>
    ErrorNumber ExtractDirectoryEntriesFromLeaf(byte[]     leafNode, ushort nodeSize, uint targetParentID,
                                                ref ushort entriesFound)
    {
        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafNode,
                                                                  0,
                                                                  System.Runtime.InteropServices.Marshal
                                                                        .SizeOf(typeof(NodeDescriptor)));

        if(nodeDesc.ndType != NodeType.ndLeafNode) return ErrorNumber.InvalidArgument;


        // Scan ALL records in this leaf node
        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            // Offset table at end of node, growing backwards
            var offsetPos = (ushort)(nodeSize - 2 - i * 2);

            if(offsetPos < 14 || offsetPos >= nodeSize) continue;

            // Read offset table as big-endian
            var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPos);

            if(recordOffset < 14 || recordOffset >= nodeSize) continue;

            // Parse the key: keyLen | reserved | parentID | name | ...
            byte keyLen      = leafNode[recordOffset];
            var  keyParentID = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);
            byte nameLen     = leafNode[recordOffset                                 + 6];

            // Only process records with parentID = targetParentID
            if(keyParentID != targetParentID) continue;


            // Calculate key size for reading data
            var keySize    = (ushort)(keyLen + 1 + 1 & ~1);
            int dataOffset = recordOffset + keySize;

            if(dataOffset + 2 > leafNode.Length) continue;

            byte recordType = leafNode[dataOffset];

            // Extract the name from the key
            string entryName = null;

            if(nameLen > 0 && nameLen <= 31)
            {
                var nameBytes = new byte[nameLen];
                Array.Copy(leafNode, recordOffset + 7, nameBytes, 0, nameLen);
                entryName = _encoding.GetString(nameBytes);
            }

            AaruLogging.Debug(MODULE_NAME,
                              $"Processing entry: name='{entryName}', type={recordType}, dataOffset={dataOffset}");

            // Process directory records (type 1)
            if(recordType == kCatalogRecordTypeDirectory)
            {
                int cdrDirRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrDirRec));

                if(dataOffset + cdrDirRecSize > leafNode.Length)
                {
                    AaruLogging.Debug(MODULE_NAME, $"Insufficient data for directory record at offset {dataOffset}");

                    continue;
                }

                CdrDirRec dirRec =
                    Marshal.ByteArrayToStructureBigEndian<CdrDirRec>(leafNode, dataOffset, cdrDirRecSize);

                var entry = new DirectoryEntry
                {
                    Name               = entryName,
                    CNID               = dirRec.dirDirID,
                    ParentID           = keyParentID,
                    Type               = 1,
                    Valence            = dirRec.dirVal,
                    CreationDate       = dirRec.dirCrDat,
                    ModificationDate   = dirRec.dirMdDat,
                    BackupDate         = dirRec.dirBkDat,
                    FinderInfo         = dirRec.dirUsrInfo,
                    ExtendedFinderInfo = dirRec.dirFndrInfo
                };

                if(!string.IsNullOrEmpty(entryName))
                {
                    _rootDirectoryCache[entryName] = entry;
                    entriesFound++;
                    AaruLogging.Debug(MODULE_NAME, $"Cached directory entry: {entryName} (CNID={dirRec.dirDirID})");
                }

                continue;
            }

            // Process file records (type 2)
            if(recordType == kCatalogRecordTypeFile)
            {
                int cdrFilRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrFilRec));

                if(dataOffset + cdrFilRecSize > leafNode.Length)
                {
                    AaruLogging.Debug(MODULE_NAME, $"Insufficient data for file record at offset {dataOffset}");

                    continue;
                }

                CdrFilRec filRec =
                    Marshal.ByteArrayToStructureBigEndian<CdrFilRec>(leafNode, dataOffset, cdrFilRecSize);

                var entry = new FileEntry
                {
                    Name                     = entryName,
                    CNID                     = filRec.filFlNum,
                    ParentID                 = keyParentID,
                    Type                     = 2,
                    FinderInfo               = filRec.filUsrWds,
                    ExtendedFinderInfo       = filRec.filFndrInfo,
                    DataForkLogicalSize      = filRec.filLgLen,
                    DataForkPhysicalSize     = filRec.filPyLen,
                    DataForkStartBlock       = filRec.filStBlk,
                    ResourceForkLogicalSize  = filRec.filRLgLen,
                    ResourceForkPhysicalSize = filRec.filRPyLen,
                    ResourceForkStartBlock   = filRec.filRStBlk,
                    CreationDate             = filRec.filCrDat,
                    ModificationDate         = filRec.filMdDat,
                    BackupDate               = filRec.filBkDat
                };

                if(!string.IsNullOrEmpty(entryName))
                {
                    _rootDirectoryCache[entryName] = entry;
                    entriesFound++;
                    AaruLogging.Debug(MODULE_NAME, $"Cached file entry: {entryName} (CNID={filRec.filFlNum})");
                }

                continue;
            }

            // Skip thread records (type 3 and 4) as they don't represent actual directory entries
            if(recordType == kCatalogRecordTypeDirectoryThread || recordType == kCatalogRecordTypeFileThread)
            {
                AaruLogging.Debug(MODULE_NAME, $"Skipping thread record (type {recordType})");

                continue;
            }

            AaruLogging.Debug(MODULE_NAME, $"Unknown record type {recordType} at offset {dataOffset}");
        }

        return ErrorNumber.NoError;
    }
}