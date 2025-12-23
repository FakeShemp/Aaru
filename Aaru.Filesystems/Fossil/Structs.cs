// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Fossil filesystem plugin
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

/// <inheritdoc />
/// <summary>Implements detection for the Plan-9 Fossil on-disk filesystem</summary>
public sealed partial class Fossil
{
#region Nested type: Header

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Header
    {
        /// <summary>Magic number</summary>
        public uint magic;
        /// <summary>Header version</summary>
        public ushort version;
        /// <summary>Block size</summary>
        public ushort blockSize;
        /// <summary>Block containing superblock</summary>
        public uint super;
        /// <summary>Block containing labels</summary>
        public uint label;
        /// <summary>Where do data blocks start</summary>
        public uint data;
        /// <summary>How many data blocks does it have</summary>
        public uint end;
    }

#endregion

#region Nested type: SuperBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>Magic number</summary>
        public uint magic;
        /// <summary>Header version</summary>
        public ushort version;
        /// <summary>file system low epoch</summary>
        public uint epochLow;
        /// <summary>file system high(active) epoch</summary>
        public uint epochHigh;
        /// <summary>next qid to allocate</summary>
        public ulong qid;
        /// <summary>data block number: root of active file system</summary>
        public int active;
        /// <summary>data block number: root of next file system to archive</summary>
        public int next;
        /// <summary>data block number: root of file system currently being archived</summary>
        public int current;
        /// <summary>Venti score of last successful archive</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] last;
        /// <summary>name of file system(just a comment)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] name;
    }

#endregion
}