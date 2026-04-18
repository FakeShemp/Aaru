// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains helpers for partclone disk images.
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

namespace Aaru.Images;

public sealed partial class PartClone
{
    /// <summary>
    ///     Reflected CRC-32 lookup table (polynomial <see cref="CRC32_POLYNOMIAL" />) used by partclone's
    ///     <c>crc32_0001()</c> implementation. Kept private to the plugin because that routine carries a source-level
    ///     bug (see <see cref="UpdateCrc32_0001" />) that would be incorrect for any other CRC-32 consumer.
    /// </summary>
    static readonly uint[] _crc0001Table = BuildCrc0001Table();

    static uint[] BuildCrc0001Table()
    {
        var table = new uint[256];

        for(uint i = 0; i < 256; i++)
        {
            uint entry = i;

            for(var j = 0; j < 8; j++)
            {
                if((entry & 1) != 0)
                    entry = entry >> 1 ^ CRC32_POLYNOMIAL;
                else
                    entry >>= 1;
            }

            table[i] = entry;
        }

        return table;
    }

    /// <summary>
    ///     Advances a running CRC-32 seed the way partclone's <c>crc32_0001()</c> does for image format 0001. The
    ///     upstream routine has a long-standing bug: the input pointer is <em>never advanced</em>, so every iteration
    ///     folds the very first byte of <paramref name="buffer" /> into the CRC. This quirk is intentionally
    ///     reproduced here so that Aaru's verification matches the checksums actually stored on disk.
    /// </summary>
    /// <param name="crc">Previous cumulative CRC (<see cref="CRC32_SEED" /> at the start of the stream).</param>
    /// <param name="buffer">Block buffer; only its first byte is used.</param>
    /// <param name="size">Number of iterations to run (i.e. the block size in bytes).</param>
    /// <returns>Updated cumulative CRC.</returns>
    static uint UpdateCrc32_0001(uint crc, byte[] buffer, int size)
    {
        if(size <= 0 || buffer is null || buffer.Length == 0) return crc;

        byte b = buffer[0];

        for(var s = 0; s < size; s++)
        {
            uint tmp = crc ^ b;
            crc = crc >> 8 ^ _crc0001Table[tmp & 0xFF];
        }

        return crc;
    }

    ulong BlockOffset(ulong sectorAddress)
    {
        _extents.GetStart(sectorAddress, out ulong extentStart);
        _extentsOff.TryGetValue(extentStart, out ulong extentStartingOffset);

        return extentStartingOffset + (sectorAddress - extentStart);
    }
}