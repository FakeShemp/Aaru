// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class NintendoPlugin
{
    /// <summary>Mount a GameCube disc filesystem (no encryption, single partition)</summary>
    ErrorNumber MountGameCube()
    {
        var partition = new PartitionInfo
        {
            DiscHeader = _discHeader
        };

        uint fstOff  = _discHeader.FstOff;
        uint fstSize = _discHeader.FstSize;

        AaruLogging.Debug(MODULE_NAME, "GameCube FST offset: 0x{0:X8}, size: 0x{1:X8}", fstOff, fstSize);

        if(fstOff == 0 || fstSize == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "GameCube FST offset or size is zero, returning InvalidArgument");

            return ErrorNumber.InvalidArgument;
        }

        // FST may not be sector-aligned, account for sub-sector offset
        uint  sectorSize     = _imagePlugin.Info.SectorSize;
        ulong fstStartSector = fstOff                               / sectorSize;
        uint  fstBase        = fstOff                               % sectorSize;
        ulong fstSectorCount = (fstBase + fstSize + sectorSize - 1) / sectorSize;

        AaruLogging.Debug(MODULE_NAME,
                          "GameCube FST: reading {0} sectors starting at sector {1}, sub-sector offset {2}",
                          fstSectorCount,
                          fstStartSector,
                          fstBase);

        ErrorNumber errno =
            _imagePlugin.ReadSectors(fstStartSector, false, (uint)fstSectorCount, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadSectors for FST returned {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Read {0} bytes of sector data", sectorData.Length);

        // Extract exact FST data from the sector-aligned buffer
        var fstData = new byte[fstSize];
        Array.Copy(sectorData, fstBase, fstData, 0, fstSize);

        ErrorNumber fstResult = ParseFst(partition, fstData, false);

        if(fstResult != ErrorNumber.NoError) return fstResult;

        // Calculate DOL size from its header
        partition.DolOffset = _discHeader.DolOff;

        ulong dolStartSector   = partition.DolOffset                          / sectorSize;
        uint  dolBase          = partition.DolOffset                          % sectorSize;
        uint  dolSectorsNeeded = (dolBase + DOL_HEADER_SIZE + sectorSize - 1) / sectorSize;

        errno = _imagePlugin.ReadSectors(dolStartSector, false, dolSectorsNeeded, out byte[] dolSectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        var dolHeader = new byte[DOL_HEADER_SIZE];
        Array.Copy(dolSectorData, dolBase, dolHeader, 0, DOL_HEADER_SIZE);

        partition.DolSize = CalculateDolSize(dolHeader);

        AaruLogging.Debug(MODULE_NAME,
                          "GameCube DOL at 0x{0:X8}, size {1} bytes",
                          partition.DolOffset,
                          partition.DolSize);

        _partitions     = [partition];
        _multiPartition = false;

        return ErrorNumber.NoError;
    }

    /// <summary>Mount a Wii disc filesystem (encrypted partitions). Enumerates all usable partitions.</summary>
    ErrorNumber MountWii(byte[] discData)
    {
        var partitions   = new List<PartitionInfo>();
        var nameCounters = new Dictionary<string, int>();

        // Scan all 4 partition tables
        for(var tbl = 0; tbl < 4; tbl++)
        {
            var  count  = BigEndianBitConverter.ToUInt32(discData, 0x40000 + tbl * 8);
            uint offset = BigEndianBitConverter.ToUInt32(discData, 0x40004 + tbl * 8) << 2;

            for(var i = 0; i < count; i++)
            {
                if(offset + i * 8 + 8 >= 0x50000) continue;

                WiiPartitionTableEntry entry =
                    Marshal.ByteArrayToStructureBigEndian<WiiPartitionTableEntry>(discData, (int)(offset + i * 8), 8);

                ulong partOffset = (ulong)entry.Offset << 2;

                AaruLogging.Debug(MODULE_NAME,
                                  "Found Wii partition: table {0}, type {1}, offset 0x{2:X8}",
                                  tbl,
                                  entry.Type,
                                  partOffset);

                PartitionInfo partInfo = TryMountWiiPartition(entry.Type, partOffset);

                if(partInfo == null) continue;

                // Assign name based on partition type
                string baseName = entry.Type switch
                                  {
                                      0 => "DATA",
                                      1 => "UPDATE",
                                      2 => "CHANNEL",
                                      _ => $"PARTITION_{entry.Type}"
                                  };

                if(!nameCounters.TryGetValue(baseName, out int nameCount))
                {
                    partInfo.Name          = baseName;
                    nameCounters[baseName] = 1;
                }
                else
                {
                    nameCounters[baseName] = nameCount + 1;
                    partInfo.Name          = $"{baseName}_{nameCount + 1}";
                }

                partitions.Add(partInfo);
            }
        }

        if(partitions.Count == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No usable partitions found on Wii disc");

            return ErrorNumber.InvalidArgument;
        }

        _partitions     = partitions.ToArray();
        _multiPartition = _partitions.Length > 1;

        AaruLogging.Debug(MODULE_NAME,
                          "Mounted {0} Wii partition(s), multiPartition = {1}",
                          _partitions.Length,
                          _multiPartition);

        return ErrorNumber.NoError;
    }

    /// <summary>Try to mount a single Wii partition, returning its info on success or null on failure</summary>
    /// <param name="type">Partition type (0 = DATA, 1 = UPDATE, 2 = CHANNEL)</param>
    /// <param name="partitionOffset">Absolute offset of the partition on disc</param>
    /// <returns>Populated PartitionInfo on success, null if the partition cannot be decrypted or parsed</returns>
    PartitionInfo TryMountWiiPartition(uint type, ulong partitionOffset)
    {
        var partInfo = new PartitionInfo
        {
            Type            = type,
            PartitionOffset = partitionOffset
        };

        // Read the ticket from the partition header to get the title key
        // Title key (encrypted) is at partition offset + 0x1BF, 16 bytes
        // IV is at partition offset + 0x1DC, 8 bytes (padded to 16 with zeros)
        ulong ticketSector = (partitionOffset + 0x1BF) / _imagePlugin.Info.SectorSize;

        var ticketSectorCount =
            (uint)((partitionOffset +
                    0x1DC           +
                    16 -
                    ticketSector * _imagePlugin.Info.SectorSize +
                    _imagePlugin.Info.SectorSize -
                    1) /
                   _imagePlugin.Info.SectorSize);

        ErrorNumber errno =
            _imagePlugin.ReadSectors(ticketSector, false, ticketSectorCount, out byte[] ticketData, out _);

        if(errno != ErrorNumber.NoError) return null;

        var ticketBase = (uint)(partitionOffset + 0x1BF - ticketSector * _imagePlugin.Info.SectorSize);

        var encryptedTitleKey = new byte[16];
        Array.Copy(ticketData, ticketBase, encryptedTitleKey, 0, 16);

        var iv = new byte[16];
        Array.Copy(ticketData, ticketBase + (0x1DC - 0x1BF), iv, 0, 8);

        // Remaining 8 bytes of IV are zero

        // Determine which common key to use based on key index at partition offset + 0x1F1
        ulong keyIndexSector = (partitionOffset + 0x1F1) / _imagePlugin.Info.SectorSize;

        errno = _imagePlugin.ReadSectors(keyIndexSector, false, 1, out byte[] keyIndexData, out _);

        if(errno != ErrorNumber.NoError) return null;

        byte keyIndex = keyIndexData[(partitionOffset + 0x1F1) % _imagePlugin.Info.SectorSize];

        byte[] commonKey = keyIndex == 1 ? WII_KOREAN_KEY : WII_COMMON_KEY;

        // Decrypt the title key using the common key
        byte[] partitionKey;

        using(var aes = Aes.Create())
        {
            aes.Key     = commonKey;
            aes.IV      = iv;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.None;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            partitionKey = decryptor.TransformFinalBlock(encryptedTitleKey, 0, 16);
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Partition at 0x{0:X8}: decrypted key {1}",
                          partitionOffset,
                          BitConverter.ToString(partitionKey));

        // Set up AES for partition data decryption
        partInfo.PartitionAes         = Aes.Create();
        partInfo.PartitionAes.Key     = partitionKey;
        partInfo.PartitionAes.Mode    = CipherMode.CBC;
        partInfo.PartitionAes.Padding = PaddingMode.None;

        // Read data offset and data size from the partition header at partition offset + 0x2B8
        ulong dataOffSector = (partitionOffset + 0x2B8) / _imagePlugin.Info.SectorSize;

        errno = _imagePlugin.ReadSectors(dataOffSector, false, 1, out byte[] dataOffData, out _);

        if(errno != ErrorNumber.NoError)
        {
            partInfo.PartitionAes.Dispose();

            return null;
        }

        var dataOffBase = (uint)((partitionOffset + 0x2B8) % _imagePlugin.Info.SectorSize);

        partInfo.PartitionDataOffset = (ulong)BigEndianBitConverter.ToUInt32(dataOffData, (int)dataOffBase) << 2;

        AaruLogging.Debug(MODULE_NAME,
                          "Partition at 0x{0:X8}: data offset 0x{1:X8}",
                          partitionOffset,
                          partInfo.PartitionDataOffset);

        // Read the partition's internal disc header to get FST offset and size
        byte[] partHeader = ReadWiiPartitionData(partInfo, 0, 0x440);

        if(partHeader == null)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read partition header at 0x{0:X8}", partitionOffset);
            partInfo.PartitionAes.Dispose();

            return null;
        }

        partInfo.DiscHeader = Marshal.ByteArrayToStructureBigEndian<DiscHeader>(partHeader);

        // Wii partition internal FST offset and size need to be shifted left by 2
        uint fstOff  = partInfo.DiscHeader.FstOff  << 2;
        uint fstSize = partInfo.DiscHeader.FstSize << 2;

        AaruLogging.Debug(MODULE_NAME,
                          "Partition at 0x{0:X8}: FST offset 0x{1:X8}, size 0x{2:X8}",
                          partitionOffset,
                          fstOff,
                          fstSize);

        if(fstOff == 0 || fstSize == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Partition at 0x{0:X8}: FST offset or size is zero", partitionOffset);

            partInfo.PartitionAes.Dispose();

            return null;
        }

        // Read FST from the encrypted partition data
        byte[] fstData = ReadWiiPartitionData(partInfo, fstOff, fstSize);

        if(fstData == null)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read FST for partition at 0x{0:X8}", partitionOffset);
            partInfo.PartitionAes.Dispose();

            return null;
        }

        ErrorNumber fstResult = ParseFst(partInfo, fstData, true);

        if(fstResult != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Failed to parse FST for partition at 0x{0:X8}: {1}",
                              partitionOffset,
                              fstResult);

            partInfo.PartitionAes.Dispose();

            return null;
        }

        // Calculate DOL size from its header within the encrypted partition data
        partInfo.DolOffset = partInfo.DiscHeader.DolOff << 2;

        byte[] dolHeader = ReadWiiPartitionData(partInfo, partInfo.DolOffset, DOL_HEADER_SIZE);

        if(dolHeader != null)
            partInfo.DolSize = CalculateDolSize(dolHeader);
        else
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read DOL header for partition at 0x{0:X8}", partitionOffset);
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Successfully mounted partition at 0x{0:X8}: DOL at 0x{1:X8}, size {2}",
                          partitionOffset,
                          partInfo.DolOffset,
                          partInfo.DolSize);

        return partInfo;
    }

    /// <summary>Read data from a Wii encrypted partition, decrypting the cluster blocks</summary>
    /// <param name="partition">Partition info with AES key and offsets</param>
    /// <param name="offset">Offset within the partition data (in decrypted space)</param>
    /// <param name="size">Number of bytes to read</param>
    /// <returns>Decrypted data, or null on error</returns>
    byte[] ReadWiiPartitionData(PartitionInfo partition, uint offset, uint size)
    {
        var  result     = new byte[size];
        uint bytesRead  = 0;
        uint currentOff = offset;

        while(bytesRead < size)
        {
            uint blockIndex  = currentOff / WII_CLUSTER_DATA_SIZE;
            uint blockOffset = currentOff % WII_CLUSTER_DATA_SIZE;
            uint bytesToRead = Math.Min(size - bytesRead, WII_CLUSTER_DATA_SIZE - blockOffset);

            // Physical offset on disc = partition offset + data offset + block * 0x8000
            ulong physicalOffset = partition.PartitionOffset     +
                                   partition.PartitionDataOffset +
                                   (ulong)blockIndex * WII_CLUSTER_SIZE;

            ulong sectorStart = physicalOffset / _imagePlugin.Info.SectorSize;

            uint sectorsNeeded = (WII_CLUSTER_SIZE + _imagePlugin.Info.SectorSize - 1) / _imagePlugin.Info.SectorSize;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(sectorStart, false, sectorsNeeded, out byte[] clusterData, out _);

            if(errno != ErrorNumber.NoError) return null;

            // The cluster may not be aligned to sector boundaries
            var clusterBase = (uint)(physicalOffset - sectorStart * _imagePlugin.Info.SectorSize);

            if(clusterBase + WII_CLUSTER_SIZE > clusterData.Length) return null;

            // IV for data decryption is at offset 0x3D0 within the cluster (in the hash area)
            var dataIv = new byte[16];
            Array.Copy(clusterData, clusterBase + 0x3D0, dataIv, 0, 16);

            // Encrypted data starts at offset 0x400 within the cluster
            var encryptedData = new byte[WII_CLUSTER_DATA_SIZE];
            Array.Copy(clusterData, clusterBase + WII_CLUSTER_HASH_SIZE, encryptedData, 0, WII_CLUSTER_DATA_SIZE);

            partition.PartitionAes.IV = dataIv;

            byte[] decryptedData;

            using(ICryptoTransform decryptor = partition.PartitionAes.CreateDecryptor())
            {
                decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, WII_CLUSTER_DATA_SIZE);
            }

            Array.Copy(decryptedData, blockOffset, result, bytesRead, bytesToRead);

            bytesRead  += bytesToRead;
            currentOff += bytesToRead;
        }

        return result;
    }

    /// <summary>Calculate the total size of a DOL executable from its header</summary>
    /// <param name="dolHeader">The first 0x100 bytes of the DOL file</param>
    /// <returns>Total size of the DOL file on disc</returns>
    static uint CalculateDolSize(byte[] dolHeader)
    {
        uint max = 0;

        // 7 code (text) segments: offsets at 0x00, sizes at 0x90
        for(var i = 0; i < DOL_CODE_SEGMENTS; i++)
        {
            var offset = BigEndianBitConverter.ToUInt32(dolHeader, i * 4);
            var size   = BigEndianBitConverter.ToUInt32(dolHeader, 0x90 + i * 4);

            if(offset + size > max) max = offset + size;
        }

        // 11 data segments: offsets at 0x1C, sizes at 0xAC
        for(var i = 0; i < DOL_DATA_SEGMENTS; i++)
        {
            var offset = BigEndianBitConverter.ToUInt32(dolHeader, 0x1C + i * 4);
            var size   = BigEndianBitConverter.ToUInt32(dolHeader, 0xAC + i * 4);

            if(offset + size > max) max = offset + size;
        }

        return max;
    }

    /// <summary>Parse the FST (File System Table) from raw FST data into a partition's entries</summary>
    /// <param name="partition">Partition to store the parsed entries in</param>
    /// <param name="fstData">Raw FST bytes</param>
    /// <param name="isWii">
    ///     True if Wii (file offsets need &lt;&lt;2), false for GameCube
    /// </param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseFst(PartitionInfo partition, byte[] fstData, bool isWii)
    {
        AaruLogging.Debug(MODULE_NAME, "ParseFst: fstData.Length = {0}, isWii = {1}", fstData.Length, isWii);

        if(fstData.Length < 12)
        {
            AaruLogging.Debug(MODULE_NAME, "ParseFst: FST data too small ({0} < 12)", fstData.Length);

            return ErrorNumber.InvalidArgument;
        }

        // Root entry is at index 0, always a directory
        FstEntry rootEntry = Marshal.ByteArrayToStructureBigEndian<FstEntry>(fstData, 0, 12);

        AaruLogging.Debug(MODULE_NAME,
                          "ParseFst: root TypeAndNameOffset=0x{0:X8}, OffsetOrParent=0x{1:X8}, SizeOrNext=0x{2:X8}",
                          rootEntry.TypeAndNameOffset,
                          rootEntry.OffsetOrParent,
                          rootEntry.SizeOrNext);

        uint totalEntries = rootEntry.SizeOrNext;

        AaruLogging.Debug(MODULE_NAME, "FST has {0} entries", totalEntries);

        if(totalEntries == 0 || totalEntries * 12 > (uint)fstData.Length)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ParseFst: invalid totalEntries ({0}), fstData.Length = {1}",
                              totalEntries,
                              fstData.Length);

            return ErrorNumber.InvalidArgument;
        }

        // String table starts right after the FST entries
        uint stringTableOffset = totalEntries * 12;

        AaruLogging.Debug(MODULE_NAME, "ParseFst: stringTableOffset = 0x{0:X}", stringTableOffset);

        // Parse all entries
        partition.FstEntries = new FstEntry[totalEntries];
        partition.FstNames   = new string[totalEntries];

        partition.FstEntries[0] = rootEntry;
        partition.FstNames[0]   = "";

        for(uint i = 1; i < totalEntries; i++)
        {
            partition.FstEntries[i] = Marshal.ByteArrayToStructureBigEndian<FstEntry>(fstData, (int)(i * 12), 12);

            uint nameOffset = partition.FstEntries[i].TypeAndNameOffset & 0x00FFFFFF;

            partition.FstNames[i] =
                StringHandlers.CToString(fstData, _encoding, false, (int)(stringTableOffset + nameOffset));

            AaruLogging.Debug(MODULE_NAME,
                              "ParseFst: entry[{0}] type={1} name=\"{2}\" offsetOrParent=0x{3:X8} sizeOrNext=0x{4:X8}",
                              i,
                              partition.FstEntries[i].TypeAndNameOffset >> 24 != 0 ? "dir" : "file",
                              partition.FstNames[i],
                              partition.FstEntries[i].OffsetOrParent,
                              partition.FstEntries[i].SizeOrNext);
        }

        AaruLogging.Debug(MODULE_NAME, "ParseFst: successfully parsed {0} entries", totalEntries);

        return ErrorNumber.NoError;
    }

    /// <summary>Cache the root directory contents for a partition (entries whose parent is 0)</summary>
    static void CachePartitionRootDirectory(PartitionInfo partition)
    {
        partition.RootDirectoryCache.Clear();

        // Add virtual system files
        partition.RootDirectoryCache["boot.bin"] = BOOT_BIN_VIRTUAL_INDEX;
        partition.RootDirectoryCache["bi2.bin"]  = BI2_BIN_VIRTUAL_INDEX;

        if(partition.DolSize > 0) partition.RootDirectoryCache["main.dol"] = DOL_VIRTUAL_INDEX;

        // Root is entry 0. Its children are entries 1..(SizeOrNext - 1) that have parent == 0
        for(var i = 1; i < partition.FstEntries.Length; i++)
        {
            bool isDirectory = partition.FstEntries[i].TypeAndNameOffset >> 24 != 0;

            if(isDirectory)
            {
                // For directories, OffsetOrParent is the parent index
                if(partition.FstEntries[i].OffsetOrParent == 0) partition.RootDirectoryCache[partition.FstNames[i]] = i;
            }
            else
            {
                // For files, check if they're a direct child of root (not inside a subdirectory)
                if(IsDirectChild(partition, i, 0)) partition.RootDirectoryCache[partition.FstNames[i]] = i;
            }
        }
    }

    /// <summary>Check if FST entry at index is a direct child of the directory at parentIndex</summary>
    static bool IsDirectChild(PartitionInfo partition, int entryIndex, int parentIndex)
    {
        bool isDirectory = partition.FstEntries[entryIndex].TypeAndNameOffset >> 24 != 0;

        if(isDirectory) return (int)partition.FstEntries[entryIndex].OffsetOrParent == parentIndex;

        // For a file, find the innermost enclosing directory
        var currentParent = 0;

        for(var i = 0; i < partition.FstEntries.Length; i++)
        {
            if(i == entryIndex) return currentParent == parentIndex;

            bool isDir = partition.FstEntries[i].TypeAndNameOffset >> 24 != 0;

            if(!isDir) continue;

            if(i == 0)
            {
                currentParent = 0;

                continue;
            }

            // If we're entering this directory's range, it becomes the parent
            if(entryIndex > i && entryIndex < (int)partition.FstEntries[i].SizeOrNext) currentParent = i;
        }

        return currentParent == parentIndex;
    }

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _encoding    = encoding ?? Encoding.GetEncoding("shift_jis");
        _imagePlugin = imagePlugin;

        AaruLogging.Debug(MODULE_NAME,
                          "Mount called. partition.Start = {0}, partition.Length = {1}",
                          partition.Start,
                          partition.Length);

        AaruLogging.Debug(MODULE_NAME,
                          "Image sectors = {0}, sector size = {1}",
                          imagePlugin.Info.Sectors,
                          imagePlugin.Info.SectorSize);

        if(partition.Start != 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Partition does not start at 0, returning InvalidArgument");

            return ErrorNumber.InvalidArgument;
        }

        ulong totalBytes = imagePlugin.Info.Sectors * imagePlugin.Info.SectorSize;

        AaruLogging.Debug(MODULE_NAME, "Total image bytes = {0} (0x{0:X})", totalBytes);

        if(totalBytes < 0x50000)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Image too small ({0} bytes < 0x50000), returning InvalidArgument",
                              totalBytes);

            return ErrorNumber.InvalidArgument;
        }

        // Read the first 0x50000 bytes (enough for disc header + partition table + region settings)
        uint sectorsToRead = 0x50000 / imagePlugin.Info.SectorSize;

        AaruLogging.Debug(MODULE_NAME, "Reading {0} sectors from sector 0", sectorsToRead);

        ErrorNumber errno = imagePlugin.ReadSectors(0, false, sectorsToRead, out byte[] header, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadSectors returned {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Read {0} bytes of header data", header.Length);

        _discHeader = Marshal.ByteArrayToStructureBigEndian<DiscHeader>(header);

        AaruLogging.Debug(MODULE_NAME,
                          "DiscHeader.WiiMagic = 0x{0:X8}, expected 0x{1:X8}",
                          _discHeader.WiiMagic,
                          WII_MAGIC);

        AaruLogging.Debug(MODULE_NAME,
                          "DiscHeader.GcMagic = 0x{0:X8}, expected 0x{1:X8}",
                          _discHeader.GcMagic,
                          GC_MAGIC);

        AaruLogging.Debug(MODULE_NAME, "DiscHeader.DiscType = 0x{0:X2}", _discHeader.DiscType);

        AaruLogging.Debug(MODULE_NAME,
                          "DiscHeader.FstOff = 0x{0:X8}, FstSize = 0x{1:X8}",
                          _discHeader.FstOff,
                          _discHeader.FstSize);

        if(_discHeader.WiiMagic == WII_MAGIC)
            _isWii = true;
        else if(_discHeader.GcMagic == GC_MAGIC)
            _isWii = false;
        else
        {
            AaruLogging.Debug(MODULE_NAME, "Neither Wii nor GameCube magic found, returning InvalidArgument");

            // Dump first 32 bytes for debugging
            AaruLogging.Debug(MODULE_NAME,
                              "First 32 bytes of header: {0}",
                              BitConverter.ToString(header, 0, Math.Min(32, header.Length)));

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Disc type: {0}", _isWii ? "Wii" : "GameCube");

        string discType = Encoding.ASCII.GetString(new[]
        {
            _discHeader.DiscType
        });

        string gameCode = Encoding.ASCII.GetString(_discHeader.GameCode);

        string regionCode = Encoding.ASCII.GetString(new[]
        {
            _discHeader.RegionCode
        });

        string publisherCode = Encoding.ASCII.GetString(_discHeader.PublisherCode);
        string discId        = discType + gameCode + regionCode + publisherCode;
        string title         = StringHandlers.CToString(_discHeader.Title, _encoding);

        AaruLogging.Debug(MODULE_NAME, "Disc ID = {0}, Title = \"{1}\"", discId, title);

        if(_isWii)
            errno = MountWii(header);
        else
            errno = MountGameCube();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Mount{0} returned {1}", _isWii ? "Wii" : "GameCube", errno);

            return errno;
        }

        // Cache root directory entries for each partition
        foreach(PartitionInfo partInfo in _partitions) CachePartitionRootDirectory(partInfo);

        Metadata = new FileSystem
        {
            Type         = _isWii ? FS_TYPE_WII : FS_TYPE_NGC,
            VolumeName   = title,
            ClusterSize  = 2048,
            Clusters     = imagePlugin.Info.Sectors * imagePlugin.Info.SectorSize / 2048,
            Bootable     = true,
            VolumeSerial = discId
        };

        _statfs = new FileSystemInfo
        {
            Blocks         = imagePlugin.Info.Sectors * imagePlugin.Info.SectorSize / 2048,
            FilenameLength = 256,
            FreeBlocks     = 0,
            PluginId       = Id,
            Type           = _isWii ? FS_TYPE_WII : FS_TYPE_NGC
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        _mounted = false;

        if(_partitions != null)
        {
            foreach(PartitionInfo partInfo in _partitions)
            {
                partInfo.PartitionAes?.Dispose();
                partInfo.RootDirectoryCache.Clear();
                partInfo.DirectoryCache.Clear();
            }

            _partitions = null;
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        stat = _statfs.ShallowCopy();

        return ErrorNumber.NoError;
    }

#endregion
}