// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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

/// <inheritdoc />
public sealed partial class QNX6
{
    /// <summary>Directory node for enumerating directory contents</summary>
    sealed class QNX6DirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Array of directory entry names in this directory</summary>
        internal string[] Entries { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>File node for reading file contents with streaming support</summary>
    /// <remarks>
    ///     Tracks the current read position and inode data without caching entire file contents.
    ///     Supports efficient streaming reads of any file size.
    /// </remarks>
    sealed class QNX6FileNode : IFileNode
    {
        /// <summary>The file's inode entry containing metadata and block pointers</summary>
        internal qnx6_inode_entry Inode { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }
    }
}