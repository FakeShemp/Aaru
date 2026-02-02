// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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

/// <summary>Implements detection of the Be (new) filesystem</summary>
public sealed partial class BeFS
{
    /// <summary>Directory node for enumerating directory contents</summary>
    public sealed class BefsDirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Array of directory entry names in this directory</summary>
        internal string[] Entries { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>File node for reading file contents with streaming support</summary>
    /// <remarks>
    ///     Tracks the current read position and file metadata without caching entire file contents.
    ///     Supports efficient streaming reads of any file size.
    /// </remarks>
    sealed class BefsFileNode : IFileNode
    {
        /// <summary>The file's i-node containing metadata</summary>
        internal bfs_inode Inode { get; set; }

        /// <summary>The file's data stream for reading file blocks</summary>
        internal data_stream DataStream { get; set; }
        /// <summary>Current read position in the file</summary>
        public long Offset { get; set; }

        /// <summary>Total file size in bytes</summary>
        public long Length { get; set; }

        /// <summary>Path to the file</summary>
        public string Path { get; init; }
    }
}