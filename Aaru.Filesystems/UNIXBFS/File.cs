// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class BFS
{
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

        // Remove leading slash for lookup
        string filename = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        // Look up the file in the root directory cache
        if(!_rootDirectoryCache.TryGetValue(filename, out ushort inodeNum))
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: File '{0}' not found", filename);

            return ErrorNumber.NoSuchFile;
        }

        // Get the inode from cache or read it
        if(!_inodeCache.TryGetValue(inodeNum, out Inode inode))
        {
            errno = ReadInode(inodeNum, out inode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadInode failed with {0}", errno);

                return errno;
            }

            _inodeCache[inodeNum] = inode;
        }

        // Calculate file size
        long fileSize = 0;

        if(inode.i_sblock != 0) fileSize = inode.i_eoffset + 1 - inode.i_sblock * BFS_BSIZE;

        node = new BfsFileNode
        {
            Path   = path,
            Length = fileSize,
            Offset = 0,
            Inode  = inode
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}", fileSize);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not BfsFileNode) return ErrorNumber.InvalidArgument;

        // Nothing to clean up - no caching
        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not BfsFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        // BFS files are stored contiguously: physical block = i_sblock + logical block
        // File data starts at byte offset (i_sblock * BFS_BSIZE) and ends at i_eoffset

        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate which block contains the current offset
            var blockNum      = (uint)(currentOffset / BFS_BSIZE);
            var offsetInBlock = (int)(currentOffset  % BFS_BSIZE);

            // BFS uses contiguous allocation: physical = i_sblock + logical
            uint physicalBlock = fileNode.Inode.i_sblock + blockNum;

            // Check if we're within the allocated blocks
            if(physicalBlock > fileNode.Inode.i_eblock)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadFile: Block {0} is past end block {1}",
                                  physicalBlock,
                                  fileNode.Inode.i_eblock);

                break;
            }

            // Read the block
            ErrorNumber errno = ReadBlock(physicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: ReadBlock failed for block {0}: {1}", physicalBlock, errno);

                return errno;
            }

            // Copy data from block to buffer
            long bytesToCopy = Math.Min(BFS_BSIZE - offsetInBlock, toRead - bytesRead);
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
            // Read root inode
            ErrorNumber errno = ReadInode(BFS_ROOT_INO, out Inode rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInode);

            return ErrorNumber.NoError;
        }

        // Remove leading slash for lookup
        string filename = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        // BFS has no subdirectories, so any path with slashes (after removing leading) is invalid
        if(filename.Contains('/'))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: BFS does not support subdirectories");

            return ErrorNumber.NoSuchFile;
        }

        // Look up the file in the root directory cache
        if(!_rootDirectoryCache.TryGetValue(filename, out ushort inodeNum))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: File '{0}' not found", filename);

            return ErrorNumber.NoSuchFile;
        }

        // Get the inode from cache or read it
        if(!_inodeCache.TryGetValue(inodeNum, out Inode inode))
        {
            ErrorNumber errno = ReadInode(inodeNum, out inode);

            if(errno != ErrorNumber.NoError) return errno;

            _inodeCache[inodeNum] = inode;
        }

        stat = InodeToFileEntryInfo(inode);

        return ErrorNumber.NoError;
    }
}