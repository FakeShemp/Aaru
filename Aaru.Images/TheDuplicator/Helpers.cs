// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains helpers for The Duplicator disk images.
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

/* Based on the work of Michal Necasek (www.os2museum.com). */

using System.IO;
using System.Linq;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class TheDuplicator
{
    bool TryReadImage(Stream stream, ref TdupHeader hdr, ref TdupCylInfo[] cylMap)
    {
        if(stream.Length < CYLMAP_OFFSET) return false;

        // Check magic.
        stream.Seek(0, SeekOrigin.Begin);
        var magic = new byte[MAGIC_SIZE];
        stream.EnsureRead(magic, 0, MAGIC_SIZE);

        if(!magic.SequenceEqual(_magic)) return false;

        // Read the image header.
        stream.Seek(HDR_OFFSET, SeekOrigin.Begin);
        var hdrBuf = new byte[Marshal.SizeOf<TdupHeader>()];
        stream.EnsureRead(hdrBuf, 0, hdrBuf.Length);
        hdr = Marshal.ByteArrayToStructureLittleEndian<TdupHeader>(hdrBuf);

        // Sanity-check geometry.
        if(hdr.numHeads is < 1 or > 2) return false;

        if(hdr.numSec == 0 || hdr.numCyls == 0) return false;

        long cylLen = (long)hdr.numSec * hdr.numHeads * SECTOR_SIZE;

        if(cylLen <= 0 || cylLen > int.MaxValue) return false;

        // Read the cylinder map.
        int cylMapLen = hdr.numCyls * CYLINFO_SIZE;

        if(stream.Length < CYLMAP_OFFSET + cylMapLen) return false;

        stream.Seek(CYLMAP_OFFSET, SeekOrigin.Begin);
        cylMap = new TdupCylInfo[hdr.numCyls];
        var entry = new byte[CYLINFO_SIZE];

        for(var i = 0; i < hdr.numCyls; i++)
        {
            stream.EnsureRead(entry, 0, CYLINFO_SIZE);
            cylMap[i] = Marshal.ByteArrayToStructureLittleEndian<TdupCylInfo>(entry);

            switch(cylMap[i].flags)
            {
                case CYLFLG_IMGDATA:
                    long cylOfs = (long)cylMap[i].start * SECTOR_SIZE;

                    if(cylOfs < CYLMAP_OFFSET + cylMapLen) return false;

                    if(cylOfs + cylLen > stream.Length) return false;

                    break;
                case CYLFLG_FILLER:
                    break;
                default:
                    return false;
            }
        }

        // Informational logging: cksum and media fields (algorithms/encoding unknown).
        AaruLogging.Debug(MODULE_NAME,
                          Localization.TheDuplicator_header_version_0_media_1_unknown_2,
                          hdr.version,
                          hdr.media,
                          hdr.unknown);

        for(var i = 0; i < cylMap.Length; i++)
        {
            AaruLogging.Debug(MODULE_NAME,
                              Localization.TheDuplicator_cylinder_0_flags_1_start_2_cksum_3_filler_4,
                              i,
                              cylMap[i].flags,
                              cylMap[i].start,
                              cylMap[i].cksum,
                              cylMap[i].filler);
        }

        return true;
    }
}