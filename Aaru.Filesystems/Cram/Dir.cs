// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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
public sealed partial class Cram
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

            node = new CramDirNode
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

            AaruLogging.Debug(MODULE_NAME, "OpenDir: Component '{0}' found", component);

            // Check if it's a directory
            if(!IsDirectory(entry.Inode.Mode))
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: '{0}' is not a directory (mode=0x{1:X4})",
                                  component,
                                  entry.Inode.Mode);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents
            uint dirOffset = entry.Inode.Offset << 2;
            uint dirSize   = entry.Inode.Size;

            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            ErrorNumber errno = ReadDirectoryContents(dirOffset, dirSize, dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: Error reading directory contents: {0}", errno);

                return errno;
            }

            // If this is the last component, we're opening this directory
            if(p == pathComponents.Length - 1)
            {
                node = new CramDirNode
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
        if(node is not CramDirNode cramNode) return ErrorNumber.InvalidArgument;

        cramNode.Position   = -1;
        cramNode.Entries    = null;
        cramNode.EntryNames = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not CramDirNode cramNode) return ErrorNumber.InvalidArgument;

        if(cramNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(cramNode.Position >= cramNode.EntryNames.Length) return ErrorNumber.NoError;

        // Get current filename and advance position
        filename = cramNode.EntryNames[cramNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and parses directory contents</summary>
    /// <param name="offset">Byte offset of the directory data</param>
    /// <param name="size">Size of the directory in bytes</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(uint offset, uint size, Dictionary<string, DirectoryEntryInfo> entries)
    {
        if(size == 0) return ErrorNumber.NoError;

        // Read the directory data
        ErrorNumber errno = ReadBytes(offset, size, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory data at offset {0}: {1}", offset, errno);

            return errno;
        }

        // Parse directory entries
        // Each entry is: cramfs_inode (12 bytes) + name (padded to 4 bytes)
        uint currentOffset = 0;

        while(currentOffset < size)
        {
            if(currentOffset + 12 > dirData.Length) break;

            var inodeData = new byte[12];
            Array.Copy(dirData, currentOffset, inodeData, 0, 12);

            Inode inode = _littleEndian
                              ? Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData)
                              : Marshal.ByteArrayToStructureBigEndian<Inode>(inodeData);

            // Name length is stored as (actual_length + 3) / 4, so multiply by 4 to get padded length
            int nameLen = inode.NameLen << 2;

            if(nameLen == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Zero name length at offset {0}", currentOffset);

                break;
            }

            if(currentOffset + 12 + nameLen > dirData.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Name extends beyond directory data");

                break;
            }

            // Read the name (it's null-padded to 4-byte boundary)
            var nameBytes = new byte[nameLen];
            Array.Copy(dirData, currentOffset + 12, nameBytes, 0, nameLen);

            string name = StringHandlers.CToString(nameBytes, _encoding);

            if(string.IsNullOrEmpty(name))
            {
                AaruLogging.Debug(MODULE_NAME, "Empty name at offset {0}", currentOffset);

                break;
            }

            // Skip . and .. entries
            if(name is not "." and not "..")
            {
                var entry = new DirectoryEntryInfo
                {
                    Name   = name,
                    Inode  = inode,
                    Offset = offset + currentOffset
                };

                entries.TryAdd(name, entry);
            }

            currentOffset += (uint)(12 + nameLen);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Checks if a mode indicates a directory</summary>
    static bool IsDirectory(ushort mode) => (mode & 0xF000) == 0x4000; // S_IFDIR
}