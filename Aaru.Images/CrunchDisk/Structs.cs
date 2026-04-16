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
//     Contains structures for CrunchDisk disk images.
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

namespace Aaru.Images;

public sealed partial class CrunchDisk
{
    /// <summary>CrunchDisk file header, 52 bytes, big-endian</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Header
    {
        /// <summary>Magic number, "CDF0" (0x43444630)</summary>
        public uint Id;
        /// <summary>Bytes per sector</summary>
        public uint BlockSize;
        /// <summary>Sectors per track</summary>
        public uint BlocksPerTrack;
        /// <summary>Number of heads</summary>
        public uint Heads;
        /// <summary>First cylinder in image</summary>
        public uint LowCyl;
        /// <summary>Last cylinder in image</summary>
        public uint HighCyl;
        /// <summary>Non-zero if password protected</summary>
        public byte IsPassword;
        /// <summary>Padding byte</summary>
        public byte Pad0;
        /// <summary>PX20 password checksum</summary>
        public ushort PasswordChecksum;
        /// <summary>PowerPacker efficiency level (1-4)</summary>
        public ushort Efficiency;
        /// <summary>Compression type</summary>
        public ushort PackerType;
    }
}