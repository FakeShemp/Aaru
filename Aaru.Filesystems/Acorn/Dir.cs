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
        entries = new Dictionary<string, DirectoryEntryInfo>();

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

        if(_isBigDirectory) return ParseBigDirectoryToDict(dirData, entries);

        return ParseStandardDirectoryToDict(dirData, entries);
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
        }
        else
        {
            // New format: indirect disc address contains fragment ID and offset
            // For the root directory, the address from disc record is usually
            // directly usable as a sector offset after accounting for the map
            // This is a simplified approach - full implementation would use the map
            byteOffset = indAddr * (ulong)_blockSize;
        }

        ulong sector       = byteOffset / _imagePlugin.Info.SectorSize + _partition.Start;
        var   offsetInSect = (int)(byteOffset % _imagePlugin.Info.SectorSize);

        var sectorsToRead = (uint)((offsetInSect + size + _imagePlugin.Info.SectorSize - 1) /
                                   _imagePlugin.Info.SectorSize);

        if(sector + sectorsToRead > _partition.End) return ErrorNumber.InvalidArgument;

        ErrorNumber errno = _imagePlugin.ReadSectors(sector, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        data = new byte[size];

        // For old format directories, when physical sector size > 256 bytes,
        // the directory data is interleaved - the tail is at the end of the physical sector
        if(_isOldMap && size == OLD_DIRECTORY_SIZE && sectorData.Length > OLD_DIRECTORY_SIZE)
        {
            // Old directory: copy first part (1227 bytes), then tail (53 bytes) from end of sector
            // This matches the logic in Info.cs
            Array.Copy(sectorData, offsetInSect, data, 0, (int)OLD_DIRECTORY_SIZE - 53);
            Array.Copy(sectorData, sectorData.Length - 54, data, (int)OLD_DIRECTORY_SIZE - 54, 53);
        }
        else if(offsetInSect + size <= sectorData.Length)
            Array.Copy(sectorData, offsetInSect, data, 0, size);
        else
            Array.Copy(sectorData, offsetInSect, data, 0, sectorData.Length - offsetInSect);

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
        if(dirData.Length < 28) // Minimum header size
            return ErrorNumber.InvalidArgument;

        // Parse big directory header
        BigDirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<BigDirectoryHeader>(dirData);

        if(header.bigDirStartName != BIG_DIR_START_NAME) return ErrorNumber.InvalidArgument;

        uint numEntries  = header.bigDirEntries;
        uint nameHeapOff = 28 + header.bigDirNameLen; // After header + directory name
        uint entriesOff  = nameHeapOff;

        // Align to 4 bytes
        if(entriesOff % 4 != 0) entriesOff += 4 - entriesOff % 4;

        const int bigEntrySize = 28;

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
                Attributes = (byte)entry.bigDirAttr
            };

            // Don't add duplicate entries
            if(!entries.ContainsKey(filename)) entries[filename] = entryInfo;
        }

        return ErrorNumber.NoError;
    }
}