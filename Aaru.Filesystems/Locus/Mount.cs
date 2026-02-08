// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin
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
public sealed partial class Locus
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting Locus volume");

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

        // Calculate block size based on version
        // SB_SB4096 = smallblock filesystem with 4096-byte blocks
        // SB_B1024 = regular 1024-byte block filesystem
        _blockSize = _superblock.s_version == Version.SB_SB4096 ? 4096 : 1024;

        // Smallblock filesystems use 512-byte inodes (with inline data buffer)
        // Regular filesystems use 128-byte inodes
        _smallBlocks = _superblock.s_version == Version.SB_SB4096;
        int inodeSize = _smallBlocks ? DINODE_SMALLBLOCK_SIZE : DINODE_SIZE;

        AaruLogging.Debug(MODULE_NAME, "Block size: {0}",            _blockSize);
        AaruLogging.Debug(MODULE_NAME, "Smallblock filesystem: {0}", _smallBlocks);
        AaruLogging.Debug(MODULE_NAME, "Inode size: {0}",            inodeSize);

        // Calculate inodes per block
        _inodesPerBlock = _blockSize / inodeSize;

        AaruLogging.Debug(MODULE_NAME, "Inodes per block: {0}", _inodesPerBlock);

        // Load root directory (inode 2 is always root)
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        string volumeName = StringHandlers.CToString(_superblock.s_fsmnt, _encoding);

        if(string.IsNullOrEmpty(volumeName)) volumeName = StringHandlers.CToString(_superblock.s_fpack, _encoding);

        Metadata = new FileSystem
        {
            Type = FS_TYPE,
            VolumeName = volumeName,
            ClusterSize = (uint)_blockSize,
            Clusters = (ulong)_superblock.s_fsize,
            FreeClusters = (ulong)_superblock.s_tfree,
            Dirty = !_superblock.s_flags.HasFlag(Flags.SB_CLEAN) || _superblock.s_flags.HasFlag(Flags.SB_DIRTY),
            ModificationDate = DateHandlers.UnixToDateTime(_superblock.s_time)
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
        _smallBlockDataCache.Clear();
        _mounted        = false;
        _imagePlugin    = null;
        _partition      = default(Partition);
        _superblock     = default(Superblock);
        _encoding       = null;
        _blockSize      = 0;
        _inodesPerBlock = 0;
        _smallBlocks    = false;
        Metadata        = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the Locus superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        uint sectorSize   = _imagePlugin.Info.SectorSize;
        int  sbStructSize = Marshal.SizeOf<Superblock>();

        // Superblock can be at sectors 0-8, search for it
        for(ulong location = 0; location <= 8; location++)
        {
            var sbSize = (uint)(sbStructSize / sectorSize);

            if(sbStructSize % sectorSize != 0) sbSize++;

            if(_partition.Start + location + sbSize >= _imagePlugin.Info.Sectors) continue;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(_partition.Start + location, false, sbSize, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) continue;

            if(sector.Length < sbStructSize) continue;

            Superblock sb = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

            AaruLogging.Debug(MODULE_NAME, "Magic at location {0} = 0x{1:X8}", location, sb.s_magic);

            if(sb.s_magic is not LOCUS_MAGIC and not LOCUS_CIGAM and not LOCUS_MAGIC_OLD and not LOCUS_CIGAM_OLD)
                continue;

            // Handle big-endian superblock
            if(sb.s_magic is LOCUS_CIGAM or LOCUS_CIGAM_OLD)
            {
                _superblock         = Marshal.ByteArrayToStructureBigEndian<Superblock>(sector);
                _superblock.s_flags = (Flags)Swapping.Swap((ushort)_superblock.s_flags);
                _bigEndian          = true;
            }
            else
            {
                _superblock = sb;
                _bigEndian  = false;
            }

            _superblockLocation = location;

            AaruLogging.Debug(MODULE_NAME, "Superblock found at location {0}", location);
            AaruLogging.Debug(MODULE_NAME, "s_magic = 0x{0:X8}",               _superblock.s_magic);
            AaruLogging.Debug(MODULE_NAME, "s_gfs = {0}",                      _superblock.s_gfs);
            AaruLogging.Debug(MODULE_NAME, "s_fsize = {0}",                    _superblock.s_fsize);
            AaruLogging.Debug(MODULE_NAME, "s_isize = {0}",                    _superblock.s_isize);
            AaruLogging.Debug(MODULE_NAME, "s_tfree = {0}",                    _superblock.s_tfree);
            AaruLogging.Debug(MODULE_NAME, "s_tinode = {0}",                   _superblock.s_tinode);
            AaruLogging.Debug(MODULE_NAME, "s_flags = {0}",                    _superblock.s_flags);
            AaruLogging.Debug(MODULE_NAME, "s_version = {0}",                  _superblock.s_version);

            return ErrorNumber.NoError;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock not found");

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();

        // Root inode is always inode 2
        ErrorNumber errno = ReadInode(ROOT_INO, out Dinode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate that root inode is a directory
        var fileType = (FileMode)(rootInode.di_mode & (ushort)FileMode.IFMT);

        if(fileType != FileMode.IFDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (type=0x{0:X4})", (ushort)fileType);

            return ErrorNumber.InvalidArgument;
        }

        _inodeCache[ROOT_INO] = rootInode;

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.di_size);

        // Read directory contents
        errno = ReadDirectoryContents(ROOT_INO, rootInode, out Dictionary<string, int> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory contents: {0}", errno);

            return errno;
        }

        // Cache entries (skip . and ..)
        foreach(KeyValuePair<string, int> entry in entries)
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
    /// <param name="inode">The read inode structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(int inodeNumber, out Dinode inode)
    {
        inode = default(Dinode);

        if(inodeNumber < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNumber);

            return ErrorNumber.InvalidArgument;
        }

        // Check cache first
        if(_inodeCache.TryGetValue(inodeNumber, out inode)) return ErrorNumber.NoError;

        // Inode numbers are 1-based in Locus
        // itod(x) = ((x-1) / INOPB) + 2  - converts inode number to block number
        // itoo(x) = (x-1) % INOPB        - converts inode number to offset within block
        // Block 0 = boot block
        // Block 1 = superblock
        // Blocks 2+ = inode list
        int inodeSize   = _smallBlocks ? DINODE_SMALLBLOCK_SIZE : DINODE_SIZE;
        int inodeBlock  = (inodeNumber - 1) / _inodesPerBlock + 2;
        int inodeOffset = (inodeNumber - 1) % _inodesPerBlock * inodeSize;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0}: block {1}, offset {2}, inode size {3}",
                          inodeNumber,
                          inodeBlock,
                          inodeOffset,
                          inodeSize);

        ErrorNumber errno = ReadBlock(inodeBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode block: {0}", errno);

            return errno;
        }

        if(inodeOffset + inodeSize > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode offset exceeds block size");

            return ErrorNumber.InvalidArgument;
        }

        var inodeData = new byte[inodeSize];
        Array.Copy(blockData, inodeOffset, inodeData, 0, inodeSize);

        // Debug: Check if inode data is all zeros
        var allZeros = true;

        for(var i = 0; i < inodeSize && allZeros; i++)
        {
            if(inodeData[i] != 0) allZeros = false;
        }

        if(allZeros)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "WARNING: Inode {0} data is all zeros at block {1} offset {2}",
                              inodeNumber,
                              inodeBlock,
                              inodeOffset);
        }

        inode = _bigEndian
                    ? Marshal.ByteArrayToStructureBigEndian<Dinode>(inodeData)
                    : Marshal.ByteArrayToStructureLittleEndian<Dinode>(inodeData);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: mode=0x{1:X4}, size={2}, nlink={3}",
                          inodeNumber,
                          inode.di_mode,
                          inode.di_size,
                          inode.di_nlink);

        _inodeCache[inodeNumber] = inode;

        // For smallblock filesystems, check if inline data is present
        if(_smallBlocks && inodeSize == DINODE_SMALLBLOCK_SIZE)
        {
            // di_sbflag is at offset 75 (after 27 bytes of padding at offset 48)
            // di_pad[27] starts at offset 48 (after di_blocks at offset 44, 4 bytes)
            // di_sbflag is at offset 48 + 27 = 75
            // di_addr[13] starts at offset 76
            // di_sbbuf[384] starts at offset 76 + 52 = 128
            const int sbflagOffset = 75;
            const int sbbufOffset  = 128; // 76 + (13 * 4) = 76 + 52 = 128

            byte sbflag = inodeData[sbflagOffset];

            AaruLogging.Debug(MODULE_NAME, "Inode {0}: sbflag=0x{1:X2}", inodeNumber, sbflag);

            if((sbflag & SBINUSE) != 0)
            {
                // Extract inline data from di_sbbuf
                var inlineData = new byte[SMBLKSZ];
                Array.Copy(inodeData, sbbufOffset, inlineData, 0, SMBLKSZ);
                _smallBlockDataCache[inodeNumber] = inlineData;

                AaruLogging.Debug(MODULE_NAME,
                                  "Inode {0}: Cached {1} bytes of inline smallblock data",
                                  inodeNumber,
                                  SMBLKSZ);
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a filesystem block</summary>
    /// <param name="blockNumber">Block number to read</param>
    /// <param name="data">The read block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlock(int blockNumber, out byte[] data)
    {
        data = null;

        if(blockNumber < 0 || blockNumber >= _superblock.s_fsize)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid block number: {0}", blockNumber);

            return ErrorNumber.InvalidArgument;
        }

        uint sectorSize      = _imagePlugin.Info.SectorSize;
        uint sectorsPerBlock = (uint)_blockSize / sectorSize;

        ulong sectorNumber = _partition.Start + (ulong)blockNumber * sectorsPerBlock;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, sectorsPerBlock, out data, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading block {0}: {1}", blockNumber, errno);

            return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory contents from an inode</summary>
    /// <param name="inodeNumber">Inode number</param>
    /// <param name="inode">Directory inode</param>
    /// <param name="entries">Dictionary of filename to inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(int inodeNumber, Dinode inode, out Dictionary<string, int> entries)
    {
        entries = new Dictionary<string, int>();

        AaruLogging.Debug(MODULE_NAME,
                          "ReadDirectoryContents: size={0}, blocks={1}, dflag=0x{2:X4}",
                          inode.di_size,
                          inode.di_blocks,
                          inode.di_dflag);

        if(inode.di_size <= 0)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadDirectoryContents: Directory has zero size");

            return ErrorNumber.NoError;
        }

        // Debug: Log first few block addresses
        if(inode.di_addr != null)
        {
            for(var i = 0; i < Math.Min(5, inode.di_addr.Length); i++)
                AaruLogging.Debug(MODULE_NAME, "ReadDirectoryContents: di_addr[{0}] = {1}", i, inode.di_addr[i]);
        }
        else
            AaruLogging.Debug(MODULE_NAME, "ReadDirectoryContents: di_addr is NULL!");

        // Check if this is a long directory (BSD 4.3 format) or old format
        bool longDir = (inode.di_dflag & (short)DiskFlags.DILONGDIR) != 0;

        AaruLogging.Debug(MODULE_NAME, "Directory format: {0}", longDir ? "long (BSD 4.3)" : "old (System V)");

        // Read all directory data
        ErrorNumber errno = ReadFileData(inodeNumber, inode, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory data: {0}", errno);

            return errno;
        }

        if(longDir)
            ParseLongDirectory(dirData, entries);
        else
            ParseOldDirectory(dirData, entries);

        return ErrorNumber.NoError;
    }

    /// <summary>Parses old System V format directory</summary>
    /// <param name="data">Raw directory data</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    void ParseOldDirectory(byte[] data, Dictionary<string, int> entries)
    {
        var offset = 0;

        while(offset + DIRSIZ + 2 <= data.Length) // 2 bytes for inode + 14 bytes for name
        {
            short ino = _bigEndian
                            ? (short)(data[offset] << 8 | data[offset + 1])
                            : (short)(data[offset]      | data[offset + 1] << 8);

            offset += 2;

            if(ino == 0)
            {
                offset += DIRSIZ;

                continue;
            }

            // Extract name (14 bytes, null-padded)
            var nameLen = 0;

            for(var i = 0; i < DIRSIZ && data[offset + i] != 0; i++) nameLen++;

            string name = _encoding.GetString(data, offset, nameLen);
            offset += DIRSIZ;

            if(!string.IsNullOrEmpty(name) && !entries.ContainsKey(name))
            {
                entries[name] = ino;

                AaruLogging.Debug(MODULE_NAME, "Old dir entry: {0} -> inode {1}", name, ino);
            }
        }
    }

    /// <summary>Parses long BSD 4.3 format directory</summary>
    /// <param name="data">Raw directory data</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    void ParseLongDirectory(byte[] data, Dictionary<string, int> entries)
    {
        var offset = 0;

        while(offset < data.Length)
        {
            if(offset + 8 > data.Length) break;

            int ino = _bigEndian
                          ? data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]
                          : data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;

            ushort reclen = _bigEndian
                                ? (ushort)(data[offset + 4] << 8 | data[offset + 5])
                                : (ushort)(data[offset                         + 4] | data[offset + 5] << 8);

            ushort namlen = _bigEndian
                                ? (ushort)(data[offset + 6] << 8 | data[offset + 7])
                                : (ushort)(data[offset                         + 6] | data[offset + 7] << 8);

            if(reclen == 0) break;

            if(ino != 0 && namlen > 0 && offset + 8 + namlen <= data.Length)
            {
                string name = _encoding.GetString(data, offset + 8, namlen);

                if(!string.IsNullOrEmpty(name) && !entries.ContainsKey(name))
                {
                    entries[name] = ino;

                    AaruLogging.Debug(MODULE_NAME, "Long dir entry: {0} -> inode {1}", name, ino);
                }
            }

            offset += reclen;
        }
    }

    /// <summary>Reads all data from a file inode</summary>
    /// <param name="inodeNumber">Inode number</param>
    /// <param name="inode">File inode</param>
    /// <param name="data">The file data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFileData(int inodeNumber, Dinode inode, out byte[] data)
    {
        data = null;

        if(inode.di_size <= 0)
        {
            data = [];

            return ErrorNumber.NoError;
        }

        AaruLogging.Debug(MODULE_NAME, "ReadFileData: Reading {0} bytes for inode {1}", inode.di_size, inodeNumber);

        // Check for smallblock inline data first
        if(_smallBlocks && _smallBlockDataCache.TryGetValue(inodeNumber, out byte[] inlineData))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFileData: Using inline smallblock data");

            // Copy inline data up to file size
            int copySize = Math.Min(inode.di_size, inlineData.Length);
            data = new byte[inode.di_size];
            Array.Copy(inlineData, 0, data, 0, copySize);

            return ErrorNumber.NoError;
        }

        data = new byte[inode.di_size];
        var bytesRead = 0;

        // Check if di_addr is valid
        if(inode.di_addr == null || inode.di_addr.Length == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFileData: di_addr is null or empty!");

            return ErrorNumber.InvalidArgument;
        }

        // Direct blocks (first NDADDR = 10 blocks)
        for(var i = 0; i < NDADDR && bytesRead < inode.di_size; i++)
        {
            int blockNum = inode.di_addr[i];

            AaruLogging.Debug(MODULE_NAME, "ReadFileData: Direct block[{0}] = {1}", i, blockNum);

            if(blockNum == 0)
            {
                // Sparse file - fill with zeros
                int toFill = Math.Min(_blockSize, inode.di_size - bytesRead);
                bytesRead += toFill;

                AaruLogging.Debug(MODULE_NAME, "ReadFileData: Sparse block, filled {0} zeros", toFill);

                continue;
            }

            ErrorNumber errno = ReadBlock(blockNum, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading direct block {0}: {1}", blockNum, errno);

                return errno;
            }

            int toCopy = Math.Min(blockData.Length, inode.di_size - bytesRead);
            Array.Copy(blockData, 0, data, bytesRead, toCopy);
            bytesRead += toCopy;
        }

        if(bytesRead >= inode.di_size) return ErrorNumber.NoError;

        // Single indirect block
        if(inode.di_addr[NDADDR] != 0)
        {
            ErrorNumber errno = ReadIndirectBlock(inode.di_addr[NDADDR], 1, ref data, ref bytesRead, inode.di_size);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(bytesRead >= inode.di_size) return ErrorNumber.NoError;

        // Double indirect block
        if(inode.di_addr[NDADDR + 1] != 0)
        {
            ErrorNumber errno = ReadIndirectBlock(inode.di_addr[NDADDR + 1], 2, ref data, ref bytesRead, inode.di_size);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(bytesRead >= inode.di_size) return ErrorNumber.NoError;

        // Triple indirect block
        if(inode.di_addr[NDADDR + 2] != 0)
        {
            ErrorNumber errno = ReadIndirectBlock(inode.di_addr[NDADDR + 2], 3, ref data, ref bytesRead, inode.di_size);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an indirect block and its data</summary>
    /// <param name="blockNum">Indirect block number</param>
    /// <param name="level">Indirection level (1=single, 2=double, 3=triple)</param>
    /// <param name="data">Data buffer to fill</param>
    /// <param name="bytesRead">Current bytes read</param>
    /// <param name="fileSize">Total file size</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadIndirectBlock(int blockNum, int level, ref byte[] data, ref int bytesRead, int fileSize)
    {
        if(blockNum == 0 || bytesRead >= fileSize) return ErrorNumber.NoError;

        ErrorNumber errno = ReadBlock(blockNum, out byte[] indirectData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading indirect block {0}: {1}", blockNum, errno);

            return errno;
        }

        int pointersPerBlock = _blockSize / 4; // 4 bytes per block pointer

        for(var i = 0; i < pointersPerBlock && bytesRead < fileSize; i++)
        {
            int offset = i * 4;

            int pointer = _bigEndian
                              ? indirectData[offset]     << 24 |
                                indirectData[offset + 1] << 16 |
                                indirectData[offset + 2] << 8  |
                                indirectData[offset + 3]
                              : indirectData[offset]           |
                                indirectData[offset + 1] << 8  |
                                indirectData[offset + 2] << 16 |
                                indirectData[offset + 3] << 24;

            if(pointer == 0)
            {
                // Sparse file
                int toFill = Math.Min(_blockSize, fileSize - bytesRead);
                bytesRead += toFill;

                continue;
            }

            if(level == 1)
            {
                // Direct data block
                errno = ReadBlock(pointer, out byte[] blockData);

                if(errno != ErrorNumber.NoError) return errno;

                int toCopy = Math.Min(blockData.Length, fileSize - bytesRead);
                Array.Copy(blockData, 0, data, bytesRead, toCopy);
                bytesRead += toCopy;
            }
            else
            {
                // Another level of indirection
                errno = ReadIndirectBlock(pointer, level - 1, ref data, ref bytesRead, fileSize);

                if(errno != ErrorNumber.NoError) return errno;
            }
        }

        return ErrorNumber.NoError;
    }
}