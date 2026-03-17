using System;
using System.Buffers.Binary;

namespace Aaru.Core.Image.WiiU;

/// <summary>A Wii U partition region entry mapping physical sector range to an AES key.</summary>
struct WiiuPartitionRegion
{
    /// <summary>First physical sector of partition (0x8000-byte units).</summary>
    public uint StartSector;
    /// <summary>End physical sector (exclusive).</summary>
    public uint EndSector;
    /// <summary>AES-128 key for encrypted sectors in this partition.</summary>
    public byte[] Key;
}

/// <summary>
///     Wii U partition key map: build from parsed partitions, serialize/deserialize
///     for the WiiUPartitionKeyMap media tag.
///     Format (little-endian): [4B count] then count × [4B start, 4B end, 16B key] = 24 bytes each.
/// </summary>
static class WiiuPartitionMap
{
    const int MAX_PARTITIONS = 8;

    /// <summary>
    ///     Build a sorted partition region map from parsed TOC partitions.
    ///     Regions are sorted by start_sector. End sector is the start of the next partition or total physical sectors.
    /// </summary>
    /// <param name="partitions">Parsed partition entries with keys.</param>
    /// <param name="totalPhysicalSectors">Total physical sectors on disc.</param>
    /// <returns>Sorted array of partition regions.</returns>
    public static WiiuPartitionRegion[] BuildRegionMap(WiiuPartition[] partitions, uint totalPhysicalSectors)
    {
        // Sort by start_sector
        var sorted = new WiiuPartition[partitions.Length];
        Array.Copy(partitions, sorted, partitions.Length);
        Array.Sort(sorted, (a, b) => a.StartSector.CompareTo(b.StartSector));

        var regions = new WiiuPartitionRegion[sorted.Length];

        for(var i = 0; i < sorted.Length; i++)
        {
            regions[i].StartSector = sorted[i].StartSector;
            regions[i].Key         = new byte[16];
            Array.Copy(sorted[i].Key, regions[i].Key, 16);

            // End sector: next partition's start, or disc end
            regions[i].EndSector = i + 1 < sorted.Length ? sorted[i + 1].StartSector : totalPhysicalSectors;
        }

        return regions;
    }

    /// <summary>
    ///     Serialize partition regions to little-endian binary for storage as a WiiUPartitionKeyMap media tag.
    ///     Format: [4B count LE] then count × [4B start LE, 4B end LE, 16B key] = 24 bytes each.
    /// </summary>
    /// <param name="regions">Array of partition regions.</param>
    /// <returns>Serialized byte array.</returns>
    public static byte[] Serialize(WiiuPartitionRegion[] regions)
    {
        int size   = 4 + regions.Length * 24;
        var buffer = new byte[size];

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), (uint)regions.Length);

        for(var i = 0; i < regions.Length; i++)
        {
            int offset = 4 + i * 24;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset,     4), regions[i].StartSector);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset + 4, 4), regions[i].EndSector);
            Array.Copy(regions[i].Key, 0, buffer, offset + 8, 16);
        }

        return buffer;
    }

    /// <summary>
    ///     Deserialize partition regions from little-endian binary (media tag format).
    /// </summary>
    /// <param name="data">Serialized data buffer.</param>
    /// <returns>Array of partition regions, or null on invalid data.</returns>
    public static WiiuPartitionRegion[] Deserialize(byte[] data)
    {
        if(data is null || data.Length < 4) return null;

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4));

        if(count > MAX_PARTITIONS) return null;

        if(count == 0) return [];

        uint required = 4 + count * 24;

        if(data.Length < required) return null;

        var regions = new WiiuPartitionRegion[count];

        for(uint i = 0; i < count; i++)
        {
            var offset = (int)(4 + i * 24);
            regions[i].StartSector = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset,     4));
            regions[i].EndSector   = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4, 4));
            regions[i].Key         = new byte[16];
            Array.Copy(data, offset + 8, regions[i].Key, 0, 16);

            if(regions[i].StartSector >= regions[i].EndSector) return null;
        }

        return regions;
    }
}