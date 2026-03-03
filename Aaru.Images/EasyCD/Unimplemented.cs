// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Unimplemented.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages Easy CD Creator disc images.
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
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

public sealed partial class EasyCD
{
    /// <inheritdoc />
    public ErrorNumber ReadMediaTag(MediaTagType tag, out byte[] buffer) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber
        ReadSector(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorLong(ulong            sectorAddress, bool negative, out byte[] buffer,
                                      out SectorStatus sectorStatus) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, bool negative, uint length, out byte[] buffer,
                                   out SectorStatus[] sectorStatus) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorsLong(ulong              sectorAddress, bool negative, uint length, out byte[] buffer,
                                       out SectorStatus[] sectorStatus) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorsTag(ulong      sectorAddress, bool negative, uint length, SectorTagType tag,
                                      out byte[] buffer) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorTag(ulong sectorAddress, bool negative, SectorTagType tag, out byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool? VerifySector(ulong sectorAddress) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, uint track, out byte[] buffer, out SectorStatus sectorStatus) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorTag(ulong sectorAddress, uint track, SectorTagType tag, out byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong              sectorAddress, uint length, uint track, out byte[] buffer,
                                   out SectorStatus[] sectorStatus) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorsTag(ulong      sectorAddress, uint length, uint track, SectorTagType tag,
                                      out byte[] buffer) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber
        ReadSectorLong(ulong sectorAddress, uint track, out byte[] buffer, out SectorStatus sectorStatus) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorsLong(ulong              sectorAddress, uint length, uint track, out byte[] buffer,
                                       out SectorStatus[] sectorStatus) => throw new NotImplementedException();

    /// <inheritdoc />
    public List<Track> GetSessionTracks(Session session) => throw new NotImplementedException();

    /// <inheritdoc />
    public List<Track> GetSessionTracks(ushort session) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, uint track, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas) => throw new NotImplementedException();
}