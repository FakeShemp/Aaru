// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

// ReSharper disable UnusedMember.Local

using System;
using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

/// <inheritdoc />
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Cram
{
#region Nested type: CramCompression

    /// <summary>Compression algorithms supported by CramFS</summary>
    enum CramCompression : ushort
    {
        /// <summary>Zlib/deflate compression</summary>
        Zlib = 1,

        /// <summary>LZMA compression</summary>
        Lzma = 2,

        /// <summary>LZO compression</summary>
        Lzo = 3,

        /// <summary>XZ compression</summary>
        Xz = 4,

        /// <summary>LZ4 compression</summary>
        Lz4 = 5
    }

#endregion

#region Nested type: CramFlags

    /// <summary>
    ///     CramFS feature flags.
    ///     Flags 0x00000000 - 0x000000ff work for all past kernels.
    ///     Flags 0x00000100 - 0xffffffff don't work for past kernels.
    /// </summary>
    [Flags]
    enum CramFlags : uint
    {
        /// <summary>No flags set</summary>
        None = 0,

        /// <summary>fsid version #2 (includes CRC, edition, blocks, files)</summary>
        FsIdVersion2 = 0x00000001,

        /// <summary>Directory entries are sorted</summary>
        SortedDirs = 0x00000002,

        /// <summary>Support for holes (sparse files)</summary>
        Holes = 0x00000100,

        /// <summary>Reserved - wrong signature detected</summary>
        WrongSignature = 0x00000200,

        /// <summary>Root filesystem has shifted offset</summary>
        ShiftedRootOffset = 0x00000400,

        /// <summary>Extended block pointer format</summary>
        ExtBlockPointers = 0x00000800
    }

#endregion

#region Nested type: CramSupportedFlags

    /// <summary>
    ///     Mask of all supported CramFS flags.
    ///     Currently mount is refused if (flags &amp; ~CRAMFS_SUPPORTED_FLAGS) is non-zero.
    /// </summary>
    [Flags]
    enum CramSupportedFlags : uint
    {
        /// <summary>All supported flags combined</summary>
        All = 0x000000FF                  |
              CramFlags.Holes             |
              CramFlags.WrongSignature    |
              CramFlags.ShiftedRootOffset |
              CramFlags.ExtBlockPointers
    }

#endregion
}