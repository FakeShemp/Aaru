// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : C2.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : CompactDisc dumping.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles CompactDisc C2 error pointers for secure audio reading.
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
// ****************************************************************************/

using System;
using Aaru.Checksums;
using Aaru.Decoders.CD;
using Aaru.Devices;
using Aaru.Logging;

namespace Aaru.Core.Devices.Dumping;

partial class Dump
{
    // Full sector size (2352) + C2 error block (294) + raw subchannel (96)
    const uint C2_DATA_SIZE  = 2352;
    const uint C2_POINTERS   = 294;
    const uint C2_SUB_SIZE   = 96;
    const uint C2_BLOCK_SIZE = C2_DATA_SIZE + C2_POINTERS + C2_SUB_SIZE; // 2742

    /// <summary>
    ///     Probes, once at dump start, whether the drive returns C2 error pointers together with subchannel and, if so,
    ///     in which byte order (the MMC spec mandates [data][C2][sub] but many drives emit [data][sub][C2]). The
    ///     subchannel region is located by decoding its Q channel and validating its CRC-16; the remaining region is
    ///     the C2 block. Results are written to the dump log only. On any ambiguity or failure C2 is left disabled and
    ///     the normal dump path is used unchanged.
    /// </summary>
    /// <param name="firstLba">First readable LBA of the disc, used to derive a test sector.</param>
    /// <param name="readcd">Whether the drive supports the READ CD command.</param>
    /// <param name="supportedSubchannel">Subchannel mode the drive supports.</param>
    void DetectC2Layout(uint firstLba, bool readcd, MmcSubchannel supportedSubchannel)
    {
        _c2Supported = false;
        _c2BlockSize = 0;
        _c2Offset    = 0;
        _c2SubOffset = 0;

        // C2 secure audio reading requires READ CD and raw (96 byte) subchannel to validate the layout via Q CRC.
        if(!readcd || _omnidrive)
        {
            AaruLogging.WriteLine("C2 secure audio: not probed (requires generic READ CD path).");

            return;
        }

        if(supportedSubchannel != MmcSubchannel.Raw)
        {
            UpdateStatus?.Invoke("C2 secure audio disabled: requires raw subchannel reading.");
            AaruLogging.WriteLine("C2 secure audio disabled: drive does not provide raw (96 byte) subchannel.");

            return;
        }

        // Use a test sector well inside the first track, like the BCD subchannel probe does.
        var testLba = (firstLba / 75 + 1) * 75 + 35;

        bool sense = _dev.ReadCd(out byte[] cmdBuf,
                                 out _,
                                 testLba,
                                 C2_BLOCK_SIZE,
                                 1,
                                 MmcSectorTypes.AllTypes,
                                 false,
                                 false,
                                 true,
                                 MmcHeaderCodes.AllHeaders,
                                 true,
                                 true,
                                 MmcErrorField.C2Pointers,
                                 supportedSubchannel,
                                 _dev.Timeout,
                                 out _);

        if(sense || _dev.Error || cmdBuf is null || cmdBuf.Length < C2_BLOCK_SIZE)
        {
            UpdateStatus?.Invoke("C2 secure audio disabled: drive cannot return C2 and subchannel together.");
            AaruLogging.WriteLine("C2 secure audio disabled: READ CD with C2 pointers + subchannel failed.");

            return;
        }

        // Two candidate layouts. The subchannel region is the one whose Q channel CRC validates.
        // Layout A (MMC spec order): [data 2352][C2 294][sub 96]  -> C2 at 2352, sub at 2646
        // Layout B (common drives)  : [data 2352][sub 96][C2 294] -> sub at 2352, C2 at 2646
        const int specC2Offset  = (int)C2_DATA_SIZE;                 // 2352
        const int specSubOffset = (int)(C2_DATA_SIZE + C2_POINTERS); // 2646
        const int altSubOffset  = (int)C2_DATA_SIZE;                 // 2352
        const int altC2Offset   = (int)(C2_DATA_SIZE + C2_POINTERS); // 2646

        bool specValid = ValidateSubchannelQ(cmdBuf, specSubOffset);
        bool altValid  = ValidateSubchannelQ(cmdBuf, altSubOffset);

        int c2Offset;
        int subOffset;
        string layout;

        switch(specValid)
        {
            // Prefer spec order if it validates; only choose alt if spec did not and alt did.
            case true:
                c2Offset  = specC2Offset;
                subOffset = specSubOffset;
                layout    = "data + C2 + subchannel (MMC spec order)";

                break;
            case false when altValid:
                c2Offset  = altC2Offset;
                subOffset = altSubOffset;
                layout    = "data + subchannel + C2";

                break;
            default:
                UpdateStatus?.Invoke("C2 secure audio disabled: could not determine C2/subchannel byte order.");

                AaruLogging.WriteLine("C2 secure audio disabled: neither candidate subchannel region produced a " +
                                      "valid Q CRC on the test sector.");

                return;
        }

        _c2Supported    = true;
        _c2BlockSize    = C2_BLOCK_SIZE;
        _c2Offset       = c2Offset;
        _c2SubOffset    = subOffset;
        _c2SuspectAudio = [];

        // Corroborating evidence: on a clean pressed sector the C2 region should be (near) all zero.
        var dirtyC2Bytes = 0;

        for(var b = 0; b < C2_POINTERS; b++)
            if(cmdBuf[c2Offset + b] != 0)
                dirtyC2Bytes++;

        UpdateStatus?.Invoke($"C2 secure audio enabled: layout {layout}.");

        AaruLogging.WriteLine($"C2 secure audio enabled. Block size {C2_BLOCK_SIZE} bytes, layout {layout}. " +
                              $"Test sector {testLba}: Q CRC valid at offset {subOffset}, C2 region at offset " +
                              $"{c2Offset} had {dirtyC2Bytes}/{C2_POINTERS} non-zero bytes.");
    }

    /// <summary>
    ///     Repacks an audio block that was read with C2 error pointers (2742 bytes/sector) back into the normal block
    ///     layout (data + subchannel) the rest of the read loop expects, and records which sectors the drive concealed
    ///     (i.e. returned at least one interpolated/guessed byte) into the C2 suspect set. That set is kept separate
    ///     from hard read errors (<see cref="Resume.BadBlocks" />): a suspect sector already has data, it just is not
    ///     trustworthy byte-for-byte. Detection only; the audio is not rewritten here. Gated on a successful C2 probe.
    /// </summary>
    /// <param name="cmdBuf">Buffer read in C2 layout; replaced in place with the normal block layout.</param>
    /// <param name="blocksToRead">Number of sectors in the buffer.</param>
    /// <param name="blockSize">Normal block size (data + subchannel) expected downstream.</param>
    /// <param name="subSize">Subchannel size in bytes.</param>
    /// <param name="firstSectorToRead">LBA of the first sector in the buffer.</param>
    void RepackAudioC2(ref byte[] cmdBuf, uint blocksToRead, uint blockSize, uint subSize, uint firstSectorToRead)
    {
        if(!_c2Supported || cmdBuf is null || cmdBuf.Length < blocksToRead * _c2BlockSize) return;

        _c2SuspectAudio ??= [];

        var repacked = new byte[blockSize * blocksToRead];
        int copySub  = (int)Math.Min(subSize, C2_SUB_SIZE);

        // Sectors read to fix a negative offset wrap around near uint.MaxValue; their LBAs are not meaningful, so the
        // data is still repacked but C2 flags are not attributed to a (wrong) sector number.
        bool offsetWrapped = firstSectorToRead >= 0xFFFF0000;

        for(var b = 0; b < blocksToRead; b++)
        {
            int src = b * (int)_c2BlockSize;
            int dst = b * (int)blockSize;

            Array.Copy(cmdBuf, src,                repacked, dst,                     (int)C2_DATA_SIZE);
            Array.Copy(cmdBuf, src + _c2SubOffset, repacked, dst + (int)C2_DATA_SIZE, copySub);

            if(offsetWrapped || !SectorHasC2Error(cmdBuf, b)) continue;

            ulong sector = firstSectorToRead + (ulong)b;

            if(_c2SuspectAudio.Add(sector))
                _errorLog?.WriteLine(sector, "Audio sector concealed (C2 error pointers set)");
        }

        cmdBuf = repacked;
    }

    /// <summary>
    ///     Returns whether the C2 error block of the sector at index <paramref name="index" /> inside a multi-sector C2
    ///     read buffer has any bit set, i.e. the drive concealed at least one byte of that audio sector.
    /// </summary>
    bool SectorHasC2Error(byte[] block, int index)
    {
        int baseOffset = index * (int)_c2BlockSize + _c2Offset;

        if(block.Length < baseOffset + (int)C2_POINTERS) return false;

        for(var b = 0; b < C2_POINTERS; b++)
            if(block[baseOffset + b] != 0)
                return true;

        return false;
    }

    /// <summary>
    ///     Deinterleaves the raw 96-byte subchannel found at <paramref name="offset" /> inside <paramref name="block" />
    ///     and validates the CRC-16 of its Q channel.
    /// </summary>
    /// <returns><c>true</c> if the Q channel CRC is valid.</returns>
    static bool ValidateSubchannelQ(byte[] block, int offset)
    {
        if(block.Length < offset + (int)C2_SUB_SIZE) return false;

        var raw = new byte[C2_SUB_SIZE];
        Array.Copy(block, offset, raw, 0, (int)C2_SUB_SIZE);

        byte[] deinterleaved = Subchannel.Deinterleave(raw);

        // Q channel is the second 12-byte group of the deinterleaved buffer.
        var q = new byte[12];
        Array.Copy(deinterleaved, 12, q, 0, 12);

        CRC16CcittContext.Data(q, 10, out byte[] crc);

        return crc[0] == q[10] && crc[1] == q[11];
    }
}
