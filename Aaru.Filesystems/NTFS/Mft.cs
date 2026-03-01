// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mft.cs
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class NTFS
{
    /// <summary>Reads an MFT record by record number from the MFT located in the partition.</summary>
    /// <param name="recordNumber">MFT record number to read.</param>
    /// <param name="recordData">Output byte array with the raw MFT record data after USA fixup.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadMftRecord(uint recordNumber, out byte[] recordData)
    {
        recordData = null;

        // Calculate the byte offset of this MFT record within the partition
        long mftStartByte  = _bpb.mft_lsn * _bytesPerCluster;
        long recordOffset  = mftStartByte + (long)recordNumber * _mftRecordSize;
        long recordSector  = recordOffset / _bytesPerSector;
        var  sectorOffset  = (int)(recordOffset % _bytesPerSector);
        uint sectorsToRead = (_mftRecordSize + _bytesPerSector - 1) / _bytesPerSector;

        // Account for any sub-sector offset
        if(sectorOffset > 0) sectorsToRead++;

        ErrorNumber errno = _image.ReadSectors(_partition.Start + (ulong)recordSector,
                                               false,
                                               sectorsToRead,
                                               out byte[] sectorData,
                                               out _);

        if(errno != ErrorNumber.NoError) return errno;

        recordData = new byte[_mftRecordSize];
        Array.Copy(sectorData, sectorOffset, recordData, 0, _mftRecordSize);

        // Apply Update Sequence Array fixup
        return ApplyUsaFixup(recordData);
    }
}