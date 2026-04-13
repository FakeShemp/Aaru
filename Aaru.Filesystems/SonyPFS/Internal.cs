// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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

public partial class SonyPFS
{
#region Nested type: DirEntry

    /// <summary>Cached directory entry used for in-memory directory tree.</summary>
    sealed class DirEntry
    {
        /// <summary>Inode number.</summary>
        public uint Inode;
        /// <summary>Sub-partition index.</summary>
        public ushort SubPart;
        /// <summary>File type from directory entry aLen upper bits.</summary>
        public ushort Mode;
    }

#endregion

#region Nested type: PfsDirNode

    /// <summary>Directory node for PFS directory enumeration.</summary>
    sealed class PfsDirNode : IDirNode
    {
        internal string[] Contents;
        internal int      Position;

    #region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

    #endregion
    }

#endregion

#region Nested type: PfsFileNode

    /// <summary>File node for PFS file reading.</summary>
    sealed class PfsFileNode : IFileNode
    {
        internal Inode  InodeData;
        internal uint   InodeNumber;
        internal ushort SubPart;

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