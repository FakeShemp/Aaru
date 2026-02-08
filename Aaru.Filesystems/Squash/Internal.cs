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

using System.Collections.Generic;
using Aaru.CommonTypes.Interfaces;

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

#region Nested type: SquashDirNode

    /// <summary>Directory node for traversing directories</summary>
    sealed class SquashDirNode : IDirNode
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

#endregion

#region Nested type: SquashFileNode

    /// <summary>File node for reading file contents</summary>
    sealed class SquashFileNode : IFileNode
    {
        /// <summary>Start block of file data (absolute position)</summary>
        public ulong StartBlock { get; set; }

        /// <summary>Fragment index (or SQUASHFS_INVALID_FRAG if no fragment)</summary>
        public uint Fragment { get; set; }

        /// <summary>Offset within fragment</summary>
        public uint FragmentOffset { get; set; }

        /// <summary>Block list metadata block (relative to inode table)</summary>
        public uint BlockListStart { get; set; }

        /// <summary>Offset within block list metadata block</summary>
        public ushort BlockListOffset { get; set; }

        /// <summary>Number of data blocks (excluding fragment)</summary>
        public uint BlockCount { get; set; }

        /// <summary>Whether this is an extended file inode</summary>
        public bool IsExtended { get; set; }

        /// <summary>Sparse file indicator (only for extended inodes)</summary>
        public ulong Sparse { get; set; }

        /// <summary>Cached decompressed block data (for read-ahead)</summary>
        public byte[] CachedBlock { get; set; }

        /// <summary>Index of the cached block (-1 if none)</summary>
        public int CachedBlockIndex { get; set; } = -1;

        /// <summary>Cached fragment data</summary>
        public byte[] CachedFragment { get; set; }
        /// <inheritdoc />
        public string Path { get; set; }

        /// <summary>Current read position within the file</summary>
        public long Offset { get; set; }

        /// <summary>File length in bytes</summary>
        public long Length { get; set; }
    }

#endregion
}