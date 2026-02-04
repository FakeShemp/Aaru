// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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

/// <inheritdoc />
public sealed partial class BFS
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "StatFs: returning filesystem statistics");

        // Calculate total blocks and free blocks
        uint totalBlocks = (_superblock.s_end     + 1)                   / BFS_BSIZE;
        uint freeBlocks  = (_superblock.s_end + 1 - _superblock.s_start) / BFS_BSIZE;

        // Count free inodes
        uint freeInodes = 0;

        for(uint i = BFS_ROOT_INO; i <= _lastInode; i++)
        {
            if(!_inodeCache.ContainsKey(i))
            {
                ErrorNumber errno = ReadInode(i, out Inode inode);

                if(errno == ErrorNumber.NoError && inode.i_ino == 0) freeInodes++;
            }
        }

        stat = new FileSystemInfo
        {
            Blocks         = totalBlocks,
            FreeBlocks     = freeBlocks,
            Files          = _lastInode - BFS_ROOT_INO + 1,
            FreeFiles      = freeInodes,
            FilenameLength = BFS_NAMELEN,
            Type           = FS_TYPE,
            PluginId       = Id
        };

        AaruLogging.Debug(MODULE_NAME,
                          "StatFs complete: blocks={0}, freeBlocks={1}, files={2}, freeFiles={3}",
                          totalBlocks,
                          freeBlocks,
                          stat.Files,
                          freeInodes);

        return ErrorNumber.NoError;
    }
}