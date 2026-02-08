// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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

using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Cram
{
    /// <summary>Converts a cramfs inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The cramfs inode</param>
    /// <returns>FileEntryInfo with the file's metadata</returns>
    FileEntryInfo InodeToFileEntryInfo(Inode inode)
    {
        ushort mode = GetInodeMode(inode);
        ushort uid  = GetInodeUid(inode);
        uint   size = GetInodeSize(inode);
        byte   gid  = GetInodeGid(inode);

        var info = new FileEntryInfo
        {
            Attributes = FileAttributes.None,
            BlockSize  = 4096, // CramFS uses 4K pages
            Length     = size,
            UID        = uid,
            GID        = gid,
            Mode       = (uint)(mode & S_IPERM)
        };

        // CramFS doesn't store timestamps
        // Links count is not stored in cramfs

        // Determine file type from mode
        var fileType = (ushort)(mode & S_IFMT);

        switch(fileType)
        {
            case S_IFDIR:
                info.Attributes = FileAttributes.Directory;

                break;
            case S_IFREG:
                info.Attributes = FileAttributes.File;

                break;
            case S_IFLNK:
                info.Attributes = FileAttributes.Symlink;

                break;
            case S_IFCHR:
                info.Attributes = FileAttributes.CharDevice;

                // For device files, Size field contains device number (i_rdev)
                // Major = bits 8-15, Minor = bits 0-7
                uint chrMajor = size >> 8 & 0xFF;
                uint chrMinor = size      & 0xFF;
                info.DeviceNo = (ulong)chrMajor << 32 | chrMinor;

                break;
            case S_IFBLK:
                info.Attributes = FileAttributes.BlockDevice;

                // For device files, Size field contains device number (i_rdev)
                // Major = bits 8-15, Minor = bits 0-7
                uint blkMajor = size >> 8 & 0xFF;
                uint blkMinor = size      & 0xFF;
                info.DeviceNo = (ulong)blkMajor << 32 | blkMinor;

                break;
            case S_IFIFO:
                info.Attributes = FileAttributes.Pipe;

                break;
            case S_IFSOCK:
                info.Attributes = FileAttributes.Socket;

                break;
            default:
                info.Attributes = FileAttributes.File;

                break;
        }

        // Calculate blocks (size / block size, rounded up)
        info.Blocks = (size + 4095) / 4096;

        return info;
    }
}