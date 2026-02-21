// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : OmniDrive.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : OmniDrive firmware vendor commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains vendor commands for OmniDrive firmware. OmniDrive is custom
//     firmware that adds SCSI command 0xC0 to read raw DVD sectors (2064 bytes)
//     directly by LBA.
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
using Aaru.CommonTypes.Structs.Devices.SCSI;
using Aaru.Logging;

namespace Aaru.Devices;

public partial class Device
{
    /// <summary>
    ///     Checks if the drive has OmniDrive firmware by inspecting INQUIRY Reserved5 (bytes 74+) for "OmniDrive",
    ///     matching redumper's is_omnidrive_firmware behaviour.
    /// </summary>
    /// <returns><c>true</c> if Reserved5 starts with "OmniDrive" and has at least 11 bytes (8 + 3 for version).</returns>
    public bool IsOmniDriveFirmware()
    {
        bool sense = ScsiInquiry(out byte[] buffer, out _, Timeout, out _);

        if(sense || buffer == null) return false;

        Inquiry? inquiry = Inquiry.Decode(buffer);

        if(!inquiry.HasValue || inquiry.Value.Reserved5 == null || inquiry.Value.Reserved5.Length < 11)
            return false;

        byte[] reserved5 = inquiry.Value.Reserved5;
        byte[] omnidrive = [0x4F, 0x6D, 0x6E, 0x69, 0x44, 0x72, 0x69, 0x76, 0x65]; // "OmniDrive"

        if(reserved5.Length < omnidrive.Length) return false;

        for(int i = 0; i < omnidrive.Length; i++)
            if(reserved5[i] != omnidrive[i])
                return false;

        return true;
    }

    /// <summary>Reads raw DVD sectors (2064 bytes) directly by LBA on OmniDrive firmware.</summary>
    /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer" /> contains the sense buffer.</returns>
    /// <param name="buffer">Buffer where the raw DataFrame response will be stored</param>
    /// <param name="senseBuffer">Sense buffer.</param>
    /// <param name="lba">Start block address (LBA).</param>
    /// <param name="transferLength">Number of 2064-byte sectors to read.</param>
    /// <param name="timeout">Timeout in seconds.</param>
    /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
    public bool OmniDriveReadRawDvd(out byte[] buffer, out ReadOnlySpan<byte> senseBuffer, uint lba, uint transferLength,
                                   uint timeout, out double duration)
    {
        senseBuffer = SenseBuffer;
        Span<byte> cdb = CdbBuffer[..12];
        cdb.Clear();

        buffer = new byte[2064 * transferLength];

        cdb[0]  = (byte)ScsiCommands.ReadOmniDrive;
        cdb[1]  = 0x11; // disc_type=1 (DVD), raw_addressing=0 (LBA), fua=0, descramble=1
        cdb[2]  = (byte)((lba >> 24) & 0xFF);
        cdb[3]  = (byte)((lba >> 16) & 0xFF);
        cdb[4]  = (byte)((lba >> 8)  & 0xFF);
        cdb[5]  = (byte)(lba & 0xFF);
        cdb[6]  = (byte)((transferLength >> 24) & 0xFF);
        cdb[7]  = (byte)((transferLength >> 16) & 0xFF);
        cdb[8]  = (byte)((transferLength >> 8)  & 0xFF);
        cdb[9]  = (byte)(transferLength & 0xFF);
        cdb[10] = 0; // subchannels=NONE, c2=0
        cdb[11] = 0; // control

        LastError = SendScsiCommand(cdb, ref buffer, timeout, ScsiDirection.In, out duration, out bool sense);

        Error = LastError != 0;

        AaruLogging.Debug(SCSI_MODULE_NAME, "OmniDrive READ RAW DVD took {0} ms", duration);

        return sense;
    }
}
