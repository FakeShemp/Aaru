// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class EFS
{
    /// <summary>Number of inodes per basic block shift (log2(512/128) = 2)</summary>
    const int EFS_INOPBBSHIFT = 2;

    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting EFS volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        if(imagePlugin.Info.SectorSize < 512)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size too small: {0}", imagePlugin.Info.SectorSize);

            return ErrorNumber.InvalidArgument;
        }

        // Read and validate the superblock
        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");

        // Calculate inodes per cylinder group
        _inodesPerCg = (short)(_superblock.sb_cgisize << EFS_INOPBBSHIFT);

        AaruLogging.Debug(MODULE_NAME, "Inodes per cylinder group: {0}", _inodesPerCg);

        // Load root directory
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        string volumeName = StringHandlers.CToString(_superblock.sb_fname, _encoding);

        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            VolumeName   = volumeName,
            ClusterSize  = EFS_BBSIZE,
            Clusters     = (ulong)_superblock.sb_size,
            FreeClusters = (ulong)_superblock.sb_tfree,
            Dirty        = _superblock.sb_dirty != 0,
            VolumeSerial = $"{_superblock.sb_checksum:X8}",
            CreationDate = DateHandlers.UnixToDateTime(_superblock.sb_time)
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Volume mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _superblock  = default(Superblock);
        _encoding    = null;
        _inodesPerCg = 0;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the EFS superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        // Handle optical disc alignment (superblock at offset 0x200 within sector)
        if(_imagePlugin.Info.MetadataMediaType == MetadataMediaType.OpticalDisc)
        {
            var sbSize = (uint)((Marshal.SizeOf<Superblock>() + 0x200) / _imagePlugin.Info.SectorSize);

            if((Marshal.SizeOf<Superblock>() + 0x200) % _imagePlugin.Info.SectorSize != 0) sbSize++;

            ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start, false, sbSize, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

                return errno;
            }

            if(sector.Length < Marshal.SizeOf<Superblock>() + 0x200)
            {
                AaruLogging.Debug(MODULE_NAME, "Sector too small for superblock");

                return ErrorNumber.InvalidArgument;
            }

            var sbpiece = new byte[Marshal.SizeOf<Superblock>()];
            Array.Copy(sector, 0x200, sbpiece, 0, Marshal.SizeOf<Superblock>());

            _superblock = Marshal.ByteArrayToStructureBigEndian<Superblock>(sbpiece);
        }
        else
        {
            // Standard layout: superblock at basic block 1
            var sbSize = (uint)(Marshal.SizeOf<Superblock>() / _imagePlugin.Info.SectorSize);

            if(Marshal.SizeOf<Superblock>() % _imagePlugin.Info.SectorSize != 0) sbSize++;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(_partition.Start + EFS_SUPERBB, false, sbSize, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

                return errno;
            }

            if(sector.Length < Marshal.SizeOf<Superblock>())
            {
                AaruLogging.Debug(MODULE_NAME, "Sector too small for superblock");

                return ErrorNumber.InvalidArgument;
            }

            _superblock = Marshal.ByteArrayToStructureBigEndian<Superblock>(sector);
        }

        AaruLogging.Debug(MODULE_NAME, "Magic: 0x{0:X8}", _superblock.sb_magic);

        // Validate magic number
        if(_superblock.sb_magic is not EFS_MAGIC and not EFS_MAGIC_NEW)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid magic: 0x{0:X8}, expected 0x{1:X8} or 0x{2:X8}",
                              _superblock.sb_magic,
                              EFS_MAGIC,
                              EFS_MAGIC_NEW);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Filesystem size: {0} blocks",        _superblock.sb_size);
        AaruLogging.Debug(MODULE_NAME, "First cylinder group at block: {0}", _superblock.sb_firstcg);
        AaruLogging.Debug(MODULE_NAME, "Cylinder group size: {0} blocks",    _superblock.sb_cgfsize);
        AaruLogging.Debug(MODULE_NAME, "Inodes per cg: {0} blocks",          _superblock.sb_cgisize);
        AaruLogging.Debug(MODULE_NAME, "Number of cylinder groups: {0}",     _superblock.sb_ncg);
        AaruLogging.Debug(MODULE_NAME, "Free blocks: {0}",                   _superblock.sb_tfree);
        AaruLogging.Debug(MODULE_NAME, "Free inodes: {0}",                   _superblock.sb_tinode);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();

        // Read the root inode (inode 2)
        ErrorNumber errno = ReadInode(EFS_ROOTINO, out Inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate that root inode is a directory
        var fileType = (FileType)(rootInode.di_mode & (ushort)FileType.IFMT);

        if(fileType != FileType.IFDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (type=0x{0:X4})", (ushort)fileType);

            return ErrorNumber.InvalidArgument;
        }

        _inodeCache[EFS_ROOTINO] = rootInode;

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.di_size);
        AaruLogging.Debug(MODULE_NAME, "Root inode extents: {0}",    rootInode.di_numextents);

        // Read directory contents from inode extents
        errno = ReadDirectoryContents(rootInode, out Dictionary<string, uint> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory contents: {0}", errno);

            return errno;
        }

        // Cache entries (skip . and ..)
        foreach(KeyValuePair<string, uint> entry in entries)
        {
            if(entry.Key is "." or "..") continue;

            _rootDirectoryCache[entry.Key] = entry.Value;

            AaruLogging.Debug(MODULE_NAME, "Cached entry: {0} -> inode {1}", entry.Key, entry.Value);
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNumber">Inode number to read</param>
    /// <param name="inode">The read inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNumber, out Inode inode)
    {
        inode = default(Inode);

        // Calculate inode location using EFS layout macros
        // EFS_ITOBB: inode to disk bb number
        // fs_firstcg + (cg * fs_cgfsize) + ((inum / inodes_per_bb) % fs_cgisize)
        var cylinderGroup   = (int)(inodeNumber / _inodesPerCg);
        var cgInodeOffset   = (int)(inodeNumber % _inodesPerCg);
        int bbInCg          = cgInodeOffset >> EFS_INOPBBSHIFT;
        var inodeOffsetInBb = (int)(inodeNumber & EFS_INOPBB - 1);

        int blockNumber = _superblock.sb_firstcg + cylinderGroup * _superblock.sb_cgfsize + bbInCg;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0}: cg={1}, bb={2}, offset={3}",
                          inodeNumber,
                          cylinderGroup,
                          blockNumber,
                          inodeOffsetInBb);

        // Read the basic block containing the inode
        ErrorNumber errno = ReadBasicBlock(blockNumber, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode block: {0}", errno);

            return errno;
        }

        // Extract the inode from the block
        int inodeOffset = inodeOffsetInBb * EFS_INODE_SIZE;

        if(inodeOffset + EFS_INODE_SIZE > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode offset exceeds block size");

            return ErrorNumber.InvalidArgument;
        }

        inode = Marshal.ByteArrayToStructureBigEndian<Inode>(blockData, inodeOffset, EFS_INODE_SIZE);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: mode=0x{1:X4}, size={2}, extents={3}",
                          inodeNumber,
                          inode.di_mode,
                          inode.di_size,
                          inode.di_numextents);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a basic block from disk</summary>
    /// <param name="blockNumber">Block number to read</param>
    /// <param name="blockData">The read block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBasicBlock(int blockNumber, out byte[] blockData)
    {
        blockData = null;

        // Handle optical disc alignment
        if(_imagePlugin.Info.MetadataMediaType == MetadataMediaType.OpticalDisc)
        {
            // On optical discs, basic blocks are at byte offsets within sectors
            long byteOffset     = blockNumber * EFS_BBSIZE;
            long sectorNumber   = byteOffset / _imagePlugin.Info.SectorSize + (long)_partition.Start;
            var  offsetInSector = (int)(byteOffset % _imagePlugin.Info.SectorSize);

            // Calculate how many sectors to read
            var sectorsToRead = (uint)((offsetInSector + EFS_BBSIZE + _imagePlugin.Info.SectorSize - 1) /
                                       _imagePlugin.Info.SectorSize);

            ErrorNumber errno =
                _imagePlugin.ReadSectors((ulong)sectorNumber, false, sectorsToRead, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            blockData = new byte[EFS_BBSIZE];

            if(offsetInSector + EFS_BBSIZE <= sectorData.Length)
                Array.Copy(sectorData, offsetInSector, blockData, 0, EFS_BBSIZE);
            else
                return ErrorNumber.InvalidArgument;
        }
        else
        {
            // Standard disk: basic blocks map directly to sectors (assuming 512-byte sectors)
            uint sectorsPerBb = EFS_BBSIZE / _imagePlugin.Info.SectorSize;

            if(sectorsPerBb == 0) sectorsPerBb = 1;

            ulong sectorNumber = _partition.Start + (ulong)blockNumber * sectorsPerBb;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(sectorNumber, false, sectorsPerBb, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            if(sectorData.Length >= EFS_BBSIZE)
            {
                blockData = new byte[EFS_BBSIZE];
                Array.Copy(sectorData, 0, blockData, 0, EFS_BBSIZE);
            }
            else
                blockData = sectorData;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory contents from an inode</summary>
    /// <param name="inode">Directory inode</param>
    /// <param name="entries">Dictionary of filename to inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(Inode inode, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>();

        if(inode.di_numextents <= 0 || inode.di_size <= 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Directory has no extents or zero size");

            return ErrorNumber.NoError;
        }

        // Read each extent
        for(var i = 0; i < inode.di_numextents && i < EFS_DIRECTEXTENTS; i++)
        {
            Extent extent = inode.di_extents[i];

            if(extent.Magic != 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Extent {0} has invalid magic: {1}", i, extent.Magic);

                continue;
            }

            uint blockNumber = extent.BlockNumber;
            byte length      = extent.Length;

            AaruLogging.Debug(MODULE_NAME, "Reading extent {0}: bn={1}, len={2}", i, blockNumber, length);

            // Read each block in the extent
            for(var j = 0; j < length; j++)
            {
                ErrorNumber errno = ReadBasicBlock((int)(blockNumber + j), out byte[] blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading directory block {0}: {1}", blockNumber + j, errno);

                    continue;
                }

                // Parse directory block
                errno = ParseDirectoryBlock(blockData, entries);

                if(errno != ErrorNumber.NoError)
                    AaruLogging.Debug(MODULE_NAME, "Error parsing directory block: {0}", errno);
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a directory block and extracts entries</summary>
    /// <param name="blockData">Directory block data</param>
    /// <param name="entries">Dictionary to add entries to</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseDirectoryBlock(byte[] blockData, Dictionary<string, uint> entries)
    {
        if(blockData.Length < EFS_DIRBLK_HEADERSIZE) return ErrorNumber.InvalidArgument;

        // Parse directory block header
        DirectoryBlock dirBlock =
            Marshal.ByteArrayToStructureBigEndian<DirectoryBlock>(blockData, 0, EFS_DIRBLK_HEADERSIZE);

        if(dirBlock.magic != EFS_DIRBLK_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid directory block magic: 0x{0:X4}", dirBlock.magic);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Directory block: slots={0}, firstused={1}", dirBlock.slots, dirBlock.firstused);

        // Process each slot
        for(var slot = 0; slot < dirBlock.slots; slot++)
        {
            // Get the offset for this slot (stored after header)
            byte compactOffset = blockData[EFS_DIRBLK_HEADERSIZE + slot];

            // Check for free slot
            if(compactOffset == 0xFF) continue;

            // Convert compact offset to real offset (multiply by 2)
            int realOffset = compactOffset << 1;

            if(realOffset == 0 || realOffset >= blockData.Length) continue;

            // Parse directory entry
            if(realOffset + 5 > blockData.Length) continue;

            // Read inode number (big-endian, stored as two 16-bit values)
            var inumHigh = (ushort)(blockData[realOffset]     << 8  | blockData[realOffset + 1]);
            var inumLow  = (ushort)(blockData[realOffset + 2] << 8  | blockData[realOffset + 3]);
            var inum     = (uint)(inumHigh                    << 16 | inumLow);

            // Read name length
            byte nameLen = blockData[realOffset + 4];

            if(nameLen == 0 || realOffset + 5 + nameLen > blockData.Length) continue;

            // Read name
            var nameBytes = new byte[nameLen];
            Array.Copy(blockData, realOffset + 5, nameBytes, 0, nameLen);
            string name = _encoding.GetString(nameBytes);

            if(string.IsNullOrWhiteSpace(name)) continue;

            if(entries.ContainsKey(name)) continue;

            entries[name] = inum;
            AaruLogging.Debug(MODULE_NAME, "Found entry: '{0}' -> inode {1}", name, inum);
        }

        return ErrorNumber.NoError;
    }
}