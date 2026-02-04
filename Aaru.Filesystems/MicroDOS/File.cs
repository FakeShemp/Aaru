// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MicroDOS filesystem plugin
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
public sealed partial class MicroDOS
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
        if(!_rootDirectoryCache.TryGetValue(filename, out DirectoryEntry entry))
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: File '{0}' not found", filename);

            return ErrorNumber.NoSuchFile;
        }

        node = new MicroDosFileNode
        {
            Path   = path,
            Length = entry.length,
            Offset = 0,
            Entry  = entry
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}", entry.length);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not MicroDosFileNode) return ErrorNumber.InvalidArgument;

        // Nothing to clean up - no caching
        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not MicroDosFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        // MicroDOS files are stored contiguously starting at blockNo
        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate which block contains the current offset
            var blockNum      = (uint)(currentOffset / BLOCK_SIZE);
            var offsetInBlock = (int)(currentOffset  % BLOCK_SIZE);

            // Physical block = starting block + logical block number
            uint physicalBlock = (uint)(fileNode.Entry.blockNo + blockNum);

            // Read the block
            ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start + physicalBlock,
                                                        false,
                                                        out byte[] blockData,
                                                        out _);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: ReadSector failed for block {0}: {1}", physicalBlock, errno);

                return errno;
            }

            // Copy data from block to buffer
            long bytesToCopy = Math.Min(BLOCK_SIZE - offsetInBlock, toRead - bytesRead);

            if(offsetInBlock + bytesToCopy > blockData.Length)
                bytesToCopy = blockData.Length - offsetInBlock;

            if(bytesToCopy <= 0)
                break;

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
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        attributes = stat.Attributes;

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
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = BLOCK_SIZE,
                Blocks     = 1,
                Length     = BLOCK_SIZE
            };

            return ErrorNumber.NoError;
        }

        // Remove leading slash for lookup
        string filename = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        // MicroDOS has no subdirectories, so any path with slashes (after removing leading) is invalid
        if(filename.Contains('/'))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: MicroDOS does not support subdirectories");

            return ErrorNumber.NoSuchFile;
        }

        // Look up the file in the root directory cache
        if(!_rootDirectoryCache.TryGetValue(filename, out DirectoryEntry entry))
        {
            AaruLogging.Debug(MODULE_NAME, "Stat: File '{0}' not found", filename);

            return ErrorNumber.NoSuchFile;
        }

        stat = EntryToFileEntryInfo(entry);

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a MicroDOS directory entry to a FileEntryInfo structure</summary>
    /// <param name="entry">The MicroDOS directory entry</param>
    /// <returns>The FileEntryInfo structure</returns>
    static FileEntryInfo EntryToFileEntryInfo(DirectoryEntry entry)
    {
        var info = new FileEntryInfo
        {
            Attributes = FileAttributes.None,
            BlockSize  = BLOCK_SIZE,
            Blocks     = entry.blocks,
            Length     = entry.length
        };

        // Determine attributes based on status
        switch(entry.status)
        {
            case (byte)FileStatus.Protected:
                info.Attributes = FileAttributes.File | FileAttributes.ReadOnly;

                break;
            case (byte)FileStatus.LogicalDisk:
                info.Attributes = FileAttributes.File | FileAttributes.System;

                break;
            default:
                info.Attributes = FileAttributes.File;

                break;
        }

        return info;
    }
}