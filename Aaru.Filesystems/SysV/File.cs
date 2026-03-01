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
using Aaru.CommonTypes.Structs;

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
}