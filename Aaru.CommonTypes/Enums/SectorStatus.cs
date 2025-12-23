// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SectorStatus.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Common types.
//
// --[ Description ] ----------------------------------------------------------
//
//     Defines enumerations of sector status.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

namespace Aaru.CommonTypes.Enums;

/// <summary>
///     Sector status. Same as in libaaruformat.
/// </summary>
public enum SectorStatus : byte
{
    /// <summary>
    ///     Sector(s) not yet acquired during image dumping.
    /// </summary>
    NotDumped = 0x0,
    /// <summary>
    ///     Sector(s) successfully dumped without error.
    /// </summary>
    Dumped = 0x1,
    /// <summary>
    ///     Error during dumping; data may be incomplete or corrupt.
    /// </summary>
    Errored = 0x2,
    /// <summary>
    ///     Valid MODE 1 data with regenerable suffix/prefix.
    /// </summary>
    Mode1Correct = 0x3,
    /// <summary>
    ///     Suffix verified/regenerable for MODE 2 Form 1.
    /// </summary>
    Mode2Form1Ok = 0x4,
    /// <summary>
    ///     Suffix matches MODE 2 Form 2 with valid CRC.
    /// </summary>
    Mode2Form2Ok = 0x5,
    /// <summary>
    ///     Suffix matches MODE 2 Form 2 but CRC empty/missing.
    /// </summary>
    Mode2Form2NoCrc = 0x6,
    /// <summary>
    ///     Pointer references a twin sector table.
    /// </summary>
    Twin = 0x7,
    /// <summary>
    ///     Sector physically unrecorded; repeated reads non-deterministic.
    /// </summary>
    Unrecorded = 0x8,
    /// <summary>
    ///     Content encrypted and stored encrypted in image.
    /// </summary>
    Encrypted = 0x9,
    /// <summary>
    ///     Content originally encrypted but stored decrypted in image.
    /// </summary>
    Unencrypted = 0xA
}