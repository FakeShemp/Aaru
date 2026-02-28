// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
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

using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ulong totalBlocks = _is64Bit ? (ulong)_superblock.blocks_hi << 32 | _superblock.blocks : _superblock.blocks;

        ulong freeBlocks = _is64Bit
                               ? (ulong)_superblock.free_blocks_hi << 32 | _superblock.free_blocks
                               : _superblock.free_blocks;

        stat = new FileSystemInfo
        {
            Blocks         = totalBlocks,
            FilenameLength = 255,
            Files          = _superblock.inodes,
            FreeBlocks     = freeBlocks,
            FreeFiles      = _superblock.free_inodes,
            PluginId       = Id,
            Type           = Metadata.Type,
            Id =
            {
                IsGuid = true,
                uuid   = _superblock.uuid
            }
        };

        return ErrorNumber.NoError;
    }
}