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
//     Contains helpers for QRST disk images.
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

using System;
using System.IO;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class Qrst
{
    ErrorNumber ReadTrackIntoCache(Stream stream, int trackNum)
    {
        if(!_trackOffset.TryGetValue(trackNum, out long offset)) return ErrorNumber.SectorNotFound;

        stream.Seek(offset, SeekOrigin.Begin);

        var trkHdrBuf = new byte[Marshal.SizeOf<QrstTrackHeader>()];

        if(stream.EnsureRead(trkHdrBuf, 0, trkHdrBuf.Length) != trkHdrBuf.Length) return ErrorNumber.InOutError;

        QrstTrackHeader trkHdr = Marshal.ByteArrayToStructureLittleEndian<QrstTrackHeader>(trkHdrBuf);

        var trackData = new byte[_trackLen];

        switch(trkHdr.type)
        {
            case TRK_NORMAL:
                if(stream.EnsureRead(trackData, 0, _trackLen) != _trackLen) return ErrorNumber.InOutError;

                break;
            case TRK_BLANK:
                int filler = stream.ReadByte();

                if(filler < 0) return ErrorNumber.InOutError;

                Array.Fill(trackData, (byte)filler);

                break;
            case TRK_CMPRSD:
                var blkLenBuf = new byte[sizeof(ushort)];

                if(stream.EnsureRead(blkLenBuf, 0, blkLenBuf.Length) != blkLenBuf.Length) return ErrorNumber.InOutError;

                var compLen = BitConverter.ToUInt16(blkLenBuf, 0);
                var cBuf    = new byte[compLen];

                if(stream.EnsureRead(cBuf, 0, compLen) != compLen) return ErrorNumber.InOutError;

                // RLE: alternating literal and run blocks, starting with a literal run.
                // Each block is prefixed by a single-byte repeat count.
                var sIdx    = 0;
                var dIdx    = 0;
                var literal = true;

                while(sIdx < compLen)
                {
                    int rep = cBuf[sIdx++];

                    if(literal)
                    {
                        if(sIdx + rep > compLen || dIdx + rep > _trackLen) return ErrorNumber.InOutError;

                        Array.Copy(cBuf, sIdx, trackData, dIdx, rep);
                        sIdx += rep;
                    }
                    else
                    {
                        if(sIdx >= compLen || dIdx + rep > _trackLen) return ErrorNumber.InOutError;

                        byte pat = cBuf[sIdx++];

                        for(var j = 0; j < rep; j++) trackData[dIdx + j] = pat;
                    }

                    dIdx    += rep;
                    literal =  !literal;
                }

                if(dIdx != _trackLen)
                {
                    AaruLogging.Error(MODULE_NAME, Localization.Qrst_track_decompression_yielded_incomplete_data);

                    return ErrorNumber.InOutError;
                }

                break;
            default:
                return ErrorNumber.InvalidArgument;
        }

        _trackCache[trackNum] = trackData;

        return ErrorNumber.NoError;
    }
}