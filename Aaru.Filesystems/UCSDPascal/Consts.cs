// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : U.C.S.D. Pascal filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     U.C.S.D. Pascal filesystem constants.
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

namespace Aaru.Filesystems;

// Information from Call-A.P.P.L.E. Pascal Disk Directory Structure
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class PascalPlugin
{
    const string FS_TYPE = "ucsd";

#region Nested type: MachineType

    /// <summary>UCSD Pascal machine/architecture type</summary>
    enum MachineType
    {
        /// <summary>Unknown machine type</summary>
        Unknown,
        /// <summary>P-Code interpreter, big endian</summary>
        PCodeBigEndian,
        /// <summary>P-Code interpreter, little endian</summary>
        PCodeLittleEndian,
        /// <summary>PDP-11, LSI-11, Terak (little endian)</summary>
        Pdp11,
        /// <summary>Intel 8080/8085 (little endian)</summary>
        Intel8080,
        /// <summary>Zilog Z80 (little endian)</summary>
        Z80,
        /// <summary>MOS 6502/65C02, Apple II, KIM-1 (little endian)</summary>
        Mos6502,
        /// <summary>Motorola 6800/6809 (big endian)</summary>
        Motorola6800,
        /// <summary>TI TMS9900, TI-99/4 (big endian)</summary>
        Ti9900,
        /// <summary>GA-16/440 (big endian)</summary>
        Ga440
    }

#endregion

#region Nested type: PascalFileKind

    enum PascalFileKind : short
    {
        /// <summary>Disk volume entry</summary>
        Volume = 0,
        /// <summary>File containing bad blocks</summary>
        Bad,
        /// <summary>Code file, machine executable</summary>
        Code,
        /// <summary>Text file, human readable</summary>
        Text,
        /// <summary>Information file for debugger</summary>
        Info,
        /// <summary>Data file</summary>
        Data,
        /// <summary>Graphics vectors</summary>
        Graf,
        /// <summary>Graphics screen image</summary>
        Foto,
        /// <summary>Security, not used</summary>
        Secure
    }

#endregion
}