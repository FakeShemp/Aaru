using System;
using System.Buffers.Binary;

namespace Aaru.Core.Image.PS3;

/// <summary>A plaintext (unencrypted) region on a PS3 disc.</summary>
struct Ps3PlaintextRegion
{
    /// <summary>First sector of plaintext region (inclusive).</summary>
    public uint StartSector;
    /// <summary>Last sector of plaintext region (inclusive).</summary>
    public uint EndSector;
}

/// <summary>PS3 encryption map: plaintext region parsing, serialization, and lookup.</summary>
static class Ps3EncryptionMap
{
    const int MAX_PLAINTEXT_REGIONS = 32;

    /// <summary>
    ///     Parses the encryption map from PS3 disc sector 0 (big-endian on disc).
    ///     Format: [4B region_count BE][4B unknown] then count × [4B start BE, 4B end BE].
    /// </summary>
    /// <param name="sector0">2048-byte sector 0 data.</param>
    /// <returns>Array of plaintext regions (may be empty).</returns>
    public static Ps3PlaintextRegion[] ParseFromSector0(byte[] sector0)
    {
        if(sector0 is null || sector0.Length < 8) throw new ArgumentOutOfRangeException(nameof(sector0));

        uint regionCount = BinaryPrimitives.ReadUInt32BigEndian(sector0.AsSpan(0, 4));

        if(regionCount > MAX_PLAINTEXT_REGIONS) throw new InvalidOperationException();

        if(regionCount == 0) return [];

        uint required = 8 + regionCount * 8;

        if(sector0.Length < required) throw new ArgumentOutOfRangeException(nameof(sector0));

        var regions = new Ps3PlaintextRegion[regionCount];

        var offset = 8; // skip region_count (4) + unknown (4)

        for(var i = 0; i < regionCount; i++)
        {
            regions[i].StartSector =  BinaryPrimitives.ReadUInt32BigEndian(sector0.AsSpan(offset,     4));
            regions[i].EndSector   =  BinaryPrimitives.ReadUInt32BigEndian(sector0.AsSpan(offset + 4, 4));
            offset                 += 8;

            if(regions[i].StartSector > regions[i].EndSector) throw new InvalidOperationException();
        }

        return regions;
    }

    /// <summary>
    ///     Serializes plaintext regions to little-endian binary for storage as a PS3_EncryptionMap media tag.
    ///     Format: [4B count LE] then count × [4B start LE, 4B end LE].
    /// </summary>
    /// <param name="regions">Array of plaintext regions.</param>
    /// <returns>Serialized byte array.</returns>
    public static byte[] Serialize(Ps3PlaintextRegion[] regions)
    {
        if(regions is null) throw new ArgumentNullException(nameof(regions));

        if(regions.Length > MAX_PLAINTEXT_REGIONS) throw new InvalidOperationException();

        int size   = 4 + regions.Length * 8;
        var buffer = new byte[size];

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), (uint)regions.Length);

        for(var i = 0; i < regions.Length; i++)
        {
            int offset = 4 + i * 8;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset,     4), regions[i].StartSector);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset + 4, 4), regions[i].EndSector);
        }

        return buffer;
    }

    /// <summary>
    ///     Deserializes plaintext regions from little-endian binary (media tag format).
    ///     Format: [4B count LE] then count × [4B start LE, 4B end LE].
    /// </summary>
    /// <param name="data">Serialized data buffer.</param>
    /// <returns>Array of plaintext regions.</returns>
    public static Ps3PlaintextRegion[] Deserialize(byte[] data)
    {
        if(data is null || data.Length < 4) throw new ArgumentOutOfRangeException(nameof(data));

        uint regionCount = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4));

        if(regionCount > MAX_PLAINTEXT_REGIONS) throw new InvalidOperationException();

        if(regionCount == 0) return [];

        uint required = 4 + regionCount * 8;

        if(data.Length < required) throw new ArgumentOutOfRangeException(nameof(data));

        var regions = new Ps3PlaintextRegion[regionCount];

        for(var i = 0; i < regionCount; i++)
        {
            int offset = 4 + i * 8;
            regions[i].StartSector = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset,     4));
            regions[i].EndSector   = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4, 4));
        }

        return regions;
    }

    /// <summary>
    ///     Checks whether a sector is encrypted (i.e., not in any plaintext region).
    /// </summary>
    /// <param name="plaintextRegions">Array of plaintext regions.</param>
    /// <param name="sectorAddress">Sector number to check.</param>
    /// <returns><c>true</c> if the sector is encrypted (not in any plaintext region).</returns>
    public static bool IsSectorEncrypted(Ps3PlaintextRegion[] plaintextRegions, ulong sectorAddress)
    {
        if(plaintextRegions is null || plaintextRegions.Length == 0) return true;

        for(var i = 0; i < plaintextRegions.Length; i++)
        {
            if(sectorAddress >= plaintextRegions[i].StartSector && sectorAddress <= plaintextRegions[i].EndSector)
                return false;
        }

        return true;
    }
}