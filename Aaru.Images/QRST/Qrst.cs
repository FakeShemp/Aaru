// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Qrst.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages Quick Release Sector Transfer (QRST) floppy disk images.
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

/* Based on the work of Michal Necasek (www.os2museum.com).
 *
 * The Quick Release Sector Transfer (QRST) format was developed by Compaq in
 * the early 1990s. There are two substantially different variants: the
 * original (pre-V5) version and version 5 (V5).
 *
 * QRST V5 contains a disk image compressed with the PKWARE Data Compression
 * Library and is not supported.
 *
 * QRST pre-V5 contains a collection of tracks. Each track may be uncompressed,
 * blank (with a filler byte), or run-length encoded. Only standard DOS disk
 * formats are supported (160K/180K/320K/360K/720K/1.2M/1.44M).
 *
 * Reference: http://fileformats.archiveteam.org/wiki/Quick_Release_Sector_Transfer
 */

using System.Collections.Generic;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

/// <inheritdoc />
/// <summary>Implements reading Quick Release Sector Transfer (QRST) disk images</summary>
public sealed partial class Qrst : IMediaImage
{
    const string MODULE_NAME = "QRST plugin";

    /// <summary>Cache of decompressed tracks, keyed by linear track index (cyl * heads + head).</summary>
    readonly Dictionary<int, byte[]> _trackCache = new();

    /// <summary>File offset of each track's header, keyed by linear track index.</summary>
    readonly Dictionary<int, long> _trackOffset = new();
    byte _cyls;

    QrstHeader _header;
    byte       _heads;

    IFilter   _imageFilter;
    ImageInfo _imageInfo;
    byte      _spt;
    int       _trackLen;

    public Qrst() => _imageInfo = new ImageInfo
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