// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
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

public sealed partial class SysVfs
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Total inodes = inode blocks * inodes per block
        long totalInodes = (_firstDataZone - FIRST_INODE_ZONE) * _inodesPerBlock;

        string fsType = _variant switch
                        {
                            SysVVariant.Xenix     => FS_TYPE_XENIX,
                            SysVVariant.Xenix3    => FS_TYPE_XENIX3,
                            SysVVariant.SystemVR4 => FS_TYPE_SVR4,
                            SysVVariant.SystemVR2 => FS_TYPE_SVR2,
                            SysVVariant.ScoAfs    => FS_TYPE_AFS,
                            SysVVariant.Coherent  => FS_TYPE_COHERENT,
                            SysVVariant.UnixV7    => FS_TYPE_UNIX7,
                            _                     => FS_TYPE_SVR4
                        };

        stat = new FileSystemInfo
        {
            Blocks         = (ulong)_totalZones,
            FreeBlocks     = (ulong)_freeBlocks,
            Files          = (ulong)totalInodes,
            FreeFiles      = (ulong)_freeInodes,
            FilenameLength = DIRSIZE,
            Type           = fsType,
            PluginId       = Id
        };

        return ErrorNumber.NoError;
    }
}