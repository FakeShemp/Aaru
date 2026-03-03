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
//     Contains structures for Easy CD Creator disc images.
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

public sealed partial class EasyCD
{
#region Nested type: CifBlockHeader

    /// <summary>RIFF block header. 12 bytes, little-endian.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CifBlockHeader
    {
        /// <summary>Always "RIFF" (0x52, 0x49, 0x46, 0x46)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>Length of block data (includes the type field)</summary>
        public uint length;
        /// <summary>Block type identifier ("imag", "disc", "ofs ", "adio", "info")</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] type;
    }

#endregion

#region Nested type: CifOffsetEntry

    /// <summary>Offset table entry pointing to a track block. 22 bytes, little-endian.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CifOffsetEntry
    {
        /// <summary>Always "RIFF" (0x52, 0x49, 0x46, 0x46)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] signature;
        /// <summary>Length of block this entry points to</summary>
        public uint length;
        /// <summary>Entry type: "adio" for audio tracks, "info" for data tracks</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] type;
        /// <summary>Offset of track block in image file</summary>
        public uint offset;
        /// <summary>Unknown</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] dummy;
    }

#endregion

#region Nested type: CifDiscDescriptor

    /// <summary>Disc descriptor. 16 bytes fixed part, little-endian, followed by variable-length title and artist.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CifDiscDescriptor
    {
        /// <summary>Full length of descriptor including variable data</summary>
        public ushort descriptorLength;
        /// <summary>Number of sessions on disc</summary>
        public ushort numSessions;
        /// <summary>Number of tracks on disc</summary>
        public ushort numTracks;
        /// <summary>Length of disc title string</summary>
        public ushort titleLength;
        /// <summary>Repeated length of descriptor</summary>
        public ushort descriptorLength2;
        /// <summary>Unknown</summary>
        public ushort dummy1;
        /// <summary>Image type, see <see cref="CifImageType" /></summary>
        public CifImageType imageType;
        /// <summary>Unknown</summary>
        public ushort dummy2;

        // Followed by: zero-terminated title and artist string (variable length)
    }

#endregion

#region Nested type: CifSessionDescriptor

    /// <summary>Session descriptor. 18 bytes, little-endian.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CifSessionDescriptor
    {
        /// <summary>Full length of descriptor</summary>
        public ushort descriptorLength;
        /// <summary>Number of tracks in this session</summary>
        public ushort numTracks;
        /// <summary>Unknown, appears to always be 1</summary>
        public ushort dummy1;
        /// <summary>1 for images with a data track, 0 otherwise</summary>
        public ushort dummy2;
        /// <summary>Unknown, appears to always be 0</summary>
        public ushort dummy3;
        /// <summary>Session type, see <see cref="CifSessionType" /></summary>
        public CifSessionType sessionType;
        /// <summary>Unknown, appears to always be 0</summary>
        public ushort dummy4;
        /// <summary>Unknown, appears to always be 0</summary>
        public ushort dummy5;
        /// <summary>Unknown, appears to always be 0</summary>
        public ushort dummy6;
    }

#endregion

#region Nested type: CifTrackDescriptor

    /// <summary>
    ///     Track descriptor. 24 bytes fixed part, little-endian, followed by either
    ///     <see cref="CifAudioTrackDescriptor" /> or <see cref="CifDataTrackDescriptor" />.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CifTrackDescriptor
    {
        /// <summary>Full length of descriptor including audio/data part</summary>
        public ushort descriptorLength;
        /// <summary>Unknown</summary>
        public ushort dummy1;
        /// <summary>Length of track in sectors</summary>
        public uint numSectors;
        /// <summary>Unknown</summary>
        public ushort dummy2;
        /// <summary>Track type, see <see cref="CifTrackType" /></summary>
        public CifTrackType trackType;
        /// <summary>Unknown</summary>
        public ushort dummy3;
        /// <summary>Unknown</summary>
        public ushort dummy4;
        /// <summary>Unknown</summary>
        public ushort dummy5;
        /// <summary>Recording mode: 0 = TAO, 4 = DAO</summary>
        public CifDaoMode daoMode;
        /// <summary>Unknown</summary>
        public ushort dummy7;
        /// <summary>Sector data size (not necessarily the stored size)</summary>
        public ushort sectorDataSize;
    }

#endregion

#region Nested type: CifAudioTrackDescriptor

    /// <summary>Audio track descriptor. 269 bytes fixed part, followed by variable-length title string.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CifAudioTrackDescriptor
    {
        /// <summary>Unknown</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 205)]
        public byte[] dummy1;
        /// <summary>ISRC code</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] isrc;
        /// <summary>Unknown</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] dummy2;
        /// <summary>Fade-in length measured in frames</summary>
        public uint fadeInLength;
        /// <summary>Unknown</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] dummy3;
        /// <summary>Fade-out length measured in frames</summary>
        public uint fadeOutLength;
        /// <summary>Unknown</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] dummy4;

        // Followed by: zero-terminated title string (variable length)
    }

#endregion

#region Nested type: CifDataTrackDescriptor

    /// <summary>Data track descriptor. 272 bytes, appears to be a fixed pattern.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CifDataTrackDescriptor
    {
        /// <summary>Unknown, appears to be a fixed pattern</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 272)]
        public byte[] dummy;
    }

#endregion
}