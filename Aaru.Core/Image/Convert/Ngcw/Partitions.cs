// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Partitions.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Image conversion.
//
// --[ Description ] ----------------------------------------------------------
//
//     Wii partition table parsing and partition key map serialization.
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
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Core.Image.Ngcw;

/// <summary>In-memory representation of a Wii partition.</summary>
struct WiiPartition
{
    /// <summary>Partition offset on disc.</summary>
    public ulong Offset;

    /// <summary>Data area offset on disc.</summary>
    public ulong DataOffset;

    /// <summary>Data area size in bytes.</summary>
    public ulong DataSize;

    /// <summary>Partition type (0=game, 1=update, 2=channel).</summary>
    public uint Type;

    /// <summary>Decrypted 16-byte AES-128 title key.</summary>
    public byte[] TitleKey;
}

/// <summary>Wii partition region for the partition key map.</summary>
struct WiiPartitionRegion
{
    /// <summary>First physical sector (0x8000-byte units).</summary>
    public uint StartSector;

    /// <summary>End physical sector (exclusive).</summary>
    public uint EndSector;

    /// <summary>16-byte AES-128 partition key.</summary>
    public byte[] Key;
}

/// <summary>
///     Wii partition table parsing and key map serialization.
/// </summary>
static class Partitions
{
    /// <summary>
    ///     Parse the Wii partition table from a source image, extracting all partitions
    ///     and decrypting their title keys.
    /// </summary>
    /// <param name="inputImage">Source image (must support ReadSectors).</param>
    /// <returns>List of parsed partitions, or null on error.</returns>
    public static List<WiiPartition> ParseWiiPartitions(IMediaImage inputImage)
    {
        // Read partition table info at disc offset 0x40000 (32 bytes)
        byte[] ptableRaw = ReadDiscBytes(inputImage, 0x40000, 32);

        if(ptableRaw == null) return null;

        var partitions = new List<WiiPartition>();
        var counts     = new uint[4];
        var offsets    = new uint[4];
        var total      = 0;

        for(var t = 0; t < 4; t++)
        {
            counts[t]  =  BigEndianBitConverter.ToUInt32(ptableRaw, t * 8);
            offsets[t] =  BigEndianBitConverter.ToUInt32(ptableRaw, t * 8 + 4);
            total      += (int)counts[t];
        }

        if(total == 0) return partitions;

        for(var t = 0; t < 4; t++)
        {
            if(counts[t] == 0) continue;

            ulong tableOffset = (ulong)offsets[t] << 2;
            int   tableSize   = (int)counts[t] * 8;

            byte[] tableData = ReadDiscBytes(inputImage, tableOffset, tableSize);

            if(tableData == null) return null;

            for(uint p = 0; p < counts[t]; p++)
            {
                ulong partOffset = (ulong)BigEndianBitConverter.ToUInt32(tableData, (int)p * 8) << 2;
                var   partType   = BigEndianBitConverter.ToUInt32(tableData, (int)p * 8 + 4);

                // Read ticket (0x2A4 bytes) at partition offset
                byte[] ticket = ReadDiscBytes(inputImage, partOffset, 0x2A4);

                if(ticket == null) return null;

                // Decrypt title key
                byte[] titleKey = Crypto.DecryptTitleKey(ticket);

                // Read partition header for data offset/size (8 bytes at partOffset + 0x2B8)
                byte[] phdr = ReadDiscBytes(inputImage, partOffset + 0x2B8, 8);

                if(phdr == null) return null;

                ulong dataOffset = partOffset + ((ulong)BigEndianBitConverter.ToUInt32(phdr, 0) << 2);
                ulong dataSize   = (ulong)BigEndianBitConverter.ToUInt32(phdr, 4) << 2;

                partitions.Add(new WiiPartition
                {
                    Offset     = partOffset,
                    DataOffset = dataOffset,
                    DataSize   = dataSize,
                    Type       = partType,
                    TitleKey   = titleKey
                });
            }
        }

        return partitions;
    }

    /// <summary>
    ///     Build a partition region map from parsed partitions.
    ///     Regions are sorted by data offset.
    /// </summary>
    public static WiiPartitionRegion[] BuildRegionMap(List<WiiPartition> partitions)
    {
        // Sort by data offset
        partitions.Sort((a, b) => a.DataOffset.CompareTo(b.DataOffset));

        var regions = new WiiPartitionRegion[partitions.Count];

        for(var i = 0; i < partitions.Count; i++)
        {
            regions[i] = new WiiPartitionRegion
            {
                StartSector = (uint)(partitions[i].DataOffset                            / Crypto.GROUP_SIZE),
                EndSector   = (uint)((partitions[i].DataOffset + partitions[i].DataSize) / Crypto.GROUP_SIZE),
                Key         = partitions[i].TitleKey
            };
        }

        return regions;
    }

    /// <summary>
    ///     Serialize the partition key map for storage as a media tag.
    ///     Format: count(u32 LE) + per-entry(start_sector u32 LE, end_sector u32 LE, key 16 bytes).
    /// </summary>
    public static byte[] SerializeKeyMap(WiiPartitionRegion[] regions)
    {
        int size = 4 + regions.Length * 24;
        var buf  = new byte[size];

        BitConverter.TryWriteBytes(buf.AsSpan(0, 4), (uint)regions.Length);

        for(var i = 0; i < regions.Length; i++)
        {
            int offset = 4 + i * 24;
            BitConverter.TryWriteBytes(buf.AsSpan(offset,     4), regions[i].StartSector);
            BitConverter.TryWriteBytes(buf.AsSpan(offset + 4, 4), regions[i].EndSector);
            Array.Copy(regions[i].Key, 0, buf, offset + 8, 16);
        }

        return buf;
    }

    /// <summary>
    ///     Find which partition (if any) contains the given disc byte offset.
    /// </summary>
    /// <param name="partitions">List of parsed partitions (sorted by data offset).</param>
    /// <param name="discOffset">Disc byte offset to look up.</param>
    /// <returns>Index into partitions, or -1 if not inside any partition data area.</returns>
    public static int FindPartitionAtOffset(List<WiiPartition> partitions, ulong discOffset)
    {
        for(var i = 0; i < partitions.Count; i++)
        {
            if(discOffset >= partitions[i].DataOffset && discOffset < partitions[i].DataOffset + partitions[i].DataSize)
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Read arbitrary bytes from a disc image at a given byte offset.
    ///     Handles the sector-based reading internally.
    /// </summary>
    static byte[] ReadDiscBytes(IMediaImage image, ulong byteOffset, int length)
    {
        var          result     = new byte[length];
        var          read       = 0;
        const ushort sectorSize = Crypto.SECTOR_SIZE;

        while(read < length)
        {
            ulong sector    = (byteOffset + (ulong)read) / sectorSize;
            var   sectorOff = (int)((byteOffset + (ulong)read) % sectorSize);
            int   chunk     = sectorSize - sectorOff;

            if(chunk > length - read) chunk = length - read;

            ErrorNumber errno = image.ReadSector(sector, false, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError || sectorData == null) return null;

            Array.Copy(sectorData, sectorOff, result, read, chunk);
            read += chunk;
        }

        return result;
    }
}