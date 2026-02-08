// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
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

using System.Collections.Generic;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Cram
{
    /// <summary>Internal representation of a directory entry</summary>
    sealed class DirectoryEntryInfo
    {
        /// <summary>Filename</summary>
        public string Name { get; set; }

        /// <summary>The cramfs inode</summary>
        public Inode Inode { get; set; }

        /// <summary>Byte offset of this entry in the filesystem</summary>
        public uint Offset { get; set; }
    }

    /// <summary>Directory node for traversing directories</summary>
    sealed class CramDirNode : IDirNode
    {
        /// <summary>Current position in the directory listing</summary>
        public int Position { get; set; }

        /// <summary>Cached directory entries</summary>
        public Dictionary<string, DirectoryEntryInfo> Entries { get; set; }

        /// <summary>Entry names for enumeration</summary>
        public string[] EntryNames { get; set; }
        /// <inheritdoc />
        public string Path { get; set; }
    }

    /// <summary>File node for reading file contents</summary>
    sealed class CramFileNode : IFileNode
    {
        /// <summary>The cramfs inode</summary>
        internal Inode Inode { get; set; }

        /// <summary>Byte offset of block pointers in the filesystem (inode.Offset &lt;&lt; 2)</summary>
        internal uint BlockPtrOffset { get; set; }

        /// <summary>Number of blocks in the file (ceiling of size / PAGE_SIZE)</summary>
        internal uint BlockCount { get; set; }

        /// <inheritdoc />
        public string Path { get; set; }

        /// <summary>Current read position within the file</summary>
        public long Offset { get; set; }

        /// <summary>File length in bytes</summary>
        public long Length { get; set; }
    }
}