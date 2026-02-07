// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class HPFS
{
    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Cannot open root directory as a file
        if(normalizedPath == "/") return ErrorNumber.IsDirectory;

        // Get the directory entry for this path
        ErrorNumber errno = GetDirectoryEntry(normalizedPath, out DirectoryEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        // Cannot open directories as files
        if(entry.attributes.HasFlag(DosAttributes.Directory)) return ErrorNumber.IsDirectory;

        // Read the fnode
        errno = ReadFNode(entry.fnode, out FNode fnodeData);

        if(errno != ErrorNumber.NoError) return errno;

        node = new HpfsFileNode
        {
            Path      = normalizedPath,
            Fnode     = entry.fnode,
            Offset    = 0,
            Length    = entry.file_size,
            FnodeData = fnodeData
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not HpfsFileNode myNode) return ErrorNumber.InvalidArgument;

        // Clear references
        myNode.FnodeData = default(FNode);
        myNode.Offset    = -1;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(buffer is null || buffer.Length < length) return ErrorNumber.InvalidArgument;

        if(node is not HpfsFileNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Offset < 0) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size
        if(myNode.Offset + length > myNode.Length) length = myNode.Length - myNode.Offset;

        if(length <= 0) return ErrorNumber.NoError;

        var bufferPos = 0;

        while(length > 0)
        {
            // Calculate which file sector we need (512-byte sectors)
            var fileSector     = (uint)(myNode.Offset / 512);
            var offsetInSector = (int)(myNode.Offset  % 512);

            // Look up the disk sector for this file sector using B+ tree
            ErrorNumber errno = BPlusLookup(myNode.FnodeData.btree,
                                            myNode.FnodeData.btree_data,
                                            fileSector,
                                            out uint diskSector,
                                            out uint runLength);

            if(errno != ErrorNumber.NoError) return errno;

            if(diskSector == 0) return ErrorNumber.InvalidArgument;

            // Calculate how many consecutive sectors we can read
            uint sectorsToRead = Math.Min(runLength - fileSector % runLength,
                                          (uint)((length + offsetInSector + 511) / 512));

            // Read the sector(s)
            errno = _image.ReadSectors(_partition.Start + diskSector,
                                       false,
                                       sectorsToRead,
                                       out byte[] sectorData,
                                       out _);

            if(errno != ErrorNumber.NoError) return errno;

            // Calculate how much data to copy
            var bytesAvailable = (int)(sectorsToRead * 512 - offsetInSector);
            var bytesToCopy    = (int)Math.Min(bytesAvailable, length);

            Array.Copy(sectorData, offsetInSector, buffer, bufferPos, bytesToCopy);

            bufferPos     += bytesToCopy;
            myNode.Offset += bytesToCopy;
            length        -= bytesToCopy;
            read          += bytesToCopy;
        }

        return ErrorNumber.NoError;
    }


    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Root directory case
        if(normalizedPath == "/")
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                Blocks     = 4, // Root dnode is 4 sectors
                BlockSize  = _bytesPerSector,
                Length     = 4 * _bytesPerSector,
                Inode      = _rootFnode,
                Links      = 1
            };

            return ErrorNumber.NoError;
        }

        // Find the directory entry for this path
        ErrorNumber errno = GetDirectoryEntry(normalizedPath, out DirectoryEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        stat = new FileEntryInfo
        {
            Attributes = new FileAttributes(),
            BlockSize  = _bytesPerSector,
            Length     = entry.file_size,
            Inode      = entry.fnode,
            Links      = 1
        };

        // Convert timestamps (seconds since 1970)
        if(entry.creation_date > 0) stat.CreationTime = DateHandlers.UnixToDateTime(entry.creation_date);

        if(entry.write_date > 0) stat.LastWriteTime = DateHandlers.UnixToDateTime(entry.write_date);

        if(entry.read_date > 0) stat.AccessTime = DateHandlers.UnixToDateTime(entry.read_date);

        // Calculate blocks - need to read fnode for accurate count
        ErrorNumber fnodeErr = ReadFNode(entry.fnode, out FNode fnode);

        if(fnodeErr == ErrorNumber.NoError)
        {
            // Get extent information to calculate actual blocks used
            BPlusLeafNode[] leafNodes = GetBPlusLeafNodes(fnode.btree, fnode.btree_data);
            uint            blocks    = 0;

            foreach(BPlusLeafNode leaf in leafNodes) blocks += leaf.length;

            stat.Blocks = blocks;
        }
        else
        {
            // Fall back to estimated blocks
            stat.Blocks = (entry.file_size + _bytesPerSector - 1) / _bytesPerSector;
        }

        // Map DOS attributes to FileAttributes
        if(entry.attributes.HasFlag(DosAttributes.Directory))
            stat.Attributes |= FileAttributes.Directory;
        else
            stat.Attributes |= FileAttributes.File;

        if(entry.attributes.HasFlag(DosAttributes.ReadOnly)) stat.Attributes |= FileAttributes.ReadOnly;

        if(entry.attributes.HasFlag(DosAttributes.Hidden)) stat.Attributes |= FileAttributes.Hidden;

        if(entry.attributes.HasFlag(DosAttributes.System)) stat.Attributes |= FileAttributes.System;

        if(entry.attributes.HasFlag(DosAttributes.Archive)) stat.Attributes |= FileAttributes.Archive;

        return ErrorNumber.NoError;
    }
}