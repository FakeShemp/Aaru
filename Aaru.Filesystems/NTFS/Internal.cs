// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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
public sealed partial class NTFS
{
#region Nested type: NtfsDirNode

    /// <summary>Directory node implementation for NTFS directory traversal.</summary>
    sealed class NtfsDirNode : IDirNode
    {
        /// <summary>Array of cached directory entry file names.</summary>
        internal string[] Entries;

        /// <summary>Current position in the directory contents array.</summary>
        internal int Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#endregion

#region Nested type: NtfsFileNode

    /// <summary>File node implementation for NTFS file reading.</summary>
    sealed class NtfsFileNode : IFileNode
    {
        /// <summary>Cached cluster data for sequential read optimization.</summary>
        internal byte[] CachedCluster;

        /// <summary>Absolute cluster offset of the cached cluster (-1 if none cached).</summary>
        internal long CachedClusterOffset = -1;

        /// <summary>Cached decompressed compression unit data.</summary>
        internal byte[] CachedCompressionUnit;

        /// <summary>VCN of the first cluster in the cached compression unit (-1 if none cached).</summary>
        internal long CachedCompressionUnitVcn = -1;

        /// <summary>Number of clusters per compression unit (0 if not compressed).</summary>
        internal int CompressionUnitClusters;
        /// <summary>Pre-computed data run list: (absolute cluster offset, length in clusters) tuples.</summary>
        internal List<(long clusterOffset, long clusterLength)> DataRuns;

        /// <summary>Whether the file data is compressed.</summary>
        internal bool IsCompressed;

        /// <summary>Whether the file data is resident (stored in the MFT record).</summary>
        internal bool IsResident;

        /// <summary>Resident file data (small files stored entirely within the MFT record).</summary>
        internal byte[] ResidentData;

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