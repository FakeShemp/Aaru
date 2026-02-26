// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Checkpoint.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <summary>Reads and validates the F2FS checkpoint</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadCheckpoint()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading checkpoint...");

        // Try checkpoint pack 1 at cp_blkaddr
        ErrorNumber errno = ValidateCheckpoint(_superblock.cp_blkaddr,
                                               out Checkpoint cp1,
                                               out ulong cp1Version,
                                               out byte[] cp1Data);

        bool cp1Valid = errno == ErrorNumber.NoError;

        AaruLogging.Debug(MODULE_NAME,
                          "Checkpoint pack 1 at block {0}: {1} (version: {2})",
                          _superblock.cp_blkaddr,
                          cp1Valid ? "valid" : "invalid",
                          cp1Valid ? cp1Version : 0);

        // Try checkpoint pack 2 at cp_blkaddr + blocks_per_segment
        uint cp2Addr = _superblock.cp_blkaddr + _blocksPerSegment;

        ErrorNumber errno2 = ValidateCheckpoint(cp2Addr, out Checkpoint cp2, out ulong cp2Version, out byte[] cp2Data);

        bool cp2Valid = errno2 == ErrorNumber.NoError;

        AaruLogging.Debug(MODULE_NAME,
                          "Checkpoint pack 2 at block {0}: {1} (version: {2})",
                          cp2Addr,
                          cp2Valid ? "valid" : "invalid",
                          cp2Valid ? cp2Version : 0);

        if(!cp1Valid && !cp2Valid)
        {
            AaruLogging.Debug(MODULE_NAME, "No valid checkpoint found");

            return ErrorNumber.InvalidArgument;
        }

        // Pick the checkpoint with the higher version
        if(cp1Valid && cp2Valid)
        {
            // ver_after: ((long long)((a) - (b)) > 0)
            if((long)(cp2Version - cp1Version) > 0)
            {
                _checkpoint     = cp2;
                _checkpointData = cp2Data;
                _cpStartAddr    = cp2Addr;
            }
            else
            {
                _checkpoint     = cp1;
                _checkpointData = cp1Data;
                _cpStartAddr    = _superblock.cp_blkaddr;
            }
        }
        else if(cp1Valid)
        {
            _checkpoint     = cp1;
            _checkpointData = cp1Data;
            _cpStartAddr    = _superblock.cp_blkaddr;
        }
        else
        {
            _checkpoint     = cp2;
            _checkpointData = cp2Data;
            _cpStartAddr    = cp2Addr;
        }

        // Extract the NAT version bitmap from the checkpoint
        ExtractNatBitmap();

        // Load NAT journal entries from the summary area
        ErrorNumber journalErrno = LoadNatJournal();

        if(journalErrno != ErrorNumber.NoError)
            AaruLogging.Debug(MODULE_NAME, "Warning: failed to load NAT journal: {0}", journalErrno);

        return ErrorNumber.NoError;
    }

    /// <summary>Validates one checkpoint pack by reading the first and last blocks and comparing versions</summary>
    ErrorNumber ValidateCheckpoint(uint cpAddr, out Checkpoint cp, out ulong version, out byte[] cpData)
    {
        cp      = default(Checkpoint);
        version = 0;
        cpData  = null;

        // Read the first block of the checkpoint pack
        ErrorNumber errno = ReadBlock(cpAddr, out byte[] firstBlock);

        if(errno != ErrorNumber.NoError) return errno;

        if(firstBlock.Length < Marshal.SizeOf<Checkpoint>()) return ErrorNumber.InvalidArgument;

        Checkpoint firstCp = Marshal.ByteArrayToStructureLittleEndian<Checkpoint>(firstBlock);

        // Validate checksum offset
        uint crcOffset         = firstCp.checksum_offset;
        var  cpMinChksumOffset = (uint)Marshal.SizeOf<Checkpoint>();

        if(crcOffset < cpMinChksumOffset || crcOffset > _blockSize - 4)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid checkpoint checksum offset: {0}", crcOffset);

            return ErrorNumber.InvalidArgument;
        }

        // Verify the first block's CRC using the same logic as f2fs_checkpoint_chksum()
        var  storedCrc   = BitConverter.ToUInt32(firstBlock, (int)crcOffset);
        uint computedCrc = F2fsCheckpointChksum(firstBlock, crcOffset, _blockSize);

        if(storedCrc != computedCrc)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Checkpoint CRC mismatch: stored=0x{0:X8}, computed=0x{1:X8}",
                              storedCrc,
                              computedCrc);

            return ErrorNumber.InvalidArgument;
        }

        ulong firstVersion = firstCp.checkpoint_ver;

        // The total block count of the cp pack must be sane
        uint cpBlocks = firstCp.cp_pack_total_block_count;

        if(cpBlocks > _blocksPerSegment || cpBlocks <= 2)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid cp_pack_total_block_count: {0}", cpBlocks);

            return ErrorNumber.InvalidArgument;
        }

        // Read the last block of the checkpoint pack and check version matches
        errno = ReadBlock(cpAddr + cpBlocks - 1, out byte[] lastBlock);

        if(errno != ErrorNumber.NoError) return errno;

        if(lastBlock.Length < Marshal.SizeOf<Checkpoint>()) return ErrorNumber.InvalidArgument;

        Checkpoint lastCp = Marshal.ByteArrayToStructureLittleEndian<Checkpoint>(lastBlock);

        // Validate the last block's CRC
        uint lastCrcOffset = lastCp.checksum_offset;

        if(lastCrcOffset < cpMinChksumOffset || lastCrcOffset > _blockSize - 4) return ErrorNumber.InvalidArgument;

        var  lastStoredCrc   = BitConverter.ToUInt32(lastBlock, (int)lastCrcOffset);
        uint lastComputedCrc = F2fsCheckpointChksum(lastBlock, lastCrcOffset, _blockSize);

        if(lastStoredCrc != lastComputedCrc) return ErrorNumber.InvalidArgument;

        ulong lastVersion = lastCp.checkpoint_ver;

        // Both blocks must have the same version
        if(firstVersion != lastVersion)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Checkpoint version mismatch: first={0}, last={1}",
                              firstVersion,
                              lastVersion);

            return ErrorNumber.InvalidArgument;
        }

        // Read all checkpoint payload blocks if needed
        uint cpPayloadBlocks = 1 + _superblock.cp_payload;
        var  fullCpData      = new byte[cpPayloadBlocks * _blockSize];
        Array.Copy(firstBlock, 0, fullCpData, 0, _blockSize);

        for(uint i = 1; i < cpPayloadBlocks; i++)
        {
            errno = ReadBlock(cpAddr + i, out byte[] payloadBlock);

            if(errno != ErrorNumber.NoError) return errno;

            Array.Copy(payloadBlock, 0, fullCpData, i * _blockSize, _blockSize);
        }

        cp      = firstCp;
        version = firstVersion;
        cpData  = fullCpData;

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Loads NAT journal entries from the checkpoint's summary area.
    ///     The kernel stores frequently-updated NAT entries in the hot data summary journal
    ///     so they may not be reflected in the on-disk NAT blocks yet.
    /// </summary>
    ErrorNumber LoadNatJournal()
    {
        _natJournal.Clear();

        byte[] journalData;

        bool compactSummary = (_checkpoint.ckpt_flags & CP_COMPACT_SUM_FLAG) != 0;

        if(compactSummary)
        {
            // For compact summaries, the NAT journal is at the very start of the summary block
            // start_sum_block = cp_start + cp_pack_start_sum
            uint sumBlockAddr = _cpStartAddr + _checkpoint.cp_pack_start_sum;

            AaruLogging.Debug(MODULE_NAME, "Loading compact NAT journal from block {0}", sumBlockAddr);

            ErrorNumber errno = ReadBlock(sumBlockAddr, out byte[] sumBlock);

            if(errno != ErrorNumber.NoError) return errno;

            // First SUM_JOURNAL_SIZE bytes = NAT journal
            journalData = sumBlock;
        }
        else
        {
            // For normal summaries, the hot data summary block is at:
            // sum_blk_addr(NR_CURSEG_PERSIST_TYPE, CURSEG_HOT_DATA) =
            //   cp_start + cp_pack_total_block_count - (NR_CURSEG_PERSIST_TYPE + 1) + CURSEG_HOT_DATA
            uint hotDataSumAddr = _cpStartAddr +
                                  _checkpoint.cp_pack_total_block_count -
                                  (NR_CURSEG_PERSIST_TYPE + 1u) +
                                  CURSEG_HOT_DATA;

            AaruLogging.Debug(MODULE_NAME,
                              "Loading normal NAT journal from hot data summary block {0}",
                              hotDataSumAddr);

            ErrorNumber errno = ReadBlock(hotDataSumAddr, out byte[] sumBlock);

            if(errno != ErrorNumber.NoError) return errno;

            // In a normal summary block, the journal starts after the summary entries
            // offset = SUM_ENTRY_SIZE = SUMMARY_SIZE * ENTRIES_IN_SUM = 3584
            journalData = new byte[SUM_JOURNAL_SIZE];
            Array.Copy(sumBlock, SUM_ENTRY_SIZE, journalData, 0, SUM_JOURNAL_SIZE);
        }

        // The f2fs_journal starts with n_nats (u16), then nat_journal_entry[NAT_JOURNAL_ENTRIES]
        if(journalData.Length < 2)
        {
            AaruLogging.Debug(MODULE_NAME, "Journal data too small");

            return ErrorNumber.InvalidArgument;
        }

        var nNats = BitConverter.ToUInt16(journalData, 0);

        AaruLogging.Debug(MODULE_NAME, "NAT journal contains {0} entries", nNats);

        if(nNats > NAT_JOURNAL_ENTRIES)
        {
            AaruLogging.Debug(MODULE_NAME, "NAT journal entry count {0} exceeds max {1}", nNats, NAT_JOURNAL_ENTRIES);

            nNats = NAT_JOURNAL_ENTRIES;
        }

        // Each nat_journal_entry = { __le32 nid; struct f2fs_nat_entry ne; } = 13 bytes
        // They start at offset 2 in the journal
        var offset = 2;

        for(var i = 0; i < nNats; i++)
        {
            if(offset + NAT_JOURNAL_ENTRY_SIZE > journalData.Length) break;

            var nid = BitConverter.ToUInt32(journalData, offset);

            // Parse the NatEntry (9 bytes: version u8, ino u32, block_addr u32)
            var entryBytes = new byte[Marshal.SizeOf<NatEntry>()];
            Array.Copy(journalData, offset + 4, entryBytes, 0, entryBytes.Length);

            NatEntry natEntry = Marshal.ByteArrayToStructureLittleEndian<NatEntry>(entryBytes);

            _natJournal[nid] = natEntry;

            AaruLogging.Debug(MODULE_NAME,
                              "NAT journal[{0}]: nid={1}, ino={2}, block_addr={3}",
                              i,
                              nid,
                              natEntry.ino,
                              natEntry.block_addr);

            offset += NAT_JOURNAL_ENTRY_SIZE;
        }

        AaruLogging.Debug(MODULE_NAME, "Loaded {0} NAT journal entries", _natJournal.Count);

        return ErrorNumber.NoError;
    }
}