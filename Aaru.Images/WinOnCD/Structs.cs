// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Structures for WinOnCD disc images.
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

using System.Runtime.InteropServices;

namespace Aaru.Images;

public sealed partial class WinOnCD
{
    /// <summary>C2D file header block.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct C2dHeaderBlock
    {
        /// <summary>File signature, 32 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] signature;

        /// <summary>Header size.</summary>
        public ushort header_size;

        /// <summary>Whether UPC/EAN is present.</summary>
        public ushort has_upc_ean;

        /// <summary>UPC/EAN barcode, 13 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public byte[] upc_ean;

        /// <summary>Padding.</summary>
        public byte dummy1;

        /// <summary>Number of track blocks.</summary>
        public ushort num_track_blocks;

        /// <summary>Size of CD-Text data.</summary>
        public uint size_cdtext;

        /// <summary>File offset to track blocks.</summary>
        public uint offset_tracks;

        /// <summary>Unknown.</summary>
        public uint dummy2;

        /// <summary>Disc description, 128 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] description;

        /// <summary>File offset to C2CK block.</summary>
        public uint offset_c2ck;
    }

    /// <summary>C2D track block, 44 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct C2dTrackBlock
    {
        /// <summary>Size of this block (always 44).</summary>
        public uint block_size;

        /// <summary>First sector number.</summary>
        public uint first_sector;

        /// <summary>Last sector number.</summary>
        public uint last_sector;

        /// <summary>File offset to track data. 0xFFFFFFFF if index &gt; 1.</summary>
        public ulong image_offset;

        /// <summary>Sector size in bytes.</summary>
        public uint sector_size;

        /// <summary>ISRC code, 12 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] isrc;

        /// <summary>Track flags.</summary>
        public C2dFlag flags;

        /// <summary>Session number (1-based).</summary>
        public byte session;

        /// <summary>Track number within session (point).</summary>
        public byte point;

        /// <summary>Index number.</summary>
        public byte index;

        /// <summary>Track mode.</summary>
        public C2dMode mode;

        /// <summary>Whether track data is compressed.</summary>
        public byte compressed;

        /// <summary>Padding.</summary>
        public ushort dummy;
    }

    /// <summary>C2D CD-Text block, 18 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct C2dCdTextBlock
    {
        /// <summary>Pack type.</summary>
        public byte pack_type;

        /// <summary>Track number.</summary>
        public byte track_number;

        /// <summary>Sequence number.</summary>
        public byte seq_number;

        /// <summary>Block number.</summary>
        public byte block_number;

        /// <summary>CD-Text data, 12 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] data;

        /// <summary>CRC, 2 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] crc;
    }

    /// <summary>C2D C2CK block, 32 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct C2dC2CkBlock
    {
        /// <summary>Size of this block (always 32).</summary>
        public uint block_size;

        /// <summary>Signature string "C2CK", 4 bytes.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;

        /// <summary>Unknown, 2 x u32.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] dummy1;

        /// <summary>Offset to blocks after track data (WOCD, C2AW, etc.).</summary>
        public ulong next_offset;

        /// <summary>Unknown, 2 x u32.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] dummy2;
    }
}