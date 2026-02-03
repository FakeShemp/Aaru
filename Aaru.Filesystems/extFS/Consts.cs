// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />

// ReSharper disable once InconsistentNaming
public sealed partial class extFS
{
    const int SB_POS = 0x400;

    /// <summary>ext superblock magic</summary>
    const ushort EXT_MAGIC = 0x137D;

    /// <summary>ext block size is always 1024 bytes</summary>
    const uint EXT_BLOCK_SIZE = 1024;

    /// <summary>Number of addresses per indirect block (1024/4)</summary>
    const uint EXT_ADDR_PER_BLOCK = 256;

    const int EXT_NAME_LEN = 255;
    const int EXT_ROOT_INO = 1;

    const string FS_TYPE = "ext";
}