// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : HP Logical Interchange Format plugin
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
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

public sealed partial class LIF
{
#region Nested type: SystemBlock

    /// <summary>
    ///     LIF Volume Header (System Block). Always stored in the first 256-byte record (record 0) of the medium. Contains
    ///     volume identification, directory location, media geometry, and creation timestamp.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SystemBlock
    {
        /// <summary>Bytes 0-1: Magic number identifying the volume as LIF. Always 0x8000.</summary>
        public ushort magic;
        /// <summary>Bytes 2-7: Volume label, 6 ASCII characters, space-padded.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] volumeLabel;
        /// <summary>Bytes 8-11: Starting sector number of the directory area.</summary>
        public uint directoryStart;
        /// <summary>Bytes 12-13: LIF identifier indicating the system that initialized the volume (e.g. 0x1000 for HP-UX).</summary>
        public ushort lifId;
        /// <summary>Bytes 14-15: Unused, should be zero.</summary>
        public ushort unused;
        /// <summary>Bytes 16-19: Size of the directory area in 256-byte sectors.</summary>
        public uint directorySize;
        /// <summary>Bytes 20-21: LIF version number.</summary>
        public ushort lifVersion;
        /// <summary>Bytes 22-23: Unused, should be zero.</summary>
        public ushort unused2;
        /// <summary>Bytes 24-27: Number of tracks per surface on the medium.</summary>
        public uint tracks;
        /// <summary>Bytes 28-31: Number of recording surfaces (heads) on the medium.</summary>
        public uint heads;
        /// <summary>Bytes 32-35: Number of sectors per track on the medium.</summary>
        public uint sectors;
        /// <summary>Bytes 36-41: Volume creation date and time in BCD format (YY MM DD HH MM SS).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] creationDate;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>
    ///     LIF Directory Entry. Each entry is 32 bytes and describes a single file in the flat (non-hierarchical) directory.
    ///     Directory entries are stored sequentially in the directory area starting at the sector indicated by the system
    ///     block's <see cref="SystemBlock.directoryStart" /> field. An entry with a filename starting with 0xFF indicates
    ///     a purged (deleted) file, and a file type of 0xFFFF marks the end of the directory.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirectoryEntry
    {
        /// <summary>Bytes 0-9: Filename, up to 10 ASCII characters, space-padded. Names are case-sensitive on some systems.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] fileName;
        /// <summary>
        ///     Bytes 10-11: File type code. The high byte identifies the originating system/division and the low byte
        ///     identifies the specific file type within that system. Type 0x0000 denotes plain ASCII. Type 0xFFFF marks the
        ///     end of the directory. Ranges include: 0x0000-0x7FFF general purpose, 0xDC00-0xDFFF CSD (64000),
        ///     0xE000-0xE3FF Series 80, 0xE400-0xE7FF Terminals, 0xE800-0xEBFF 9845/9835/98X6/500,
        ///     0xEC00-0xEFFF HP 250/260, 0xF000-0xF3FF HP 1000, 0xF400-0xF7FF HP 3000, 0xF800-0xFBFF HP 300,
        ///     0xFC00-0xFFFE special interdivisional types.
        /// </summary>
        public ushort fileType;
        /// <summary>Bytes 12-15: Starting sector number of the file data area (absolute sector address on the medium).</summary>
        public uint fileStart;
        /// <summary>Bytes 16-19: File length in 256-byte records.</summary>
        public uint fileLength;
        /// <summary>Bytes 20-25: File creation date and time in BCD format (YY MM DD HH MM SS).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] creationDate;
        /// <summary>
        ///     Bytes 26-27: Volume number (high byte) and implementation flags (low byte). Usage varies by system and
        ///     implementation.
        /// </summary>
        public ushort volumeNumber;
        /// <summary>
        ///     Bytes 28-31: General purpose field. Usage is implementation-specific and varies between systems. On some
        ///     systems it stores the defined record size, on others it holds an execution address or other metadata.
        /// </summary>
        public uint generalPurpose;
    }

#endregion
}