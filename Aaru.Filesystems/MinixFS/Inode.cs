// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class MinixFS
{
    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNumber">Inode number to read (1-based)</param>
    /// <param name="inode">The read inode structure (V1DiskInode or V2DiskInode)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNumber, out object inode)
    {
        inode = null;

        if(inodeNumber < 1 || inodeNumber > _ninodes)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNumber);

            return ErrorNumber.InvalidArgument;
        }

        // Check cache first
        if(_version == FilesystemVersion.V1)
        {
            if(_inodeCacheV1.TryGetValue(inodeNumber, out V1DiskInode cachedV1))
            {
                inode = cachedV1;

                return ErrorNumber.NoError;
            }
        }
        else
        {
            if(_inodeCache.TryGetValue(inodeNumber, out V2DiskInode cachedV2))
            {
                inode = cachedV2;

                return ErrorNumber.NoError;
            }
        }

        // Calculate inode location
        // Inodes are numbered from 1, but stored from index 0
        int inodeSize   = _version == FilesystemVersion.V1 ? V1_INODE_SIZE : V2_INODE_SIZE;
        var inodeIndex  = (int)(inodeNumber - 1);
        int inodeBlock  = inodeIndex / _inodesPerBlock + _firstInodeBlock;
        int inodeOffset = inodeIndex % _inodesPerBlock * inodeSize;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0}: block {1}, offset {2}",
                          inodeNumber,
                          inodeBlock,
                          inodeOffset);

        // Read the block containing the inode
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

        if(_version == FilesystemVersion.V1)
        {
            V1DiskInode v1Inode = _littleEndian
                                      ? Marshal.ByteArrayToStructureLittleEndian<V1DiskInode>(inodeData)
                                      : Marshal.ByteArrayToStructureBigEndian<V1DiskInode>(inodeData);

            _inodeCacheV1[inodeNumber] = v1Inode;
            inode                      = v1Inode;

            AaruLogging.Debug(MODULE_NAME,
                              "V1 Inode {0}: mode=0x{1:X4}, size={2}, nlinks={3}",
                              inodeNumber,
                              v1Inode.d1_mode,
                              v1Inode.d1_size,
                              v1Inode.d1_nlinks);
        }
        else
        {
            V2DiskInode v2Inode = _littleEndian
                                      ? Marshal.ByteArrayToStructureLittleEndian<V2DiskInode>(inodeData)
                                      : Marshal.ByteArrayToStructureBigEndian<V2DiskInode>(inodeData);

            _inodeCache[inodeNumber] = v2Inode;
            inode                    = v2Inode;

            AaruLogging.Debug(MODULE_NAME,
                              "V2 Inode {0}: mode=0x{1:X4}, size={2}, nlinks={3}",
                              inodeNumber,
                              v2Inode.d2_mode,
                              v2Inode.d2_size,
                              v2Inode.d2_nlinks);
        }

        return ErrorNumber.NoError;
    }
}