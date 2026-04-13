// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : EWF.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages Expert Witness Format disk images.
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
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Track = Aaru.CommonTypes.Structs.Track;

namespace Aaru.Images;

/// <inheritdoc cref="Aaru.CommonTypes.Interfaces.IOpticalMediaImage" />
/// <summary>Implements reading Expert Witness Format (EWF) disk images</summary>
public sealed partial class Ewf : IOpticalMediaImage
{
    const string MODULE_NAME = "EWF plugin";

    /// <summary>List of bad sector ranges from error2 sections</summary>
    List<(ulong start, uint count)> _badSectors;
    uint _bytesPerSector;

    /// <summary>Cache of decompressed chunks</summary>
    Dictionary<ulong, byte[]> _chunkCache;
    uint _chunkSize;

    /// <summary>Map from chunk index to location in segment file</summary>
    Dictionary<ulong, (int segmentIndex, long offset, uint size, bool compressed)> _chunkTable;

    /// <summary>Compression method for EWF v2</summary>
    EwfCompressionMethod _compressionMethod;

    /// <summary>Parsed header metadata key/value pairs</summary>
    Dictionary<string, string> _headerValues;

    ImageInfo _imageInfo;

    /// <summary>Whether the image uses the SMART volume section format</summary>
    bool _isSmart;

    /// <summary>Whether the image is EWF v2 format</summary>
    bool _isV2;

    int _maxChunkCache;

    /// <summary>Stored MD5 hash from hash/digest section</summary>
    byte[] _md5Stored;

    /// <summary>Populated metadata from header sections</summary>
    Metadata _metadata;

    /// <summary>Cache of read sectors</summary>
    Dictionary<ulong, byte[]> _sectorCache;

    uint _sectorsPerChunk;

    /// <summary>Ordered list of open segment file streams</summary>
    List<Stream> _segmentStreams;

    /// <summary>Session list for optical media</summary>
    List<Session> _sessions;

    /// <summary>Stored SHA1 hash from digest section</summary>
    byte[] _sha1Stored;

    /// <summary>Track list for optical media</summary>
    List<Track> _tracks;

    public Ewf() => _imageInfo = new ImageInfo
    {
        ReadableSectorTags    = [],
        ReadableMediaTags     = [],
        HasPartitions         = false,
        HasSessions           = false,
        Version               = null,
        Application           = null,
        ApplicationVersion    = null,
        Creator               = null,
        Comments              = null,
        MediaManufacturer     = null,
        MediaModel            = null,
        MediaSerialNumber     = null,
        MediaBarcode          = null,
        MediaPartNumber       = null,
        MediaSequence         = 0,
        LastMediaSequence     = 0,
        DriveManufacturer     = null,
        DriveModel            = null,
        DriveSerialNumber     = null,
        DriveFirmwareRevision = null
    };
}