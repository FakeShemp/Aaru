// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the CRAM filesystem</summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Cram
{
    /// <summary>CramFS magic number (little-endian)</summary>
    const uint CRAM_MAGIC = 0x28CD3D45;

    /// <summary>CramFS magic number (big-endian)</summary>
    const uint CRAM_CIGAM = 0x453DCD28;

    /// <summary>CramFS signature string</summary>
    const string CRAMFS_SIGNATURE = "Compressed ROMFS";

    /// <summary>Filesystem type identifier</summary>
    const string FS_TYPE = "cramfs";

#region Inode bitfield widths

    /// <summary>Width of mode field in cramfs_inode (16 bits)</summary>
    const int CRAMFS_MODE_WIDTH = 16;

    /// <summary>Width of uid field in cramfs_inode (16 bits)</summary>
    const int CRAMFS_UID_WIDTH = 16;

    /// <summary>Width of size field in cramfs_inode (24 bits)</summary>
    const int CRAMFS_SIZE_WIDTH = 24;

    /// <summary>Width of gid field in cramfs_inode (8 bits)</summary>
    const int CRAMFS_GID_WIDTH = 8;

    /// <summary>Width of namelen field in cramfs_inode (6 bits)</summary>
    const int CRAMFS_NAMELEN_WIDTH = 6;

    /// <summary>Width of offset field in cramfs_inode (26 bits)</summary>
    const int CRAMFS_OFFSET_WIDTH = 26;

#endregion

#region Path length limits

    /// <summary>
    ///     Maximum cramfs path length.
    ///     Since inode.namelen is a unsigned 6-bit number, the maximum is 63 &lt;&lt; 2 = 252.
    /// </summary>
    const int CRAMFS_MAXPATHLEN = (1 << CRAMFS_NAMELEN_WIDTH) - 1 << 2;

#endregion

#region Block pointer constants

    /// <summary>Block is stored uncompressed</summary>
    const uint CRAMFS_BLK_FLAG_UNCOMPRESSED = 1u << 31;

    /// <summary>Block uses direct pointer (shifted by 2 bits)</summary>
    const uint CRAMFS_BLK_FLAG_DIRECT_PTR = 1u << 30;

    /// <summary>Mask for all block pointer flags</summary>
    const uint CRAMFS_BLK_FLAGS = CRAMFS_BLK_FLAG_UNCOMPRESSED | CRAMFS_BLK_FLAG_DIRECT_PTR;

    /// <summary>
    ///     Direct blocks are at least 4-byte aligned.
    ///     Pointers to direct blocks are shifted down by 2 bits.
    /// </summary>
    const int CRAMFS_BLK_DIRECT_PTR_SHIFT = 2;

#endregion
}