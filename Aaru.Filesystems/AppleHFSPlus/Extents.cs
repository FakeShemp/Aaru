// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Extents.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Aaru.Filesystems;

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <summary>
    ///     Reads overflow extents from the Extents Overflow File for a resource fork.
    ///     The Extents Overflow File is a B-Tree that stores additional extent records for files
    ///     that have more than 8 extents (the maximum that can be stored in the catalog file).
    /// </summary>
    /// <param name="fileEntry">The file entry containing the resource fork</param>
    /// <param name="allExtents">List to append overflow extents to</param>
    /// <returns>Error number</returns>
    private ErrorNumber ReadResourceForkOverflowExtents(FileEntry fileEntry, List<HFSPlusExtentDescriptor> allExtents)
    {
        if(_volumeHeader.extentsFile.totalBlocks == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadResourceForkOverflowExtents: No Extents Overflow File present");

            return ErrorNumber.NoError;
        }

        // Ensure the Extents Overflow File header is loaded
        ErrorNumber headerErr = EnsureExtentsFileHeaderLoaded();

        if(headerErr != ErrorNumber.NoError) return headerErr;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadResourceForkOverflowExtents: Searching Extents Overflow File for resource fork extents (CNID={0})",
                          fileEntry.CNID);

        // Search the Extents Overflow File B-Tree for extent records with:
        // - CNID = fileEntry.CNID
        // - ForkType = 0xFF (resource fork)
        // The search uses a custom predicate that looks for extent records
        ErrorNumber errno = SearchExtentsOverflowFile(fileEntry.CNID, 0xFF, allExtents);

        return errno;
    }

    /// <summary>
    ///     Searches the Extents Overflow File B-Tree for extent records matching a CNID and fork type.
    /// </summary>
    /// <param name="cnid">Catalog Node ID to search for</param>
    /// <param name="forkType">Fork type (0 for data, 0xFF for resource)</param>
    /// <param name="allExtents">List to append found extents to</param>
    /// <returns>Error number</returns>
    private ErrorNumber SearchExtentsOverflowFile(uint cnid, byte forkType, List<HFSPlusExtentDescriptor> allExtents) =>

        // Traverse the Extents Overflow File B-Tree from the root
        TraverseExtentsOverflowFile(_extentsFileHeader.rootNode, cnid, forkType, allExtents);

    /// <summary>
    ///     Recursively traverses the Extents Overflow File B-Tree to find extent records.
    /// </summary>
    private ErrorNumber TraverseExtentsOverflowFile(uint nodeNumber, uint targetCNID, byte targetForkType,
                                                    List<HFSPlusExtentDescriptor> allExtents)
    {
        ErrorNumber errno = ReadExtentsFileNode(nodeNumber, out byte[] nodeData);

        if(errno != ErrorNumber.NoError) return errno;

        int ndSize = Marshal.SizeOf(typeof(BTNodeDescriptor));

        if(nodeData.Length < ndSize) return ErrorNumber.InvalidArgument;

        BTNodeDescriptor nodeDesc =
            Helpers.Marshal.ByteArrayToStructureBigEndian<BTNodeDescriptor>(nodeData, 0, ndSize);

        switch(nodeDesc.kind)
        {
            case BTNodeKind.kBTLeafNode:
            {
                // Process leaf node - extract extent records
                ushort numRecords = nodeDesc.numRecords;

                for(ushort i = 0; i < numRecords; i++)
                {
                    int offsetPointerOffset = _extentsFileHeader.nodeSize - 2 * (i + 1);

                    if(offsetPointerOffset < 0 || offsetPointerOffset + 2 > nodeData.Length) continue;

                    var recordOffset = BigEndianBitConverter.ToUInt16(nodeData, offsetPointerOffset);

                    if(recordOffset >= nodeData.Length || recordOffset + 10 > nodeData.Length) continue;

                    // Parse extent key: keyLength(2) + CNID(4) + forkType(1) + padding(1) + startBlock(4)
                    var  keyLength      = BigEndianBitConverter.ToUInt16(nodeData, recordOffset);
                    var  recordCNID     = BigEndianBitConverter.ToUInt32(nodeData, recordOffset + 2);
                    byte recordForkType = nodeData[recordOffset                                 + 6];

                    // Check if this record matches our search criteria
                    if(recordCNID != targetCNID || recordForkType != targetForkType) continue;

                    // Found an extent record - parse the extent data
                    int extentDataOffset = recordOffset + 2 + keyLength;

                    if(extentDataOffset + Marshal.SizeOf(typeof(HFSPlusExtentRecord)) > nodeData.Length) continue;

                    HFSPlusExtentRecord extentRecord =
                        Helpers.Marshal.ByteArrayToStructureBigEndian<HFSPlusExtentRecord>(nodeData,
                            extentDataOffset,
                            Marshal.SizeOf(typeof(HFSPlusExtentRecord)));

                    // Add all extents from this record
                    foreach(HFSPlusExtentDescriptor extent in extentRecord.extentDescriptors.TakeWhile(static extent =>
                                extent.blockCount != 0))
                    {
                        allExtents.Add(extent);

                        AaruLogging.Debug(MODULE_NAME,
                                          "TraverseExtentsOverflowFile: Added overflow extent: startBlock={0}, blockCount={1}",
                                          extent.startBlock,
                                          extent.blockCount);
                    }
                }

                return ErrorNumber.NoError;
            }
            case BTNodeKind.kBTIndexNode:
            {
                // Index node - traverse child nodes
                // For now, traverse all children by looking at record offsets
                ushort numRecords = nodeDesc.numRecords;

                for(ushort i = 0; i < numRecords; i++)
                {
                    int offsetPointerOffset = _extentsFileHeader.nodeSize - 2 * (i + 1);

                    if(offsetPointerOffset < 0 || offsetPointerOffset + 2 > nodeData.Length) continue;

                    var recordOffset = BigEndianBitConverter.ToUInt16(nodeData, offsetPointerOffset);

                    if(recordOffset >= nodeData.Length || recordOffset + 2 > nodeData.Length) continue;

                    // Parse index record key to get child pointer
                    var keyLength      = BigEndianBitConverter.ToUInt16(nodeData, recordOffset);
                    int childPtrOffset = recordOffset + 2 + keyLength;

                    if(childPtrOffset + 4 > nodeData.Length) continue;

                    var childNode = BigEndianBitConverter.ToUInt32(nodeData, childPtrOffset);

                    // Recursively traverse the child node
                    ErrorNumber childErr =
                        TraverseExtentsOverflowFile(childNode, targetCNID, targetForkType, allExtents);

                    if(childErr != ErrorNumber.NoError) return childErr;
                }

                return ErrorNumber.NoError;
            }
            default:
                return ErrorNumber.InvalidArgument;
        }
    }

    /// <summary>
    ///     Ensures the Extents Overflow File B-Tree header has been read and cached.
    ///     This is called lazily on first access to the Extents Overflow File.
    /// </summary>
    /// <returns>Error number</returns>
    private ErrorNumber EnsureExtentsFileHeaderLoaded()
    {
        if(_extentsFileHeader.rootNode != 0) return ErrorNumber.NoError; // Already loaded

        if(_volumeHeader.extentsFile.totalBlocks == 0) return ErrorNumber.InvalidArgument; // No Extents Overflow File

        AaruLogging.Debug(MODULE_NAME, "EnsureExtentsFileHeaderLoaded: Reading Extents Overflow File header");

        // The Extents Overflow File is similar to the Catalog File - it's a B-Tree
        // Read the first node (node 0) which contains the header
        ErrorNumber errno = ReadExtentsFileNode(0, out byte[] headerNode);

        if(errno != ErrorNumber.NoError) return errno;

        int headerSize = Marshal.SizeOf(typeof(BTHeaderRec));

        if(headerNode.Length < 14 + headerSize) // 14 bytes for node descriptor + header
            return ErrorNumber.InvalidArgument;

        // Parse the B-Tree header (starts after node descriptor)
        _extentsFileHeader = Helpers.Marshal.ByteArrayToStructureBigEndian<BTHeaderRec>(headerNode, 14, headerSize);

        AaruLogging.Debug(MODULE_NAME,
                          "EnsureExtentsFileHeaderLoaded: Extents File B-Tree header: depth={0}, rootNode={1}, nodeSize={2}",
                          _extentsFileHeader.treeDepth,
                          _extentsFileHeader.rootNode,
                          _extentsFileHeader.nodeSize);

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Reads a node from the Extents Overflow File by node number.
    ///     Similar to reading catalog nodes but uses the Extents File fork data.
    /// </summary>
    /// <param name="nodeNumber">The node number to read</param>
    /// <param name="nodeData">The node data read from disk</param>
    /// <returns>Error number</returns>
    private ErrorNumber ReadExtentsFileNode(uint nodeNumber, out byte[] nodeData)
    {
        nodeData = null;

        HFSPlusForkData extentsFork = _volumeHeader.extentsFile;

        if(extentsFork.extents.extentDescriptors == null || extentsFork.extents.extentDescriptors.Length == 0)
            return ErrorNumber.InvalidArgument;

        // Read the B-Tree header first if not already loaded
        if(_extentsFileHeader.rootNode == 0)
        {
            ErrorNumber headerErr = EnsureExtentsFileHeaderLoaded();

            if(headerErr != ErrorNumber.NoError) return headerErr;
        }

        // Calculate byte offset of node within the extents file
        ulong nodeOffset = (ulong)nodeNumber * _extentsFileHeader.nodeSize;

        // Find which extent contains this offset
        ulong currentOffset = 0;

        foreach(HFSPlusExtentDescriptor extent in extentsFork.extents.extentDescriptors)
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

                uint sectorsToRead = (_extentsFileHeader.nodeSize + byteOffset + _sectorSize - 1) / _sectorSize;

                ErrorNumber errno = _imagePlugin.ReadSectors(deviceSector,
                                                             false,
                                                             sectorsToRead,
                                                             out byte[] sectorData,
                                                             out _);

                if(errno != ErrorNumber.NoError) return errno;

                if(sectorData.Length < byteOffset + _extentsFileHeader.nodeSize) return ErrorNumber.InvalidArgument;

                nodeData = new byte[_extentsFileHeader.nodeSize];
                Array.Copy(sectorData, (int)byteOffset, nodeData, 0, _extentsFileHeader.nodeSize);

                return ErrorNumber.NoError;
            }

            currentOffset += extentSizeInBytes;
        }

        return ErrorNumber.InvalidArgument;
    }
}