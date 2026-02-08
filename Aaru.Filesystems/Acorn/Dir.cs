// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory case
        if(normalizedPath == "/")
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new AcornDirNode
            {
                Path       = "/",
                Position   = 0,
                Entries    = _rootDirectoryCache,
                EntryNames = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory traversal
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
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo entry)) return ErrorNumber.NoSuchFile;

            // Check if it's a directory (attribute bit 3 = directory)
            if((entry.Attributes & 0x08) == 0) return ErrorNumber.NotDirectory;

            // Read the subdirectory
            ErrorNumber errno =
                ReadDirectoryContents(entry.IndAddr, out Dictionary<string, DirectoryEntryInfo> subDirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            // If this is the last component, we're opening this directory
            if(p == pathComponents.Length - 1)
            {
                node = new AcornDirNode
                {
                    Path       = normalizedPath,
                    Position   = 0,
                    Entries    = subDirEntries,
                    EntryNames = subDirEntries.Keys.ToArray()
                };

                return ErrorNumber.NoError;
            }

            // Not the last component - move to next level
            currentEntries = subDirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not AcornDirNode acornNode) return ErrorNumber.InvalidArgument;

        acornNode.Position   = -1;
        acornNode.Entries    = null;
        acornNode.EntryNames = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not AcornDirNode acornNode) return ErrorNumber.InvalidArgument;

        if(acornNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(acornNode.Position >= acornNode.EntryNames.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = acornNode.EntryNames[acornNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and parses directory contents from the specified indirect disc address</summary>
    /// <param name="indAddr">Indirect disc address of the directory</param>
    /// <param name="entries">Output dictionary of directory entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(uint indAddr, out Dictionary<string, DirectoryEntryInfo> entries)
    {
        // Check cache first
        if(_directoryCache.TryGetValue(indAddr, out entries)) return ErrorNumber.NoError;

        entries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.OrdinalIgnoreCase);

        // Determine directory size based on format
        uint dirSize = _isBigDirectory
                           ? 0
                           : _isOldMap
                               ? OLD_DIRECTORY_SIZE
                               : NEW_DIRECTORY_SIZE;

        // For big directories, we need to read the header first to get the size
        if(_isBigDirectory)
        {
            // Read just the header first (28 bytes minimum)
            ErrorNumber errno = ReadDirectoryData(indAddr, 28, out byte[] headerData);

            if(errno != ErrorNumber.NoError) return errno;

            BigDirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<BigDirectoryHeader>(headerData);

            if(header.bigDirStartName != BIG_DIR_START_NAME) return ErrorNumber.InvalidArgument;

            dirSize = header.bigDirSize;
        }

        ErrorNumber err = ReadDirectoryData(indAddr, dirSize, out byte[] dirData);

        if(err != ErrorNumber.NoError) return err;

        if(_isBigDirectory)
            err = ParseBigDirectoryToDict(dirData, entries);
        else
            err = ParseStandardDirectoryToDict(dirData, entries);

        if(err != ErrorNumber.NoError) return err;

        // Cache the result (limit cache size to avoid memory issues)
        if(_directoryCache.Count < 1000) _directoryCache[indAddr] = entries;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory data from the specified indirect disc address</summary>
    /// <param name="indAddr">Indirect disc address</param>
    /// <param name="size">Size of directory in bytes</param>
    /// <param name="data">Output directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryData(uint indAddr, uint size, out byte[] data)
    {
        data = null;

        if(size == 0) size = _isBigDirectory ? 0 : NEW_DIRECTORY_SIZE;

        // For old formats, indAddr from directory entries is the disc address / 256
        // (i.e., sector number in 256-byte sectors), so multiply by 256 to get byte offset.
        // However, for the root directory, we store the absolute byte offset directly.
        // For new formats, we need to use the map to look up the fragment
        ulong byteOffset;

        if(_isOldMap)
        {
            // Old format: indAddr is the disc address divided by 256
            // For root directory (0x200 or 0x400), this is the absolute byte offset we stored
            // For subdirectories, the 3-byte address field is sector_number * 256 / 256 = sector_number
            // So we need to multiply by 256 to get the byte offset
            // However, since root directory addresses are stored as absolute byte offsets,
            // we can detect them by checking if they're at the known fixed locations
            if(indAddr == OLD_DIRECTORY_LOCATION || indAddr == NEW_DIRECTORY_LOCATION)
                byteOffset = indAddr; // Root directory - already a byte offset
            else
                byteOffset = indAddr * 256UL; // Subdirectory - multiply by sector size

            ulong sector       = byteOffset / _imageSectorSize + _partition.Start;
            var   offsetInSect = (int)(byteOffset % _imageSectorSize);

            var sectorsToRead = (uint)((offsetInSect + size + _imageSectorSize - 1) / _imageSectorSize);

            if(sector + sectorsToRead > _partition.End) return ErrorNumber.InvalidArgument;

            ErrorNumber errno = _imagePlugin.ReadSectors(sector, false, sectorsToRead, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            data = new byte[size];

            // For old format directories, when physical sector size > 256 bytes,
            // the directory data is interleaved - the tail is at the end of the physical sector
            // The directory structure is:
            // - Header (5 bytes) + Entries (47 * 26 = 1222 bytes) = 1227 bytes at start
            // - Tail (53 bytes) at position (sector_size - 54) within the physical sector
            if(size == OLD_DIRECTORY_SIZE && sectorData.Length > OLD_DIRECTORY_SIZE)
            {
                // Copy the first part (header + entries, 1227 bytes)
                const int tailSize  = 53;
                const int headSize  = (int)OLD_DIRECTORY_SIZE - tailSize;
                int       tailStart = sectorData.Length - tailSize - 1; // -1 for 0-based indexing

                // Copy header and entries
                Array.Copy(sectorData, offsetInSect, data, 0, headSize);

                // Copy tail from end of physical sector
                Array.Copy(sectorData, tailStart, data, headSize, tailSize);
            }
            else if(offsetInSect + size <= sectorData.Length)
                Array.Copy(sectorData, offsetInSect, data, 0, size);
            else
                Array.Copy(sectorData, offsetInSect, data, 0, sectorData.Length - offsetInSect);

            return ErrorNumber.NoError;
        }

        // New format: use the map to look up the fragment
        // indAddr contains fragment ID in upper bits and offset in lower 8 bits
        // We need to read the directory sector by sector using MapBlock
        AaruLogging.Debug(MODULE_NAME,
                          "ReadDirectoryData (new format): indAddr={0}, size={1}, blockSize={2}",
                          indAddr,
                          size,
                          _blockSize);

        data = new byte[size];
        var bytesRead = 0;

        while(bytesRead < size)
        {
            // Calculate logical sector within the directory
            int logicalSector = bytesRead / _blockSize;

            // Map the sector to get physical location
            ErrorNumber errno = MapBlock(indAddr, (uint)logicalSector, out ulong physicalSector);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadDirectoryData: MapBlock failed for logical sector {0}: {1}",
                                  logicalSector,
                                  errno);

                return errno;
            }

            AaruLogging.Debug(MODULE_NAME,
                              "ReadDirectoryData: logicalSector={0} -> physicalSector={1}",
                              logicalSector,
                              physicalSector);

            // Read the sector - physicalSector is the ADFS sector number
            // Account for if image sector size differs from ADFS sector size
            ulong imageSector;
            int   offsetInImageSector;

            if(_imageSectorSize == (uint)_blockSize)
            {
                imageSector         = physicalSector + _partition.Start;
                offsetInImageSector = 0;
            }
            else if(_imageSectorSize > (uint)_blockSize)
            {
                // Image sectors are larger than ADFS sectors
                ulong adfsSecPerImgSec = _imageSectorSize / (uint)_blockSize;
                imageSector         = physicalSector / adfsSecPerImgSec + _partition.Start;
                offsetInImageSector = (int)(physicalSector % adfsSecPerImgSec * (uint)_blockSize);
            }
            else
            {
                // Image sectors are smaller than ADFS sectors
                ulong imgSecPerAdfsSec = (uint)_blockSize / _imageSectorSize;
                imageSector         = physicalSector * imgSecPerAdfsSec + _partition.Start;
                offsetInImageSector = 0;
            }

            errno = _imagePlugin.ReadSector(imageSector, false, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            // Copy data from this sector
            int offsetInSector = bytesRead % _blockSize;
            int bytesToCopy    = Math.Min(_blockSize - offsetInSector, (int)size - bytesRead);

            if(offsetInImageSector + offsetInSector + bytesToCopy > sectorData.Length)
                bytesToCopy = sectorData.Length - offsetInImageSector - offsetInSector;

            if(bytesToCopy <= 0) break;

            Array.Copy(sectorData, offsetInImageSector + offsetInSector, data, bytesRead, bytesToCopy);

            bytesRead += bytesToCopy;
        }


        return ErrorNumber.NoError;
    }

    /// <summary>Parses a standard (F format) directory and caches entries to root cache</summary>
    /// <param name="dirData">Raw directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseStandardDirectory(byte[] dirData) => ParseStandardDirectoryToDict(dirData, _rootDirectoryCache);

    /// <summary>Parses a standard (F format) directory into a dictionary</summary>
    /// <param name="dirData">Raw directory data</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseStandardDirectoryToDict(byte[] dirData, Dictionary<string, DirectoryEntryInfo> entries)
    {
        // Check minimum size - old format is 1280 bytes, new format is 2048 bytes
        if(dirData.Length < OLD_DIRECTORY_SIZE) return ErrorNumber.InvalidArgument;

        // Validate directory magic
        DirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<DirectoryHeader>(dirData);

        if(header.magic != OLD_DIR_MAGIC && header.magic != NEW_DIR_MAGIC) return ErrorNumber.InvalidArgument;

        // Directory entries start at offset 5
        const int entryOffset = 5;
        const int entrySize   = 26;

        // Old format (1280 bytes) has 47 entries, new format (2048 bytes) has 77
        int maxEntries = dirData.Length >= NEW_DIRECTORY_SIZE ? NEW_DIR_MAX_ENTRIES : OLD_DIR_MAX_ENTRIES;

        for(var i = 0; i < maxEntries; i++)
        {
            int offset = entryOffset + i * entrySize;

            if(offset + entrySize > dirData.Length) break;

            // Check if entry is used (first byte of name is non-zero and >= space)
            if(dirData[offset] < 0x20) break;

            var entryBytes = new byte[entrySize];
            Array.Copy(dirData, offset, entryBytes, 0, entrySize);

            DirectoryEntry entry = Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(entryBytes);

            // Extract filename (up to 10 characters, terminated by char < 0x20)
            var nameLen = 0;

            for(var j = 0; j < F_NAME_LEN; j++)
            {
                if(entry.name[j] < 0x20) break;

                nameLen++;
            }

            if(nameLen == 0) break;

            string filename = _encoding.GetString(entry.name, 0, nameLen);

            // Calculate indirect disc address from 3 bytes
            var indAddr = (uint)(entry.address[0] | entry.address[1] << 8 | entry.address[2] << 16);

            var entryInfo = new DirectoryEntryInfo
            {
                Name       = filename,
                LoadAddr   = entry.load,
                ExecAddr   = entry.exec,
                Length     = entry.length,
                IndAddr    = indAddr,
                Attributes = entry.atts
            };

            // Don't add duplicate entries
            if(!entries.ContainsKey(filename)) entries[filename] = entryInfo;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a big (F+ format) directory and caches entries to root cache</summary>
    /// <param name="dirData">Raw directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseBigDirectory(byte[] dirData) => ParseBigDirectoryToDict(dirData, _rootDirectoryCache);

    /// <summary>Parses a big (F+ format) directory into a dictionary</summary>
    /// <param name="dirData">Raw directory data</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseBigDirectoryToDict(byte[] dirData, Dictionary<string, DirectoryEntryInfo> entries)
    {
        // Minimum size: header (28) + tail (8)
        if(dirData.Length < 36) return ErrorNumber.InvalidArgument;

        // Parse big directory header
        BigDirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<BigDirectoryHeader>(dirData);

        // Validate header magic
        if(header.bigDirStartName != BIG_DIR_START_NAME) return ErrorNumber.InvalidArgument;

        // Validate version (must be 0.0.0 according to Linux kernel)
        if(header.bigDirVersion[0] != 0 || header.bigDirVersion[1] != 0 || header.bigDirVersion[2] != 0)
            return ErrorNumber.InvalidArgument;

        // Validate directory size (must be multiple of 2048 and not exceed 4MB)
        uint dirSize = header.bigDirSize;

        if(dirSize == 0 || (dirSize & 2047) != 0 || dirSize > 4 * 1024 * 1024) return ErrorNumber.InvalidArgument;

        // Validate tail magic
        if(dirData.Length >= dirSize)
        {
            BigDirectoryTail tail =
                Marshal.ByteArrayToStructureLittleEndian<BigDirectoryTail>(dirData, (int)(dirSize - 8), 8);

            if(tail.bigDirEndName != BIG_DIR_END_NAME) return ErrorNumber.InvalidArgument;

            // Validate master sequence numbers match
            if(tail.bigDirEndMasSeq != header.startMasSeq) return ErrorNumber.InvalidArgument;
        }

        uint numEntries = header.bigDirEntries;

        // Calculate entries offset: header (28 bytes) + directory name (aligned to 4 bytes)
        uint entriesOff = 28 + (header.bigDirNameLen + 3 & ~3u);

        const int bigEntrySize = 28;

        // Validate entries don't overflow
        if(numEntries > 4 * 1024 * 1024 / bigEntrySize) return ErrorNumber.InvalidArgument;

        // Calculate name heap start (after all entries)
        uint nameHeapStart = entriesOff + numEntries * bigEntrySize;

        for(uint i = 0; i < numEntries; i++)
        {
            uint offset = entriesOff + i * bigEntrySize;

            if(offset + bigEntrySize > dirData.Length) break;

            var entryBytes = new byte[bigEntrySize];
            Array.Copy(dirData, offset, entryBytes, 0, bigEntrySize);

            BigDirectoryEntry entry = Marshal.ByteArrayToStructureLittleEndian<BigDirectoryEntry>(entryBytes);

            // Get filename from name heap
            uint nameOff = header.bigDirSize - 8 - header.bigDirNameSize + entry.bigDirObNamePtr;
            uint nameLen = entry.bigDirObNameLen;

            if(nameOff + nameLen > dirData.Length || nameLen == 0) continue;

            var nameBytes = new byte[nameLen];
            Array.Copy(dirData, nameOff, nameBytes, 0, nameLen);

            string filename = _encoding.GetString(nameBytes).TrimEnd('\0');

            if(string.IsNullOrEmpty(filename)) continue;

            var entryInfo = new DirectoryEntryInfo
            {
                Name       = filename,
                LoadAddr   = entry.bigDirLoad,
                ExecAddr   = entry.bigDirExec,
                Length     = entry.bigDirLen,
                IndAddr    = entry.bigDirIndAddr,
                Attributes = entry.bigDirAttr
            };

            // Don't add duplicate entries
            if(!entries.ContainsKey(filename)) entries[filename] = entryInfo;
        }

        return ErrorNumber.NoError;
    }
}