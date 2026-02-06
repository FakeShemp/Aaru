// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class ProDOSPlugin
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(string.IsNullOrEmpty(normalizedPath) || normalizedPath == ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/")
        {
            // Read root directory contents
            ErrorNumber errno = ReadDirectoryContents(2, true, out Dictionary<string, CachedEntry> entries);

            if(errno != ErrorNumber.NoError) return errno;

            node = new ProDosDirNode
            {
                Path              = normalizedPath,
                Contents          = entries.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray(),
                Position          = 0,
                DirectoryKeyBlock = 2
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory - traverse path to find it
        string[] pathComponents = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Start from root directory cache
        Dictionary<string, CachedEntry> currentDir  = _rootDirectoryCache;
        CachedEntry                     targetEntry = null;

        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            if(component is "." or "..") continue;

            if(!currentDir.TryGetValue(component, out CachedEntry entry)) return ErrorNumber.NoSuchFile;

            // Last component must be a directory
            if(i == pathComponents.Length - 1)
            {
                if(!entry.IsDirectory) return ErrorNumber.NotDirectory;

                targetEntry = entry;

                break;
            }

            // Intermediate components must be directories
            if(!entry.IsDirectory) return ErrorNumber.NotDirectory;

            // Read this subdirectory's contents
            ErrorNumber errno =
                ReadDirectoryContents(entry.KeyBlock, false, out Dictionary<string, CachedEntry> subDir);

            if(errno != ErrorNumber.NoError) return errno;

            currentDir = subDir;
        }

        if(targetEntry == null) return ErrorNumber.NoSuchFile;

        // Read the target directory contents
        ErrorNumber err =
            ReadDirectoryContents(targetEntry.KeyBlock, false, out Dictionary<string, CachedEntry> dirContents);

        if(err != ErrorNumber.NoError) return err;

        node = new ProDosDirNode
        {
            Path              = normalizedPath,
            Contents          = dirContents.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray(),
            Position          = 0,
            DirectoryKeyBlock = targetEntry.KeyBlock
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not ProDosDirNode proDosNode) return ErrorNumber.InvalidArgument;

        proDosNode.Contents = null;
        proDosNode.Position = -1;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(node is not ProDosDirNode proDosNode) return ErrorNumber.InvalidArgument;

        if(proDosNode.Position < 0 || proDosNode.Contents == null) return ErrorNumber.InvalidArgument;

        if(proDosNode.Position >= proDosNode.Contents.Length) return ErrorNumber.NoError;

        filename = proDosNode.Contents[proDosNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory contents from a key block</summary>
    /// <param name="keyBlock">Key block of the directory</param>
    /// <param name="isVolumeDirectory">True if this is the volume directory (block 2)</param>
    /// <param name="entries">Output dictionary of cached entries</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadDirectoryContents(ushort                              keyBlock, bool isVolumeDirectory,
                                      out Dictionary<string, CachedEntry> entries)
    {
        entries = new Dictionary<string, CachedEntry>(StringComparer.OrdinalIgnoreCase);

        ushort currentBlock = keyBlock;
        ushort prevBlock    = 0;
        ushort entryCount   = 0;
        var    entriesRead  = 0;

        // Entry index within block: 0 = header, 1-12 = entries (13 total slots per block)
        // In first block, slot 0 is the directory/volume header
        // In subsequent blocks, slot 0 is a regular entry
        var currentEntry = 1; // Start at entry 1 (skip header in first block)

        while(currentBlock != 0)
        {
            ErrorNumber errno = ReadBlock(currentBlock, out byte[] block);

            if(errno != ErrorNumber.NoError)
                return errno;

            // Read block header
            DirectoryBlockHeader blockHeader = Marshal.ByteArrayToStructureLittleEndian<DirectoryBlockHeader>(block);

            // Validate prev pointer matches (like Linux driver does)
            if(blockHeader.prev != prevBlock)
                return ErrorNumber.InvalidArgument;

            // First block has directory header at entry slot 0, read it to get entry count
            if(currentBlock == keyBlock)
            {
                if(isVolumeDirectory)
                {
                    VolumeDirectoryHeader volHeader =
                        Marshal.ByteArrayToStructureLittleEndian<VolumeDirectoryHeader>(block,
                            4,
                            System.Runtime.InteropServices.Marshal.SizeOf<VolumeDirectoryHeader>());

                    entryCount = volHeader.entry_count;
                }
                else
                {
                    DirectoryHeader dirHeader =
                        Marshal.ByteArrayToStructureLittleEndian<DirectoryHeader>(block,
                            4,
                            System.Runtime.InteropServices.Marshal.SizeOf<DirectoryHeader>());

                    entryCount = dirHeader.entry_count;
                }
            }

            // Parse entries in this block
            // Each block has 13 entry slots (4 byte header + 13 * 39 byte entries = 511 bytes)
            while(currentEntry < ENTRIES_PER_BLOCK)
            {
                // Calculate entry offset: 4 bytes header + entry index * 39 bytes
                int entryOffset = 4 + currentEntry * ENTRY_LENGTH;

                // Check if entry is active
                byte storageTypeNameLength = block[entryOffset];
                var  storageType           = (byte)((storageTypeNameLength & STORAGE_TYPE_MASK) >> 4);

                // Skip deleted entries (storage type 0) and header entries
                if(storageType != EMPTY_STORAGE_TYPE       &&
                   storageType != SUBDIRECTORY_HEADER_TYPE &&
                   storageType != ROOT_DIRECTORY_TYPE)
                {
                    // Parse the entry
                    CachedEntry entry = ParseDirectoryEntry(block, entryOffset);

                    if(entry != null && !string.IsNullOrEmpty(entry.Name))
                    {
                        entries[entry.Name] = entry;
                        entriesRead++;
                    }
                }

                currentEntry++;

                // Stop if we've read all entries
                if(entriesRead >= entryCount)
                    break;
            }

            // Stop if we've read all entries
            if(entriesRead >= entryCount)
                break;

            // Move to next block
            prevBlock    = currentBlock;
            currentBlock = blockHeader.next;
            currentEntry = 0; // In subsequent blocks, start at entry 0
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a directory entry from a block using marshalling</summary>
    CachedEntry ParseDirectoryEntry(byte[] block, int offset)
    {
        // Marshal the entry structure
        Entry entry =
            Marshal.ByteArrayToStructureLittleEndian<Entry>(block,
                                                            offset,
                                                            System.Runtime.InteropServices.Marshal.SizeOf<Entry>());

        var storageType = (byte)((entry.storage_type_name_length & STORAGE_TYPE_MASK) >> 4);
        var nameLength  = (byte)(entry.storage_type_name_length & NAME_LENGTH_MASK);

        if(nameLength == 0) return null;

        // Extract filename
        string fileName = _encoding.GetString(entry.file_name, 0, nameLength);

        // Apply case bits if present (GS/OS extension)
        if((entry.case_bits & 0x8000) != 0) fileName = ApplyCaseBits(fileName, entry.case_bits);

        // Parse EOF (3 bytes little-endian)
        var eof = (uint)(entry.eof[0] | entry.eof[1] << 8 | entry.eof[2] << 16);

        return new CachedEntry
        {
            Name             = fileName,
            StorageType      = storageType,
            FileType         = entry.file_type,
            KeyBlock         = entry.key_pointer,
            BlocksUsed       = entry.blocks_used,
            Eof              = eof,
            CreationTime     = DateHandlers.ProDosToDateTime(entry.creation_date,     entry.creation_time),
            ModificationTime = DateHandlers.ProDosToDateTime(entry.modification_date, entry.modification_time),
            Access           = entry.access,
            AuxType          = entry.aux_type,
            HeaderPointer    = entry.header_pointer,
            CaseBits         = entry.case_bits
        };
    }
}