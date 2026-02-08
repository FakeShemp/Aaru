// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Logging;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Cram
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            stat = InodeToFileEntryInfo(_superBlock.root);

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo entry))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // If this is the last component, return its stat
            if(p == pathComponents.Length - 1)
            {
                stat = InodeToFileEntryInfo(entry.Inode);

                return ErrorNumber.NoError;
            }

            // Not the last component - must be a directory
            if(!IsDirectory(entry.Inode.Mode))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents for next iteration
            uint dirOffset = entry.Inode.Offset << 2;
            uint dirSize   = entry.Inode.Size;

            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            ErrorNumber errno = ReadDirectoryContents(dirOffset, dirSize, dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory contents: {0}", errno);

                return errno;
            }

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Get file stat to verify it exists and determine its type
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: Stat failed with {0}", errno);

            return errno;
        }

        // Check it's not a directory
        if(stat.Attributes.HasFlag(FileAttributes.Directory))
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: path is a directory");

            return ErrorNumber.IsDirectory;
        }

        // Look up the file's inode
        errno = LookupFile(path, out DirectoryEntryInfo entry);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LookupFile failed with {0}", errno);

            return errno;
        }

        uint size       = entry.Inode.Size;
        uint blockCount = (size + PAGE_SIZE - 1) / PAGE_SIZE;

        node = new CramFileNode
        {
            Path           = path,
            Length         = size,
            Offset         = 0,
            Inode          = entry.Inode,
            BlockPtrOffset = entry.Inode.Offset << 2,
            BlockCount     = blockCount
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}, blocks={1}", size, blockCount);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not CramFileNode fileNode) return ErrorNumber.InvalidArgument;

        // Clear node state to indicate it's closed
        fileNode.Offset         = -1;
        fileNode.Length         = 0;
        fileNode.BlockPtrOffset = 0;
        fileNode.BlockCount     = 0;
        fileNode.Path           = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ReadLink: path='{0}'", path);

        // Get file stat to verify it's a symlink
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        if(!stat.Attributes.HasFlag(FileAttributes.Symlink)) return ErrorNumber.InvalidArgument;

        // Look up the file's entry
        errno = LookupFile(path, out DirectoryEntryInfo entry);

        if(errno != ErrorNumber.NoError) return errno;

        uint size = entry.Inode.Size;

        if(size == 0)
        {
            dest = "";

            return ErrorNumber.NoError;
        }

        // Read symlink target - symlinks are typically small
        // Create a temporary file node to read the data
        uint blockCount = (size + PAGE_SIZE - 1) / PAGE_SIZE;

        var tempNode = new CramFileNode
        {
            Path           = path,
            Length         = size,
            Offset         = 0,
            Inode          = entry.Inode,
            BlockPtrOffset = entry.Inode.Offset << 2,
            BlockCount     = blockCount
        };

        var linkData  = new byte[size];
        var bytesRead = 0;

        // Read block by block
        for(var blockIndex = 0; blockIndex < blockCount && bytesRead < size; blockIndex++)
        {
            errno = ReadBlock(tempNode, blockIndex, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadBlock failed for block {0}: {1}", blockIndex, errno);

                return errno;
            }

            int toCopy = Math.Min(blockData.Length, (int)(size - bytesRead));
            Array.Copy(blockData, 0, linkData, bytesRead, toCopy);
            bytesRead += toCopy;
        }

        dest = _encoding.GetString(linkData, 0, (int)size).TrimEnd('\0');

        AaruLogging.Debug(MODULE_NAME, "ReadLink: target='{0}'", dest);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not CramFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate which block we need
            var blockIndex = (int)(currentOffset / PAGE_SIZE);

            // Read and decompress this block
            ErrorNumber errno = ReadBlock(fileNode, blockIndex, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: ReadBlock failed for block {0}: {1}", blockIndex, errno);

                if(bytesRead > 0) break;

                return errno;
            }

            // Calculate offset within the block
            var offsetInBlock = (int)(currentOffset % PAGE_SIZE);

            // Calculate how much to copy from this block
            int toCopy = Math.Min(blockData.Length - offsetInBlock, (int)(toRead - bytesRead));

            if(toCopy <= 0) break;

            Array.Copy(blockData, offsetInBlock, buffer, bytesRead, toCopy);

            bytesRead     += toCopy;
            currentOffset += toCopy;
        }

        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        return ErrorNumber.NoError;
    }


    /// <summary>Looks up a file by path and returns its directory entry</summary>
    /// <param name="path">Path to the file</param>
    /// <param name="entry">The directory entry info</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupFile(string path, out DirectoryEntryInfo entry)
    {
        entry = null;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath == "/") return ErrorNumber.InvalidArgument;

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo foundEntry)) return ErrorNumber.NoSuchFile;

            // If this is the last component, return it
            if(p == pathComponents.Length - 1)
            {
                entry = foundEntry;

                return ErrorNumber.NoError;
            }

            // Not the last component - must be a directory
            if(!IsDirectory(foundEntry.Inode.Mode)) return ErrorNumber.NotDirectory;

            // Read directory contents for next iteration
            uint dirOffset = foundEntry.Inode.Offset << 2;
            uint dirSize   = foundEntry.Inode.Size;

            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            ErrorNumber errno = ReadDirectoryContents(dirOffset, dirSize, dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }
}