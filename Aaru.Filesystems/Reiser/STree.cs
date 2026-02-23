// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : STree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser filesystem plugin
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

public sealed partial class Reiser
{
    /// <summary>
    ///     Compares two keys for ordering in the S+tree.
    ///     Returns negative if a &lt; b, zero if equal, positive if a &gt; b.
    /// </summary>
    static int CompareKeys(uint aDirId, uint aObjId, ulong aOffset, int aType, uint bDirId, uint bObjId, ulong bOffset,
                           int  bType)
    {
        if(aDirId != bDirId) return aDirId < bDirId ? -1 : 1;

        if(aObjId != bObjId) return aObjId < bObjId ? -1 : 1;

        if(aOffset != bOffset) return aOffset < bOffset ? -1 : 1;

        if(aType != bType) return aType < bType ? -1 : 1;

        return 0;
    }

    /// <summary>
    ///     Compares only the short key (dir_id, object_id) of two keys.
    ///     Mirrors the kernel's <c>comp_short_keys</c>.
    /// </summary>
    static int CompareShortKeys(uint aDirId, uint aObjId, uint bDirId, uint bObjId)
    {
        if(aDirId != bDirId) return aDirId < bDirId ? -1 : 1;

        if(aObjId != bObjId) return aObjId < bObjId ? -1 : 1;

        return 0;
    }

    /// <summary>Extracts the offset from an on-disk key, interpreting the version correctly</summary>
    static ulong GetKeyOffset(in Key key, int version)
    {
        if(version == KEY_FORMAT_3_6) return key.k_offset_v2 & ~0UL >> 4;

        // v3.5: lower 32 bits are offset
        return (uint)(key.k_offset_v2 & 0xFFFFFFFF);
    }

    /// <summary>Extracts the type from an on-disk key, interpreting the version correctly</summary>
    static int GetKeyType(in Key key, int version)
    {
        if(version == KEY_FORMAT_3_6)
        {
            var type = (int)(key.k_offset_v2 >> 60);

            return type <= TYPE_MAXTYPE ? type : TYPE_ANY;
        }

        // v3.5: upper 32 bits are uniqueness
        var uniqueness = (uint)(key.k_offset_v2 >> 32);

        return Uniqueness2Type(uniqueness);
    }

    /// <summary>Converts a v3.5 uniqueness value to an item type</summary>
    static int Uniqueness2Type(uint uniqueness) => uniqueness switch
                                                   {
                                                       V1_SD_UNIQUENESS       => TYPE_STAT_DATA,
                                                       V1_INDIRECT_UNIQUENESS => TYPE_INDIRECT,
                                                       V1_DIRECT_UNIQUENESS   => TYPE_DIRECT,
                                                       V1_DIRENTRY_UNIQUENESS => TYPE_DIRENTRY,
                                                       _                      => TYPE_ANY
                                                   };

    /// <summary>Determines the key version of an item header from its ih_version field</summary>
    static int GetItemKeyVersion(ushort ihVersion) => (ihVersion & 0x7FFF) == KEY_FORMAT_3_6
                                                          ? KEY_FORMAT_3_6
                                                          : KEY_FORMAT_3_5;

    /// <summary>
    ///     Searches the S+tree for the item matching the given key, or the closest item
    ///     with a key less than the search key. Maintains a tree path for subsequent
    ///     <see cref="GetRKey" /> calls. Mirrors the kernel's <c>search_by_key</c>.
    /// </summary>
    ErrorNumber SearchByKey(uint dirId, uint objectId, ulong offset, int type, out byte[] leafBlock, out int itemIndex)
    {
        TreePath ignored = null;

        return SearchByKey(dirId, objectId, offset, type, out leafBlock, out itemIndex, ref ignored);
    }

    /// <summary>
    ///     Searches the S+tree for the item matching the given key, or the closest item
    ///     with a key less than the search key. Maintains a tree path for subsequent
    ///     <see cref="GetRKey" /> calls. Mirrors the kernel's <c>search_by_key</c>.
    /// </summary>
    ErrorNumber SearchByKey(uint dirId, uint objectId, ulong offset, int type, out byte[] leafBlock, out int itemIndex,
                            ref TreePath path)
    {
        leafBlock = null;
        itemIndex = -1;

        // Allocate/reset path
        path = new TreePath
        {
            Elements = new PathElement[MAX_HEIGHT + 1],
            Length   = -1
        };

        uint currentBlock = _superblock.root_block;

        for(var level = 0; level < MAX_HEIGHT; level++)
        {
            ErrorNumber errno = ReadBlock(currentBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError) return errno;

            if(blockData.Length < BLKH_SIZE) return ErrorNumber.InvalidArgument;

            BlockHead blkHead = ReadBlockHead(blockData);

            if(blkHead.blk_nr_item == 0) return ErrorNumber.NoSuchFile;

            // Record this node in the path
            path.Length++;

            path.Elements[path.Length] = new PathElement
            {
                BlockData = blockData,
                Position  = 0
            };

            if(blkHead.blk_level == DISK_LEAF_NODE_LEVEL)
            {
                // Leaf node: binary search through item headers
                leafBlock = blockData;

                var lo   = 0;
                int hi   = blkHead.blk_nr_item - 1;
                int best = -1;

                while(lo <= hi)
                {
                    int      mid       = (lo + hi) / 2;
                    ItemHead ih        = ReadItemHead(blockData, BLKH_SIZE + mid * IH_SIZE);
                    int      ihVersion = GetItemKeyVersion(ih.ih_version);

                    ulong ihOffset = GetKeyOffset(ih.ih_key, ihVersion);
                    int   ihType   = GetKeyType(ih.ih_key, ihVersion);

                    int cmp = CompareKeys(dirId,
                                          objectId,
                                          offset,
                                          type,
                                          ih.ih_key.k_dir_id,
                                          ih.ih_key.k_objectid,
                                          ihOffset,
                                          ihType);

                    switch(cmp)
                    {
                        case 0:
                            itemIndex                           = mid;
                            path.Elements[path.Length].Position = mid;

                            return ErrorNumber.NoError;
                        case > 0:
                            best = mid;
                            lo   = mid + 1;

                            break;
                        default:
                            hi = mid - 1;

                            break;
                    }
                }

                itemIndex                           = best;
                path.Elements[path.Length].Position = best;

                return best >= 0 ? ErrorNumber.NoError : ErrorNumber.NoSuchFile;
            }

            // Internal node: binary search through keys to find which child to follow
            var lo2  = 0;
            int hi2  = blkHead.blk_nr_item - 1;
            var pos2 = 0;

            while(lo2 <= hi2)
            {
                int mid2    = (lo2 + hi2) / 2;
                Key nodeKey = ReadKey(blockData, BLKH_SIZE + mid2 * KEY_SIZE);

                int nodeKeyVersion = DetermineKeyVersion(nodeKey);

                ulong nodeKeyOffset = GetKeyOffset(nodeKey, nodeKeyVersion);
                int   nodeKeyType   = GetKeyType(nodeKey, nodeKeyVersion);

                int cmp2 = CompareKeys(dirId,
                                       objectId,
                                       offset,
                                       type,
                                       nodeKey.k_dir_id,
                                       nodeKey.k_objectid,
                                       nodeKeyOffset,
                                       nodeKeyType);

                if(cmp2 == 0)
                {
                    // Exact match: go right (child at position mid2 + 1)
                    pos2 = mid2 + 1;

                    break;
                }

                if(cmp2 > 0)
                {
                    pos2 = mid2 + 1;
                    lo2  = mid2 + 1;
                }
                else
                    hi2 = mid2 - 1;
            }

            // Record position chosen in this internal node
            path.Elements[path.Length].Position = pos2;

            int dcOffset = BLKH_SIZE + blkHead.blk_nr_item * KEY_SIZE + pos2 * DC_SIZE;

            if(dcOffset + DC_SIZE > blockData.Length) return ErrorNumber.InvalidArgument;

            DiskChild dc = ReadDiskChild(blockData, dcOffset);

            currentBlock = dc.dc_block_number;

            if(currentBlock == 0) return ErrorNumber.NoSuchFile;
        }

        AaruLogging.Debug(MODULE_NAME, "Tree traversal exceeded maximum height");

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>
    ///     Gets the right delimiting key by walking up the tree path.
    ///     Mirrors the kernel's <c>get_rkey</c>.
    ///     Returns null if the item is at the rightmost edge of the tree (equivalent to MAX_KEY),
    ///     or if the path is invalid (equivalent to MIN_KEY treated as "continue with next_pos").
    /// </summary>
    static Key? GetRKey(TreePath path)
    {
        if(path?.Elements == null || path.Length < 1) return null;

        // Walk up from the leaf's parent toward the root
        // path.Length is the leaf level index
        // path.Length - 1 is the leaf's parent, etc.
        for(int pathOffset = path.Length - 1; pathOffset >= 0; pathOffset--)
        {
            PathElement parent = path.Elements[pathOffset];

            if(parent?.BlockData == null) return new Key(); // MIN_KEY equivalent (all zeros)

            BlockHead parentHead = ReadBlockHead(parent.BlockData);

            // If position in parent is not the last one, return the delimiting key
            if(parent.Position < parentHead.blk_nr_item)
                return ReadKey(parent.BlockData, BLKH_SIZE + parent.Position * KEY_SIZE);
        }

        // Reached the root — return null to signify MAX_KEY (rightmost edge)
        return null;
    }

    /// <summary>Reads a Key struct from raw block data at the given offset</summary>
    static Key ReadKey(byte[] data, int offset) =>
        Marshal.ByteArrayToStructureLittleEndian<Key>(data, offset, KEY_SIZE);

    /// <summary>Reads an ItemHead struct from raw block data at the given offset</summary>
    static ItemHead ReadItemHead(byte[] data, int offset) =>
        Marshal.ByteArrayToStructureLittleEndian<ItemHead>(data, offset, IH_SIZE);

    /// <summary>Reads a BlockHead struct from the start of raw block data</summary>
    static BlockHead ReadBlockHead(byte[] data) =>
        Marshal.ByteArrayToStructureLittleEndian<BlockHead>(data, 0, BLKH_SIZE);

    /// <summary>Reads a DiskChild struct from raw block data at the given offset</summary>
    static DiskChild ReadDiskChild(byte[] data, int offset) =>
        Marshal.ByteArrayToStructureLittleEndian<DiskChild>(data, offset, DC_SIZE);

    /// <summary>
    ///     Determines the key version of an on-disk key by examining the offset_v2 type
    ///     field, matching the kernel's <c>le_key_version()</c> heuristic.
    ///     Only TYPE_DIRECT, TYPE_INDIRECT, and TYPE_DIRENTRY confirm v3.6 format.
    ///     TYPE_STAT_DATA (0) is ambiguous because a v3.5 key whose k_uniqueness is 0
    ///     also produces 0 in the top bits, so it must fall through to v3.5.
    /// </summary>
    static int DetermineKeyVersion(in Key key)
    {
        var type = (int)(key.k_offset_v2 >> 60);

        return type is TYPE_DIRECT or TYPE_INDIRECT or TYPE_DIRENTRY ? KEY_FORMAT_3_6 : KEY_FORMAT_3_5;
    }

    /// <summary>
    ///     Element in a tree path, recording a node and the position taken within it.
    ///     Mirrors the kernel's <c>struct path_element</c>.
    /// </summary>
    sealed class PathElement
    {
        /// <summary>Block data of this node</summary>
        public byte[] BlockData;

        /// <summary>Position (key/child index) chosen within this node</summary>
        public int Position;
    }

    /// <summary>
    ///     Tree path from root to leaf, recording every node visited during a search.
    ///     Mirrors the kernel's <c>struct treepath</c>.
    /// </summary>
    sealed class TreePath
    {
        public PathElement[] Elements;
        public int           Length; // Number of valid elements (0-based index of last)
    }
}