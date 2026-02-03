// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Xia filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class Xia
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "StatFs: returning filesystem statistics");

        stat = new FileSystemInfo
        {
            Blocks         = _superblock.s_nzones,
            FreeBlocks     = _superblock.s_nzones - _superblock.s_ndatazones,
            FilenameLength = XIAFS_NAME_LEN,
            Type           = FS_TYPE,
            Files          = _superblock.s_ninodes,
            FreeFiles      = 0,
            PluginId       = Id
        };

        AaruLogging.Debug(MODULE_NAME,
                          "StatFs complete: totalZones={0}, dataZones={1}, inodes={2}, zoneSize={3}",
                          _superblock.s_nzones,
                          _superblock.s_ndatazones,
                          _superblock.s_ninodes,
                          _superblock.s_zone_size);

        return ErrorNumber.NoError;
    }
}