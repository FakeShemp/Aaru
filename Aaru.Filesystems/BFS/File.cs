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
using Aaru.CommonTypes.Enums;
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

    // ...existing code...


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

        string[] pathComponents = pathWithoutLeadingSlash.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

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

            // Convert i-node address to block_run
            var ag          = (uint)(childInodeAddr >> 32);
            var blockOffset = (uint)(childInodeAddr & 0xFFFFFFFF);

            var childInodeBlockRun = new block_run
            {
                allocation_group = ag,
                start            = (ushort)blockOffset,
                len              = 1
            };

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
        var targetAg          = (uint)(targetInodeAddr >> 32);
        var targetBlockOffset = (uint)(targetInodeAddr & 0xFFFFFFFF);

        var targetInodeBlockRun = new block_run
        {
            allocation_group = targetAg,
            start            = (ushort)targetBlockOffset,
            len              = 1
        };

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
}