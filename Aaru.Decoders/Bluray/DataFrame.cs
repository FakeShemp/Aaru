// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DataFrame.cs
//
// Component      : Blu-ray DataFrame (redumper bd::DataFrame) scrambling.
//
// --[ Description ] ----------------------------------------------------------
//
//     Descrambling and validity for 2052-byte Blu-ray raw frames per
//     ISO/IEC 30190, matching redumper bd_scrambler / bd::DataFrame.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using Aaru.Decoders.DVD;
using Aaru.Decoders.Bluray;

namespace Aaru.Decoders.Bluray;

/// <summary>Blu-ray DataFrame: 2048 bytes main_data + 4 bytes EDC.</summary>
public static class DataFrame
{
    /// <summary>Total size of one BD DataFrame.</summary>
    public const int Size = 2048 + 4;
    public const int UserControlDataSize = 18;

    /// <summary>Descrambles a 2052-byte frame in place (ISO/IEC 30190 LFSR XOR).</summary>
    public static void Descramble(Span<byte> frame, int lba, bool nintendo)
    {
        if(frame.Length != Size) throw new ArgumentException(nameof(frame));

        ushort seed = nintendo ? SeedNintendo(lba) : SeedBluray(lba);
        Process(frame, seed);
    }

    /// <summary>Copies <paramref name="scrambled" />, descrambles the copy, returns whether EDC matches.</summary>
    public static bool IsValid(ReadOnlySpan<byte> scrambled2052, int lba, bool nintendo)
    {
        if(scrambled2052.Length != Size) return false;

        byte[] work = new byte[Size];
        scrambled2052.CopyTo(work);
        Descramble(work, lba, nintendo);

        return Sector.CheckEdc(work);
    }

    static ushort SeedBluray(int lba)
    {
        uint psn = (uint)lba + 0x100000;

        return (ushort)(psn >> 5);
    }

    static ushort SeedNintendo(int lba)
    {
        uint m      = (uint)lba & 0xFFFE0;
        uint local  = m & 0xFFFF;
        uint k      = local >> 8;
        uint slope  = (local & 0xFF) << 7;
        uint layer  = (m >> 16) & 0xF;
        uint jump4k = ((local >> 12) & 0x3) << 9;
        uint jump32k = (local >> 15) << 12;
        uint toggle = (k & 1) << 5;
        uint decay  = k << 4;

        return (ushort)(slope + 1248 + layer + toggle + jump4k + jump32k - decay);
    }

    static void Process(Span<byte> data, ushort seed)
    {
        ushort shiftRegister = (ushort)(0x8000 | (seed & 0x7FFF));

        for(int i = 0; i < data.Length; i++)
        {
            data[i] ^= (byte)shiftRegister;

            for(int b = 0; b < 8; b++)
            {
                int lsb = ((shiftRegister >> 15) ^ (shiftRegister >> 14) ^ (shiftRegister >> 12) ^ (shiftRegister >> 3)) & 1;
                shiftRegister = (ushort)((shiftRegister << 1) | lsb);
            }
        }
    }
}
