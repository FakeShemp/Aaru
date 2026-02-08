// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Cram
{
#region Nested type: Inode

    /// <summary>CramFS inode structure</summary>
    /// <remarks>
    ///     The inode uses bit fields:
    ///     - mode: 16 bits (file mode/permissions)
    ///     - uid: 16 bits (user ID)
    ///     - size: 24 bits (file size, or i_rdev for device files)
    ///     - gid: 8 bits (group ID)
    ///     - namelen: 6 bits (name length divided by 4, rounded up)
    ///     - offset: 26 bits (data offset divided by 4)
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Inode
    {
        /// <summary>Lower 32 bits: mode (16 bits) | uid (16 bits)</summary>
        public uint modeUid;

        /// <summary>Middle 32 bits: size (24 bits) | gid (8 bits)</summary>
        public uint sizeGid;

        /// <summary>Upper 32 bits: namelen (6 bits) | offset (26 bits)</summary>
        public uint namelenOffset;

        /// <summary>Gets the file mode (permissions and type)</summary>
        public ushort Mode => (ushort)(modeUid & 0xFFFF);

        /// <summary>Gets the user ID</summary>
        public ushort Uid => (ushort)(modeUid >> 16);

        /// <summary>Gets the file size (or device number for device files)</summary>
        public uint Size => sizeGid & 0xFFFFFF;

        /// <summary>Gets the group ID</summary>
        public byte Gid => (byte)(sizeGid >> 24);

        /// <summary>Gets the name length (actual length = namelen * 4)</summary>
        public byte NameLen => (byte)(namelenOffset & 0x3F);

        /// <summary>Gets the data offset (actual offset = offset * 4)</summary>
        public uint Offset => namelenOffset >> 6;
    }

#endregion

#region Nested type: Info

    /// <summary>CramFS filesystem info structure (fsid)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Info
    {
        /// <summary>CRC32 of the entire filesystem</summary>
        public uint crc;

        /// <summary>Edition number (version)</summary>
        public uint edition;

        /// <summary>Number of data blocks</summary>
        public uint blocks;

        /// <summary>Number of files</summary>
        public uint files;
    }

#endregion

#region Nested type: SuperBlock

    /// <summary>CramFS superblock structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>Magic number: 0x28cd3d45</summary>
        public uint magic;

        /// <summary>Total filesystem size in bytes</summary>
        public uint size;

        /// <summary>Feature flags</summary>
        public uint flags;

        /// <summary>Reserved for future use</summary>
        public uint future;

        /// <summary>Signature: "Compressed ROMFS"</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] signature;

        /// <summary>Filesystem info (CRC, edition, blocks, files)</summary>
        public Info fsid;

        /// <summary>User-defined filesystem name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] name;

        /// <summary>Root directory inode</summary>
        public Inode root;
    }

#endregion
}