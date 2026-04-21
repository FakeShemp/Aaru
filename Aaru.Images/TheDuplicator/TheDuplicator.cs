// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : TheDuplicator.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages floppy disk images created with The Duplicator.
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
 * The Duplicator diskette image format was used by the eponymous DOS tool
 * used for duplicating floppies on a PC. The tool was developed by The Finot
 * Group of Mountain View, CA in the 1980s.
 *
 * All information about this format was obtained by analyzing available image
 * files and may not be entirely accurate.
 *
 * The format assumes uniform geometry with 512-byte sectors. The geometry
 * (heads/sectors per track/cylinders) is stored in the image.
 *
 * The image header is followed by a cylinder map. Provisions are made for
 * leaving empty cylinders out of the image.
 *
 * The format appears to store checksums for each cylinder and possibly the
 * header itself, but the algorithm is not known.
 *
 * An ASCII signature uniquely identifies this format.
 */

using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

/// <inheritdoc />
/// <summary>Implements reading The Duplicator disk images</summary>
public sealed partial class TheDuplicator : IMediaImage
{
    const string MODULE_NAME = "TheDuplicator plugin";
    /// <summary>The cylinder map, after the image has been opened.</summary>
    TdupCylInfo[] _cylMap;
    /// <summary>The image header, after the image has been opened.</summary>
    TdupHeader _header;

    /// <summary>The filter we are reading from, after the image has been opened.</summary>
    IFilter _imageFilter;
    /// <summary>The image information.</summary>
    ImageInfo _imageInfo;

    public TheDuplicator() => _imageInfo = new ImageInfo
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