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
//     Contains structures for MagicISO UIF disc images.
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

namespace Aaru.Images;

public sealed partial class MagicIso
{
#region Nested type: BbisFooter

    /// <summary>
    ///     UIF footer located at the end of the file. 64 bytes, little-endian. The blhr field is 64-bit; with the
    ///     original packing (two-byte) it sits at offset 28 within the structure.
    /// </summary>
    struct BbisFooter
    {
        /// <summary>"bbis" signature.</summary>
        public uint signature;
        /// <summary>Declared footer size in bytes.</summary>
        public uint footerSize;
        /// <summary>Version number.</summary>
        public ushort version;
        /// <summary>Image variant: 8 for ISO, 9 for mixed/NRG-based layouts.</summary>
        public ushort imageType;
        public ushort unknown1;
        public ushort padding;
        /// <summary>Total number of virtual sectors in the decompressed image.</summary>
        public uint sectors;
        /// <summary>Sector size used by the BLHR entries. 2048 for ISO, 2352 for mixed.</summary>
        public uint sectorSize;
        public uint unknown2;
        /// <summary>Absolute file offset of the BLHR descriptor.</summary>
        public ulong blhrOffset;
        /// <summary>Combined size of the BLHR and BBIS headers in bytes.</summary>
        public uint blhrBbisSize;
        /// <summary>Hash used for password validation (unused when not encrypted).</summary>
        public byte[] hash;
        public uint unknown3;
        public uint unknown4;
    }

#endregion

#region Nested type: BlhrHeader

    /// <summary>BLHR / BLMS / BLSS descriptor header. 16 bytes, little-endian.</summary>
    struct BlhrHeader
    {
        public uint signature;
        /// <summary>Size of the payload that follows, including the version and num fields (minus the 8-byte header).</summary>
        public uint size;
        public uint version;
        /// <summary>Number of BLHR entries (BLHR) or payload bytes (BLMS/BLSS).</summary>
        public uint num;
    }

#endregion

#region Nested type: BlhrEntry

    /// <summary>
    ///     Single BLHR entry. 24 bytes, little-endian. Describes a contiguous run of output sectors and its compressed
    ///     representation inside the UIF file.
    /// </summary>
    struct BlhrEntry
    {
        /// <summary>Absolute offset within the UIF file where the compressed payload starts.</summary>
        public ulong offset;
        /// <summary>Compressed size in bytes.</summary>
        public uint compressedSize;
        /// <summary>First output sector covered by this entry, expressed in BbisFooter.sectorSize units.</summary>
        public uint startSector;
        /// <summary>Number of output sectors covered by this entry.</summary>
        public uint sectorCount;
        /// <summary>Block compression type (1 raw, 3 zero, 5 zlib).</summary>
        public uint type;
    }

#endregion

#region Nested type: MagicIsoTrack

    /// <summary>In-memory track record mapping a disc track onto the decompressed linear stream.</summary>
    struct MagicIsoTrack
    {
        public uint   sequence;
        public ushort session;
        public ulong  virtualByteOffset;
        public uint   sectorSize;
        public uint   cookedBytesPerSector;
        public ulong  startSector;
        public ulong  endSector;
        public int    index0;
        public int    index1;
        public uint   nrgMode;
        public bool   rawMode1;
        public bool   rawMode2;
    }

#endregion
}