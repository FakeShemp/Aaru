// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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

// ReSharper disable UnusedMember.Local

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the CRAM filesystem</summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Cram
{
#region Nested type: SuperBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        public uint magic;
        public uint size;
        public uint flags;
        public uint future;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] signature;
        public uint crc;
        public uint edition;
        public uint blocks;
        public uint files;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] name;
    }

#endregion
}