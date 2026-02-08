// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles file operations
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
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        // Get the file entry
        ErrorNumber errno = GetEntry(path, out DirectoryEntryInfo entry);

        if(errno != ErrorNumber.NoError) return errno;

        // Check it's not a directory
        if((entry.Attributes & 0x08) != 0) return ErrorNumber.IsDirectory;

        node = new AcornFileNode
        {
            Path       = path,
            Offset     = 0,
            Length     = entry.Length,
            IndAddr    = entry.IndAddr,
            Attributes = entry.Attributes
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not AcornFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not AcornFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(length < 0) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        // Read file data
        ErrorNumber errno = ReadFileData(fileNode.IndAddr, fileNode.Offset, toRead, buffer, out read);

        if(errno != ErrorNumber.NoError) return errno;

        fileNode.Offset += read;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Get the file entry
        ErrorNumber errno = GetEntry(path, out DirectoryEntryInfo entry);

        if(errno != ErrorNumber.NoError) return errno;

        // Check it's a symbolic link (filetype 0xFC0 = LinkFS)
        if(!HasFiletype(entry.LoadAddr) || GetFiletype(entry.LoadAddr) != FILETYPE_LINKFS)
            return ErrorNumber.InvalidArgument;

        // Read the link target (stored as file content)
        if(entry.Length == 0)
        {
            dest = string.Empty;

            return ErrorNumber.NoError;
        }

        var buffer = new byte[entry.Length];

        errno = ReadFileData(entry.IndAddr, 0, entry.Length, buffer, out long read);

        if(errno != ErrorNumber.NoError) return errno;

        // Convert to string, trimming any null terminator
        dest = _encoding.GetString(buffer, 0, (int)read).TrimEnd('\0');

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetEntry(path, out DirectoryEntryInfo entry);

        if(err != ErrorNumber.NoError) return err;

        stat = EntryToFileEntryInfo(entry);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads file data from the specified indirect disc address</summary>
    /// <param name="indAddr">Indirect disc address of the file</param>
    /// <param name="offset">Offset within the file to start reading</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="buffer">Buffer to store the read data</param>
    /// <param name="read">Number of bytes actually read</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFileData(uint indAddr, long offset, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(_isOldMap) return ReadFileDataOldMap(indAddr, offset, length, buffer, out read);

        return ReadFileDataNewMap(indAddr, offset, length, buffer, out read);
    }

    /// <summary>Reads file data for old map format (contiguous allocation)</summary>
    /// <param name="indAddr">Indirect disc address (sector number * 256)</param>
    /// <param name="offset">Offset within the file</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="buffer">Buffer to store the read data</param>
    /// <param name="read">Number of bytes actually read</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFileDataOldMap(uint indAddr, long offset, long length, byte[] buffer, out long read)
    {
        read = 0;

        // For old map format, indAddr is the sector address / 256 (i.e., sector number in 256-byte sectors)
        // Files are stored contiguously starting at this address
        ulong byteOffset = indAddr * 256UL + (ulong)offset;

        ulong sector       = byteOffset / _imagePlugin.Info.SectorSize + _partition.Start;
        var   offsetInSect = (int)(byteOffset % _imagePlugin.Info.SectorSize);

        long bytesRead = 0;

        while(bytesRead < length)
        {
            // Calculate how many sectors to read (at least one)
            long remainingBytes = length                       - bytesRead;
            long bytesInSector  = _imagePlugin.Info.SectorSize - offsetInSect;
            long bytesToCopy    = Math.Min(remainingBytes, bytesInSector);

            // Read the sector
            ErrorNumber errno = _imagePlugin.ReadSector(sector, false, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            // Copy the relevant portion to the buffer
            Array.Copy(sectorData, offsetInSect, buffer, bytesRead, bytesToCopy);

            bytesRead    += bytesToCopy;
            sector       += 1;
            offsetInSect =  0; // After first sector, we read from the beginning
        }

        read = bytesRead;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads file data for new map format (using fragment map)</summary>
    /// <param name="indAddr">Indirect disc address (fragment ID + offset)</param>
    /// <param name="offset">Offset within the file</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="buffer">Buffer to store the read data</param>
    /// <param name="read">Number of bytes actually read</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFileDataNewMap(uint indAddr, long offset, long length, byte[] buffer, out long read)
    {
        read = 0;

        // For new map format, we need to map logical blocks to physical blocks
        // indAddr contains: fragment ID in upper 24 bits, offset within fragment in lower 8 bits
        // The __adfs_block_map function in Linux handles this

        long bytesRead     = 0;
        long currentOffset = offset;

        while(bytesRead < length)
        {
            // Calculate logical block number within the file
            long logicalBlock = currentOffset / _blockSize;

            // Map the logical block to a physical block
            ErrorNumber errno = MapBlock(indAddr, (uint)logicalBlock, out ulong physicalBlock);

            if(errno != ErrorNumber.NoError)
            {
                // If we've read some data, return what we have
                if(bytesRead > 0) break;

                return errno;
            }

            // Calculate offset within the block
            var offsetInBlock = (int)(currentOffset % _blockSize);

            // Read the block
            ulong sector          = physicalBlock * (ulong)_blockSize / _imagePlugin.Info.SectorSize + _partition.Start;
            var   sectorsPerBlock = (uint)(_blockSize / (int)_imagePlugin.Info.SectorSize);

            if(sectorsPerBlock == 0) sectorsPerBlock = 1;

            errno = _imagePlugin.ReadSectors(sector, false, sectorsPerBlock, out byte[] blockData, out _);

            if(errno != ErrorNumber.NoError)
            {
                // If we've read some data, return what we have
                if(bytesRead > 0) break;

                return errno;
            }

            // Calculate how much data to copy from this block
            long bytesToCopy = Math.Min(_blockSize - offsetInBlock, length - bytesRead);

            if(offsetInBlock + bytesToCopy > blockData.Length) bytesToCopy = blockData.Length - offsetInBlock;

            Array.Copy(blockData, offsetInBlock, buffer, bytesRead, bytesToCopy);

            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        read = bytesRead;

        return ErrorNumber.NoError;
    }

    /// <summary>Maps a logical block within a file to a physical block on disk</summary>
    /// <param name="indAddr">Indirect disc address of the file</param>
    /// <param name="logicalBlock">Logical block number within the file</param>
    /// <param name="physicalBlock">Output physical block number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber MapBlock(uint indAddr, uint logicalBlock, out ulong physicalBlock)
    {
        physicalBlock = 0;

        // Extract fragment ID and offset from indirect address
        // indAddr format: fragment ID in bits [31:8], offset in bits [7:0]
        uint fragId     = indAddr >> 8;
        uint fragOffset = indAddr & 0xFF;

        // If there's an offset, adjust the logical block
        if(fragOffset > 0)
        {
            // The offset is in units of (1 << log2sharesize) sectors
            // This allows small files to share a fragment with a directory
            int shareSize = _discRecord.flags & 0x0F; // log2sharesize is in lower 4 bits of flags
            logicalBlock += fragOffset - 1 << shareSize;
        }

        // Look up the fragment in the map
        return MapLookup(fragId, logicalBlock, out physicalBlock);
    }

    /// <summary>Looks up a fragment ID in the map to find the physical block</summary>
    /// <param name="fragId">Fragment ID to look up</param>
    /// <param name="blockOffset">Block offset within the fragment</param>
    /// <param name="physicalBlock">Output physical block number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber MapLookup(uint fragId, uint blockOffset, out ulong physicalBlock)
    {
        physicalBlock = 0;

        int nzones     = _discRecord.nzones + (_discRecord.nzones_high << 8);
        int idlen      = _discRecord.idlen;
        int sectorSize = 1 << _discRecord.log2secsize;
        int zoneSpare  = _discRecord.zone_spare;

        // Calculate which zone to start searching
        // Root fragment (ID 2) starts in the middle zone
        // Other fragments start in zone (fragId / ids_per_zone)
        int idsPerZone = (sectorSize                             * 8 - zoneSpare) / (idlen + 1);
        int startZone  = fragId == 2 ? nzones / 2 : (int)(fragId / (uint)idsPerZone);

        if(startZone >= nzones) startZone = 0;

        // Calculate bits per zone
        int bitsPerZone = sectorSize * 8 - zoneSpare;

        // Search through zones
        for(var zoneCount = 0; zoneCount < nzones; zoneCount++)
        {
            int zone = (startZone + zoneCount) % nzones;

            // Read the zone
            ErrorNumber errno =
                _imagePlugin.ReadSector(_partition.Start + (ulong)zone, false, out byte[] zoneData, out _);

            if(errno != ErrorNumber.NoError) continue;

            // Calculate zone bit address (for physical block calculation)
            long zoneBitAddr = zone == 0 ? 0 : (long)(bitsPerZone * zone - DISC_RECORD_SIZE * 8);

            // Search for the fragment in this zone
            int startBit = zone == 0 ? 32 + DISC_RECORD_SIZE * 8 : 32;
            int endBit   = Math.Min(bitsPerZone, zoneData.Length * 8);

            int position = startBit;

            while(position < endBit)
            {
                // Read fragment ID at current position
                int frag = GetBits(zoneData, position, idlen);

                // Find the end of this fragment (next '1' bit)
                int fragEnd = FindNextSetBit(zoneData, position + idlen, endBit);

                if(fragEnd < 0 || fragEnd >= endBit) break;

                int fragLength = fragEnd + 1 - position;

                // Check if this is the fragment we're looking for
                if(frag == fragId)
                {
                    // Calculate how many blocks this fragment spans
                    int blocksInFrag = fragLength << _discRecord.log2bpmb >> _discRecord.log2secsize;

                    if(blockOffset < blocksInFrag)
                    {
                        // Found the block
                        long bitAddr = zoneBitAddr + position;

                        physicalBlock = (ulong)((bitAddr << _discRecord.log2bpmb >> _discRecord.log2secsize) +
                                                blockOffset);

                        return ErrorNumber.NoError;
                    }

                    // Block is in a later extent of this fragment
                    blockOffset -= (uint)blocksInFrag;
                }

                position = fragEnd + 1;
            }
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Gets a directory entry by path</summary>
    /// <param name="path">Path to the entry</param>
    /// <param name="entry">Output directory entry</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetEntry(string path, out DirectoryEntryInfo entry)
    {
        entry = null;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory case - return a dummy entry for root
        if(normalizedPath == "/")
        {
            entry = new DirectoryEntryInfo
            {
                Name       = "/",
                LoadAddr   = 0,
                ExecAddr   = 0,
                Length     = 0,
                IndAddr    = _rootDirectoryAddress,
                Attributes = 0x08 // Directory attribute
            };

            return ErrorNumber.NoError;
        }

        // Parse the path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo foundEntry)) return ErrorNumber.NoSuchFile;

            // If this is the last component, return this entry
            if(p == pathComponents.Length - 1)
            {
                entry = foundEntry;

                return ErrorNumber.NoError;
            }

            // Not the last component - check if it's a directory and traverse
            if((foundEntry.Attributes & 0x08) == 0) return ErrorNumber.NotDirectory;

            // Read the subdirectory
            ErrorNumber errno =
                ReadDirectoryContents(foundEntry.IndAddr, out Dictionary<string, DirectoryEntryInfo> subDirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = subDirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts a directory entry to a FileEntryInfo structure</summary>
    /// <param name="entry">Directory entry info</param>
    /// <returns>FileEntryInfo structure</returns>
    FileEntryInfo EntryToFileEntryInfo(DirectoryEntryInfo entry)
    {
        var info = new FileEntryInfo
        {
            Inode     = entry.IndAddr,
            Length    = entry.Length,
            BlockSize = _blockSize,
            Blocks    = (entry.Length + _blockSize - 1) / _blockSize,
            Links     = 1
        };

        // Set attributes
        var attrs = (FileAttributes)entry.Attributes;

        info.Attributes = attrs.HasFlag(FileAttributes.Directory)
                              ? CommonTypes.Structs.FileAttributes.Directory
                              : CommonTypes.Structs.FileAttributes.File;

        if(attrs.HasFlag(FileAttributes.Locked)) info.Attributes |= CommonTypes.Structs.FileAttributes.ReadOnly;

        // Check for symbolic link (filetype 0xFC0 = LinkFS)
        if(HasFiletype(entry.LoadAddr) && GetFiletype(entry.LoadAddr) == FILETYPE_LINKFS)
            info.Attributes |= CommonTypes.Structs.FileAttributes.Symlink;

        // Convert RISC OS timestamp to DateTime if the file is stamped
        // RISC OS timestamp: 40-bit centi-second value since 1 Jan 1900
        // When load address bits [31:20] == 0xFFF, it's a stamped file:
        // - load address bits [7:0] = top 8 bits of timestamp
        // - exec address = bottom 32 bits of timestamp
        if(!HasFiletype(entry.LoadAddr)) return info;

        // File is stamped - extract timestamp
        ulong timestamp = (ulong)(entry.LoadAddr & 0xFF) << 32 | entry.ExecAddr;

        // Convert from centi-seconds to ticks (100ns intervals)
        // 1 centi-second = 10,000,000 nanoseconds = 100,000 ticks
        const long ticksPerCentiSecond = 100000;

        // RISC OS epoch: 1 Jan 1900 00:00:00
        var riscOsEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var ticks = (long)(timestamp * ticksPerCentiSecond);

        // Check if the resulting date would be valid
        if(ticks >= 0 && ticks <= DateTime.MaxValue.Ticks - riscOsEpoch.Ticks)
        {
            info.LastWriteTimeUtc = riscOsEpoch.AddTicks(ticks);
            info.AccessTimeUtc    = info.LastWriteTimeUtc;
        }

        return info;
    }
}