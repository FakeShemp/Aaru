// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    /// <summary>Traverses a B-tree rooted in an inode's bmap to find the block number for a logical block</summary>
    /// <param name="inode">Inode containing the B-tree root</param>
    /// <param name="logicalBlock">Logical block offset to look up</param>
    /// <param name="rootLevel">Level of the root node</param>
    /// <param name="rootNchildren">Number of children in the root node</param>
    /// <param name="isRootMetadata">Whether bmap values are physical (true) or virtual (false)</param>
    /// <param name="blockNr">Output block number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ResolveBtree(in Inode inode,          ulong     logicalBlock, byte rootLevel, ushort rootNchildren,
                             bool     isRootMetadata, out ulong blockNr)
    {
        blockNr = 0;

        // Root node layout in inode bmap (56 bytes total, 7 x 8):
        //   bmap[0] = header
        //   bmap[1..3] = keys   (max 3, NILFS_BTREE_ROOT_NCHILDREN_MAX = 3)
        //   bmap[4..6] = ptrs   (max 3)
        const int rootNcmax = 3; // (56 - 8) / 16

        if(rootNchildren == 0 || rootNchildren > rootNcmax)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid root B-tree nchildren: {0}", rootNchildren);

            return ErrorNumber.InvalidArgument;
        }

        // Binary search the root node keys
        var rootKeys = new ulong[rootNchildren];

        for(var i = 0; i < rootNchildren; i++) rootKeys[i] = inode.bmap[1 + i];

        int index = BtreeSearch(rootKeys, rootNchildren, logicalBlock, out bool exact);

        if(index < 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Key {0} not found in root B-tree node", logicalBlock);

            return ErrorNumber.InvalidArgument;
        }

        // If root is leaf level (level == 1), require exact match
        if(rootLevel == 1 && !exact) return ErrorNumber.InvalidArgument;

        ulong ptr = inode.bmap[1 + rootNcmax + index];

        if(ptr == 0) return ErrorNumber.InvalidArgument;

        // Traverse down the tree from level (rootLevel - 1) to level 1
        for(int currentLevel = rootLevel - 1; currentLevel > 0; currentLevel--)
        {
            // Resolve the child pointer to a physical block
            ulong physBlock;

            if(isRootMetadata)
                physBlock = ptr;
            else
            {
                ErrorNumber translateErrno = TranslateVirtualBlock(ptr, out physBlock);

                if(translateErrno != ErrorNumber.NoError) return translateErrno;
            }

            ErrorNumber readErrno = ReadPhysicalBlock(physBlock, out byte[] nodeData);

            if(readErrno != ErrorNumber.NoError) return readErrno;

            // Parse the child node header
            BTreeNode node  = Marshal.ByteArrayToStructureLittleEndian<BTreeNode>(nodeData);
            var       ncmax = (int)((_blockSize - 8) / 16);

            if(node.nchildren == 0 || node.nchildren > ncmax)
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid B-tree node nchildren: {0} (max {1})", node.nchildren, ncmax);

                return ErrorNumber.InvalidArgument;
            }

            // Extract keys from the child node
            var keys = new ulong[node.nchildren];

            for(var i = 0; i < node.nchildren; i++) keys[i] = BitConverter.ToUInt64(nodeData, 8 + i * 8);

            index = BtreeSearch(keys, node.nchildren, logicalBlock, out exact);

            if(index < 0) return ErrorNumber.InvalidArgument;

            // At leaf level (currentLevel == 1), require exact match
            if(currentLevel == 1 && !exact) return ErrorNumber.InvalidArgument;

            // Get the child/leaf pointer (ptrs start after all max key slots)
            ptr = BitConverter.ToUInt64(nodeData, 8 + ncmax * 8 + index * 8);

            if(ptr == 0) return ErrorNumber.InvalidArgument;
        }

        blockNr = ptr;

        return ErrorNumber.NoError;
    }

    /// <summary>Binary search for a key in a sorted array of B-tree keys</summary>
    /// <param name="keys">Array of keys</param>
    /// <param name="count">Number of valid keys in the array</param>
    /// <param name="target">Key to search for</param>
    /// <param name="exactMatch">Set to true if an exact match was found</param>
    /// <returns>Index of the matching or floor key, or -1 if target is less than all keys</returns>
    static int BtreeSearch(ulong[] keys, int count, ulong target, out bool exactMatch)
    {
        exactMatch = false;

        var low   = 0;
        int high  = count - 1;
        var index = 0;
        var s     = 0;

        while(low <= high)
        {
            index = (low + high) / 2;

            if(keys[index] == target)
            {
                exactMatch = true;

                return index;
            }

            if(keys[index] < target)
            {
                low = index + 1;
                s   = -1;
            }
            else
            {
                high = index - 1;
                s    = 1;
            }
        }

        // s > 0: last comparison was keys[index] > target, so floor is at index - 1
        if(s > 0) return index - 1;

        // s < 0: last comparison was keys[index] < target, index IS the floor
        return index;
    }
}