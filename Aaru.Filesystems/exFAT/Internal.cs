// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft exFAT filesystem plugin.
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

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
public sealed partial class exFAT
{
#region Nested type: CompleteDirectoryEntry

    /// <summary>Represents a complete directory entry set with file entry, stream extension, and file name.</summary>
    sealed class CompleteDirectoryEntry
    {
        /// <summary>Data length in bytes.</summary>
        public ulong DataLength;

        /// <summary>The File directory entry.</summary>
        public FileDirectoryEntry FileEntry;

        /// <summary>The complete file name assembled from File Name directory entries.</summary>
        public string FileName;

        /// <summary>First cluster of the file data.</summary>
        public uint FirstCluster;

        /// <summary>Whether the allocation is contiguous (NoFatChain).</summary>
        public bool IsContiguous;

        /// <summary>Whether this entry is a directory.</summary>
        public bool IsDirectory;

        /// <summary>The Stream Extension directory entry.</summary>
        public StreamExtensionDirectoryEntry StreamEntry;

        /// <summary>Valid data length in bytes.</summary>
        public ulong ValidDataLength;

        /// <inheritdoc />
        public override string ToString() => FileName ?? string.Empty;
    }

#endregion

#region Nested type: ExFatDirNode

    sealed class ExFatDirNode : IDirNode
    {
        internal CompleteDirectoryEntry[] Entries;
        internal int                      Position;

        /// <inheritdoc />
        public string Path { get; init; }
    }

#endregion

#region Nested type: ExFatFileNode

    sealed class ExFatFileNode : IFileNode
    {
        /// <summary>First cluster of the file.</summary>
        internal uint FirstCluster;

        /// <summary>Whether the file allocation is contiguous (NoFatChain).</summary>
        internal bool IsContiguous;

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }
    }

#endregion
}