// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains structures for partclone disk images.
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

public sealed partial class PartClone
{
#region Nested type: Header

    /// <summary>PartClone v0001 disk image header, little-endian</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Header
    {
        /// <summary>Magic, <see cref="PartClone._partCloneMagic" /></summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public readonly byte[] magic;
        /// <summary>Source filesystem</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public readonly byte[] filesystem;
        /// <summary>Version</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] version;
        /// <summary>Padding</summary>
        public readonly ushort padding;
        /// <summary>Block (sector) size</summary>
        public readonly uint blockSize;
        /// <summary>Size of device containing the cloned partition</summary>
        public readonly ulong deviceSize;
        /// <summary>Total blocks in cloned partition</summary>
        public readonly ulong totalBlocks;
        /// <summary>Used blocks in cloned partition</summary>
        public readonly ulong usedBlocks;
        /// <summary>Empty space</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        public readonly byte[] buffer;
    }

#endregion

#region Nested type: HeaderV2

    /// <summary>PartClone v0002 image descriptor, little-endian. 110 bytes including trailing CRC-32.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct HeaderV2
    {
        /// <summary>Magic, NUL terminated. The first 15 bytes match <see cref="PartClone._partCloneMagic" />.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] magic;
        /// <summary>partclone version that produced the image, e.g. "0.3.1".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public readonly byte[] ptcVersion;
        /// <summary>Image format version, in text: "0002".</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] version;
        /// <summary>Endianness marker: <see cref="PartClone.ENDIAN_MAGIC" /> for little-endian.</summary>
        public readonly ushort endianess;

        /// <summary>File system magic name, NUL terminated.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] filesystem;
        /// <summary>Source device size in bytes.</summary>
        public readonly ulong deviceSize;
        /// <summary>Total block count.</summary>
        public readonly ulong totalBlocks;
        /// <summary>Used block count as reported by the file system superblock.</summary>
        public readonly ulong superBlockUsedBlocks;
        /// <summary>Used block count as counted from the bitmap.</summary>
        public readonly ulong usedBlocks;
        /// <summary>Block size in bytes.</summary>
        public readonly uint blockSize;

        /// <summary>Size of this options section.</summary>
        public readonly uint featureSize;
        /// <summary>Image format version, in binary: 0x0002.</summary>
        public readonly ushort imageVersion;
        /// <summary>CPU width that produced the image (32 / 64).</summary>
        public readonly ushort cpuBits;
        /// <summary>Checksum algorithm (see CSM_* constants).</summary>
        public readonly ushort checksumMode;
        /// <summary>Checksum size in bytes.</summary>
        public readonly ushort checksumSize;
        /// <summary>Number of consecutive blocks that share one checksum.</summary>
        public readonly uint blocksPerChecksum;
        /// <summary>If non-zero, the checksum is reseeded after every strip.</summary>
        public readonly byte reseedChecksum;
        /// <summary>Bitmap layout (see BM_* constants).</summary>
        public readonly byte bitmapMode;

        /// <summary>CRC-32 over the previous bytes of this descriptor.</summary>
        public readonly uint crc;
    }

#endregion
}