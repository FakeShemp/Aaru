// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : HL-DT-ST.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : HL-DT-ST vendor commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains vendor commands for HL-DT-ST SCSI devices.
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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Decoders.DVD;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Devices;

public partial class Device
{
    readonly Sector _decoding = new();

    /// <summary>Reads a "raw" sector from DVD on HL-DT-ST drives.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    /// <param name="buffer">Buffer where the HL-DT-ST READ DVD (RAW) response will be stored</param>
    /// <param name="senseBuffer">Sense buffer.</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
    /// <param name="lba">Start block address.</param>
    /// <param name="transferLength">How many blocks to read.</param>
    /// <param name="layerbreak">The address in which the layerbreak occur</param>
    /// <param name="otp">Set to <c>true</c> if disk is Opposite Track Path (OTP)</param>
    public bool HlDtStReadRawDvd(out byte[] buffer,  out ReadOnlySpan<byte> senseBuffer, uint lba, uint transferLength,
                                 uint       timeout, out double             duration,    uint layerbreak, bool otp)
    {
        // We need to fill the buffer before reading it with the HL-DT-ST command. We don't care about sense,
        // because the data can be wrong anyway, so we check the buffer data later instead.
        Read12(out _, out _, 0, false, false, false, false, lba, 2048, 0, 16, false, timeout, out duration);

        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..12];
        cdb.Clear();
        buffer = new byte[2064 * transferLength];

        uint cacheDataOffset = 0x80000000 + lba % 96 * 2064;

        cdb[0]  = (byte)ScsiCommands.HlDtStVendor;
        cdb[1]  = 0x48;
        cdb[2]  = 0x49;
        cdb[3]  = 0x54;
        cdb[4]  = 0x01;
        cdb[6]  = (byte)((cacheDataOffset & 0xFF000000) >> 24);
        cdb[7]  = (byte)((cacheDataOffset & 0xFF0000)   >> 16);
        cdb[8]  = (byte)((cacheDataOffset & 0xFF00)     >> 8);
        cdb[9]  = (byte)(cacheDataOffset & 0xFF);
        cdb[10] = (byte)((buffer.Length & 0xFF00) >> 8);
        cdb[11] = (byte)(buffer.Length & 0xFF);

        LastError = SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.In, out duration, out bool sense);

        Error = LastError != 0;

        AaruLogging.Debug(SCSI_MODULE_NAME, Localization.HL_DT_ST_READ_DVD_RAW_took_0_ms, duration);

        if(!CheckSectorNumber(buffer, lba, transferLength, layerbreak, otp)) return true;

        if(_decoding.Scramble(buffer, transferLength, out byte[] scrambledBuffer) != ErrorNumber.NoError) return true;

        buffer = scrambledBuffer;

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

            var sectorNumber = BigEndianBitConverter.ToUInt32(sectorBuffer, 0);


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
    /// <param name="sectorNumber">The Sector Number from Identification Data (ID) </param>
    /// <param name="lba">The expected LBA</param>
    /// <returns><c>false</c> if the sector is not matching expected value, else <c>true</c></returns>
    private static bool IsCorrectSlPsn(uint sectorNumber, ulong lba) => sectorNumber == lba + 0x30000;

    /// <summary>
    ///     Checks if the PSN for a raw sector matches the expected LBA for a dual layer DVD with parallel track path
    /// </summary>
    /// <param name="sectorNumber">The Sector Number from Identification Data (ID) </param>
    /// <param name="lba">The expected LBA</param>
    /// <returns><c>false</c> if the sector is not matching expected value, else <c>true</c></returns>
    private static bool IsCorrectDlPtpPsn(uint sectorNumber, ulong lba, byte layer, uint layerbreak)
    {
        if(layer != 1) return IsCorrectSlPsn(sectorNumber, lba);

        return sectorNumber == lba - layerbreak + 0x30000;
    }

    /// <summary>
    ///     Checks if the PSN for a raw sector matches the expected LBA for a dual layer DVD with opposite track path
    /// </summary>
    /// <param name="sectorNumber">The Sector Number from Identification Data (ID) </param>
    /// <param name="lba">The expected LBA</param>
    /// <returns><c>false</c> if the sector is not matching expected value, else <c>true</c></returns>
    private static bool IsCorrectDlOtpPsn(uint sectorNumber, ulong lba, byte layer, uint layerbreak)
    {
        if(layer != 1) return IsCorrectSlPsn(sectorNumber, lba);

        ulong n = ~(layerbreak + 1 + (layerbreak - (lba + 0x30000))) & 0x00ffffff;

        return sectorNumber == n;
    }
}