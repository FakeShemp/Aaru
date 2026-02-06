// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class RBF
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/")
        {
            node = new RbfDirNode
            {
                Path           = "/",
                Position       = 0,
                DirectoryFdLsn = _rootDirLsn,
                Contents       = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start traversal from root directory cache
        Dictionary<string, CachedDirectoryEntry> currentDirectory = _rootDirectoryCache;
        CachedDirectoryEntry                     targetEntry      = null;

        // Traverse all path components
        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            // Skip . and ..
            if(component == "." || component == "..") continue;

            // Find the component in current directory
            if(!currentDirectory.TryGetValue(component, out CachedDirectoryEntry entry)) return ErrorNumber.NoSuchFile;

            // If this is the last component, we found our target
            if(i == pathComponents.Length - 1)
            {
                targetEntry = entry;

                break;
            }

            // Not the last component - must be a directory to continue traversal
            if(!entry.IsDirectory) return ErrorNumber.NotDirectory;

            // Read the subdirectory contents
            ErrorNumber errno = ReadDirectoryContents(entry.Fd, out Dictionary<string, CachedDirectoryEntry> subDir);

            if(errno != ErrorNumber.NoError) return errno;

            currentDirectory = subDir;
        }

        if(targetEntry == null) return ErrorNumber.NoSuchFile;

        // Verify the target is a directory
        if(!targetEntry.IsDirectory) return ErrorNumber.NotDirectory;

        // Read the target directory contents
        ErrorNumber readError = ReadDirectoryContents(targetEntry.Fd,
                                                      out Dictionary<string, CachedDirectoryEntry> targetContents);

        if(readError != ErrorNumber.NoError) return readError;

        node = new RbfDirNode
        {
            Path           = normalizedPath,
            Position       = 0,
            DirectoryFdLsn = targetEntry.FdLsn,
            Contents       = targetContents.Keys.ToArray()
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not RbfDirNode rbfNode) return ErrorNumber.InvalidArgument;

        rbfNode.Position = -1;
        rbfNode.Contents = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not RbfDirNode rbfNode) return ErrorNumber.InvalidArgument;

        if(rbfNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(rbfNode.Contents == null || rbfNode.Position >= rbfNode.Contents.Length) return ErrorNumber.NoError;

        filename = rbfNode.Contents[rbfNode.Position++];

        return ErrorNumber.NoError;
    }
}