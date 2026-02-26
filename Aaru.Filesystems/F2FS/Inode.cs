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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <summary>Reads an inode by its node ID via NAT lookup, with node footer and inode sanity validation</summary>
    /// <param name="nid">Node ID of the inode</param>
    /// <param name="inode">Output inode structure</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInode(uint nid, out Inode inode)
    {
        inode = default(Inode);

        ErrorNumber errno = LookupNat(nid, out uint blockAddr);

        if(errno != ErrorNumber.NoError) return errno;

        if(blockAddr == 0) return ErrorNumber.InvalidArgument;

        errno = ReadBlock(blockAddr, out byte[] nodeBlock);

        if(errno != ErrorNumber.NoError) return errno;

        // Validate node footer (last 24 bytes of the 4K block)
        if(!ValidateNodeFooter(nodeBlock, nid, nid))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadInode: node footer validation failed for nid={0}", nid);

            return ErrorNumber.InvalidArgument;
        }

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(nodeBlock);

        // Run inode sanity checks
        if(!SanityCheckInode(inode, nid))
        {
            AaruLogging.Debug(MODULE_NAME, "ReadInode: inode sanity check failed for nid={0}", nid);

            return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Validates the node footer at the end of a node block.
    ///     The footer is the last 24 bytes: nid(4) + ino(4) + flag(4) + cp_ver(8) + next_blkaddr(4).
    ///     For inode nodes, nid must equal ino. For other nodes, expectedIno can be 0 to skip that check.
    /// </summary>
    bool ValidateNodeFooter(byte[] nodeBlock, uint expectedNid, uint expectedIno)
    {
        if(nodeBlock == null || nodeBlock.Length < _blockSize) return false;

        // Footer is at blockSize - sizeof(NodeFooter)
        // NodeFooter: nid(4) + ino(4) + flag(4) + cp_ver(8) + next_blkaddr(4) = 24 bytes
        int footerOffset = (int)_blockSize - 24;

        var footerNid = BitConverter.ToUInt32(nodeBlock, footerOffset);
        var footerIno = BitConverter.ToUInt32(nodeBlock, footerOffset + 4);

        // Check nid matches expected
        if(expectedNid != 0 && footerNid != expectedNid)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ValidateNodeFooter: nid mismatch: footer={0}, expected={1}",
                              footerNid,
                              expectedNid);

            return false;
        }

        // For inode nodes, ino must equal nid
        if(expectedIno != 0 && footerIno != expectedIno)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ValidateNodeFooter: ino mismatch: footer={0}, expected={1}",
                              footerIno,
                              expectedIno);

            return false;
        }

        // Validate that xattr nid (if present in the inode) does not equal ino
        // (checked later in SanityCheckInode, but we can't do it here without parsing the inode)

        return true;
    }

    /// <summary>
    ///     Performs sanity checks on an inode, matching the kernel's sanity_check_inode().
    ///     Returns true if the inode passes all checks, false otherwise.
    /// </summary>
    bool SanityCheckInode(in Inode inode, uint nid)
    {
        // i_blocks must be non-zero
        if(inode.i_blocks == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "SanityCheckInode: nid={0} has i_blocks=0", nid);

            return false;
        }

        // xattr_nid must not equal the inode's own nid
        if(inode.i_xattr_nid != 0 && inode.i_xattr_nid == nid)
        {
            AaruLogging.Debug(MODULE_NAME, "SanityCheckInode: nid={0} has i_xattr_nid equal to its own nid", nid);

            return false;
        }

        // xattr_nid must be within valid NID range
        if(inode.i_xattr_nid != 0 && inode.i_xattr_nid >= _maxNid)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "SanityCheckInode: nid={0} has out-of-range i_xattr_nid={1} (max={2})",
                              nid,
                              inode.i_xattr_nid,
                              _maxNid);

            return false;
        }

        // Extra attribute validation
        if((inode.i_inline & F2FS_EXTRA_ATTR) != 0)
        {
            // Superblock must have the extra_attr feature
            if((_superblock.feature & F2FS_FEATURE_EXTRA_ATTR) == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "SanityCheckInode: nid={0} has EXTRA_ATTR but feature is off", nid);

                return false;
            }

            // Validate i_extra_isize range and alignment
            if(inode.i_addr is { Length: > 0 })
            {
                var extraIsizeBytes = (int)(inode.i_addr[0] & 0xFFFF);

                if(extraIsizeBytes     < F2FS_MIN_EXTRA_ATTR_SIZE   ||
                   extraIsizeBytes     > F2FS_TOTAL_EXTRA_ATTR_SIZE ||
                   extraIsizeBytes % 4 != 0)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "SanityCheckInode: nid={0} has invalid i_extra_isize={1} (min={2}, max={3})",
                                      nid,
                                      extraIsizeBytes,
                                      F2FS_MIN_EXTRA_ATTR_SIZE,
                                      F2FS_TOTAL_EXTRA_ATTR_SIZE);

                    return false;
                }
            }

            // Compression sanity checks
            if((_superblock.feature & F2FS_FEATURE_COMPRESSION) != 0 &&
               (inode.i_flags       & F2FS_COMPR_FL)            != 0 &&
               inode.i_addr is { Length: > EXTRA_OFFSET_COMPRESS_ALG })
            {
                var compAlg        = (byte)(inode.i_addr[EXTRA_OFFSET_COMPRESS_ALG]      & 0xFF);
                var logClusterSize = (byte)(inode.i_addr[EXTRA_OFFSET_COMPRESS_ALG] >> 8 & 0xFF);

                if(compAlg >= COMPRESS_MAX)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "SanityCheckInode: nid={0} has invalid compress algorithm={1}",
                                      nid,
                                      compAlg);

                    return false;
                }

                if(logClusterSize < MIN_COMPRESS_LOG_SIZE || logClusterSize > MAX_COMPRESS_LOG_SIZE)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "SanityCheckInode: nid={0} has invalid log_cluster_size={1}",
                                      nid,
                                      logClusterSize);

                    return false;
                }
            }
        }
        else
        {
            // Without extra_attr, certain features should not be enabled on the superblock
            if((_superblock.feature & F2FS_FEATURE_PRJQUOTA) != 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "SanityCheckInode: nid={0} has no EXTRA_ATTR but PRJQUOTA feature is on",
                                  nid);

                return false;
            }

            if((_superblock.feature & F2FS_FEATURE_INODE_CHKSUM) != 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "SanityCheckInode: nid={0} has no EXTRA_ATTR but INODE_CHKSUM feature is on",
                                  nid);

                return false;
            }

            if((_superblock.feature & F2FS_FEATURE_FLEXIBLE_INLINE_XATTR) != 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "SanityCheckInode: nid={0} has no EXTRA_ATTR but FLEXIBLE_INLINE_XATTR is on",
                                  nid);

                return false;
            }

            if((_superblock.feature & F2FS_FEATURE_INODE_CRTIME) != 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "SanityCheckInode: nid={0} has no EXTRA_ATTR but INODE_CRTIME is on",
                                  nid);

                return false;
            }

            if((_superblock.feature & F2FS_FEATURE_COMPRESSION) != 0)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "SanityCheckInode: nid={0} has no EXTRA_ATTR but COMPRESSION feature is on",
                                  nid);

                return false;
            }
        }

        // Flexible inline xattr size validation
        if((_superblock.feature & F2FS_FEATURE_FLEXIBLE_INLINE_XATTR) != 0 && (inode.i_inline & F2FS_INLINE_XATTR) != 0)
        {
            int inlineXattrSize = GetInlineXattrAddrs(inode);

            if(inlineXattrSize < MIN_INLINE_XATTR_SIZE || inlineXattrSize > MAX_INLINE_XATTR_SIZE)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "SanityCheckInode: nid={0} has invalid i_inline_xattr_size={1} (min={2}, max={3})",
                                  nid,
                                  inlineXattrSize,
                                  MIN_INLINE_XATTR_SIZE,
                                  MAX_INLINE_XATTR_SIZE);

                return false;
            }
        }

        ushort mode = inode.i_mode;

        // Inline data is only valid on regular files and symlinks
        if((inode.i_inline & F2FS_INLINE_DATA) != 0 && (mode & 0xF000) != 0x8000 && (mode & 0xF000) != 0xA000)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "SanityCheckInode: nid={0} has inline data but mode=0x{1:X4} is not regular/symlink",
                              nid,
                              mode);

            return false;
        }

        // Inline dentry is only valid on directories
        if((inode.i_inline & F2FS_INLINE_DENTRY) != 0 && (mode & 0xF000) != 0x4000)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "SanityCheckInode: nid={0} has inline dentry but mode=0x{1:X4} is not a directory",
                              nid,
                              mode);

            return false;
        }

        // Casefold flag requires the casefold feature
        if((inode.i_flags & F2FS_CASEFOLD_FL) != 0 && (_superblock.feature & F2FS_FEATURE_CASEFOLD) == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "SanityCheckInode: nid={0} has casefold flag but feature is off", nid);

            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns the extra inode size in __le32 units.
    ///     Matches the kernel's offset_in_addr() / get_extra_isize().
    /// </summary>
    static int GetExtraIsize(in Inode inode)
    {
        if((inode.i_inline & F2FS_EXTRA_ATTR) == 0 || inode.i_addr is not { Length: > 0 }) return 0;

        // i_extra_isize is stored as the first u16 of i_addr, value is in bytes
        return (int)(inode.i_addr[0] & 0xFFFF) / 4;
    }

    /// <summary>
    ///     Returns the number of __le32 slots reserved for inline xattrs.
    ///     Matches the kernel's get_inline_xattr_addrs() with the three-way check from do_read_inode():
    ///     1) If superblock has FLEXIBLE_INLINE_XATTR feature → use per-inode i_inline_xattr_size
    ///     2) Else if inode has INLINE_XATTR or INLINE_DENTRY → DEFAULT_INLINE_XATTR_ADDRS (50)
    ///     3) Else → 0
    /// </summary>
    int GetInlineXattrAddrs(in Inode inode)
    {
        if((_superblock.feature & F2FS_FEATURE_FLEXIBLE_INLINE_XATTR) != 0)
        {
            // Per-inode value from extra attributes: i_inline_xattr_size is at offset 2 in the extra area
            if((inode.i_inline                     & F2FS_EXTRA_ATTR) != 0 && inode.i_addr is { Length: > 0 })
                return (int)(inode.i_addr[0] >> 16 & 0xFFFF);

            return 0;
        }

        if((inode.i_inline & (F2FS_INLINE_XATTR | F2FS_INLINE_DENTRY)) != 0) return DEFAULT_INLINE_XATTR_ADDRS;

        return 0;
    }

    /// <summary>Returns the number of usable direct-address slots in an inode's i_addr array</summary>
    int GetAddrsPerInode(in Inode inode) => DEF_ADDRS_PER_INODE - GetExtraIsize(inode) - GetInlineXattrAddrs(inode);

    /// <summary>Resolves a file-page index to a data block address for a given inode</summary>
    ErrorNumber ResolveDataBlock(Inode inode, uint pageIndex, int addrsPerInode, out uint blockAddr)
    {
        blockAddr = 0;

        // Direct addresses in the inode itself
        if(pageIndex < addrsPerInode)
        {
            var addrIndex = (int)pageIndex;

            // Extra isize consumes the start of i_addr
            int extraIsize = GetExtraIsize(inode);

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