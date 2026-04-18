// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Verify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Verifies partclone disc images.
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
using System.IO;
using System.IO.Hashing;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class PartClone
{
#region IVerifiableImage Members

    /// <inheritdoc />
    /// <summary>
    ///     Verifies a partclone image. Dispatches by detected format version: the v0001 path replays the cumulative
    ///     buggy CRC-32 stored after each block, while the v0002 path walks the data in <c>blocks_per_checksum</c>
    ///     strips and validates each strip with the algorithm declared in the image header.
    /// </summary>
    public bool? VerifyMediaImage()
    {
        if(_imageStream is null) return null;

        return _imageVersion switch
               {
                   2 => VerifyV2(),
                   _ => VerifyV1()
               };
    }

    bool? VerifyV1()
    {
        if(_pHdr.usedBlocks == 0) return true;

        var blockSize = (int)_pHdr.blockSize;

        if(blockSize <= 0 || _crcSize != CRC_SIZE_NORMAL && _crcSize != CRC_SIZE_X64_BUG) return null;

        var dataBuffer = new byte[blockSize];
        var crcBuffer  = new byte[_crcSize];
        var allOk      = true;

        try
        {
            _imageStream.Seek(_dataOff, SeekOrigin.Begin);

            uint running = CRC32_SEED;

            for(ulong blockIdx = 0; blockIdx < _pHdr.usedBlocks; blockIdx++)
            {
                int dataRead = _imageStream.EnsureRead(dataBuffer, 0, blockSize);

                if(dataRead != blockSize)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Short read while verifying partclone data block {0}: expected {1}, got {2}",
                                      blockIdx,
                                      blockSize,
                                      dataRead);

                    return null;
                }

                running = UpdateCrc32_0001(running, dataBuffer, blockSize);

                int crcRead = _imageStream.EnsureRead(crcBuffer, 0, _crcSize);

                if(crcRead != _crcSize)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Short read while reading CRC trailer at data block {0}: expected {1}, got {2}",
                                      blockIdx,
                                      _crcSize,
                                      crcRead);

                    return null;
                }

                // The x64 bug stores the CRC as an 8-byte little-endian value whose lower 4 bytes are the actual
                // CRC (source cast the seed via `*(uint32_t*)checksum`). Upper 4 bytes are padding and ignored.
                var storedCrc = BitConverter.ToUInt32(crcBuffer, 0);

                if(storedCrc != running)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "CRC mismatch at data block {0}: stored 0x{1:X8}, computed 0x{2:X8}",
                                      blockIdx,
                                      storedCrc,
                                      running);

                    allOk = false;
                }
                else
                    AaruLogging.Debug(MODULE_NAME, "Data block {0} CRC 0x{1:X8} OK", blockIdx, storedCrc);
            }
        }
        catch(IOException ex)
        {
            AaruLogging.Error(MODULE_NAME, "I/O error while verifying partclone data blocks: {0}", ex.Message);

            return null;
        }

        return allOk;
    }

    bool? VerifyV2()
    {
        if(_v2UsedBlocks == 0) return true;

        // CSM_NONE: nothing to verify in the data section.
        if(_v2ChecksumMode == CSM_NONE) return true;

        var blockSize = (int)_v2BlockSize;

        if(blockSize <= 0 || _v2BlocksPerChecksum == 0) return null;

        int csSize   = _v2ChecksumSize;
        var dataBuf  = new byte[blockSize];
        var storedCs = new byte[csSize];
        var computed = new byte[csSize];
        var allOk    = true;

        // Stateful instances for the streaming hashes; recreated when reseeding is required.
        XxHash64  xxh64  = null;
        XxHash128 xxh128 = null;
        uint      crc    = CRC32_SEED;

        ResetStrip(ref crc, ref xxh64, ref xxh128);

        try
        {
            _imageStream.Seek(_dataOff, SeekOrigin.Begin);

            ulong blocksInStrip = 0;
            ulong stripIndex    = 0;

            for(ulong blockIdx = 0; blockIdx < _v2UsedBlocks; blockIdx++)
            {
                int dataRead = _imageStream.EnsureRead(dataBuf, 0, blockSize);

                if(dataRead != blockSize)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Short read while verifying partclone v2 data block {0}: expected {1}, got {2}",
                                      blockIdx,
                                      blockSize,
                                      dataRead);

                    return null;
                }

                FeedStrip(dataBuf, blockSize, crc, xxh64, xxh128, ref crc);

                blocksInStrip++;

                bool isLastBlock      = blockIdx      == _v2UsedBlocks - 1;
                bool reachedStripSize = blocksInStrip == _v2BlocksPerChecksum;

                if(!reachedStripSize && !isLastBlock) continue;

                int csRead = _imageStream.EnsureRead(storedCs, 0, csSize);

                if(csRead != csSize)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Short read while reading partclone v2 strip checksum at strip {0}: expected {1}, got {2}",
                                      stripIndex,
                                      csSize,
                                      csRead);

                    return null;
                }

                FinalizeStrip(crc, xxh64, xxh128, computed);

                if(!ConstantTimeEqual(storedCs, computed))
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Partclone v2 checksum mismatch at strip {0} (block {1}): stored {2}, computed {3}",
                                      stripIndex,
                                      blockIdx,
                                      ToHex(storedCs),
                                      ToHex(computed));

                    allOk = false;
                }
                else
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Partclone v2 strip {0} checksum {1} OK",
                                      stripIndex,
                                      ToHex(storedCs));
                }

                stripIndex++;
                blocksInStrip = 0;

                if(_v2ReseedChecksum && !isLastBlock) ResetStrip(ref crc, ref xxh64, ref xxh128);
            }
        }
        catch(IOException ex)
        {
            AaruLogging.Error(MODULE_NAME, "I/O error while verifying partclone v2 data blocks: {0}", ex.Message);

            return null;
        }

        return allOk;
    }

    void ResetStrip(ref uint crc, ref XxHash64 xxh64, ref XxHash128 xxh128)
    {
        switch(_v2ChecksumMode)
        {
            case CSM_CRC32:
            case CSM_CRC32_0001:
                crc = CRC32_SEED;

                break;
            case CSM_XXH64:
                xxh64 = new XxHash64(0);

                break;
            case CSM_XXH128:
                xxh128 = new XxHash128(0);

                break;
        }
    }

    void FeedStrip(byte[] data, int size, uint crcIn, XxHash64 xxh64, XxHash128 xxh128, ref uint crcOut)
    {
        switch(_v2ChecksumMode)
        {
            case CSM_CRC32:
                crcOut = UpdateCrc32(crcIn, data, 0, size);

                break;
            case CSM_CRC32_0001:
                crcOut = UpdateCrc32_0001(crcIn, data, size);

                break;
            case CSM_XXH64:
                xxh64.Append(new ReadOnlySpan<byte>(data, 0, size));

                break;
            case CSM_XXH128:
                xxh128.Append(new ReadOnlySpan<byte>(data, 0, size));

                break;
        }
    }

    void FinalizeStrip(uint crc, XxHash64 xxh64, XxHash128 xxh128, byte[] dest)
    {
        switch(_v2ChecksumMode)
        {
            case CSM_CRC32:
            case CSM_CRC32_0001:
                dest[0] = (byte)(crc       & 0xFF);
                dest[1] = (byte)(crc >> 8  & 0xFF);
                dest[2] = (byte)(crc >> 16 & 0xFF);
                dest[3] = (byte)(crc >> 24 & 0xFF);

                break;
            case CSM_XXH64:
                xxh64.GetCurrentHash(dest);

                break;
            case CSM_XXH128:
                xxh128.GetCurrentHash(dest);

                break;
        }
    }

    static bool ConstantTimeEqual(byte[] a, byte[] b)
    {
        if(a.Length != b.Length) return false;

        var diff = 0;

        for(var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];

        return diff == 0;
    }

    static string ToHex(byte[] data)
    {
        var chars = new char[data.Length * 2];

        for(var i = 0; i < data.Length; i++)
        {
            byte b = data[i];
            chars[i * 2]     = HexNibble(b >> 4);
            chars[i * 2 + 1] = HexNibble(b & 0xF);
        }

        return new string(chars);
    }

    static char HexNibble(int n) => (char)(n < 10 ? '0' + n : 'A' + (n - 10));

#endregion
}