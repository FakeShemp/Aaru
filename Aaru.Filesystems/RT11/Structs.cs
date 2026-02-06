// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : RT-11 file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the RT-11 file system and shows information.
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

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
public sealed partial class RT11
{
#region Nested type: HomeBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct HomeBlock
    {
        /// <summary>Bad block replacement table</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 130)]
        public readonly byte[] badBlockTable;
        /// <summary>Unused</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] unused;
        /// <summary>INITIALIZE/RESTORE data area</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
        public readonly byte[] initArea;
        /// <summary>BUP information area</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public readonly byte[] bupInformation;
        /// <summary>Empty</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        public readonly byte[] empty;
        /// <summary>Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved1;
        /// <summary>Reserved</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public readonly byte[] empty2;
        /// <summary>Cluster size</summary>
        public readonly ushort cluster;
        /// <summary>Block of the first directory segment</summary>
        public readonly ushort rootBlock;
        /// <summary>"V3A" in Radix-50</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] systemVersion;
        /// <summary>Name of the volume, 12 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] volname;
        /// <summary>Name of the volume owner, 12 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] ownername;
        /// <summary>RT11 defines it as "DECRT11A    ", 12 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] format;
        /// <summary>Unused</summary>
        public readonly ushort unused2;
        /// <summary>Checksum of preceding 255 words (16 bit units)</summary>
        public readonly ushort checksum;
    }

#endregion

#region Nested type: DirectorySegmentHeader

    /// <summary>Directory segment header, 5 words (10 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectorySegmentHeader
    {
        /// <summary>Total number of segments in this directory (1-31)</summary>
        public readonly ushort totalSegments;
        /// <summary>Segment number of the next logical directory segment (0 if last)</summary>
        public readonly ushort nextSegment;
        /// <summary>Number of the highest segment currently in use (only valid in segment 1)</summary>
        public readonly ushort highestSegmentInUse;
        /// <summary>Number of extra bytes per directory entry (always even)</summary>
        public readonly ushort extraBytesPerEntry;
        /// <summary>Block number on volume where data begins for this segment</summary>
        public readonly ushort dataBlockStart;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>Directory entry, 7 words (14 bytes) + extra bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectoryEntry
    {
        /// <summary>Status word (high byte: entry type, low byte: file class)</summary>
        public readonly ushort status;
        /// <summary>First word of filename in Radix-50</summary>
        public readonly ushort filename1;
        /// <summary>Second word of filename in Radix-50</summary>
        public readonly ushort filename2;
        /// <summary>File type in Radix-50</summary>
        public readonly ushort filetype;
        /// <summary>Total file length in blocks</summary>
        public readonly ushort length;
        /// <summary>Job number (high byte) and channel number (low byte) - only for tentative files</summary>
        public readonly ushort jobChannel;
        /// <summary>Creation date</summary>
        public readonly ushort creationDate;
    }

#endregion
}