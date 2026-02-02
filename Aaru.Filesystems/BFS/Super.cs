// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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
using Aaru.CommonTypes.Structs;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from Practical Filesystem Design, ISBN 1-55860-497-9
/// <inheritdoc />
public sealed partial class BeFS
{
    /// <summary>Returns filesystem statistics</summary>
    /// <remarks>
    ///     Provides information about the BeFS volume including total blocks, free blocks,
    ///     block size, and other filesystem metadata from the cached superblock.
    /// </remarks>
    /// <param name="stat">Output filesystem information</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "StatFs: returning filesystem statistics");

        // Calculate free blocks
        long freeBlocks = _superblock.num_blocks - _superblock.used_blocks;

        // Create filesystem info from superblock data
        stat = new FileSystemInfo
        {
            Blocks         = (ulong)_superblock.num_blocks,
            FreeBlocks     = (ulong)freeBlocks,
            FilenameLength = 255,
            Type           = FS_TYPE,
            Files          = 0, // Not tracked in BeFS superblock
            FreeFiles      = 0  // Not tracked in BeFS superblock
        };

        AaruLogging.Debug(MODULE_NAME,
                          "StatFs complete: totalBlocks={0}, freeBlocks={1}, blockSize={2}",
                          stat.Blocks,
                          stat.FreeBlocks,
                          _superblock.block_size);

        return ErrorNumber.NoError;
    }
}