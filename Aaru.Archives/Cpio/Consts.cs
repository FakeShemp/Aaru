// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : CPIO plugin.
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

namespace Aaru.Archives;

public sealed partial class Cpio
{
    const string MAGIC_OLD_CHARACTER = "070707";
    const string MAGIC_NEW_ASCII     = "070701";
    const string MAGIC_NEW_CRC       = "070702";
    const ushort MAGIC_BINARY_BE     = 0x71C7;
    const ushort MAGIC_BINARY_LE     = 0xC771;
    const string TRAILER_NAME        = "TRAILER!!!";
    const int    MIN_HEADER_SIZE     = 6;
    const uint   FILE_TYPE_MASK      = 0xF000;
}