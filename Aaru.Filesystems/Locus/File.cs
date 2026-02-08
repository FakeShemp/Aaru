// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Locus
{
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
        errno = LookupFile(path, out int inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        // Read the inode
        errno = ReadInode(inodeNumber, out Dinode inode);

        if(errno != ErrorNumber.NoError) return errno;

        // Check for inline smallblock data
        if(_smallBlocks && _smallBlockDataCache.TryGetValue(inodeNumber, out byte[] inlineData))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: Using inline smallblock data");

            int linkLen = Math.Min(inode.di_size, inlineData.Length);
            dest = _encoding.GetString(inlineData, 0, linkLen).TrimEnd('\0');

            return ErrorNumber.NoError;
        }

        // Read symlink data from disk blocks
        errno = ReadFileData(inodeNumber, inode, out byte[] linkData);

        if(errno != ErrorNumber.NoError) return errno;

        dest = _encoding.GetString(linkData).TrimEnd('\0');

        return ErrorNumber.NoError;
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
        errno = LookupFile(path, out int inodeNumber);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LookupFile failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInode(inodeNumber, out Dinode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadInode failed with {0}", errno);

            return errno;
        }

        // Check if file uses inline smallblock data
        bool hasInlineData = _smallBlocks && _smallBlockDataCache.ContainsKey(inodeNumber);

        node = new LocusFileNode
        {
            Path          = path,
            Length        = inode.di_size,
            Offset        = 0,
            InodeNumber   = inodeNumber,
            Inode         = inode,
            HasInlineData = hasInlineData
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}, hasInlineData={1}", inode.di_size, hasInlineData);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not LocusFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not LocusFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        // Check for inline smallblock data
        if(fileNode.HasInlineData && _smallBlockDataCache.TryGetValue(fileNode.InodeNumber, out byte[] inlineData))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFile: Using inline smallblock data");

            // Copy from inline data
            var copySize = (int)Math.Min(toRead, inlineData.Length - fileNode.Offset);

            if(copySize > 0)
            {
                Array.Copy(inlineData, fileNode.Offset, buffer, 0, copySize);
                read            =  copySize;
                fileNode.Offset += copySize;
            }

            return ErrorNumber.NoError;
        }

        // Normal file: read from disk blocks
        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate which block contains the current offset
            var logicalBlock  = (int)(currentOffset / _blockSize);
            var offsetInBlock = (int)(currentOffset % _blockSize);

            // Get the physical block number
            ErrorNumber errno = GetPhysicalBlock(fileNode.Inode, logicalBlock, out int physicalBlock);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "ReadFile: GetPhysicalBlock failed for logical block {0}: {1}",
                                  logicalBlock,
                                  errno);

                // If we've read some data, return what we have
                if(bytesRead > 0) break;

                return errno;
            }

            byte[] blockData;

            if(physicalBlock == 0)
            {
                // Sparse file hole - return zeros
                blockData = new byte[_blockSize];
            }
            else
            {
                // Read the block
                errno = ReadBlock(physicalBlock, out blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadFile: ReadBlock failed for block {0}: {1}",
                                      physicalBlock,
                                      errno);

                    // If we've read some data, return what we have
                    if(bytesRead > 0) break;

                    return errno;
                }
            }

            // Calculate how much data to copy from this block
            long bytesToCopy = Math.Min(_blockSize - offsetInBlock, toRead - bytesRead);
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
            ErrorNumber errno = ReadInode(ROOT_INO, out Dinode rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInode, ROOT_INO);

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, int> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out int inodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Read the inode
            ErrorNumber errno = ReadInode(inodeNumber, out Dinode inode);

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
            var fileType = (FileMode)(inode.di_mode & (ushort)FileMode.IFMT);

            if(fileType != FileMode.IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents for next iteration
            errno = ReadDirectoryContents(inodeNumber, inode, out Dictionary<string, int> dirEntries);

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
    ErrorNumber LookupFile(string path, out int inodeNumber)
    {
        inodeNumber = 0;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath == "/")
        {
            inodeNumber = ROOT_INO;

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, int> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out int foundInodeNumber)) return ErrorNumber.NoSuchFile;

            // If this is the last component, we found it
            if(p == pathComponents.Length - 1)
            {
                inodeNumber = foundInodeNumber;

                return ErrorNumber.NoError;
            }

            // Not the last component - read directory contents for next iteration
            ErrorNumber errno = ReadInode(foundInodeNumber, out Dinode inode);

            if(errno != ErrorNumber.NoError) return errno;

            var fileType = (FileMode)(inode.di_mode & (ushort)FileMode.IFMT);

            if(fileType != FileMode.IFDIR) return ErrorNumber.NotDirectory;

            errno = ReadDirectoryContents(foundInodeNumber, inode, out Dictionary<string, int> dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }


    /// <summary>Reads all data from a file inode</summary>
    /// <param name="inodeNumber">Inode number</param>
    /// <param name="inode">File inode</param>
    /// <param name="data">The file data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFileData(int inodeNumber, Dinode inode, out byte[] data)
    {
        data = null;

        if(inode.di_size <= 0)
        {
            data = [];

            return ErrorNumber.NoError;
        }

        AaruLogging.Debug(MODULE_NAME, "ReadFileData: Reading {0} bytes for inode {1}", inode.di_size, inodeNumber);

        // Check for smallblock inline data first
        if(_smallBlocks && _smallBlockDataCache.TryGetValue(inodeNumber, out byte[] inlineData))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFileData: Using inline smallblock data");

            // Copy inline data up to file size
            int copySize = Math.Min(inode.di_size, inlineData.Length);
            data = new byte[inode.di_size];
            Array.Copy(inlineData, 0, data, 0, copySize);

            return ErrorNumber.NoError;
        }

        data = new byte[inode.di_size];
        var bytesRead = 0;

        // Check if di_addr is valid
        if(inode.di_addr == null || inode.di_addr.Length == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFileData: di_addr is null or empty!");

            return ErrorNumber.InvalidArgument;
        }

        // Direct blocks (first NDADDR = 10 blocks)
        for(var i = 0; i < NDADDR && bytesRead < inode.di_size; i++)
        {
            int blockNum = inode.di_addr[i];

            AaruLogging.Debug(MODULE_NAME, "ReadFileData: Direct block[{0}] = {1}", i, blockNum);

            if(blockNum == 0)
            {
                // Sparse file - fill with zeros
                int toFill = Math.Min(_blockSize, inode.di_size - bytesRead);
                bytesRead += toFill;

                AaruLogging.Debug(MODULE_NAME, "ReadFileData: Sparse block, filled {0} zeros", toFill);

                continue;
            }

            ErrorNumber errno = ReadBlock(blockNum, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading direct block {0}: {1}", blockNum, errno);

                return errno;
            }

            int toCopy = Math.Min(blockData.Length, inode.di_size - bytesRead);
            Array.Copy(blockData, 0, data, bytesRead, toCopy);
            bytesRead += toCopy;
        }

        if(bytesRead >= inode.di_size) return ErrorNumber.NoError;

        // Single indirect block
        if(inode.di_addr[NDADDR] != 0)
        {
            ErrorNumber errno = ReadIndirectBlock(inode.di_addr[NDADDR], 1, ref data, ref bytesRead, inode.di_size);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(bytesRead >= inode.di_size) return ErrorNumber.NoError;

        // Double indirect block
        if(inode.di_addr[NDADDR + 1] != 0)
        {
            ErrorNumber errno = ReadIndirectBlock(inode.di_addr[NDADDR + 1], 2, ref data, ref bytesRead, inode.di_size);

            if(errno != ErrorNumber.NoError) return errno;
        }

        if(bytesRead >= inode.di_size) return ErrorNumber.NoError;

        // Triple indirect block
        if(inode.di_addr[NDADDR + 2] != 0)
        {
            ErrorNumber errno = ReadIndirectBlock(inode.di_addr[NDADDR + 2], 3, ref data, ref bytesRead, inode.di_size);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }
}