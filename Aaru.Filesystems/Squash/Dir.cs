// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
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
using System.Linq;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
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

            node = new SquashDirNode
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

        AaruLogging.Debug(MODULE_NAME, "OpenDir: Traversing path with {0} components", pathComponents.Length);

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            AaruLogging.Debug(MODULE_NAME, "OpenDir: Navigating to component '{0}'", component);

            // Skip "." and ".." during traversal
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo entry))
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Component '{0}' not found in directory", component);

                return ErrorNumber.NoSuchFile;
            }

            AaruLogging.Debug(MODULE_NAME,
                              "OpenDir: Component '{0}' found with inode {1}",
                              component,
                              entry.InodeNumber);

            // Check if it's a directory
            if(entry.Type != SquashInodeType.Directory && entry.Type != SquashInodeType.ExtendedDirectory)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: '{0}' is not a directory (type={1})", component, entry.Type);

                return ErrorNumber.NotDirectory;
            }

            // Read directory inode to get directory parameters
            ErrorNumber errno = ReadDirectoryInode(entry.InodeBlock,
                                                   entry.InodeOffset,
                                                   out uint dirStartBlock,
                                                   out uint dirOffset,
                                                   out uint dirSize);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading directory inode: {0}", errno);

                return errno;
            }

            // Read directory contents
            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            errno = ReadDirectoryContents(dirStartBlock, dirOffset, dirSize, dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading directory contents: {0}", errno);

                return errno;
            }

            // If this is the last component, we're opening this directory
            if(p == pathComponents.Length - 1)
            {
                node = new SquashDirNode
                {
                    Path       = normalizedPath,
                    Position   = 0,
                    Entries    = dirEntries,
                    EntryNames = dirEntries.Keys.ToArray()
                };

                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: Successfully opened directory '{0}' with {1} entries",
                                  normalizedPath,
                                  dirEntries.Count);

                return ErrorNumber.NoError;
            }

            // Not the last component - move to next level
            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not SquashDirNode squashNode) return ErrorNumber.InvalidArgument;

        squashNode.Position   = -1;
        squashNode.Entries    = null;
        squashNode.EntryNames = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not SquashDirNode squashNode) return ErrorNumber.InvalidArgument;

        if(squashNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(squashNode.Position >= squashNode.EntryNames.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = squashNode.EntryNames[squashNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a directory inode and extracts directory parameters</summary>
    /// <param name="inodeBlock">Block containing the inode (relative to inode table)</param>
    /// <param name="inodeOffset">Offset within the metadata block</param>
    /// <param name="startBlock">Output: start block of directory data</param>
    /// <param name="offset">Output: offset within the directory block</param>
    /// <param name="size">Output: size of directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryInode(uint     inodeBlock, ushort inodeOffset, out uint startBlock, out uint offset,
                                   out uint size)
    {
        startBlock = 0;
        offset     = 0;
        size       = 0;

        // Calculate absolute position of the inode
        ulong inodePosition = _superBlock.inode_table_start + inodeBlock;

        // Read the metadata block containing the inode
        ErrorNumber errno = ReadMetadataBlock(inodePosition, out byte[] inodeBlockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode metadata block: {0}", errno);

            return errno;
        }

        if(inodeBlockData == null || inodeBlockData.Length <= inodeOffset)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode block data");

            return ErrorNumber.InvalidArgument;
        }

        // Read the base inode to get the type
        var baseInodeData = new byte[Marshal.SizeOf<BaseInode>()];
        Array.Copy(inodeBlockData, inodeOffset, baseInodeData, 0, baseInodeData.Length);

        BaseInode baseInode = _littleEndian
                                  ? Helpers.Marshal.ByteArrayToStructureLittleEndian<BaseInode>(baseInodeData)
                                  : Helpers.Marshal.ByteArrayToStructureBigEndian<BaseInode>(baseInodeData);

        // Read the directory inode based on type
        if(baseInode.inode_type == (ushort)SquashInodeType.Directory)
        {
            var dirInodeData = new byte[Marshal.SizeOf<DirInode>()];
            Array.Copy(inodeBlockData, inodeOffset, dirInodeData, 0, dirInodeData.Length);

            DirInode dirInode = _littleEndian
                                    ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DirInode>(dirInodeData)
                                    : Helpers.Marshal.ByteArrayToStructureBigEndian<DirInode>(dirInodeData);

            startBlock = dirInode.start_block;
            size       = dirInode.file_size;
            offset     = dirInode.offset;
        }
        else if(baseInode.inode_type == (ushort)SquashInodeType.ExtendedDirectory)
        {
            var extDirInodeData = new byte[Marshal.SizeOf<ExtendedDirInode>()];
            Array.Copy(inodeBlockData, inodeOffset, extDirInodeData, 0, extDirInodeData.Length);

            ExtendedDirInode extDirInode = _littleEndian
                                               ? Helpers.Marshal
                                                        .ByteArrayToStructureLittleEndian<
                                                             ExtendedDirInode>(extDirInodeData)
                                               : Helpers.Marshal
                                                        .ByteArrayToStructureBigEndian<
                                                             ExtendedDirInode>(extDirInodeData);

            startBlock = extDirInode.start_block;
            size       = extDirInode.file_size;
            offset     = extDirInode.offset;
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Inode is not a directory, type: {0}",
                              (SquashInodeType)baseInode.inode_type);

            return ErrorNumber.NotDirectory;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Directory inode: start_block={0}, size={1}, offset={2}",
                          startBlock,
                          size,
                          offset);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and parses directory contents</summary>
    /// <param name="startBlock">Start block of the directory data</param>
    /// <param name="offset">Offset within the block</param>
    /// <param name="size">Total size of the directory data</param>
    /// <param name="cache">Dictionary to cache the entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(uint                                   startBlock, uint offset, uint size,
                                      Dictionary<string, DirectoryEntryInfo> cache)
    {
        if(size <= 3) // Empty directory (size includes "." and ".." overhead)
        {
            AaruLogging.Debug(MODULE_NAME, "Empty directory");

            return ErrorNumber.NoError;
        }

        // Calculate absolute position
        ulong blockPosition = _superBlock.directory_table_start + startBlock;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading directory: block=0x{0:X16}, offset={1}, size={2}",
                          blockPosition,
                          offset,
                          size);

        // Read the metadata block
        ErrorNumber errno = ReadMetadataBlock(blockPosition, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory metadata block: {0}", errno);

            return errno;
        }

        if(dirData == null || dirData.Length <= offset)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid directory data");

            return ErrorNumber.InvalidArgument;
        }

        // Parse directory entries
        uint currentOffset = offset;
        uint bytesRead     = 0;

        // The size field in directory inodes is 3 greater than the real size
        // to account for the "." and ".." entries that are not stored
        uint realSize = size - 3;

        while(bytesRead < realSize && currentOffset < dirData.Length)
        {
            // Read directory header
            if(currentOffset + Marshal.SizeOf<DirHeader>() > dirData.Length) break;

            var headerData = new byte[Marshal.SizeOf<DirHeader>()];
            Array.Copy(dirData, currentOffset, headerData, 0, headerData.Length);

            DirHeader header = _littleEndian
                                   ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DirHeader>(headerData)
                                   : Helpers.Marshal.ByteArrayToStructureBigEndian<DirHeader>(headerData);

            currentOffset += (uint)headerData.Length;
            bytesRead     += (uint)headerData.Length;

            // count is stored as count-1
            uint entryCount = header.count + 1;

            if(entryCount > SQUASHFS_DIR_COUNT)
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid directory entry count: {0}", entryCount);

                break;
            }

            AaruLogging.Debug(MODULE_NAME,
                              "Directory header: count={0}, start_block={1}, inode_number={2}",
                              entryCount,
                              header.start_block,
                              header.inode_number);

            // Read entries for this header
            for(uint i = 0; i < entryCount && bytesRead < realSize; i++)
            {
                if(currentOffset + Marshal.SizeOf<DirEntry>() > dirData.Length) break;

                var entryData = new byte[Marshal.SizeOf<DirEntry>()];
                Array.Copy(dirData, currentOffset, entryData, 0, entryData.Length);

                DirEntry entry = _littleEndian
                                     ? Helpers.Marshal.ByteArrayToStructureLittleEndian<DirEntry>(entryData)
                                     : Helpers.Marshal.ByteArrayToStructureBigEndian<DirEntry>(entryData);

                currentOffset += (uint)entryData.Length;
                bytesRead     += (uint)entryData.Length;

                // size is stored as size-1
                var nameSize = (uint)(entry.size + 1);

                if(nameSize > SQUASHFS_NAME_LEN || currentOffset + nameSize > dirData.Length)
                {
                    AaruLogging.Debug(MODULE_NAME, "Invalid entry name size: {0}", nameSize);

                    break;
                }

                var nameData = new byte[nameSize];
                Array.Copy(dirData, currentOffset, nameData, 0, nameSize);
                string name = _encoding.GetString(nameData).TrimEnd('\0');

                currentOffset += nameSize;
                bytesRead     += nameSize;

                // Calculate the actual inode number
                // The entry's inode_number is a signed offset from the header's inode_number
                var inodeNumber = (uint)((int)header.inode_number + entry.inode_number);

                var entryInfo = new DirectoryEntryInfo
                {
                    Name        = name,
                    InodeNumber = inodeNumber,
                    Type        = (SquashInodeType)entry.type,
                    InodeBlock  = header.start_block,
                    InodeOffset = entry.offset
                };

                if(cache.TryAdd(name, entryInfo))
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Cached entry: {0} -> inode {1}, type {2}",
                                      name,
                                      inodeNumber,
                                      (SquashInodeType)entry.type);
                }
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Directory read complete, {0} entries cached", cache.Count);

        return ErrorNumber.NoError;
    }
}