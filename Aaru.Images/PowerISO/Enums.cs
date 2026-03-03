// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains enumerations for PowerISO disc images.
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

namespace Aaru.Images;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class PowerISO
{
#region Nested type: DaaCompressionType

    /// <summary>Compression type used for chunks in DAA images</summary>
    enum DaaCompressionType
    {
        /// <summary>No compression, raw data</summary>
        None = 0x00,
        /// <summary>zlib/deflate compression</summary>
        Zlib = 0x10,
        /// <summary>LZMA compression</summary>
        Lzma = 0x20
    }

#endregion

#region Nested type: DaaDescriptorType

    /// <summary>Descriptor block types found between header and chunk table</summary>
    enum DaaDescriptorType : uint
    {
        /// <summary>Part information</summary>
        Part = 1,
        /// <summary>Split archive information</summary>
        Split = 2,
        /// <summary>Encryption information</summary>
        Encryption = 3,
        /// <summary>Comment</summary>
        Comment = 4
    }

#endregion

#region Nested type: DaaFormatVersion

    /// <summary>Format version identifiers</summary>
    enum DaaFormatVersion : uint
    {
        /// <summary>Version 1: uses zlib compression, 3-byte chunk table entries</summary>
        Version1 = 0x100,
        /// <summary>Version 2: uses bit-packed chunk table, supports LZMA compression</summary>
        Version2 = 0x110
    }

#endregion

#region Nested type: DaaImageType

    /// <summary>Image type identifiers</summary>
    enum DaaImageType
    {
        /// <summary>PowerISO DAA format</summary>
        Daa = 0x00,
        /// <summary>gBurner GBI format</summary>
        Gbi = 0x01
    }

#endregion
}