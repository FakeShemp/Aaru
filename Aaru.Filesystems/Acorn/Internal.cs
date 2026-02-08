// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <summary>Internal representation of a directory entry</summary>
    sealed class DirectoryEntryInfo
    {
        /// <summary>Filename</summary>
        public string Name { get; set; }

        /// <summary>RISC OS load address</summary>
        public uint LoadAddr { get; set; }

        /// <summary>RISC OS exec address</summary>
        public uint ExecAddr { get; set; }

        /// <summary>File length in bytes</summary>
        public uint Length { get; set; }

        /// <summary>Indirect disc address</summary>
        public uint IndAddr { get; set; }

        /// <summary>File attributes</summary>
        public byte Attributes { get; set; }
    }
}

