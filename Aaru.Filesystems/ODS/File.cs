// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     File operations for the Files-11 On-Disk Structure.
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

public sealed partial class ODS
{
    /// <summary>Reads a file header by file ID.</summary>
    /// <param name="fileNum">File number (1-based).</param>
    /// <param name="header">Output file header.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadFileHeader(ushort fileNum, out FileHeader header)
    {
        header = default(FileHeader);

        // File header LBN = ibmaplbn + ibmapsize + (fileNum - 1)
        // The header for file ID n is at CLUSTER*4 + IBMAPSIZE + n (in VBN)
        // or ibmaplbn + ibmapsize + (n-1) in LBN
        uint headerLbn = _homeBlock.ibmaplbn + _homeBlock.ibmapsize + fileNum - 1;

        ErrorNumber errno = ReadOdsBlock(_image, _partition, headerLbn, out byte[] headerSector);

        if(errno != ErrorNumber.NoError) return errno;

        header = Marshal.ByteArrayToStructureLittleEndian<FileHeader>(headerSector);

        // Validate file header checksum
        ushort calculatedChecksum = 0;

        for(var i = 0; i < 0x1FE; i += 2) calculatedChecksum += BitConverter.ToUInt16(headerSector, i);

        if(calculatedChecksum != header.checksum)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "File header checksum mismatch for file {0}: expected {1:X4}, calculated {2:X4}",
                              fileNum,
                              header.checksum,
                              calculatedChecksum);

            return ErrorNumber.InvalidArgument;
        }

        // Validate structure level
        var headerStrucLevel = (byte)(header.struclev >> 8 & 0xFF);

        if(headerStrucLevel != 2 && headerStrucLevel != 5)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid file header structure level: {0}", headerStrucLevel);

            return ErrorNumber.InvalidArgument;
        }

        // Validate offsets are in correct order
        if(header.idoffset > header.mpoffset || header.mpoffset > header.acoffset || header.acoffset > header.rsoffset)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid file header offsets");

            return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }
}