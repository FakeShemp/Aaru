// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting NILFS2 volume");

        _imagePlugin        = imagePlugin;
        _partition          = partition;
        _encoding           = encoding ?? Encoding.UTF8;
        _rootDirectoryCache = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

        // Step 1: Read and validate the superblock
        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");
        AaruLogging.Debug(MODULE_NAME, "Block size: {0}",           _blockSize);
        AaruLogging.Debug(MODULE_NAME, "Segments: {0}",             _superblock.nsegments);
        AaruLogging.Debug(MODULE_NAME, "Last checkpoint: {0}",      _superblock.last_cno);
        AaruLogging.Debug(MODULE_NAME, "Last partial segment: {0}", _superblock.last_pseg);

        // Step 2: Find and read the super root (contains DAT, cpfile, sufile inodes)
        errno = FindSuperRoot(out Inode datInode, out Inode cpfileInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error finding super root: {0}", errno);

            return errno;
        }

        _datInode = datInode;

        AaruLogging.Debug(MODULE_NAME, "Super root found successfully");

        // Step 3: Read the latest checkpoint from the checkpoint file to get the ifile inode
        errno = ReadLatestCheckpoint(cpfileInode, out Checkpoint checkpoint);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading latest checkpoint: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Checkpoint {0} read successfully", checkpoint.cno);

        // Cache the ifile inode for later inode lookups (subdirectory traversal, etc.)
        _ifileInode = checkpoint.ifile_inode;

        // Step 4: Read the root directory inode (inode 2) from the ifile
        errno = ReadInodeFromIfile(checkpoint.ifile_inode, NILFS2_ROOT_INO, out Inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root directory inode: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root inode read, size: {0} bytes", rootInode.size);

        // Step 5: Load and cache the root directory entries
        errno = LoadRootDirectory(rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory loaded successfully with {0} entries",
                          _rootDirectoryCache.Count);

        // Build filesystem metadata
        Metadata = new FileSystem
        {
            Type             = FS_TYPE,
            ClusterSize      = _blockSize,
            Clusters         = _superblock.dev_size / _blockSize,
            FreeClusters     = _superblock.free_blocks_count,
            VolumeName       = StringHandlers.CToString(_superblock.volume_name, _encoding),
            VolumeSerial     = _superblock.uuid.ToString(),
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(_superblock.ctime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(_superblock.wtime)
        };

        if(_superblock.creator_os == NILFS2_OS_LINUX) Metadata.SystemIdentifier = "Linux";

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Mount complete");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _rootDirectoryCache?.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _superblock  = default(Superblock);
        _datInode    = default(Inode);
        _ifileInode  = default(Inode);
        _encoding    = null;
        _blockSize   = 0;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the NILFS2 superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        if(_imagePlugin.Info.SectorSize < 512)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size too small: {0}", _imagePlugin.Info.SectorSize);

            return ErrorNumber.InvalidArgument;
        }

        uint sbAddr = NILFS2_SUPER_OFFSET / _imagePlugin.Info.SectorSize;

        if(sbAddr == 0) sbAddr = 1;

        var sbSize = (uint)(Marshal.SizeOf<Superblock>() / _imagePlugin.Info.SectorSize);

        if(Marshal.SizeOf<Superblock>() % _imagePlugin.Info.SectorSize != 0) sbSize++;

        if(_partition.Start + sbAddr + sbSize >= _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME, "Partition too small for superblock");

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno =
            _imagePlugin.ReadSectors(_partition.Start + sbAddr, false, sbSize, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

            return errno;
        }

        if(sector.Length < Marshal.SizeOf<Superblock>())
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock sector too small");

            return ErrorNumber.InvalidArgument;
        }

        _superblock = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

        bool primaryValid = _superblock.magic == NILFS2_MAGIC;

        // Try the secondary superblock at NILFS_SB2_OFFSET_BYTES(devsize) = (((devsize >> 12) - 1) << 12)
        // The kernel reads both copies and uses the one with the higher s_last_cno
        ulong partitionSize = (_partition.End - _partition.Start + 1) * _imagePlugin.Info.SectorSize;

        if(partitionSize >= NILFS2_SEG_MIN_BLOCKS * NILFS2_MIN_BLOCK_SIZE + 4096)
        {
            ulong sb2Offset = ((partitionSize >> 12) - 1) << 12;
            uint  sb2Addr   = (uint)(sb2Offset / _imagePlugin.Info.SectorSize);

            if(_partition.Start + sb2Addr + sbSize < _partition.End)
            {
                ErrorNumber sb2Errno =
                    _imagePlugin.ReadSectors(_partition.Start + sb2Addr, false, sbSize, out byte[] sector2, out _);

                if(sb2Errno == ErrorNumber.NoError && sector2.Length >= Marshal.SizeOf<Superblock>())
                {
                    Superblock sb2 = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector2);

                    if(sb2.magic == NILFS2_MAGIC)
                    {
                        // Use the secondary superblock if the primary is invalid,
                        // or if the secondary has a newer checkpoint number
                        if(!primaryValid || sb2.last_cno > _superblock.last_cno)
                        {
                            AaruLogging.Debug(MODULE_NAME,
                                              "Using secondary superblock (primary valid={0}, sb1 cno={1}, sb2 cno={2})",
                                              primaryValid,
                                              _superblock.last_cno,
                                              sb2.last_cno);

                            _superblock  = sb2;
                            primaryValid = true;
                        }
                    }
                }
            }
        }

        if(!primaryValid)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid magic: 0x{0:X4}, expected 0x{1:X4}",
                              _superblock.magic,
                              NILFS2_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        if(_superblock.rev_level < NILFS2_MIN_SUPP_REV)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Unsupported revision: {0}, minimum: {1}",
                              _superblock.rev_level,
                              NILFS2_MIN_SUPP_REV);

            return ErrorNumber.NotSupported;
        }

        _blockSize = (uint)(1 << (int)(_superblock.log_block_size + 10));

        if(_blockSize < NILFS2_MIN_BLOCK_SIZE || _blockSize > NILFS2_MAX_BLOCK_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid block size: {0}", _blockSize);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock validation successful");

        return ErrorNumber.NoError;
    }

    /// <summary>Finds and reads the super root from the last partial segment</summary>
    /// <param name="datInode">Output DAT inode from the super root</param>
    /// <param name="cpfileInode">Output checkpoint file inode from the super root</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber FindSuperRoot(out Inode datInode, out Inode cpfileInode)
    {
        datInode    = default(Inode);
        cpfileInode = default(Inode);

        AaruLogging.Debug(MODULE_NAME, "Finding super root at last_pseg block {0}...", _superblock.last_pseg);

        // Read the segment summary at the start of the last partial segment
        ErrorNumber errno = ReadPhysicalBlock(_superblock.last_pseg, out byte[] ssData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading segment summary: {0}", errno);

            return errno;
        }

        SegmentSummary segsum = Marshal.ByteArrayToStructureLittleEndian<SegmentSummary>(ssData);

        if(segsum.magic != NILFS2_SEGSUM_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid segment summary magic: 0x{0:X8}, expected 0x{1:X8}",
                              segsum.magic,
                              NILFS2_SEGSUM_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Segment summary: seq={0}, nblocks={1}, flags=0x{2:X4}",
                          segsum.seq,
                          segsum.nblocks,
                          segsum.flags);

        if((segsum.flags & SegmentSummaryFlags.SR) == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Last partial segment does not contain a super root");

            return ErrorNumber.InvalidArgument;
        }

        // Super root is the last block in the partial segment
        ulong srBlockNr = _superblock.last_pseg + segsum.nblocks - 1;

        AaruLogging.Debug(MODULE_NAME, "Reading super root at block {0}...", srBlockNr);

        errno = ReadPhysicalBlock(srBlockNr, out byte[] srData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading super root block: {0}", errno);

            return errno;
        }

        // Parse super root header (16 bytes: sum(4) + bytes(2) + flags(2) + nongc_ctime(8))
        // Then parse inodes at variable offsets based on inode_size from superblock
        uint       inodeSize       = _superblock.inode_size;
        const uint srHeaderSize    = 16; // offsetof(nilfs_super_root, sr_dat)
        int        inodeStructSize = Marshal.SizeOf<Inode>();

        if(srData.Length < srHeaderSize + inodeSize * 3)
        {
            AaruLogging.Debug(MODULE_NAME, "Super root block too small");

            return ErrorNumber.InvalidArgument;
        }

        datInode = Marshal.ByteArrayToStructureLittleEndian<Inode>(srData, (int)srHeaderSize, inodeStructSize);

        cpfileInode = Marshal.ByteArrayToStructureLittleEndian<Inode>(srData,
                                                                      (int)(srHeaderSize + inodeSize),
                                                                      inodeStructSize);

        AaruLogging.Debug(MODULE_NAME,
                          "Super root parsed: DAT inode size={0}, cpfile inode size={1}",
                          datInode.size,
                          cpfileInode.size);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and caches the root directory contents</summary>
    /// <param name="rootInode">The root directory inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory(in Inode rootInode)
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();

        ErrorNumber errno = ReadDirectoryEntries(rootInode, _rootDirectoryCache);

        if(errno != ErrorNumber.NoError) return errno;

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}