// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DataMap.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Image conversion.
//
// --[ Description ] ----------------------------------------------------------
//
//     FST-based data region map for classifying disc sectors as data or junk.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program. If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2019-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using Aaru.Helpers;

namespace Aaru.Core.Image.Ngcw;

/// <summary>A contiguous region of file data on disc.</summary>
readonly struct DataRegion : IComparable<DataRegion>
{
    public readonly ulong Offset;
    public readonly ulong Length;

    public DataRegion(ulong offset, ulong length)
    {
        Offset = offset;
        Length = length;
    }

    public int CompareTo(DataRegion other) => Offset.CompareTo(other.Offset);
}

/// <summary>
///     Sorted map of file data regions parsed from a Nintendo GameCube/Wii FST.
///     Used to classify disc sectors as data (file content / system area) or potential junk.
/// </summary>
static class DataMap
{
    /// <summary>
    ///     Build a data region map from an FST (File System Table).
    /// </summary>
    /// <param name="fst">Raw FST bytes.</param>
    /// <param name="dataStart">Base offset added to each file's offset (typically 0).</param>
    /// <param name="addressShift">
    ///     Left-shift applied to the file offset field.
    ///     0 for GameCube, 2 for Wii.
    /// </param>
    /// <returns>Sorted array of data regions, or null on error.</returns>
    public static DataRegion[] BuildFromFst(byte[] fst, ulong dataStart, int addressShift)
    {
        if(fst == null || fst.Length < 12) return null;

        var totalEntries = BigEndianBitConverter.ToUInt32(fst, 8);

        if(totalEntries * 12 > (uint)fst.Length) return null;

        List<DataRegion> regions = new();

        for(uint i = 1; i < totalEntries; i++)
        {
            var entryOffset = (int)(i * 12);

            if(fst[entryOffset] != 0) // not a file
                continue;

            ulong off = (ulong)BigEndianBitConverter.ToUInt32(fst, entryOffset + 4) << addressShift;
            ulong len = BigEndianBitConverter.ToUInt32(fst, entryOffset + 8);

            if(len == 0) continue;

            regions.Add(new DataRegion(dataStart + off, len));
        }

        regions.Sort();

        return regions.ToArray();
    }

    /// <summary>
    ///     Check if a byte range overlaps any data region using binary search.
    /// </summary>
    /// <param name="regions">Sorted data regions.</param>
    /// <param name="offset">Start byte offset to check.</param>
    /// <param name="length">Length of the range to check.</param>
    /// <returns><c>true</c> if the range overlaps a data region.</returns>
    public static bool IsDataRegion(DataRegion[] regions, ulong offset, ulong length)
    {
        if(regions == null || regions.Length == 0) return false;

        var lo = 0;
        int hi = regions.Length - 1;

        while(lo <= hi)
        {
            int   mid = lo                  + (hi - lo) / 2;
            ulong end = regions[mid].Offset + regions[mid].Length;

            if(end <= offset)
                lo = mid + 1;
            else if(regions[mid].Offset >= offset + length)
                hi = mid - 1;
            else
                return true;
        }

        return false;
    }
}