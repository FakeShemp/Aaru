// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Optical.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains IOpticalMediaImage implementation for Expert Witness Format.
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

using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Track = Aaru.CommonTypes.Structs.Track;

namespace Aaru.Images;

public sealed partial class Ewf
{
#region IOpticalMediaImage Members

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, uint track, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer       = null;
        sectorStatus = SectorStatus.NotDumped;

        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc) return ErrorNumber.NotSupported;

        Track trk = _tracks?.FirstOrDefault(t => t.Sequence == track);

        if(trk == null) return ErrorNumber.SectorNotFound;

        return ReadSector(trk.StartSector + sectorAddress, false, out buffer, out sectorStatus);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectorTag(ulong sectorAddress, uint track, SectorTagType tag, out byte[] buffer)
    {
        buffer = null;

        return ErrorNumber.NotSupported;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, uint length, uint track, out byte[] buffer,
                                   out SectorStatus[] sectorStatus)
    {
        buffer       = null;
        sectorStatus = null;

        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc) return ErrorNumber.NotSupported;

        Track trk = _tracks?.FirstOrDefault(t => t.Sequence == track);

        if(trk == null) return ErrorNumber.SectorNotFound;

        return ReadSectors(trk.StartSector + sectorAddress, false, length, out buffer, out sectorStatus);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectorsTag(ulong      sectorAddress, uint length, uint track, SectorTagType tag,
                                      out byte[] buffer)
    {
        buffer = null;

        return ErrorNumber.NotSupported;
    }

    /// <inheritdoc />
    public ErrorNumber ReadSectorLong(ulong            sectorAddress, uint track, out byte[] buffer,
                                      out SectorStatus sectorStatus) =>
        ReadSector(sectorAddress, track, out buffer, out sectorStatus);

    /// <inheritdoc />
    public ErrorNumber ReadSectorsLong(ulong              sectorAddress, uint length, uint track, out byte[] buffer,
                                       out SectorStatus[] sectorStatus) =>
        ReadSectors(sectorAddress, length, track, out buffer, out sectorStatus);

    /// <inheritdoc />
    public List<Track> GetSessionTracks(Session session)
    {
        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc || _tracks == null) return null;

        return _tracks.Where(t => t.Session == session.Sequence).ToList();
    }

    /// <inheritdoc />
    public List<Track> GetSessionTracks(ushort session)
    {
        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc || _tracks == null) return null;

        return _tracks.Where(t => t.Session == session).ToList();
    }

    /// <inheritdoc />
    public bool? VerifySector(ulong sectorAddress) => null;

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas)
    {
        failingLbas = [];
        unknownLbas = [];

        if(_badSectors is not { Count: > 0 })
        {
            for(ulong i = sectorAddress; i < sectorAddress + length; i++) unknownLbas.Add(i);

            return null;
        }

        for(ulong i = sectorAddress; i < sectorAddress + length; i++)
        {
            var isBad = false;

            foreach((ulong start, uint count) in _badSectors)
            {
                if(i < start || i >= start + count) continue;

                isBad = true;

                break;
            }

            if(isBad)
                failingLbas.Add(i);
            else
                unknownLbas.Add(i);
        }

        if(unknownLbas.Count > 0) return null;

        return failingLbas.Count <= 0;
    }

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, uint track, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas)
    {
        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc || _tracks == null)
        {
            failingLbas = [];
            unknownLbas = [];

            for(ulong i = sectorAddress; i < sectorAddress + length; i++) unknownLbas.Add(i);

            return null;
        }

        Track trk = _tracks.FirstOrDefault(t => t.Sequence == track);

        if(trk == null)
        {
            failingLbas = [];
            unknownLbas = [];

            return null;
        }

        return VerifySectors(trk.StartSector + sectorAddress, length, out failingLbas, out unknownLbas);
    }

#endregion
}