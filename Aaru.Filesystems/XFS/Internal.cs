// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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

public sealed partial class XFS
{
    /// <summary>Directory node for enumerating directory contents</summary>
    sealed class XfsDirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Array of directory entry names in this directory</summary>
        internal string[] Entries { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>Represents a single BMBT extent mapping a range of file logical blocks to physical blocks</summary>
    readonly struct XfsExtent
    {
        /// <summary>Starting file offset in filesystem blocks</summary>
        internal ulong StartOff { get; init; }

        /// <summary>Starting filesystem block number</summary>
        internal ulong StartBlock { get; init; }

        /// <summary>Number of blocks in this extent</summary>
        internal uint BlockCount { get; init; }

        /// <summary>Whether this is an unwritten (preallocated) extent that should return zeros</summary>
        internal bool Unwritten { get; init; }
    }

    /// <summary>File node for reading file contents with cached extent list</summary>
    sealed class XfsFileNode : IFileNode
    {
        /// <summary>The file's inode number</summary>
        internal ulong InodeNumber { get; init; }

        /// <summary>The file's inode structure</summary>
        internal Dinode Inode { get; init; }

        /// <summary>Sorted array of extents mapping logical to physical blocks</summary>
        internal XfsExtent[] Extents { get; set; }

        /// <inheritdoc />
        public long Offset { get; set; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }
    }
}