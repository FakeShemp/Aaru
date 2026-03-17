using System;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Core.Image.WiiU;

/// <summary>Parsed Wii U TOC partition entry.</summary>
struct WiiuPartition
{
    /// <summary>Partition identifier (e.g., "SI", "GI", "GM...").</summary>
    public string Identifier;
    /// <summary>Start physical sector (0x8000-byte units).</summary>
    public uint StartSector;
    /// <summary>Decrypted partition key (disc key or derived title key).</summary>
    public byte[] Key;
    /// <summary>Whether a title key was found for this partition.</summary>
    public bool HasTitleKey;
}

/// <summary>Wii U TOC parsing from decrypted sector data.</summary>
static class WiiuToc
{
    const uint TOC_ENTRIES_OFFSET = 0x800;
    const uint TOC_ENTRY_SIZE    = 0x80;
    const int  MAX_PARTITIONS    = 8;

    /// <summary>
    ///     Read the TOC sector from the disc image, decrypt it, validate, and parse partition entries.
    /// </summary>
    /// <param name="inputImage">Source image to read from.</param>
    /// <param name="discKey">16-byte disc key.</param>
    /// <param name="partitions">Parsed partition array (output).</param>
    /// <returns>True on success, false if TOC cannot be read or validated.</returns>
    public static bool ParseToc(IMediaImage inputImage, byte[] discKey, out WiiuPartition[] partitions)
    {
        partitions = null;

        // Read the physical sector at ENCRYPTED_OFFSET (physical sector 3)
        uint sectorSize     = inputImage.Info.SectorSize;
        ulong tocStartSector = WiiuCrypto.ENCRYPTED_OFFSET / sectorSize;
        uint sectorsToRead  = (uint)WiiuCrypto.SECTOR_SIZE / sectorSize;

        ErrorNumber errno = inputImage.ReadSectors(tocStartSector, false, sectorsToRead, out byte[] encSector, out _);

        if(errno != ErrorNumber.NoError) return false;

        // Decrypt with disc key, IV = all zeros
        byte[] decSector = WiiuCrypto.DecryptSector(discKey, encSector);

        // Validate signature
        uint sig = BigEndianBitConverter.ToUInt32(decSector, 0);

        if(sig != WiiuCrypto.TOC_SIGNATURE) return false;

        // Parse partition count and entries
        uint partCount = BigEndianBitConverter.ToUInt32(decSector, 0x1C);

        if(partCount > MAX_PARTITIONS) partCount = (uint)MAX_PARTITIONS;

        partitions = new WiiuPartition[partCount];

        for(uint i = 0; i < partCount; i++)
        {
            uint entryOff = TOC_ENTRIES_OFFSET + i * TOC_ENTRY_SIZE;

            var identBytes = new byte[25];
            Array.Copy(decSector, (int)entryOff, identBytes, 0, 25);

            partitions[i] = new WiiuPartition
            {
                Identifier  = Encoding.ASCII.GetString(identBytes).TrimEnd('\0'),
                StartSector = BigEndianBitConverter.ToUInt32(decSector, (int)(entryOff + 0x20)),
                Key         = new byte[16],
                HasTitleKey = false
            };
        }

        return true;
    }

    /// <summary>
    ///     Read and decrypt data from a Wii U partition at a given offset.
    ///     Handles physical sector boundaries and AES-128-CBC decryption with IV=0.
    /// </summary>
    /// <param name="inputImage">Source image.</param>
    /// <param name="key">16-byte AES key for this partition.</param>
    /// <param name="partitionDiscOffset">Absolute byte offset of the partition on disc.</param>
    /// <param name="offset">Offset within the partition.</param>
    /// <param name="size">Bytes to read.</param>
    /// <returns>Decrypted data, or null on error.</returns>
    public static byte[] ReadVolumeDecrypted(IMediaImage inputImage, byte[] key, ulong partitionDiscOffset,
                                             ulong offset, uint size)
    {
        var  result     = new byte[size];
        uint done       = 0;
        uint sectorSize = inputImage.Info.SectorSize;

        while(done < size)
        {
            ulong cur    = offset + done;
            ulong secIdx = cur / (uint)WiiuCrypto.SECTOR_SIZE;
            uint  secOff = (uint)(cur % (uint)WiiuCrypto.SECTOR_SIZE);

            ulong discOff       = partitionDiscOffset + secIdx * (uint)WiiuCrypto.SECTOR_SIZE;
            ulong sectorStart   = discOff / sectorSize;
            uint  sectorsToRead = (uint)WiiuCrypto.SECTOR_SIZE / sectorSize;

            ErrorNumber errno = inputImage.ReadSectors(sectorStart, false, sectorsToRead, out byte[] encSector, out _);

            if(errno != ErrorNumber.NoError) return null;

            byte[] decSector = WiiuCrypto.DecryptSector(key, encSector);

            uint chunk = (uint)WiiuCrypto.SECTOR_SIZE - secOff;

            if(chunk > size - done) chunk = size - done;

            Array.Copy(decSector, secOff, result, done, chunk);
            done += chunk;
        }

        return result;
    }
}