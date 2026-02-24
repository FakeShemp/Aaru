// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : XTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : IBM JFS filesystem plugin
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of IBM's Journaled File System</summary>
public sealed partial class JFS
{
    /// <summary>Extracts the 40-bit offset from an extent allocation descriptor (xad_t)</summary>
    static long XadOffset(byte[] xadData, int offset)
    {
        byte off1 = xadData[offset                        + 3];
        var  off2 = BitConverter.ToUInt32(xadData, offset + 4);

        return (long)off1 << 32 | off2;
    }

    /// <summary>Extracts the address from a raw xad_t at the given byte offset</summary>
    static ulong XadAddress(byte[] xadData, int offset)
    {
        // The pxd_t (loc) is at offset+8 within the xad
        var   lenAddr = BitConverter.ToUInt32(xadData, offset + 8);
        var   addr2   = BitConverter.ToUInt32(xadData, offset + 12);
        ulong n       = lenAddr & ~0xFFFFFFu;

        return (n << 8) + addr2;
    }

    /// <summary>Extracts the length from a raw xad_t at the given byte offset</summary>
    static uint XadLength(byte[] xadData, int offset)
    {
        var lenAddr = BitConverter.ToUInt32(xadData, offset + 8);

        return lenAddr & 0xFFFFFF;
    }

    /// <summary>Looks up a logical block number in an xtree to find the physical block</summary>
    /// <param name="extensionData">The inode extension area (di_u, 384 bytes)</param>
    /// <param name="isDirectory">Whether the inode is a directory (affects offset of xtroot)</param>
    /// <param name="logicalBlock">Logical block number to look up</param>
    /// <param name="physicalBlock">Output physical block number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber XTreeLookup(byte[] extensionData, bool isDirectory, long logicalBlock, out long physicalBlock)
    {
        physicalBlock = -1;

        // For file inodes (including FILESYSTEM_I), xtroot starts at offset 96 in di_u
        // For directory inodes, it would be different but we don't use xtree for directories
        const int xtreeOffset = 96;

        // Parse the xtheader (first 32 bytes of xtroot)
        // xtheader: next(8) + prev(8) + flag(1) + rsrvd1(1) + nextindex(2) + maxentry(2) + rsrvd2(2) + self(8) = 32
        byte flag      = extensionData[xtreeOffset                        + 16];
        var  nextindex = BitConverter.ToUInt16(extensionData, xtreeOffset + 18);

        AaruLogging.Debug(MODULE_NAME, "XTree root: flag=0x{0:X2}, nextindex={1}", flag, nextindex);

        if(nextindex <= XTENTRYSTART)
        {
            AaruLogging.Debug(MODULE_NAME, "XTree root has no entries");

            return ErrorNumber.InvalidArgument;
        }

        if((flag & BT_LEAF) != 0)
        {
            // Root is a leaf - search xad entries directly
            return XTreeSearchLeaf(extensionData,
                                   xtreeOffset,
                                   nextindex,
                                   XTROOTMAXSLOT,
                                   logicalBlock,
                                   out physicalBlock);
        }

        if((flag & BT_INTERNAL) != 0)
        {
            // Root is internal - find child page, then search it
            return XTreeSearchInternal(extensionData,
                                       xtreeOffset,
                                       nextindex,
                                       XTROOTMAXSLOT,
                                       logicalBlock,
                                       out physicalBlock);
        }

        AaruLogging.Debug(MODULE_NAME, "XTree root has unknown flag: 0x{0:X2}", flag);

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Searches a leaf xtree node for a logical block</summary>
    ErrorNumber XTreeSearchLeaf(byte[]   data, int baseOffset, int nextindex, int maxSlots, long logicalBlock,
                                out long physicalBlock)
    {
        physicalBlock = -1;

        for(int i = XTENTRYSTART; i < nextindex && i < maxSlots; i++)
        {
            int xadOffset = baseOffset + i * XTSLOTSIZE;

            long  xadOff  = XadOffset(data, xadOffset);
            uint  xadLen  = XadLength(data, xadOffset);
            ulong xadAddr = XadAddress(data, xadOffset);

            if(logicalBlock >= xadOff && logicalBlock < xadOff + xadLen)
            {
                physicalBlock = (long)(xadAddr + (ulong)(logicalBlock - xadOff));

                AaruLogging.Debug(MODULE_NAME,
                                  "XTree leaf match: xad[{0}] off={1}, len={2}, addr={3} -> phys={4}",
                                  i,
                                  xadOff,
                                  xadLen,
                                  xadAddr,
                                  physicalBlock);

                return ErrorNumber.NoError;
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Logical block {0} not found in xtree leaf", logicalBlock);

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Searches an internal xtree node to find the child page, then searches the child</summary>
    ErrorNumber XTreeSearchInternal(byte[]   data, int baseOffset, int nextindex, int maxSlots, long logicalBlock,
                                    out long physicalBlock)
    {
        physicalBlock = -1;

        // In internal nodes, xad entries point to child pages
        // Find the last entry whose offset <= logicalBlock
        int childIdx = -1;

        for(int i = XTENTRYSTART; i < nextindex && i < maxSlots; i++)
        {
            int  xadOffset = baseOffset + i * XTSLOTSIZE;
            long xadOff    = XadOffset(data, xadOffset);

            if(xadOff <= logicalBlock)
                childIdx = i;
            else
                break;
        }

        if(childIdx < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No matching internal xtree entry for logical block {0}", logicalBlock);

            return ErrorNumber.InvalidArgument;
        }

        int   childXadOffset = baseOffset + childIdx * XTSLOTSIZE;
        ulong childAddr      = XadAddress(data, childXadOffset);

        AaruLogging.Debug(MODULE_NAME, "XTree internal: following child at block {0}", childAddr);

        // Read the child xtree page (always PSIZE = 4096 bytes)
        ErrorNumber errno = ReadBytes((long)childAddr * _superblock.s_bsize, PSIZE, out byte[] childPage);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse child page header
        byte childFlag      = childPage[16];
        var  childNextindex = BitConverter.ToUInt16(childPage, 18);

        AaruLogging.Debug(MODULE_NAME, "XTree child page: flag=0x{0:X2}, nextindex={1}", childFlag, childNextindex);

        if((childFlag & BT_LEAF) != 0)
            return XTreeSearchLeaf(childPage, 0, childNextindex, XTPAGEMAXSLOT, logicalBlock, out physicalBlock);

        // If still internal, we'd need to recurse deeper - for now return error
        AaruLogging.Debug(MODULE_NAME, "XTree has more than 2 levels, not supported");

        return ErrorNumber.NotSupported;
    }
}