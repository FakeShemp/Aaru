// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from Practical Filesystem Design, ISBN 1-55860-497-9
/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class BeFS
{
    /// <summary>Mounts a BeFS volume</summary>
    /// <param name="imagePlugin">The media image to mount</param>
    /// <param name="partition">The partition to mount</param>
    /// <param name="encoding">The encoding to use for text</param>
    /// <param name="options">Optional mount options</param>
    /// <param name="namespace">The namespace prefix to use</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Superblock read successfully. Endianness: {0}",
                          _littleEndian ? "Little-endian" : "Big-endian");

        AaruLogging.Debug(MODULE_NAME, "Volume name: {0}",      StringHandlers.CToString(_superblock.name, _encoding));
        AaruLogging.Debug(MODULE_NAME, "Block size: {0} bytes", _superblock.block_size);

        AaruLogging.Debug(MODULE_NAME,
                          "Total blocks: {0} ({1} bytes)",
                          _superblock.num_blocks,
                          _superblock.num_blocks * _superblock.block_size);

        AaruLogging.Debug(MODULE_NAME,
                          "Used blocks: {0} ({1} bytes)",
                          _superblock.used_blocks,
                          _superblock.used_blocks * _superblock.block_size);

        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded successfully");

        Metadata = new FileSystem
        {
            Clusters     = (ulong)_superblock.num_blocks,
            ClusterSize  = _superblock.block_size,
            Dirty        = _superblock.flags == BEFS_DIRTY,
            FreeClusters = (ulong)(_superblock.num_blocks - _superblock.used_blocks),
            Type         = FS_TYPE,
            VolumeName   = StringHandlers.CToString(_superblock.name, _encoding)
        };

        AaruLogging.Debug(MODULE_NAME,
                          "Mount complete. Dirty: {0}, Free clusters: {1}",
                          Metadata.Dirty,
                          Metadata.FreeClusters);

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the filesystem superblock</summary>
    /// <remarks>
    ///     The superblock can be located at different offsets depending on whether a boot sector is present.
    ///     This method searches the standard locations and validates the magic numbers and key fields.
    ///     It also detects the byte order (endianness) of the filesystem.
    /// </remarks>
    /// <returns>Error code indicating success or failure</returns>
    private ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");
        AaruLogging.Debug(MODULE_NAME, "Sector size: {0} bytes", _imagePlugin.Info.SectorSize);

        ErrorNumber errno = _imagePlugin.ReadSector(0 + _partition.Start, false, out byte[] sbSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading sector 0: {0}", errno);

            return errno;
        }

        var magic = BigEndianBitConverter.ToUInt32(sbSector, 0x20);
        AaruLogging.Debug(MODULE_NAME, "Magic at offset 0x20: 0x{0:X8}", magic);

        if(magic is BEFS_MAGIC1 or BEFS_CIGAM1)
        {
            _littleEndian = magic == BEFS_CIGAM1;
            AaruLogging.Debug(MODULE_NAME, "Superblock found at sector 0, offset 0x20");
        }
        else if(sbSector.Length >= 0x400)
        {
            magic = BigEndianBitConverter.ToUInt32(sbSector, 0x220);
            AaruLogging.Debug(MODULE_NAME, "Magic at offset 0x220: 0x{0:X8}", magic);

            if(magic is BEFS_MAGIC1 or BEFS_CIGAM1)
            {
                _littleEndian = magic == BEFS_CIGAM1;
                sbSector      = new byte[0x200];
                Array.Copy(sbSector, 0x200, sbSector, 0, 0x200);
                AaruLogging.Debug(MODULE_NAME, "Superblock found at sector 0, offset 0x220 (boot sector present)");
            }
            else
            {
                AaruLogging.Debug(MODULE_NAME, "Superblock not found at offsets 0x20 or 0x220, trying sector 1");
                errno = _imagePlugin.ReadSector(1 + _partition.Start, false, out sbSector, out _);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading sector 1: {0}", errno);

                    return errno;
                }

                magic = BigEndianBitConverter.ToUInt32(sbSector, 0x20);
                AaruLogging.Debug(MODULE_NAME, "Magic at sector 1, offset 0x20: 0x{0:X8}", magic);

                if(magic is BEFS_MAGIC1 or BEFS_CIGAM1)
                {
                    _littleEndian = magic == BEFS_CIGAM1;
                    AaruLogging.Debug(MODULE_NAME, "Superblock found at sector 1, offset 0x20");
                }
                else
                {
                    AaruLogging.Debug(MODULE_NAME, "Invalid magic at sector 1");

                    return ErrorNumber.InvalidArgument;
                }
            }
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME, "Sector 0 too small, trying sector 1");
            errno = _imagePlugin.ReadSector(1 + _partition.Start, false, out sbSector, out _);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading sector 1: {0}", errno);

                return errno;
            }

            magic = BigEndianBitConverter.ToUInt32(sbSector, 0x20);
            AaruLogging.Debug(MODULE_NAME, "Magic at sector 1, offset 0x20: 0x{0:X8}", magic);

            if(magic is BEFS_MAGIC1 or BEFS_CIGAM1)
                _littleEndian = magic == BEFS_CIGAM1;
            else
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid magic at sector 1");

                return ErrorNumber.InvalidArgument;
            }
        }

        _superblock = _littleEndian
                          ? Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sbSector)
                          : Marshal.ByteArrayToStructureBigEndian<SuperBlock>(sbSector);

        AaruLogging.Debug(MODULE_NAME, "Validating superblock...");

        // Validate superblock
        if(_superblock.magic1                != BEFS_MAGIC1 ||
           _superblock.magic2                != BEFS_MAGIC2 ||
           _superblock.magic3                != BEFS_MAGIC3 ||
           _superblock.root_dir.len          != 1           ||
           _superblock.indices.len           != 1           ||
           1 << (int)_superblock.block_shift != _superblock.block_size)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock validation failed!");
            AaruLogging.Debug(MODULE_NAME, "  magic1: 0x{0:X8} (expected 0x{1:X8})", _superblock.magic1, BEFS_MAGIC1);
            AaruLogging.Debug(MODULE_NAME, "  magic2: 0x{0:X8} (expected 0x{1:X8})", _superblock.magic2, BEFS_MAGIC2);
            AaruLogging.Debug(MODULE_NAME, "  magic3: 0x{0:X8} (expected 0x{1:X8})", _superblock.magic3, BEFS_MAGIC3);
            AaruLogging.Debug(MODULE_NAME, "  root_dir.len: {0} (expected 1)",       _superblock.root_dir.len);
            AaruLogging.Debug(MODULE_NAME, "  indices.len: {0} (expected 1)",        _superblock.indices.len);

            AaruLogging.Debug(MODULE_NAME,
                              "  block_shift: {0}, block_size: {1} (1 << {0} should equal {1})",
                              _superblock.block_shift,
                              _superblock.block_size);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock validation successful!");

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and parses the root directory</summary>
    /// <remarks>
    ///     Locates the root directory i-node using the superblock's root_dir block_run,
    ///     reads it from disk, validates the i-node magic number, and parses the B+tree
    ///     structure to cache all directory entries.
    /// </remarks>
    /// <returns>Error code indicating success or failure</returns>
    private ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        AaruLogging.Debug(MODULE_NAME,
                          "Partition info: Start={0} sectors (bytes: {1:X8}), Length={2} sectors",
                          _partition.Start,
                          _partition.Start * _imagePlugin.Info.SectorSize,
                          _partition.Length);

        // Debug: Show the raw block_run structure values
        AaruLogging.Debug(MODULE_NAME,
                          "Root dir block_run: allocation_group={0}, start={1}, len={2}",
                          _superblock.root_dir.allocation_group,
                          _superblock.root_dir.start,
                          _superblock.root_dir.len);

        AaruLogging.Debug(MODULE_NAME,
                          "Superblock info: blocks_per_ag={0}, ag_shift={1}, block_size={2}, inode_size={3}",
                          _superblock.blocks_per_ag,
                          _superblock.ag_shift,
                          _superblock.block_size,
                          _superblock.inode_size);

        // Calculate the block address using the same method as Linux: iaddr2blockno
        // block_address = (allocation_group << ag_shift) + start
        long blockAddress = ((long)_superblock.root_dir.allocation_group << _superblock.ag_shift) +
                            _superblock.root_dir.start;

        AaruLogging.Debug(MODULE_NAME,
                          "Block calculation: AG {0} << {1} + start {2} = block {3}",
                          _superblock.root_dir.allocation_group,
                          _superblock.ag_shift,
                          _superblock.root_dir.start,
                          blockAddress);

        // Convert block address to byte address within filesystem
        long byteAddressInFS = blockAddress * _superblock.block_size;

        AaruLogging.Debug(MODULE_NAME,
                          "Byte calculation: block {0} * block_size {1} = FS byte offset 0x{2:X8}",
                          blockAddress,
                          _superblock.block_size,
                          byteAddressInFS);

        // Calculate absolute byte address (filesystem bytes + partition offset)
        uint sectorSize          = _imagePlugin.Info.SectorSize;
        long partitionByteOffset = (long)_partition.Start * sectorSize;
        long absoluteByteAddress = byteAddressInFS + partitionByteOffset;

        long sectorAddress  = absoluteByteAddress / sectorSize;
        var  offsetInSector = (int)(absoluteByteAddress % sectorSize);
        int  bytesToRead    = _superblock.inode_size;

        AaruLogging.Debug(MODULE_NAME,
                          "Absolute address: FS offset 0x{0:X8} + partition offset 0x{1:X8} = 0x{2:X8}",
                          byteAddressInFS,
                          partitionByteOffset,
                          absoluteByteAddress);

        AaruLogging.Debug(MODULE_NAME,
                          "Sector calculation: absolute byte 0x{0:X8} / sector_size {1} = sector {2}, offset {3}",
                          absoluteByteAddress,
                          sectorSize,
                          sectorAddress,
                          offsetInSector);

        AaruLogging.Debug(MODULE_NAME,
                          "Reading {0} bytes from sector {1}, offset {2}",
                          bytesToRead,
                          sectorAddress,
                          offsetInSector);

        // Read enough sectors to cover the i-node
        int sectorsNeeded = (offsetInSector + bytesToRead + (int)sectorSize - 1) / (int)sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors((ulong)sectorAddress,
                                                     false,
                                                     (uint)sectorsNeeded,
                                                     out byte[] sectorData,
                                                     out SectorStatus[] _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root i-node sectors: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root i-node sectors read successfully ({0} sectors). First 32 bytes: {1}",
                          sectorsNeeded,
                          BitConverter.ToString(sectorData, 0, Math.Min(32, sectorData.Length)));

        // Extract the i-node from the correct offset
        var inodeData = new byte[bytesToRead];
        Array.Copy(sectorData, offsetInSector, inodeData, 0, bytesToRead);

        AaruLogging.Debug(MODULE_NAME,
                          "I-node data extracted. First 32 bytes: {0}",
                          BitConverter.ToString(inodeData, 0, Math.Min(32, inodeData.Length)));

        bfs_inode rootInode = _littleEndian
                                  ? Marshal.ByteArrayToStructureLittleEndian<bfs_inode>(inodeData)
                                  : Marshal.ByteArrayToStructureBigEndian<bfs_inode>(inodeData);

        AaruLogging.Debug(MODULE_NAME, "Root i-node unmarshalled. Magic: 0x{0:X8}", rootInode.magic1);

        // Debug: Show root i-node data stream structure
        AaruLogging.Debug(MODULE_NAME, "Root i-node data stream:");
        AaruLogging.Debug(MODULE_NAME, "  Total size: {0} bytes",   rootInode.data.size);
        AaruLogging.Debug(MODULE_NAME, "  Max direct range: {0}",   rootInode.data.max_direct_range);
        AaruLogging.Debug(MODULE_NAME, "  Max indirect range: {0}", rootInode.data.max_indirect_range);

        // Also print raw hex of data_stream area to diagnose
        // Offset calculation: 4(magic1) + 8(inode_num) + 4(uid) + 4(gid) + 4(mode) + 4(flags) + 8(create_time) +
        //                     8(last_modified_time) + 8(parent) + 8(attributes) + 4(type) + 4(node_size) + 4(etc) = 88 bytes
        const int DATA_STREAM_OFFSET = 88;
        const int DATA_STREAM_SIZE   = 144; // Size of data_stream struct

        if(inodeData.Length >= DATA_STREAM_OFFSET + DATA_STREAM_SIZE)
        {
            var hexDump = BitConverter.ToString(inodeData, DATA_STREAM_OFFSET, Math.Min(32, DATA_STREAM_SIZE));
            AaruLogging.Debug(MODULE_NAME, "  Raw data stream first 32 bytes: {0}", hexDump);
        }

        for(var i = 0; i < NUM_DIRECT_BLOCKS; i++)
        {
            if(rootInode.data.direct[i].len > 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "  Direct block {0}: AG={1}, start={2}, len={3}",
                                  i,
                                  rootInode.data.direct[i].allocation_group,
                                  rootInode.data.direct[i].start,
                                  rootInode.data.direct[i].len);
            }
        }

        if(rootInode.data.indirect.len > 0)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "  Indirect block: AG={0}, start={1}, len={2}",
                              rootInode.data.indirect.allocation_group,
                              rootInode.data.indirect.start,
                              rootInode.data.indirect.len);
        }

        // Validate root i-node
        if(rootInode.magic1 != INODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid root i-node magic! Expected 0x{0:X8}, got 0x{1:X8}",
                              INODE_MAGIC,
                              rootInode.magic1);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Root i-node valid. Data stream size: {0} bytes", rootInode.data.size);

        // Read the B+tree from the data stream blocks (like Linux does)
        errno = ParseDirectoryBTree(rootInode.data);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error parsing root directory B+tree: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory B+tree parsed successfully. Cached {0} entries",
                          _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}