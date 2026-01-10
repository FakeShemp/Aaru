// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ReadBuffer.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : SCSI READ BUFFER command.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains generic SCSI READ BUFFER command implementation.
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
using System.Collections.Generic;
using Aaru.Helpers;

namespace Aaru.Devices;

public partial class Device
{
    /// <summary>Reads from device buffer using SCSI READ BUFFER command with specified variant</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    /// <param name="buffer">Buffer where the ReadBuffer response will be stored</param>
    /// <param name="senseBuffer">Sense buffer</param>
    /// <param name="bufferOffset">The offset to read the buffer at</param>
    /// <param name="transferLength">Number of bytes to read</param>
    /// <param name="timeout">Timeout in seconds</param>
    /// <param name="duration">Duration in milliseconds</param>
    /// <param name="mode">Buffer mode (CDB byte 1, e.g., 0x01 for data, 0x02 for descriptors)</param>
    /// <param name="bufferId">Buffer ID (CDB byte 2, e.g., 0x01, 0x00)</param>
    public bool ScsiReadBuffer(out byte[] buffer,         out ReadOnlySpan<byte> senseBuffer, uint bufferOffset,
                                uint       transferLength, uint timeout, out double duration, byte mode, byte bufferId)
    {
        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..10];
        cdb.Clear();

        buffer = new byte[transferLength];

        cdb[0] = (byte)ScsiCommands.ReadBuffer;
        cdb[1] = mode;
        cdb[2] = bufferId;
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
    ///     Makes sure the data's sector number is the one expected.
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="firstLba">First consecutive LBA of the buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <param name="otp">Set to <c>true</c> if disk is Opposite Track Path (OTP)</param>
    /// <returns><c>false</c> if any sector is not matching expected value, else <c>true</c></returns>
    static bool CheckSectorNumber(IReadOnlyList<byte> buffer, uint firstLba, uint transferLength, uint layerbreak,
                                  bool                otp)
    {
        for(var i = 0; i < transferLength; i++)
        {
            var    layer        = (byte)(buffer[0 + 2064 * i] & 0x1);
            byte[] sectorBuffer = [0x0, buffer[1 + 2064 * i], buffer[2 + 2064 * i], buffer[3 + 2064 * i]];

            uint sectorNumber = BigEndianBitConverter.ToUInt32(sectorBuffer, 0);

            if(otp)
            {
                if(!IsCorrectDlOtpPsn(sectorNumber, (ulong)(firstLba + i), layer, layerbreak)) return false;
            }
            else
            {
                if(!IsCorrectSlPsn(sectorNumber, (ulong)(firstLba + i))) return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks if the PSN for a raw sector matches the expected LBA for a single layer DVD
    /// </summary>
    /// <param name="sectorNumber">The Sector Number from Identification Data (ID)</param>
    /// <param name="lba">The expected LBA</param>
    /// <returns><c>false</c> if the sector is not matching expected value, else <c>true</c></returns>
    static bool IsCorrectSlPsn(uint sectorNumber, ulong lba) => sectorNumber == lba + 0x30000;

    /// <summary>
    ///     Checks if the PSN for a raw sector matches the expected LBA for a dual layer DVD with parallel track path
    /// </summary>
    /// <param name="sectorNumber">The Sector Number from Identification Data (ID)</param>
    /// <param name="lba">The expected LBA</param>
    /// <param name="layer">Layer number</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <returns><c>false</c> if the sector is not matching expected value, else <c>true</c></returns>
    static bool IsCorrectDlPtpPsn(uint sectorNumber, ulong lba, byte layer, uint layerbreak)
    {
        if(layer != 1) return IsCorrectSlPsn(sectorNumber, lba);

        return sectorNumber == lba - layerbreak + 0x30000;
    }

    /// <summary>
    ///     Checks if the PSN for a raw sector matches the expected LBA for a dual layer DVD with opposite track path
    /// </summary>
    /// <param name="sectorNumber">The Sector Number from Identification Data (ID)</param>
    /// <param name="lba">The expected LBA</param>
    /// <param name="layer">Layer number</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <returns><c>false</c> if the sector is not matching expected value, else <c>true</c></returns>
    static bool IsCorrectDlOtpPsn(uint sectorNumber, ulong lba, byte layer, uint layerbreak)
    {
        if(layer != 1) return IsCorrectSlPsn(sectorNumber, lba);

        ulong n = ~(layerbreak + 1 + (layerbreak - (lba + 0x30000))) & 0x00ffffff;

        return sectorNumber == n;
    }

    /// <summary>
    ///     Deinterleave full ECC block with interleaved PI (e.g., 2384 bytes)
    /// </summary>
    /// <param name="buffer">Data buffer</param>
    /// <param name="transferLength">How many blocks in buffer</param>
    /// <param name="stride">Bytes per sector in buffer</param>
    /// <returns>The deinterleaved sectors</returns>
    static byte[] DeinterleaveFullEccInterleaved(byte[] buffer, uint transferLength, uint stride)
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
    static byte[] DeinterleavePoOnly(byte[] buffer, uint transferLength, uint stride)
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
    static byte[] DeinterleaveFullEccWithPadding(byte[] buffer, uint transferLength, uint stride)
    {
        // Same as FullEccInterleaved, padding is ignored
        return DeinterleaveFullEccInterleaved(buffer, transferLength, stride);
    }
}

