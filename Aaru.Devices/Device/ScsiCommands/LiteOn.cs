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
    private uint _bufferCapacityInSectors;
    private LiteOnBufferFormat _bufferFormat;

    /// <summary>Reads a "raw" sector from DVD on Lite-On drives.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    /// <param name="buffer">Buffer where the ReadBuffer (RAW) response will be stored</param>
    /// <param name="senseBuffer">Sense buffer.</param>
    /// <param name="lba">Start block address.</param>
    /// <param name="transferLength">How many blocks to read.</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <param name="otp">Set to <c>true</c> if disk is Opposite Track Path (OTP)</param>
    public bool LiteOnReadRawDvd(out byte[] buffer,  out ReadOnlySpan<byte> senseBuffer, uint lba, uint transferLength,
                                 uint       timeout, out double             duration,    uint layerbreak, bool otp)
    {
        // Detect ReadBuffer 3C variant and stride on first call
        if(!_readBuffer3CDetected)
        {
            bool detected = DetectReadBuffer3C(lba, timeout, out double detectDuration);

            if(!detected || _detectedBufferStride == 0 || _detectedBufferStride < 2064 || _detectedBufferStride > 10000)
            {
                // Detection failed - raw reading is not supported on this drive
                AaruLogging.Debug(SCSI_MODULE_NAME,
                                  "ReadBuffer 3C detection failed - raw reading is not supported on this drive");

                buffer      = Array.Empty<byte>();
                senseBuffer = SenseBuffer;
                duration    = detectDuration;
                Error       = true;
                _readBuffer3CDetected = true;

                return true; // Return failure - raw reading not supported
            }

            // Detect format based on stride (Lite-On specific)
            _bufferFormat = _detectedBufferStride switch
            {
                2064 => LiteOnBufferFormat.SectorDataOnly,
                2236 => LiteOnBufferFormat.PoOnly,
                2384 => LiteOnBufferFormat.FullEccInterleaved,
                > 2384 => LiteOnBufferFormat.FullEccWithPadding,
                _ => LiteOnBufferFormat.FullEccInterleaved // Default for backward compatibility
            };

            AaruLogging.Debug(SCSI_MODULE_NAME,
                              "LiteOn buffer format detected based on stride: {0}, format: {1}",
                              _detectedBufferStride, _bufferFormat);

            // TODO: Calculate buffer capacity in sectors
            // Buffer size is approximately 1,700,576 bytes but need to test on other drives to get the correct value
            // It might also be the case that the buffer overflow works differently on different drives, so we need to test that as well.
            // const uint BUFFER_SIZE = 1700576;
            // _bufferCapacityInSectors = BUFFER_SIZE / _detectedBufferStride;
            // if(_bufferCapacityInSectors == 0) _bufferCapacityInSectors = 714; // Fallback to known value
            _bufferCapacityInSectors = 714;
            _readBuffer3CDetected = true;
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

        // Use generic ReadBuffer method with detected variant
        return ScsiReadBuffer(out buffer, out senseBuffer, bufferOffset, transferLength, timeout, out duration,
                              _detectedReadBufferMode, _detectedReadBufferId);
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
                                      _bufferOffset  * _detectedBufferStride,
                                      transferLength * _detectedBufferStride,
                                      timeout,
                                      out duration,
                                      lba);

        byte[] deinterleaved = DeinterleaveEccBlock(buffer, transferLength, _detectedBufferStride, _bufferFormat);

        if(!CheckSectorNumber(deinterleaved, lba, transferLength, layerbreak, true))
        {
            // Buffer offset lost, try to find it again
            int offset = FindBufferOffset(lba, timeout, layerbreak, otp);

            if(offset == -1) return true;

            _bufferOffset = (uint)offset;

            sense = LiteOnReadBuffer(out buffer,
                                     out senseBuffer,
                                     _bufferOffset  * _detectedBufferStride,
                                     transferLength * _detectedBufferStride,
                                     timeout,
                                     out duration,
                                     lba);

            deinterleaved = DeinterleaveEccBlock(buffer, transferLength, _detectedBufferStride, _bufferFormat);

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
        uint newTransferLength2 = transferLength           - newTransferLength1;

        bool sense1 = LiteOnReadBuffer(out byte[] buffer1,
                                      out _,
                                      _bufferOffset      * _detectedBufferStride,
                                      newTransferLength1 * _detectedBufferStride,
                                      timeout,
                                      out double duration1,
                                      lba);

        bool sense2 = LiteOnReadBuffer(out byte[] buffer2,
                                      out _,
                                      0,
                                      newTransferLength2 * _detectedBufferStride,
                                      timeout,
                                      out double duration2,
                                      lba);

        senseBuffer = SenseBuffer; // TODO

        buffer = new byte[_detectedBufferStride * transferLength];
        Array.Copy(buffer1, buffer, buffer1.Length);
        Array.Copy(buffer2, 0,      buffer, buffer1.Length, buffer2.Length);

        duration = duration1 + duration2;

        byte[] deinterleaved = DeinterleaveEccBlock(buffer, transferLength, _detectedBufferStride, _bufferFormat);

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
            LiteOnReadBuffer(out byte[] buffer, out _, i * _detectedBufferStride, _detectedBufferStride, timeout,
                            out double _, lba);

            byte[] deinterleaved = DeinterleaveEccBlock(buffer, 1, _detectedBufferStride, _bufferFormat);

            if(CheckSectorNumber(deinterleaved, lba, 1, layerbreak, otp)) return (int)i;
        }

        return -1;
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
}