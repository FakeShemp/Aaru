// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
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
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AtheOS
{
    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ReadLink: path='{0}'", path);

        // Get the inode for the path
        ErrorNumber errno = GetInodeForPath(path, out Inode inode, out byte[] _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting inode for path: {0}", errno);

            return errno;
        }

        // Validate inode magic
        if(inode.magic1 != INODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid i-node magic: 0x{0:X8}", inode.magic1);

            return ErrorNumber.InvalidArgument;
        }

        // Check if it's a symbolic link (S_IFLNK = 0xA000)
        int fileType = inode.mode & 0xF000;

        if(fileType != 0xA000)
        {
            AaruLogging.Debug(MODULE_NAME, "Path is not a symbolic link (mode=0x{0:X})", inode.mode);

            return ErrorNumber.InvalidArgument;
        }

        // Read the symlink target from the data stream
        if(inode.data.size <= 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Symlink has empty data stream");
            dest = "";

            return ErrorNumber.NoError;
        }

        // Limit symlink size to reasonable value
        if(inode.data.size > 4096)
        {
            AaruLogging.Debug(MODULE_NAME, "Symlink data size too large: {0}", inode.data.size);

            return ErrorNumber.InvalidArgument;
        }

        errno = ReadFromDataStream(inode.data, 0, (int)inode.data.size, out byte[] linkData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading symlink data: {0}", errno);

            return errno;
        }

        // Convert to string, stopping at null terminator if present
        dest = _encoding.GetString(linkData).TrimEnd('\0');

        AaruLogging.Debug(MODULE_NAME, "ReadLink successful: target='{0}'", dest);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Get the inode for the path
        ErrorNumber errno = GetInodeForPath(path, out Inode inode, out byte[] _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting inode for path: {0}", errno);

            return errno;
        }

        // Validate inode magic
        if(inode.magic1 != INODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid i-node magic: 0x{0:X8}", inode.magic1);

            return ErrorNumber.InvalidArgument;
        }

        // Check if it's a regular file
        int fileType = inode.mode & 0xF000;

        if(fileType == 0x4000) // S_IFDIR
        {
            AaruLogging.Debug(MODULE_NAME, "Cannot open directory as file");

            return ErrorNumber.IsDirectory;
        }

        // Create file node
        node = new AtheosFileNode
        {
            Path       = path,
            Inode      = inode,
            DataStream = inode.data,
            Length     = inode.data.size,
            Offset     = 0
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile successful: path='{0}', size={1}", path, inode.data.size);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not AtheosFileNode atheosNode) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "CloseFile: path='{0}'", atheosNode.Path);

        // Reset the node
        atheosNode.Offset = -1;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not AtheosFileNode atheosNode) return ErrorNumber.InvalidArgument;

        if(atheosNode.Offset < 0) return ErrorNumber.InvalidArgument;

        if(buffer == null || buffer.Length < length) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadFile: path='{0}', offset={1}, length={2}",
                          atheosNode.Path,
                          atheosNode.Offset,
                          length);

        // Check if we're at or past EOF
        if(atheosNode.Offset >= atheosNode.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFile: at EOF");

            return ErrorNumber.NoError;
        }

        // Adjust length if it would read past EOF
        long bytesToRead = length;

        if(atheosNode.Offset + bytesToRead > atheosNode.Length) bytesToRead = atheosNode.Length - atheosNode.Offset;

        if(bytesToRead <= 0) return ErrorNumber.NoError;

        // Read from the data stream
        ErrorNumber errno = ReadFromDataStream(atheosNode.DataStream,
                                               atheosNode.Offset,
                                               (int)bytesToRead,
                                               out byte[] readBuffer);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading from data stream: {0}", errno);

            return errno;
        }

        // Copy to output buffer
        Array.Copy(readBuffer, 0, buffer, 0, bytesToRead);
        read              =  bytesToRead;
        atheosNode.Offset += bytesToRead;

        AaruLogging.Debug(MODULE_NAME, "ReadFile successful: read {0} bytes, new offset={1}", read, atheosNode.Offset);

        return ErrorNumber.NoError;
    }

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

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize and parse the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Calculate block address for root directory
            long rootBlockAddress = (long)_superblock.root_dir_ag * _superblock.blocks_per_ag +
                                    _superblock.root_dir_start;

            // Read the actual root i-node to get its timestamps
            ErrorNumber rootError = ReadInode(rootBlockAddress, out Inode rootInode);

            if(rootError != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading root i-node: {0}", rootError);

                return rootError;
            }

            // Return stats for root directory with real i-node data
            // AtheOS timestamps are in microseconds since epoch
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                Inode      = (ulong)rootBlockAddress,
                CreationTimeUtc =
                    DateHandlers.UnixUnsignedToDateTime((ulong)(rootInode.create_time / 1000000))
                                .AddTicks(rootInode.create_time % 1000000 * 10),
                LastWriteTimeUtc =
                    DateHandlers.UnixUnsignedToDateTime((ulong)(rootInode.modified_time / 1000000))
                                .AddTicks(rootInode.modified_time % 1000000 * 10),
                AccessTimeUtc =
                    DateHandlers.UnixUnsignedToDateTime((ulong)(rootInode.modified_time / 1000000))
                                .AddTicks(rootInode.modified_time % 1000000 * 10),
                UID   = (ulong)rootInode.uid,
                GID   = (ulong)rootInode.gid,
                Mode  = (uint)rootInode.mode,
                Links = (ulong)rootInode.link_count
            };

            return ErrorNumber.NoError;
        }

        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Navigate to the target file/directory
        Dictionary<string, long> currentEntries = _rootDirectoryCache;
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

            // Read the child i-node
            ErrorNumber errno = ReadInode(childInodeAddr, out Inode childInode);

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
        }

        // Find the target file/directory in the current directory
        if(!currentEntries.TryGetValue(targetName, out long targetInodeAddr))
        {
            AaruLogging.Debug(MODULE_NAME, "Target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        // Read the target i-node
        AaruLogging.Debug(MODULE_NAME, "Target '{0}': i-node block address = {1}", targetName, targetInodeAddr);

        ErrorNumber readError = ReadInode(targetInodeAddr, out Inode targetInode);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading target i-node: {0}", readError);

            return readError;
        }

        // Validate inode magic
        if(targetInode.magic1 != INODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid i-node magic: 0x{0:X8}", targetInode.magic1);

            return ErrorNumber.InvalidArgument;
        }

        // Build FileEntryInfo from the i-node
        // AtheOS timestamps are in microseconds since epoch
        stat = new FileEntryInfo
        {
            Length = targetInode.data.size,
            Inode  = (ulong)targetInodeAddr,
            UID    = (ulong)targetInode.uid,
            GID    = (ulong)targetInode.gid,
            CreationTimeUtc =
                DateHandlers.UnixUnsignedToDateTime((ulong)(targetInode.create_time / 1000000))
                            .AddTicks(targetInode.create_time % 1000000 * 10),
            LastWriteTimeUtc =
                DateHandlers.UnixUnsignedToDateTime((ulong)(targetInode.modified_time / 1000000))
                            .AddTicks(targetInode.modified_time % 1000000 * 10),
            AccessTimeUtc = DateHandlers.UnixUnsignedToDateTime((ulong)(targetInode.modified_time / 1000000))
                                        .AddTicks(targetInode.modified_time % 1000000 * 10),
            Mode  = (uint)targetInode.mode,
            Links = (ulong)targetInode.link_count
        };

        // Determine file type from mode field (Unix-style file type bits)
        int fileType = targetInode.mode & 0xF000;

        stat.Attributes = fileType switch
                          {
                              0x4000 => // S_IFDIR - Directory
                                  FileAttributes.Directory,
                              0xA000 => // S_IFLNK - Symbolic link
                                  FileAttributes.Symlink,
                              0x2000 => // S_IFCHR - Character device
                                  FileAttributes.CharDevice,
                              0x6000 => // S_IFBLK - Block device
                                  FileAttributes.BlockDevice,
                              0x1000 => // S_IFIFO - FIFO (named pipe)
                                  FileAttributes.FIFO,
                              0xC000 => // S_IFSOCK - Socket
                                  FileAttributes.Socket,
                              _ => FileAttributes.File
                          };

        AaruLogging.Debug(MODULE_NAME,
                          "Stat successful: name='{0}', size={1}, inode={2}, mode=0x{3:X}",
                          targetName,
                          stat.Length,
                          stat.Inode,
                          stat.Mode);

        return ErrorNumber.NoError;
    }
}