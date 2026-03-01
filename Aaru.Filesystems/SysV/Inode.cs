// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class SysVfs
{
    /// <summary>Reads an inode from the inode table</summary>
    /// <param name="inodeNumber">The inode number (1-based)</param>
    /// <param name="inode">The parsed inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(int inodeNumber, out Inode inode)
    {
        inode = default(Inode);

        if(inodeNumber < 1)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNumber);

            return ErrorNumber.InvalidArgument;
        }

        int inodeIndex    = inodeNumber      - 1;
        int block         = FIRST_INODE_ZONE + inodeIndex / _inodesPerBlock;
        int offsetInBlock = inodeIndex                    % _inodesPerBlock * INODE_SIZE;

        ErrorNumber errno = ReadBlock(block, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode block {0}: {1}", block, errno);

            return errno;
        }

        if(offsetInBlock + INODE_SIZE > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode offset {0} exceeds block size {1}", offsetInBlock, blockData.Length);

            return ErrorNumber.InvalidArgument;
        }

        var inodeData = new byte[INODE_SIZE];
        Array.Copy(blockData, offsetInBlock, inodeData, 0, INODE_SIZE);

        switch(_bytesex)
        {
            case Bytesex.Pdp:
                inode = Marshal.ByteArrayToStructurePdpEndian<Inode>(inodeData);

                break;
            case Bytesex.BigEndian:
                inode = Marshal.ByteArrayToStructureBigEndian<Inode>(inodeData);

                break;
            default:
                inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData);

                break;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Reads a 3-byte disk address from an inode's di_addr field and converts it to a 32-bit block number,
    ///     respecting the filesystem's byte sex.
    /// </summary>
    /// <param name="addr">The 39-byte di_addr array</param>
    /// <param name="index">The address index (0-12)</param>
    /// <returns>The block number</returns>
    uint Read3ByteAddress(byte[] addr, int index)
    {
        int off = index * 3;
        var to  = new byte[4];

        switch(_bytesex)
        {
            case Bytesex.Pdp:
                // NXI -> N0XI: to[0]=from[0], to[1]=0, to[2]=from[1], to[3]=from[2]
                to[0] = addr[off];
                to[1] = 0;
                to[2] = addr[off + 1];
                to[3] = addr[off + 2];

                break;
            case Bytesex.LittleEndian:
                // XIN -> XIN0: to[0]=from[0], to[1]=from[1], to[2]=from[2], to[3]=0
                to[0] = addr[off];
                to[1] = addr[off + 1];
                to[2] = addr[off + 2];
                to[3] = 0;

                break;
            case Bytesex.BigEndian:
                // NIX -> 0NIX: to[0]=0, to[1]=from[0], to[2]=from[1], to[3]=from[2]
                to[0] = 0;
                to[1] = addr[off];
                to[2] = addr[off + 1];
                to[3] = addr[off + 2];

                break;
        }

        return BitConverter.ToUInt32(to, 0);
    }
}