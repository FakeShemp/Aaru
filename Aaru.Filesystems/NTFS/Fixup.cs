// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Fixup.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class NTFS
{
    /// <summary>Applies Update Sequence Array fixups to a multi-sector protected record.</summary>
    /// <param name="record">Record data to fix up in place.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ApplyUsaFixup(byte[] record)
    {
        if(record.Length < Marshal.SizeOf<NtfsRecordHeader>()) return ErrorNumber.InvalidArgument;

        var usaOffset = BitConverter.ToUInt16(record, 4);
        var usaCount  = BitConverter.ToUInt16(record, 6);

        // USA count includes the USN itself, so we have (usaCount - 1) fixup entries
        if(usaCount == 0 || usaOffset + usaCount * 2 > record.Length) return ErrorNumber.InvalidArgument;

        // The first entry of the USA is the Update Sequence Number (USN)
        var usn = BitConverter.ToUInt16(record, usaOffset);

        // Each subsequent entry replaces the last 2 bytes of each sector
        for(var i = 1; i < usaCount; i++)
        {
            int sectorEnd = i * (int)_bytesPerSector - 2;

            if(sectorEnd + 1 >= record.Length) break;

            // Verify the USN at the end of each sector matches
            var sectorUsn = BitConverter.ToUInt16(record, sectorEnd);

            if(sectorUsn != usn)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "USA fixup mismatch at sector {0}: 0x{1:X4} != 0x{2:X4}",
                                  i,
                                  sectorUsn,
                                  usn);

                return ErrorNumber.InOutError;
            }

            // Replace with the original data from the USA
            var originalValue = BitConverter.ToUInt16(record, usaOffset + i * 2);
            record[sectorEnd]     = (byte)(originalValue & 0xFF);
            record[sectorEnd + 1] = (byte)(originalValue >> 8);
        }

        return ErrorNumber.NoError;
    }
}