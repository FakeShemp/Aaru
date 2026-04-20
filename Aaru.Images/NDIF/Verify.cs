// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Verify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Verifies the internal 28-bit CRC of an Apple New Disk Image Format
//     image.
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

using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class Ndif
{
#region IVerifiableImage Members

    /// <summary>
    ///     Verifies the image against the 28-bit CRC stored in the NDIF <c>bcem</c> header. The CRC is computed over the
    ///     reconstructed 512-byte-per-sector stream in image order; <c>CHUNK_TYPE_NOCOPY</c> sectors feed in as zeros,
    ///     matching Apple DiskCopy's behaviour.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if the recomputed CRC matches the one stored in the header; <c>false</c> otherwise; <c>null</c> when
    ///     there are no chunks to verify.
    /// </returns>
    public bool? VerifyMediaImage()
    {
        if(_chunks is null || _chunks.Count == 0) return null;

        AaruLogging.WriteLine(Localization.Verifying_NDIF_image_CRC);

        var  zeroSector = new byte[SECTOR_SIZE];
        uint crc        = NdifCrc28.Init();

        // NDIF BlockChunk does not carry an explicit sector count, so we derive it from the start of the next chunk (or
        // the image total for the last one) after sorting by starting sector.
        var ordered = _chunks.OrderBy(static kvp => kvp.Key).ToList();

        for(var i = 0; i < ordered.Count; i++)
        {
            ulong      start = ordered[i].Key;
            BlockChunk chunk = ordered[i].Value;

            if(chunk.type == CHUNK_TYPE_END) continue;

            ulong end           = i + 1 < ordered.Count ? ordered[i + 1].Key : _imageInfo.Sectors;
            ulong chunkSectorCt = end - start;

            if(chunk.type == CHUNK_TYPE_NOCOPY)
            {
                // Fast path: avoid going through ReadSector for empty regions.
                for(ulong s = 0; s < chunkSectorCt; s++) crc = NdifCrc28.Update(crc, zeroSector, (int)SECTOR_SIZE);

                continue;
            }

            for(ulong s = 0; s < chunkSectorCt; s++)
            {
                ErrorNumber errno = ReadSector(start + s, false, out byte[] sector, out _);

                if(errno != ErrorNumber.NoError || sector is null) return false;

                crc = NdifCrc28.Update(crc, sector, (int)SECTOR_SIZE);
            }
        }

        uint calculated = NdifCrc28.Finish(crc);
        uint stored     = _header.crc & 0x0FFFFFFF;

        if(calculated == stored) return true;

        AaruLogging.WriteLine(Localization.NDIF_CRC_mismatch_expected_0_got_1, stored, calculated);

        return false;
    }

#endregion
}