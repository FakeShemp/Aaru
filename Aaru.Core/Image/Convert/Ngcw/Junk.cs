// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Junk.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Image conversion.
//
// --[ Description ] ----------------------------------------------------------
//
//     Nintendo GameCube/Wii junk detection, collection, and serialization.
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
using Aaru.CommonTypes.Enums;

namespace Aaru.Core.Image.Ngcw;

/// <summary>In-memory junk map entry.</summary>
struct JunkEntry
{
    /// <summary>Disc byte offset where junk starts.</summary>
    public ulong Offset;

    /// <summary>Length of junk region in bytes.</summary>
    public ulong Length;

    /// <summary>Partition index (0xFFFF for GC / inter-partition).</summary>
    public ushort PartitionIndex;

    /// <summary>LFG seed (17 words, big-endian).</summary>
    public uint[] Seed;
}

/// <summary>
///     Collects junk entries during conversion, merging contiguous entries with the same seed.
/// </summary>
sealed class JunkCollector
{
    public int Count => Entries.Count;

    public List<JunkEntry> Entries { get; } = new();

    /// <summary>
    ///     Add a junk region, merging with the last entry if contiguous with the same seed and partition.
    /// </summary>
    public void Add(ulong offset, ulong length, ushort partitionIndex, uint[] seed)
    {
        // Try merge with last entry
        if(Entries.Count > 0)
        {
            JunkEntry last = Entries[^1];

            if(last.PartitionIndex       == partitionIndex &&
               last.Offset + last.Length == offset         &&
               SeedEquals(last.Seed, seed))
            {
                last.Length += length;
                Entries[^1] =  last;

                return;
            }
        }

        var seedCopy = new uint[Lfg.SEED_SIZE];
        Array.Copy(seed, seedCopy, Lfg.SEED_SIZE);

        Entries.Add(new JunkEntry
        {
            Offset         = offset,
            Length         = length,
            PartitionIndex = partitionIndex,
            Seed           = seedCopy
        });
    }

    static bool SeedEquals(uint[] a, uint[] b)
    {
        if(a == null || b == null) return false;

        for(var i = 0; i < Lfg.SEED_SIZE; i++)
        {
            if(a[i] != b[i]) return false;
        }

        return true;
    }
}

/// <summary>
///     Junk map serialization/deserialization and block-level junk detection.
/// </summary>
static class Junk
{
    const ushort JUNK_MAP_VERSION  = 1;
    const int    JUNK_MAP_HEADER   = 8; // version(2) + count(4) + seed_size(2)
    const int    SECTOR_SIZE       = 2048;
    const int    SECTORS_PER_BLOCK = 16;

    /// <summary>
    ///     Serialize a junk map for storage as a media tag.
    /// </summary>
    public static byte[] Serialize(List<JunkEntry> entries)
    {
        int entrySize = 18              + Lfg.SEED_SIZE * sizeof(uint); // 86 bytes
        int totalSize = JUNK_MAP_HEADER + entries.Count * entrySize;
        var buf       = new byte[totalSize];

        BitConverter.TryWriteBytes(buf.AsSpan(0, 2), JUNK_MAP_VERSION);      // version
        BitConverter.TryWriteBytes(buf.AsSpan(2, 4), (uint)entries.Count);   // entry_count
        BitConverter.TryWriteBytes(buf.AsSpan(6, 2), (ushort)Lfg.SEED_SIZE); // seed_size

        for(var i = 0; i < entries.Count; i++)
        {
            int offset = JUNK_MAP_HEADER + i * entrySize;

            BitConverter.TryWriteBytes(buf.AsSpan(offset,      8), entries[i].Offset);
            BitConverter.TryWriteBytes(buf.AsSpan(offset + 8,  8), entries[i].Length);
            BitConverter.TryWriteBytes(buf.AsSpan(offset + 16, 2), entries[i].PartitionIndex);

            // Seed: raw bytes (big-endian uint32 words stored as-is)
            Buffer.BlockCopy(entries[i].Seed, 0, buf, offset + 18, Lfg.SEED_SIZE * sizeof(uint));
        }

        return buf;
    }

    /// <summary>
    ///     Detect junk sectors in a 0x8000-byte block.
    ///     Exact port of detect_junk_in_block from tool/ngcw/convert.c.
    /// </summary>
    /// <param name="blockBuf">Block data (up to 0x8000 bytes).</param>
    /// <param name="blockBytes">Actual number of valid bytes in <paramref name="blockBuf" />.</param>
    /// <param name="blockOff">Disc byte offset of this block.</param>
    /// <param name="dataMap">FST data map (null to treat all sectors as data).</param>
    /// <param name="sysEnd">End of system area (below this = always data).</param>
    /// <param name="partitionIndex">Partition index for the junk collector (0xFFFF for GC / inter-partition).</param>
    /// <param name="jc">Junk collector to record detected junk entries.</param>
    /// <param name="dataSectors">Incremented for each data sector.</param>
    /// <param name="junkSectors">Incremented for each junk sector.</param>
    /// <param name="sectorStatusOut">
    ///     Output array of per-sector status (SECTORS_PER_BLOCK elements).
    /// </param>
    public static void DetectJunkInBlock(byte[] blockBuf, int blockBytes, ulong blockOff, DataRegion[] dataMap,
                                         ulong sysEnd, ushort partitionIndex, JunkCollector jc, ref ulong dataSectors,
                                         ref ulong junkSectors, SectorStatus[] sectorStatusOut)
    {
        int numSectors = blockBytes / SECTOR_SIZE;

        if(blockBytes % SECTOR_SIZE != 0) numSectors++;

        // Classify sectors
        var sectorIsData = new bool[SECTORS_PER_BLOCK];

        for(var si = 0; si < numSectors; si++)
        {
            ulong off  = blockOff + (ulong)si * SECTOR_SIZE;
            int   slen = SECTOR_SIZE;

            if(si * SECTOR_SIZE + slen > blockBytes) slen = blockBytes - si * SECTOR_SIZE;

            if(off < sysEnd)
                sectorIsData[si] = true;
            else if(dataMap != null)
                sectorIsData[si] = DataMap.IsDataRegion(dataMap, off, (ulong)slen);
            else
                sectorIsData[si] = true;
        }

        // Try full-block LFG seed extraction
        var blockIsLfg = false;
        var blockSeed  = new uint[Lfg.SEED_SIZE];

        if(blockBytes >= Lfg.MIN_SEED_DATA_BYTES)
        {
            int matched = Lfg.GetSeed(blockBuf.AsSpan(0, blockBytes), 0, blockSeed);

            if(matched >= blockBytes) blockIsLfg = true;
        }

        if(blockIsLfg)
        {
            for(var si = 0; si < numSectors; si++)
            {
                int slen = SECTOR_SIZE;

                if(si * SECTOR_SIZE + slen > blockBytes) slen = blockBytes - si * SECTOR_SIZE;

                if(sectorIsData[si])
                {
                    sectorStatusOut[si] = SectorStatus.Dumped;
                    dataSectors++;
                }
                else
                {
                    sectorStatusOut[si] = SectorStatus.Generable;
                    jc.Add(blockOff + (ulong)si * SECTOR_SIZE, (ulong)slen, partitionIndex, blockSeed);
                    junkSectors++;
                }
            }
        }
        else
        {
            // Mixed block: per-run seed extraction
            var si = 0;

            while(si < numSectors)
            {
                if(sectorIsData[si])
                {
                    sectorStatusOut[si] = SectorStatus.Dumped;
                    dataSectors++;
                    si++;

                    continue;
                }

                int runStart = si;

                while(si < numSectors && !sectorIsData[si]) si++;

                int runEnd       = si;
                int runByteStart = runStart * SECTOR_SIZE;
                int runByteEnd   = runEnd   * SECTOR_SIZE;

                if(runByteEnd > blockBytes) runByteEnd = blockBytes;

                int runBytes   = runByteEnd - runByteStart;
                var runHasSeed = false;
                var runSeed    = new uint[Lfg.SEED_SIZE];

                if(runBytes >= Lfg.MIN_SEED_DATA_BYTES)
                {
                    int matched = Lfg.GetSeed(blockBuf.AsSpan(runByteStart, runBytes), runByteStart, runSeed);

                    if(matched >= runBytes) runHasSeed = true;
                }

                for(int ri = runStart; ri < runEnd; ri++)
                {
                    int s    = ri * SECTOR_SIZE;
                    int slen = SECTOR_SIZE;

                    if(s + slen > blockBytes) slen = blockBytes - s;

                    if(runHasSeed)
                    {
                        // Verify this sector against LFG
                        var lfgBuffer = new uint[Lfg.MIN_SEED_DATA_BYTES / sizeof(uint)]; // K=521 words
                        var seedCopy  = new uint[Lfg.SEED_SIZE];
                        Array.Copy(runSeed, seedCopy, Lfg.SEED_SIZE);
                        Lfg.SetSeed(lfgBuffer, seedCopy);
                        var positionBytes = 0;

                        if(s > 0)
                        {
                            var discard = new byte[4096];
                            int adv     = s;

                            while(adv > 0)
                            {
                                int step = adv > discard.Length ? discard.Length : adv;
                                Lfg.GetBytes(lfgBuffer, ref positionBytes, discard, 0, step);
                                adv -= step;
                            }
                        }

                        var expected = new byte[SECTOR_SIZE];
                        Lfg.GetBytes(lfgBuffer, ref positionBytes, expected, 0, slen);

                        if(blockBuf.AsSpan(s, slen).SequenceEqual(expected.AsSpan(0, slen)))
                        {
                            sectorStatusOut[ri] = SectorStatus.Generable;
                            jc.Add(blockOff + (ulong)s, (ulong)slen, partitionIndex, runSeed);
                            junkSectors++;
                        }
                        else
                        {
                            sectorStatusOut[ri] = SectorStatus.Dumped;
                            dataSectors++;
                        }
                    }
                    else
                    {
                        // No seed — keep as data (zero-fill will dedup)
                        sectorStatusOut[ri] = SectorStatus.Dumped;
                        dataSectors++;
                    }
                }
            }
        }
    }
}