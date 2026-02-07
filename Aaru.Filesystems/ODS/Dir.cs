// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Directory operations for the Files-11 On-Disk Structure.
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
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Filesystems;

public sealed partial class ODS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Root directory case
        if(normalizedPath == "/")
        {
            if(_rootDirectoryCache == null) return ErrorNumber.InvalidArgument;

            node = new OdsDirNode
            {
                Path     = "/",
                Position = 0,
                Entries = _rootDirectoryCache.Where(static kvp => !kvp.Key.Contains(';'))
                                             .OrderBy(static k => k.Key)
                                             .Select(static kvp => (kvp.Key, kvp.Value))
                                             .ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Parse path components
        string cutPath = normalizedPath[1..]; // Remove leading '/'

        string[] pieces = cutPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        if(pieces.Length == 0) return ErrorNumber.NoSuchFile;

        // Start from root directory
        Dictionary<string, CachedFile> currentDirectory = _rootDirectoryCache;

        // Traverse through path components
        for(var p = 0; p < pieces.Length; p++)
        {
            string component = pieces[p].ToUpperInvariant();

            // ODS filenames may include version - strip it for lookup
            int versionPos = component.IndexOf(';');

            if(versionPos >= 0) component = component[..versionPos];

            // Look for the component in current directory
            if(!currentDirectory.TryGetValue(component, out CachedFile cachedFile)) return ErrorNumber.NoSuchFile;

            // Read file header to check if it's a directory
            ErrorNumber errno = ReadFileHeader(cachedFile.Fid.num, out FileHeader fileHeader);

            if(errno != ErrorNumber.NoError) return errno;

            if(!fileHeader.filechar.HasFlag(FileCharacteristicFlags.Directory)) return ErrorNumber.NotDirectory;

            // Read directory entries
            errno = ReadDirectoryEntries(fileHeader, out Dictionary<string, CachedFile> dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            // If this is the last component, we're opening this directory
            if(p == pieces.Length - 1)
            {
                node = new OdsDirNode
                {
                    Path     = normalizedPath,
                    Position = 0,
                    Entries = dirEntries.Where(static kvp => !kvp.Key.Contains(';'))
                                        .OrderBy(static k => k.Key)
                                        .Select(static kvp => (kvp.Key, kvp.Value))
                                        .ToArray()
                };

                return ErrorNumber.NoError;
            }

            // Not the last component - move to next level
            currentDirectory = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not OdsDirNode myNode) return ErrorNumber.InvalidArgument;

        myNode.Position = -1;
        myNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not OdsDirNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(myNode.Position >= myNode.Entries.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = myNode.Entries[myNode.Position++].Filename;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory entries from a directory file header.</summary>
    /// <param name="dirHeader">File header of the directory.</param>
    /// <param name="entries">Output dictionary of cached entries.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadDirectoryEntries(in FileHeader dirHeader, out Dictionary<string, CachedFile> entries)
    {
        entries = new Dictionary<string, CachedFile>();

        // Get mapping information
        byte[] mapData = GetMapData(dirHeader);

        if(mapData == null || mapData.Length == 0) return ErrorNumber.NoError; // Empty directory

        // Calculate file size from FAT
        long fileSize = ((long)dirHeader.recattr.efblk.Value - 1) * ODS_BLOCK_SIZE + dirHeader.recattr.ffbyte;

        if(fileSize <= 0) return ErrorNumber.NoError; // Empty directory

        // Read directory contents VBN by VBN
        var vbn = 1;

        while((vbn - 1) * ODS_BLOCK_SIZE < fileSize)
        {
            ErrorNumber errno = MapVbnToLbn(mapData, dirHeader.map_inuse, (uint)vbn, out uint lbn, out _);

            if(errno != ErrorNumber.NoError) break;

            errno = ReadOdsBlock(_image, _partition, lbn, out byte[] dirBlock);

            if(errno != ErrorNumber.NoError) break;

            // Parse directory entries in this block
            ParseDirectoryBlockToCache(dirBlock, entries);

            vbn++;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses directory entries from a directory block into a cache dictionary.</summary>
    /// <param name="block">Directory block data.</param>
    /// <param name="cache">Cache dictionary to populate.</param>
    void ParseDirectoryBlockToCache(byte[] block, Dictionary<string, CachedFile> cache)
    {
        var offset = 0;

        while(offset < block.Length - 2)
        {
            // Check for end of records marker
            var size = BitConverter.ToUInt16(block, offset);

            if(size is NO_MORE_RECORDS or 0) break;

            // Ensure we have enough data for the record header
            if(offset + 6 > block.Length) break;

            byte flags     = block[offset + 4];
            byte namecount = block[offset + 5];

            // Extract name type from flags
            var nameType = (DirectoryNameType)(flags >> 3 & 0x07);

            // Read filename
            int nameOffset = offset + 6;

            if(nameOffset + namecount > block.Length) break;

            string filename = nameType == DirectoryNameType.Ucs2
                                  ? Encoding.Unicode.GetString(block, nameOffset, namecount)
                                  : _encoding.GetString(block, nameOffset, namecount);

            // Value field (directory entries) starts after name, word-aligned
            int valueOffset = nameOffset + (namecount + 1 & ~1);

            // Read directory entry (first version)
            if(valueOffset + 8 <= block.Length)
            {
                var entryVersion = BitConverter.ToUInt16(block, valueOffset);

                FileId fid = Marshal.ByteArrayToStructureLittleEndian<FileId>(block, valueOffset + 2, 6);

                // Store without version for directory listing
                if(!cache.ContainsKey(filename.ToUpperInvariant()))
                {
                    cache[filename.ToUpperInvariant()] = new CachedFile
                    {
                        Fid     = fid,
                        Version = entryVersion
                    };
                }

                // Store with version too
                var fullName = $"{filename};{entryVersion}";

                cache[fullName.ToUpperInvariant()] = new CachedFile
                {
                    Fid     = fid,
                    Version = entryVersion
                };
            }

            // Move to next record
            offset += size + 2; // size doesn't include the size field itself
        }
    }

    /// <summary>Parses directory entries from a directory block.</summary>
    /// <param name="block">Directory block data.</param>
    void ParseDirectoryBlock(byte[] block)
    {
        var offset = 0;

        while(offset < block.Length - 2)
        {
            // Check for end of records marker
            var size = BitConverter.ToUInt16(block, offset);

            if(size is NO_MORE_RECORDS or 0) break;

            // Ensure we have enough data for the record header
            if(offset + 6 > block.Length) break;

            byte flags     = block[offset + 4];
            byte namecount = block[offset + 5];

            // Extract name type from flags
            var nameType = (DirectoryNameType)(flags >> 3 & 0x07);

            // Read filename
            int nameOffset = offset + 6;

            if(nameOffset + namecount > block.Length) break;

            string filename = nameType == DirectoryNameType.Ucs2
                                  ? Encoding.Unicode.GetString(block, nameOffset, namecount)
                                  : _encoding.GetString(block, nameOffset, namecount);

            // Value field (directory entries) starts after name, word-aligned
            int valueOffset = nameOffset + (namecount + 1 & ~1);

            // Read directory entry (first version)
            if(valueOffset + 8 <= block.Length)
            {
                var entryVersion = BitConverter.ToUInt16(block, valueOffset);

                FileId fid = Marshal.ByteArrayToStructureLittleEndian<FileId>(block, valueOffset + 2, 6);

                // Create filename with version (ODS style: FILENAME.EXT;VERSION)
                var fullName = $"{filename};{entryVersion}";

                // Also store without version for easier lookup
                if(!_rootDirectoryCache.ContainsKey(filename.ToUpperInvariant()))
                {
                    _rootDirectoryCache[filename.ToUpperInvariant()] = new CachedFile
                    {
                        Fid     = fid,
                        Version = entryVersion
                    };
                }

                // Store with version too
                _rootDirectoryCache[fullName.ToUpperInvariant()] = new CachedFile
                {
                    Fid     = fid,
                    Version = entryVersion
                };
            }

            // Move to next record
            offset += size + 2; // size doesn't include the size field itself
        }
    }
}