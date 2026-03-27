// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Redumper.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages Redumper raw DVD dump images (.sdram + .state).
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

/// <inheritdoc cref="Aaru.CommonTypes.Interfaces.IOpticalMediaImage" />
/// <summary>
///     Implements reading Redumper raw DVD dump images (.sdram with .state sidecar).
///     The .sdram file stores scrambled DVD RecordingFrames (2366 bytes each, including
///     inner and outer Reed–Solomon parity). The .state file has one byte per frame
///     indicating dump status. The first frame in the file corresponds to physical
///     sector number LBA_START (-0x30000 / -196608).
/// </summary>
public sealed partial class Redumper : IOpticalMediaImage
{
    const string MODULE_NAME = "Redumper plugin";

    /// <summary>Size of a single DVD RecordingFrame: 12 rows of (172 main + 10 PI) + 182 PO.</summary>
    const int RECORDING_FRAME_SIZE = 2366;

    /// <summary>Size of a DVD sector without parity (ID + CPR_MAI + user data + EDC).</summary>
    const int DVD_SECTOR_SIZE = 2064;

    /// <summary>DVD user data size.</summary>
    const int DVD_USER_DATA_SIZE = 2048;

    /// <summary>Number of main-data bytes per row in a RecordingFrame.</summary>
    const int ROW_MAIN_DATA_SIZE = 172;

    /// <summary>Number of inner-parity bytes per row.</summary>
    const int ROW_PARITY_INNER_SIZE = 10;

    /// <summary>Number of rows in a RecordingFrame.</summary>
    const int RECORDING_FRAME_ROWS = 12;

    /// <summary>Size of the outer parity block.</summary>
    const int PARITY_OUTER_SIZE = 182;

    /// <summary>
    ///     First physical LBA stored at file offset 0 in the .sdram/.state files.
    ///     DVD user-data LBA 0 starts at file index -LBA_START (196608).
    /// </summary>
    const int LBA_START = -0x30000;

    /// <summary>SCSI READ DVD STRUCTURE parameter list header size (4 bytes).</summary>
    const int SCSI_HEADER_SIZE = 4;

    readonly Decoders.DVD.Sector      _decoding = new();
    readonly Decoders.Nintendo.Sector _nintendoDecoder = new();

    /// <summary>Derived Nintendo key from LBA 0 so sectors 16+ can be descrambled.</summary>
    byte? _nintendoDerivedKey;

    IFilter                          _imageFilter;
    ImageInfo                        _imageInfo;
    Dictionary<MediaTagType, byte[]> _mediaTags;
    byte[]                           _stateData;
    IFilter                          _sdramFilter;
    long                             _totalFrames;

    public Redumper() => _imageInfo = new ImageInfo
    {
        ReadableSectorTags    = [],
        ReadableMediaTags     = [],
        HasPartitions         = true,
        HasSessions           = true,
        Version               = null,
        Application           = "Redumper",
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