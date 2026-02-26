// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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

public sealed partial class F2FS
{
    /// <summary>Resolves a file-page index to a data block address for a given inode</summary>
    ErrorNumber ResolveDataBlock(Inode inode, uint pageIndex, int addrsPerInode, out uint blockAddr)
    {
        blockAddr = 0;

        // Direct addresses in the inode itself
        if(pageIndex < addrsPerInode)
        {
            var addrIndex = (int)pageIndex;

            // Extra isize consumes the start of i_addr
            var extraIsize = 0;

            if((inode.i_inline & F2FS_EXTRA_ATTR) != 0 && inode.i_addr?.Length > 0)
            {
                extraIsize =  (int)(inode.i_addr[0] & 0xFFFF);
                extraIsize /= 4;
            }

            blockAddr = inode.i_addr[extraIsize + addrIndex];

            return ErrorNumber.NoError;
        }

        // Beyond direct addresses — need to go through indirect/double-indirect node blocks
        // i_nid[0] = direct node 1
        // i_nid[1] = direct node 2
        // i_nid[2] = indirect node 1
        // i_nid[3] = indirect node 2
        // i_nid[4] = double indirect node

        uint remaining = pageIndex - (uint)addrsPerInode;

        // Direct node blocks: each has DEF_ADDRS_PER_BLOCK entries
        if(remaining < DEF_ADDRS_PER_BLOCK) return ResolveDirectNode(inode.i_nid[0], remaining, out blockAddr);

        remaining -= DEF_ADDRS_PER_BLOCK;

        if(remaining < DEF_ADDRS_PER_BLOCK) return ResolveDirectNode(inode.i_nid[1], remaining, out blockAddr);

        remaining -= DEF_ADDRS_PER_BLOCK;

        // Indirect nodes: each points to NIDS_PER_BLOCK direct nodes
        const uint indirectCapacity = NIDS_PER_BLOCK * DEF_ADDRS_PER_BLOCK;

        if(remaining < indirectCapacity) return ResolveIndirectNode(inode.i_nid[2], remaining, out blockAddr);

        remaining -= indirectCapacity;

        if(remaining < indirectCapacity) return ResolveIndirectNode(inode.i_nid[3], remaining, out blockAddr);

        remaining -= indirectCapacity;

        // Double indirect
        const uint dindirectCapacity = (uint)((long)NIDS_PER_BLOCK * NIDS_PER_BLOCK * DEF_ADDRS_PER_BLOCK);

        if(remaining < dindirectCapacity) return ResolveDoubleIndirectNode(inode.i_nid[4], remaining, out blockAddr);

        AaruLogging.Debug(MODULE_NAME, "Page index {0} out of range", pageIndex);

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Resolves a block address through a direct node</summary>
    ErrorNumber ResolveDirectNode(uint nid, uint offset, out uint blockAddr)
    {
        blockAddr = 0;

        if(nid == 0) return ErrorNumber.NoError; // Not allocated

        ErrorNumber errno = LookupNat(nid, out uint nodeBlockAddr);

        if(errno != ErrorNumber.NoError) return errno;

        if(nodeBlockAddr == 0) return ErrorNumber.NoError;

        errno = ReadBlock(nodeBlockAddr, out byte[] nodeData);

        if(errno != ErrorNumber.NoError) return errno;

        // Direct node: array of __le32 block addresses, followed by node_footer
        // The addr array starts at offset 0 of the node data
        var addrOffset = (int)(offset * 4);

        if(addrOffset + 4 > nodeData.Length) return ErrorNumber.InvalidArgument;

        blockAddr = BitConverter.ToUInt32(nodeData, addrOffset);

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a block address through an indirect node</summary>
    ErrorNumber ResolveIndirectNode(uint nid, uint offset, out uint blockAddr)
    {
        blockAddr = 0;

        if(nid == 0) return ErrorNumber.NoError;

        ErrorNumber errno = LookupNat(nid, out uint nodeBlockAddr);

        if(errno != ErrorNumber.NoError) return errno;

        if(nodeBlockAddr == 0) return ErrorNumber.NoError;

        errno = ReadBlock(nodeBlockAddr, out byte[] nodeData);

        if(errno != ErrorNumber.NoError) return errno;

        // Indirect node: array of __le32 nids, pick the right direct node
        uint directNodeIndex = offset / DEF_ADDRS_PER_BLOCK;
        uint offsetInDirect  = offset % DEF_ADDRS_PER_BLOCK;

        var nidOffset = (int)(directNodeIndex * 4);

        if(nidOffset + 4 > nodeData.Length) return ErrorNumber.InvalidArgument;

        var directNid = BitConverter.ToUInt32(nodeData, nidOffset);

        return ResolveDirectNode(directNid, offsetInDirect, out blockAddr);
    }

    /// <summary>Resolves a block address through a double-indirect node</summary>
    ErrorNumber ResolveDoubleIndirectNode(uint nid, uint offset, out uint blockAddr)
    {
        blockAddr = 0;

        if(nid == 0) return ErrorNumber.NoError;

        ErrorNumber errno = LookupNat(nid, out uint nodeBlockAddr);

        if(errno != ErrorNumber.NoError) return errno;

        if(nodeBlockAddr == 0) return ErrorNumber.NoError;

        errno = ReadBlock(nodeBlockAddr, out byte[] nodeData);

        if(errno != ErrorNumber.NoError) return errno;

        const uint indirectCapacity = NIDS_PER_BLOCK * DEF_ADDRS_PER_BLOCK;
        uint       indirectIndex    = offset / indirectCapacity;
        uint       remainingOffset  = offset % indirectCapacity;

        var nidOffset = (int)(indirectIndex * 4);

        if(nidOffset + 4 > nodeData.Length) return ErrorNumber.InvalidArgument;

        var indirectNid = BitConverter.ToUInt32(nodeData, nidOffset);

        return ResolveIndirectNode(indirectNid, remainingOffset, out blockAddr);
    }
}