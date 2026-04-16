// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : TarSparseStream.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Tar plugin.
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
using System.IO;

namespace Aaru.Archives;

/// <summary>Provides a read-only stream that reconstructs sparse files from TAR sparse region maps.</summary>
sealed class TarSparseStream : Stream
{
    readonly List<Tar.SparseRegion> _regions;
    readonly Stream                 _source;

    public TarSparseStream(Stream source, List<Tar.SparseRegion> regions, long realSize)
    {
        _source  = source;
        _regions = regions;
        Length   = realSize;
        Position = 0;
    }

    public override bool CanRead  => true;
    public override bool CanSeek  => true;
    public override bool CanWrite => false;
    public override long Length   { get; }

    public override long Position { get; set; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if(Position >= Length) return 0;

        // Clamp to end of file
        if(Position + count > Length) count = (int)(Length - Position);

        var totalRead = 0;

        while(count > 0 && Position < Length)
        {
            // Find which region (if any) contains the current position
            var  inDataRegion    = false;
            long regionDataStart = 0; // Offset in the source stream where this region's data begins
            long regionEnd       = 0;

            // Source data is laid out sequentially: region 0 data, region 1 data, etc.
            long sourceOffset = 0;

            for(var i = 0; i < _regions.Count; i++)
            {
                Tar.SparseRegion region      = _regions[i];
                long             regionStart = region.Offset;
                regionEnd = region.Offset + region.Length;

                if(Position >= regionStart && Position < regionEnd)
                {
                    inDataRegion    = true;
                    regionDataStart = sourceOffset + (Position - regionStart);

                    break;
                }

                sourceOffset += region.Length;
            }

            if(inDataRegion)
            {
                // Read from the source stream
                var toRead = (int)Math.Min(count, regionEnd - Position);
                _source.Position = regionDataStart;
                int read = _source.Read(buffer, offset, toRead);

                if(read <= 0) break;

                totalRead += read;
                offset    += read;
                count     -= read;
                Position  += read;
            }
            else
            {
                // We're in a gap — fill with zeros until we hit the next region or end of file
                long nextRegionStart = Length;

                foreach(Tar.SparseRegion region in _regions)
                {
                    if(region.Offset > Position && region.Offset < nextRegionStart) nextRegionStart = region.Offset;
                }

                var toZero = (int)Math.Min(count, nextRegionStart - Position);
                Array.Clear(buffer, offset, toZero);
                totalRead += toZero;
                offset    += toZero;
                count     -= toZero;
                Position  += toZero;
            }
        }

        return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
                   {
                       SeekOrigin.Begin   => offset,
                       SeekOrigin.Current => Position + offset,
                       SeekOrigin.End     => Length   + offset,
                       _                  => Position
                   };

        return Position;
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush() {}
}