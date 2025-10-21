// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SUSP.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ISO9660 filesystem plugin.
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
// In the loving memory of Facunda "Tata" Suárez Domínguez, R.I.P. 2019/07/24
// ****************************************************************************/

// ReSharper disable UnusedType.Local

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

public sealed partial class ISO9660
{
#region Nested type: ContinuationArea

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct ContinuationArea
    {
        public ushort signature;
        public byte   length;
        public byte   version;
        public uint   block;
        public uint   block_be;
        public uint   offset;
        public uint   offset_be;
        public uint   ca_length;
        public uint   ca_length_be;
    }

#endregion

#region Nested type: IndicatorArea

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct IndicatorArea
    {
        public readonly ushort signature;
        public readonly byte   length;
        public readonly byte   version;
        public readonly ushort magic;
        public readonly byte   skipped;
    }

#endregion

#region Nested type: PaddingArea

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PaddingArea
    {
        public readonly ushort signature;
        public readonly byte   length;
        public readonly byte   version;
    }

#endregion

#region Nested type: ReferenceArea

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct ReferenceArea
    {
        public ushort signature;
        public byte   length;
        public byte   version;
        public byte   id_len;
        public byte   des_len;
        public byte   src_len;
        public byte   ext_ver;

        // Follows extension identifier for id_len bytes
        // Follows extension descriptor for des_len bytes
        // Follows extension source for src_len bytes
    }

#endregion

#region Nested type: SelectorArea

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SelectorArea
    {
        public readonly ushort signature;
        public readonly byte   length;
        public readonly byte   version;
        public readonly byte   sequence;
    }

#endregion

#region Nested type: TerminatorArea

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct TerminatorArea
    {
        public readonly ushort signature;
        public readonly byte   length;
        public readonly byte   version;
    }

#endregion
}