// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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
using System.Security.Cryptography;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class NintendoPlugin
{
#region Nested type: NintendoDirNode

    sealed class NintendoDirNode : IDirNode
    {
        internal string[] Contents;
        internal int      Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#endregion

#region Nested type: NintendoFileNode

    sealed class NintendoFileNode : IFileNode
    {
        /// <summary>FST index for this file entry (negative for virtual files)</summary>
        internal int FstIndex;

        /// <summary>Index into _partitions identifying which partition this file belongs to</summary>
        internal int PartitionIndex;

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

#region Nested type: PartitionInfo

    /// <summary>Per-partition state for multi-partition support (Wii discs may have DATA, UPDATE, CHANNEL partitions)</summary>
    sealed class PartitionInfo
    {
        /// <summary>Cache of subdirectory entries: path → (filename → FST index)</summary>
        internal readonly Dictionary<string, Dictionary<string, int>> DirectoryCache = new();

        /// <summary>Cache of root directory entries: filename → FST index</summary>
        internal readonly Dictionary<string, int> RootDirectoryCache = new();

        /// <summary>Partition's internal disc header</summary>
        internal DiscHeader DiscHeader;

        /// <summary>Offset of the DOL executable within partition data</summary>
        internal uint DolOffset;

        /// <summary>Calculated size of the DOL executable</summary>
        internal uint DolSize;

        /// <summary>Parsed FST entries for this partition</summary>
        internal FstEntry[] FstEntries;

        /// <summary>Parsed FST entry names for this partition</summary>
        internal string[] FstNames;
        /// <summary>Display name for the partition (e.g., "DATA", "UPDATE", "CHANNEL")</summary>
        internal string Name;

        /// <summary>AES cipher for decrypting partition data (Wii only, null for GameCube)</summary>
        internal Aes PartitionAes;

        /// <summary>Offset of the data area within the partition (Wii only)</summary>
        internal ulong PartitionDataOffset;

        /// <summary>Absolute offset of the partition on disc (Wii only)</summary>
        internal ulong PartitionOffset;

        /// <summary>Partition type value (0 = DATA, 1 = UPDATE, 2 = CHANNEL)</summary>
        internal uint Type;
    }

#endregion
}