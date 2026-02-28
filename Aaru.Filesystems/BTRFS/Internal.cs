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

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
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
}