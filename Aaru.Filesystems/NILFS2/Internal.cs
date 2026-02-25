// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

using System.Collections.Generic;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
#region Nested type: DirectoryEntryInfo

    /// <summary>Cached directory entry information</summary>
    sealed class DirectoryEntryInfo
    {
        /// <summary>Entry name</summary>
        public string Name { get; init; }

        /// <summary>Inode number</summary>
        public ulong InodeNumber { get; init; }

        /// <summary>Entry type</summary>
        public FileType Type { get; init; }
    }

#endregion

#region Nested type: Nilfs2DirNode

    /// <summary>Directory node for traversing directories</summary>
    sealed class Nilfs2DirNode : IDirNode
    {
        /// <summary>Current position in the directory listing</summary>
        public int Position { get; set; }

        /// <summary>Cached directory entries</summary>
        public Dictionary<string, DirectoryEntryInfo> Entries { get; set; }

        /// <summary>Entry names for ordered enumeration</summary>
        public string[] EntryNames { get; set; }

        /// <inheritdoc />
        public string Path { get; set; }
    }

#endregion
}