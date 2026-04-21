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
using Aaru.Compression.Zip;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class Qrst
{
    ErrorNumber WalkPreV5Tracks(Stream stream, int headerSize, int totalTracks)
    {
        long curOfs    = headerSize;
        var  trkHdrBuf = new byte[Marshal.SizeOf<QrstTrackHeader>()];
        var  blkLenBuf = new byte[sizeof(ushort)];

        for(var i = 0; i < totalTracks; i++)
        {
            stream.Seek(curOfs, SeekOrigin.Begin);

            if(stream.EnsureRead(trkHdrBuf, 0, trkHdrBuf.Length) != trkHdrBuf.Length)
                return ErrorNumber.InvalidArgument;

            QrstTrackHeader trkHdr = Marshal.ByteArrayToStructureLittleEndian<QrstTrackHeader>(trkHdrBuf);

            if(trkHdr.cyl > _cyls || trkHdr.head > _heads || trkHdr.type > TRK_CMPRSD)
                return ErrorNumber.InvalidArgument;

            int trkIdx = trkHdr.cyl * _heads + trkHdr.head;

            if(_trackOffset.ContainsKey(trkIdx)) return ErrorNumber.InvalidArgument;

            _trackOffset[trkIdx] =  curOfs;
            curOfs               += trkHdrBuf.Length;

            switch(trkHdr.type)
            {
                case TRK_NORMAL:
                    curOfs += _trackLen;

                    break;
                case TRK_BLANK:
                    curOfs += 1;

                    break;
                case TRK_CMPRSD:
                    if(stream.EnsureRead(blkLenBuf, 0, blkLenBuf.Length) != blkLenBuf.Length)
                        return ErrorNumber.InvalidArgument;

                    var blkLen = BitConverter.ToUInt16(blkLenBuf, 0);
                    curOfs += blkLenBuf.Length + blkLen;

                    break;
                default:
                    return ErrorNumber.InvalidArgument;
            }
        }

        if(stream.Length < curOfs) return ErrorNumber.InvalidArgument;

        if(_trackOffset.Count != totalTracks) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    ErrorNumber DecompressV5(Stream stream, int headerSize, long totalBytes)
    {
        if(!Blast.IsSupported)
        {
            AaruLogging.Error(MODULE_NAME, Localization.Qrst_V5_requires_native_decompressor);

            return ErrorNumber.NotSupported;
        }

        long payloadLen = stream.Length - headerSize;

        if(payloadLen <= 0 || payloadLen > int.MaxValue) return ErrorNumber.InvalidArgument;

        var compressed = new byte[payloadLen];
        stream.Seek(headerSize, SeekOrigin.Begin);

        if(stream.EnsureRead(compressed, 0, compressed.Length) != compressed.Length) return ErrorNumber.InOutError;

        var decompressed = new byte[totalBytes];
        int actual       = Blast.DecodeBuffer(compressed, decompressed);

        if(actual != totalBytes)
        {
            AaruLogging.Error(MODULE_NAME, Localization.Qrst_V5_decompression_yielded_incomplete_data);

            return ErrorNumber.InOutError;
        }

        _flatImage = decompressed;

        return ErrorNumber.NoError;
    }

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