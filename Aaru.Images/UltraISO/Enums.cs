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
//     Contains enumerations for UltraISO disc images.
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

namespace Aaru.Images;

public sealed partial class UltraISO
{
#region Nested type: IszChunkType

    /// <summary>Chunk compression/data type in ISZ images</summary>
    enum IszChunkType : byte
    {
        /// <summary>Chunk is all zeros, no data stored</summary>
        Zero = 0,
        /// <summary>Uncompressed raw data</summary>
        Data = 1,
        /// <summary>zlib/deflate compressed data</summary>
        Zlib = 2,
        /// <summary>bzip2 compressed data</summary>
        Bz2 = 3
    }

#endregion

#region Nested type: IszEncryption

    /// <summary>Encryption type used in ISZ images</summary>
    enum IszEncryption : byte
    {
        /// <summary>No encryption</summary>
        None = 0,
        /// <summary>Password-based encryption</summary>
        Password = 1,
        /// <summary>AES-128 encryption</summary>
        Aes128 = 2,
        /// <summary>AES-192 encryption</summary>
        Aes192 = 3,
        /// <summary>AES-256 encryption</summary>
        Aes256 = 4
    }

#endregion
}