// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
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
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // f_blocks = block_count - segment0_blkaddr (same as kernel's f2fs_statfs)
        ulong totalBlocks = _superblock.block_count - _superblock.segment0_blkaddr;

        // f_bfree = user_block_count - valid_block_count
        ulong freeBlocks = _checkpoint.user_block_count > _checkpoint.valid_block_count
                               ? _checkpoint.user_block_count - _checkpoint.valid_block_count
                               : 0;

        stat = new FileSystemInfo
        {
            Blocks         = totalBlocks,
            FreeBlocks     = freeBlocks,
            Files          = _checkpoint.valid_inode_count,
            FreeFiles      = 0,
            FilenameLength = F2FS_NAME_LEN,
            Type           = FS_TYPE,
            PluginId       = Id,
            Id =
            {
                IsGuid = true,
                uuid   = _superblock.uuid
            }
        };

        return ErrorNumber.NoError;
    }
}