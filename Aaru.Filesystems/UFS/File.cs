// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX FIle System plugin.
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
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class UFSPlugin
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = ResolvePath(path, out uint inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        if(_superBlock.fs_isUfs2)
        {
            errno = ReadInode2(inodeNumber, out Inode2 inode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = Inode2ToFileEntryInfo(inode, inodeNumber);
        }
        else
        {
            errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(inode, inodeNumber);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a UFS1 inode to a FileEntryInfo</summary>
    FileEntryInfo InodeToFileEntryInfo(Inode inode, uint inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = ModeToAttributes(inode.di_mode),
            BlockSize           = _superBlock.fs_bsize,
            Blocks              = inode.di_blocks,
            Inode               = inodeNumber,
            Length              = (long)inode.di_size,
            Links               = (ushort)inode.di_nlink,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0xFFF),
            AccessTimeUtc       = DateHandlers.UnixToDateTime(inode.di_atime),
            LastWriteTimeUtc    = DateHandlers.UnixToDateTime(inode.di_mtime),
            StatusChangeTimeUtc = DateHandlers.UnixToDateTime(inode.di_ctime)
        };

        // Block and character devices store dev_t in di_db[0]
        if((inode.di_mode & 0xF000) is 0x2000 or 0x6000 && inode.di_db?.Length > 0)
            info.DeviceNo = (uint)inode.di_db[0];

        return info;
    }

    /// <summary>Converts a UFS2 inode to a FileEntryInfo</summary>
    FileEntryInfo Inode2ToFileEntryInfo(Inode2 inode, uint inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = ModeToAttributes(inode.di_mode),
            BlockSize           = inode.di_blksize > 0 ? inode.di_blksize : _superBlock.fs_bsize,
            Blocks              = (long)inode.di_blocks,
            Inode               = inodeNumber,
            Length              = (long)inode.di_size,
            Links               = inode.di_nlink,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0xFFF),
            AccessTimeUtc       = DateHandlers.UnixToDateTime(inode.di_atime),
            LastWriteTimeUtc    = DateHandlers.UnixToDateTime(inode.di_mtime),
            StatusChangeTimeUtc = DateHandlers.UnixToDateTime(inode.di_ctime),
            CreationTimeUtc     = DateHandlers.UnixToDateTime(inode.di_birthtime)
        };

        // Block and character devices store dev_t in di_db[0]
        if((inode.di_mode & 0xF000) is 0x2000 or 0x6000 && inode.di_db?.Length > 0)
            info.DeviceNo = (ulong)inode.di_db[0];

        return info;
    }

    /// <summary>Maps UNIX inode mode to Aaru FileAttributes</summary>
    static FileAttributes ModeToAttributes(ushort mode)
    {
        FileAttributes attrs = FileAttributes.None;

        switch(mode & 0xF000)
        {
            case 0x1000: // FIFO
                attrs = FileAttributes.FIFO;

                break;
            case 0x2000: // Character device
                attrs = FileAttributes.CharDevice | FileAttributes.Device;

                break;
            case 0x4000: // Directory
                attrs = FileAttributes.Directory;

                break;
            case 0x6000: // Block device
                attrs = FileAttributes.BlockDevice | FileAttributes.Device;

                break;
            case 0x8000: // Regular file
                attrs = FileAttributes.File;

                break;
            case 0xA000: // Symbolic link
                attrs = FileAttributes.Symlink;

                break;
            case 0xC000: // Socket
                attrs = FileAttributes.Socket;

                break;
            case 0xE000: // Whiteout
                break;
        }

        return attrs;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = ResolvePath(path, out uint inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        ulong  fileSize;
        long[] directBlocks;
        long[] indirectBlocks;
        ushort mode;

        if(_superBlock.fs_isUfs2)
        {
            errno = ReadInode2(inodeNumber, out Inode2 inode2);

            if(errno != ErrorNumber.NoError) return errno;

            mode           = inode2.di_mode;
            fileSize       = inode2.di_size;
            directBlocks   = inode2.di_db;
            indirectBlocks = inode2.di_ib;
        }
        else
        {
            errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            mode     = inode.di_mode;
            fileSize = inode.di_size;

            directBlocks = new long[NDADDR];

            for(var i = 0; i < NDADDR; i++) directBlocks[i] = inode.di_db[i];

            indirectBlocks = new long[NIADDR];

            for(var i = 0; i < NIADDR; i++) indirectBlocks[i] = inode.di_ib[i];
        }

        // Must be a regular file
        if((mode & 0xF000) != 0x8000) return ErrorNumber.IsDirectory;

        errno = GetBlockList(directBlocks, indirectBlocks, fileSize, out List<long> blockList);

        if(errno != ErrorNumber.NoError) return errno;

        node = new UfsFileNode
        {
            Path        = path,
            Length      = (long)fileSize,
            Offset      = 0,
            InodeNumber = inodeNumber,
            BlockList   = blockList
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not UfsFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not UfsFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(length <= 0 || buffer is null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset >= fileNode.Length) return ErrorNumber.NoError;

        long remaining = fileNode.Length - fileNode.Offset;

        if(length > remaining) length = remaining;

        int blockSize = _superBlock.fs_bsize;

        while(read < length)
        {
            var  logicalBlock  = (int)(fileNode.Offset / blockSize);
            var  offsetInBlock = (int)(fileNode.Offset % blockSize);
            long toRead        = Math.Min(length - read, blockSize - offsetInBlock);

            if(logicalBlock >= fileNode.BlockList.Count) break;

            long fragAddr = fileNode.BlockList[logicalBlock];

            if(fragAddr == 0)
            {
                // Sparse block — zeros already in buffer from allocation
                Array.Clear(buffer, (int)read, (int)toRead);
            }
            else
            {
                ErrorNumber errno = ReadFragments(fragAddr, _superBlock.fs_frag, out byte[] blockData);

                if(errno != ErrorNumber.NoError) return errno;

                var copyLen = (int)Math.Min(toRead, blockData.Length - offsetInBlock);

                if(copyLen > 0) Array.Copy(blockData, offsetInBlock, buffer, read, copyLen);
            }

            read            += toRead;
            fileNode.Offset += toRead;
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = ResolvePath(path, out uint inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        if(_superBlock.fs_isUfs2)
        {
            errno = ReadInode2(inodeNumber, out Inode2 inode);

            if(errno != ErrorNumber.NoError) return errno;

            // Must be a symlink
            if((inode.di_mode & 0xF000) != 0xA000) return ErrorNumber.InvalidArgument;

            // Fast symlink: target stored inline in di_db/di_ib area
            if((long)inode.di_size <= _superBlock.fs_maxsymlinklen && _superBlock.fs_maxsymlinklen > 0)
            {
                // di_db (12 * 8 = 96 bytes) + di_ib (3 * 8 = 24 bytes) = 120 bytes
                var linkData = new byte[inode.di_size];
                var offset   = 0;

                for(var i = 0; i < NDADDR && offset < linkData.Length; i++)
                {
                    byte[] blockBytes = BitConverter.GetBytes(inode.di_db[i]);
                    int    toCopy     = Math.Min(blockBytes.Length, linkData.Length - offset);
                    Array.Copy(blockBytes, 0, linkData, offset, toCopy);
                    offset += toCopy;
                }

                for(var i = 0; i < NIADDR && offset < linkData.Length; i++)
                {
                    byte[] blockBytes = BitConverter.GetBytes(inode.di_ib[i]);
                    int    toCopy     = Math.Min(blockBytes.Length, linkData.Length - offset);
                    Array.Copy(blockBytes, 0, linkData, offset, toCopy);
                    offset += toCopy;
                }

                dest = _encoding.GetString(linkData).TrimEnd('\0');

                return ErrorNumber.NoError;
            }

            // Slow symlink: read from data blocks
            errno = ReadInodeData(inodeNumber, 0, (long)inode.di_size, out byte[] data);

            if(errno != ErrorNumber.NoError) return errno;

            dest = _encoding.GetString(data).TrimEnd('\0');

            return ErrorNumber.NoError;
        }
        else
        {
            errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            if((inode.di_mode & 0xF000) != 0xA000) return ErrorNumber.InvalidArgument;

            // Fast symlink: target stored inline in di_db/di_ib area
            if((long)inode.di_size <= _superBlock.fs_maxsymlinklen && _superBlock.fs_maxsymlinklen > 0)
            {
                // di_db (12 * 4 = 48 bytes) + di_ib (3 * 4 = 12 bytes) = 60 bytes
                var linkData = new byte[inode.di_size];
                var offset   = 0;

                for(var i = 0; i < NDADDR && offset < linkData.Length; i++)
                {
                    byte[] blockBytes = BitConverter.GetBytes(inode.di_db[i]);
                    int    toCopy     = Math.Min(blockBytes.Length, linkData.Length - offset);
                    Array.Copy(blockBytes, 0, linkData, offset, toCopy);
                    offset += toCopy;
                }

                for(var i = 0; i < NIADDR && offset < linkData.Length; i++)
                {
                    byte[] blockBytes = BitConverter.GetBytes(inode.di_ib[i]);
                    int    toCopy     = Math.Min(blockBytes.Length, linkData.Length - offset);
                    Array.Copy(blockBytes, 0, linkData, offset, toCopy);
                    offset += toCopy;
                }

                dest = _encoding.GetString(linkData).TrimEnd('\0');

                return ErrorNumber.NoError;
            }

            // Slow symlink: read from data blocks
            errno = ReadInodeData(inodeNumber, 0, (long)inode.di_size, out byte[] data);

            if(errno != ErrorNumber.NoError) return errno;

            dest = _encoding.GetString(data).TrimEnd('\0');

            return ErrorNumber.NoError;
        }
    }
}