// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Checksum.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Checksum operations for the Files-11 On-Disk Structure.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class ODS
{
    /// <summary>Validates the home block checksums.</summary>
    /// <param name="hbSector">Raw home block sector data.</param>
    /// <returns>True if checksums are valid, false otherwise.</returns>
    static bool ValidateHomeBlockChecksums(byte[] hbSector)
    {
        AaruLogging.Debug(MODULE_NAME, "ValidateHomeBlockChecksums: buffer length={0}", hbSector.Length);

        // Also parse as struct to compare
        HomeBlock hb = Marshal.ByteArrayToStructureLittleEndian<HomeBlock>(hbSector);

        AaruLogging.Debug(MODULE_NAME, "From struct: checksum1={0:X4}, checksum2={1:X4}", hb.checksum1, hb.checksum2);

        AaruLogging.Debug(MODULE_NAME,
                          "From bytes:  checksum1={0:X4}, checksum2={1:X4}",
                          BitConverter.ToUInt16(hbSector, 0x3A),
                          BitConverter.ToUInt16(hbSector, 0x1FE));

        // checksum1 is the sum of all 16-bit words from offset 0 to offset 0x3A (exclusive)
        ushort calculatedChecksum1 = 0;

        for(var i = 0; i < 0x3A; i += 2) calculatedChecksum1 += BitConverter.ToUInt16(hbSector, i);

        ushort storedChecksum1 = hb.checksum1;

        AaruLogging.Debug(MODULE_NAME,
                          "Checksum1: stored={0:X4}, calculated={1:X4}",
                          storedChecksum1,
                          calculatedChecksum1);

        if(calculatedChecksum1 != storedChecksum1)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Checksum1 mismatch: stored={0:X4}, calculated={1:X4}",
                              storedChecksum1,
                              calculatedChecksum1);

            return false;
        }

        // checksum2 is the continuation of that sum from offset 0x3A to offset 0x1FE (exclusive)
        // Note: This INCLUDES the checksum1 word itself (Linux kernel does this)
        ushort calculatedChecksum2 = calculatedChecksum1;

        for(var i = 0x3A; i < 0x1FE; i += 2) calculatedChecksum2 += BitConverter.ToUInt16(hbSector, i);

        ushort storedChecksum2 = hb.checksum2;

        AaruLogging.Debug(MODULE_NAME,
                          "Checksum2: stored={0:X4}, calculated={1:X4}",
                          storedChecksum2,
                          calculatedChecksum2);

        if(calculatedChecksum2 != storedChecksum2)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Checksum2 mismatch: stored={0:X4}, calculated={1:X4}",
                              storedChecksum2,
                              calculatedChecksum2);

            return false;
        }

        return true;
    }
}