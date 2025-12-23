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
    private uint _bufferOffset;

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
        _bufferOffset %= 714;

        bool sense;

        if(layerbreak > 0 && transferLength > 1 && lba + 0x30000 > layerbreak - 256 && lba + 0x30000 < layerbreak + 256)
        {
            buffer      = new byte[transferLength * 2064];
            duration    = 0;
            senseBuffer = SenseBuffer;

            return true;
        }

        if(714 - _bufferOffset < transferLength)
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
    /// <param name="transferLength"></param>
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

        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..10];
        cdb.Clear();

        buffer = new byte[transferLength];

        cdb[0] = (byte)ScsiCommands.ReadBuffer;
        cdb[1] = 0x01;
        cdb[2] = 0x01;
        cdb[3] = (byte)((bufferOffset & 0xFF0000) >> 16);
        cdb[4] = (byte)((bufferOffset & 0xFF00)   >> 8);
        cdb[5] = (byte)(bufferOffset & 0xFF);
        cdb[6] = (byte)((buffer.Length & 0xFF0000) >> 16);
        cdb[7] = (byte)((buffer.Length & 0xFF00)   >> 8);
        cdb[8] = (byte)(buffer.Length & 0xFF);

        LastError = SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.In, out duration, out bool sense);

        return sense;
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
                                      _bufferOffset  * 2384,
                                      transferLength * 2384,
                                      timeout,
                                      out duration,
                                      lba);

        byte[] deinterleaved = DeinterleaveEccBlock(buffer, transferLength);

        if(!CheckSectorNumber(deinterleaved, lba, transferLength, layerbreak, true))
        {
            // Buffer offset lost, try to find it again
            int offset = FindBufferOffset(lba, timeout, layerbreak, otp);

            if(offset == -1) return true;

            _bufferOffset = (uint)offset;

            sense = LiteOnReadBuffer(out buffer,
                                     out senseBuffer,
                                     _bufferOffset  * 2384,
                                     transferLength * 2384,
                                     timeout,
                                     out duration,
                                     lba);

            deinterleaved = DeinterleaveEccBlock(buffer, transferLength);

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
        uint newTransferLength1 = 714            - _bufferOffset;
        uint newTransferLength2 = transferLength - newTransferLength1;

        bool sense1 = LiteOnReadBuffer(out byte[] buffer1,
                                       out _,
                                       _bufferOffset      * 2384,
                                       newTransferLength1 * 2384,
                                       timeout,
                                       out double duration1,
                                       lba);

        bool sense2 = LiteOnReadBuffer(out byte[] buffer2,
                                       out _,
                                       0,
                                       newTransferLength2 * 2384,
                                       timeout,
                                       out double duration2,
                                       lba);

        senseBuffer = SenseBuffer; // TODO

        buffer = new byte[2384 * transferLength];
        Array.Copy(buffer1, buffer, buffer1.Length);
        Array.Copy(buffer2, 0,      buffer, buffer1.Length, buffer2.Length);

        duration = duration1 + duration2;

        byte[] deinterleaved = DeinterleaveEccBlock(buffer, transferLength);

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
        for(uint i = 0; i < 714; i++)
        {
            LiteOnReadBuffer(out byte[] buffer, out _, i * 2384, 2384, timeout, out double _, lba);

            if(CheckSectorNumber(buffer, lba, 1, layerbreak, otp)) return (int)i;
        }

        return -1;
    }

    /// <summary>
    ///     Deinterleave the ECC block stored within a 2384 byte raw sector
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <param name="deinterleaved"></param>
    /// <returns>The deinterleaved sectors</returns>
    private static byte[] DeinterleaveEccBlock(byte[] buffer, uint transferLength)
    {
        // TODO: Save ECC instead of just throwing it away

        var deinterleaved = new byte[2064 * transferLength];

        for(var j = 0; j < transferLength; j++)
        {
            for(var i = 0; i < 12; i++) Array.Copy(buffer, j * 2384 + i * 182, deinterleaved, j * 2064 + i * 172, 172);
        }

        return deinterleaved;
    }
}