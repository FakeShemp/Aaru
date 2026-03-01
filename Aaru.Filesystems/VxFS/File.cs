// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Veritas File System plugin.
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
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = LookupInode(path, out DiskInode inode, out uint inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        stat = InodeToFileEntryInfo(inode, inodeNumber);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = LookupInode(path, out DiskInode inode, out uint _);

        if(errno != ErrorNumber.NoError) return errno;

        var fileType = (VxfsFileType)(inode.vdi_mode & VXFS_TYPE_MASK);

        if(fileType != VxfsFileType.Lnk) return ErrorNumber.InvalidArgument;

        byte[] linkData = ReadInodeData(inode);

        if(linkData == null) return ErrorNumber.InvalidArgument;

        dest = Encoding.UTF8.GetString(linkData).TrimEnd('\0');

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        ErrorNumber errno = LookupInode(path, out DiskInode inode, out uint inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        var fileType = (VxfsFileType)(inode.vdi_mode & VXFS_TYPE_MASK);

        if(fileType == VxfsFileType.Dir) return ErrorNumber.IsDirectory;

        node = new VxFsFileNode
        {
            Path        = path,
            Length      = (long)inode.vdi_size,
            Offset      = 0,
            InodeNumber = inodeNumber,
            Inode       = inode
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not VxFsFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not VxFsFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        int  blockSize     = _superblock.vs_bsize;
        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            var blockNum      = (uint)(currentOffset / blockSize);
            var offsetInBlock = (int)(currentOffset  % blockSize);

            byte[] blockData = ReadInodeBlock(fileNode.Inode, blockNum);

            if(blockData == null)
            {
                // Sparse block / hole — fill with zeros
                long bytesToZero = Math.Min(blockSize - offsetInBlock, toRead - bytesRead);
                Array.Clear(buffer, (int)bytesRead, (int)bytesToZero);
                bytesRead     += bytesToZero;
                currentOffset += bytesToZero;

                continue;
            }

            long bytesToCopy = Math.Min(blockSize - offsetInBlock, toRead - bytesRead);
            Array.Copy(blockData, offsetInBlock, buffer, bytesRead, bytesToCopy);
            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a path to its inode</summary>
    /// <param name="path">Path to resolve</param>
    /// <param name="inode">Output inode</param>
    /// <param name="inodeNumber">Output inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupInode(string path, out DiskInode inode, out uint inodeNumber)
    {
        inode       = default(DiskInode);
        inodeNumber = 0;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath is "/")
        {
            // Root inode
            ErrorNumber errno = ReadInodeFromInode(_ilistInode, VXFS_ROOT_INO, out inode);
            inodeNumber = VXFS_ROOT_INO;

            return errno;
        }

        string stripped = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        string[] components = stripped.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(components.Length == 0) return ErrorNumber.InvalidArgument;

        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < components.Length; i++)
        {
            string component = components[i];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out inodeNumber)) return ErrorNumber.NoSuchFile;

            ErrorNumber errno = ReadInodeFromInode(_ilistInode, inodeNumber, out inode);

            if(errno != ErrorNumber.NoError) return errno;

            // Last component — we found it
            if(i == components.Length - 1) return ErrorNumber.NoError;

            // Intermediate component — must be a directory
            if((VxfsFileType)(inode.vdi_mode & VXFS_TYPE_MASK) != VxfsFileType.Dir) return ErrorNumber.NotDirectory;

            errno = ReadDirectoryEntries(inode, out Dictionary<string, uint> subEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = subEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts a VxFS disk inode to a <see cref="FileEntryInfo" /></summary>
    /// <param name="inode">VxFS disk inode</param>
    /// <param name="inodeNumber">Inode number</param>
    /// <returns>File entry info</returns>
    static FileEntryInfo InodeToFileEntryInfo(DiskInode inode, uint inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Inode            = inodeNumber,
            Mode             = inode.vdi_mode & 0xFFF,
            Links            = inode.vdi_nlink,
            UID              = inode.vdi_uid,
            GID              = inode.vdi_gid,
            Length           = (long)inode.vdi_size,
            Blocks           = inode.vdi_blocks,
            Attributes       = new FileAttributes(),
            AccessTimeUtc    = DateHandlers.UnixToDateTime(inode.vdi_atime),
            LastWriteTimeUtc = DateHandlers.UnixToDateTime(inode.vdi_mtime),
            CreationTimeUtc  = DateHandlers.UnixToDateTime(inode.vdi_ctime)
        };

        var fileType = (VxfsFileType)(inode.vdi_mode & VXFS_TYPE_MASK);

        switch(fileType)
        {
            case VxfsFileType.Dir:
                info.Attributes = FileAttributes.Directory;

                break;
            case VxfsFileType.Lnk:
                info.Attributes = FileAttributes.Symlink;

                break;
            case VxfsFileType.Chr:
                info.Attributes = FileAttributes.Device | FileAttributes.CharDevice;
                info.DeviceNo   = inode.vdi_ftarea;

                break;
            case VxfsFileType.Blk:
                info.Attributes = FileAttributes.Device | FileAttributes.BlockDevice;
                info.DeviceNo   = inode.vdi_ftarea;

                break;
            case VxfsFileType.Fifo:
                info.Attributes = FileAttributes.Pipe;

                break;
            case VxfsFileType.Soc:
                info.Attributes = FileAttributes.Socket;

                break;
            case VxfsFileType.Reg:
            default:
                info.Attributes = FileAttributes.File;

                break;
        }

        return info;
    }
}