// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Ngcw.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     NGCW (GameCube/Wii) helpers for OmniDrive raw DVD dumping.
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
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// Copyright © 2020-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Decryption.Ngcw;
using Aaru.Decoders.Nintendo;
using Aaru.Helpers;
using Aaru.Localization;
using NgcwPartitions = Aaru.Decryption.Ngcw.Partitions;

namespace Aaru.Core.Devices.Dumping;

partial class Dump
{
    const int NGCW_LONG_SECTOR_SIZE = 2064;
    const int NGCW_PAYLOAD_OFFSET   = 6;
    const int NGCW_SECTOR_SIZE      = 2048;
    const int NGCW_SECTORS_PER_GROUP = 16;

    bool                _ngcwEnabled;
    MediaType           _ngcwMediaType;
    JunkCollector       _ngcwJunkCollector;
    List<WiiPartition>  _ngcwPartitions;
    DataRegion[][]      _ngcwPartDataMaps;
    ulong[]             _ngcwPartSysEnd;
    DataRegion[]        _ngcwGcDataMap;
    ulong               _ngcwGcSysEnd;

    bool                _omniDriveNintendoSoftwareDescramble;
    byte?               _nintendoDerivedDiscKey;
    readonly Aaru.Decoders.Nintendo.Sector _nintendoSectorDecoder = new Aaru.Decoders.Nintendo.Sector();

    bool InitializeNgcwContext(MediaType dskType, Reader scsiReader, IWritableImage outputFormat)
    {
        _ngcwEnabled     = dskType is MediaType.GOD or MediaType.WOD;
        _ngcwMediaType   = dskType;
        _ngcwJunkCollector = new JunkCollector();
        _omniDriveNintendoSoftwareDescramble = scsiReader.OmniDriveNintendoMode;

        if(!_ngcwEnabled) return true;

        if(_omniDriveNintendoSoftwareDescramble)
        {
            UpdateStatus?.Invoke(UI.Ngcw_nintendo_software_descramble);

            if(!EnsureNintendoDerivedKeyFromLba0(scsiReader)) return false;
        }

        if(dskType == MediaType.GOD)
            return InitializeGameCubeContext(scsiReader);

        return InitializeWiiContext(scsiReader, outputFormat);
    }

    void FinalizeNgcwContext(IWritableImage outputFormat)
    {
        if(!_ngcwEnabled || _ngcwJunkCollector is null || _ngcwJunkCollector.Count == 0) return;

        byte[] junkMapData = Junk.Serialize(_ngcwJunkCollector.Entries);
        outputFormat.WriteMediaTag(junkMapData, MediaTagType.NgcwJunkMap);
        UpdateStatus?.Invoke(string.Format(UI.Ngcw_stored_junk_map_0_entries_1_bytes,
                                           _ngcwJunkCollector.Count,
                                           junkMapData.Length));
    }

    bool TransformNgcwLongSectors(Reader scsiReader, byte[] longBuffer, ulong startSector, uint sectors,
                                  out SectorStatus[] statuses)
    {
        statuses = new SectorStatus[sectors];

        if(!_ngcwEnabled)
        {
            for(int i = 0; i < sectors; i++) statuses[i] = SectorStatus.Dumped;

            return true;
        }

        if(_omniDriveNintendoSoftwareDescramble &&
           !DescrambleNintendoLongBuffer(longBuffer, startSector, sectors))
            return false;

        if(_ngcwMediaType == MediaType.GOD)
            return TransformGameCubeLongSectors(longBuffer, startSector, sectors, statuses);

        return TransformWiiLongSectors(scsiReader, longBuffer, startSector, sectors, statuses);
    }

    bool InitializeGameCubeContext(Reader scsiReader)
    {
        byte[] extHeader = ReadDiscBytesFromDevice(scsiReader, 0, 0x440);

        if(extHeader == null || extHeader.Length < 0x42C)
        {
            StoppingErrorMessage?.Invoke(Localization.Core.Unable_to_read_medium);

            return false;
        }

        uint  fstOffset = BigEndianBitConverter.ToUInt32(extHeader, 0x424);
        uint  fstSize   = BigEndianBitConverter.ToUInt32(extHeader, 0x428);
        _ngcwGcSysEnd = fstOffset + fstSize;

        if(fstSize > 0 && fstSize < 64 * 1024 * 1024)
        {
            byte[] fst = ReadDiscBytesFromDevice(scsiReader, fstOffset, (int)fstSize);

            if(fst != null) _ngcwGcDataMap = DataMap.BuildFromFst(fst, 0, 0);
        }

        return true;
    }

    bool InitializeWiiContext(Reader scsiReader, IWritableImage outputFormat)
    {
        UpdateStatus?.Invoke(UI.Ngcw_parsing_partition_table);
        _ngcwPartitions = ParseWiiPartitionsFromDevice(scsiReader);

        if(_ngcwPartitions == null)
        {
            StoppingErrorMessage?.Invoke(Localization.Core.Unable_to_read_medium);

            return false;
        }

        WiiPartitionRegion[] regions = NgcwPartitions.BuildRegionMap(_ngcwPartitions);
        byte[] keyMapData = NgcwPartitions.SerializeKeyMap(regions);

        outputFormat.WriteMediaTag(keyMapData, MediaTagType.WiiPartitionKeyMap);
        UpdateStatus?.Invoke(UI.Ngcw_written_partition_key_map);

        BuildWiiPartitionFstMaps(scsiReader);

        return true;
    }

    void BuildWiiPartitionFstMaps(Reader scsiReader)
    {
        if(_ngcwPartitions == null || _ngcwPartitions.Count == 0) return;

        _ngcwPartDataMaps = new DataRegion[_ngcwPartitions.Count][];
        _ngcwPartSysEnd   = new ulong[_ngcwPartitions.Count];

        for(int p = 0; p < _ngcwPartitions.Count; p++)
        {
            byte[] encGrp0 = ReadRawPayloadSectors(scsiReader, _ngcwPartitions[p].DataOffset / NGCW_SECTOR_SIZE, 16);

            if(encGrp0 == null || encGrp0.Length < Crypto.GROUP_SIZE) continue;

            byte[] hb0 = new byte[Crypto.GROUP_HASH_SIZE];
            byte[] gd0 = new byte[Crypto.GROUP_DATA_SIZE];
            Crypto.DecryptGroup(_ngcwPartitions[p].TitleKey, encGrp0, hb0, gd0);

            uint fstOffset = BigEndianBitConverter.ToUInt32(gd0, 0x424) << 2;
            uint fstSize   = BigEndianBitConverter.ToUInt32(gd0, 0x428) << 2;

            _ngcwPartSysEnd[p] = fstOffset + fstSize;

            if(fstSize == 0 || fstSize >= 64 * 1024 * 1024) continue;

            byte[] fstBuffer = new byte[fstSize];
            uint   fstRead   = 0;
            bool   ok        = true;

            while(fstRead < fstSize)
            {
                ulong logicalOffset = fstOffset + fstRead;
                ulong groupIndex    = logicalOffset / Crypto.GROUP_DATA_SIZE;
                int   groupOffset   = (int)(logicalOffset % Crypto.GROUP_DATA_SIZE);
                ulong discOffset    = _ngcwPartitions[p].DataOffset + groupIndex * Crypto.GROUP_SIZE;

                byte[] encGroup = ReadRawPayloadSectors(scsiReader, discOffset / NGCW_SECTOR_SIZE, 16);

                if(encGroup == null || encGroup.Length < Crypto.GROUP_SIZE)
                {
                    ok = false;

                    break;
                }

                byte[] hb = new byte[Crypto.GROUP_HASH_SIZE];
                byte[] gd = new byte[Crypto.GROUP_DATA_SIZE];
                Crypto.DecryptGroup(_ngcwPartitions[p].TitleKey, encGroup, hb, gd);

                int available = Crypto.GROUP_DATA_SIZE - groupOffset;
                int chunk     = fstSize - (int)fstRead < available ? (int)(fstSize - fstRead) : available;
                Array.Copy(gd, groupOffset, fstBuffer, fstRead, chunk);
                fstRead += (uint)chunk;
            }

            if(ok) _ngcwPartDataMaps[p] = DataMap.BuildFromFst(fstBuffer, 0, 2);
        }
    }

    bool TransformGameCubeLongSectors(byte[] longBuffer, ulong startSector, uint sectors, SectorStatus[] statuses)
    {
        byte[] payload = new byte[sectors * NGCW_SECTOR_SIZE];

        for(uint i = 0; i < sectors; i++)
            Array.Copy(longBuffer, i * NGCW_LONG_SECTOR_SIZE + NGCW_PAYLOAD_OFFSET, payload, i * NGCW_SECTOR_SIZE,
                       NGCW_SECTOR_SIZE);

        ulong dataSectors = 0;
        ulong junkSectors = 0;
        Junk.DetectJunkInBlock(payload,
                               payload.Length,
                               startSector * NGCW_SECTOR_SIZE,
                               _ngcwGcDataMap,
                               _ngcwGcSysEnd,
                               0xFFFF,
                               _ngcwJunkCollector,
                               ref dataSectors,
                               ref junkSectors,
                               statuses);

        return true;
    }

    bool TransformWiiLongSectors(Reader scsiReader, byte[] longBuffer, ulong startSector, uint sectors, SectorStatus[] statuses)
    {
        ulong discOffset = startSector * NGCW_SECTOR_SIZE;
        int   partIndex  = NgcwPartitions.FindPartitionAtOffset(_ngcwPartitions, discOffset);

        if(partIndex < 0)
        {
            byte[] payload = new byte[sectors * NGCW_SECTOR_SIZE];

            for(uint i = 0; i < sectors; i++)
                Array.Copy(longBuffer, i * NGCW_LONG_SECTOR_SIZE + NGCW_PAYLOAD_OFFSET, payload, i * NGCW_SECTOR_SIZE,
                           NGCW_SECTOR_SIZE);

            ulong dataSectors = 0;
            ulong junkSectors = 0;
            Junk.DetectJunkInBlock(payload,
                                   payload.Length,
                                   discOffset,
                                   null,
                                   0x50000,
                                   0xFFFF,
                                   _ngcwJunkCollector,
                                   ref dataSectors,
                                   ref junkSectors,
                                   statuses);

            return true;
        }

        ulong groupStartOffset = _ngcwPartitions[partIndex].DataOffset +
                                 (discOffset - _ngcwPartitions[partIndex].DataOffset) / Crypto.GROUP_SIZE * Crypto.GROUP_SIZE;

        byte[] groupPayload;

        if(sectors == NGCW_SECTORS_PER_GROUP && discOffset == groupStartOffset)
        {
            groupPayload = new byte[Crypto.GROUP_SIZE];

            for(uint i = 0; i < sectors; i++)
                Array.Copy(longBuffer, i * NGCW_LONG_SECTOR_SIZE + NGCW_PAYLOAD_OFFSET, groupPayload, i * NGCW_SECTOR_SIZE,
                           NGCW_SECTOR_SIZE);
        }
        else
        {
            groupPayload = ReadRawPayloadSectors(scsiReader, groupStartOffset / NGCW_SECTOR_SIZE, NGCW_SECTORS_PER_GROUP);

            if(groupPayload == null || groupPayload.Length < Crypto.GROUP_SIZE) return false;
        }

        byte[] hashBlock = new byte[Crypto.GROUP_HASH_SIZE];
        byte[] groupData = new byte[Crypto.GROUP_DATA_SIZE];
        Crypto.DecryptGroup(_ngcwPartitions[partIndex].TitleKey, groupPayload, hashBlock, groupData);

        ulong groupNumber    = (groupStartOffset - _ngcwPartitions[partIndex].DataOffset) / Crypto.GROUP_SIZE;
        ulong logicalOffset  = groupNumber * Crypto.GROUP_DATA_SIZE;
        bool[] sectorIsData  = new bool[NGCW_SECTORS_PER_GROUP];
        int    userDataCount = 0;

        for(ulong off = 0; off < Crypto.GROUP_DATA_SIZE; off += NGCW_SECTOR_SIZE)
        {
            ulong chunk = Crypto.GROUP_DATA_SIZE - off;

            if(chunk > NGCW_SECTOR_SIZE) chunk = NGCW_SECTOR_SIZE;

            if(logicalOffset + off < _ngcwPartSysEnd[partIndex])
                sectorIsData[userDataCount] = true;
            else if(_ngcwPartDataMaps[partIndex] != null)
            {
                sectorIsData[userDataCount] = DataMap.IsDataRegion(_ngcwPartDataMaps[partIndex], logicalOffset + off, chunk);
            }
            else
                sectorIsData[userDataCount] = true;

            userDataCount++;
        }

        ulong blockPhase  = logicalOffset % Crypto.GROUP_SIZE;
        ulong block2Start = blockPhase > 0 ? Crypto.GROUP_SIZE - blockPhase : Crypto.GROUP_DATA_SIZE;
        if(block2Start > Crypto.GROUP_DATA_SIZE) block2Start = Crypto.GROUP_DATA_SIZE;

        bool   haveSeed1 = false;
        uint[] seed1     = new uint[Lfg.SEED_SIZE];
        bool   haveSeed2 = false;
        uint[] seed2     = new uint[Lfg.SEED_SIZE];

        for(int s = 0; s < userDataCount; s++)
        {
            if(sectorIsData[s]) continue;

            ulong sectorOffset = (ulong)s * NGCW_SECTOR_SIZE;
            bool  inBlock2     = sectorOffset >= block2Start;

            if(inBlock2 && haveSeed2) continue;
            if(!inBlock2 && haveSeed1) continue;

            int available = Crypto.GROUP_DATA_SIZE - (int)sectorOffset;
            int dataOffset = (int)((logicalOffset + sectorOffset) % Crypto.GROUP_SIZE);

            if(available < Lfg.MIN_SEED_DATA_BYTES) continue;

            uint[] destination = inBlock2 ? seed2 : seed1;
            int    matched     = Lfg.GetSeed(groupData.AsSpan((int)sectorOffset, available), dataOffset, destination);

            if(matched > 0)
            {
                if(inBlock2)
                    haveSeed2 = true;
                else
                    haveSeed1 = true;
            }

            if(haveSeed1 && haveSeed2) break;
        }

        byte[] decryptedGroup = new byte[Crypto.GROUP_SIZE];
        Array.Copy(hashBlock, 0, decryptedGroup, 0, Crypto.GROUP_HASH_SIZE);

        for(int s = 0; s < userDataCount; s++)
        {
            ulong off      = (ulong)s * NGCW_SECTOR_SIZE;
            int   chunk    = Crypto.GROUP_DATA_SIZE - (int)off;
            int   outOff   = Crypto.GROUP_HASH_SIZE + (int)off;

            if(chunk > NGCW_SECTOR_SIZE) chunk = NGCW_SECTOR_SIZE;

            if(sectorIsData[s])
            {
                Array.Copy(groupData, (int)off, decryptedGroup, outOff, chunk);

                continue;
            }

            bool   inBlock2 = off >= block2Start;
            bool   haveSeed = inBlock2 ? haveSeed2 : haveSeed1;
            uint[] seed     = inBlock2 ? seed2 : seed1;

            if(!haveSeed)
            {
                Array.Copy(groupData, (int)off, decryptedGroup, outOff, chunk);

                continue;
            }

            uint[] lfgBuffer    = new uint[Lfg.MIN_SEED_DATA_BYTES / sizeof(uint)];
            uint[] seedCopy     = new uint[Lfg.SEED_SIZE];
            int    position     = 0;
            byte[] expectedData = new byte[NGCW_SECTOR_SIZE];
            Array.Copy(seed, seedCopy, Lfg.SEED_SIZE);
            Lfg.SetSeed(lfgBuffer, seedCopy);

            int advance = (int)((logicalOffset + off) % Crypto.GROUP_SIZE);

            if(advance > 0)
            {
                byte[] discard = new byte[4096];
                int    remain  = advance;

                while(remain > 0)
                {
                    int step = remain > discard.Length ? discard.Length : remain;
                    Lfg.GetBytes(lfgBuffer, ref position, discard, 0, step);
                    remain -= step;
                }
            }

            Lfg.GetBytes(lfgBuffer, ref position, expectedData, 0, chunk);

            if(groupData.AsSpan((int)off, chunk).SequenceEqual(expectedData.AsSpan(0, chunk)))
            {
                Array.Clear(decryptedGroup, outOff, chunk);
                _ngcwJunkCollector.Add(groupStartOffset + Crypto.GROUP_HASH_SIZE + off, (ulong)chunk, (ushort)partIndex, seed);
            }
            else
                Array.Copy(groupData, (int)off, decryptedGroup, outOff, chunk);
        }

        for(uint i = 0; i < sectors; i++)
        {
            ulong absoluteSector = startSector + i;
            int   groupIndex     = (int)(absoluteSector - groupStartOffset / NGCW_SECTOR_SIZE);
            if(groupIndex < 0 || groupIndex >= NGCW_SECTORS_PER_GROUP) continue;

            Array.Copy(decryptedGroup,
                       groupIndex * NGCW_SECTOR_SIZE,
                       longBuffer,
                       i * NGCW_LONG_SECTOR_SIZE + NGCW_PAYLOAD_OFFSET,
                       NGCW_SECTOR_SIZE);
            statuses[i] = SectorStatus.Unencrypted;
        }

        return true;
    }

    bool EnsureNintendoDerivedKeyFromLba0(Reader scsiReader)
    {
        if(!_omniDriveNintendoSoftwareDescramble || _nintendoDerivedDiscKey.HasValue) return true;

        bool sense = scsiReader.ReadBlock(out byte[] raw, 0, out _, out _, out _);

        if(sense || _dev.Error || raw == null || raw.Length < NGCW_LONG_SECTOR_SIZE)
        {
            StoppingErrorMessage?.Invoke(Localization.Core.Unable_to_read_medium);

            return false;
        }

        ErrorNumber errno = _nintendoSectorDecoder.Scramble(raw, 0, out byte[] descrambled);

        if(errno != ErrorNumber.NoError || descrambled == null)
        {
            StoppingErrorMessage?.Invoke(Localization.Core.Unable_to_read_medium);

            return false;
        }

        byte[] cprMai8 = new byte[8];
        Array.Copy(descrambled, 6, cprMai8, 0, 8);
        _nintendoDerivedDiscKey = Sector.DeriveNintendoKey(cprMai8);
        UpdateStatus?.Invoke(string.Format(UI.Ngcw_nintendo_derived_key_0, _nintendoDerivedDiscKey.Value));

        return true;
    }

    bool DescrambleNintendoLongBuffer(byte[] longBuffer, ulong startSector, uint sectors)
    {
        if(!_omniDriveNintendoSoftwareDescramble) return true;

        for(uint i = 0; i < sectors; i++)
        {
            if(!DescrambleNintendo2064At(longBuffer, (int)(i * NGCW_LONG_SECTOR_SIZE), startSector + i))
                return false;
        }

        return true;
    }

    bool DescrambleNintendo2064At(byte[] buffer, int offset, ulong lba)
    {
        byte[] one = new byte[NGCW_LONG_SECTOR_SIZE];
        Array.Copy(buffer, offset, one, 0, NGCW_LONG_SECTOR_SIZE);
        byte key = lba < NGCW_SECTORS_PER_GROUP ? (byte)0 : (_nintendoDerivedDiscKey ?? (byte)0);

        ErrorNumber error = _nintendoSectorDecoder.Scramble(one, key, out byte[] decoded);

        if(error != ErrorNumber.NoError)
        {
            Array.Clear(buffer, offset, NGCW_LONG_SECTOR_SIZE);

            return false;
        }

        if(decoded != null) Array.Copy(decoded, 0, buffer, offset, NGCW_LONG_SECTOR_SIZE);

        if(lba == 0 && decoded != null)
        {
            byte[] cprMai8 = new byte[8];
            Array.Copy(decoded, 6, cprMai8, 0, 8);
            _nintendoDerivedDiscKey = Sector.DeriveNintendoKey(cprMai8);
        }

        return true;
    }

    List<WiiPartition> ParseWiiPartitionsFromDevice(Reader scsiReader)
    {
        byte[] partitionTable = ReadDiscBytesFromDevice(scsiReader, 0x40000, 32);

        if(partitionTable == null) return null;

        List<WiiPartition> partitions = new List<WiiPartition>();
        uint[] counts  = new uint[4];
        uint[] offsets = new uint[4];

        for(int t = 0; t < 4; t++)
        {
            counts[t]  = BigEndianBitConverter.ToUInt32(partitionTable, t * 8);
            offsets[t] = BigEndianBitConverter.ToUInt32(partitionTable, t * 8 + 4);
        }

        for(int t = 0; t < 4; t++)
        {
            if(counts[t] == 0) continue;

            ulong tableOffset = (ulong)offsets[t] << 2;
            int   tableSize   = (int)counts[t] * 8;
            byte[] tableData  = ReadDiscBytesFromDevice(scsiReader, tableOffset, tableSize);

            if(tableData == null) return null;

            for(uint p = 0; p < counts[t]; p++)
            {
                ulong partitionOffset = (ulong)BigEndianBitConverter.ToUInt32(tableData, (int)p * 8) << 2;
                uint  partType        = BigEndianBitConverter.ToUInt32(tableData, (int)p * 8 + 4);
                byte[] ticket         = ReadDiscBytesFromDevice(scsiReader, partitionOffset, 0x2A4);

                if(ticket == null) return null;

                byte[] titleKey = Crypto.DecryptTitleKey(ticket);
                byte[] header   = ReadDiscBytesFromDevice(scsiReader, partitionOffset + 0x2B8, 8);

                if(header == null) return null;

                ulong dataOffset = partitionOffset + ((ulong)BigEndianBitConverter.ToUInt32(header, 0) << 2);
                ulong dataSize   = (ulong)BigEndianBitConverter.ToUInt32(header, 4) << 2;

                partitions.Add(new WiiPartition
                {
                    Offset     = partitionOffset,
                    DataOffset = dataOffset,
                    DataSize   = dataSize,
                    Type       = partType,
                    TitleKey   = titleKey
                });
            }
        }

        return partitions;
    }

    byte[] ReadDiscBytesFromDevice(Reader scsiReader, ulong byteOffset, int length)
    {
        byte[] result = new byte[length];
        int    read   = 0;

        while(read < length)
        {
            ulong sector     = (byteOffset + (ulong)read) / NGCW_SECTOR_SIZE;
            int   sectorOff  = (int)((byteOffset + (ulong)read) % NGCW_SECTOR_SIZE);
            int   chunk      = NGCW_SECTOR_SIZE - sectorOff;

            if(chunk > length - read) chunk = length - read;

            bool sense = scsiReader.ReadBlock(out byte[] rawSector, sector, out _, out _, out _);

            if(sense || _dev.Error || rawSector == null || rawSector.Length < NGCW_PAYLOAD_OFFSET + NGCW_SECTOR_SIZE)
                return null;

            if(_omniDriveNintendoSoftwareDescramble && rawSector.Length >= NGCW_LONG_SECTOR_SIZE)
            {
                if(!DescrambleNintendo2064At(rawSector, 0, sector))
                    return null;
            }

            Array.Copy(rawSector, NGCW_PAYLOAD_OFFSET + sectorOff, result, read, chunk);
            read += chunk;
        }

        return result;
    }

    byte[] ReadRawPayloadSectors(Reader scsiReader, ulong startSector, uint count)
    {
        bool sense = scsiReader.ReadBlocks(out byte[] rawBuffer, startSector, count, out _, out _, out _);

        if(sense || _dev.Error || rawBuffer == null) return null;

        if(_omniDriveNintendoSoftwareDescramble && rawBuffer.Length >= count * NGCW_LONG_SECTOR_SIZE)
        {
            for(uint i = 0; i < count; i++)
            {
                if(!DescrambleNintendo2064At(rawBuffer, (int)(i * NGCW_LONG_SECTOR_SIZE), startSector + i))
                    return null;
            }
        }

        byte[] payload = new byte[count * NGCW_SECTOR_SIZE];

        for(uint i = 0; i < count; i++)
            Array.Copy(rawBuffer, i * NGCW_LONG_SECTOR_SIZE + NGCW_PAYLOAD_OFFSET, payload, i * NGCW_SECTOR_SIZE,
                       NGCW_SECTOR_SIZE);

        return payload;
    }
}
