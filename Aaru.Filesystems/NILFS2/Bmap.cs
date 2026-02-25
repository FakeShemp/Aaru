// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Bmap.cs
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

using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    /// <summary>
    ///     Resolves a logical block offset to a virtual or physical block number
    ///     using the inode's block mapping (direct or B-tree)
    /// </summary>
    /// <param name="inode">Inode whose bmap to resolve</param>
    /// <param name="logicalBlock">Logical block offset</param>
    /// <param name="isRootMetadata">Whether this is a root metadata file (affects B-tree child block reading)</param>
    /// <param name="blockNr">Output block number (virtual or physical depending on file type)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ResolveBmap(in Inode inode, ulong logicalBlock, bool isRootMetadata, out ulong blockNr)
    {
        blockNr = 0;

        if(inode.bmap == null || inode.bmap.Length < NILFS2_INODE_BMAP_SIZE) return ErrorNumber.InvalidArgument;

        // Parse bmap[0] as a btree_node header to determine mapping type
        // On-disk layout of nilfs_btree_node: flags(u8) level(u8) nchildren(le16) pad(le32)
        // After little-endian deserialization of the __le64, we extract sub-fields with bit ops
        ulong header    = inode.bmap[0];
        var   level     = (byte)(header   >> 8  & 0xFF);
        var   nchildren = (ushort)(header >> 16 & 0xFFFF);

        // Decision: if level > 0 or nchildren > NILFS_DIRECT_NKEYS_MAX (6) -> B-tree
        bool isBtree = level > NILFS2_BTREE_LEVEL_DATA || nchildren > 6;

        if(!isBtree)
        {
            // Direct mapping: bmap[key + 1] holds the block number for logical block 'key'
            // Supports up to 6 direct blocks (keys 0..5)
            if(logicalBlock > 5) return ErrorNumber.InvalidArgument;

            blockNr = inode.bmap[logicalBlock + 1];

            return blockNr == 0 ? ErrorNumber.InvalidArgument : ErrorNumber.NoError;
        }

        // B-tree mapping
        return ResolveBtree(inode, logicalBlock, level, nchildren, isRootMetadata, out blockNr);
    }
}