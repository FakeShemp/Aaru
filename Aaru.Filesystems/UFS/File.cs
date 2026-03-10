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

using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes.Enums;
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
}