// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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

public sealed partial class F2FS
{
    /// <summary>Directory node for enumerating directory contents</summary>
    sealed class F2fsDirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Array of directory entry names in this directory</summary>
        internal string[] Entries { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>File node for reading file contents with single-block caching</summary>
    sealed class F2fsFileNode : IFileNode
    {
        /// <summary>The file's node ID (inode number)</summary>
        internal uint InodeNumber { get; init; }

        /// <summary>The file's on-disk inode (contains block mapping and inline flags)</summary>
        internal Inode Inode { get; init; }

        /// <summary>Number of usable direct address slots in the inode</summary>
        internal int AddrsPerInode { get; init; }

        /// <summary>Whether this file uses inline data stored in the inode itself</summary>
        internal bool HasInlineData { get; init; }

        /// <summary>Byte offset where inline data begins within the raw node block</summary>
        internal int InlineDataOffset { get; init; }

        /// <summary>Maximum size of inline data in bytes</summary>
        internal int InlineDataSize { get; init; }

        /// <summary>Raw node block bytes (only retained when <see cref="HasInlineData" /> is true)</summary>
        internal byte[] NodeBlock { get; set; }

        /// <summary>Cached block data from the last read (single block, not the whole file)</summary>
        internal byte[] CachedBlock { get; set; }

        /// <summary>Logical block index of the cached block (-1 if none)</summary>
        internal long CachedBlockIndex { get; set; } = -1;

        /// <summary>Whether this file uses F2FS compression (F2FS_COMPR_FL)</summary>
        internal bool IsCompressed { get; init; }

        /// <summary>Compression algorithm: 0=LZO, 1=LZ4, 2=ZSTD, 3=LZO-RLE</summary>
        internal byte CompressAlgorithm { get; init; }

        /// <summary>log2(cluster_size) — number of pages per compression cluster</summary>
        internal byte LogClusterSize { get; init; }

        /// <summary>Number of pages per compression cluster (1 &lt;&lt; LogClusterSize)</summary>
        internal int ClusterSize { get; init; }

        /// <summary>Decompressed cluster data cache (one cluster at a time)</summary>
        internal byte[] CachedCluster { get; set; }

        /// <summary>Cluster index of the cached decompressed cluster (-1 if none)</summary>
        internal long CachedClusterIndex { get; set; } = -1;

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }

        /// <inheritdoc />
        public long Length { get; init; }
    }
}