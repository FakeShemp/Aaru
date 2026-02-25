// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
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
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    /// <summary>Reads a physical block from the volume</summary>
    /// <param name="physBlockNr">Physical block number</param>
    /// <param name="data">Buffer to receive the block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadPhysicalBlock(ulong physBlockNr, out byte[] data)
    {
        data = null;

        ulong bytesPerSector  = _imagePlugin.Info.SectorSize;
        ulong blockByteOffset = physBlockNr     * _blockSize;
        ulong sectorOffset    = blockByteOffset / bytesPerSector;
        var   sectorsPerBlock = (uint)(_blockSize / bytesPerSector);

        if(_blockSize % bytesPerSector != 0) sectorsPerBlock++;

        ErrorNumber errno =
            _imagePlugin.ReadSectors(_partition.Start + sectorOffset, false, sectorsPerBlock, out data, out _);

        return errno;
    }

    /// <summary>
    ///     Reads a logical block from a file, resolving the block mapping and optionally
    ///     translating virtual block numbers through the DAT
    /// </summary>
    /// <param name="inode">Inode of the file to read from</param>
    /// <param name="logicalBlock">Logical block offset within the file</param>
    /// <param name="isRootMetadata">
    ///     If true, bmap values are physical block numbers (for DAT, cpfile, sufile).
    ///     If false, bmap values are virtual block numbers that need DAT translation.
    /// </param>
    /// <param name="data">Output block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadLogicalBlock(in Inode inode, ulong logicalBlock, bool isRootMetadata, out byte[] data)
    {
        data = null;

        ErrorNumber errno = ResolveBmap(inode, logicalBlock, isRootMetadata, out ulong blockNr);

        if(errno != ErrorNumber.NoError) return errno;

        ulong physicalBlock;

        if(isRootMetadata)
            physicalBlock = blockNr;
        else
        {
            errno = TranslateVirtualBlock(blockNr, out physicalBlock);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ReadPhysicalBlock(physicalBlock, out data);
    }

    /// <summary>Translates a virtual block number to a physical block number using the DAT</summary>
    /// <param name="vblocknr">Virtual block number</param>
    /// <param name="physicalBlock">Output physical block number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber TranslateVirtualBlock(ulong vblocknr, out ulong physicalBlock)
    {
        physicalBlock = 0;

        // The DAT uses the persistent allocator (palloc) layout
        uint  datEntrySize    = _superblock.dat_entry_size;
        uint  entriesPerBlock = _blockSize        / datEntrySize;
        ulong entriesPerGroup = (ulong)_blockSize * 8;
        uint  blocksPerGroup  = (uint)((entriesPerGroup + entriesPerBlock - 1) / entriesPerBlock) + 2;

        ulong group         = vblocknr / entriesPerGroup;
        ulong groupOffset   = vblocknr % entriesPerGroup;
        ulong logicalBlock  = group * blocksPerGroup + 2 + groupOffset / entriesPerBlock;
        var   offsetInBlock = (uint)(groupOffset % entriesPerBlock * datEntrySize);

        // DAT is a root metadata file, so its bmap values are physical block numbers
        ErrorNumber errno = ResolveBmap(_datInode, logicalBlock, true, out ulong datBlockNr);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ReadPhysicalBlock(datBlockNr, out byte[] blockData);

        if(errno != ErrorNumber.NoError) return errno;

        int datEntryStructSize = Marshal.SizeOf<DatEntry>();

        if(offsetInBlock + datEntryStructSize > blockData.Length) return ErrorNumber.InvalidArgument;

        DatEntry entry = Marshal.ByteArrayToStructureLittleEndian<DatEntry>(blockData,
                                                                            (int)offsetInBlock,
                                                                            datEntryStructSize);

        physicalBlock = entry.blocknr;

        return physicalBlock == 0 ? ErrorNumber.InvalidArgument : ErrorNumber.NoError;
    }
}