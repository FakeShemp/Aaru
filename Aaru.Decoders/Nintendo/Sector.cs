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
    ///     Start of the 2048-byte DVD XOR (scramble) region in a 2064-byte Nintendo sector — same as ECMA-267
    ///     <c>main_data</c> for a standard DVD sector. Nintendo still applies the table to these 2048 bytes.
    /// </summary>
    public const int NintendoScrambledDataOffset = 12;

    /// <summary>
    ///     Start of the 2048-byte logical <c>main_data</c> exposed to the game / filesystem in Nintendo GameCube/Wii DVD
    ///     sectors. Unlike ECMA-267 (where <c>main_data</c> begins at byte 12), Nintendo uses byte 6; CPR_MAI and related
    ///     fields follow a different layout than on a standard DVD-ROM. The DVD XOR layer still scrambles 2048 bytes
    ///     starting at <see cref="NintendoScrambledDataOffset" />; bytes 6–11 are not part of that scrambled block.
    /// </summary>
    public const int NintendoMainDataOffset = 6;

    /// <summary>
    ///     Derives the Nintendo descramble key from the first 8 bytes at <see cref="NintendoMainDataOffset" /> in the
    ///     LBA 0 sector after descramble (same 8 bytes used for key derivation in the drive/firmware path).
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
        int mainDataStart = NintendoScrambledDataOffset;

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