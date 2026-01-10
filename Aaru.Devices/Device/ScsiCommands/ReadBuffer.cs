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
using Aaru.CommonTypes.Enums;

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
}

