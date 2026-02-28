// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Internal (non on-disk) structures used by the btrfs plugin.
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

public sealed partial class BTRFS
{
    /// <summary>Represents an opened btrfs directory for enumeration</summary>
    sealed class BtrfsDirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Array of directory entry names in this directory</summary>
        internal string[] Entries { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>Stores a logical-to-physical chunk mapping entry used to translate btrfs logical addresses</summary>
    struct ChunkMapping
    {
        /// <summary>The logical byte offset where this chunk starts</summary>
        public ulong LogicalOffset;

        /// <summary>The length of this chunk in bytes</summary>
        public ulong Length;

        /// <summary>The physical byte offset on the device where this chunk starts</summary>
        public ulong PhysicalOffset;

        /// <summary>The device id this chunk resides on</summary>
        public ulong DevId;

        /// <summary>Number of stripes in this chunk</summary>
        public ushort NumStripes;

        /// <summary>Stripe length in bytes</summary>
        public ulong StripeLen;

        /// <summary>Chunk type flags (DATA, SYSTEM, METADATA, RAID profiles)</summary>
        public ulong Type;
    }

    /// <summary>Represents an opened btrfs file for reading</summary>
    sealed class BtrfsFileNode : IFileNode
    {
        /// <summary>The objectid (inode number) of this file</summary>
        internal ulong ObjectId { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }
    }

    /// <summary>Cached directory entry information</summary>
    struct DirEntry
    {
        /// <summary>Object id (inode number) of the target</summary>
        public ulong ObjectId;

        /// <summary>File type (BTRFS_FT_REG_FILE, BTRFS_FT_DIR, etc.)</summary>
        public byte Type;

        /// <summary>Index of this entry in the directory</summary>
        public ulong Index;
    }

    /// <summary>Describes a file extent for on-demand reading</summary>
    struct ExtentEntry
    {
        /// <summary>Byte offset in the file where this extent starts</summary>
        public ulong FileOffset;

        /// <summary>Length of this extent in bytes (file-level)</summary>
        public ulong Length;

        /// <summary>Extent type (inline, regular, prealloc)</summary>
        public byte Type;

        /// <summary>Compression type (none, zlib, lzo, zstd)</summary>
        public byte Compression;

        /// <summary>Logical byte number on disk (for REG/PREALLOC extents)</summary>
        public ulong DiskBytenr;

        /// <summary>Number of bytes on disk (for REG/PREALLOC extents)</summary>
        public ulong DiskBytes;

        /// <summary>Offset into the on-disk extent (for REG/PREALLOC extents)</summary>
        public ulong ExtentOffset;

        /// <summary>Inline data bytes (for INLINE extents only)</summary>
        public byte[] InlineData;
    }
}