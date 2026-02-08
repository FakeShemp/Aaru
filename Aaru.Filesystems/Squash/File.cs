// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read root inode
            ErrorNumber errno = ReadRootInodeStat(out stat);

            return errno;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo entry))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // If this is the last component, return its stat
            if(p == pathComponents.Length - 1)
            {
                ErrorNumber errno = ReadInodeStat(entry.InodeBlock, entry.InodeOffset, out stat);

                return errno;
            }

            // Not the last component - must be a directory
            if(entry.Type != SquashInodeType.Directory && entry.Type != SquashInodeType.ExtendedDirectory)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory inode to get directory parameters
            ErrorNumber dirErrno = ReadDirectoryInode(entry.InodeBlock,
                                                      entry.InodeOffset,
                                                      out uint dirStartBlock,
                                                      out uint dirOffset,
                                                      out uint dirSize);

            if(dirErrno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory inode: {0}", dirErrno);

                return dirErrno;
            }

            // Read directory contents for next iteration
            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            dirErrno = ReadDirectoryContents(dirStartBlock, dirOffset, dirSize, dirEntries);

            if(dirErrno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory contents: {0}", dirErrno);

                return dirErrno;
            }

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Look up the file in the directory tree
        ErrorNumber errno = LookupFile(path, out DirectoryEntryInfo entry);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LookupFile failed with {0}", errno);

            return errno;
        }

        // Check it's not a directory
        if(entry.Type is SquashInodeType.Directory or SquashInodeType.ExtendedDirectory)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: path is a directory");

            return ErrorNumber.IsDirectory;
        }

        // Check it's a regular file
        if(entry.Type is not SquashInodeType.RegularFile and not SquashInodeType.ExtendedRegularFile)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: path is not a regular file, type={0}", entry.Type);

            return ErrorNumber.InvalidArgument;
        }

        // Read the file inode
        errno = ReadFileInode(entry.InodeBlock, entry.InodeOffset, out SquashFileNode fileNode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadFileInode failed with {0}", errno);

            return errno;
        }

        fileNode.Path = path;

        AaruLogging.Debug(MODULE_NAME,
                          "OpenFile: success, size={0}, blocks={1}, fragment={2}",
                          fileNode.Length,
                          fileNode.BlockCount,
                          fileNode.Fragment);

        node = fileNode;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not SquashFileNode fileNode) return ErrorNumber.InvalidArgument;

        // Clear cached data
        fileNode.CachedBlock      = null;
        fileNode.CachedBlockIndex = -1;
        fileNode.CachedFragment   = null;
        fileNode.Path             = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not SquashFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate which block contains the current offset
            var blockIndex    = (int)(currentOffset / _superBlock.block_size);
            var offsetInBlock = (int)(currentOffset % _superBlock.block_size);

            // Check if we're reading from a fragment (last part of file that doesn't fill a full block)
            bool isFragment = blockIndex >= fileNode.BlockCount && fileNode.Fragment != SQUASHFS_INVALID_FRAG;

            byte[] blockData;

            if(isFragment)
            {
                // Read from fragment
                ErrorNumber errno = ReadFragment(fileNode, out blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "ReadFile: ReadFragment failed: {0}", errno);

                    if(bytesRead > 0) break;

                    return errno;
                }

                // Adjust offset to be relative to fragment
                offsetInBlock = (int)fileNode.FragmentOffset +
                                (int)(currentOffset - (long)fileNode.BlockCount * _superBlock.block_size);
            }
            else
            {
                // Read from data block
                ErrorNumber errno = ReadDataBlock(fileNode, blockIndex, out blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadFile: ReadDataBlock failed for block {0}: {1}",
                                      blockIndex,
                                      errno);

                    if(bytesRead > 0) break;

                    return errno;
                }
            }

            if(blockData == null || blockData.Length == 0)
            {
                // Sparse block - fill with zeros
                int zerosToCopy = Math.Min((int)_superBlock.block_size - offsetInBlock, (int)(toRead - bytesRead));
                Array.Clear(buffer, (int)bytesRead, zerosToCopy);
                bytesRead     += zerosToCopy;
                currentOffset += zerosToCopy;

                continue;
            }

            // Calculate how much data to copy from this block
            int availableInBlock = blockData.Length - offsetInBlock;
            int toCopy           = Math.Min(availableInBlock, (int)(toRead - bytesRead));

            if(toCopy <= 0) break;

            Array.Copy(blockData, offsetInBlock, buffer, bytesRead, toCopy);

            bytesRead     += toCopy;
            currentOffset += toCopy;
        }

        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: read {0} bytes, new offset={1}", read, fileNode.Offset);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "ReadLink: path='{0}'", path);

        // Look up the symlink in the directory tree
        ErrorNumber errno = LookupFile(path, out DirectoryEntryInfo entry);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: LookupFile failed with {0}", errno);

            return errno;
        }

        // Check it's a symlink
        if(entry.Type is not SquashInodeType.Symlink and not SquashInodeType.ExtendedSymlink)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: path is not a symlink, type={0}", entry.Type);

            return ErrorNumber.InvalidArgument;
        }

        // Read the symlink inode
        int symlinkInodeSize = Marshal.SizeOf<SymlinkInode>();

        errno = ReadInodeData(entry.InodeBlock, entry.InodeOffset, symlinkInodeSize, out byte[] inodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadInodeData failed with {0}", errno);

            return errno;
        }

        SymlinkInode symlinkInode = _littleEndian
                                        ? Helpers.Marshal.ByteArrayToStructureLittleEndian<SymlinkInode>(inodeData)
                                        : Helpers.Marshal.ByteArrayToStructureBigEndian<SymlinkInode>(inodeData);

        if(symlinkInode.symlink_size == 0)
        {
            dest = string.Empty;

            return ErrorNumber.NoError;
        }

        // The symlink target data immediately follows the inode structure in the metadata
        // Read the inode data plus the symlink target
        int totalSize = symlinkInodeSize + (int)symlinkInode.symlink_size;

        errno = ReadInodeData(entry.InodeBlock, entry.InodeOffset, totalSize, out byte[] fullData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadInodeData (with target) failed with {0}", errno);

            return errno;
        }

        // Extract the symlink target (after the inode structure)
        dest = _encoding.GetString(fullData, symlinkInodeSize, (int)symlinkInode.symlink_size);

        AaruLogging.Debug(MODULE_NAME, "ReadLink: target='{0}'", dest);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a file inode and creates a file node</summary>
    /// <param name="inodeBlock">Block containing the inode (relative to inode table)</param>
    /// <param name="inodeOffset">Offset within the metadata block</param>
    /// <param name="fileNode">Output file node</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFileInode(uint inodeBlock, ushort inodeOffset, out SquashFileNode fileNode)
    {
        fileNode = null;

        // First read the base inode to determine the type
        int baseInodeSize = Marshal.SizeOf<BaseInode>();

        ErrorNumber errno = ReadInodeData(inodeBlock, inodeOffset, baseInodeSize, out byte[] baseInodeData);

        if(errno != ErrorNumber.NoError) return errno;

        BaseInode baseInode = _littleEndian
                                  ? Helpers.Marshal.ByteArrayToStructureLittleEndian<BaseInode>(baseInodeData)
                                  : Helpers.Marshal.ByteArrayToStructureBigEndian<BaseInode>(baseInodeData);

        var inodeType = (SquashInodeType)baseInode.inode_type;

        if(inodeType == SquashInodeType.RegularFile)
        {
            int regInodeSize = Marshal.SizeOf<RegInode>();

            errno = ReadInodeData(inodeBlock, inodeOffset, regInodeSize, out byte[] regInodeData);

            if(errno != ErrorNumber.NoError) return errno;

            RegInode regInode = _littleEndian
                                    ? Helpers.Marshal.ByteArrayToStructureLittleEndian<RegInode>(regInodeData)
                                    : Helpers.Marshal.ByteArrayToStructureBigEndian<RegInode>(regInodeData);

            // Calculate number of data blocks
            uint blockCount = regInode.file_size / _superBlock.block_size;

            // If there's no fragment, the last partial block is a full data block
            if(regInode.fragment == SQUASHFS_INVALID_FRAG && regInode.file_size % _superBlock.block_size != 0)
                blockCount++;

            fileNode = new SquashFileNode
            {
                Length          = regInode.file_size,
                Offset          = 0,
                StartBlock      = regInode.start_block,
                Fragment        = regInode.fragment,
                FragmentOffset  = regInode.offset,
                BlockListStart  = inodeBlock,
                BlockListOffset = (ushort)(inodeOffset + regInodeSize),
                BlockCount      = blockCount,
                IsExtended      = false,
                Sparse          = 0
            };
        }
        else if(inodeType == SquashInodeType.ExtendedRegularFile)
        {
            int extRegInodeSize = Marshal.SizeOf<ExtendedRegInode>();

            errno = ReadInodeData(inodeBlock, inodeOffset, extRegInodeSize, out byte[] extRegInodeData);

            if(errno != ErrorNumber.NoError) return errno;

            ExtendedRegInode extRegInode = _littleEndian
                                               ? Helpers.Marshal
                                                        .ByteArrayToStructureLittleEndian<
                                                             ExtendedRegInode>(extRegInodeData)
                                               : Helpers.Marshal
                                                        .ByteArrayToStructureBigEndian<
                                                             ExtendedRegInode>(extRegInodeData);

            // Calculate number of data blocks
            var blockCount = (uint)(extRegInode.file_size / _superBlock.block_size);

            // If there's no fragment, the last partial block is a full data block
            if(extRegInode.fragment == SQUASHFS_INVALID_FRAG && extRegInode.file_size % _superBlock.block_size != 0)
                blockCount++;

            fileNode = new SquashFileNode
            {
                Length          = (long)extRegInode.file_size,
                Offset          = 0,
                StartBlock      = extRegInode.start_block,
                Fragment        = extRegInode.fragment,
                FragmentOffset  = extRegInode.offset,
                BlockListStart  = inodeBlock,
                BlockListOffset = (ushort)(inodeOffset + extRegInodeSize),
                BlockCount      = blockCount,
                IsExtended      = true,
                Sparse          = extRegInode.sparse
            };
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFileInode: Not a regular file, type={0}", inodeType);

            return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a data block from a file</summary>
    /// <param name="fileNode">The file node</param>
    /// <param name="blockIndex">Block index (0-based)</param>
    /// <param name="blockData">Output decompressed block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDataBlock(SquashFileNode fileNode, int blockIndex, out byte[] blockData)
    {
        blockData = null;

        if(blockIndex < 0 || blockIndex >= fileNode.BlockCount) return ErrorNumber.InvalidArgument;

        // Check if this block is cached
        if(fileNode.CachedBlockIndex == blockIndex && fileNode.CachedBlock != null)
        {
            blockData = fileNode.CachedBlock;

            return ErrorNumber.NoError;
        }

        // Read the block size list to find the compressed size of this block
        // The block list immediately follows the inode structure in the metadata
        ErrorNumber errno = ReadBlockList(fileNode, blockIndex, out uint blockSize, out ulong blockOffset);

        if(errno != ErrorNumber.NoError) return errno;

        // Check for sparse block (size = 0)
        if(blockSize == 0)
        {
            blockData = null; // Sparse block returns null

            return ErrorNumber.NoError;
        }

        // Check compression flag (bit 24)
        bool compressed     = (blockSize      & SQUASHFS_COMPRESSED_BIT_BLOCK) == 0;
        var  compressedSize = (int)(blockSize & ~SQUASHFS_COMPRESSED_BIT_BLOCK);

        // Read the block data from disk
        ulong blockPosition = fileNode.StartBlock + blockOffset;

        errno = ReadRawData(blockPosition, compressedSize, out byte[] rawData);

        if(errno != ErrorNumber.NoError) return errno;

        if(!compressed)
            blockData = rawData;
        else
        {
            // Decompress the block
            blockData = new byte[_superBlock.block_size];
            int decompressedSize = DecompressBlock(rawData, blockData);

            if(decompressedSize < 0)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadDataBlock: Decompression failed for block {0}", blockIndex);

                return ErrorNumber.InvalidArgument;
            }

            // Resize to actual size if smaller (last block)
            if(decompressedSize < blockData.Length) Array.Resize(ref blockData, decompressedSize);
        }

        // Cache this block for read-ahead (improves sequential read performance)
        fileNode.CachedBlock      = blockData;
        fileNode.CachedBlockIndex = blockIndex;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the block list to get block size and offset</summary>
    /// <param name="fileNode">The file node</param>
    /// <param name="blockIndex">Block index</param>
    /// <param name="blockSize">Output: compressed size with flags</param>
    /// <param name="blockOffset">Output: offset from start_block</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlockList(SquashFileNode fileNode, int blockIndex, out uint blockSize, out ulong blockOffset)
    {
        blockSize   = 0;
        blockOffset = 0;

        // Read all block sizes up to and including the requested block
        // Each block size is 4 bytes (uint32)
        int bytesToRead = (blockIndex + 1) * 4;

        ErrorNumber errno = ReadInodeData(fileNode.BlockListStart,
                                          fileNode.BlockListOffset,
                                          bytesToRead,
                                          out byte[] blockListData);

        if(errno != ErrorNumber.NoError) return errno;

        // Calculate offset by summing sizes of previous blocks
        ulong offset = 0;

        for(var i = 0; i < blockIndex; i++)
        {
            uint size = _littleEndian
                            ? BitConverter.ToUInt32(blockListData, i * 4)
                            : (uint)(blockListData[i * 4]     << 24 |
                                     blockListData[i * 4 + 1] << 16 |
                                     blockListData[i * 4 + 2] << 8  |
                                     blockListData[i * 4 + 3]);

            // Get compressed size (mask off flags)
            var compSize = (int)(size & ~SQUASHFS_COMPRESSED_BIT_BLOCK);
            offset += (uint)compSize;
        }

        blockOffset = offset;

        // Get the size of the requested block
        blockSize = _littleEndian
                        ? BitConverter.ToUInt32(blockListData, blockIndex * 4)
                        : (uint)(blockListData[blockIndex * 4]     << 24 |
                                 blockListData[blockIndex * 4 + 1] << 16 |
                                 blockListData[blockIndex * 4 + 2] << 8  |
                                 blockListData[blockIndex * 4 + 3]);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and caches fragment data</summary>
    /// <param name="fileNode">The file node</param>
    /// <param name="fragmentData">Output fragment data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFragment(SquashFileNode fileNode, out byte[] fragmentData)
    {
        fragmentData = null;

        if(fileNode.Fragment == SQUASHFS_INVALID_FRAG) return ErrorNumber.InvalidArgument;

        // Check if fragment is already cached
        if(fileNode.CachedFragment != null)
        {
            fragmentData = fileNode.CachedFragment;

            return ErrorNumber.NoError;
        }

        // Look up fragment in fragment table
        ErrorNumber errno = ReadFragmentEntry(fileNode.Fragment, out ulong fragBlock, out uint fragSize);

        if(errno != ErrorNumber.NoError) return errno;

        // Check compression flag
        bool compressed     = (fragSize      & SQUASHFS_COMPRESSED_BIT_BLOCK) == 0;
        var  compressedSize = (int)(fragSize & ~SQUASHFS_COMPRESSED_BIT_BLOCK);

        // Read fragment block
        errno = ReadRawData(fragBlock, compressedSize, out byte[] rawData);

        if(errno != ErrorNumber.NoError) return errno;

        if(!compressed)
            fragmentData = rawData;
        else
        {
            fragmentData = new byte[_superBlock.block_size];
            int decompressedSize = DecompressBlock(rawData, fragmentData);

            if(decompressedSize < 0) return ErrorNumber.InvalidArgument;

            if(decompressedSize < fragmentData.Length) Array.Resize(ref fragmentData, decompressedSize);
        }

        // Cache the fragment
        fileNode.CachedFragment = fragmentData;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a fragment table entry</summary>
    /// <param name="fragmentIndex">Fragment index</param>
    /// <param name="startBlock">Output: fragment start block</param>
    /// <param name="size">Output: fragment size with compression flag</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFragmentEntry(uint fragmentIndex, out ulong startBlock, out uint size)
    {
        startBlock = 0;
        size       = 0;

        if(fragmentIndex >= _superBlock.fragments) return ErrorNumber.InvalidArgument;

        // Fragment table is indexed by 8-byte entries
        // First read the fragment table index to find the metadata block
        var indexBlock  = (int)(fragmentIndex / (SQUASHFS_METADATA_SIZE / 16)); // entries per block
        int indexOffset = (int)(fragmentIndex % (SQUASHFS_METADATA_SIZE / 16)) * 16;

        // Read fragment table lookup
        ulong fragTableLookupPos = _superBlock.fragment_table_start + (ulong)(indexBlock * 8);

        ErrorNumber errno = ReadRawData(fragTableLookupPos, 8, out byte[] lookupData);

        if(errno != ErrorNumber.NoError) return errno;

        ulong metadataBlockPos = _littleEndian
                                     ? BitConverter.ToUInt64(lookupData, 0)
                                     : (ulong)(lookupData[0] << 56 |
                                               lookupData[1] << 48 |
                                               lookupData[2] << 40 |
                                               lookupData[3] << 32 |
                                               lookupData[4] << 24 |
                                               lookupData[5] << 16 |
                                               lookupData[6] << 8  |
                                               lookupData[7]);

        // Read fragment entry from metadata block
        errno = ReadMetadataBlock(metadataBlockPos, out byte[] metadataData);

        if(errno != ErrorNumber.NoError) return errno;

        // FragmentEntry is 16 bytes: 8 bytes start_block, 4 bytes size, 4 bytes unused
        if(indexOffset + 16 > metadataData.Length) return ErrorNumber.InvalidArgument;

        startBlock = _littleEndian
                         ? BitConverter.ToUInt64(metadataData, indexOffset)
                         : (ulong)((long)metadataData[indexOffset]     << 56 |
                                   (long)metadataData[indexOffset + 1] << 48 |
                                   (long)metadataData[indexOffset + 2] << 40 |
                                   (long)metadataData[indexOffset + 3] << 32 |
                                   (long)metadataData[indexOffset + 4] << 24 |
                                   (long)metadataData[indexOffset + 5] << 16 |
                                   (long)metadataData[indexOffset + 6] << 8  |
                                   metadataData[indexOffset + 7]);

        size = _littleEndian
                   ? BitConverter.ToUInt32(metadataData, indexOffset + 8)
                   : (uint)(metadataData[indexOffset + 8]  << 24 |
                            metadataData[indexOffset + 9]  << 16 |
                            metadataData[indexOffset + 10] << 8  |
                            metadataData[indexOffset + 11]);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads raw data from the filesystem</summary>
    /// <param name="position">Absolute byte position</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="data">Output data buffer</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadRawData(ulong position, int length, out byte[] data)
    {
        data = null;

        ulong byteOffset     = _partition.Start * _imagePlugin.Info.SectorSize + position;
        ulong sectorNumber   = byteOffset / _imagePlugin.Info.SectorSize;
        var   offsetInSector = (uint)(byteOffset % _imagePlugin.Info.SectorSize);

        var sectorsToRead = (uint)((offsetInSector + length + _imagePlugin.Info.SectorSize - 1) /
                                   _imagePlugin.Info.SectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        data = new byte[length];
        Array.Copy(sectorData, offsetInSector, data, 0, Math.Min(length, sectorData.Length - (int)offsetInSector));

        return ErrorNumber.NoError;
    }

    /// <summary>Looks up a file by path and returns its directory entry info</summary>
    /// <param name="path">Path to the file</param>
    /// <param name="entry">Output directory entry info</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupFile(string path, out DirectoryEntryInfo entry)
    {
        entry = null;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath == "/") return ErrorNumber.InvalidArgument;

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo foundEntry)) return ErrorNumber.NoSuchFile;

            // If this is the last component, return it
            if(p == pathComponents.Length - 1)
            {
                entry = foundEntry;

                return ErrorNumber.NoError;
            }

            // Not the last component - must be a directory
            if(foundEntry.Type is not SquashInodeType.Directory and not SquashInodeType.ExtendedDirectory)
                return ErrorNumber.NotDirectory;

            // Read directory inode to get directory parameters
            ErrorNumber errno = ReadDirectoryInode(foundEntry.InodeBlock,
                                                   foundEntry.InodeOffset,
                                                   out uint dirStartBlock,
                                                   out uint dirOffset,
                                                   out uint dirSize);

            if(errno != ErrorNumber.NoError) return errno;

            // Read directory contents for next iteration
            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            errno = ReadDirectoryContents(dirStartBlock, dirOffset, dirSize, dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }
}