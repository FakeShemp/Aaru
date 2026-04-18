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
//     Verifies partimage disk images.
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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Aaru.Checksums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class Partimage
{
#region IVerifiableImage Members

    /// <inheritdoc />
    public bool? VerifyMediaImage()
    {
        if(_imageStream is null) return null;

        ulong blockSize = _imageInfo.SectorSize;

        if(blockSize == 0 || _usedBlocks == 0) return true;

        ulong totalUsedBytes = _usedBlocks * blockSize;
        int   checkStructLen = Marshal.SizeOf<CCheck>();
        var   dataBuffer     = new byte[CHECK_FREQUENCY];
        var   checkBuffer    = new byte[checkStructLen];
        var   allOk          = true;

        try
        {
            _imageStream.Seek(_dataOff, SeekOrigin.Begin);

            ulong remaining = totalUsedBytes;
            ulong chunkIdx  = 0;

            while(remaining >= CHECK_FREQUENCY)
            {
                int dataRead = _imageStream.EnsureRead(dataBuffer, 0, CHECK_FREQUENCY);

                if(dataRead != CHECK_FREQUENCY)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Short read while verifying partimage data block {0}: expected {1}, got {2}",
                                      chunkIdx,
                                      CHECK_FREQUENCY,
                                      dataRead);

                    return null;
                }

                Crc32Context crc32 = new();
                crc32.Update(dataBuffer);
                var computedCrc = BigEndianBitConverter.ToUInt32(crc32.Final(), 0);

                int checkRead = _imageStream.EnsureRead(checkBuffer, 0, checkStructLen);

                if(checkRead != checkStructLen)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Short read while reading CCheck structure at data block {0}",
                                      chunkIdx);

                    return null;
                }

                CCheck check = Marshal.ByteArrayToStructureLittleEndian<CCheck>(checkBuffer);

                if(checkBuffer[0] != (byte)'C' ||
                   checkBuffer[1] != (byte)'H' ||
                   checkBuffer[2] != (byte)'K' ||
                   checkBuffer[3] != 0)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "Invalid CCheck magic at data block {0}: 0x{1:X2} 0x{2:X2} 0x{3:X2} 0x{4:X2}",
                                      chunkIdx,
                                      checkBuffer[0],
                                      checkBuffer[1],
                                      checkBuffer[2],
                                      checkBuffer[3]);

                    allOk = false;
                }
                else if(check.dwCRC != computedCrc)
                {
                    AaruLogging.Error(MODULE_NAME,
                                      "CRC mismatch at data block {0} (qwPos={1}): expected 0x{2:X8}, computed 0x{3:X8}",
                                      chunkIdx,
                                      check.qwPos,
                                      check.dwCRC,
                                      computedCrc);

                    allOk = false;
                }
                else
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Data block {0} (qwPos={1}) CRC 0x{2:X8} OK",
                                      chunkIdx,
                                      check.qwPos,
                                      computedCrc);
                }

                remaining -= CHECK_FREQUENCY;
                chunkIdx++;
            }

            // Partimage does not emit a CCheck for the trailing partial chunk (< CHECK_FREQUENCY bytes),
            // so there is nothing else to verify at the data-block level.
        }
        catch(IOException ex)
        {
            AaruLogging.Error(MODULE_NAME, "I/O error while verifying partimage data blocks: {0}", ex.Message);

            return null;
        }

        return allOk;
    }

#endregion
}