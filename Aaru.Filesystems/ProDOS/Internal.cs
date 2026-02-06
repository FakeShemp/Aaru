// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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

using System;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class ProDOSPlugin
{
    /// <summary>Cached directory entry with file information</summary>
    internal sealed class CachedEntry
    {
        /// <summary>Filename</summary>
        internal string Name { get; init; }

        /// <summary>Storage type (seedling, sapling, tree, directory, etc.)</summary>
        internal byte StorageType { get; init; }

        /// <summary>File type</summary>
        internal byte FileType { get; init; }

        /// <summary>Key block pointer</summary>
        internal ushort KeyBlock { get; init; }

        /// <summary>Blocks used by the file</summary>
        internal ushort BlocksUsed { get; init; }

        /// <summary>File size in bytes (EOF)</summary>
        internal uint Eof { get; init; }

        /// <summary>Creation time</summary>
        internal DateTime CreationTime { get; init; }

        /// <summary>Modification time</summary>
        internal DateTime ModificationTime { get; init; }

        /// <summary>Access flags</summary>
        internal byte Access { get; init; }

        /// <summary>Auxiliary type</summary>
        internal ushort AuxType { get; init; }

        /// <summary>Header (parent directory) block pointer</summary>
        internal ushort HeaderPointer { get; init; }

        /// <summary>GS/OS case bits for filename</summary>
        internal ushort CaseBits { get; init; }

        /// <summary>Is this entry a directory?</summary>
        internal bool IsDirectory => StorageType == 0x0D;
    }

    /// <summary>DirNode implementation for ProDOS</summary>
    public sealed class ProDosDirNode : IDirNode
    {
        /// <summary>Array of sorted filenames in the directory</summary>
        internal string[] Contents;

        /// <summary>Key block of this directory</summary>
        internal ushort DirectoryKeyBlock;

        /// <summary>Current position in the directory contents array</summary>
        internal int Position;

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>FileNode implementation for ProDOS</summary>
    public sealed class ProDosFileNode : IFileNode
    {
        /// <summary>Cached entry information</summary>
        internal CachedEntry Entry { get; init; }

        /// <summary>Effective storage type (from data fork for extended files)</summary>
        internal byte EffectiveStorageType { get; init; }

        /// <summary>Effective key block (from data fork for extended files)</summary>
        internal ushort EffectiveKeyBlock { get; init; }

        /// <summary>Cached index block for sapling/tree files</summary>
        internal ushort[] IndexBlock { get; set; }

        /// <summary>Cached master index block for tree files</summary>
        internal ushort[] MasterIndexBlock { get; set; }

        /// <summary>Cached index block number for tree files (which index block is loaded)</summary>
        internal int CachedIndexBlockNumber { get; set; } = -1;

        /// <summary>Cached data block index</summary>
        internal int CachedBlockIndex { get; set; } = -1;

        /// <summary>Cached block data</summary>
        internal byte[] CachedBlockData { get; set; }

        /// <inheritdoc />
        public long Offset { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }
    }
}