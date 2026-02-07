// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Extent.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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

/// <inheritdoc />
public sealed partial class EFS
{
    /// <summary>Loads extents for a file, handling indirect extents for large files</summary>
    /// <param name="inode">The file's inode</param>
    /// <param name="extents">The loaded extents</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadExtents(Inode inode, out Extent[] extents)
    {
        extents = null;

        if(inode.di_numextents <= 0) return ErrorNumber.NoError;

        // Direct extents: up to 12 extents stored directly in the inode
        if(inode.di_numextents <= EFS_DIRECTEXTENTS)
        {
            extents = new Extent[inode.di_numextents];
            Array.Copy(inode.di_extents, extents, inode.di_numextents);

            return ErrorNumber.NoError;
        }

        // Indirect extents: the direct extent area contains pointers to blocks
        // that hold the actual extent descriptors
        AaruLogging.Debug(MODULE_NAME, "LoadExtents: Loading {0} indirect extents", inode.di_numextents);

        // The first extent's offset field contains the number of indirect extents
        var numIndirectExtents = (int)inode.di_extents[0].Offset;

        if(numIndirectExtents <= 0 || numIndirectExtents > EFS_DIRECTEXTENTS)
            numIndirectExtents = 1; // Fix up for old kernels

        extents = new Extent[inode.di_numextents];
        var extentIndex = 0;

        // Number of extents that fit in a basic block
        const int extentsPerBlock = EFS_BBSIZE / 8;

        // Read each indirect extent block
        for(var i = 0; i < numIndirectExtents; i++)
        {
            Extent indirectExtent = inode.di_extents[i];

            if(indirectExtent.Length == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "LoadExtents: Indirect extent {0} has zero length", i);

                continue;
            }

            // Read all blocks in this indirect extent
            for(var j = 0; j < indirectExtent.Length; j++)
            {
                uint blockNum = indirectExtent.BlockNumber + (uint)j;

                ErrorNumber errno = ReadBasicBlock((int)blockNum, out byte[] blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "LoadExtents: Error reading indirect block {0}: {1}",
                                      blockNum,
                                      errno);

                    return errno;
                }

                // Parse extents from the block (each extent is 8 bytes)
                for(var k = 0; k < extentsPerBlock && extentIndex < inode.di_numextents; k++)
                {
                    int offset = k * 8;

                    if(offset + 8 > blockData.Length) break;

                    Extent extent = Marshal.ByteArrayToStructureBigEndian<Extent>(blockData, offset, 8);
                    extents[extentIndex++] = extent;
                }
            }
        }

        AaruLogging.Debug(MODULE_NAME, "LoadExtents: Loaded {0} extents", extentIndex);

        return ErrorNumber.NoError;
    }

    /// <summary>Finds the extent containing a logical block</summary>
    /// <param name="extents">Array of extents</param>
    /// <param name="logicalBlock">Logical block number within the file</param>
    /// <param name="extent">The extent containing the block</param>
    /// <param name="blockInExtent">Offset within the extent</param>
    /// <returns>Error number indicating success or failure</returns>
    static ErrorNumber FindExtentForBlock(Extent[] extents, uint logicalBlock, out Extent extent,
                                          out uint blockInExtent)
    {
        extent        = default(Extent);
        blockInExtent = 0;

        if(extents == null || extents.Length == 0) return ErrorNumber.InvalidArgument;

        // Search through extents to find the one containing the logical block
        // Extents are sorted by ex_offset (logical block offset)
        foreach(Extent ex in extents)
        {
            // Skip invalid extents
            if(ex.Magic != 0) continue;

            uint extentStart = ex.Offset;
            uint extentEnd   = extentStart + ex.Length;

            if(logicalBlock >= extentStart && logicalBlock < extentEnd)
            {
                extent        = ex;
                blockInExtent = logicalBlock - extentStart;

                return ErrorNumber.NoError;
            }
        }

        return ErrorNumber.InvalidArgument;
    }
}