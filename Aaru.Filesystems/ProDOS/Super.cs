// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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

// Information from Apple ProDOS 8 Technical Reference
/// <inheritdoc />
public sealed partial class ProDOSPlugin
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Count free blocks by reading the volume bitmap
        ulong freeBlocks = CountFreeBlocks();

        stat = new FileSystemInfo
        {
            Blocks         = _totalBlocks,
            FilenameLength = 15, // ProDOS maximum filename length
            Files          = _volumeHeader.entry_count,
            FreeBlocks     = freeBlocks,
            FreeFiles      = 0, // ProDOS doesn't have a fixed file count limit
            PluginId       = Id,
            Type           = FS_TYPE
        };

        return ErrorNumber.NoError;
    }
}