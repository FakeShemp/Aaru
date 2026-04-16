// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Tar plugin.
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

public sealed partial class Tar
{
    enum TarFormat
    {
        V7,
        Gnu,
        Ustar,
        Star,
        V7Recognized
    }

    enum TypeFlag : byte
    {
        File        = (byte)'0',
        AltFile     = 0,
        HardLink    = (byte)'1',
        SymLink     = (byte)'2',
        CharDevice  = (byte)'3',
        BlockDevice = (byte)'4',
        Directory   = (byte)'5',
        Fifo        = (byte)'6',
        Contiguous  = (byte)'7',
        GnuLongName = (byte)'L',
        GnuLongLink = (byte)'K',
        GnuSparse   = (byte)'S',
        PaxGlobal   = (byte)'g',
        PaxExtended = (byte)'x'
    }
}