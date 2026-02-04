// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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
public sealed partial class AtheOS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory - return cached entries
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new AtheosDirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory traversal
        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "Traversing path with {0} components", pathComponents.Length);

        // Start from root directory cache
        Dictionary<string, long> currentEntries = _rootDirectoryCache;

        // Traverse each path component
        foreach(string component in pathComponents)
        {
            AaruLogging.Debug(MODULE_NAME, "Navigating to component '{0}'", component);

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out long childInodeAddr))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' not found in directory", component);

                return ErrorNumber.NoSuchFile;
            }

            // Convert i-node address to block address
            // In AtheOS, the inode address stored in B+tree is: (group * blocks_per_ag) + start
            // We can use it directly as a block number
            AaruLogging.Debug(MODULE_NAME, "Component '{0}': i-node block address = {1}", component, childInodeAddr);

            // Read the child i-node
            ErrorNumber errno = ReadInode(childInodeAddr, out Inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child i-node: {0}", errno);

                return errno;
            }

            // Validate inode magic
            if(childInode.magic1 != INODE_MAGIC)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Invalid i-node magic for '{0}': 0x{1:X8}",
                                  component,
                                  childInode.magic1);

                return ErrorNumber.InvalidArgument;
            }

            // Check if it's a directory
            if(!IsDirectory(childInode))
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Component '{0}' is not a directory (mode=0x{1:X})",
                                  component,
                                  childInode.mode);

                return ErrorNumber.NotDirectory;
            }

            // Parse the child directory's B+tree
            errno = ParseDirectoryBTree(childInode.data, out Dictionary<string, long> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error parsing child directory B+tree: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Create directory node with the entries we found
        var dirNode = new AtheosDirNode
        {
            Path     = normalizedPath,
            Position = 0,
            Entries  = currentEntries.Keys.ToArray()
        };

        node = dirNode;

        AaruLogging.Debug(MODULE_NAME,
                          "Successfully opened directory '{0}' with {1} entries",
                          normalizedPath,
                          dirNode.Entries.Length);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not AtheosDirNode atheosDirNode) return ErrorNumber.InvalidArgument;

        atheosDirNode.Position = -1;
        atheosDirNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not AtheosDirNode atheosDirNode) return ErrorNumber.InvalidArgument;

        if(atheosDirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(atheosDirNode.Position >= atheosDirNode.Entries.Length) return ErrorNumber.NoError;

        filename = atheosDirNode.Entries[atheosDirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Checks if an i-node represents a directory</summary>
    /// <param name="inode">The inode to check</param>
    /// <returns>True if the inode is a directory, false otherwise</returns>
    static bool IsDirectory(Inode inode) =>

        // In AtheOS, the mode field contains Unix-style permission bits
        // S_IFDIR = 0x4000 (octal 040000)
        (inode.mode & 0xF000) == 0x4000;
}