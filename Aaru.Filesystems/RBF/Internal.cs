// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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
public sealed partial class RBF
{
    /// <summary>Cached directory entry with file descriptor information</summary>
    internal sealed class CachedDirectoryEntry
    {
        /// <summary>Filename</summary>
        internal string Name { get; init; }

        /// <summary>LSN of the file descriptor</summary>
        internal uint FdLsn { get; init; }

        /// <summary>Cached file descriptor</summary>
        internal FileDescriptor Fd { get; init; }

        /// <summary>Is this entry a directory?</summary>
        internal bool IsDirectory => (Fd.fd_att & 0x80) != 0;

        /// <summary>File size in bytes</summary>
        internal uint FileSize { get; init; }
    }

    /// <summary>DirNode implementation for RBF</summary>
    public sealed class RbfDirNode : IDirNode
    {
        /// <summary>Array of sorted filenames in the directory</summary>
        internal string[] Contents;

        /// <summary>LSN of this directory's FD</summary>
        internal uint DirectoryFdLsn;

        /// <summary>Current position in the directory contents array</summary>
        internal int Position;

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>FileNode implementation for RBF</summary>
    public sealed class RbfFileNode : IFileNode
    {
        /// <summary>File descriptor</summary>
        internal FileDescriptor Fd { get; set; }

        /// <summary>LSN of the file descriptor</summary>
        internal uint FdLsn { get; init; }

        /// <summary>Cached segment list</summary>
        internal List<(uint lsn, uint sectors)> Segments { get; set; }

        /// <inheritdoc />
        public long Offset { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }
    }
}