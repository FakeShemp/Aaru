// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : IBM JFS filesystem plugin
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

// ReSharper disable UnusedMember.Local

namespace Aaru.Filesystems;

public sealed partial class JFS
{
    const uint JFS_BOOT_BLOCKS_SIZE = 0x8000;
    const uint JFS_MAGIC            = 0x3153464A;

    const string FS_TYPE = "jfs";

    /// <summary>Number of disk inode extent per IAG</summary>
    const int EXTSPERIAG = 128;

    /// <summary>Number of words per summary map</summary>
    const int SMAPSZ = 4;

    /// <summary>Maximum number of allocation groups</summary>
    const int MAXAG = 128;

    /// <summary>Size of a dmap tree</summary>
    const int TREESIZE = 256 + 64 + 16 + 4 + 1; // 341

    /// <summary>Size of a dmapctl tree</summary>
    const int CTLTREESIZE = 1024 + 256 + 64 + 16 + 4 + 1; // 1365

    /// <summary>Number of leaves per dmap tree</summary>
    const int LPERDMAP = 256;

    /// <summary>Maximum number of active file systems sharing the log</summary>
    const int MAX_ACTIVE = 128;

    /// <summary>Log page size in bytes</summary>
    const int LOGPSIZE = 4096;
}