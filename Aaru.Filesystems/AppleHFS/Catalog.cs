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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class AppleHFS
{
    /// <summary>Caches the root directory from the catalog B-Tree</summary>
    ErrorNumber CacheRootDirectory()
    {
        // Use the catalog B-tree header that was already validated in Mount.cs
        BTHdrRed bthdr = _catalogBTreeHeader;

        AaruLogging.Debug(MODULE_NAME,
                          $"Catalog B-Tree: depth={bthdr.bthDepth}, rootNode={bthdr.bthRoot}, nodeSize={bthdr.bthNodeSize}");

        AaruLogging.Debug(MODULE_NAME,
                          $"B-Tree header details: numRecs={bthdr.bthNRecs}, firstLeaf={bthdr.bthFNode}, lastLeaf={bthdr.bthLNode}, totalNodes={bthdr.bthNNodes}");

        ErrorNumber errno;

        // **BRUTE FORCE APPROACH**: Scan all leaf nodes from firstLeaf to lastLeaf looking for CNID=2
        // macOS appears to do this rather than strict B-tree pointer following
        // Skip if leaf range is invalid (firstLeaf > lastLeaf means empty or unusual structure)
        if(bthdr.bthFNode <= bthdr.bthLNode)
        {
            AaruLogging.Debug(MODULE_NAME,
                              $"Scanning leaf nodes from {bthdr.bthFNode} to {bthdr.bthLNode} for CNID={kRootCnid} (root)");

            for(uint leafNode = bthdr.bthFNode; leafNode <= bthdr.bthLNode; leafNode++)
            {
                ErrorNumber scanErr = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] leafData);

                if(scanErr != ErrorNumber.NoError) continue;

                NodeDescriptor leafDesc =
                    Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafData,
                                                                          0,
                                                                          System.Runtime.InteropServices.Marshal
                                                                             .SizeOf(typeof(NodeDescriptor)));

                if(leafDesc.ndType != NodeType.ndLeafNode) continue;

                // Search this leaf for CNID=2
                ErrorNumber foundErr = FindRootInLeaf(leafData, bthdr.bthNodeSize, out CdrDirRec rootRec);

                if(foundErr == ErrorNumber.NoError)
                {
                    _rootDirectory = rootRec;

                    AaruLogging.Debug(MODULE_NAME,
                                      $"Found root directory (CNID={kRootCnid}) in leaf node {leafNode}: {rootRec.dirVal} entries");

                    return ErrorNumber.NoError;
                }
            }
        }
        else
        {
            // Leaf range is invalid - do brute-force scan of ALL nodes
            AaruLogging.Debug(MODULE_NAME,
                              $"Leaf node range invalid (firstLeaf={bthdr.bthFNode} > lastLeaf={bthdr.bthLNode}), scanning ALL nodes for root");

            uint maxNodes = bthdr.bthNNodes > 0 ? bthdr.bthNNodes : 2000;

            for(uint nodeNum = 0; nodeNum < maxNodes; nodeNum++)
            {
                ErrorNumber scanErr = ReadNode(nodeNum, bthdr.bthNodeSize, out byte[] nodeData);

                if(scanErr != ErrorNumber.NoError) continue;

                NodeDescriptor nodeDesc =
                    Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData,
                                                                          0,
                                                                          System.Runtime.InteropServices.Marshal
                                                                             .SizeOf(typeof(NodeDescriptor)));

                if(nodeDesc.ndType != NodeType.ndLeafNode) continue;

                // Search this leaf for CNID=2
                ErrorNumber foundErr = FindRootInLeaf(nodeData, bthdr.bthNodeSize, out CdrDirRec rootRec);

                if(foundErr == ErrorNumber.NoError)
                {
                    _rootDirectory = rootRec;

                    AaruLogging.Debug(MODULE_NAME,
                                      $"Found root directory (CNID={kRootCnid}) via brute-force in node {nodeNum}: {rootRec.dirVal} entries");

                    return ErrorNumber.NoError;
                }
            }

            AaruLogging.Debug(MODULE_NAME, $"Brute-force scan of {maxNodes} nodes did not find root directory");
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory (CNID=2) not found in any leaf node. Falling back to standard tree traversal.");

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
                              $"Level {level}: Node {currentNode}, type={nodeDesc.ndType} ({(nodeDesc.ndType == NodeType.ndLeafNode ? "LEAF" : nodeDesc.ndType == NodeType.ndIndxNode ? "INDEX" : "UNKNOWN")}), nRecs={nodeDesc.ndNRecs}, fLink={nodeDesc.ndFLink}, bLink={nodeDesc.ndBLink}");

            // Validate node type - must be leaf (-1) or index (0)
            if(nodeDesc.ndType != NodeType.ndLeafNode && nodeDesc.ndType != NodeType.ndIndxNode)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  $"Invalid node type {nodeDesc.ndType} in node {currentNode}, stopping traversal");

                return ErrorNumber.InvalidArgument;
            }

            // If this is a leaf node, we've reached the end
            if(nodeDesc.ndType == NodeType.ndLeafNode)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  $"Reached leaf node at level {level} (expected depth={bthdr.bthDepth}). This may indicate a malformed B-tree.");

                errno = FindRootInLeaf(nodeData, bthdr.bthNodeSize, out CdrDirRec rootRec);

                if(errno == ErrorNumber.NoError)
                {
                    _rootDirectory = rootRec;

                    AaruLogging.Debug(MODULE_NAME,
                                      $"Root directory found: CNID={rootRec.dirDirID}, {rootRec.dirVal} entries");

                    return ErrorNumber.NoError;
                }

                // Root directory not found in this leaf node, try traversing linked leaves
                AaruLogging.Debug(MODULE_NAME,
                                  $"Root directory not found in leaf node {currentNode}, traversing sibling nodes");

                // Try forward link first
                if(nodeDesc.ndFLink != 0)
                {
                    uint      nextNode    = nodeDesc.ndFLink;
                    var       attempts    = 0;
                    const int maxAttempts = 100; // Prevent infinite loops

                    while(nextNode != 0 && attempts < maxAttempts)
                    {
                        attempts++;
                        ErrorNumber readErr = ReadNode(nextNode, bthdr.bthNodeSize, out byte[] nextNodeData);

                        if(readErr != ErrorNumber.NoError)
                        {
                            AaruLogging.Debug(MODULE_NAME, $"Failed to read sibling node {nextNode}");

                            break;
                        }

                        NodeDescriptor nextDesc =
                            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nextNodeData,
                                0,
                                System.Runtime.InteropServices.Marshal.SizeOf(typeof(NodeDescriptor)));

                        // Stop if we hit an invalid node
                        if(nextDesc.ndType != NodeType.ndLeafNode)
                        {
                            AaruLogging.Debug(MODULE_NAME,
                                              $"Sibling node {nextNode} is not a leaf node (type={nextDesc.ndType})");

                            break;
                        }

                        // Try to find root in this sibling
                        ErrorNumber siblingErr =
                            FindRootInLeaf(nextNodeData, bthdr.bthNodeSize, out CdrDirRec siblingRoot);

                        if(siblingErr == ErrorNumber.NoError)
                        {
                            _rootDirectory = siblingRoot;

                            AaruLogging.Debug(MODULE_NAME,
                                              $"Root directory found in sibling node {nextNode}: CNID={siblingRoot.dirDirID}, {siblingRoot.dirVal} entries");

                            return ErrorNumber.NoError;
                        }

                        nextNode = nextDesc.ndFLink;
                    }
                }

                // Try backward link as fallback
                if(nodeDesc.ndBLink != 0)
                {
                    uint      prevNode    = nodeDesc.ndBLink;
                    var       attempts    = 0;
                    const int maxAttempts = 100;

                    while(prevNode != 0 && attempts < maxAttempts)
                    {
                        attempts++;
                        ErrorNumber readErr = ReadNode(prevNode, bthdr.bthNodeSize, out byte[] prevNodeData);

                        if(readErr != ErrorNumber.NoError)
                        {
                            AaruLogging.Debug(MODULE_NAME, $"Failed to read backward node {prevNode}");

                            break;
                        }

                        NodeDescriptor prevDesc =
                            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(prevNodeData,
                                0,
                                System.Runtime.InteropServices.Marshal.SizeOf(typeof(NodeDescriptor)));

                        // Stop if we hit an invalid node
                        if(prevDesc.ndType != NodeType.ndLeafNode)
                        {
                            AaruLogging.Debug(MODULE_NAME,
                                              $"Backward node {prevNode} is not a leaf node (type={prevDesc.ndType})");

                            break;
                        }

                        // Try to find root in this backward node
                        ErrorNumber backErr = FindRootInLeaf(prevNodeData, bthdr.bthNodeSize, out CdrDirRec backRoot);

                        if(backErr == ErrorNumber.NoError)
                        {
                            _rootDirectory = backRoot;

                            AaruLogging.Debug(MODULE_NAME,
                                              $"Root directory found in backward node {prevNode}: CNID={backRoot.dirDirID}, {backRoot.dirVal} entries");

                            return ErrorNumber.NoError;
                        }

                        prevNode = prevDesc.ndBLink;
                    }
                }

                // No more leaf nodes to check
                AaruLogging.Debug(MODULE_NAME, "No root directory found in any accessible leaf node");

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

        // All HFS offsets are in 512-byte sectors:
        // - drAlBlSt: Start of allocation blocks (from partition start, in 512-byte sectors)
        // - xdrStABN: Start allocation block number
        // - drAlBlkSiz: Allocation block size in bytes
        ulong allocBlockSectorSize = _mdb.drAlBlkSiz / 512;

        // Calculate HFS offset for this node (in 512-byte sectors)
        // Node offset from start of catalog file (in bytes)
        ulong nodeByteOffset = nodeNumber * 512UL;

        // Catalog file starts at this offset from partition (in 512-byte sectors)
        ulong catalogSectorOffset512 = _mdb.drAlBlSt + catalogExtent.xdrStABN * allocBlockSectorSize;

        // Total offset for this node
        ulong nodeOffsetSector512 = catalogSectorOffset512 + nodeByteOffset / 512;

        // Convert to device sector address
        HfsOffsetToDeviceSector(nodeOffsetSector512, out ulong deviceSector, out uint byteOffset);

        AaruLogging.Debug(MODULE_NAME, $"ReadNode: num={nodeNumber}, deviceSector={deviceSector}, offset={byteOffset}");

        // Read sectors containing the node
        ErrorNumber errno = _imagePlugin.ReadSectors(deviceSector, false, 2, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Extract node data from the appropriate offset
        if(sectorData.Length < (int)byteOffset + 512) return ErrorNumber.InvalidArgument;

        nodeData = new byte[512];
        Array.Copy(sectorData, (int)byteOffset, nodeData, 0, 512);

        return ErrorNumber.NoError;
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
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      $"Record {i}: dataOffset {dataOffset} + {cdrDirRecSize} > leafNode.Length {leafNode.Length}");
                }
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

        // Third pass: As final fallback, use any directory record with parentID=1 (root parent)
        // Some non-standard volumes might not follow the CNID=2 convention
        AaruLogging.Debug(MODULE_NAME, "Third pass: using any directory record with parentID=1 as fallback root");

        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            var offsetPos = (ushort)(nodeSize - 2 - i * 2);

            if(offsetPos < 14 || offsetPos >= nodeSize) continue;

            var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPos);

            if(recordOffset < 14 || recordOffset >= nodeSize) continue;

            // Parse the key
            byte keyLen      = leafNode[recordOffset];
            var  keyParentID = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);
            byte nameLen     = leafNode[recordOffset                                 + 6];

            // Skip named entries - we want the thread/root entries with empty names
            if(nameLen != 0) continue;

            // Calculate key size
            var keySize    = (ushort)(keyLen + 1 + 1 & ~1);
            int dataOffset = recordOffset + keySize;

            if(dataOffset + 2 > leafNode.Length) continue;

            byte recordType = leafNode[dataOffset];

            // Only look at directory records (type 1)
            if(recordType != kCatalogRecordTypeDirectory) continue;

            // Only records with parentID=1 (root parent)
            if(keyParentID != kRootParentCnid) continue;

            // Parse the directory record
            int cdrDirRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrDirRec));

            if(dataOffset + cdrDirRecSize > leafNode.Length) continue;

            rootRec = Marshal.ByteArrayToStructureBigEndian<CdrDirRec>(leafNode, dataOffset, cdrDirRecSize);

            AaruLogging.Debug(MODULE_NAME,
                              $"Record {i} (third pass/fallback): Found directory with parentID=1, CNID={rootRec.dirDirID}");

            // Only accept CNID=2 as the real root
            if(rootRec.dirDirID == kRootCnid) return ErrorNumber.NoError;
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

        AaruLogging.Debug(MODULE_NAME,
                          $"Root directory details: dirDirID={_rootDirectory.dirDirID}, dirVal={_rootDirectory.dirVal}");

        // Use the catalog B-tree header that was already validated and recovered in Mount.cs
        BTHdrRed bthdr = _catalogBTreeHeader;

        AaruLogging.Debug(MODULE_NAME,
                          $"Using cached B-tree header: firstLeaf={bthdr.bthFNode}, lastLeaf={bthdr.bthLNode}");

        ErrorNumber errno;

        // **BRUTE FORCE APPROACH**: Since we found the root by scanning leaf nodes,
        // scan through all leaf nodes looking for entries with parentID = targetParentID (which is 2 for root)
        // Skip if leaf range is invalid
        if(bthdr.bthFNode <= bthdr.bthLNode)
        {
            ushort entriesFound = 0;

            AaruLogging.Debug(MODULE_NAME,
                              $"CacheRootDirectoryEntries brute-force: Scanning leaf nodes {bthdr.bthFNode} to {bthdr.bthLNode}");

            for(uint leafNode = bthdr.bthFNode; leafNode <= bthdr.bthLNode; leafNode++)
            {
                errno = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] leafData);

                if(errno != ErrorNumber.NoError) continue;

                NodeDescriptor leafDesc =
                    Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafData,
                                                                          0,
                                                                          System.Runtime.InteropServices.Marshal
                                                                             .SizeOf(typeof(NodeDescriptor)));

                if(leafDesc.ndType != NodeType.ndLeafNode) continue;

                AaruLogging.Debug(MODULE_NAME, $"Processing leaf node {leafNode}, {leafDesc.ndNRecs} records");

                // Extract all entries with parentID = targetParentID from this leaf node
                errno = ExtractDirectoryEntriesFromLeaf(leafData, bthdr.bthNodeSize, targetParentID, ref entriesFound);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, $"Error extracting entries from leaf node {leafNode}");

                    continue;
                }

                AaruLogging.Debug(MODULE_NAME,
                                  $"After leaf {leafNode}, entriesFound={entriesFound}, expectedCount={expectedCount}");

                // Check if we've found all entries
                if(entriesFound >= expectedCount)
                {
                    AaruLogging.Debug(MODULE_NAME, $"Found all expected entries: {entriesFound}/{expectedCount}");

                    break;
                }
            }

            AaruLogging.Debug(MODULE_NAME,
                              $"Root directory caching complete: {entriesFound} entries found, cache has {_rootDirectoryCache.Count} items");

            return ErrorNumber.NoError;
        }

        // Leaf range is invalid, fall back to brute-force scan of ALL nodes
        AaruLogging.Debug(MODULE_NAME,
                          $"Leaf node range invalid (firstLeaf={bthdr.bthFNode} > lastLeaf={bthdr.bthLNode}), using brute-force scan of all nodes");

        // Brute-force scan ALL nodes looking for leaf nodes with our targetParentID
        {
            ushort entriesFound = 0;
            uint   maxNodes     = bthdr.bthNNodes > 0 ? bthdr.bthNNodes : 2000;

            AaruLogging.Debug(MODULE_NAME,
                              $"CacheRootDirectoryEntries brute-force: bthNNodes={bthdr.bthNNodes}, maxNodes={maxNodes}, scanning for parentID={targetParentID}");

            uint leafNodesScanned = 0;

            for(uint nodeNum = 0; nodeNum < maxNodes; nodeNum++)
            {
                errno = ReadNode(nodeNum, bthdr.bthNodeSize, out byte[] nodeData);

                if(errno != ErrorNumber.NoError) continue;

                NodeDescriptor nodeDesc =
                    Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData,
                                                                          0,
                                                                          System.Runtime.InteropServices.Marshal
                                                                             .SizeOf(typeof(NodeDescriptor)));

                if(nodeDesc.ndType != NodeType.ndLeafNode) continue;

                leafNodesScanned++;
                ushort beforeCount = entriesFound;

                // Extract all entries with parentID = targetParentID from this leaf node
                errno = ExtractDirectoryEntriesFromLeaf(nodeData, bthdr.bthNodeSize, targetParentID, ref entriesFound);

                if(entriesFound > beforeCount)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      $"Node {nodeNum}: Found {entriesFound - beforeCount} matching entries (total now: {entriesFound})");
                }

                if(errno != ErrorNumber.NoError) continue;

                // Check if we've found all entries
                if(entriesFound >= expectedCount)
                {
                    AaruLogging.Debug(MODULE_NAME, $"Found all expected entries: {entriesFound}/{expectedCount}");

                    break;
                }
            }

            AaruLogging.Debug(MODULE_NAME,
                              $"Root directory caching complete: scanned {leafNodesScanned} leaf nodes, {entriesFound} entries found, cache has {_rootDirectoryCache.Count} items");

            if(entriesFound > 0 || _rootDirectoryCache.Count > 0) return ErrorNumber.NoError;
        }

        // If brute-force failed, try standard B-tree traversal as last resort
        AaruLogging.Debug(MODULE_NAME, "Brute-force scan found no entries, trying standard B-tree traversal");

        // Navigate B-Tree to find entries with parentID = targetParentID
        uint currentNode = bthdr.bthRoot;

        // Traverse from root to leaf
        for(var level = 0; level < bthdr.bthDepth; level++)
        {
            errno = ReadNode(currentNode, bthdr.bthNodeSize, out byte[] nodeData);

            if(errno != ErrorNumber.NoError) return errno;

            NodeDescriptor nodeDesc =
                Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData,
                                                                      0,
                                                                      System.Runtime.InteropServices.Marshal
                                                                            .SizeOf(typeof(NodeDescriptor)));

            // Reached a leaf node
            if(nodeDesc.ndType == NodeType.ndLeafNode)
            {
                // Extract all entries with parentID = targetParentID from this leaf and following siblings
                // First, backtrack to find the FIRST leaf node that contains targetParentID entries
                uint   leafNode     = currentNode;
                ushort entriesFound = 0;

                // Traverse backward to find the start of targetParentID entries
                while(nodeDesc.ndBLink != 0)
                {
                    errno = ReadNode(nodeDesc.ndBLink, bthdr.bthNodeSize, out byte[] backData);

                    if(errno != ErrorNumber.NoError) break;

                    NodeDescriptor backDesc =
                        Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(backData,
                                                                              0,
                                                                              System.Runtime.InteropServices.Marshal
                                                                                 .SizeOf(typeof(NodeDescriptor)));

                    // Check if previous leaf contains entries with parentID <= targetParentID
                    if(!LeafHasTargetOrEarlier(backData, bthdr.bthNodeSize, targetParentID))
                    {
                        // Previous leaf doesn't have our target, so current leaf is the start
                        break;
                    }

                    // Previous leaf has our target or earlier, so go back
                    leafNode = nodeDesc.ndBLink;
                    nodeDesc = backDesc;
                }

                // Now traverse forward and extract all entries
                while(leafNode != 0)
                {
                    errno = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] leafData);

                    if(errno != ErrorNumber.NoError) return errno;

                    NodeDescriptor leafDesc =
                        Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafData,
                                                                              0,
                                                                              System.Runtime.InteropServices.Marshal
                                                                                 .SizeOf(typeof(NodeDescriptor)));

                    // Extract entries from this leaf
                    errno = ExtractDirectoryEntriesFromLeaf(leafData,
                                                            bthdr.bthNodeSize,
                                                            targetParentID,
                                                            ref entriesFound);

                    if(errno != ErrorNumber.NoError) return errno;

                    // Check if we've found all entries
                    if(entriesFound >= expectedCount)
                    {
                        AaruLogging.Debug(MODULE_NAME, $"Found all expected entries: {entriesFound}/{expectedCount}");

                        break;
                    }

                    // Move to next leaf node
                    if(leafDesc.ndFLink == 0) break;

                    leafNode = leafDesc.ndFLink;
                    errno    = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] nextLeafData);

                    if(errno != ErrorNumber.NoError) break;

                    if(!LeafHasTargetParentID(nextLeafData, bthdr.bthNodeSize, targetParentID)) break;
                }

                AaruLogging.Debug(MODULE_NAME, $"Root directory caching complete: {entriesFound} entries found");

                return ErrorNumber.NoError;
            }

            // Index node - find the child pointer for targetParentID
            errno = FindIndexPointer(nodeData, bthdr.bthNodeSize, targetParentID, out currentNode);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Checks if a leaf node contains any entries with a specific parentID</summary>
    bool LeafHasTargetParentID(byte[] leafNode, ushort nodeSize, uint targetParentID)
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

    /// <summary>Checks if a leaf node contains entries with parentID less than or equal to target</summary>
    bool LeafHasTargetOrEarlier(byte[] leafNode, ushort nodeSize, uint targetParentID)
    {
        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafNode,
                                                                  0,
                                                                  System.Runtime.InteropServices.Marshal
                                                                        .SizeOf(typeof(NodeDescriptor)));

        if(nodeDesc.ndType != NodeType.ndLeafNode) return false;

        if(nodeDesc.ndNRecs == 0) return false;

        // Check first record (smallest parentID in this node)
        var offsetPos = (ushort)(nodeSize - 2);

        if(offsetPos < 14 || offsetPos >= nodeSize) return false;

        var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPos);

        if(recordOffset < 14 || recordOffset >= nodeSize) return false;

        var minParentID = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);

        return minParentID <= targetParentID;
    }

    /// <summary>Checks if a leaf node contains any entries with a specific parentID</summary>
    bool LeafContainsTargetParentID(byte[] leafNode, ushort nodeSize, uint targetParentID) =>
        LeafHasTargetParentID(leafNode, nodeSize, targetParentID);


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

            AaruLogging.Debug(MODULE_NAME,
                              $"Record {i}: keyParentID={keyParentID}, targetParentID={targetParentID}, nameLen={nameLen}");

            // Only process records with parentID = targetParentID
            if(keyParentID != targetParentID) continue;

            AaruLogging.Debug(MODULE_NAME, $"Record {i}: MATCH! Processing record with matching parentID");


            // Calculate key size for reading data
            var keySize    = (ushort)(keyLen + 1 + 1 & ~1);
            int dataOffset = recordOffset + keySize;

            if(dataOffset + 2 > leafNode.Length) continue;

            byte recordType = leafNode[dataOffset];

            // Extract the name from the key using proper encoding handling
            string entryName = ExtractCatalogName(leafNode, recordOffset + 7, nameLen);

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
                    DataForkExtents          = filRec.filExtRec,
                    ResourceForkLogicalSize  = filRec.filRLgLen,
                    ResourceForkPhysicalSize = filRec.filRPyLen,
                    ResourceForkStartBlock   = filRec.filRStBlk,
                    ResourceForkExtents      = filRec.filRExtRec,
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

    /// <summary>Extracts a catalog name from a byte array with proper encoding handling</summary>
    /// <param name="data">Source byte array</param>
    /// <param name="offset">Offset to name in the array</param>
    /// <param name="length">Length of the name in bytes</param>
    /// <returns>Decoded name string, or empty string if length is 0</returns>
    string ExtractCatalogName(byte[] data, int offset, byte length)
    {
        // HFS catalog names are not Pascal strings - they are just length-delimited
        // Valid names have length from 1 to 31 bytes
        if(length == 0 || length > 31 || offset < 0 || offset + length > data.Length) return "";

        // Extract the name bytes and decode using the configured encoding
        var nameBytes = new byte[length];
        Array.Copy(data, offset, nameBytes, 0, length);

        // Use StringHandlers.CToString for proper null-terminated string handling
        return StringHandlers.CToString(nameBytes, _encoding);
    }

    /// <summary>
    ///     Caches directory entries for a given CNID if not already cached.
    ///     Reuses existing root directory cache if CNID is kRootCnid.
    /// </summary>
    /// <param name="cnid">Catalog Node ID of the directory to cache</param>
    /// <returns>Error status</returns>
    ErrorNumber CacheDirectoryIfNeeded(uint cnid)
    {
        // Initialize directory cache dictionary if needed
        _directoryCaches ??= new Dictionary<uint, Dictionary<string, CatalogEntry>>();

        // Root directory is already cached in _rootDirectoryCache
        if(cnid == kRootCnid) return ErrorNumber.NoError;

        // Check if already cached
        if(_directoryCaches.ContainsKey(cnid)) return ErrorNumber.NoError;

        // Cache this directory
        return CacheDirectory(cnid);
    }

    /// <summary>
    ///     Caches all entries for a directory by its CNID.
    ///     Searches the catalog B-Tree for all records with the given parentID.
    /// </summary>
    /// <param name="cnid">Catalog Node ID (used as parentID for records) of the directory to cache</param>
    /// <returns>Error status</returns>
    ErrorNumber CacheDirectory(uint cnid)
    {
        _directoryCaches ??= new Dictionary<uint, Dictionary<string, CatalogEntry>>();

        // Use the catalog B-tree header that was already validated and recovered in Mount.cs
        BTHdrRed bthdr = _catalogBTreeHeader;

        var directoryEntries = new Dictionary<string, CatalogEntry>();

        ErrorNumber errno;

        // This is robust against malformed B-tree structures like the one in this image
        // Skip if leaf range is invalid
        if(bthdr.bthFNode <= bthdr.bthLNode)
        {
            uint targetParentID = cnid;

            for(uint leafNode = bthdr.bthFNode; leafNode <= bthdr.bthLNode; leafNode++)
            {
                errno = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] leafData);

                if(errno != ErrorNumber.NoError) continue;

                NodeDescriptor leafDesc =
                    Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafData,
                                                                          0,
                                                                          System.Runtime.InteropServices.Marshal
                                                                             .SizeOf(typeof(NodeDescriptor)));

                if(leafDesc.ndType != NodeType.ndLeafNode) continue;

                // Extract entries from this leaf
                errno = ExtractDirectoryEntriesFromLeafForCnid(leafData,
                                                               bthdr.bthNodeSize,
                                                               targetParentID,
                                                               directoryEntries);

                if(errno != ErrorNumber.NoError) continue;
            }

            // Cache the directory
            _directoryCaches[cnid] = directoryEntries;

            return ErrorNumber.NoError;
        }

        // Leaf range is invalid, fall back to standard B-tree traversal
        AaruLogging.Debug(MODULE_NAME,
                          $"Leaf node range invalid (firstLeaf={bthdr.bthFNode} > lastLeaf={bthdr.bthLNode}), using standard B-tree traversal");

        // Navigate B-Tree to find entries with parentID = cnid
        uint currentNode     = bthdr.bthRoot;
        uint targetParentID2 = cnid;

        // Traverse from root to leaf
        for(var level = 0; level < bthdr.bthDepth; level++)
        {
            errno = ReadNode(currentNode, bthdr.bthNodeSize, out byte[] nodeData);

            if(errno != ErrorNumber.NoError) return errno;

            NodeDescriptor nodeDesc =
                Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(nodeData,
                                                                      0,
                                                                      System.Runtime.InteropServices.Marshal
                                                                            .SizeOf(typeof(NodeDescriptor)));

            // Reached a leaf node
            if(nodeDesc.ndType == NodeType.ndLeafNode)
            {
                // Extract all entries with parentID = cnid from this leaf and following siblings
                // First, traverse backward to find the first leaf with targetParentID entries
                uint leafNode  = currentNode;
                var  leafCount = 0;

                AaruLogging.Debug(MODULE_NAME,
                                  $"CacheDirectory B-tree fallback: Starting leaf traversal for CNID={cnid}, first leaf={currentNode}");

                // Traverse backward to find first leaf with targetParentID
                while(nodeDesc.ndBLink != 0)
                {
                    errno = ReadNode(nodeDesc.ndBLink, bthdr.bthNodeSize, out byte[] backData);

                    if(errno != ErrorNumber.NoError) break;

                    NodeDescriptor backDesc =
                        Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(backData,
                                                                              0,
                                                                              System.Runtime.InteropServices.Marshal
                                                                                 .SizeOf(typeof(NodeDescriptor)));

                    if(!LeafHasTargetOrEarlier(backData, bthdr.bthNodeSize, targetParentID2)) break;

                    leafNode = nodeDesc.ndBLink;
                    nodeDesc = backDesc;
                }

                AaruLogging.Debug(MODULE_NAME, $"  Backward traversal found first leaf at {leafNode}");

                // Now traverse forward from the first leaf, extracting all entries for this CNID
                while(leafNode != 0)
                {
                    errno = ReadNode(leafNode, bthdr.bthNodeSize, out byte[] leafData);

                    if(errno != ErrorNumber.NoError) return errno;

                    NodeDescriptor leafDesc =
                        Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafData,
                                                                              0,
                                                                              System.Runtime.InteropServices.Marshal
                                                                                 .SizeOf(typeof(NodeDescriptor)));

                    AaruLogging.Debug(MODULE_NAME, $"  Checking leaf {leafNode}, next={leafDesc.ndFLink}");

                    // Extract entries from this leaf
                    errno = ExtractDirectoryEntriesFromLeafForCnid(leafData,
                                                                   bthdr.bthNodeSize,
                                                                   targetParentID2,
                                                                   directoryEntries);

                    if(errno != ErrorNumber.NoError) return errno;

                    leafCount++;

                    // Continue to next sibling leaf
                    if(leafDesc.ndFLink == 0)
                    {
                        AaruLogging.Debug(MODULE_NAME, "  No next leaf, stopping");

                        break;
                    }

                    leafNode = leafDesc.ndFLink;
                }

                AaruLogging.Debug(MODULE_NAME,
                                  $"CacheDirectory traversed {leafCount} leaves, found {directoryEntries.Count} total entries");

                // Cache the directory
                _directoryCaches[cnid] = directoryEntries;

                return ErrorNumber.NoError;
            }

            // Index node - find the child pointer for targetParentID
            errno = FindIndexPointer(nodeData, bthdr.bthNodeSize, targetParentID2, out currentNode);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>
    ///     Extracts directory entries from a leaf node for a specific CNID.
    ///     Similar to ExtractDirectoryEntriesFromLeaf but extracts to a provided dictionary.
    /// </summary>
    ErrorNumber ExtractDirectoryEntriesFromLeafForCnid(byte[] leafNode, ushort nodeSize, uint targetParentID,
                                                       Dictionary<string, CatalogEntry> entries)
    {
        NodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(leafNode,
                                                                  0,
                                                                  System.Runtime.InteropServices.Marshal
                                                                        .SizeOf(typeof(NodeDescriptor)));

        if(nodeDesc.ndType != NodeType.ndLeafNode) return ErrorNumber.InvalidArgument;

        var entriesInThisLeaf = 0;

        // Iterate through records in this leaf node
        for(ushort i = 0; i < nodeDesc.ndNRecs; i++)
        {
            var offsetPos = (ushort)(nodeSize - 2 - i * 2);

            if(offsetPos < 14 || offsetPos >= nodeSize) continue;

            var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPos);

            if(recordOffset < 14 || recordOffset >= nodeSize) continue;

            // Parse the key
            byte keyLen      = leafNode[recordOffset];
            var  keyParentID = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);

            // Stop if we've passed the target parentID
            if(keyParentID > targetParentID) break;

            // Skip if not our target parentID
            if(keyParentID != targetParentID) continue;

            // Extract the name
            byte   nameLen = leafNode[recordOffset                     + 6];
            string name    = ExtractCatalogName(leafNode, recordOffset + 7, nameLen);

            if(string.IsNullOrEmpty(name)) continue;

            entriesInThisLeaf++;

            // Key size (padded to word boundary)
            var keySize    = (ushort)(keyLen + 1 + 1 & ~1);
            int dataOffset = recordOffset + keySize;

            if(dataOffset >= leafNode.Length) continue;

            byte recordType = leafNode[dataOffset];

            CatalogEntry entry = null;

            // Parse based on record type
            if(recordType == kCatalogRecordTypeDirectory)
            {
                int cdrDirRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrDirRec));

                if(dataOffset + cdrDirRecSize <= leafNode.Length)
                {
                    CdrDirRec dirRec =
                        Marshal.ByteArrayToStructureBigEndian<CdrDirRec>(leafNode, dataOffset, cdrDirRecSize);

                    entry = new DirectoryEntry
                    {
                        Name               = name,
                        CNID               = dirRec.dirDirID,
                        ParentID           = keyParentID,
                        Type               = kCatalogRecordTypeDirectory,
                        Valence            = dirRec.dirVal,
                        CreationDate       = dirRec.dirCrDat,
                        ModificationDate   = dirRec.dirMdDat,
                        BackupDate         = dirRec.dirBkDat,
                        FinderInfo         = dirRec.dirUsrInfo,
                        ExtendedFinderInfo = dirRec.dirFndrInfo
                    };
                }
            }
            else if(recordType == kCatalogRecordTypeFile)
            {
                int cdrFilRecSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CdrFilRec));

                if(dataOffset + cdrFilRecSize <= leafNode.Length)
                {
                    CdrFilRec filRec =
                        Marshal.ByteArrayToStructureBigEndian<CdrFilRec>(leafNode, dataOffset, cdrFilRecSize);

                    entry = new FileEntry
                    {
                        Name                     = name,
                        CNID                     = filRec.filFlNum,
                        ParentID                 = keyParentID,
                        Type                     = kCatalogRecordTypeFile,
                        FinderInfo               = filRec.filUsrWds,
                        ExtendedFinderInfo       = filRec.filFndrInfo,
                        DataForkLogicalSize      = filRec.filLgLen,
                        DataForkPhysicalSize     = filRec.filPyLen,
                        DataForkStartBlock       = filRec.filStBlk,
                        DataForkExtents          = filRec.filExtRec,
                        ResourceForkLogicalSize  = filRec.filRLgLen,
                        ResourceForkPhysicalSize = filRec.filRPyLen,
                        ResourceForkStartBlock   = filRec.filRStBlk,
                        ResourceForkExtents      = filRec.filRExtRec,
                        CreationDate             = filRec.filCrDat,
                        ModificationDate         = filRec.filMdDat,
                        BackupDate               = filRec.filBkDat
                    };
                }
            }

            if(entry != null) entries[name] = entry;
        }

        AaruLogging.Debug(MODULE_NAME,
                          $"Extracted {entriesInThisLeaf} entries with parentID={targetParentID} from this leaf");

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Gets cached directory entries for a given CNID.
    ///     Returns the root directory cache if CNID is kRootCnid.
    /// </summary>
    /// <param name="cnid">Catalog Node ID of the directory</param>
    /// <returns>Dictionary of entries keyed by filename, or null if not cached</returns>
    Dictionary<string, CatalogEntry> GetDirectoryEntries(uint cnid)
    {
        if(cnid == kRootCnid) return _rootDirectoryCache;

        if(_directoryCaches == null) return null;

        _directoryCaches.TryGetValue(cnid, out Dictionary<string, CatalogEntry> entries);

        return entries;
    }
}