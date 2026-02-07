// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Acorn.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ISO9660 filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Acorn RISC OS system area structures.
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
// In the loving memory of Facunda "Tata" Suárez Domínguez, R.I.P. 2019/07/24
// ****************************************************************************/

using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

public sealed partial class ISO9660
{
    /// <summary>Size of the Acorn system area structure.</summary>
    const int ACORN_SYSTEM_AREA_SIZE = 32;

    /// <summary>Acorn RISC OS system area magic signature.</summary>
    static readonly byte[] _acornMagic = "ARCHIMEDES"u8.ToArray();

    /// <summary>Acorn RISC OS system area structure, exactly 32 bytes.</summary>
    /// <remarks>
    ///     This structure is found in the system use area of ISO9660 directory records on discs created with Acorn RISC
    ///     OS tools. It stores RISC OS-specific file attributes including the filetype.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct AcornSystemArea
    {
        /// <summary>Magic signature "ARCHIMEDES" (10 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly byte[] Signature;

        /// <summary>Reserved byte at offset 10.</summary>
        public readonly byte Reserved10;

        /// <summary>Filetype low byte at offset 11.</summary>
        public readonly byte FiletypeLow;

        /// <summary>
        ///     Filetype high nibble and flags at offset 12. If high nibble is 0xF0, filetype is present. Filetype =
        ///     ((FiletypeHighAndFlags &amp; 0x0F) &lt;&lt; 8) | FiletypeLow
        /// </summary>
        public readonly byte FiletypeHighAndFlags;

        /// <summary>Filetype present marker at offset 13. If 0xFF, the filetype field is valid.</summary>
        public readonly byte FiletypePresent;

        /// <summary>Reserved bytes at offset 14-18 (5 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] Reserved14;

        /// <summary>
        ///     Application and attributes flags at offset 19. Bit 0 = application flag (filename should start with '!'
        ///     instead of '_').
        /// </summary>
        public readonly byte Flags;

        /// <summary>Load address (RISC OS specific) at offset 20-23.</summary>
        public readonly uint LoadAddress;

        /// <summary>Execution address (RISC OS specific) at offset 24-27.</summary>
        public readonly uint ExecAddress;

        /// <summary>File attributes (RISC OS specific) at offset 28-31.</summary>
        public readonly uint Attributes;

        /// <summary>Gets a value indicating whether this entry represents an application (starts with '!').</summary>
        public bool IsApplication => (Flags & 0x01) == 0x01;

        /// <summary>Gets the RISC OS filetype if present, otherwise null.</summary>
        public ushort? Filetype => HasFiletype ? (ushort)((FiletypeHighAndFlags & 0x0F) << 8 | FiletypeLow) : null;

        bool HasFiletype => FiletypePresent == 0xFF && (FiletypeHighAndFlags & 0xF0) == 0xF0;
    }
}