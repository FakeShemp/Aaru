// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Read.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Reads Easy CD Creator disc images.
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
using System.IO;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;

namespace Aaru.Images;

public sealed partial class EasyCD
{
    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, uint track, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer       = null;
        sectorStatus = SectorStatus.NotDumped;

        Track trk = Tracks.FirstOrDefault(t => t.Sequence == track);

        if(trk is null) return ErrorNumber.SectorNotFound;

        if(sectorAddress < trk.StartSector || sectorAddress > trk.EndSector) return ErrorNumber.OutOfRange;

        ulong offsetInTrack = sectorAddress - trk.StartSector;
        var   fileOffset    = (long)(trk.FileOffset + offsetInTrack * (ulong)trk.RawBytesPerSector);

        if(trk.Type == TrackType.CdMode2Form1)
        {
            var rawSector = new byte[trk.RawBytesPerSector];
            _imageStream.Seek(fileOffset, SeekOrigin.Begin);
            _imageStream.EnsureRead(rawSector, 0, trk.RawBytesPerSector);

            buffer = new byte[trk.BytesPerSector];
            Array.Copy(rawSector, 8, buffer, 0, trk.BytesPerSector);
        }
        else
        {
            buffer = new byte[trk.BytesPerSector];
            _imageStream.Seek(fileOffset, SeekOrigin.Begin);
            _imageStream.EnsureRead(buffer, 0, trk.BytesPerSector);
        }

        sectorStatus = SectorStatus.Dumped;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, uint length, uint track, out byte[] buffer,
                                   out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        Track trk = Tracks.FirstOrDefault(t => t.Sequence == track);

        if(trk is null) return ErrorNumber.SectorNotFound;

        if(sectorAddress + length - 1 > trk.EndSector) return ErrorNumber.OutOfRange;

        using var ms = new MemoryStream();
        sectorStatus = new SectorStatus[length];

        for(uint i = 0; i < length; i++)
        {
            ErrorNumber errno = ReadSector(sectorAddress + i, track, out byte[] sectorData, out SectorStatus status);

            if(errno != ErrorNumber.NoError) return errno;

            ms.Write(sectorData, 0, sectorData.Length);
            sectorStatus[i] = status;
        }

        buffer = ms.ToArray();

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer       = null;
        sectorStatus = SectorStatus.NotDumped;

        if(negative) return ErrorNumber.NotSupported;

        Track trk = Tracks.FirstOrDefault(t => sectorAddress >= t.StartSector && sectorAddress <= t.EndSector);

        if(trk is null) return ErrorNumber.SectorNotFound;

        return ReadSector(sectorAddress, trk.Sequence, out buffer, out sectorStatus);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, bool negative, uint length, out byte[] buffer,
                                   out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        if(negative) return ErrorNumber.NotSupported;

        Track trk = Tracks.FirstOrDefault(t => sectorAddress >= t.StartSector && sectorAddress <= t.EndSector);

        if(trk is null) return ErrorNumber.SectorNotFound;

        return ReadSectors(sectorAddress, length, trk.Sequence, out buffer, out sectorStatus);
    }
}