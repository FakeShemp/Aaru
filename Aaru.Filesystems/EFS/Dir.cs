// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
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
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class EFS
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
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new EfsDirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory traversal
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "OpenDir: Traversing path with {0} components", pathComponents.Length);

        // Start from root directory cache
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            AaruLogging.Debug(MODULE_NAME, "OpenDir: Navigating to component '{0}'", component);

            // Skip "." and ".." during traversal
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out uint inodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Component '{0}' not found in directory", component);

                return ErrorNumber.NoSuchFile;
            }

            AaruLogging.Debug(MODULE_NAME, "OpenDir: Component '{0}' found with inode {1}", component, inodeNumber);

            // Read the inode
            ErrorNumber errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading inode {0}: {1}", inodeNumber, errno);

                return errno;
            }

            // Check if it's a directory
            var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

            if(fileType != FileType.IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: '{0}' is not a directory (type=0x{1:X4})",
                                  component,
                                  (ushort)fileType);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents
            errno = ReadDirectoryContents(inode, out Dictionary<string, uint> dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading directory contents: {0}", errno);

                return errno;
            }

            // Filter out "." and ".." entries
            var filteredEntries = new Dictionary<string, uint>();

            foreach(KeyValuePair<string, uint> entry in dirEntries)
            {
                if(entry.Key is not ("." or "..")) filteredEntries[entry.Key] = entry.Value;
            }

            // If this is the last component, we're opening this directory
            if(p == pathComponents.Length - 1)
            {
                node = new EfsDirNode
                {
                    Path     = normalizedPath,
                    Position = 0,
                    Entries  = filteredEntries.Keys.ToArray()
                };

                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: Successfully opened directory '{0}' with {1} entries",
                                  normalizedPath,
                                  filteredEntries.Count);

                return ErrorNumber.NoError;
            }

            // Not the last component - move to next level
            currentEntries = filteredEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not EfsDirNode efsNode) return ErrorNumber.InvalidArgument;

        efsNode.Position = -1;
        efsNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not EfsDirNode efsNode) return ErrorNumber.InvalidArgument;

        if(efsNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(efsNode.Position >= efsNode.Entries.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = efsNode.Entries[efsNode.Position++];

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