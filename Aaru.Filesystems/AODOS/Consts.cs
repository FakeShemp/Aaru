// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AO-DOS file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the AO-DOS file system and shows information.
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

namespace Aaru.Filesystems;

public sealed partial class AODOS
{
    const string FS_TYPE     = "aodos";
    const int    SECTOR_SIZE = 512;

    /// <summary>Size of a directory entry in bytes</summary>
    const int DIR_ENTRY_SIZE = 24;

    /// <summary>Offset of first directory entry in the boot block sector</summary>
    const int DIR_START_OFFSET = 320;

    /// <summary>Maximum number of directory entries in the boot block sector</summary>
    const int ENTRIES_IN_BLOCK_0 = (SECTOR_SIZE - DIR_START_OFFSET) / DIR_ENTRY_SIZE;

    /// <summary>Maximum number of directory entries per subsequent sector</summary>
    const int ENTRIES_PER_SECTOR = SECTOR_SIZE / DIR_ENTRY_SIZE;

    readonly byte[] _identifier = " AO-DOS "u8.ToArray();
}