// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX4
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

        // Resolve the path to get the inode entry
        errno = ResolvePath(path, out qnx4_inode_entry inode, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: ResolvePath failed with {0}", errno);

            return errno;
        }

        // Validate symlink size
        if(inode.di_size == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: symlink has zero size");

            return ErrorNumber.InvalidArgument;
        }

        // Read the symlink target from the file data blocks
        var  linkData      = new byte[inode.di_size];
        uint bytesRead     = 0;
        uint currentOffset = 0;

        while(bytesRead < inode.di_size)
        {
            uint blockNum      = currentOffset / QNX4_BLOCK_SIZE;
            var  offsetInBlock = (int)(currentOffset % QNX4_BLOCK_SIZE);

            errno = MapBlock(inode, blockNum, out uint physicalBlock);

            if(errno != ErrorNumber.NoError || physicalBlock == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadLink: MapBlock failed for block {0}", blockNum);

                return ErrorNumber.InvalidArgument;
            }

            errno = ReadBlock(physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadLink: ReadBlock failed for block {0}", physicalBlock);

                return errno;
            }

            uint bytesToCopy = Math.Min((uint)(QNX4_BLOCK_SIZE - offsetInBlock), inode.di_size - bytesRead);
            Array.Copy(blockData, offsetInBlock, linkData, bytesRead, bytesToCopy);

            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        // Convert to string, stopping at null terminator
        dest = StringHandlers.CToString(linkData, _encoding);

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

        // Resolve the path to get the inode entry
        errno = ResolvePath(path, out qnx4_inode_entry inode, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ResolvePath failed with {0}", errno);

            return errno;
        }

        node = new QNX4FileNode
        {
            Path   = path,
            Length = inode.di_size,
            Offset = 0,
            Inode  = inode
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}", inode.di_size);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not QNX4FileNode) return ErrorNumber.InvalidArgument;

        // Nothing to clean up - no caching
        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not QNX4FileNode fileNode) return ErrorNumber.InvalidArgument;

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
            // Calculate which block contains the current offset
            var blockNum      = (uint)(currentOffset / QNX4_BLOCK_SIZE);
            var offsetInBlock = (int)(currentOffset  % QNX4_BLOCK_SIZE);

            // Map logical block to physical block using extent mapping
            ErrorNumber errno = MapBlock(fileNode.Inode, blockNum, out uint physicalBlock);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: MapBlock failed for block {0}: {1}", blockNum, errno);

                // Sparse file - block not allocated, fill with zeros
                if(errno == ErrorNumber.NoSuchFile)
                {
                    long bytesToZero = Math.Min(QNX4_BLOCK_SIZE - offsetInBlock, toRead - bytesRead);
                    Array.Clear(buffer, (int)bytesRead, (int)bytesToZero);
                    bytesRead     += bytesToZero;
                    currentOffset += bytesToZero;

                    continue;
                }

                return errno;
            }

            // Block address 0 means sparse/hole
            if(physicalBlock == 0)
            {
                long bytesToZero = Math.Min(QNX4_BLOCK_SIZE - offsetInBlock, toRead - bytesRead);
                Array.Clear(buffer, (int)bytesRead, (int)bytesToZero);
                bytesRead     += bytesToZero;
                currentOffset += bytesToZero;

                continue;
            }

            // Read the block
            errno = ReadBlock(physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: ReadBlock failed for block {0}: {1}", physicalBlock, errno);

                return errno;
            }

            // Copy data from block to buffer
            long bytesToCopy = Math.Min(QNX4_BLOCK_SIZE - offsetInBlock, toRead - bytesRead);
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
            stat = InodeToFileEntryInfo(_superblock.RootDir);

            return ErrorNumber.NoError;
        }

        // Resolve the path to get the target entry
        ErrorNumber errno = ResolvePath(normalizedPath, out qnx4_inode_entry targetEntry, out _);

        if(errno != ErrorNumber.NoError) return errno;

        stat = InodeToFileEntryInfo(targetEntry);

        return ErrorNumber.NoError;
    }
}