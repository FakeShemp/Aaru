// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Locus
{
    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNumber">Inode number to read</param>
    /// <param name="inode">The read inode structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(int inodeNumber, out Dinode inode)
    {
        inode = default(Dinode);

        if(inodeNumber < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNumber);

            return ErrorNumber.InvalidArgument;
        }

        // Check cache first
        if(_inodeCache.TryGetValue(inodeNumber, out inode)) return ErrorNumber.NoError;

        // Inode numbers are 1-based in Locus
        // itod(x) = ((x-1) / INOPB) + 2  - converts inode number to block number
        // itoo(x) = (x-1) % INOPB        - converts inode number to offset within block
        // Block 0 = boot block
        // Block 1 = superblock
        // Blocks 2+ = inode list
        int inodeSize   = _smallBlocks ? DINODE_SMALLBLOCK_SIZE : DINODE_SIZE;
        int inodeBlock  = (inodeNumber - 1) / _inodesPerBlock + 2;
        int inodeOffset = (inodeNumber - 1) % _inodesPerBlock * inodeSize;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0}: block {1}, offset {2}, inode size {3}",
                          inodeNumber,
                          inodeBlock,
                          inodeOffset,
                          inodeSize);

        ErrorNumber errno = ReadBlock(inodeBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode block: {0}", errno);

            return errno;
        }

        if(inodeOffset + inodeSize > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode offset exceeds block size");

            return ErrorNumber.InvalidArgument;
        }

        var inodeData = new byte[inodeSize];
        Array.Copy(blockData, inodeOffset, inodeData, 0, inodeSize);

        // Debug: Check if inode data is all zeros
        var allZeros = true;

        for(var i = 0; i < inodeSize && allZeros; i++)
            if(inodeData[i] != 0)
                allZeros = false;

        if(allZeros)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "WARNING: Inode {0} data is all zeros at block {1} offset {2}",
                              inodeNumber,
                              inodeBlock,
                              inodeOffset);
        }

        inode = _bigEndian
                    ? Marshal.ByteArrayToStructureBigEndian<Dinode>(inodeData)
                    : Marshal.ByteArrayToStructureLittleEndian<Dinode>(inodeData);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: mode=0x{1:X4}, size={2}, nlink={3}",
                          inodeNumber,
                          inode.di_mode,
                          inode.di_size,
                          inode.di_nlink);

        _inodeCache[inodeNumber] = inode;

        // For smallblock filesystems, check if inline data is present
        if(_smallBlocks && inodeSize == DINODE_SMALLBLOCK_SIZE)
        {
            // di_sbflag is at offset 75 (after 27 bytes of padding at offset 48)
            // di_pad[27] starts at offset 48 (after di_blocks at offset 44, 4 bytes)
            // di_sbflag is at offset 48 + 27 = 75
            // di_addr[13] starts at offset 76
            // di_sbbuf[384] starts at offset 76 + 52 = 128
            const int sbflagOffset = 75;
            const int sbbufOffset  = 128; // 76 + (13 * 4) = 76 + 52 = 128

            byte sbflag = inodeData[sbflagOffset];

            AaruLogging.Debug(MODULE_NAME, "Inode {0}: sbflag=0x{1:X2}", inodeNumber, sbflag);

            if((sbflag & SBINUSE) != 0)
            {
                // Extract inline data from di_sbbuf
                var inlineData = new byte[SMBLKSZ];
                Array.Copy(inodeData, sbbufOffset, inlineData, 0, SMBLKSZ);
                _smallBlockDataCache[inodeNumber] = inlineData;

                AaruLogging.Debug(MODULE_NAME,
                                  "Inode {0}: Cached {1} bytes of inline smallblock data",
                                  inodeNumber,
                                  SMBLKSZ);
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a Locus disk inode to a FileEntryInfo structure</summary>
    /// <param name="inode">The Locus disk inode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The FileEntryInfo structure</returns>
    FileEntryInfo InodeToFileEntryInfo(Dinode inode, int inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = _blockSize,
            Inode               = (ulong)inodeNumber,
            Length              = inode.di_size,
            Links               = (ulong)inode.di_nlink,
            UID                 = (ulong)inode.di_uid,
            GID                 = (ulong)inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0x0FFF), // Lower 12 bits are permissions
            AccessTimeUtc       = DateHandlers.UnixToDateTime(inode.di_atime),
            LastWriteTimeUtc    = DateHandlers.UnixToDateTime(inode.di_mtime),
            StatusChangeTimeUtc = DateHandlers.UnixToDateTime(inode.di_ctime),
            CreationTimeUtc     = DateHandlers.UnixToDateTime(inode.di_ctime),
            Blocks              = inode.di_blocks
        };

        // Determine file type from di_mode
        var fileType = (FileMode)(inode.di_mode & (ushort)FileMode.IFMT);

        info.Attributes = fileType switch
                          {
                              FileMode.IFDIR => FileAttributes.Directory,
                              FileMode.IFREG => FileAttributes.File,
                              FileMode.IFIFO => FileAttributes.Pipe,
                              FileMode.IFCHR => FileAttributes.CharDevice,
                              FileMode.IFBLK => FileAttributes.BlockDevice,
                              FileMode.IFMPC => FileAttributes.CharDevice,
                              FileMode.IFMPB => FileAttributes.BlockDevice,
                              _              => FileAttributes.File
                          };

        // Check disk flags for symbolic link
        if((inode.di_dflag & (short)DiskFlags.DILINK) != 0) info.Attributes = FileAttributes.Symlink;

        // Check disk flags for socket
        if((inode.di_dflag & (short)DiskFlags.DISOCKET) != 0) info.Attributes = FileAttributes.Socket;

        // Check disk flags for hidden
        if((inode.di_dflag & (short)DiskFlags.DIHIDDEN) != 0) info.Attributes |= FileAttributes.Hidden;

        // Extract device numbers for block/character devices
        if(fileType is not (FileMode.IFCHR or FileMode.IFBLK or FileMode.IFMPC or FileMode.IFMPB) ||
           inode.di_addr is not { Length: > 0 })
            return info;

        // Device numbers are stored in the first address entry
        var dev = (uint)inode.di_addr[0];

        // Old Unix format: upper 8 bits are major, lower 8 bits are minor
        uint major = dev >> 8 & 0xFF;
        uint minor = dev      & 0xFF;

        info.DeviceNo = (ulong)major << 32 | minor;

        return info;
    }
}