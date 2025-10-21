// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commodore file system plugin.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;
using Aaru.CommonTypes.Interfaces;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the filesystem used in 8-bit Commodore microcomputers</summary>
public sealed partial class CBM
{
#region Nested type: BAM

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BAM
    {
        /// <summary>Track where directory starts</summary>
        public byte directoryTrack;
        /// <summary>Sector where directory starts</summary>
        public byte directorySector;
        /// <summary>Disk DOS version, 0x41</summary>
        public byte dosVersion;
        /// <summary>Set to 0x80 if 1571, 0x00 if not</summary>
        public byte doubleSided;
        /// <summary>Block allocation map</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 140)]
        public byte[] bam;
        /// <summary>Disk name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] name;
        /// <summary>Filled with 0xA0</summary>
        public ushort fill1;
        /// <summary>Disk ID</summary>
        public ushort diskId;
        /// <summary>Filled with 0xA0</summary>
        public byte fill2;
        /// <summary>DOS type</summary>
        public ushort dosType;
        /// <summary>Filled with 0xA0</summary>
        public uint fill3;
        /// <summary>Unused</summary>
        public byte unused1;
        /// <summary>Block allocation map for Dolphin DOS extended tracks</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] dolphinBam;
        /// <summary>Block allocation map for Speed DOS extended tracks</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] speedBam;
        /// <summary>Unused</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] unused2;
        /// <summary>Free sector count for second side in 1571</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] freeCount;
    }

#endregion

#region Nested type: CachedFile

    struct CachedFile
    {
        public byte[]         data;
        public ulong          length;
        public FileAttributes attributes;
        public int            blocks;
        public ulong          id;
    }

#endregion

#region Nested type: CbmDirNode

    sealed class CbmDirNode : IDirNode
    {
        internal string[] Contents;
        internal int      Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#endregion

#region Nested type: CbmFileNode

    sealed class CbmFileNode : IFileNode
    {
        internal byte[] Cache;

#region IFileNode Members

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }

#endregion
    }

#endregion

#region Nested type: DirectoryEntry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirectoryEntry
    {
        public byte nextDirBlockTrack;
        public byte nextDirBlockSector;
        public byte fileType;
        public byte firstFileBlockTrack;
        public byte firstFileBlockSector;
        /// <summary>Filename</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] name;
        public byte  firstSideBlockTrack;
        public byte  firstSideBlockSector;
        public uint  unused;
        public byte  replacementTrack;
        public byte  replacementSector;
        public short blocks;
    }

#endregion

#region Nested type: Header

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Header
    {
        /// <summary>Track where directory starts</summary>
        public byte directoryTrack;
        /// <summary>Sector where directory starts</summary>
        public byte directorySector;
        /// <summary>Disk DOS version, 0x44</summary>
        public byte diskDosVersion;
        /// <summary>Unusued</summary>
        public byte unused1;
        /// <summary>Disk name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] name;
        /// <summary>Filled with 0xA0</summary>
        public ushort fill1;
        /// <summary>Disk ID</summary>
        public ushort diskId;
        /// <summary>Filled with 0xA0</summary>
        public byte fill2;
        /// <summary>DOS version ('3')</summary>
        public byte dosVersion;
        /// <summary>Disk version ('D')</summary>
        public byte diskVersion;
        /// <summary>Filled with 0xA0</summary>
        public short fill3;
    }

#endregion
}