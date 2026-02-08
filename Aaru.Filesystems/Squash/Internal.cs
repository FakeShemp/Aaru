// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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

/// <inheritdoc />
public sealed partial class Squash
{
#region Nested type: DirectoryEntryInfo

    /// <summary>Cached directory entry information</summary>
    sealed class DirectoryEntryInfo
    {
        /// <summary>Entry name</summary>
        public string Name { get; init; }

        /// <summary>Inode number</summary>
        public uint InodeNumber { get; init; }

        /// <summary>Entry type</summary>
        public SquashInodeType Type { get; init; }

        /// <summary>Block containing the inode (relative to inode table)</summary>
        public uint InodeBlock { get; init; }

        /// <summary>Offset within the inode block</summary>
        public ushort InodeOffset { get; init; }
    }

#endregion
}