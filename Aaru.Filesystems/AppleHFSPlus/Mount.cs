// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
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
// ReSharper disable UnusedMember.Local

using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        // Note: encoding parameter is ignored because HFS+ uses Unicode natively,
        // and C# strings are already UTF-16, matching HFS+ Unicode format

        // Store parameters for later use
        _imagePlugin    = imagePlugin;
        _partitionStart = partition.Start;
        _sectorSize     = imagePlugin.Info.SectorSize;

        // Initialize metadata object
        Metadata = new FileSystem();

        // Try to read and parse the Volume Header
        ErrorNumber errno = ReadVolumeHeader();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to read volume header: {errno}");

            return errno;
        }

        // Initialize directory cache dictionary
        _directoryCaches    = new Dictionary<uint, Dictionary<string, CatalogEntry>>();
        _rootDirectoryCache = new Dictionary<string, CatalogEntry>();
        _catalogBTreeHeader = default(BTHeaderRec);
        _rootFolder         = default(HFSPlusCatalogFolder);

        // Attempt to read and validate the Catalog File (B-Tree) header
        errno = ReadAndValidateCatalogHeader();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to read or validate catalog header: {errno}");

            return errno;
        }

        // Find and cache the root folder (CNID = 2)
        errno = FindRootFolder();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to find root folder: {errno}");

            return errno;
        }

        // Cache root folder entries
        errno = CacheRootFolderEntries();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to cache root folder entries: {errno}");

            return errno;
        }

        // Populate metadata from volume header
        PopulateMetadata();

        // Mark filesystem as mounted
        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Filesystem mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and parses the Volume Header from offset 0x0400</summary>
    /// <returns>ErrorNumber indicating success or failure</returns>
    ErrorNumber ReadVolumeHeader()
    {
        // The HFS+ Volume Header can be at:
        // 1. Offset 0x0400 (1024 bytes) for pure HFS+ volumes
        // 2. At an offset calculated from the HFS MDB for HFS+ wrapped in HFS

        // First, try to read the HFS MDB to check if this is a wrapped HFS+ volume
        ulong hfspOffset = 0; // Offset in 512-byte sectors

        // Read sector 2 (512-byte sectors) which contains the HFS MDB or HFS+ VH
        uint sectorsToRead = (0x400 + 512 + _sectorSize - 1) / _sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partitionStart,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] vhSectors,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(vhSectors.Length < 0x400 + 512) return ErrorNumber.InvalidArgument;

        // Check if there's an HFS MDB at offset 0x0400
        var hfsMdbSig = BigEndianBitConverter.ToUInt16(vhSectors, 0x400);

        if(hfsMdbSig == AppleCommon.HFS_MAGIC)
        {
            // This is an HFS wrapper around HFS+
            // Read the embedded HFS+ location from the HFS MDB
            // MDB offsets:
            // 0x414: drAlBlkSiz (allocation block size)
            // 0x41C: drAlBlSt (first allocation block start, in 512-byte sectors)
            // 0x47E: xdrStABN (start allocation block number of embedded HFS+)

            var drAlBlkSiz = BigEndianBitConverter.ToUInt32(vhSectors, 0x400 + 0x14);
            var drAlBlSt   = BigEndianBitConverter.ToUInt16(vhSectors, 0x400 + 0x1C);
            var xdrStABN   = BigEndianBitConverter.ToUInt16(vhSectors, 0x400 + 0x7E);

            // Calculate the offset to the embedded HFS+ volume header
            // offset = drAlBlSt + (xdrStABN * drAlBlkSiz)
            // This gives us the byte offset, which we need to convert to sector offset
            ulong byteOffset = (ulong)drAlBlSt * 512 + (ulong)xdrStABN * drAlBlkSiz;
            hfspOffset = byteOffset / _sectorSize;

            AaruLogging.Debug(MODULE_NAME,
                              $"Found HFS wrapper: drAlBlSt={drAlBlSt}, xdrStABN={xdrStABN}, drAlBlkSiz={drAlBlkSiz}, hfspOffset={hfspOffset} sectors");

            // Now read the HFS+ VH from the calculated offset
            sectorsToRead = (uint)((byteOffset % _sectorSize + 512 + _sectorSize - 1) / _sectorSize);

            errno = _imagePlugin.ReadSectors(_partitionStart + hfspOffset, false, sectorsToRead, out vhSectors, out _);

            if(errno != ErrorNumber.NoError) return errno;

            var vhByteOffset = (uint)(byteOffset % _sectorSize);

            if(vhSectors.Length < vhByteOffset + 512) return ErrorNumber.InvalidArgument;

            // Parse the Volume Header structure
            _volumeHeader = Marshal.ByteArrayToStructureBigEndian<VolumeHeader>(vhSectors,
                                                                                    (int)vhByteOffset,
                                                                                    System.Runtime.InteropServices
                                                                                       .Marshal
                                                                                       .SizeOf(typeof(VolumeHeader)));
        }
        else
        {
            // Pure HFS+ volume, VH is at offset 0x0400 from partition start
            uint vhByteOffset   = 0x400 % _sectorSize;
            uint vhSectorOffset = 0x400 / _sectorSize;

            sectorsToRead = (vhByteOffset + 512 + _sectorSize - 1) / _sectorSize;

            errno = _imagePlugin.ReadSectors(_partitionStart + vhSectorOffset,
                                             false,
                                             sectorsToRead,
                                             out vhSectors,
                                             out _);

            if(errno != ErrorNumber.NoError) return errno;

            if(vhSectors.Length < vhByteOffset + 512) return ErrorNumber.InvalidArgument;

            // Parse the Volume Header structure
            _volumeHeader = Marshal.ByteArrayToStructureBigEndian<VolumeHeader>(vhSectors,
                                                                                    (int)vhByteOffset,
                                                                                    System.Runtime.InteropServices
                                                                                       .Marshal
                                                                                       .SizeOf(typeof(VolumeHeader)));
        }

        // Verify the signature
        if(_volumeHeader.signature != AppleCommon.HFSP_MAGIC && _volumeHeader.signature != AppleCommon.HFSX_MAGIC)
            return ErrorNumber.InvalidArgument;

        // Verify the version
        switch(_volumeHeader.signature)
        {
            case AppleCommon.HFSP_MAGIC when _volumeHeader.version != 4:
            case AppleCommon.HFSX_MAGIC when _volumeHeader.version != 5:
                return ErrorNumber.InvalidArgument;
        }

        // Validate critical fields
        if(_volumeHeader.blockSize == 0 || _volumeHeader.totalBlocks == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME,
                          $"VolumeHeader: signature=0x{_volumeHeader.signature:X4}, version={_volumeHeader.version}, blockSize={_volumeHeader.blockSize}, totalBlocks={_volumeHeader.totalBlocks}, freeBlocks={_volumeHeader.freeBlocks}");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the Catalog File B-Tree header node</summary>
    /// <returns>ErrorNumber indicating success or failure</returns>
    ErrorNumber ReadAndValidateCatalogHeader()
    {
        // The catalog file location and extents are stored in the Volume Header
        HFSPlusForkData catalogFork = _volumeHeader.catalogFile;

        if(catalogFork.totalBlocks == 0) return ErrorNumber.InvalidArgument;

        // Read the first extent of the catalog file
        // In HFS+, the first 8 extents are stored in the fork data structure
        if(catalogFork.extents.extentDescriptors               == null ||
           catalogFork.extents.extentDescriptors.Length        == 0    ||
           catalogFork.extents.extentDescriptors[0].blockCount == 0)
            return ErrorNumber.InvalidArgument;

        HFSPlusExtentDescriptor firstExtent = catalogFork.extents.extentDescriptors[0];

        // Calculate the byte offset of the catalog file in the volume
        ulong catalogFileOffset = firstExtent.startBlock * _volumeHeader.blockSize;

        // Convert to device sector address
        ulong deviceSector = (_partitionStart * _sectorSize + catalogFileOffset) / _sectorSize;
        var   byteOffset   = (uint)((_partitionStart * _sectorSize + catalogFileOffset) % _sectorSize);

        // Read the header node (first node in the catalog B-Tree)
        // For now, we don't know the node size yet, so read a reasonable amount
        uint sectorsToRead = (4096 + byteOffset + _sectorSize - 1) / _sectorSize; // Read up to 4KB

        ErrorNumber errno = _imagePlugin.ReadSectors(deviceSector,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] headerSectors,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(headerSectors.Length < byteOffset + System.Runtime.InteropServices.Marshal.SizeOf(typeof(BTNodeDescriptor)))
            return ErrorNumber.InvalidArgument;

        // Parse the node descriptor
        int ndSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BTNodeDescriptor));

        BTNodeDescriptor nodeDesc =
            Marshal.ByteArrayToStructureBigEndian<BTNodeDescriptor>(headerSectors, (int)byteOffset, ndSize);

        // Verify this is a header node
        if(nodeDesc.kind != BTNodeKind.kBTHeaderNode) return ErrorNumber.InvalidArgument;

        // Parse the B-Tree header record (follows the node descriptor)
        int bhSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BTHeaderRec));

        if(headerSectors.Length < byteOffset + ndSize + bhSize) return ErrorNumber.InvalidArgument;

        _catalogBTreeHeader =
            Marshal.ByteArrayToStructureBigEndian<BTHeaderRec>(headerSectors, (int)(byteOffset + ndSize), bhSize);

        // Validate B-Tree consistency
        if(_catalogBTreeHeader.treeDepth == 0 || _catalogBTreeHeader.leafRecords == 0)
            return ErrorNumber.InvalidArgument;

        // Node size must be a power of 2 and within valid range
        if(_catalogBTreeHeader.nodeSize is < 512 or > 16384) return ErrorNumber.InvalidArgument;

        if((_catalogBTreeHeader.nodeSize & _catalogBTreeHeader.nodeSize - 1) != 0)
            return ErrorNumber.InvalidArgument; // Not a power of 2

        AaruLogging.Debug(MODULE_NAME,
                          $"Catalog B-Tree header: depth={_catalogBTreeHeader.treeDepth}, rootNode={_catalogBTreeHeader.rootNode}, nodeSize={_catalogBTreeHeader.nodeSize}, leafRecords={_catalogBTreeHeader.leafRecords}");

        return ErrorNumber.NoError;
    }

    /// <summary>Finds the root folder (CNID=2) in the catalog B-Tree</summary>
    /// <returns>ErrorNumber indicating success or failure</returns>
    ErrorNumber FindRootFolder()
    {
        // The root folder has CNID=2 and its parent ID is 1 (special marker)
        // Use the generic B-Tree traversal with a predicate that matches the root folder

        return TraverseCatalogBTree(_catalogBTreeHeader.rootNode,
                                    (leafNode, recordOffset) =>
                                    {
                                        // Parse the catalog key at this offset
                                        // Key structure: keyLength(2) + parentID(4) + nodeName(variable)
                                        if(recordOffset + 6 > leafNode.Length) return false;

                                        var keyLength = BigEndianBitConverter.ToUInt16(leafNode, recordOffset);
                                        var parentID  = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);

                                        // Check if this is the root folder (parentID=1)
                                        if(parentID != kHFSRootParentID) return false;

                                        // The record type is after the key
                                        int recordTypeOffset = recordOffset + 2 + keyLength;

                                        if(recordTypeOffset + 2 > leafNode.Length) return false;

                                        var recordType = BigEndianBitConverter.ToInt16(leafNode, recordTypeOffset);

                                        if(recordType != (short)BTreeRecordType.kHFSPlusFolderRecord) return false;

                                        // This is the root folder record, parse it
                                        int folderRecordSize =
                                            System.Runtime.InteropServices.Marshal.SizeOf(typeof(HFSPlusCatalogFolder));

                                        if(recordTypeOffset + folderRecordSize > leafNode.Length) return false;

                                        _rootFolder =
                                            Marshal.ByteArrayToStructureBigEndian<HFSPlusCatalogFolder>(leafNode,
                                                recordTypeOffset,
                                                folderRecordSize);

                                        AaruLogging.Debug(MODULE_NAME,
                                                          $"Found root folder: CNID={_rootFolder.folderID}, valence={_rootFolder.valence}");

                                        return true; // Stop traversal
                                    });
    }

    /// <summary>Caches the root folder entries</summary>
    /// <returns>ErrorNumber indicating success or failure</returns>
    ErrorNumber CacheRootFolderEntries()
    {
        // Cache the root folder entries by traversing the catalog B-Tree
        // and finding all entries where parentID == kHFSRootFolderID (2)

        if(_rootFolder.folderID != kHFSRootFolderID) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, $"Caching root folder entries (valence={_rootFolder.valence})");

        // Traverse leaf nodes in the catalog B-Tree until we've cached all root folder entries
        ErrorNumber errno = ReadCatalogNode(_catalogBTreeHeader.firstLeafNode, out byte[] leafNode);

        if(errno != ErrorNumber.NoError) return errno;

        uint currentLeafNode = _catalogBTreeHeader.firstLeafNode;

        while(currentLeafNode != 0 && _rootDirectoryCache.Count < _rootFolder.valence)
        {
            errno = ReadCatalogNode(currentLeafNode, out leafNode);

            if(errno != ErrorNumber.NoError) break;

            // Parse leaf node and extract root folder entries
            ParseLeafNodeForRootEntries(leafNode);

            // Stop early if we've cached all expected entries
            if(_rootDirectoryCache.Count >= _rootFolder.valence) break;

            // Get the next leaf node
            int ndSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BTNodeDescriptor));

            if(leafNode.Length < ndSize) break;

            BTNodeDescriptor nodeDesc = Marshal.ByteArrayToStructureBigEndian<BTNodeDescriptor>(leafNode, 0, ndSize);

            currentLeafNode = nodeDesc.fLink;
        }

        AaruLogging.Debug(MODULE_NAME, $"Cached {_rootDirectoryCache.Count} root folder entries");

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a leaf node and extracts root folder entries (parentID=2)</summary>
    void ParseLeafNodeForRootEntries(byte[] leafNode)
    {
        int ndSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BTNodeDescriptor));

        if(leafNode.Length < ndSize) return;

        BTNodeDescriptor nodeDesc = Marshal.ByteArrayToStructureBigEndian<BTNodeDescriptor>(leafNode, 0, ndSize);

        ushort numRecords = nodeDesc.numRecords;

        for(ushort i = 0; i < numRecords; i++)
        {
            int offsetPointerOffset = _catalogBTreeHeader.nodeSize - 2 * (i + 1);

            if(offsetPointerOffset < 0 || offsetPointerOffset + 2 > leafNode.Length) continue;

            var recordOffset = BigEndianBitConverter.ToUInt16(leafNode, offsetPointerOffset);

            if(recordOffset >= leafNode.Length || recordOffset + 4 > leafNode.Length) continue;

            var keyLength = BigEndianBitConverter.ToUInt16(leafNode, recordOffset);
            var parentID  = BigEndianBitConverter.ToUInt32(leafNode, recordOffset + 2);

            // Only process entries with parentID == 2 (root folder)
            if(parentID != kHFSRootFolderID) continue;

            if(recordOffset + keyLength + 2 + 2 > leafNode.Length) continue;

            var recordType = BigEndianBitConverter.ToInt16(leafNode, recordOffset + keyLength + 2);

            // Extract the filename from the key
            string entryName = ExtractNameFromCatalogKey(leafNode, recordOffset);

            // Process folder and file records
            if(recordType == (short)BTreeRecordType.kHFSPlusFolderRecord)
            {
                int folderRecordSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(HFSPlusCatalogFolder));

                if(recordOffset + keyLength + 2 + folderRecordSize <= leafNode.Length)
                {
                    HFSPlusCatalogFolder folder =
                        Marshal.ByteArrayToStructureBigEndian<HFSPlusCatalogFolder>(leafNode,
                            recordOffset + keyLength + 2,
                            folderRecordSize);

                    var entry = new DirectoryEntry
                    {
                        Name             = entryName,
                        CNID             = folder.folderID,
                        ParentID         = parentID,
                        Type             = (int)BTreeRecordType.kHFSPlusFolderRecord,
                        Valence          = folder.valence,
                        CreationDate     = folder.createDate,
                        ContentModDate   = folder.contentModDate,
                        AttributeModDate = folder.attributeModDate,
                        AccessDate       = folder.accessDate,
                        BackupDate       = folder.backupDate
                    };

                    _rootDirectoryCache[entryName] = entry;

                    AaruLogging.Debug(MODULE_NAME, $"Cached ROOT folder: {entryName} (CNID={folder.folderID})");
                }
            }
            else if(recordType == (short)BTreeRecordType.kHFSPlusFileRecord)
            {
                int fileRecordSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(HFSPlusCatalogFile));

                if(recordOffset + keyLength + 2 + fileRecordSize <= leafNode.Length)
                {
                    HFSPlusCatalogFile file =
                        Marshal.ByteArrayToStructureBigEndian<HFSPlusCatalogFile>(leafNode,
                            recordOffset + keyLength + 2,
                            fileRecordSize);

                    var entry = new FileEntry
                    {
                        Name                     = entryName,
                        CNID                     = file.fileID,
                        ParentID                 = parentID,
                        Type                     = (int)BTreeRecordType.kHFSPlusFileRecord,
                        DataForkLogicalSize      = file.dataFork.logicalSize,
                        DataForkPhysicalSize     = file.dataFork.logicalSize,
                        DataForkTotalBlocks      = file.dataFork.totalBlocks,
                        DataForkExtents          = file.dataFork.extents,
                        ResourceForkLogicalSize  = file.resourceFork.logicalSize,
                        ResourceForkPhysicalSize = file.resourceFork.logicalSize,
                        ResourceForkTotalBlocks  = file.resourceFork.totalBlocks,
                        ResourceForkExtents      = file.resourceFork.extents,
                        CreationDate             = file.createDate,
                        ContentModDate           = file.contentModDate,
                        AttributeModDate         = file.attributeModDate,
                        AccessDate               = file.accessDate,
                        BackupDate               = file.backupDate
                    };

                    _rootDirectoryCache[entryName] = entry;

                    AaruLogging.Debug(MODULE_NAME, $"Cached ROOT file: {entryName} (CNID={file.fileID})");
                }
            }
        }
    }


    /// <summary>Populates the Metadata object from the parsed Volume Header</summary>
    void PopulateMetadata()
    {
        Metadata.Type = _volumeHeader.signature == AppleCommon.HFSX_MAGIC ? FS_TYPE_HFSX : FS_TYPE_HFSP;

        Metadata.Clusters     = _volumeHeader.totalBlocks;
        Metadata.ClusterSize  = _volumeHeader.blockSize;
        Metadata.Files        = _volumeHeader.fileCount;
        Metadata.FreeClusters = _volumeHeader.freeBlocks;

        // Check if volume was cleanly unmounted
        Metadata.Dirty = !_volumeHeader.attributes.HasFlag(VolumeAttributes.kHFSVolumeUnmountedBit) ||
                         _volumeHeader.attributes.HasFlag(VolumeAttributes.kHFSBootVolumeInconsistentBit);

        // Parse volume name from Volume Header
        // TODO: Parse volume name from the root folder thread record

        if(_volumeHeader.createDate > 0) Metadata.CreationDate = DateHandlers.MacToDateTime(_volumeHeader.createDate);

        if(_volumeHeader.modifyDate > 0)
            Metadata.ModificationDate = DateHandlers.MacToDateTime(_volumeHeader.modifyDate);

        if(_volumeHeader.backupDate > 0) Metadata.BackupDate = DateHandlers.MacToDateTime(_volumeHeader.backupDate);

        // Create volume serial from Finder info fields
        if(_volumeHeader.drFndrInfo6 != 0 && _volumeHeader.drFndrInfo7 != 0)
            Metadata.VolumeSerial = $"{_volumeHeader.drFndrInfo6:X8}{_volumeHeader.drFndrInfo7:X8}";

        // Create FileSystemInfo for StatFs
        _fileSystemInfo = new FileSystemInfo
        {
            Type           = Metadata.Type,
            Blocks         = _volumeHeader.totalBlocks,
            Files          = _volumeHeader.fileCount,
            FreeBlocks     = _volumeHeader.freeBlocks,
            FilenameLength = 255, // HFS+ supports up to 255 character UTF-16 names
            PluginId       = Id,
            Id = new FileSystemId
            {
                IsLong   = true,
                Serial64 = (ulong)_volumeHeader.drFndrInfo6 << 32 | _volumeHeader.drFndrInfo7
            }
        };
    }
}