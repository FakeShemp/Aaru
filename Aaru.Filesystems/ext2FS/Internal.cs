// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
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

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    sealed class Ext2DirNode : IDirNode
    {
        internal string[] Entries  { get; set; }
        internal int      Position { get; set; }
        public   string   Path     { get; init; }
    }

    /// <summary>File node for reading ext2/3/4 file contents with single-block caching</summary>
    sealed class Ext2FileNode : IFileNode
    {
        /// <summary>The inode number</summary>
        internal uint InodeNumber { get; init; }

        /// <summary>The file's on-disk inode</summary>
        internal Inode Inode { get; init; }

        /// <summary>Pre-computed list of physical data blocks for this file</summary>
        internal List<(ulong physicalBlock, uint length)> BlockList { get; init; }

        /// <summary>Cached block data from the last read</summary>
        internal byte[] CachedBlock { get; set; }

        /// <summary>Logical block index of the cached block (-1 if none)</summary>
        internal long CachedBlockIndex { get; set; } = -1;

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }

        /// <inheritdoc />
        public long Length { get; init; }
    }
}