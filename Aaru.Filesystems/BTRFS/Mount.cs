// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting btrfs volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        // Step 1: Read and validate the superblock
        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");
        AaruLogging.Debug(MODULE_NAME, "UUID: {0}",        _superblock.uuid);
        AaruLogging.Debug(MODULE_NAME, "Generation: {0}",  _superblock.generation);
        AaruLogging.Debug(MODULE_NAME, "Root tree: {0}",   _superblock.root_lba);
        AaruLogging.Debug(MODULE_NAME, "Chunk tree: {0}",  _superblock.chunk_lba);
        AaruLogging.Debug(MODULE_NAME, "Node size: {0}",   _superblock.nodesize);
        AaruLogging.Debug(MODULE_NAME, "Sector size: {0}", _superblock.sectorsize);
        AaruLogging.Debug(MODULE_NAME, "Label: {0}",       _superblock.label);

        // Step 2: Parse the system chunk array from the superblock to bootstrap logical→physical translation
        errno = ParseSystemChunkArray();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error parsing system chunk array: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Parsed {0} chunk mappings from system chunk array", _chunkMap.Count);

        // Step 3: Read the chunk tree to complete the chunk map
        errno = ReadChunkTree();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading chunk tree: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Chunk tree read, total {0} chunk mappings", _chunkMap.Count);

        // Step 4: Find the FS tree root by searching the root tree for the FS_TREE root item
        errno = FindFsTreeRoot();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error finding FS tree root: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "FS tree root at logical address {0}", _fsTreeRoot);

        // Step 5: Load the root directory from the FS tree
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            ClusterSize  = _superblock.sectorsize,
            Clusters     = _superblock.total_bytes / _superblock.sectorsize,
            VolumeName   = _superblock.label,
            VolumeSerial = _superblock.uuid.ToString()
        };

        Metadata.FreeClusters = Metadata.Clusters - _superblock.bytes_used / _superblock.sectorsize;

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "btrfs volume mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting btrfs volume");

        _rootDirectoryCache?.Clear();
        _chunkMap?.Clear();
        _rootDirectoryCache = null;
        _chunkMap           = null;
        _mounted            = false;
        _imagePlugin        = null;
        _partition          = default(Partition);
        _encoding           = null;
        _superblock         = default(SuperBlock);
        _fsTreeRoot         = 0;
        _fsTreeLevel        = 0;
        Metadata            = null;

        AaruLogging.Debug(MODULE_NAME, "btrfs volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the btrfs superblock from the image</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        ulong sbSectorOff  = 0x10000 / _imagePlugin.Info.SectorSize;
        uint  sbSectorSize = 0x1000  / _imagePlugin.Info.SectorSize;

        if(sbSectorOff + _partition.Start >= _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME, "Partition too small for superblock");

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno =
            _imagePlugin.ReadSectors(sbSectorOff + _partition.Start, false, sbSectorSize, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sectors: {0}", errno);

            return errno;
        }

        _superblock = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);

        if(_superblock.magic != BTRFS_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid magic: 0x{0:X16}, expected 0x{1:X16}",
                              _superblock.magic,
                              BTRFS_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        if(_superblock.nodesize is < 4096 or > 65536)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid node size: {0}", _superblock.nodesize);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock validated successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Searches the root tree to find the FS tree root item (objectid=5, type=BTRFS_ROOT_ITEM_KEY) and caches the FS
    ///     tree root's bytenr and level
    /// </summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber FindFsTreeRoot()
    {
        AaruLogging.Debug(MODULE_NAME, "Searching root tree for FS tree root item...");

        ErrorNumber errno = ReadTreeBlock(_superblock.root_lba, out byte[] rootTreeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root tree: {0}", errno);

            return errno;
        }

        Header rootHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(rootTreeData);

        AaruLogging.Debug(MODULE_NAME, "Root tree level: {0}, items: {1}", rootHeader.level, rootHeader.nritems);

        errno = SearchTreeForRootItem(rootTreeData, rootHeader, BTRFS_FS_TREE_OBJECTID);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "FS tree root item not found in root tree");

            return ErrorNumber.NoSuchFile;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Recursively searches a tree (starting from a node) for a ROOT_ITEM with the specified objectid. When found,
    ///     stores the FS tree root address and level in instance fields.
    /// </summary>
    /// <param name="nodeData">Raw tree node data</param>
    /// <param name="header">Parsed node header</param>
    /// <param name="targetObjectId">The objectid to search for (typically BTRFS_FS_TREE_OBJECTID = 5)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber SearchTreeForRootItem(byte[] nodeData, in Header header, ulong targetObjectId)
    {
        int headerSize = Marshal.SizeOf<Header>();

        if(header.level == 0)
        {
            // Leaf node - search for the root item
            int itemSize = Marshal.SizeOf<Item>();

            for(uint i = 0; i < header.nritems; i++)
            {
                int itemOffset = headerSize + (int)i * itemSize;

                if(itemOffset + itemSize > nodeData.Length) break;

                // Parse DiskKey fields manually to avoid potential marshalling issues
                var  objectid = BitConverter.ToUInt64(nodeData, itemOffset);
                byte keyType  = nodeData[itemOffset                        + 8];
                var  dataOff  = BitConverter.ToUInt32(nodeData, itemOffset + 17);

                if(objectid != targetObjectId || keyType != BTRFS_ROOT_ITEM_KEY) continue;

                // Found the FS tree root item - parse the RootItem to get the bytenr
                int dataOffset   = headerSize + (int)dataOff;
                int rootItemSize = Marshal.SizeOf<RootItem>();

                if(dataOffset + rootItemSize > nodeData.Length)
                {
                    AaruLogging.Debug(MODULE_NAME, "Root item data truncated");

                    return ErrorNumber.InvalidArgument;
                }

                RootItem rootItem =
                    Marshal.ByteArrayToStructureLittleEndian<RootItem>(nodeData, dataOffset, rootItemSize);

                _fsTreeRoot  = rootItem.bytenr;
                _fsTreeLevel = rootItem.level;

                AaruLogging.Debug(MODULE_NAME,
                                  "Found FS tree root: bytenr={0}, level={1}, root_dirid={2}",
                                  rootItem.bytenr,
                                  rootItem.level,
                                  rootItem.root_dirid);

                return ErrorNumber.NoError;
            }

            return ErrorNumber.NoSuchFile;
        }

        // Internal node - traverse key pointers
        int keyPtrSize = Marshal.SizeOf<KeyPtr>();

        for(uint i = 0; i < header.nritems; i++)
        {
            int keyPtrOffset = headerSize + (int)i * keyPtrSize;

            if(keyPtrOffset + keyPtrSize > nodeData.Length) break;

            KeyPtr keyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, keyPtrOffset, keyPtrSize);

            // Only follow this child if the target objectid could be in its range
            // For the last key pointer, or if the key is <= target, follow it
            if(i + 1 < header.nritems)
            {
                int    nextOffset = headerSize + (int)(i + 1) * keyPtrSize;
                KeyPtr nextKeyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, nextOffset, keyPtrSize);

                if(nextKeyPtr.key.objectid <= targetObjectId) continue; // Target is beyond this child's range
            }

            ErrorNumber errno = ReadTreeBlock(keyPtr.blockptr, out byte[] childData);

            if(errno != ErrorNumber.NoError) continue;

            Header childHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(childData);

            errno = SearchTreeForRootItem(childData, childHeader, targetObjectId);

            if(errno == ErrorNumber.NoError) return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }
}