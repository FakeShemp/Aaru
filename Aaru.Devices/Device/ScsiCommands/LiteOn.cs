// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LiteOn.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : LiteOn vendor commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains vendor commands for Lite-On SCSI devices.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Devices;

public partial class Device
{
    private enum LiteOnBufferFormat
    {
        Unknown = 0,
        FullEccInterleaved,
        PoOnly,
        SectorDataOnly,
        FullEccWithPadding
    }

    private uint _bufferOffset;
    private uint _bufferStride;
    private uint _bufferCapacityInSectors;
    private LiteOnBufferFormat _bufferFormat;

    /// <summary>Reads a "raw" sector from DVD on Lite-On drives.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    /// <param name="buffer">Buffer where the ReadBuffer (RAW) response will be stored</param>
    /// <param name="senseBuffer">Sense buffer.</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
    /// <param name="lba">Start block address.</param>
    /// <param name="transferLength">How many blocks to read.</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <param name="otp">Set to <c>true</c> if disk is Opposite Track Path (OTP)</param>
    public bool LiteOnReadRawDvd(out byte[] buffer,  out ReadOnlySpan<byte> senseBuffer, uint lba, uint transferLength,
                                 uint       timeout, out double             duration,    uint layerbreak, bool otp)
    {
        // Detect stride and format on first call
        if(_bufferStride == 0)
        {
            uint detectedStride = DetectBufferStride(lba, timeout, out double detectDuration);
            
            if(detectedStride == 0 || detectedStride < 2064 || detectedStride > 10000)
            {
                // Detection failed, use default
                _bufferStride = 2384;
                _bufferFormat = LiteOnBufferFormat.FullEccInterleaved;
            }
            else
            {
                _bufferStride = detectedStride;
            }
            
            // Calculate buffer capacity in sectors
            // Buffer size is approximately 1,700,576 bytes
            // const uint BUFFER_SIZE = 1700576;
            // _bufferCapacityInSectors = BUFFER_SIZE / _bufferStride;
            // if(_bufferCapacityInSectors == 0) _bufferCapacityInSectors = 714; // Fallback to known value
            _bufferCapacityInSectors = 714;
        }

        _bufferOffset %= _bufferCapacityInSectors;

        bool sense;

        if(layerbreak > 0 && transferLength > 1 && lba + 0x30000 > layerbreak - 256 && lba + 0x30000 < layerbreak + 256)
        {
            buffer      = new byte[transferLength * 2064];
            duration    = 0;
            senseBuffer = SenseBuffer;

            return true;
        }

        if(_bufferCapacityInSectors - _bufferOffset < transferLength)
        {
            sense = LiteOnReadSectorsAcrossBufferBorder(out buffer,
                                                        out senseBuffer,
                                                        lba,
                                                        transferLength,
                                                        timeout,
                                                        out duration,
                                                        layerbreak,
                                                        otp);
        }
        else
        {
            sense = LiteOnReadSectorsFromBuffer(out buffer,
                                                out senseBuffer,
                                                lba,
                                                transferLength,
                                                timeout,
                                                out duration,
                                                layerbreak,
                                                otp);
        }

        Error = LastError != 0;

        AaruLogging.Debug(SCSI_MODULE_NAME, Localization.LiteOn_READ_DVD_RAW_took_0_ms, duration);

        return sense;
    }

    /// <summary>
    ///     Reads the Lite-On device's memory buffer and returns raw sector data
    /// </summary>
    /// <param name="buffer">Buffer where the ReadBuffer (RAW) response will be stored</param>
    /// <param name="senseBuffer">Sense buffer.</param>
    /// <param name="bufferOffset">The offset to read the buffer at</param>
    /// <param name="transferLength">How many blocks to read.</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
    /// <param name="lba">Start block address.</param>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    private bool LiteOnReadBuffer(out byte[] buffer,         out ReadOnlySpan<byte> senseBuffer, uint bufferOffset,
                                  uint       transferLength, uint timeout, out double duration, uint lba)
    {
        // We need to fill the buffer before reading it with the ReadBuffer command. We don't care about sense,
        // because the data can be wrong anyway, so we check the buffer data later instead.
        Read12(out _, out _, 0, false, false, false, false, lba, 2048, 0, 16, false, timeout, out duration);

        // Use generic ReadBuffer method with Lite-On specific mode (0x01) and buffer ID (0x01)
        return ScsiReadBuffer(out buffer, out senseBuffer, bufferOffset, transferLength, timeout, out duration, 0x01,
                              0x01);
    }

    /// <summary>
    ///     Reads raw sectors from the device's memory
    /// </summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    /// <param name="buffer">Buffer where the ReadBuffer (RAW) response will be stored</param>
    /// <param name="senseBuffer">Sense buffer.</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
    /// <param name="lba">Start block address.</param>
    /// <param name="transferLength">How many blocks to read.</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <param name="otp">Set to <c>true</c> if disk is Opposite Track Path (OTP)</param>
    private bool LiteOnReadSectorsFromBuffer(out byte[] buffer, out ReadOnlySpan<byte> senseBuffer, uint lba,
                                             uint transferLength, uint timeout, out double duration, uint layerbreak,
                                             bool otp)
    {
        bool sense = LiteOnReadBuffer(out buffer,
                                      out senseBuffer,
                                      _bufferOffset  * _bufferStride,
                                      transferLength * _bufferStride,
                                      timeout,
                                      out duration,
                                      lba);

        byte[] deinterleaved = DeinterleaveEccBlock(buffer, transferLength, _bufferStride, _bufferFormat);

        if(!CheckSectorNumber(deinterleaved, lba, transferLength, layerbreak, true))
        {
            // Buffer offset lost, try to find it again
            int offset = FindBufferOffset(lba, timeout, layerbreak, otp);

            if(offset == -1) return true;

            _bufferOffset = (uint)offset;

            sense = LiteOnReadBuffer(out buffer,
                                     out senseBuffer,
                                     _bufferOffset  * _bufferStride,
                                     transferLength * _bufferStride,
                                     timeout,
                                     out duration,
                                     lba);

            deinterleaved = DeinterleaveEccBlock(buffer, transferLength, _bufferStride, _bufferFormat);

            if(!CheckSectorNumber(deinterleaved, lba, transferLength, layerbreak, otp)) return true;
        }

        if(_decoding.Scramble(deinterleaved, transferLength, out byte[] scrambledBuffer) != ErrorNumber.NoError)
            return true;

        buffer = scrambledBuffer;

        _bufferOffset += transferLength;

        return sense;
    }

    /// <summary>
    ///     Reads raw sectors when they cross the device's memory border
    /// </summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    /// <param name="buffer">Buffer where the ReadBuffer (RAW) response will be stored</param>
    /// <param name="senseBuffer">Sense buffer.</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
    /// <param name="lba">Start block address.</param>
    /// <param name="transferLength">How many blocks to read.</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <param name="otp">Set to <c>true</c> if disk is Opposite Track Path (OTP)</param>
    private bool LiteOnReadSectorsAcrossBufferBorder(out byte[] buffer, out ReadOnlySpan<byte> senseBuffer, uint lba,
                                                     uint       transferLength, uint timeout, out double duration,
                                                     uint       layerbreak, bool otp)
    {
        uint newTransferLength1 = _bufferCapacityInSectors - _bufferOffset;
        uint newTransferLength2 = transferLength            - newTransferLength1;

        bool sense1 = LiteOnReadBuffer(out byte[] buffer1,
                                      out _,
                                      _bufferOffset      * _bufferStride,
                                      newTransferLength1 * _bufferStride,
                                      timeout,
                                      out double duration1,
                                      lba);

        bool sense2 = LiteOnReadBuffer(out byte[] buffer2,
                                      out _,
                                      0,
                                      newTransferLength2 * _bufferStride,
                                      timeout,
                                      out double duration2,
                                      lba);

        senseBuffer = SenseBuffer; // TODO

        buffer = new byte[_bufferStride * transferLength];
        Array.Copy(buffer1, buffer, buffer1.Length);
        Array.Copy(buffer2, 0,      buffer, buffer1.Length, buffer2.Length);

        duration = duration1 + duration2;

        byte[] deinterleaved = DeinterleaveEccBlock(buffer, transferLength, _bufferStride, _bufferFormat);

        if(!CheckSectorNumber(deinterleaved, lba, transferLength, layerbreak, otp)) return true;

        if(_decoding.Scramble(deinterleaved, transferLength, out byte[] scrambledBuffer) != ErrorNumber.NoError)
            return true;

        buffer = scrambledBuffer;

        _bufferOffset = newTransferLength2;

        return sense1 && sense2;
    }

    /// <summary>
    ///     Sometimes the offset on the drive memory can get lost. This tries to find it again.
    /// </summary>
    /// <param name="lba">The expected LBA</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <param name="otp">Set to <c>true</c> if disk is Opposite Track Path (OTP)</param>
    /// <returns>The offset on the device memory, or -1 if not found</returns>
    private int FindBufferOffset(uint lba, uint timeout, uint layerbreak, bool otp)
    {
        for(uint i = 0; i < _bufferCapacityInSectors; i++)
        {
            LiteOnReadBuffer(out byte[] buffer, out _, i * _bufferStride, _bufferStride, timeout, out double _, lba);

            byte[] deinterleaved = DeinterleaveEccBlock(buffer, 1, _bufferStride, _bufferFormat);

            if(CheckSectorNumber(deinterleaved, lba, 1, layerbreak, otp)) return (int)i;
        }

        return -1;
    }

    /// <summary>
    ///     Detects the stride (bytes per sector) in the Lite-On buffer by searching for
    ///     the 00 03 00 pattern that appears at the start of each sector
    /// </summary>
    /// <param name="lba">LBA to use for filling the buffer (sectors 0-16)</param>
    /// <param name="timeout">Timeout in seconds</param>
    /// <param name="duration">Duration in milliseconds</param>
    /// <returns>Detected stride in bytes, or 0 if detection failed</returns>
    private uint DetectBufferStride(uint lba, uint timeout, out double duration)
    {
        // Fill buffer with sectors 0-16
        Read12(out _, out _, 0, false, false, false, false, lba, 2048, 0, 16, false, timeout, out duration);

        // Read a large buffer chunk (enough for 16+ sectors)
        uint readSize = 16 * 3000; // Enough for 16 sectors even with large stride
        bool sense = ScsiReadBuffer(out byte[] buffer, out _, 0, readSize, timeout, out double readDuration, 0x01, 0x01);
        duration += readDuration;

        if(sense || buffer == null || buffer.Length < 2236 * 3) // Need at least 3 sectors worth
        {
            AaruLogging.Debug(SCSI_MODULE_NAME, "LiteOn buffer stride detection failed, sense or buffer too small, using default");
            _bufferFormat = LiteOnBufferFormat.FullEccInterleaved;
            return 2384; // Default to known value
        }

        // Search for pattern 00 03 00 starting from beginning
        // Find first occurrence
        int firstOffset = -1;
        for(int i = 0; i < buffer.Length - 3; i++)
        {
            if(buffer[i] == 0x00 && buffer[i + 1] == 0x03 && buffer[i + 2] == 0x00)
            {
                firstOffset = i;
                break;
            }
        }

        if(firstOffset != 0)
        {
            AaruLogging.Debug(SCSI_MODULE_NAME, "LiteOn buffer stride detection failed, pattern not at start, using default");
            _bufferFormat = LiteOnBufferFormat.FullEccInterleaved;
            return 2384; // Pattern not at start, use default
        }

        // Find second occurrence to calculate stride
        int secondOffset = -1;
        for(int i = firstOffset + 2064; i < Math.Min(firstOffset + 2500, buffer.Length - 3); i++)
        {
            if(buffer[i] == 0x00 && buffer[i + 1] == 0x03 && buffer[i + 2] == 0x00)
            {
                secondOffset = i;
                break;
            }
        }

        if(secondOffset == -1)
        {
            AaruLogging.Debug(SCSI_MODULE_NAME, "LiteOn buffer stride detection failed, couldn't find second sector, using default");
            _bufferFormat = LiteOnBufferFormat.FullEccInterleaved;
            return 2384; // Couldn't find second sector, use default
        }

        uint stride = (uint)(secondOffset - firstOffset);

        // Verify stride by checking 3rd and 4th sectors
        for(int sectorNum = 2; sectorNum <= 3; sectorNum++)
        {
            int expectedOffset = (int)(firstOffset + stride * sectorNum);
            if(expectedOffset + 3 >= buffer.Length) break;

            if(buffer[expectedOffset] != 0x00 ||
               buffer[expectedOffset + 1] != 0x03 ||
               buffer[expectedOffset + 2] != 0x00)
            {
                _bufferFormat = LiteOnBufferFormat.FullEccInterleaved;
                return 2384; // Verification failed, use default
            }
        }

        // Detect format based on stride
        _bufferFormat = stride switch
        {
            2064 => LiteOnBufferFormat.SectorDataOnly,
            2236 => LiteOnBufferFormat.PoOnly,
            2384 => LiteOnBufferFormat.FullEccInterleaved,
            > 2384 => LiteOnBufferFormat.FullEccWithPadding,
            _ => LiteOnBufferFormat.FullEccInterleaved // Default for backward compatibility
        };

        AaruLogging.Debug(SCSI_MODULE_NAME, "LiteOn buffer stride detection succeeded, stride: {0}, format: {1}", stride,
                          _bufferFormat);

        return stride;
    }

    /// <summary>
    ///     Deinterleave the ECC block based on detected format
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <param name="stride">Bytes per sector in buffer</param>
    /// <param name="format">Buffer format type</param>
    /// <returns>The deinterleaved sectors</returns>
    private byte[] DeinterleaveEccBlock(byte[] buffer, uint transferLength, uint stride, LiteOnBufferFormat format)
    {
        return format switch
        {
            LiteOnBufferFormat.FullEccInterleaved => DeinterleaveFullEccInterleaved(buffer, transferLength, stride),
            LiteOnBufferFormat.PoOnly => DeinterleavePoOnly(buffer, transferLength, stride),
            LiteOnBufferFormat.SectorDataOnly => buffer, // No deinterleaving needed for sector-data-only format
            LiteOnBufferFormat.FullEccWithPadding => DeinterleaveFullEccWithPadding(buffer, transferLength, stride),
            _ => DeinterleaveFullEccInterleaved(buffer, transferLength, stride) // Default fallback
        };
    }

    /// <summary>
    ///     Deinterleave the ECC block stored within a raw sector (backward compatibility wrapper)
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <returns>The deinterleaved sectors</returns>
    private byte[] DeinterleaveEccBlock(byte[] buffer, uint transferLength)
    {
        return DeinterleaveEccBlock(buffer, transferLength, _bufferStride, _bufferFormat);
    }

    /// <summary>
    ///     Deinterleave full ECC block with interleaved PI (e.g., 2384 bytes)
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <param name="stride">Bytes per sector in buffer</param>
    /// <returns>The deinterleaved sectors</returns>
    private static byte[] DeinterleaveFullEccInterleaved(byte[] buffer, uint transferLength, uint stride)
    {
        // TODO: Save ECC instead of just throwing it away

        var deinterleaved = new byte[2064 * transferLength];

        for(var j = 0; j < transferLength; j++)
        {
            for(var i = 0; i < 12; i++) Array.Copy(buffer, j * stride + i * 182, deinterleaved, j * 2064 + i * 172, 172);
        }

        return deinterleaved;
    }

    /// <summary>
    ///     Extract sector data from PO-only format (e.g., 2236 bytes)
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <param name="stride">Bytes per sector in buffer</param>
    /// <returns>The extracted sector data</returns>
    private static byte[] DeinterleavePoOnly(byte[] buffer, uint transferLength, uint stride)
    {
        var deinterleaved = new byte[2064 * transferLength];

        for(var j = 0; j < transferLength; j++)
        {
            Array.Copy(buffer, j * stride, deinterleaved, j * 2064, 2064);
        }

        return deinterleaved;
    }

    /// <summary>
    ///     Deinterleave full ECC block with padding (e.g., 2816 bytes)
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <param name="stride">Bytes per sector in buffer</param>
    /// <returns>The deinterleaved sectors</returns>
    private static byte[] DeinterleaveFullEccWithPadding(byte[] buffer, uint transferLength, uint stride)
    {
        // Same as FullEccInterleaved, padding is ignored
        return DeinterleaveFullEccInterleaved(buffer, transferLength, stride);
    }

}