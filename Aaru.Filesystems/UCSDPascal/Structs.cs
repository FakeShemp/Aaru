// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : U.C.S.D. Pascal filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     U.C.S.D. Pascal filesystem structures.
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

// Information from Call-A.P.P.L.E. Pascal Disk Directory Structure
[SuppressMessage("ReSharper", "NotAccessedField.Local")]
public sealed partial class PascalPlugin
{
#region Nested type: PascalDirNode

    sealed class PascalDirNode : IDirNode
    {
        internal string[] Contents;
        internal int      Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#endregion

#region Nested type: PascalFileEntry

    /// <summary>UCSD Pascal directory file entry (26 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct PascalFileEntry
    {
        /// <summary>0x00, first block of file</summary>
        public short FirstBlock;
        /// <summary>0x02, last block of file</summary>
        public short LastBlock;
        /// <summary>0x04, entry type</summary>
        public PascalFileKind EntryType;
        /// <summary>0x06, file name (Pascal string: first byte is length, followed by up to 15 characters)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Filename;
        /// <summary>0x16, bytes used in last block</summary>
        public short LastBytes;
        /// <summary>0x18, modification time</summary>
        public short ModificationTime;
    }

#endregion

#region Nested type: PascalFileNode

    sealed class PascalFileNode : IFileNode
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

#region Nested type: PascalVolumeEntry

    /// <summary>UCSD Pascal volume directory entry (26 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct PascalVolumeEntry
    {
        /// <summary>0x00, first block of volume entry (always 0)</summary>
        public short FirstBlock;
        /// <summary>0x02, last block of volume entry</summary>
        public short LastBlock;
        /// <summary>0x04, entry type (Volume or Secure)</summary>
        public PascalFileKind EntryType;
        /// <summary>0x06, volume name (Pascal string: first byte is length, followed by up to 7 characters)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] VolumeName;
        /// <summary>0x0E, blocks in volume</summary>
        public short Blocks;
        /// <summary>0x10, files in volume</summary>
        public short Files;
        /// <summary>0x12, dummy/reserved</summary>
        public short Dummy;
        /// <summary>0x14, last boot date</summary>
        public short LastBoot;
        /// <summary>0x16, tail to make record same size as <see cref="PascalFileEntry" /></summary>
        public int Tail;
    }

#endregion
}