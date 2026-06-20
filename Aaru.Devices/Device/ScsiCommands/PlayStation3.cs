// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : PlayStation3.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Sony PS3 optical drive vendor commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains vendor SCSI commands for Sony PS-SYSTEM optical drives used to
//     authenticate with the drive and retrieve PS3 disc Data1/Data2 keys.
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
using Aaru.Logging;

namespace Aaru.Devices;

public partial class Device
{
    const byte PS3_SEND_KEY_MARKER   = 0xE0;
    const byte PS3_REPORT_KEY_MARKER  = 0xE0;
    const int  PS3_VENDOR_CDB_LENGTH  = 12;

    /// <summary>
    ///     Checks if the drive is a Sony PS3 optical drive (PS-SYSTEM 302R/408R) by INQUIRY vendor and product strings.
    /// </summary>
    /// <returns><c>true</c> if the drive is a Sony PS-SYSTEM optical drive.</returns>
    public bool IsPlayStation3Drive()
    {
        if(string.IsNullOrEmpty(Manufacturer) || string.IsNullOrEmpty(Model)) return false;

        return Manufacturer.Equals("SONY", StringComparison.Ordinal) &&
               Model.StartsWith("PS-SYSTEM", StringComparison.Ordinal);
    }

    static void FillPlayStation3SendKeyCdb(Span<byte> cdb, byte selector, ushort parameterListLength)
    {
        cdb.Clear();
        cdb[0]  = (byte)ScsiCommands.SendKey;
        cdb[7]  = PS3_SEND_KEY_MARKER;
        cdb[8]  = (byte)((parameterListLength >> 8) & 0xFF);
        cdb[9]  = (byte)(parameterListLength & 0xFF);
        cdb[10] = (byte)(selector & 0x3F);
    }

    static void FillPlayStation3ReportKeyCdb(Span<byte> cdb, byte selector, ushort allocationLength)
    {
        cdb.Clear();
        cdb[0]  = (byte)ScsiCommands.ReportKey;
        cdb[7]  = PS3_REPORT_KEY_MARKER;
        cdb[8]  = (byte)((allocationLength >> 8) & 0xFF);
        cdb[9]  = (byte)(allocationLength & 0xFF);
        cdb[10] = (byte)(selector & 0x3F);
    }

    static void FillPlayStation3VendorCdb(Span<byte> cdb, byte opcode, ReadOnlySpan<byte> token, uint payloadSize)
    {
        cdb.Clear();
        cdb[0] = opcode;
        cdb[2] = (byte)payloadSize;
        cdb[4] = token[0];
        cdb[5] = token[1];
        cdb[6] = token[2];
        cdb[7] = token[3];
        cdb[8] = token[4];
        cdb[9] = token[5];
        cdb[10] = token[6];
        cdb[11] = token[7];
    }

    /// <summary>Sends a PS3-modified MMC SEND KEY command with the given selector and parameter list.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    public bool PlayStation3SendKey(byte[] blob, byte selector, out ReadOnlySpan<byte> senseBuffer, uint timeout,
                                      out double duration)
    {
        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..PS3_VENDOR_CDB_LENGTH];
        FillPlayStation3SendKeyCdb(cdb, selector, (ushort)blob.Length);

        LastError = SendScsiCommand(cdb, ref blob, timeout, ScsiDirection.Out, out duration, out bool sense);

        Error = LastError != 0;

        AaruLogging.Debug(SCSI_MODULE_NAME, "PS3 SEND KEY selector {0} took {1} ms", selector, duration);

        return sense;
    }

    /// <summary>Receives a PS3-modified MMC REPORT KEY response with the given selector.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    public bool PlayStation3ReportKey(out byte[] buffer, byte selector, ushort allocationLength,
                                      out ReadOnlySpan<byte> senseBuffer, uint timeout, out double duration)
    {
        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..PS3_VENDOR_CDB_LENGTH];
        buffer = new byte[allocationLength];
        FillPlayStation3ReportKeyCdb(cdb, selector, allocationLength);

        LastError = SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.In, out duration, out bool sense);

        Error = LastError != 0;

        AaruLogging.Debug(SCSI_MODULE_NAME, "PS3 REPORT KEY selector {0} took {1} ms", selector, duration);

        return sense;
    }

    /// <summary>Sends a PS3 vendor command 0xE1 with an encrypted 8-byte token and 84-byte payload.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    public bool PlayStation3VendorE1(ReadOnlySpan<byte> token, byte[] payload84, out ReadOnlySpan<byte> senseBuffer,
                                     uint timeout, out double duration)
    {
        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..PS3_VENDOR_CDB_LENGTH];
        FillPlayStation3VendorCdb(cdb, (byte)ScsiCommands.PlayStation3VendorE1, token, (uint)payload84.Length);

        LastError = SendScsiCommand(cdb, ref payload84, timeout, ScsiDirection.Out, out duration, out bool sense);

        Error = LastError != 0;

        AaruLogging.Debug(SCSI_MODULE_NAME, "PS3 vendor E1 took {0} ms", duration);

        return sense;
    }

    /// <summary>Receives a PS3 vendor command 0xE0 response with an encrypted 8-byte token.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    public bool PlayStation3VendorE0(out byte[] buffer, ReadOnlySpan<byte> token, uint responseLength,
                                     out ReadOnlySpan<byte> senseBuffer, uint timeout, out double duration)
    {
        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..PS3_VENDOR_CDB_LENGTH];
        buffer = new byte[responseLength];
        FillPlayStation3VendorCdb(cdb, (byte)ScsiCommands.PlayStation3VendorE0, token, responseLength);

        LastError = SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.In, out duration, out bool sense);

        Error = LastError != 0;

        AaruLogging.Debug(SCSI_MODULE_NAME, "PS3 vendor E0 took {0} ms", duration);

        return sense;
    }
}
