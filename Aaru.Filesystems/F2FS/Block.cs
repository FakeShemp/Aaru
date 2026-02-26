// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
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

using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <summary>
    ///     Validates that a data block address falls within the main area.
    ///     Matches the kernel's DATA_GENERIC check in __f2fs_is_valid_blkaddr():
    ///     blkaddr must be >= main_blkaddr and &lt; MAX_BLKADDR.
    /// </summary>
    bool IsValidDataBlockAddress(uint blkAddr)
    {
        if(blkAddr == NULL_ADDR || blkAddr == NEW_ADDR || blkAddr == COMPRESS_ADDR) return false;

        return blkAddr >= _superblock.main_blkaddr && blkAddr < _maxBlockAddr;
    }

    /// <summary>Reads a single filesystem block by its block number</summary>
    ErrorNumber ReadBlock(uint blockNumber, out byte[] blockData)
    {
        blockData = null;

        // Reject blocks outside the filesystem: must be within [segment0_blkaddr, maxBlockAddr)
        // This allows both metadata area (NAT, SIT, SSA) and main data area blocks.
        if(blockNumber != 0 && (blockNumber < _superblock.segment0_blkaddr || blockNumber >= _maxBlockAddr))
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ReadBlock: block address {0} outside filesystem range [{1}, {2})",
                              blockNumber,
                              _superblock.segment0_blkaddr,
                              _maxBlockAddr);

            return ErrorNumber.InvalidArgument;
        }

        // Convert block number to sector address
        uint  sectorsPerBlock = _blockSize / _imagePlugin.Info.SectorSize;
        ulong sectorAddr      = _partition.Start + (ulong)blockNumber * sectorsPerBlock;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddr, false, sectorsPerBlock, out blockData, out _);

        return errno;
    }
}