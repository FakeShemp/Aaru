// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
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
public sealed partial class HPFS
{
#region Nested type: HpfsDirNode

    /// <summary>Directory node implementation for HPFS directory traversal.</summary>
    sealed class HpfsDirNode : IDirNode
    {
        /// <summary>Array of cached directory entry information (filename, fnode).</summary>
        internal (string Filename, uint Fnode)[] Entries;

        /// <summary>Current position in the directory contents array.</summary>
        internal int Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#endregion

#region Nested type: HpfsFileNode

    /// <summary>File node implementation for HPFS file reading.</summary>
    sealed class HpfsFileNode : IFileNode
    {
        /// <summary>Fnode sector number for this file.</summary>
        internal uint Fnode;

        /// <summary>Cached fnode structure for B+ tree traversal.</summary>
        internal FNode FnodeData;

#region IFileNode Members

        /// <inheritdoc />
        public string Path { get; init; }
        /// <inheritdoc />
        public long Length { get; init; }
        /// <inheritdoc />
        public long Offset { get; set; }

#endregion
    }

#endregion
}