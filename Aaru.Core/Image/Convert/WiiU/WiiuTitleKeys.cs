using System;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Core.Image.WiiU;

/// <summary>Wii U title key extraction from SI/GI partitions.</summary>
static class WiiuTitleKeys
{
    /// <summary>
    ///     Extract title keys from TITLE.TIK files within SI and GI partitions,
    ///     and assign them to matching GM partitions. Partitions without a title key
    ///     are assigned the disc key as fallback.
    /// </summary>
    /// <param name="inputImage">Source image.</param>
    /// <param name="discKey">16-byte disc key.</param>
    /// <param name="partitions">Partition array to update with extracted keys.</param>
    /// <returns>Number of title keys found.</returns>
    public static int ExtractTitleKeys(IMediaImage inputImage, byte[] discKey, WiiuPartition[] partitions)
    {
        int keysFound = 0;

        for(int p = 0; p < partitions.Length; p++)
        {
            // Only scan SI and GI partitions
            if(!partitions[p].Identifier.StartsWith("SI", StringComparison.Ordinal) &&
               !partitions[p].Identifier.StartsWith("GI", StringComparison.Ordinal))
                continue;

            ulong partDiscOff = WiiuCrypto.ENCRYPTED_OFFSET +
                                (ulong)partitions[p].StartSector * (uint)WiiuCrypto.SECTOR_SIZE -
                                0x10000;

            // Read FST header (first physical sector of partition)
            byte[] fstHdr = WiiuToc.ReadVolumeDecrypted(inputImage, discKey, partDiscOff, 0,
                                                        (uint)WiiuCrypto.SECTOR_SIZE);

            if(fstHdr == null) continue;

            // Verify "FST\0" magic
            if(fstHdr[0] != 'F' || fstHdr[1] != 'S' || fstHdr[2] != 'T' || fstHdr[3] != 0) continue;

            uint offsetFactor = BigEndianBitConverter.ToUInt32(fstHdr, 4);
            uint clusterCount = BigEndianBitConverter.ToUInt32(fstHdr, 8);

            var clusterOffsets = new ulong[clusterCount];

            for(uint c = 0; c < clusterCount; c++)
            {
                uint  raw   = BigEndianBitConverter.ToUInt32(fstHdr, (int)(0x20 + c * 0x20));
                ulong start = (ulong)raw * (uint)WiiuCrypto.SECTOR_SIZE;

                clusterOffsets[c] = start > (uint)WiiuCrypto.SECTOR_SIZE
                                        ? start - (uint)WiiuCrypto.SECTOR_SIZE
                                        : 0;
            }

            ulong entriesOffset = (ulong)offsetFactor * clusterCount + 0x20;

            // Read root entry to get total entries
            byte[] rootEntryData = WiiuToc.ReadVolumeDecrypted(inputImage, discKey, partDiscOff, entriesOffset, 0x10);

            if(rootEntryData == null) continue;

            uint totalEntries = BigEndianBitConverter.ToUInt32(rootEntryData, 8);

            if(totalEntries > 100000) continue;

            ulong nameTableOffset = entriesOffset + (ulong)totalEntries * 0x10;

            // Read all FST entries + name table
            uint fstDataSize = (uint)(nameTableOffset - entriesOffset + 0x10000);

            if(fstDataSize > 16 * 1024 * 1024) continue;

            byte[] fstData = WiiuToc.ReadVolumeDecrypted(inputImage, discKey, partDiscOff, entriesOffset, fstDataSize);

            if(fstData == null) continue;

            uint nameTableBase = (uint)(nameTableOffset - entriesOffset);

            // Scan for TITLE.TIK files
            for(uint e = 0; e < totalEntries; e++)
            {
                int    entOff    = (int)(e * 0x10);
                byte   type      = fstData[entOff];
                uint   nameOff   = BigEndianBitConverter.ToUInt32(fstData, entOff) & 0x00FFFFFF;
                ulong  fileOff   = (ulong)BigEndianBitConverter.ToUInt32(fstData, entOff + 4) << 5;
                uint   fileSize  = BigEndianBitConverter.ToUInt32(fstData, entOff + 8);
                ushort clusterId = (ushort)((fstData[entOff + 0x0E] << 8) | fstData[entOff + 0x0F]);

                if(type == 1) continue; // directory

                if(fileSize < 0x200) continue;

                if(nameOff >= fstDataSize - nameTableBase) continue;

                // Get filename from name table
                int    nameStart = (int)(nameTableBase + nameOff);
                int    nameEnd   = nameStart;

                while(nameEnd < fstData.Length && fstData[nameEnd] != 0) nameEnd++;

                string fname = Encoding.ASCII.GetString(fstData, nameStart, nameEnd - nameStart);

                if(!fname.Equals("title.tik", StringComparison.OrdinalIgnoreCase)) continue;

                if(clusterId >= clusterCount) continue;

                // Read ticket: encrypted title key at TIK+0x1BF, title ID at TIK+0x1DC
                ulong tikVolumeOff = clusterOffsets[clusterId] + fileOff;

                byte[] tikBuf = WiiuToc.ReadVolumeDecrypted(inputImage, discKey, partDiscOff,
                                                            tikVolumeOff + 0x1BF, 0x10 + 0x1D + 8);

                if(tikBuf == null) continue;

                var encTitleKey = new byte[16];
                var titleId    = new byte[8];
                Array.Copy(tikBuf, 0,    encTitleKey, 0, 16);
                Array.Copy(tikBuf, 0x1D, titleId,    0, 8);

                // Decrypt title key
                byte[] decTitleKey = WiiuCrypto.DecryptTitleKey(encTitleKey, titleId);

                // Build expected GM partition name from title ID
                string gmName = $"GM{titleId[0]:X2}{titleId[1]:X2}{titleId[2]:X2}{titleId[3]:X2}" +
                                $"{titleId[4]:X2}{titleId[5]:X2}{titleId[6]:X2}{titleId[7]:X2}";

                // Match to a GM partition
                for(int g = 0; g < partitions.Length; g++)
                {
                    if(!partitions[g].Identifier.StartsWith("GM", StringComparison.Ordinal)) continue;

                    if(!partitions[g].Identifier.StartsWith(gmName, StringComparison.Ordinal)) continue;

                    Array.Copy(decTitleKey, partitions[g].Key, 16);
                    partitions[g].HasTitleKey = true;
                    keysFound++;

                    break;
                }
            }
        }

        // Set disc key as fallback for partitions without title key
        for(int i = 0; i < partitions.Length; i++)
        {
            if(!partitions[i].HasTitleKey) Array.Copy(discKey, partitions[i].Key, 16);
        }

        return keysFound;
    }
}