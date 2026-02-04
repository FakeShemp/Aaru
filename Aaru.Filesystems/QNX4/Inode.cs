// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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

using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX4
{
    /// <summary>Reads an inode entry from a specific block and index</summary>
    /// <param name="blockNum">The block number containing the inode</param>
    /// <param name="index">The index of the inode within the block (0-7)</param>
    /// <param name="entry">The read inode entry</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInodeEntry(uint blockNum, byte index, out qnx4_inode_entry entry)
    {
        entry = default(qnx4_inode_entry);

        if(index >= QNX4_INODES_PER_BLOCK)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadInodeEntry: Invalid index {0}", index);

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno = ReadBlock(blockNum, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadInodeEntry: Error reading block {0}", blockNum);

            return errno;
        }

        int offset = index * QNX4_DIR_ENTRY_SIZE;
        entry = Marshal.ByteArrayToStructureLittleEndian<qnx4_inode_entry>(blockData, offset, QNX4_DIR_ENTRY_SIZE);

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a QNX4 inode entry to a FileEntryInfo structure</summary>
    /// <param name="inode">The QNX4 inode entry</param>
    /// <returns>The FileEntryInfo structure</returns>
    static FileEntryInfo InodeToFileEntryInfo(qnx4_inode_entry inode)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = QNX4_BLOCK_SIZE,
            Links               = inode.di_nlink,
            Length              = inode.di_size,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = inode.di_mode,
            CreationTimeUtc     = DateHandlers.UnixUnsignedToDateTime(inode.di_ftime),
            LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime(inode.di_mtime),
            AccessTimeUtc       = DateHandlers.UnixUnsignedToDateTime(inode.di_atime),
            StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime(inode.di_ctime)
        };

        // Convert UNIX mode to FileAttributes
        // S_IFMT = 0xF000 (file type mask)
        info.Attributes = (inode.di_mode & 0xF000) switch
                          {
                              0x4000 => FileAttributes.Directory,   // S_IFDIR
                              0x8000 => FileAttributes.File,        // S_IFREG
                              0xA000 => FileAttributes.Symlink,     // S_IFLNK
                              0x2000 => FileAttributes.CharDevice,  // S_IFCHR
                              0x6000 => FileAttributes.BlockDevice, // S_IFBLK
                              0x1000 => FileAttributes.FIFO,        // S_IFIFO
                              0xC000 => FileAttributes.Socket,      // S_IFSOCK
                              _      => FileAttributes.File
                          };

        return info;
    }
}