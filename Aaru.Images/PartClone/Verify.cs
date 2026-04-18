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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class PartClone
{
#region IVerifiableImage Members

    /// <inheritdoc />
    /// <summary>
    ///     Verifies a partclone v0001 image by replaying the cumulative CRC-32 stored after each used block. The
    ///     checksum is produced by partclone's buggy <c>crc32_0001()</c> routine (see <see cref="UpdateCrc32_0001" />)
    ///     and is never reseeded, so the running seed is propagated from one block to the next.
    /// </summary>
    public bool? VerifyMediaImage()
    {
        if(_imageStream is null) return null;

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
                {
                    AaruLogging.Debug(MODULE_NAME, "Data block {0} CRC 0x{1:X8} OK", blockIdx, storedCrc);
                }
            }
        }
        catch(IOException ex)
        {
            AaruLogging.Error(MODULE_NAME, "I/O error while verifying partclone data blocks: {0}", ex.Message);

            return null;
        }

        return allOk;
    }

#endregion
}