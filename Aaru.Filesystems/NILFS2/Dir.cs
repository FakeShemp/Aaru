// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
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
        if(normalizedPath is "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new Nilfs2DirNode
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
            if(entry.Type != FileType.Dir)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: '{0}' is not a directory (type={1})", component, entry.Type);

                return ErrorNumber.NotDirectory;
            }

            // Read the child directory inode from the ifile
            ErrorNumber errno = ReadInodeFromIfile(_ifileInode, entry.InodeNumber, out Inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading inode {0}: {1}", entry.InodeNumber, errno);

                return errno;
            }

            // Verify it's actually a directory (S_IFDIR = 0x4000)
            if((childInode.mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: Inode {0} is not a directory (mode=0x{1:X4})",
                                  entry.InodeNumber,
                                  childInode.mode);

                return ErrorNumber.NotDirectory;
            }

            // Read directory entries from child inode
            var childEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            errno = ReadDirectoryEntries(childInode, childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: Error reading directory entries for inode {0}: {1}",
                                  entry.InodeNumber,
                                  errno);

                return errno;
            }

            // If this is the last component, open this directory
            if(p == pathComponents.Length - 1)
            {
                node = new Nilfs2DirNode
                {
                    Path       = normalizedPath,
                    Position   = 0,
                    Entries    = childEntries,
                    EntryNames = childEntries.Keys.ToArray()
                };

                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: Successfully opened directory '{0}' with {1} entries",
                                  normalizedPath,
                                  childEntries.Count);

                return ErrorNumber.NoError;
            }

            // Not the last component — move to next level
            currentEntries = childEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not Nilfs2DirNode nilfs2Node) return ErrorNumber.InvalidArgument;

        nilfs2Node.Position   = -1;
        nilfs2Node.Entries    = null;
        nilfs2Node.EntryNames = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not Nilfs2DirNode nilfs2Node) return ErrorNumber.InvalidArgument;

        if(nilfs2Node.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(nilfs2Node.Position >= nilfs2Node.EntryNames.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = nilfs2Node.EntryNames[nilfs2Node.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads all directory entries from a directory inode into a dictionary</summary>
    /// <param name="dirInode">The directory inode to read</param>
    /// <param name="entries">Dictionary to populate with directory entries keyed by name</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(in Inode dirInode, Dictionary<string, DirectoryEntryInfo> entries)
    {
        ulong dirSize      = dirInode.size;
        ulong bytesRead    = 0;
        ulong logicalBlock = 0;

        while(bytesRead < dirSize)
        {
            // Directory data blocks need DAT translation
            ErrorNumber errno = ReadLogicalBlock(dirInode, logicalBlock, false, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading directory block {0}: {1}", logicalBlock, errno);

                return errno;
            }

            uint offset = 0;

            while(offset < _blockSize && bytesRead < dirSize)
            {
                // Minimum directory entry is 12 bytes (inode(8) + rec_len(2) + name_len(1) + file_type(1))
                if(offset + 12 > blockData.Length) break;

                var  entInode = BitConverter.ToUInt64(blockData, (int)offset);
                var  recLen   = BitConverter.ToUInt16(blockData, (int)offset + 8);
                byte nameLen  = blockData[(int)offset                        + 10];
                var  fileType = (FileType)blockData[(int)offset + 11];

                if(recLen < 12)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Invalid directory entry rec_len: {0} at offset {1}",
                                      recLen,
                                      offset);

                    break;
                }

                if(entInode != 0 && nameLen > 0 && offset + 12 + nameLen <= blockData.Length)
                {
                    string name = _encoding.GetString(blockData, (int)offset + 12, nameLen);

                    entries[name] = new DirectoryEntryInfo
                    {
                        Name        = name,
                        InodeNumber = entInode,
                        Type        = fileType
                    };
                }

                offset    += recLen;
                bytesRead += recLen;
            }

            logicalBlock++;
        }

        return ErrorNumber.NoError;
    }
}