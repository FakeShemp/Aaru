// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
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
    enum CpioFormat
    {
        OldBinaryBE,
        OldBinaryLE,
        OldCharacter,
        NewAscii,
        NewCrc
    }

    enum CpioFileType : uint
    {
        Fifo        = 0x1000,
        CharDevice  = 0x2000,
        Directory   = 0x4000,
        BlockDevice = 0x6000,
        Regular     = 0x8000,
        Symlink     = 0xA000,
        Socket      = 0xC000
    }
}