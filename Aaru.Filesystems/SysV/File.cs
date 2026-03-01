// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
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

public sealed partial class SysVfs
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or "." or "./") normalizedPath = "/";

        if(!normalizedPath.StartsWith('/')) normalizedPath = "/" + normalizedPath;

        if(normalizedPath.Length > 1 && normalizedPath.EndsWith('/')) normalizedPath = normalizedPath[..^1];

        ushort inodeNumber;

        if(normalizedPath == "/")
            inodeNumber = SYSV_ROOT_INO;
        else
        {
            ErrorNumber errno = ResolvePath(normalizedPath, out inodeNumber);

            if(errno != ErrorNumber.NoError) return errno;
        }

        ErrorNumber readErrno = ReadInode(inodeNumber, out Inode inode);

        if(readErrno != ErrorNumber.NoError) return readErrno;

        stat = InodeToFileEntryInfo(inode, inodeNumber);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or "." or "./") normalizedPath = "/";

        if(!normalizedPath.StartsWith('/')) normalizedPath = "/" + normalizedPath;

        if(normalizedPath.Length > 1 && normalizedPath.EndsWith('/')) normalizedPath = normalizedPath[..^1];

        ushort inodeNumber;

        if(normalizedPath == "/")
            inodeNumber = SYSV_ROOT_INO;
        else
        {
            ErrorNumber errno = ResolvePath(normalizedPath, out inodeNumber);

            if(errno != ErrorNumber.NoError) return errno;
        }

        ErrorNumber readErrno = ReadInode(inodeNumber, out Inode inode);

        if(readErrno != ErrorNumber.NoError) return readErrno;

        // Only regular files and symlinks can be opened for reading
        var fileType = (ushort)(inode.di_mode & S_IFMT);

        if(fileType == S_IFDIR) return ErrorNumber.IsDirectory;

        node = new SysVFileNode
        {
            Path        = normalizedPath,
            Length      = inode.di_size,
            Offset      = 0,
            InodeNumber = inodeNumber,
            DiAddr      = inode.di_addr
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not SysVFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not SysVFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate logical block number within the file
            var logicalBlock = (int)(currentOffset / _blockSize);

            // Map logical block to physical block
            ErrorNumber errno = MapFileBlock(fileNode.DiAddr, logicalBlock, out uint physicalBlock);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadFile: MapFileBlock failed for block {0}: {1}", logicalBlock, errno);

                if(bytesRead > 0) break;

                return errno;
            }

            // Calculate offset within the block
            var offsetInBlock = (int)(currentOffset % _blockSize);

            byte[] blockData;

            if(physicalBlock == 0)
            {
                // Sparse file hole — return zeros
                blockData = new byte[_blockSize];
            }
            else
            {
                errno = ReadBlock(physicalBlock, out blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadFile: ReadBlock failed for block {0}: {1}",
                                      physicalBlock,
                                      errno);

                    if(bytesRead > 0) break;

                    return errno;
                }
            }

            // Copy data from this block
            long bytesToCopy = Math.Min(_blockSize - offsetInBlock, toRead - bytesRead);
            Array.Copy(blockData, offsetInBlock, buffer, bytesRead, bytesToCopy);

            bytesRead     += bytesToCopy;
            currentOffset += bytesToCopy;
        }

        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a SysV inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The inode to convert</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The FileEntryInfo structure</returns>
    FileEntryInfo InodeToFileEntryInfo(Inode inode, ushort inodeNumber)
    {
        FileEntryInfo info = new()
        {
            BlockSize        = _blockSize,
            Inode            = inodeNumber,
            Links            = (ulong)inode.di_nlink,
            UID              = (ushort)inode.di_uid,
            GID              = (ushort)inode.di_gid,
            Mode             = inode.di_mode,
            Length           = inode.di_size,
            Blocks           = (inode.di_size + _blockSize - 1) / _blockSize,
            Attributes       = ModeToAttributes(inode.di_mode),
            AccessTime       = DateTimeOffset.FromUnixTimeSeconds(inode.di_atime).DateTime,
            LastWriteTime    = DateTimeOffset.FromUnixTimeSeconds(inode.di_mtime).DateTime,
            StatusChangeTime = DateTimeOffset.FromUnixTimeSeconds(inode.di_ctime).DateTime
        };

        return info;
    }

    /// <summary>Converts UNIX mode bits to FileAttributes</summary>
    /// <param name="mode">The UNIX mode bits</param>
    /// <returns>The corresponding FileAttributes</returns>
    static FileAttributes ModeToAttributes(ushort mode)
    {
        FileAttributes attrs = new();

        switch(mode & S_IFMT)
        {
            case S_IFDIR:
                attrs = FileAttributes.Directory;

                break;
            case 0x8000: // S_IFREG
                break;
            case 0x6000: // S_IFBLK
                attrs = FileAttributes.BlockDevice;

                break;
            case 0x2000: // S_IFCHR
                attrs = FileAttributes.CharDevice;

                break;
            case 0x1000: // S_IFIFO
                attrs = FileAttributes.Pipe;

                break;
            case 0xA000: // S_IFLNK
                attrs = FileAttributes.Symlink;

                break;
            case 0xC000: // S_IFSOCK
                attrs = FileAttributes.Socket;

                break;
        }

        return attrs;
    }

    /// <summary>
    ///     Maps a logical file block number to a physical disk block number by walking
    ///     the inode's direct, single-indirect, double-indirect, and triple-indirect block pointers.
    /// </summary>
    /// <param name="diAddr">The 39-byte di_addr array from the inode</param>
    /// <param name="logicalBlock">The zero-based logical block within the file</param>
    /// <param name="physicalBlock">The resulting physical block number (0 for holes)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber MapFileBlock(byte[] diAddr, int logicalBlock, out uint physicalBlock)
    {
        physicalBlock = 0;

        // Number of 3-byte block pointers that fit in one indirect block
        int pointersPerBlock = _blockSize / 3;

        // Direct blocks: indices 0-9
        if(logicalBlock < 10)
        {
            physicalBlock = Read3ByteAddress(diAddr, logicalBlock);

            return ErrorNumber.NoError;
        }

        int excess = logicalBlock - 10;

        // Single indirect: index 10
        if(excess < pointersPerBlock)
        {
            uint indirectBlock = Read3ByteAddress(diAddr, 10);

            if(indirectBlock == 0) return ErrorNumber.NoError;

            ErrorNumber errno = ReadBlock(indirectBlock, out byte[] indirectData);

            if(errno != ErrorNumber.NoError) return errno;

            physicalBlock = Read3ByteAddress(indirectData, excess);

            return ErrorNumber.NoError;
        }

        excess -= pointersPerBlock;

        // Double indirect: index 11
        long doubleRange = (long)pointersPerBlock * pointersPerBlock;

        if(excess < doubleRange)
        {
            uint doubleIndirectBlock = Read3ByteAddress(diAddr, 11);

            if(doubleIndirectBlock == 0) return ErrorNumber.NoError;

            ErrorNumber errno = ReadBlock(doubleIndirectBlock, out byte[] doubleIndirectData);

            if(errno != ErrorNumber.NoError) return errno;

            int  firstIndex    = excess / pointersPerBlock;
            uint indirectBlock = Read3ByteAddress(doubleIndirectData, firstIndex);

            if(indirectBlock == 0) return ErrorNumber.NoError;

            errno = ReadBlock(indirectBlock, out byte[] indirectData);

            if(errno != ErrorNumber.NoError) return errno;

            int secondIndex = excess % pointersPerBlock;
            physicalBlock = Read3ByteAddress(indirectData, secondIndex);

            return ErrorNumber.NoError;
        }

        excess -= (int)doubleRange;

        // Triple indirect: index 12
        uint tripleIndirectBlock = Read3ByteAddress(diAddr, 12);

        if(tripleIndirectBlock == 0) return ErrorNumber.NoError;

        ErrorNumber err = ReadBlock(tripleIndirectBlock, out byte[] tripleIndirectData);

        if(err != ErrorNumber.NoError) return err;

        int tripleFirstIndex = excess / (pointersPerBlock * pointersPerBlock);

        uint doubleBlock = Read3ByteAddress(tripleIndirectData, tripleFirstIndex);

        if(doubleBlock == 0) return ErrorNumber.NoError;

        err = ReadBlock(doubleBlock, out byte[] doubleData);

        if(err != ErrorNumber.NoError) return err;

        int remainder    = excess    % (pointersPerBlock * pointersPerBlock);
        int doubleSecond = remainder / pointersPerBlock;

        uint singleBlock = Read3ByteAddress(doubleData, doubleSecond);

        if(singleBlock == 0) return ErrorNumber.NoError;

        err = ReadBlock(singleBlock, out byte[] singleData);

        if(err != ErrorNumber.NoError) return err;

        int thirdIndex = remainder % pointersPerBlock;
        physicalBlock = Read3ByteAddress(singleData, thirdIndex);

        return ErrorNumber.NoError;
    }
}