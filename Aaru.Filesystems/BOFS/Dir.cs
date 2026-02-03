// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS old filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class BOFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(string.IsNullOrEmpty(path) || path == "/")
        {
            var dirNode = new BOFSDirNode
            {
                Path                      = "/",
                FirstDirectoryBlockSector = _track0.FirstDirectorySector,
                DirectoryBlocks           = new List<DirectoryBlock>(),
                CurrentEntryIndex         = 0,
                CurrentBlockIndex         = 0,
                Disposed                  = false
            };

            node = dirNode;

            AaruLogging.Debug(MODULE_NAME,
                              "OpenDir: root directory, FirstDirectoryBlockSector={0}",
                              _track0.FirstDirectorySector);

            return ErrorNumber.NoError;
        }

        // Use helper to lookup the entry
        ErrorNumber lookupErr = LookupEntry(path, out FileEntry dirEntry);

        if(lookupErr != ErrorNumber.NoError) return ErrorNumber.NoSuchFile;

        // Check if this entry is actually a directory (FileType = -1 for SDIR)
        if(dirEntry.FileType != DIR_TYPE) return ErrorNumber.NotDirectory;

        AaruLogging.Debug(MODULE_NAME,
                          "OpenDir: path={0}, FirstAllocList={1}, FileType=0x{2:X8}, LogicalSize={3}",
                          path,
                          dirEntry.FirstAllocList,
                          dirEntry.FileType,
                          dirEntry.LogicalSize);

        // Check if directory is empty
        if(dirEntry.FirstAllocList == 0)
            AaruLogging.Debug(MODULE_NAME, "OpenDir: directory {0} is EMPTY (FirstAllocList=0)", path);

        var resultNode = new BOFSDirNode
        {
            Path                      = path,
            FirstDirectoryBlockSector = dirEntry.FirstAllocList,
            DirectoryBlocks           = new List<DirectoryBlock>(),
            CurrentEntryIndex         = 0,
            CurrentBlockIndex         = 0,
            Disposed                  = false
        };

        node = resultNode;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not BOFSDirNode dirNode) return ErrorNumber.InvalidArgument;

        dirNode.Disposed = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(node is not BOFSDirNode dirNode) return ErrorNumber.InvalidArgument;

        if(dirNode.Disposed) return ErrorNumber.InvalidArgument;

        // For root directory, iterate through cached entries
        if(dirNode.Path == "/")
        {
            lock(_rootDirectoryCache)
            {
                var entries = new List<string>(_rootDirectoryCache.Keys);

                if(dirNode.CurrentEntryIndex >= entries.Count) return ErrorNumber.NoError;

                filename = entries[dirNode.CurrentEntryIndex];
                dirNode.CurrentEntryIndex++;

                return ErrorNumber.NoError;
            }
        }

        // For subdirectories, load directory blocks on demand and enumerate entries
        const int bofsLogicalSectorSize = 512;
        const int directoryBlockSize    = 8192;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadDir: subdirectory={0}, FirstDirectoryBlockSector={1}",
                          dirNode.Path,
                          dirNode.FirstDirectoryBlockSector);

        while(true)
        {
            // Load next directory block if needed
            if(dirNode.CurrentBlockIndex >= dirNode.DirectoryBlocks.Count)
            {
                int sectorToRead = dirNode.CurrentBlockIndex == 0
                                       ? dirNode.FirstDirectoryBlockSector
                                       : dirNode.DirectoryBlocks[dirNode.CurrentBlockIndex - 1].Header
                                                .NextDirectoryBlock;

                AaruLogging.Debug(MODULE_NAME,
                                  "ReadDir: loading block index {0}, sector {1}",
                                  dirNode.CurrentBlockIndex,
                                  sectorToRead);

                if(sectorToRead == 0)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadDir: end of directory blocks, loaded {0} total blocks",
                                      dirNode.DirectoryBlocks.Count);

                    return ErrorNumber.NoError; // End of directory blocks
                }

                // Load the directory block (same logic as Mount.cs)
                ulong byteOffset                      = (ulong)sectorToRead * bofsLogicalSectorSize;
                ulong deviceSectorOffsetFromPartition = byteOffset          / _imagePlugin.Info.SectorSize;
                ulong offsetInDeviceSector            = byteOffset          % _imagePlugin.Info.SectorSize;
                ulong absoluteDeviceSector            = _partition.Start + deviceSectorOffsetFromPartition;

                ulong bytesNeeded = offsetInDeviceSector + directoryBlockSize;

                var sectorsToRead =
                    (uint)((bytesNeeded + _imagePlugin.Info.SectorSize - 1) / _imagePlugin.Info.SectorSize);

                ErrorNumber errno =
                    _imagePlugin.ReadSectors(absoluteDeviceSector, false, sectorsToRead, out byte[] sectorData, out _);

                if(errno != ErrorNumber.NoError) return errno;

                if(sectorData.Length < (int)(offsetInDeviceSector + directoryBlockSize))
                    return ErrorNumber.InvalidArgument;

                var dirBlockBuffer = new byte[directoryBlockSize];
                Array.Copy(sectorData, (int)offsetInDeviceSector, dirBlockBuffer, 0, directoryBlockSize);

                DirectoryBlock dirBlock = Marshal.ByteArrayToStructureBigEndian<DirectoryBlock>(dirBlockBuffer);
                dirNode.DirectoryBlocks.Add(dirBlock);

                AaruLogging.Debug(MODULE_NAME,
                                  "ReadDir: loaded block {0} from sector {1}, NextBlock={2}",
                                  dirNode.CurrentBlockIndex,
                                  sectorToRead,
                                  dirBlock.Header.NextDirectoryBlock);
            }

            // Enumerate entries in current block
            DirectoryBlock currentBlock = dirNode.DirectoryBlocks[dirNode.CurrentBlockIndex];

            // Find next non-empty entry
            while(dirNode.CurrentEntryIndex < 63)
            {
                FileEntry entry = currentBlock.Entries[dirNode.CurrentEntryIndex];

                dirNode.CurrentEntryIndex++;

                // Skip empty entries
                if(entry.FileName == null || entry.FileName.Length == 0 || entry.FileName[0] == 0) continue;

                filename = StringHandlers.CToString(entry.FileName, _encoding);

                if(!string.IsNullOrWhiteSpace(filename))
                {
                    // Debug: show first entry in detail
                    if(dirNode.CurrentBlockIndex == 0 && dirNode.CurrentEntryIndex == 1)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "First entry hex dump - FileName: {0}, FirstAllocList=0x{1:X8}, LastAllocList=0x{2:X8}, FileType=0x{3:X8}, CreationDate=0x{4:X8}, ModDate=0x{5:X8}, LogicalSize=0x{6:X8}",
                                          filename,
                                          entry.FirstAllocList,
                                          entry.LastAllocList,
                                          entry.FileType,
                                          entry.CreationDate,
                                          entry.ModificationDate,
                                          entry.LogicalSize);
                    }

                    // Cache the entry for Stat lookups
                    dirNode.EntriesCache[filename] = entry;

                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadDir: found entry {0} at block {1}, index {2}, FileType=0x{3:X8}, LogicalSize={4}",
                                      filename,
                                      dirNode.CurrentBlockIndex,
                                      dirNode.CurrentEntryIndex - 1,
                                      entry.FileType,
                                      entry.LogicalSize);

                    return ErrorNumber.NoError;
                }
            }

            // Move to next block
            dirNode.CurrentEntryIndex = 0;
            dirNode.CurrentBlockIndex++;
        }
    }

    /// <summary>Helper method to parse a path and lookup an entry in either root or subdirectory cache</summary>
    /// <param name="path">Full path to the file</param>
    /// <param name="entry">The FileEntry if found</param>
    /// <returns>Error code</returns>
    private ErrorNumber LookupEntry(string path, out FileEntry entry)
    {
        entry = default(FileEntry);

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.NoSuchFile;

        // Parse path to get directory and filename
        string trimmedPath = path.TrimStart('/');
        int    lastSlash   = trimmedPath.LastIndexOf('/');

        string dirPath;
        string fileName;

        if(lastSlash < 0)
        {
            // File in root directory
            dirPath  = "/";
            fileName = trimmedPath;
        }
        else
        {
            // File in subdirectory
            dirPath  = "/" + trimmedPath.Substring(0, lastSlash);
            fileName = trimmedPath.Substring(lastSlash + 1);
        }

        // Look up the entry
        lock(_rootDirectoryCache)
        {
            if(dirPath == "/" && _rootDirectoryCache.TryGetValue(fileName, out FileEntry rootEntry))
            {
                entry = rootEntry;

                return ErrorNumber.NoError;
            }
        }

        // For subdirectory files, open parent and search
        if(dirPath != "/")
        {
            ErrorNumber openErr = OpenDir(dirPath, out IDirNode dirNode);

            if(openErr != ErrorNumber.NoError) return ErrorNumber.NoSuchFile;

            if(dirNode is not BOFSDirNode bofsDir)
            {
                CloseDir(dirNode);

                return ErrorNumber.InvalidArgument;
            }

            try
            {
                // Read all entries to populate cache
                while(ReadDir(dirNode, out string entryName) == ErrorNumber.NoError && entryName != null)
                {
                    // Cache is being populated
                }

                bool found = bofsDir.EntriesCache.TryGetValue(fileName, out FileEntry subEntry);
                CloseDir(dirNode);

                if(found)
                {
                    entry = subEntry;

                    return ErrorNumber.NoError;
                }

                return ErrorNumber.NoSuchFile;
            }
            catch
            {
                CloseDir(dirNode);

                return ErrorNumber.InvalidArgument;
            }
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    private sealed class BOFSDirNode : IDirNode
    {
        /// <summary>First directory block sector</summary>
        public int FirstDirectoryBlockSector { get; set; }

        /// <summary>List of directory blocks for this directory</summary>
        public List<DirectoryBlock> DirectoryBlocks { get; set; }

        /// <summary>Index of current entry in directory blocks</summary>
        public int CurrentEntryIndex { get; set; }

        /// <summary>Index of current block</summary>
        public int CurrentBlockIndex { get; set; }

        /// <summary>Enumeration state</summary>
        public bool Disposed { get; set; }

        /// <summary>Cache of entries found during enumeration for Stat lookups</summary>
        public Dictionary<string, FileEntry> EntriesCache { get; } = new();

        /// <inheritdoc />
        public string Path { get; set; }
    }
}