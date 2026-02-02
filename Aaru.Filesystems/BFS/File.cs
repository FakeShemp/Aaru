// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from Practical Filesystem Design, ISBN 1-55860-497-9
/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class BeFS
{
    /// <summary>Reads the target path of a symbolic link</summary>
    /// <remarks>
    ///     In BeFS, symbolic links store the target path as a null-terminated string in the file's data stream.
    ///     The symlink i-node's data stream contains the full target path, which can be relative or absolute.
    /// </remarks>
    /// <param name="path">Path to the symbolic link</param>
    /// <param name="dest">Output containing the target path of the symlink</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ReadLink: path='{0}'", path);

        // Get file metadata
        ErrorNumber statError = Stat(path, out FileEntryInfo fileInfo);

        if(statError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting file stat: {0}", statError);

            return statError;
        }

        // Verify it's a symbolic link
        if(!fileInfo.Attributes.HasFlag(FileAttributes.Symlink))
        {
            AaruLogging.Debug(MODULE_NAME, "Path is not a symbolic link");

            return ErrorNumber.InvalidArgument;
        }

        // Get the i-node for the symlink
        ErrorNumber inodeError = GetInodeForPath(path, out bfs_inode inode);

        if(inodeError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting i-node: {0}", inodeError);

            return inodeError;
        }

        // Read the symlink target from the data stream
        // The target is stored as a null-terminated string
        if(inode.data.size <= 0 || inode.data.size > 4096)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid symlink data size: {0}", inode.data.size);

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber readError = ReadFromDataStream(inode.data, 0, (int)inode.data.size, out byte[] linkData);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading symlink data: {0}", readError);

            return readError;
        }

        // Convert the data to a string, stopping at the first null terminator
        dest = _encoding.GetString(linkData).TrimEnd('\0');

        AaruLogging.Debug(MODULE_NAME, "ReadLink successful: target='{0}'", dest);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets the file attributes for a given path</summary>
    /// <remarks>
    ///     Determines the file attributes (directory, file, symlink, etc.) based on the i-node
    ///     mode field. Uses Unix-style file type bits from the mode field.
    /// </remarks>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="attributes">Output file attributes</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.File;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "GetAttributes: path='{0}'", path);

        // Use Stat to get the file information
        ErrorNumber statError = Stat(path, out FileEntryInfo fileInfo);

        if(statError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting file stat: {0}", statError);

            return statError;
        }

        // Copy the attributes from the stat result
        attributes = fileInfo.Attributes;

        AaruLogging.Debug(MODULE_NAME, "GetAttributes successful: path='{0}', attributes=0x{1:X}", path, attributes);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets file metadata (stat) for a given path</summary>
    /// <remarks>
    ///     Locates the file/directory at the specified path, reads its i-node,
    ///     and returns comprehensive metadata including size, permissions, timestamps,
    ///     and ownership information.
    /// </remarks>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="stat">Output file entry information</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize and parse the path
        string normalizedPath                                            = path ?? "/";
        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read the actual root i-node to get its timestamps
            ErrorNumber rootError = ReadInode(_superblock.root_dir, out bfs_inode rootInode);

            if(rootError != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading root i-node: {0}", rootError);

                return rootError;
            }

            // Return stats for root directory with real i-node data
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                Inode = _superblock.root_dir.allocation_group << _superblock.ag_shift | _superblock.root_dir.start,
                CreationTimeUtc = DateHandlers.UnixUnsignedToDateTime(rootInode.create_time >> 16),
                LastWriteTimeUtc = DateHandlers.UnixUnsignedToDateTime(rootInode.last_modified_time >> 16),
                AccessTimeUtc = DateHandlers.UnixUnsignedToDateTime(rootInode.last_modified_time >> 16),
                UID = (ulong)rootInode.uid,
                GID = (ulong)rootInode.gid,
                Mode = (uint)rootInode.mode
            };

            return ErrorNumber.NoError;
        }

        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Navigate to the target file/directory
        Dictionary<string, long> currentEntries = _rootDirectoryCache;
        bfs_inode                currentInode   = default;
        string                   targetName     = pathComponents[^1]; // Last component is the target name

        // Traverse all but the last component
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];
            AaruLogging.Debug(MODULE_NAME, "Navigating to component '{0}'", component);

            if(!currentEntries.TryGetValue(component, out long childInodeAddr))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // The childInodeAddr is a direct block number (inode address)
            // Use it directly to calculate the block_run
            AaruLogging.Debug(MODULE_NAME,
                              "Component '{0}': raw i-node block address = {1}",
                              component,
                              childInodeAddr);

            var ag    = (uint)(childInodeAddr >> _superblock.ag_shift);
            var start = (uint)(childInodeAddr - (ag << _superblock.ag_shift));

            block_run childInodeBlockRun = new()
            {
                allocation_group = ag,
                start            = (ushort)start,
                len              = 1
            };

            AaruLogging.Debug(MODULE_NAME,
                              "Component '{0}': converted to AG={1}, start={2}, len={3}",
                              component,
                              ag,
                              start,
                              1);

            ErrorNumber errno = ReadInode(childInodeBlockRun, out bfs_inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child i-node: {0}", errno);

                return errno;
            }

            if(!IsDirectory(childInode))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            errno = ParseDirectoryBTree(childInode.data, out Dictionary<string, long> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error parsing child directory: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
            currentInode   = childInode;
        }

        // Find the target file/directory in the current directory
        if(!currentEntries.TryGetValue(targetName, out long targetInodeAddr))
        {
            AaruLogging.Debug(MODULE_NAME, "Target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        // Read the target i-node
        // The targetInodeAddr is a direct block number (inode address)
        // Use it directly to calculate the block_run
        AaruLogging.Debug(MODULE_NAME, "Target '{0}': raw i-node block address = {1}", targetName, targetInodeAddr);

        var targetAg    = (uint)(targetInodeAddr >> _superblock.ag_shift);
        var targetStart = (uint)(targetInodeAddr - (targetAg << _superblock.ag_shift));

        block_run targetInodeBlockRun = new()
        {
            allocation_group = targetAg,
            start            = (ushort)targetStart,
            len              = 1
        };

        AaruLogging.Debug(MODULE_NAME,
                          "Target '{0}': converted to AG={1}, start={2}, len={3}",
                          targetName,
                          targetAg,
                          targetStart,
                          1);

        ErrorNumber readError = ReadInode(targetInodeBlockRun, out bfs_inode targetInode);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading target i-node: {0}", readError);

            return readError;
        }

        // Build FileEntryInfo from the i-node
        stat = new FileEntryInfo
        {
            Length           = targetInode.data.size,
            Inode            = (ulong)targetInodeAddr,
            UID              = (ulong)targetInode.uid,
            GID              = (ulong)targetInode.gid,
            CreationTimeUtc  = DateHandlers.UnixUnsignedToDateTime(targetInode.create_time        >> 16),
            LastWriteTimeUtc = DateHandlers.UnixUnsignedToDateTime(targetInode.last_modified_time >> 16),
            AccessTimeUtc    = DateHandlers.UnixUnsignedToDateTime(targetInode.last_modified_time >> 16),
            Mode             = (uint)targetInode.mode
        };

        // Determine file type from mode field
        if(IsDirectory(targetInode))
            stat.Attributes |= FileAttributes.Directory;
        else if(IsSymlink(targetInode))
            stat.Attributes |= FileAttributes.Symlink;
        else if(IsCharDevice(targetInode))
            stat.Attributes |= FileAttributes.CharDevice;
        else if(IsBlockDevice(targetInode))
            stat.Attributes |= FileAttributes.BlockDevice;
        else if(IsFIFO(targetInode))
            stat.Attributes |= FileAttributes.FIFO;
        else if(IsSocket(targetInode))
            stat.Attributes |= FileAttributes.Socket;
        else
        {
            // Regular file
            stat.Attributes |= FileAttributes.File;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Stat successful: name='{0}', size={1}, inode={2}",
                          targetName,
                          stat.Length,
                          stat.Inode);

        return ErrorNumber.NoError;
    }

    /// <summary>Checks if an i-node represents a symbolic link</summary>
    /// <remarks>
    ///     Uses Unix-style file type bits from the mode field.
    ///     S_IFLNK = 0xA000
    /// </remarks>
    private bool IsSymlink(bfs_inode inode) => (inode.mode & 0xF000) == 0xA000;

    /// <summary>Checks if an i-node represents a character device</summary>
    /// <remarks>
    ///     Uses Unix-style file type bits from the mode field.
    ///     S_IFCHR = 0x2000
    /// </remarks>
    private bool IsCharDevice(bfs_inode inode) => (inode.mode & 0xF000) == 0x2000;

    /// <summary>Checks if an i-node represents a block device</summary>
    /// <remarks>
    ///     Uses Unix-style file type bits from the mode field.
    ///     S_IFBLK = 0x6000
    /// </remarks>
    private bool IsBlockDevice(bfs_inode inode) => (inode.mode & 0xF000) == 0x6000;

    /// <summary>Checks if an i-node represents a FIFO (named pipe)</summary>
    /// <remarks>
    ///     Uses Unix-style file type bits from the mode field.
    ///     S_IFIFO = 0x1000
    /// </remarks>
    private bool IsFIFO(bfs_inode inode) => (inode.mode & 0xF000) == 0x1000;

    /// <summary>Checks if an i-node represents a socket</summary>
    /// <remarks>
    ///     Uses Unix-style file type bits from the mode field.
    ///     S_IFSOCK = 0xC000
    /// </remarks>
    private bool IsSocket(bfs_inode inode) => (inode.mode & 0xF000) == 0xC000;

    /// <summary>Opens a file for reading</summary>
    /// <remarks>
    ///     Validates that the path points to a regular file (not a directory),
    ///     reads the file's i-node, and creates a file node for tracking the current read position.
    ///     No data is cached - only metadata needed for reading.
    /// </remarks>
    /// <param name="path">Path to the file to open</param>
    /// <param name="node">Output file node for read operations</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Get file metadata
        ErrorNumber statError = Stat(path, out FileEntryInfo fileInfo);

        if(statError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting file stat: {0}", statError);

            return statError;
        }

        // Verify it's a regular file, not a directory or special file
        if(fileInfo.Attributes.HasFlag(FileAttributes.Directory))
        {
            AaruLogging.Debug(MODULE_NAME, "Cannot open directory as file");

            return ErrorNumber.IsDirectory;
        }

        // Get the i-node for the file to store in the node
        ErrorNumber inodeError = GetInodeForPath(path, out bfs_inode inode);

        if(inodeError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting i-node: {0}", inodeError);

            return inodeError;
        }

        // Create file node
        node = new BefsFileNode
        {
            Path       = path,
            Length     = fileInfo.Length,
            Offset     = 0,
            Inode      = inode,
            DataStream = inode.data
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile successful: path='{0}', size={1}", path, fileInfo.Length);

        return ErrorNumber.NoError;
    }

    /// <summary>Closes a file, freeing any resources</summary>
    /// <remarks>
    ///     Validates the file node and clears references.
    ///     No actual cleanup needed since no data is cached.
    /// </remarks>
    /// <param name="node">The file node to close</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not BefsFileNode befsNode)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid file node type");

            return ErrorNumber.InvalidArgument;
        }

        // Clear the i-node data
        befsNode.Inode = default(bfs_inode);

        AaruLogging.Debug(MODULE_NAME, "CloseFile successful: path='{0}'", node.Path);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads data from an opened file at the current offset</summary>
    /// <remarks>
    ///     Reads the specified amount of data from the file using the data stream.
    ///     Updates the file node's offset after each read.
    ///     No caching - data is read on-demand using ReadFromDataStream.
    /// </remarks>
    /// <param name="node">The file node to read from</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="buffer">Buffer to receive the data</param>
    /// <param name="read">Output: actual number of bytes read</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(buffer is null || buffer.Length < length)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid buffer");

            return ErrorNumber.InvalidArgument;
        }

        if(node is not BefsFileNode befsNode)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid file node type");

            return ErrorNumber.InvalidArgument;
        }

        if(befsNode.Offset < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid file offset");

            return ErrorNumber.InvalidArgument;
        }

        if(length < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid read length");

            return ErrorNumber.InvalidArgument;
        }

        // If at or past end of file, return zero bytes read
        if(befsNode.Offset >= befsNode.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFile: at EOF");

            return ErrorNumber.NoError;
        }

        // Adjust length to not read past end of file
        long bytesToRead                                                = length;
        if(befsNode.Offset + bytesToRead > befsNode.Length) bytesToRead = befsNode.Length - befsNode.Offset;

        if(bytesToRead == 0) return ErrorNumber.NoError;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}", befsNode.Offset, bytesToRead);

        // Read data from the data stream at current offset
        ErrorNumber readError =
            ReadFromDataStream(befsNode.DataStream, befsNode.Offset, (int)bytesToRead, out byte[] fileData);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading from data stream: {0}", readError);

            return readError;
        }

        // Copy to output buffer
        Array.Copy(fileData, 0, buffer, 0, fileData.Length);
        read = fileData.Length;

        // Update file offset
        befsNode.Offset += read;

        AaruLogging.Debug(MODULE_NAME, "ReadFile successful: read={0}, newOffset={1}", read, befsNode.Offset);

        return ErrorNumber.NoError;
    }
}