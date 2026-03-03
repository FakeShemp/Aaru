// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : PowerISO.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages PowerISO disc images.
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
using System.IO;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decoders.CD;

namespace Aaru.Images;

public sealed partial class PowerISO : IOpticalMediaImage
{
    const string              MODULE_NAME = "PowerISO plugin";
    int                       _bitsizeLength;
    int                       _bitsizeType;
    int                       _bitSwapType;
    int                       _cachedChunk;
    Dictionary<int, byte[]>   _chunkCache;
    int                       _chunkDataOffset;
    uint                      _chunkSize;
    DaaChunk[]                _chunkTable;
    int                       _chunkTableOffset;
    bool                      _compressedChunkTable;
    uint                      _currentChunkCacheSize;
    bool                      _encrypted;
    DaaMainHeader             _header;
    IFilter                   _imageFilter;
    ImageInfo                 _imageInfo;
    DaaImageType              _imageType;
    byte[]                    _inflateBuffer;
    byte[]                    _ioBuffer;
    int                       _numChunks;
    int                       _numParts;
    bool                      _obfuscatedBits;
    bool                      _obfuscatedChunkTable;
    List<Stream>              _partStreams;
    DaaPart[]                 _partTable;
    SectorBuilder             _sectorBuilder;
    Dictionary<ulong, byte[]> _sectorCache;

    public PowerISO() => _imageInfo = new ImageInfo
    {
        ReadableSectorTags    = [],
        ReadableMediaTags     = [],
        HasPartitions         = true,
        HasSessions           = true,
        Version               = null,
        ApplicationVersion    = null,
        MediaTitle            = null,
        Creator               = null,
        MediaManufacturer     = null,
        MediaModel            = null,
        MediaPartNumber       = null,
        MediaSequence         = 0,
        LastMediaSequence     = 0,
        DriveManufacturer     = null,
        DriveModel            = null,
        DriveSerialNumber     = null,
        DriveFirmwareRevision = null
    };
}