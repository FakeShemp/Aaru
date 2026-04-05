// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Verify.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Verifies Redumper raw DVD and Blu-ray dump images.
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

using System.Collections.Generic;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.Decoders.Bluray;
using DvdSector = Aaru.Decoders.DVD.Sector;

namespace Aaru.Images;

public sealed partial class Redumper
{
#region IOpticalMediaImage Members

    /// <inheritdoc />
    public bool? VerifySector(ulong sectorAddress)
    {
        long frameIndex = (long)sectorAddress - _lbaStart;

        if(frameIndex < 0 || frameIndex >= _totalFrames) return null;

        if(MapState(_stateData[frameIndex]) != SectorStatus.Dumped) return null;

        if(_isBluRay)
        {
            ErrorNumber errno = ReadBdFrameRawForVerify(sectorAddress, out byte[] raw);

            if(errno != ErrorNumber.NoError || raw is null) return null;

            return DataFrame.IsValid(raw, (int)sectorAddress, _bdNintendo);
        }

        ErrorNumber err = ReadSectorLong(sectorAddress, false, out byte[] dvdSector, out _);

        if(err != ErrorNumber.NoError || dvdSector is null) return null;

        if(!DvdSector.CheckIed(dvdSector)) return false;

        return DvdSector.CheckEdc(dvdSector, 1);
    }

    /// <summary>Reads scrambled BD frame without going through ReadSectorLong (avoids double descramble).</summary>
    ErrorNumber ReadBdFrameRawForVerify(ulong sectorAddress, out byte[] raw)
    {
        raw = null;

        long frameIndex = (long)sectorAddress - _lbaStart;

        if(frameIndex < 0 || frameIndex >= _totalFrames) return ErrorNumber.OutOfRange;

        raw = ReadBdFrame(frameIndex);

        return raw is null ? ErrorNumber.InvalidArgument : ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas)
    {
        failingLbas = [];
        unknownLbas = [];

        for(ulong i = 0; i < length; i++)
        {
            bool? result = VerifySector(sectorAddress + i);

            switch(result)
            {
                case null:
                    unknownLbas.Add(sectorAddress + i);

                    break;
                case false:
                    failingLbas.Add(sectorAddress + i);

                    break;
            }
        }

        if(unknownLbas.Count > 0) return null;

        return failingLbas.Count <= 0;
    }

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, uint track, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas) =>
        VerifySectors(sectorAddress, length, out failingLbas, out unknownLbas);

#endregion
}
