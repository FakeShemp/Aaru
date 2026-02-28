// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Extent.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    /// <summary>Gets data blocks from an extent tree</summary>
    /// <param name="inode">The inode with extent data in block[]</param>
    /// <param name="blockList">List to add blocks to</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber GetExtentBlocks(Inode inode, List<(ulong physicalBlock, uint length)> blockList)
    {
        // The extent tree header and first extents/indexes are stored in inode.block[15]
        // which is 60 bytes. Reinterpret as a byte array.
        var extentData = new byte[60];

        for(var i = 0; i < 15; i++)
        {
            byte[] blockBytes = BitConverter.GetBytes(inode.block[i]);
            Array.Copy(blockBytes, 0, extentData, i * 4, 4);
        }

        return ParseExtentNode(extentData, 0, blockList);
    }

    /// <summary>Parses an extent tree node (header + entries)</summary>
    /// <param name="data">Raw data containing the extent tree node</param>
    /// <param name="dataOffset">Offset within data to start parsing</param>
    /// <param name="blockList">List to add leaf extents to</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseExtentNode(byte[] data, int dataOffset, List<(ulong physicalBlock, uint length)> blockList)
    {
        if(dataOffset + 12 > data.Length) return ErrorNumber.InvalidArgument;

        // Parse extent header (12 bytes)
        ExtentHeader header =
            Marshal.ByteArrayToStructureLittleEndian<ExtentHeader>(data, dataOffset, Marshal.SizeOf<ExtentHeader>());

        if(header.magic != EXT4_EXTENT_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid extent magic: 0x{0:X4}, expected 0x{1:X4}",
                              header.magic,
                              EXT4_EXTENT_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        int entryOffset = dataOffset + Marshal.SizeOf<ExtentHeader>();

        if(header.depth == 0)
        {
            // Leaf level: entries are ext4_extent structs
            int extentSize = Marshal.SizeOf<Extent>();

            for(ushort i = 0; i < header.entries; i++)
            {
                if(entryOffset + extentSize > data.Length) break;

                Extent extent = Marshal.ByteArrayToStructureLittleEndian<Extent>(data, entryOffset, extentSize);

                ulong physBlock = (ulong)extent.start_hi << 32 | extent.start_lo;

                // len MSB is the unwritten flag, mask it off for the actual length
                var len = (uint)(extent.len & 0x7FFF);

                if(len == 0) len = 0x8000;

                blockList.Add((physBlock, len));

                entryOffset += extentSize;
            }
        }
        else
        {
            // Internal node: entries are ext4_extent_idx structs
            int idxSize = Marshal.SizeOf<ExtentIndex>();

            for(ushort i = 0; i < header.entries; i++)
            {
                if(entryOffset + idxSize > data.Length) break;

                ExtentIndex idx = Marshal.ByteArrayToStructureLittleEndian<ExtentIndex>(data, entryOffset, idxSize);

                ulong leafBlock = (ulong)idx.leaf_hi << 32 | idx.leaf_lo;

                // Read the next level block
                ErrorNumber errno = ReadBytes(leafBlock * _blockSize, _blockSize, out byte[] leafData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error reading extent index block {0}: {1}", leafBlock, errno);

                    entryOffset += idxSize;

                    continue;
                }

                // Recurse into the next level
                errno = ParseExtentNode(leafData, 0, blockList);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Error parsing extent node at block {0}: {1}", leafBlock, errno);

                    entryOffset += idxSize;

                    continue;
                }

                entryOffset += idxSize;
            }
        }

        return ErrorNumber.NoError;
    }
}