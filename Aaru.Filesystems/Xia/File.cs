// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Xia filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class Xia
{
    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ReadLink: path='{0}'", path);

        // Get file metadata to verify it's a symlink
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: Stat failed with {0}", errno);

            return errno;
        }

        // Verify it's a symbolic link
        if(!stat.Attributes.HasFlag(FileAttributes.Symlink))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: path is not a symbolic link");

            return ErrorNumber.InvalidArgument;
        }

        // Get the inode number
        errno = GetInodeNumber(path, out uint inodeNum);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: GetInodeNumber failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInode(inodeNum, out Inode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadInode failed with {0}", errno);

            return errno;
        }

        // Validate symlink size
        if(inode.i_size <= 0 || inode.i_size > XIAFS_NAME_LEN)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: invalid symlink size {0}", inode.i_size);

            return ErrorNumber.InvalidArgument;
        }

        // Read the symlink target from the file data
        // The target path is stored in the data zones, just like file content
        var linkData = new byte[inode.i_size];

        uint bytesRead     = 0;
        uint currentOffset = 0;

        while(bytesRead < inode.i_size)
        {
            uint zoneSize     = _superblock.s_zone_size;
            uint zoneNum      = currentOffset / zoneSize;
            var  offsetInZone = (int)(currentOffset % zoneSize);

            errno = MapZone(inode, zoneNum, out uint physicalZone);

            if(errno != ErrorNumber.NoError || physicalZone == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadLink: MapZone failed for zone {0}", zoneNum);

                return ErrorNumber.InvalidArgument;
            }

            errno = ReadZone(physicalZone, out byte[] zoneData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadZone failed for zone {0}", physicalZone);

                return errno;
            }

            uint bytesToCopy = Math.Min(zoneSize - (uint)offsetInZone, inode.i_size - bytesRead);
            Array.Copy(zoneData, offsetInZone, linkData, bytesRead, bytesToCopy);

            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        // Convert to string, stopping at null terminator
        dest = _encoding.GetString(linkData).TrimEnd('\0');

        AaruLogging.Debug(MODULE_NAME, "ReadLink: target='{0}'", dest);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Get file stat to verify it exists and is a regular file
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

        // Get the inode number by navigating the path
        errno = GetInodeNumber(path, out uint inodeNum);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: GetInodeNumber failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInode(inodeNum, out Inode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadInode failed with {0}", errno);

            return errno;
        }

        node = new XiaFileNode
        {
            Path     = path,
            Length   = inode.i_size,
            Offset   = 0,
            InodeNum = inodeNum,
            Inode    = inode
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}", inode.i_size);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not XiaFileNode) return ErrorNumber.InvalidArgument;

        // Nothing to clean up - no caching
        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not XiaFileNode fileNode) return ErrorNumber.InvalidArgument;

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
            // Calculate which zone contains the current offset
            uint zoneSize     = _superblock.s_zone_size;
            var  zoneNum      = (uint)(currentOffset / zoneSize);
            var  offsetInZone = (int)(currentOffset  % zoneSize);

            // Map logical zone to physical zone using bmap logic
            ErrorNumber errno = MapZone(fileNode.Inode, zoneNum, out uint physicalZone);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: MapZone failed for zone {0}: {1}", zoneNum, errno);

                // Sparse file - zone not allocated, fill with zeros
                if(errno == ErrorNumber.NoSuchFile)
                {
                    long bytesToZero = Math.Min(zoneSize - offsetInZone, toRead - bytesRead);
                    Array.Clear(buffer, (int)bytesRead, (int)bytesToZero);
                    bytesRead     += bytesToZero;
                    currentOffset += bytesToZero;

                    continue;
                }

                return errno;
            }

            // Zone address 0 means sparse/hole
            if(physicalZone == 0)
            {
                long bytesToZero = Math.Min(zoneSize - offsetInZone, toRead - bytesRead);
                Array.Clear(buffer, (int)bytesRead, (int)bytesToZero);
                bytesRead     += bytesToZero;
                currentOffset += bytesToZero;

                continue;
            }

            // Read the zone
            errno = ReadZone(physicalZone, out byte[] zoneData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: ReadZone failed for zone {0}: {1}", physicalZone, errno);

                return errno;
            }

            // Copy data from zone to buffer
            long bytesToCopy = Math.Min(zoneSize - offsetInZone, toRead - bytesRead);
            Array.Copy(zoneData, offsetInZone, buffer, bytesRead, bytesToCopy);

            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: read {0} bytes, new offset={1}", read, fileNode.Offset);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read the root inode
            ErrorNumber rootError = ReadInode(XIAFS_ROOT_INO, out Inode rootInode);

            if(rootError != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", rootError);

                return rootError;
            }

            stat = new FileEntryInfo
            {
                Attributes       = FileAttributes.Directory,
                Inode            = XIAFS_ROOT_INO,
                Links            = rootInode.i_nlinks,
                Length           = rootInode.i_size,
                BlockSize        = _superblock.s_zone_size,
                UID              = rootInode.i_uid,
                GID              = rootInode.i_gid,
                Mode             = rootInode.i_mode,
                CreationTimeUtc  = DateHandlers.UnixToDateTime(rootInode.i_ctime),
                LastWriteTimeUtc = DateHandlers.UnixToDateTime(rootInode.i_mtime),
                AccessTimeUtc    = DateHandlers.UnixToDateTime(rootInode.i_atime)
            };

            return ErrorNumber.NoError;
        }

        // Parse path and navigate to target
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Navigate to the target
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;
        string                   targetName     = pathComponents[^1];

        // Traverse all but the last component
        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            AaruLogging.Debug(MODULE_NAME, "Navigating to component '{0}'", component);

            if(!currentEntries.TryGetValue(component, out uint childInodeNum))
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            ErrorNumber errno = ReadInode(childInodeNum, out Inode childInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child inode: {0}", errno);

                return errno;
            }

            // Check if it's a directory (S_IFDIR = 0x4000)
            if((childInode.i_mode & 0xF000) != 0x4000)
            {
                AaruLogging.Debug(MODULE_NAME, "Component '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            errno = ReadDirectoryEntries(childInode, out Dictionary<string, uint> childEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading child directory: {0}", errno);

                return errno;
            }

            currentEntries = childEntries;
        }

        // Find the target in the current directory
        if(!currentEntries.TryGetValue(targetName, out uint targetInodeNum))
        {
            AaruLogging.Debug(MODULE_NAME, "Target '{0}' not found", targetName);

            return ErrorNumber.NoSuchFile;
        }

        // Read the target inode
        ErrorNumber readError = ReadInode(targetInodeNum, out Inode targetInode);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading target inode: {0}", readError);

            return readError;
        }

        // Build FileEntryInfo from the inode
        stat = new FileEntryInfo
        {
            Inode            = targetInodeNum,
            Links            = targetInode.i_nlinks,
            Length           = targetInode.i_size,
            BlockSize        = _superblock.s_zone_size,
            UID              = targetInode.i_uid,
            GID              = targetInode.i_gid,
            Mode             = targetInode.i_mode,
            CreationTimeUtc  = DateHandlers.UnixToDateTime(targetInode.i_ctime),
            LastWriteTimeUtc = DateHandlers.UnixToDateTime(targetInode.i_mtime),
            AccessTimeUtc    = DateHandlers.UnixToDateTime(targetInode.i_atime)
        };

        // Determine file type from mode field (Unix-style S_IFMT mask = 0xF000)
        stat.Attributes = (targetInode.i_mode & 0xF000) switch
                          {
                              0x4000 => // S_IFDIR - directory
                                  FileAttributes.Directory,
                              0xA000 => // S_IFLNK - symbolic link
                                  FileAttributes.Symlink,
                              0x2000 => // S_IFCHR - character device
                                  FileAttributes.CharDevice,
                              0x6000 => // S_IFBLK - block device
                                  FileAttributes.BlockDevice,
                              0x1000 => // S_IFIFO - FIFO
                                  FileAttributes.FIFO,
                              0xC000 => // S_IFSOCK - socket
                                  FileAttributes.Socket,
                              _ => FileAttributes.File
                          };

        AaruLogging.Debug(MODULE_NAME,
                          "Stat successful: name='{0}', size={1}, inode={2}, mode=0x{3:X4}",
                          targetName,
                          stat.Length,
                          stat.Inode,
                          stat.Mode);

        return ErrorNumber.NoError;
    }
}