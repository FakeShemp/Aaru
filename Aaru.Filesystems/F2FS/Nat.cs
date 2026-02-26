// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Nat.cs
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
    /// <summary>Looks up a node ID in the NAT to find its block address</summary>
    ErrorNumber LookupNat(uint nid, out uint blockAddr)
    {
        blockAddr = 0;

        // Check the NAT journal first (entries here override on-disk NAT)
        if(_natJournal.TryGetValue(nid, out NatEntry journalEntry))
        {
            blockAddr = journalEntry.block_addr;

            AaruLogging.Debug(MODULE_NAME,
                              "NAT journal hit: nid={0}, ino={1}, block_addr={2}",
                              nid,
                              journalEntry.ino,
                              journalEntry.block_addr);

            return ErrorNumber.NoError;
        }

        // NAT_BLOCK_OFFSET(nid) = nid / NAT_ENTRY_PER_BLOCK
        uint natBlockOffset = nid / NAT_ENTRY_PER_BLOCK;
        uint natEntryOffset = nid % NAT_ENTRY_PER_BLOCK;

        // Calculate the actual block address using the same logic as current_nat_addr()
        // block_addr = nat_blkaddr + (block_off << 1) - (block_off & (blks_per_seg - 1))
        uint natBlkAddr = _superblock.nat_blkaddr;
        uint baseAddr   = natBlkAddr + (natBlockOffset << 1) - (natBlockOffset & _blocksPerSegment - 1);

        // Check the NAT bitmap to determine which copy to use
        if(TestBit(natBlockOffset, _natBitmap)) baseAddr += _blocksPerSegment;

        AaruLogging.Debug(MODULE_NAME,
                          "NAT lookup: nid={0}, block_offset={1}, entry_offset={2}, nat_block_addr={3}",
                          nid,
                          natBlockOffset,
                          natEntryOffset,
                          baseAddr);

        // Read the NAT block
        ErrorNumber errno = ReadBlock(baseAddr, out byte[] natBlockData);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse the specific NAT entry
        // Each NatEntry is 9 bytes (1 + 4 + 4), packed
        int entrySize   = Marshal.SizeOf<NatEntry>();
        var entryOffset = (int)(natEntryOffset * entrySize);

        if(entryOffset + entrySize > natBlockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "NAT entry offset exceeds block size");

            return ErrorNumber.InvalidArgument;
        }

        var entryBytes = new byte[entrySize];
        Array.Copy(natBlockData, entryOffset, entryBytes, 0, entrySize);

        NatEntry natEntry = Marshal.ByteArrayToStructureLittleEndian<NatEntry>(entryBytes);

        blockAddr = natEntry.block_addr;

        AaruLogging.Debug(MODULE_NAME,
                          "NAT entry: version={0}, ino={1}, block_addr={2}",
                          natEntry.version,
                          natEntry.ino,
                          natEntry.block_addr);

        return ErrorNumber.NoError;
    }

    /// <summary>Extracts the NAT version bitmap from checkpoint data</summary>
    void ExtractNatBitmap()
    {
        uint natBitmapSize = _checkpoint.nat_ver_bitmap_bytesize;

        AaruLogging.Debug(MODULE_NAME, "NAT bitmap size: {0} bytes", natBitmapSize);

        // The bitmap starts at the sit_nat_version_bitmap field
        // which is right after the fixed part of the checkpoint structure
        int fixedCpSize = Marshal.SizeOf<Checkpoint>();

        bool hasLargeNatBitmap = (_checkpoint.ckpt_flags & CP_LARGE_NAT_BITMAP_FLAG) != 0;

        int bitmapOffset;

        if(hasLargeNatBitmap)
        {
            // NAT bitmap starts after a __le32 checksum at beginning of bitmap area
            bitmapOffset = fixedCpSize + 4;

            AaruLogging.Debug(MODULE_NAME, "Using large NAT bitmap at offset {0}", bitmapOffset);
        }
        else if(_superblock.cp_payload > 0)
        {
            // NAT bitmap is at beginning of sit_nat_version_bitmap, SIT bitmap in next block
            bitmapOffset = fixedCpSize;

            AaruLogging.Debug(MODULE_NAME, "Using NAT bitmap with payload at offset {0}", bitmapOffset);
        }
        else
        {
            // SIT bitmap comes first, then NAT bitmap
            uint sitBitmapSize = _checkpoint.sit_ver_bitmap_bytesize;
            bitmapOffset = fixedCpSize + (int)sitBitmapSize;

            AaruLogging.Debug(MODULE_NAME,
                              "Using NAT bitmap after SIT bitmap at offset {0} (SIT size: {1})",
                              bitmapOffset,
                              sitBitmapSize);
        }

        _natBitmap = new byte[natBitmapSize];

        if(bitmapOffset + natBitmapSize <= _checkpointData.Length)
            Array.Copy(_checkpointData, bitmapOffset, _natBitmap, 0, natBitmapSize);
        else
            AaruLogging.Debug(MODULE_NAME, "WARNING: NAT bitmap extends beyond checkpoint data");
    }
}