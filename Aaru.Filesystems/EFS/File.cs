// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class EFS
{
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
        errno = LookupFile(path, out uint inodeNumber);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LookupFile failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInode(inodeNumber, out Inode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadInode failed with {0}", errno);

            return errno;
        }

        // Load extents (handling indirect extents for large files)
        errno = LoadExtents(inode, out Extent[] extents);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LoadExtents failed with {0}", errno);

            return errno;
        }

        node = new EfsFileNode
        {
            Path        = path,
            Length      = inode.di_size,
            Offset      = 0,
            InodeNumber = inodeNumber,
            Inode       = inode,
            Extents     = extents
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}, extents={1}", inode.di_size, extents?.Length ?? 0);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not EfsFileNode efsNode) return ErrorNumber.InvalidArgument;

        // Clear cached extents
        efsNode.Extents = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not EfsFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        // Check for inline symlink (no extents, data stored in extent area)
        var fileType = (FileType)(fileNode.Inode.di_mode & (ushort)FileType.IFMT);

        if(fileType is FileType.IFLNK or FileType.IFCHRLNK or FileType.IFBLKLNK &&
           fileNode.Inode.di_numextents == 0                                    &&
           fileNode.Inode.di_size       <= EFS_MAX_INLINE)
        {
            // Inline symlink: data is stored in the extent area of the inode
            // This is read directly from the inode's extent data
            return ReadInlineData(fileNode, toRead, buffer, out read);
        }

        // Normal file: read from extents
        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Find the extent containing the current offset
            // Offset in file is in bytes, extents use basic block offsets
            var logicalBlock = (uint)(currentOffset / EFS_BBSIZE);

            ErrorNumber errno =
                FindExtentForBlock(fileNode.Extents, logicalBlock, out Extent extent, out uint blockInExtent);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadFile: FindExtentForBlock failed for block {0}: {1}",
                                  logicalBlock,
                                  errno);

                // If we've read some data, return what we have
                if(bytesRead > 0) break;

                return errno;
            }

            // Calculate the physical block number
            uint physicalBlock = extent.BlockNumber + blockInExtent;

            // Calculate offset within the block
            var offsetInBlock = (int)(currentOffset % EFS_BBSIZE);

            // Read the block
            errno = ReadBasicBlock((int)physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadFile: ReadBasicBlock failed for block {0}: {1}",
                                  physicalBlock,
                                  errno);

                // If we've read some data, return what we have
                if(bytesRead > 0) break;

                return errno;
            }

            // Calculate how much data to copy from this block
            long bytesToCopy = Math.Min(EFS_BBSIZE - offsetInBlock, toRead - bytesRead);
            Array.Copy(blockData, offsetInBlock, buffer, bytesRead, bytesToCopy);

            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: read {0} bytes, new offset={1}", read, fileNode.Offset);

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

        // Look up the file's inode
        errno = LookupFile(path, out uint inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        // Read the inode
        errno = ReadInode(inodeNumber, out Inode inode);

        if(errno != ErrorNumber.NoError) return errno;

        // Check for inline symlink (data stored in extent area)
        if(inode.di_numextents == 0 && inode.di_size <= EFS_MAX_INLINE)
        {
            // Inline symlink: extract data from the inode's extent area
            // The extents array contains the raw bytes of the symlink target
            if(inode.di_extents == null || inode.di_size <= 0)
            {
                dest = string.Empty;

                return ErrorNumber.NoError;
            }

            // Convert extent array to bytes
            var extentBytes = new byte[EFS_DIRECTEXTENTS * 8];

            for(var i = 0; i < EFS_DIRECTEXTENTS; i++)
            {
                // Each extent is 8 bytes: 4 bytes magic_bn + 4 bytes length_offset
                int    offset = i * 8;
                byte[] bn     = BitConverter.GetBytes(inode.di_extents[i].ex_magic_bn);
                byte[] lo     = BitConverter.GetBytes(inode.di_extents[i].ex_length_offset);

                // Big-endian
                extentBytes[offset]     = bn[3];
                extentBytes[offset + 1] = bn[2];
                extentBytes[offset + 2] = bn[1];
                extentBytes[offset + 3] = bn[0];
                extentBytes[offset + 4] = lo[3];
                extentBytes[offset + 5] = lo[2];
                extentBytes[offset + 6] = lo[1];
                extentBytes[offset + 7] = lo[0];
            }

            dest = _encoding.GetString(extentBytes, 0, inode.di_size).TrimEnd('\0');

            return ErrorNumber.NoError;
        }

        // Non-inline symlink: read from extents like a regular file
        errno = LoadExtents(inode, out Extent[] extents);

        if(errno != ErrorNumber.NoError) return errno;

        var linkData  = new byte[inode.di_size];
        var bytesRead = 0;

        while(bytesRead < inode.di_size)
        {
            var logicalBlock = (uint)(bytesRead / EFS_BBSIZE);

            errno = FindExtentForBlock(extents, logicalBlock, out Extent extent, out uint blockInExtent);

            if(errno != ErrorNumber.NoError) return errno;

            uint physicalBlock = extent.BlockNumber + blockInExtent;
            int  offsetInBlock = bytesRead % EFS_BBSIZE;

            errno = ReadBasicBlock((int)physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError) return errno;

            int bytesToCopy = Math.Min(EFS_BBSIZE - offsetInBlock, inode.di_size - bytesRead);
            Array.Copy(blockData, offsetInBlock, linkData, bytesRead, bytesToCopy);

            bytesRead += bytesToCopy;
        }

        dest = _encoding.GetString(linkData).TrimEnd('\0');

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

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read root inode
            ErrorNumber errno = ReadInode(EFS_ROOTINO, out Inode rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInode, EFS_ROOTINO);

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out uint inodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Read the inode
            ErrorNumber errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading inode {0}: {1}", inodeNumber, errno);

                return errno;
            }

            // If this is the last component, return its stat
            if(p == pathComponents.Length - 1)
            {
                stat = InodeToFileEntryInfo(inode, inodeNumber);

                return ErrorNumber.NoError;
            }

            // Not the last component - must be a directory
            var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

            if(fileType != FileType.IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents for next iteration
            errno = ReadDirectoryContents(inode, out Dictionary<string, uint> dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory contents: {0}", errno);

                return errno;
            }

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Looks up a file by path and returns its inode number</summary>
    /// <param name="path">Path to the file</param>
    /// <param name="inodeNumber">The inode number of the file</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupFile(string path, out uint inodeNumber)
    {
        inodeNumber = 0;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath == "/")
        {
            inodeNumber = EFS_ROOTINO;

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out uint foundInodeNumber)) return ErrorNumber.NoSuchFile;

            // If this is the last component, we found it
            if(p == pathComponents.Length - 1)
            {
                inodeNumber = foundInodeNumber;

                return ErrorNumber.NoError;
            }

            // Not the last component - read directory contents for next iteration
            ErrorNumber errno = ReadInode(foundInodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

            if(fileType != FileType.IFDIR) return ErrorNumber.NotDirectory;

            errno = ReadDirectoryContents(inode, out Dictionary<string, uint> dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }


    /// <summary>Reads inline data from a file node (for inline symlinks)</summary>
    /// <param name="fileNode">The file node</param>
    /// <param name="toRead">Number of bytes to read</param>
    /// <param name="buffer">Buffer to read into</param>
    /// <param name="read">Number of bytes actually read</param>
    /// <returns>Error number indicating success or failure</returns>
    static ErrorNumber ReadInlineData(EfsFileNode fileNode, long toRead, byte[] buffer, out long read)
    {
        read = 0;

        // Convert extent array to bytes
        var extentBytes = new byte[EFS_DIRECTEXTENTS * 8];

        for(var i = 0; i < EFS_DIRECTEXTENTS; i++)
        {
            int    offset = i * 8;
            byte[] bn     = BitConverter.GetBytes(fileNode.Inode.di_extents[i].ex_magic_bn);
            byte[] lo     = BitConverter.GetBytes(fileNode.Inode.di_extents[i].ex_length_offset);

            // Big-endian
            extentBytes[offset]     = bn[3];
            extentBytes[offset + 1] = bn[2];
            extentBytes[offset + 2] = bn[1];
            extentBytes[offset + 3] = bn[0];
            extentBytes[offset + 4] = lo[3];
            extentBytes[offset + 5] = lo[2];
            extentBytes[offset + 6] = lo[1];
            extentBytes[offset + 7] = lo[0];
        }

        // Calculate how much to copy
        long available = fileNode.Length - fileNode.Offset;

        if(available <= 0) return ErrorNumber.NoError;

        long bytesToCopy = Math.Min(toRead, available);

        if(fileNode.Offset + bytesToCopy > extentBytes.Length) bytesToCopy = extentBytes.Length - fileNode.Offset;

        if(bytesToCopy <= 0) return ErrorNumber.NoError;

        Array.Copy(extentBytes, fileNode.Offset, buffer, 0, bytesToCopy);

        read            =  bytesToCopy;
        fileNode.Offset += bytesToCopy;

        return ErrorNumber.NoError;
    }

    /// <summary>Converts an EFS inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The EFS inode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The FileEntryInfo structure</returns>
    static FileEntryInfo InodeToFileEntryInfo(Inode inode, uint inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = EFS_BBSIZE,
            Inode               = inodeNumber,
            Length              = inode.di_size,
            Links               = (ulong)inode.di_nlink,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0x0FFF), // Lower 12 bits are permissions
            AccessTimeUtc       = DateHandlers.UnixToDateTime(inode.di_atime),
            LastWriteTimeUtc    = DateHandlers.UnixToDateTime(inode.di_mtime),
            StatusChangeTimeUtc = DateHandlers.UnixToDateTime(inode.di_ctime),
            CreationTimeUtc     = DateHandlers.UnixToDateTime(inode.di_ctime)
        };

        // Calculate blocks used from extents
        long blocksUsed = 0;

        if(inode.di_extents != null)
        {
            for(var i = 0; i < inode.di_numextents && i < EFS_DIRECTEXTENTS; i++)
                if(inode.di_extents[i].Magic == 0)
                    blocksUsed += inode.di_extents[i].Length;
        }

        info.Blocks = blocksUsed;

        // Determine file type from di_mode
        var fileType = (FileType)(inode.di_mode & (ushort)FileType.IFMT);

        info.Attributes = fileType switch
                          {
                              FileType.IFDIR    => FileAttributes.Directory,
                              FileType.IFREG    => FileAttributes.File,
                              FileType.IFLNK    => FileAttributes.Symlink,
                              FileType.IFCHR    => FileAttributes.CharDevice,
                              FileType.IFBLK    => FileAttributes.BlockDevice,
                              FileType.IFCHRLNK => FileAttributes.CharDevice,
                              FileType.IFBLKLNK => FileAttributes.BlockDevice,
                              _                 => FileAttributes.File
                          };

        return info;
    }
}