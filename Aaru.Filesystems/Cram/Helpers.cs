// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
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

namespace Aaru.Filesystems;

public sealed partial class Cram
{
    /// <summary>Gets the file mode from an inode based on current endianness</summary>
    /// <remarks>
    ///     Little-endian layout: mode in bits 0-15 of modeUid
    ///     Big-endian layout: mode in bits 16-31 of modeUid
    /// </remarks>
    ushort GetInodeMode(Inode inode) =>
        _littleEndian ? (ushort)(inode.modeUid & 0xFFFF) : (ushort)(inode.modeUid >> 16);

    /// <summary>Gets the user ID from an inode based on current endianness</summary>
    /// <remarks>
    ///     Little-endian layout: uid in bits 16-31 of modeUid
    ///     Big-endian layout: uid in bits 0-15 of modeUid
    /// </remarks>
    ushort GetInodeUid(Inode inode) => _littleEndian ? (ushort)(inode.modeUid >> 16) : (ushort)(inode.modeUid & 0xFFFF);

    /// <summary>Gets the file size from an inode based on current endianness</summary>
    /// <remarks>
    ///     Little-endian layout: size in bits 0-23 of sizeGid
    ///     Big-endian layout: size in bits 8-31 of sizeGid
    /// </remarks>
    uint GetInodeSize(Inode inode) => _littleEndian ? inode.sizeGid & 0xFFFFFF : inode.sizeGid >> 8;

    /// <summary>Gets the group ID from an inode based on current endianness</summary>
    /// <remarks>
    ///     Little-endian layout: gid in bits 24-31 of sizeGid
    ///     Big-endian layout: gid in bits 0-7 of sizeGid
    /// </remarks>
    byte GetInodeGid(Inode inode) => _littleEndian ? (byte)(inode.sizeGid >> 24) : (byte)(inode.sizeGid & 0xFF);

    /// <summary>Gets the name length from an inode based on current endianness</summary>
    /// <remarks>
    ///     Little-endian layout: namelen in bits 0-5 of namelenOffset
    ///     Big-endian layout: namelen in bits 26-31 of namelenOffset
    /// </remarks>
    byte GetInodeNameLen(Inode inode) => _littleEndian
                                             ? (byte)(inode.namelenOffset & 0x3F)
                                             : (byte)(inode.namelenOffset >> 26);

    /// <summary>Gets the data offset from an inode based on current endianness</summary>
    /// <remarks>
    ///     Little-endian layout: offset in bits 6-31 of namelenOffset
    ///     Big-endian layout: offset in bits 0-25 of namelenOffset
    /// </remarks>
    uint GetInodeOffset(Inode inode) => _littleEndian ? inode.namelenOffset >> 6 : inode.namelenOffset & 0x3FFFFFF;
}