// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
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

using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class SysVfs
{
    /// <summary>Directory node for enumerating directory contents</summary>
    sealed class SysVDirNode : IDirNode
    {
        /// <summary>Sorted array of filenames in this directory</summary>
        internal string[] Contents;

        /// <summary>Inode number of this directory</summary>
        internal ushort InodeNumber;

        /// <summary>Current position in the directory contents array</summary>
        internal int Position;

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>File node for reading file contents block by block</summary>
    sealed class SysVFileNode : IFileNode
    {
        /// <summary>The file's inode number</summary>
        internal ushort InodeNumber { get; init; }

        /// <summary>Raw di_addr array from the inode (39 bytes containing 13 × 3-byte addresses)</summary>
        internal byte[] DiAddr { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }
    }
}