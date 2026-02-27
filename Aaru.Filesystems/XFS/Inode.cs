// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    /// <summary>Reads an inode from disk given its inode number</summary>
    /// <param name="inodeNumber">The inode number to read</param>
    /// <param name="inode">The resulting dinode structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(ulong inodeNumber, out Dinode inode)
    {
        inode = default(Dinode);

        // Check cache first
        if(_inodeCache.TryGetValue(inodeNumber, out inode)) return ErrorNumber.NoError;

        // Decode inode number into AG number, block within AG, and offset within block
        // inode_number = (ag_number << (agblklog + inopblog)) | (ag_block << inopblog) | offset_in_block
        int inopblog = _superblock.inopblog;
        int agblklog = _superblock.agblklog;

        var agNo     = (uint)(inodeNumber >> agblklog                     + inopblog);
        var agBlock  = (uint)(inodeNumber >> inopblog & (1UL << agblklog) - 1);
        var inodeOff = (uint)(inodeNumber             & (1UL << inopblog) - 1);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: AG={1}, block={2}, offset={3}",
                          inodeNumber,
                          agNo,
                          agBlock,
                          inodeOff);

        if(agNo >= _superblock.agcount)
        {
            AaruLogging.Debug(MODULE_NAME, "AG number {0} out of range (max {1})", agNo, _superblock.agcount);

            return ErrorNumber.InvalidArgument;
        }

        // Calculate the filesystem block number
        ulong fsBlock = (ulong)agNo * _superblock.agblocks + agBlock;

        // Calculate the byte offset within the partition
        ulong byteOffset = fsBlock * _superblock.blocksize + inodeOff * _superblock.inodesize;

        // Read the inode from disk
        ErrorNumber errno = ReadBytes(byteOffset, _superblock.inodesize, out byte[] inodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode data: {0}", errno);

            return errno;
        }

        int dinodeSize = Marshal.SizeOf<Dinode>();

        if(inodeData.Length < dinodeSize)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode data too small: {0} bytes, need {1}", inodeData.Length, dinodeSize);

            return ErrorNumber.InvalidArgument;
        }

        inode = Marshal.ByteArrayToStructureBigEndian<Dinode>(inodeData, 0, dinodeSize);

        // Validate inode magic
        if(inode.di_magic != XFS_DINODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid inode magic 0x{0:X4} for inode {1}, expected 0x{2:X4}",
                              inode.di_magic,
                              inodeNumber,
                              XFS_DINODE_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        // Cache for future lookups
        _inodeCache[inodeNumber] = inode;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the full raw inode data (core + forks) for the given inode number</summary>
    /// <param name="inodeNumber">The inode number</param>
    /// <param name="rawInode">The raw byte data of the entire inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInodeRaw(ulong inodeNumber, out byte[] rawInode)
    {
        rawInode = null;

        int inopblog = _superblock.inopblog;
        int agblklog = _superblock.agblklog;

        var agNo     = (uint)(inodeNumber >> agblklog                     + inopblog);
        var agBlock  = (uint)(inodeNumber >> inopblog & (1UL << agblklog) - 1);
        var inodeOff = (uint)(inodeNumber             & (1UL << inopblog) - 1);

        ulong fsBlock    = (ulong)agNo * _superblock.agblocks  + agBlock;
        ulong byteOffset = fsBlock     * _superblock.blocksize + inodeOff * _superblock.inodesize;

        return ReadBytes(byteOffset, _superblock.inodesize, out rawInode);
    }
}