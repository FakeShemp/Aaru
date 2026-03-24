// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Sector.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Device structures decoders.
//
// --[ Description ] ----------------------------------------------------------
//
//     Decodes and descrambles Nintendo (GameCube/Wii) DVD sectors.
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

#nullable enable
using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes.Enums;

namespace Aaru.Decoders.Nintendo;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class Sector
{
    /// <summary>
    ///     ECMA-267 <c>main_data</c> offset in OmniDrive 2064-byte Nintendo sectors: DVD XOR applies to 2048 bytes from
    ///     here (same as standard DVD). Bytes 6-11 (<c>cpr_mai</c>) are not scrambled on media.
    /// </summary>
    public const int NintendoMainDataOffset = 12;

    /// <summary>
    ///     Derives the Nintendo descramble key from the first 8 bytes of the cpr_mai region (LBA 0 payload).
    ///     Used when software-descrambling Nintendo sectors.
    /// </summary>
    public static byte DeriveNintendoKey(byte[] cprMaiFirst8)
    {
        if(cprMaiFirst8 == null || cprMaiFirst8.Length < 8) return 0;

        int sum = 0;

        for(int i = 0; i < 8; i++)
            sum += cprMaiFirst8[i];

        return (byte)(((sum >> 4) + sum) & 0xF);
    }

    /// <summary>
    ///     Descrambles a Nintendo DVD sector. Uses PSN from header (bytes 1-3) to select XOR table and Nintendo key.
    /// </summary>
    /// <param name="sector">Scrambled 2064-byte sector</param>
    /// <param name="nintendoKey">Nintendo key (0-15)</param>
    /// <param name="scrambled">Descrambled sector output</param>
    public ErrorNumber Scramble(byte[] sector, byte nintendoKey, out byte[] scrambled)
    {
        scrambled = new byte[sector.Length];

        if(sector is not { Length: 2064 }) return ErrorNumber.NotSupported;

        int psn = DVD.Sector.GetPsn(sector);
        int mainDataStart = NintendoMainDataOffset;

        int tableOffset = (int)((nintendoKey ^ (psn >> 4 & 0xF)) * DVD.Sector.Form1DataSize +
                                7 * DVD.Sector.Form1DataSize + DVD.Sector.Form1DataSize / 2);
        Array.Copy(sector, 0, scrambled, 0, sector.Length);
        DVD.Sector.ApplyTableWithWrap(scrambled, mainDataStart, tableOffset);

        return DVD.Sector.CheckEdc(scrambled) ? ErrorNumber.NoError : ErrorNumber.NotVerifiable;
    }

    public ErrorNumber Scramble(byte[] sector, uint transferLength, byte nintendoKey, out byte[] scrambled)
    {
        scrambled = new byte[sector.Length];

        if(sector.Length % 2064 != 0 || sector.Length / 2064 != transferLength) return ErrorNumber.NotSupported;

        for(uint i = 0; i < transferLength; i++)
        {
            ErrorNumber error = Scramble(sector.Skip((int)(i * 2064)).Take(2064).ToArray(), nintendoKey, out byte[]? currentSector);

            if(error != ErrorNumber.NoError) return error;

            Array.Copy(currentSector, 0, scrambled, i * 2064, 2064);
        }

        return ErrorNumber.NoError;
    }
}