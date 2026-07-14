// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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

namespace Aaru.Filesystems;

public sealed partial class GDFX
{
    /// <summary>XDVDFS volume descriptor. Located at sector 32 (or 0 for rebuilt images) within the game partition.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct VolumeDescriptor
    {
        /// <summary>Bytes 0x000–0x013: "MICROSOFT*XBOX*MEDIA" (20 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] magic0;

        /// <summary>Bytes 0x014–0x017: Root directory starting sector, relative to game partition base</summary>
        public uint rootDirSector;

        /// <summary>Bytes 0x018–0x01B: Root directory size in bytes</summary>
        public uint rootDirSize;

        /// <summary>Bytes 0x01C–0x023: Volume creation timestamp as Windows FILETIME (100-nanosecond intervals since 1601-01-01)</summary>
        public ulong fileTime;

        /// <summary>Bytes 0x024–0x7EB: Unused, zero-filled</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x7C8)]
        public byte[] unused;

        /// <summary>Bytes 0x7EC–0x7FF: Duplicate "MICROSOFT*XBOX*MEDIA" magic</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] magic1;
    }

    /// <summary>
    ///     Fixed-size header of a directory entry node in the XDVDFS binary search tree. The variable-length filename
    ///     immediately follows this header and is NOT null-terminated.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DirectoryEntryHeader
    {
        /// <summary>
        ///     Bytes 0–1: Offset to the left child node in 4-byte units from the start of the directory block. 0xFFFF means
        ///     no left child.
        /// </summary>
        public ushort leftEntryOffset;

        /// <summary>
        ///     Bytes 2–3: Offset to the right child node in 4-byte units from the start of the directory block. 0xFFFF means
        ///     no right child.
        /// </summary>
        public ushort rightEntryOffset;

        /// <summary>Bytes 4–7: Starting sector of the file or subdirectory data, relative to game partition base</summary>
        public uint dataSector;

        /// <summary>Bytes 8–11: File size in bytes. For directories this is the size of the directory block.</summary>
        public uint dataSize;

        /// <summary>Bytes 12: File attribute flags (ATTR_* constants)</summary>
        public byte attributes;

        /// <summary>Bytes 13: Length of the filename in bytes (max 255, Windows-1252 encoded)</summary>
        public byte filenameLength;
    }
}