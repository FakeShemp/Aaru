// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for A2R flux images.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class A2R
{
    // Per A2R spec: File signature "A2R2" (version 2.x) - 0x32523241 little-endian
    readonly byte[] _a2Rv2Signature = "A2R2"u8.ToArray();

    // Per A2R spec: File signature "A2R3" (version 3.x) - 0x33523241 little-endian
    readonly byte[] _a2Rv3Signature = "A2R3"u8.ToArray();

    // Per A2R spec: INFO chunk signature - contains fundamental image information
    // Must be the first chunk in the file (after 8-byte header)
    // Used in both 2.x and 3.x formats
    readonly byte[] _infoChunkSignature = "INFO"u8.ToArray();

    // Per A2R spec: META chunk signature - contains tab-delimited UTF-8 metadata
    // Optional chunk, can appear anywhere after INFO chunk
    // Used in both 2.x and 3.x formats
    readonly byte[] _metaChunkSignature = "META"u8.ToArray();

    // Per A2R 3.x spec: RWCP (Raw Captures) chunk signature - contains raw flux data streams
    // Replaces STRM chunk from 2.x format
    // Supports configurable resolution per chunk
    readonly byte[] _rwcpChunkSignature = "RWCP"u8.ToArray();

    // Per A2R 3.x spec: SLVD (Solved) chunk signature
    // Contains solved flux data (not yet supported in Aaru)
    readonly byte[] _slvdChunkSignature = "SLVD"u8.ToArray();

    // Per A2R 2.x spec: STRM (Stream) chunk signature - contains raw flux data streams
    // Replaced by RWCP chunk in 3.x format
    // Uses fixed 125ns resolution
    readonly byte[] _strmChunkSignature = "STRM"u8.ToArray();
}