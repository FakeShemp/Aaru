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
//     Decodes and descrambles DVD sectors.
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;

namespace Aaru.Decoders.DVD;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class Sector
{
    /// <summary>Offset of main user data in a standard ECMA-267 DVD sector (bytes 12-2059).</summary>
    const int DvdMainDataOffset = 12;

    public const int Form1DataSize = 2048;
    const int Ecma267TableSize = 16 * Form1DataSize; // 32768

    static readonly byte[] _ecma267Table = BuildEcma267Table();

    static byte[] BuildEcma267Table()
    {
        byte[] table = new byte[Ecma267TableSize];
        ushort shiftRegister = 0x0001;

        for(int t = 0; t < Ecma267TableSize; t++)
        {
            table[t] = (byte)shiftRegister;

            for(int b = 0; b < 8; b++)
            {
                int lsb = (shiftRegister >> 14 & 1) ^ (shiftRegister >> 10 & 1);
                shiftRegister = (ushort)((shiftRegister << 1 | (ushort)lsb) & 0x7FFF);
            }
        }

        return table;
    }

    /// <summary>Gets the Physical Sector Number (PSN) from the sector header (bytes 1-3, big-endian).</summary>
    public static int GetPsn(byte[] sector)
    {
        if(sector == null || sector.Length < 4) return 0;

        return (sector[1] << 16) | (sector[2] << 8) | sector[3];
    }

    public static void ApplyTableWithWrap(byte[] sector, int dataStart, int tableOffset)
    {
        for(int i = 0; i < Form1DataSize; i++)
        {
            int index = tableOffset + i;

            if(index >= Ecma267TableSize)
                index -= Ecma267TableSize - 1;

            sector[dataStart + i] ^= _ecma267Table[index];
        }
    }

    static readonly ushort[] _ecma267InitialValues =
    [
        0x0001, 0x5500, 0x0002, 0x2A00, 0x0004, 0x5400, 0x0008, 0x2800, 0x0010, 0x5000, 0x0020, 0x2001, 0x0040,
        0x4002, 0x0080, 0x0005
    ];

    static readonly uint[] _edcTable =
    [
        0x00000000, 0x80000011, 0x80000033, 0x00000022, 0x80000077, 0x00000066, 0x00000044, 0x80000055, 0x800000FF,
        0x000000EE, 0x000000CC, 0x800000DD, 0x00000088, 0x80000099, 0x800000BB, 0x000000AA, 0x800001EF, 0x000001FE,
        0x000001DC, 0x800001CD, 0x00000198, 0x80000189, 0x800001AB, 0x000001BA, 0x00000110, 0x80000101, 0x80000123,
        0x00000132, 0x80000167, 0x00000176, 0x00000154, 0x80000145, 0x800003CF, 0x000003DE, 0x000003FC, 0x800003ED,
        0x000003B8, 0x800003A9, 0x8000038B, 0x0000039A, 0x00000330, 0x80000321, 0x80000303, 0x00000312, 0x80000347,
        0x00000356, 0x00000374, 0x80000365, 0x00000220, 0x80000231, 0x80000213, 0x00000202, 0x80000257, 0x00000246,
        0x00000264, 0x80000275, 0x800002DF, 0x000002CE, 0x000002EC, 0x800002FD, 0x000002A8, 0x800002B9, 0x8000029B,
        0x0000028A, 0x8000078F, 0x0000079E, 0x000007BC, 0x800007AD, 0x000007F8, 0x800007E9, 0x800007CB, 0x000007DA,
        0x00000770, 0x80000761, 0x80000743, 0x00000752, 0x80000707, 0x00000716, 0x00000734, 0x80000725, 0x00000660,
        0x80000671, 0x80000653, 0x00000642, 0x80000617, 0x00000606, 0x00000624, 0x80000635, 0x8000069F, 0x0000068E,
        0x000006AC, 0x800006BD, 0x000006E8, 0x800006F9, 0x800006DB, 0x000006CA, 0x00000440, 0x80000451, 0x80000473,
        0x00000462, 0x80000437, 0x00000426, 0x00000404, 0x80000415, 0x800004BF, 0x000004AE, 0x0000048C, 0x8000049D,
        0x000004C8, 0x800004D9, 0x800004FB, 0x000004EA, 0x800005AF, 0x000005BE, 0x0000059C, 0x8000058D, 0x000005D8,
        0x800005C9, 0x800005EB, 0x000005FA, 0x00000550, 0x80000541, 0x80000563, 0x00000572, 0x80000527, 0x00000536,
        0x00000514, 0x80000505, 0x80000F0F, 0x00000F1E, 0x00000F3C, 0x80000F2D, 0x00000F78, 0x80000F69, 0x80000F4B,
        0x00000F5A, 0x00000FF0, 0x80000FE1, 0x80000FC3, 0x00000FD2, 0x80000F87, 0x00000F96, 0x00000FB4, 0x80000FA5,
        0x00000EE0, 0x80000EF1, 0x80000ED3, 0x00000EC2, 0x80000E97, 0x00000E86, 0x00000EA4, 0x80000EB5, 0x80000E1F,
        0x00000E0E, 0x00000E2C, 0x80000E3D, 0x00000E68, 0x80000E79, 0x80000E5B, 0x00000E4A, 0x00000CC0, 0x80000CD1,
        0x80000CF3, 0x00000CE2, 0x80000CB7, 0x00000CA6, 0x00000C84, 0x80000C95, 0x80000C3F, 0x00000C2E, 0x00000C0C,
        0x80000C1D, 0x00000C48, 0x80000C59, 0x80000C7B, 0x00000C6A, 0x80000D2F, 0x00000D3E, 0x00000D1C, 0x80000D0D,
        0x00000D58, 0x80000D49, 0x80000D6B, 0x00000D7A, 0x00000DD0, 0x80000DC1, 0x80000DE3, 0x00000DF2, 0x80000DA7,
        0x00000DB6, 0x00000D94, 0x80000D85, 0x00000880, 0x80000891, 0x800008B3, 0x000008A2, 0x800008F7, 0x000008E6,
        0x000008C4, 0x800008D5, 0x8000087F, 0x0000086E, 0x0000084C, 0x8000085D, 0x00000808, 0x80000819, 0x8000083B,
        0x0000082A, 0x8000096F, 0x0000097E, 0x0000095C, 0x8000094D, 0x00000918, 0x80000909, 0x8000092B, 0x0000093A,
        0x00000990, 0x80000981, 0x800009A3, 0x000009B2, 0x800009E7, 0x000009F6, 0x000009D4, 0x800009C5, 0x80000B4F,
        0x00000B5E, 0x00000B7C, 0x80000B6D, 0x00000B38, 0x80000B29, 0x80000B0B, 0x00000B1A, 0x00000BB0, 0x80000BA1,
        0x80000B83, 0x00000B92, 0x80000BC7, 0x00000BD6, 0x00000BF4, 0x80000BE5, 0x00000AA0, 0x80000AB1, 0x80000A93,
        0x00000A82, 0x80000AD7, 0x00000AC6, 0x00000AE4, 0x80000AF5, 0x80000A5F, 0x00000A4E, 0x00000A6C, 0x80000A7D,
        0x00000A28, 0x80000A39, 0x80000A1B, 0x00000A0A
    ];

    readonly Dictionary<ushort, byte[]> _seeds = new();

    ushort _lastSeed;

    ushort _lfsr;

    void LfsrInit(ushort seed) => _lfsr = seed;

    int LfsrTick()
    {
        int ret = _lfsr >> 14;

        int n = ret ^ _lfsr >> 10 & 1;
        _lfsr = (ushort)((_lfsr << 1 | n) & 0x7FFF);

        return ret;
    }

    byte LfsrByte()
    {
        byte ret = 0;

        for(var i = 0; i < 8; i++) ret = (byte)(ret << 1 | LfsrTick());

        return ret;
    }

    static readonly byte[] DvdGfExp = CreateDvdGfExpTable();
    static readonly int[] DvdGfLog = CreateDvdGfLogTable(DvdGfExp);

    static byte[] CreateDvdGfExpTable()
    {
        const ushort primitive = 0x11D;
        byte[] exp = new byte[512];
        exp[0] = 1;

        for(int i = 1; i < 255; i++)
        {
            ushort x = (ushort)(exp[i - 1] << 1);

            if((x & 0x100) != 0) x ^= primitive;

            exp[i] = (byte)x;
        }

        for(int i = 255; i < 512; i++) exp[i] = exp[i - 255];

        return exp;
    }

    static int[] CreateDvdGfLogTable(byte[] exp)
    {
        int[] log = new int[256];

        for(int i = 0; i < 256; i++) log[i] = -1;

        for(int i = 0; i < 255; i++) log[exp[i]] = i;

        log[0] = -1;

        return log;
    }

    static byte DvdGfMul(byte a, byte b)
    {
        if(a == 0 || b == 0) return 0;

        return DvdGfExp[DvdGfLog[a] + DvdGfLog[b]];
    }

    /// <summary>
    ///     Store seed and its cipher in cache
    /// </summary>
    /// <param name="seed">The seed to store</param>
    /// <returns>The cipher for the seed</returns>
    byte[] AddSeed(ushort seed)
    {
        int i;
        var cypher = new byte[2048];

        LfsrInit(seed);

        for(i = 0; i < 2048; i++) cypher[i] = LfsrByte();

        _seeds.Add(seed, cypher);

        return cypher;
    }

    static uint ComputeEdc(uint edc, IReadOnlyList<byte> src, int size)
    {
        var pos = 0;

        for(; size > 0; size--) edc = _edcTable[(edc >> 24 ^ src[pos++]) & 0xFF] ^ edc << 8;

        return edc;
    }

    /// <summary>
    ///     Check if the EDC of a sector is correct
    /// </summary>
    /// <param name="sectorLong">Buffer of the sector</param>
    /// <returns><c>True</c> if EDC is correct, <c>False</c> if not</returns>
    public static bool CheckEdc(byte[] sectorLong) => ComputeEdc(0, sectorLong, 2060) == BigEndianBitConverter.ToUInt32(sectorLong, 2060);

    /// <summary>
    ///     Check if the EDC of sectors is correct
    /// </summary>
    /// <param name="sectorLong">Buffer of the sector</param>
    /// <param name="transferLength">The number of sectors to check</param>
    /// <returns><c>True</c> if EDC is correct, <c>False</c> if not</returns>
    public static bool CheckEdc(byte[] sectorLong, uint transferLength)
    {
        for(uint i = 0; i < transferLength; i++)
        {
            if(!CheckEdc(sectorLong.Skip((int)(i * 2064)).Take(2064).ToArray())) return false;
        }
        return true;
    }

    /// <summary>
    ///     Computes the 16-bit ID Error Detection (IED) for bytes 0-3 of a DVD ROM sector (ID + 3-byte PSN),
    ///     per the ECMA-267 RS remainder step used in <c>gpsxre::dvd::DataFrame::ID::valid()</c> (redumper).
    /// </summary>
    /// <param name="sectorLong">Buffer of the sector (must be at least 4 bytes; first 4 bytes are the ID field).</param>
    /// <returns>The IED value in the same byte order as stored at sector bytes 4-5 (little-endian uint16 layout).</returns>
    public static ushort ComputeIed(byte[] sectorLong)
    {
        if(sectorLong == null || sectorLong.Length < 4) return 0;

        // G(x) = x^2 + g1*x + g2, with g1 = alpha^0 + alpha^1, g2 = alpha^0 * alpha^1 in GF(2^8)
        byte g1 = (byte)(1 ^ DvdGfExp[1]);
        byte g2 = DvdGfExp[1];

        Span<byte> poly = stackalloc byte[6];
        poly[0] = sectorLong[0];
        poly[1] = sectorLong[1];
        poly[2] = sectorLong[2];
        poly[3] = sectorLong[3];
        poly[4] = 0;
        poly[5] = 0;

        for(int i = 0; i < 4; i++)
        {
            byte coef = poly[i];

            if(coef == 0) continue;

            poly[i] = 0;
            poly[i + 1] ^= DvdGfMul(coef, g1);
            poly[i + 2] ^= DvdGfMul(coef, g2);
        }

        return (ushort)(poly[4] | poly[5] << 8);
    }

    /// <summary>
    ///     Check if the IED of a sector is correct (bytes 0-3 vs bytes 4-5, same layout as redumper <c>DataFrame::ID</c>).
    /// </summary>
    /// <param name="sectorLong">Buffer of the sector</param>
    /// <returns><c>True</c> if IED is correct, <c>False</c> if not</returns>
    public static bool CheckIed(byte[] sectorLong)
    {
        if(sectorLong == null || sectorLong.Length < 6) return false;

        ushort computed = ComputeIed(sectorLong);
        ushort stored = (ushort)(sectorLong[4] | sectorLong[5] << 8);

        return computed == stored;
    }

    /// <summary>
    ///     Check if the IED of sectors is correct
    /// </summary>
    /// <param name="sectorLong">Buffer of the sector</param>
    /// <param name="transferLength">The number of sectors to check</param>
    /// <returns><c>True</c> if IED is correct, <c>False</c> if not</returns>
    public static bool CheckIed(byte[] sectorLong, uint transferLength)
    {
        for(uint i = 0; i < transferLength; i++)
        {
            if(!CheckIed(sectorLong.Skip((int)(i * 2064)).Take(2064).ToArray())) return false;
        }
        return true;
    }

    /// <summary>
    ///     Tests if a seed unscrambles a sector correctly
    /// </summary>
    /// <param name="sector">Buffer of the scrambled sector</param>
    /// <param name="seed">Seed to test</param>
    /// <returns><c>True</c> if seed is correct, <c>False</c> if not</returns>
    bool TestSeed(in byte[] sector, ushort seed)
    {
        var tmp = new byte[sector.Length];
        Array.Copy(sector, 0, tmp, 0, sector.Length);

        LfsrInit(seed);

        for(var i = 12; i < 2060; i++) tmp[i] ^= LfsrByte();

        return CheckEdc(tmp);
    }

    /// <summary>
    ///     Find the seed used for scrambling a sector
    /// </summary>
    /// <param name="sector">Buffer of the scrambled sector.</param>
    /// <returns>The scramble cipher</returns>
    byte[]? GetSeed(byte[] sector)
    {
        // Try the last used key
        if(TestSeed(sector, _lastSeed)) return _seeds[_lastSeed];

        // Try the cached keys
        foreach(ushort seedsKey in _seeds.Keys.Where(seedsKey => TestSeed(sector, seedsKey)))
        {
            _lastSeed = seedsKey;

            return _seeds[seedsKey];
        }

        // Try the ECMA-267 keys since they are often used
        foreach(ushort iv in _ecma267InitialValues.Where(iv => TestSeed(sector, iv)))
        {
            _lastSeed = iv;

            return AddSeed(iv);
        }

        // Brute force all other keys
        for(ushort i = 0; i < 0x7FFF; i++)
        {
            if(!TestSeed(sector, i)) continue;

            _lastSeed = i;

            return AddSeed(i);
        }

        return null;
    }

    /// <summary>
    ///     Unscramble a sector with a cipher
    /// </summary>
    /// <param name="sector">Buffer of the scrambled sector</param>
    /// <param name="cipher">Buffer of the scrambling cipher</param>
    /// <param name="scrambled">Buffer of unscrambled sector data</param>
    /// <returns>The Error.</returns>
    static ErrorNumber UnscrambleSector(byte[] sector, IReadOnlyList<byte> cipher, out byte[] scrambled)
    {
        scrambled = new byte[sector.Length];
        Array.Copy(sector, 0, scrambled, 0, sector.Length);

        for(var i = 0; i < 2048; i++) scrambled[i + 12] = (byte)(sector[i + 12] ^ cipher[i]);

        return !CheckEdc(scrambled) ? ErrorNumber.NotVerifiable : ErrorNumber.NoError;
    }

    /// <summary>
    ///     Descrambles a DVD sector. Uses PSN from header (bytes 1-3) to select XOR table.
    /// </summary>
    /// <param name="sector">Scrambled 2064-byte sector</param>
    /// <param name="scrambled">Descrambled sector output</param>
    public ErrorNumber Scramble(byte[] sector, out byte[] scrambled)
    {
        scrambled = new byte[sector.Length];

        if(sector is not { Length: 2064 }) return ErrorNumber.NotSupported;

        int psn = GetPsn(sector);

        // Standard DVD: try PSN-based descramble first
        int offset = (psn >> 4 & 0xF) * Form1DataSize;
        Array.Copy(sector, 0, scrambled, 0, sector.Length);
        ApplyTableWithWrap(scrambled, DvdMainDataOffset, offset);

        if(CheckEdc(scrambled))
            return ErrorNumber.NoError;

        // Fallback: seed search for non-standard
        byte[]? cipher = GetSeed(sector);

        return cipher == null ? ErrorNumber.UnrecognizedFormat : UnscrambleSector(sector, cipher, out scrambled);
    }

    public ErrorNumber Scramble(byte[] sector, uint transferLength, out byte[] scrambled)
    {
        scrambled = new byte[sector.Length];

        if(sector.Length % 2064 != 0 || sector.Length / 2064 != transferLength) return ErrorNumber.NotSupported;

        for(uint i = 0; i < transferLength; i++)
        {
            ErrorNumber error = Scramble(sector.Skip((int)(i * 2064)).Take(2064).ToArray(), out byte[]? currentSector);

            if(error != ErrorNumber.NoError) return error;

            Array.Copy(currentSector, 0, scrambled, i * 2064, 2064);
        }

        return ErrorNumber.NoError;
    }

    public static byte[] GetUserData(byte[] data)
    {
        if(data.Length != 2064) return data;

        byte[] sector = new byte[2048];
        Array.Copy(data, 12, sector, 0, 2048);

        return sector;
    }
}