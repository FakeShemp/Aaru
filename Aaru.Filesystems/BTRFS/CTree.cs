// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : CTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Chunk tree and logical-to-physical address translation methods.
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
    /// <summary>
    ///     Parses the system chunk array embedded in the superblock to build the initial logical-to-physical address
    ///     mapping needed to bootstrap reading the chunk tree
    /// </summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseSystemChunkArray()
    {
        AaruLogging.Debug(MODULE_NAME, "Parsing system chunk array ({0} bytes)...", _superblock.n);

        _chunkMap = new List<ChunkMapping>();

        if(_superblock.chunkpairs is null || _superblock.n == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No system chunk array data");

            return ErrorNumber.InvalidArgument;
        }

        int diskKeySize = Marshal.SizeOf<DiskKey>();
        var offset      = 0;

        while(offset < _superblock.n)
        {
            if(offset + diskKeySize > _superblock.chunkpairs.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "System chunk array truncated at key offset {0}", offset);

                break;
            }

            DiskKey key =
                Marshal.ByteArrayToStructureLittleEndian<DiskKey>(_superblock.chunkpairs, offset, diskKeySize);

            offset += diskKeySize;

            if(key.type != BTRFS_CHUNK_ITEM_KEY)
            {
                AaruLogging.Debug(MODULE_NAME, "Unexpected key type {0} in system chunk array", key.type);

                break;
            }

            // Read the chunk item (without the inline stripe, we'll read that separately)
            int chunkBaseSize = Marshal.SizeOf<Chunk>() - Marshal.SizeOf<Stripe>();

            if(offset + chunkBaseSize > _superblock.chunkpairs.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "System chunk array truncated at chunk offset {0}", offset);

                break;
            }

            // Parse chunk header fields manually from the byte array
            var chunkLength     = BitConverter.ToUInt64(_superblock.chunkpairs, offset);
            var chunkOwner      = BitConverter.ToUInt64(_superblock.chunkpairs, offset + 8);
            var chunkStripeLen  = BitConverter.ToUInt64(_superblock.chunkpairs, offset + 16);
            var chunkType       = BitConverter.ToUInt64(_superblock.chunkpairs, offset + 24);
            var chunkIoAlign    = BitConverter.ToUInt32(_superblock.chunkpairs, offset + 32);
            var chunkIoWidth    = BitConverter.ToUInt32(_superblock.chunkpairs, offset + 36);
            var chunkSectorSize = BitConverter.ToUInt32(_superblock.chunkpairs, offset + 40);
            var numStripes      = BitConverter.ToUInt16(_superblock.chunkpairs, offset + 44);
            var subStripes      = BitConverter.ToUInt16(_superblock.chunkpairs, offset + 46);

            offset += 48; // Size of chunk header without stripes

            int stripeSize = Marshal.SizeOf<Stripe>();

            for(var i = 0; i < numStripes; i++)
            {
                if(offset + stripeSize > _superblock.chunkpairs.Length)
                {
                    AaruLogging.Debug(MODULE_NAME, "System chunk array truncated at stripe {0}", i);

                    break;
                }

                Stripe stripe =
                    Marshal.ByteArrayToStructureLittleEndian<Stripe>(_superblock.chunkpairs, offset, stripeSize);

                // We only use the first stripe for non-RAID (single device) mapping
                if(i == 0)
                {
                    _chunkMap.Add(new ChunkMapping
                    {
                        LogicalOffset  = key.offset,
                        Length         = chunkLength,
                        PhysicalOffset = stripe.offset,
                        DevId          = stripe.devid,
                        NumStripes     = numStripes,
                        StripeLen      = chunkStripeLen,
                        Type           = chunkType
                    });

                    AaruLogging.Debug(MODULE_NAME,
                                      "Chunk mapping: logical 0x{0:X} -> physical 0x{1:X}, length 0x{2:X}",
                                      key.offset,
                                      stripe.offset,
                                      chunkLength);
                }

                offset += stripeSize;
            }
        }

        if(_chunkMap.Count == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No chunk mappings found in system chunk array");

            return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the chunk tree to build a complete logical-to-physical address map</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadChunkTree()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading chunk tree at logical address {0}...", _superblock.chunk_lba);

        ErrorNumber errno = ReadTreeBlock(_superblock.chunk_lba, out byte[] nodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading chunk tree root: {0}", errno);

            return errno;
        }

        Header header = Marshal.ByteArrayToStructureLittleEndian<Header>(nodeData);

        AaruLogging.Debug(MODULE_NAME, "Chunk tree root level: {0}, items: {1}", header.level, header.nritems);

        // Walk the chunk tree to find all CHUNK_ITEM entries
        errno = WalkTreeForChunks(nodeData, header);

        return errno;
    }

    /// <summary>
    ///     Walks a tree node (recursively for internal nodes) to find all chunk items and add them to the chunk map. For
    ///     leaf nodes, extracts BTRFS_CHUNK_ITEM_KEY items. For internal nodes, follows key pointers to child nodes.
    /// </summary>
    /// <param name="nodeData">The raw node/leaf data</param>
    /// <param name="header">The parsed header of the node</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber WalkTreeForChunks(byte[] nodeData, in Header header)
    {
        if(header.level == 0)
        {
            // Leaf node - extract chunk items
            return ExtractChunkItemsFromLeaf(nodeData, header);
        }

        // Internal node - follow key pointers
        int keyPtrSize = Marshal.SizeOf<KeyPtr>();
        int headerSize = Marshal.SizeOf<Header>();

        for(uint i = 0; i < header.nritems; i++)
        {
            int keyPtrOffset = headerSize + (int)i * keyPtrSize;

            if(keyPtrOffset + keyPtrSize > nodeData.Length) break;

            KeyPtr keyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, keyPtrOffset, keyPtrSize);

            ErrorNumber errno = ReadTreeBlock(keyPtr.blockptr, out byte[] childData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading chunk tree child at {0}: {1}", keyPtr.blockptr, errno);

                continue;
            }

            Header childHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(childData);

            errno = WalkTreeForChunks(childData, childHeader);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Extracts chunk items from a leaf node and adds new mappings to the chunk map</summary>
    /// <param name="leafData">Raw leaf node data</param>
    /// <param name="header">Parsed leaf header</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ExtractChunkItemsFromLeaf(byte[] leafData, in Header header)
    {
        int itemSize   = Marshal.SizeOf<Item>();
        int headerSize = Marshal.SizeOf<Header>();

        for(uint i = 0; i < header.nritems; i++)
        {
            int itemOffset = headerSize + (int)i * itemSize;

            if(itemOffset + itemSize > leafData.Length) break;

            Item item = Marshal.ByteArrayToStructureLittleEndian<Item>(leafData, itemOffset, itemSize);

            if(item.key.type != BTRFS_CHUNK_ITEM_KEY) continue;

            // Item data offset is relative to the end of the header area
            int dataOffset = headerSize + (int)item.offset;

            if(dataOffset + 48 > leafData.Length) continue; // Need at least the chunk header

            var chunkLength = BitConverter.ToUInt64(leafData, dataOffset);
            var numStripes  = BitConverter.ToUInt16(leafData, dataOffset + 44);

            // Check if this logical offset is already mapped
            var alreadyMapped = false;

            foreach(ChunkMapping existing in _chunkMap)
            {
                if(existing.LogicalOffset == item.key.offset)
                {
                    alreadyMapped = true;

                    break;
                }
            }

            if(alreadyMapped) continue;

            int stripeOffset = dataOffset + 48;
            int stripeSize   = Marshal.SizeOf<Stripe>();

            if(stripeOffset + stripeSize > leafData.Length) continue;

            Stripe stripe = Marshal.ByteArrayToStructureLittleEndian<Stripe>(leafData, stripeOffset, stripeSize);

            var chunkStripeLen = BitConverter.ToUInt64(leafData, dataOffset + 16);
            var chunkType      = BitConverter.ToUInt64(leafData, dataOffset + 24);

            _chunkMap.Add(new ChunkMapping
            {
                LogicalOffset  = item.key.offset,
                Length         = chunkLength,
                PhysicalOffset = stripe.offset,
                DevId          = stripe.devid,
                NumStripes     = numStripes,
                StripeLen      = chunkStripeLen,
                Type           = chunkType
            });

            AaruLogging.Debug(MODULE_NAME,
                              "Chunk tree mapping: logical 0x{0:X} -> physical 0x{1:X}, length 0x{2:X}, type 0x{3:X}, stripes {4}",
                              item.key.offset,
                              stripe.offset,
                              chunkLength,
                              chunkType,
                              numStripes);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Translates a logical address to a physical sector address and reads a full tree block (node). Uses the chunk
    ///     map to perform logical-to-physical address translation.
    /// </summary>
    /// <param name="logicalAddr">Logical byte address in the btrfs address space</param>
    /// <param name="nodeData">The raw tree block data, sized to nodesize</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadTreeBlock(ulong logicalAddr, out byte[] nodeData)
    {
        nodeData = null;

        // Check cache first
        if(_treeBlockCache != null && _treeBlockCache.TryGetValue(logicalAddr, out byte[] cached))
        {
            nodeData = cached;

            return ErrorNumber.NoError;
        }

        ulong physicalAddr = LogicalToPhysical(logicalAddr);

        if(physicalAddr == ulong.MaxValue)
        {
            AaruLogging.Debug(MODULE_NAME, "Cannot translate logical address 0x{0:X} to physical", logicalAddr);

            return ErrorNumber.InvalidArgument;
        }

        // Convert physical byte address to absolute sector address (physical addresses are partition-relative)
        ulong sectorAddr = _partition.Start + physicalAddr / _imagePlugin.Info.SectorSize;
        uint  numSectors = _superblock.nodesize / _imagePlugin.Info.SectorSize;

        if(numSectors == 0) numSectors = 1;

        if(sectorAddr + numSectors > _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Physical sector address {0} beyond partition end {1}",
                              sectorAddr,
                              _partition.End);

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddr, false, numSectors, out nodeData, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading tree block at sector {0}: {1}", sectorAddr, errno);

            return errno;
        }

        // Validate the header to catch garbage data early
        if(nodeData.Length >= Marshal.SizeOf<Header>())
        {
            Header hdr = Marshal.ByteArrayToStructureLittleEndian<Header>(nodeData);

            // Level should not exceed a reasonable depth (btrfs max is ~8, but allow some margin)
            if(hdr.level > 64)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Invalid tree block header: level={0}, nritems={1} at logical 0x{2:X}",
                                  hdr.level,
                                  hdr.nritems,
                                  logicalAddr);

                return ErrorNumber.InvalidArgument;
            }
        }

        // Cache the successfully read block
        _treeBlockCache?.TryAdd(logicalAddr, nodeData);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an arbitrary range of bytes starting at a logical byte address</summary>
    /// <param name="logicalAddr">Logical byte address to start reading from</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="data">The read data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadLogicalBytes(ulong logicalAddr, uint length, out byte[] data)
    {
        data = null;

        ulong physicalAddr = LogicalToPhysical(logicalAddr);

        if(physicalAddr == ulong.MaxValue)
        {
            AaruLogging.Debug(MODULE_NAME, "Cannot translate logical address 0x{0:X} to physical", logicalAddr);

            return ErrorNumber.InvalidArgument;
        }

        uint sectorSize = _imagePlugin.Info.SectorSize;

        // Calculate sector-aligned read
        ulong sectorAddr       = _partition.Start + physicalAddr / sectorSize;
        var   offsetInSector   = (uint)(physicalAddr % sectorSize);
        uint  totalBytesNeeded = offsetInSector + length;
        uint  numSectors       = (totalBytesNeeded + sectorSize - 1) / sectorSize;

        if(sectorAddr + numSectors > _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Physical sector address {0} beyond partition end {1}",
                              sectorAddr,
                              _partition.End);

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddr, false, numSectors, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        data = new byte[length];
        Array.Copy(sectorData, (int)offsetInSector, data, 0, (int)length);

        return ErrorNumber.NoError;
    }

    /// <summary>Translates a logical byte address to a physical byte address using the chunk map</summary>
    /// <param name="logicalAddr">The logical byte address to translate</param>
    /// <returns>The physical byte address, or ulong.MaxValue if the mapping is not found</returns>
    ulong LogicalToPhysical(ulong logicalAddr)
    {
        foreach(ChunkMapping mapping in _chunkMap)
        {
            if(logicalAddr < mapping.LogicalOffset || logicalAddr >= mapping.LogicalOffset + mapping.Length) continue;

            ulong offsetInChunk = logicalAddr - mapping.LogicalOffset;

            // For DUP and RAID1, both stripes contain identical data — just use the first stripe
            if((mapping.Type & BTRFS_BLOCK_GROUP_DUP)   != 0 ||
               (mapping.Type & BTRFS_BLOCK_GROUP_RAID1) != 0 ||
               mapping.NumStripes                       <= 1 ||
               mapping.StripeLen                        == 0)
                return mapping.PhysicalOffset + offsetInChunk;

            // For striped (RAID0/RAID10), calculate which stripe and offset within the stripe
            ulong stripeNumber = offsetInChunk / mapping.StripeLen;
            ulong stripeIndex  = stripeNumber  % mapping.NumStripes;

            // We only have the first stripe's physical offset; for single-device images, stripeIndex should be 0
            if(stripeIndex != 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Logical address 0x{0:X} maps to stripe {1}, but we only support single-device images",
                                  logicalAddr,
                                  stripeIndex);

                return ulong.MaxValue;
            }

            ulong stripeOffset = stripeNumber / mapping.NumStripes * mapping.StripeLen +
                                 offsetInChunk                     % mapping.StripeLen;

            return mapping.PhysicalOffset + stripeOffset;
        }

        return ulong.MaxValue;
    }

    /// <summary>Compares two DiskKey values using btrfs key ordering (objectid, type, offset)</summary>
    /// <param name="a">First key</param>
    /// <param name="b">Second key</param>
    /// <returns>Negative if a &lt; b, zero if equal, positive if a &gt; b</returns>
    static int CompareKeys(in DiskKey a, in DiskKey b)
    {
        if(a.objectid != b.objectid) return a.objectid < b.objectid ? -1 : 1;
        if(a.type     != b.type) return a.type         < b.type ? -1 : 1;
        if(a.offset   != b.offset) return a.offset     < b.offset ? -1 : 1;

        return 0;
    }

    /// <summary>
    ///     Binary searches an internal node's key pointers to find the index of the child subtree
    ///     that could contain the target key. Returns the index of the rightmost KeyPtr whose key ≤ targetKey.
    /// </summary>
    /// <param name="nodeData">Raw internal node data</param>
    /// <param name="header">Parsed node header</param>
    /// <param name="targetKey">The key to search for</param>
    /// <returns>The index of the child to descend into, or -1 if the node is empty</returns>
    static int BinarySearchNode(byte[] nodeData, in Header header, in DiskKey targetKey)
    {
        if(header.nritems == 0) return -1;

        int headerSize = Marshal.SizeOf<Header>();
        int keyPtrSize = Marshal.SizeOf<KeyPtr>();

        var lo = 0;
        int hi = (int)header.nritems - 1;

        while(lo <= hi)
        {
            int mid       = lo         + (hi - lo) / 2;
            int midOffset = headerSize + mid       * keyPtrSize;

            if(midOffset + keyPtrSize > nodeData.Length) break;

            KeyPtr midKeyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, midOffset, keyPtrSize);

            int cmp = CompareKeys(midKeyPtr.key, targetKey);

            if(cmp <= 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        // hi is now the index of the rightmost KeyPtr with key ≤ targetKey
        return hi >= 0 ? hi : 0;
    }

    /// <summary>
    ///     Binary searches an internal node to find the range of children whose subtrees could contain
    ///     items with the specified objectid. Returns the start (inclusive) and end (exclusive) indices.
    /// </summary>
    /// <param name="nodeData">Raw internal node data</param>
    /// <param name="header">Parsed node header</param>
    /// <param name="targetObjectId">The objectid to search for</param>
    /// <param name="startIndex">First child index that could contain matching items (inclusive)</param>
    /// <param name="endIndex">Last child index that could contain matching items (exclusive)</param>
    static void BinarySearchNodeRange(byte[]  nodeData, in Header header, ulong targetObjectId, out int startIndex,
                                      out int endIndex)
    {
        startIndex = 0;
        endIndex   = (int)header.nritems;

        if(header.nritems == 0) return;

        int headerSize = Marshal.SizeOf<Header>();
        int keyPtrSize = Marshal.SizeOf<KeyPtr>();

        // Find the first child whose key's objectid could include targetObjectId
        // This is the rightmost child with key.objectid ≤ targetObjectId
        var keyBytes = new byte[17];
        BitConverter.TryWriteBytes(keyBytes.AsSpan(0), targetObjectId);

        // type = 0 and offset = 0 already from array init
        DiskKey targetLow = Marshal.ByteArrayToStructureLittleEndian<DiskKey>(keyBytes);

        startIndex = BinarySearchNode(nodeData, header, targetLow);

        // Find the end: first child whose key.objectid > targetObjectId
        // We scan forward from startIndex since the range is typically small
        endIndex = startIndex + 1;

        while(endIndex < (int)header.nritems)
        {
            int endOffset = headerSize + endIndex * keyPtrSize;

            if(endOffset + keyPtrSize > nodeData.Length) break;

            KeyPtr endKeyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, endOffset, keyPtrSize);

            if(endKeyPtr.key.objectid > targetObjectId) break;

            endIndex++;
        }
    }

    /// <summary>
    ///     Binary searches a leaf node's items to find the index of the first item with
    ///     objectid ≥ targetObjectId. Returns nritems if no matching item exists.
    /// </summary>
    /// <param name="leafData">Raw leaf node data</param>
    /// <param name="header">Parsed leaf header</param>
    /// <param name="targetObjectId">The objectid to search for</param>
    /// <returns>Index of the first item with objectid ≥ targetObjectId</returns>
    static uint BinarySearchLeaf(byte[] leafData, in Header header, ulong targetObjectId)
    {
        if(header.nritems == 0) return 0;

        int headerSize = Marshal.SizeOf<Header>();
        int itemSize   = Marshal.SizeOf<Item>();

        uint lo = 0;
        uint hi = header.nritems - 1;

        while(lo <= hi)
        {
            uint mid       = lo         + (hi - lo) / 2;
            int  midOffset = headerSize + (int)mid  * itemSize;

            if(midOffset + itemSize > leafData.Length) break;

            var midObjectId = BitConverter.ToUInt64(leafData, midOffset);

            if(midObjectId < targetObjectId)
                lo = mid + 1;
            else if(midObjectId > targetObjectId)
            {
                if(mid == 0) break;

                hi = mid - 1;
            }
            else
            {
                // Found matching objectid — scan backward to find the first occurrence
                while(mid > 0)
                {
                    int prevOffset = headerSize + (int)(mid - 1) * itemSize;
                    var prevObjId  = BitConverter.ToUInt64(leafData, prevOffset);

                    if(prevObjId != targetObjectId) break;

                    mid--;
                }

                return mid;
            }
        }

        return lo;
    }
}