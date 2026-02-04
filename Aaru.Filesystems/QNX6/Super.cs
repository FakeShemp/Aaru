// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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
public sealed partial class QNX6
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "StatFs: returning filesystem statistics");

        stat = new FileSystemInfo
        {
            Blocks         = _superblock.sb_num_blocks,
            FreeBlocks     = _superblock.sb_free_blocks,
            Files          = _superblock.sb_num_inodes,
            FreeFiles      = _superblock.sb_free_inodes,
            FilenameLength = QNX6_LONG_NAME_MAX,
            Type           = FS_TYPE,
            PluginId       = Id
        };

        AaruLogging.Debug(MODULE_NAME,
                          "StatFs complete: blocks={0}, freeBlocks={1}, inodes={2}, freeInodes={3}",
                          _superblock.sb_num_blocks,
                          _superblock.sb_free_blocks,
                          _superblock.sb_num_inodes,
                          _superblock.sb_free_inodes);

        return ErrorNumber.NoError;
    }
}