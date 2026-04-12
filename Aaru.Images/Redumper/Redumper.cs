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
//     Manages Redumper raw DVD (.sdram + .state) and Blu-ray (.sbram + .state) dumps.
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
///     Implements reading Redumper raw DVD (.sdram + .state) and Blu-ray (.sbram + .state) dumps.
///     DVD .sdram stores scrambled RecordingFrames (2366 bytes each). BD .sbram stores
///     scrambled DataFrames (2052 bytes: 2048 user + 4 EDC). The .state file has one byte
///     per frame. DVD LBA at file offset 0 is -0x30000; BD LBA at offset 0 is -0x100000.
/// </summary>
public sealed partial class Redumper : IOpticalMediaImage
{
    const string MODULE_NAME = "Redumper plugin";

    /// <summary>Size of a single DVD RecordingFrame: 12 rows of (172 main + 10 PI) + 182 PO.</summary>
    const int RECORDING_FRAME_SIZE = 2366;

    /// <summary>Size of a DVD sector without parity (ID + CPR_MAI + user data + EDC).</summary>
    const int DVD_SECTOR_SIZE = 2064;

    /// <summary>DVD/BD user data size.</summary>
    const int USER_DATA_SIZE = 2048;

    /// <summary>Number of main-data bytes per row in a RecordingFrame.</summary>
    const int ROW_MAIN_DATA_SIZE = 172;

    /// <summary>Number of inner-parity bytes per row.</summary>
    const int ROW_PARITY_INNER_SIZE = 10;

    /// <summary>Number of rows in a RecordingFrame.</summary>
    const int RECORDING_FRAME_ROWS = 12;

    /// <summary>Size of the outer parity block.</summary>
    const int PARITY_OUTER_SIZE = 182;

    /// <summary>First physical LBA at file offset 0 for DVD .sdram/.state.</summary>
    const int DVD_LBA_START = -0x30000;

    /// <summary>First physical LBA at file offset 0 for BD .sbram/.state.</summary>
    const int BD_LBA_START = -0x100000;

    /// <summary>One BD DataFrame in .sbram: main_data + EDC.</summary>
    const int BD_DATA_FRAME_SIZE = Decoders.Bluray.DataFrame.Size;

    /// <summary>SCSI READ DVD STRUCTURE parameter list header size (4 bytes).</summary>
    const int SCSI_HEADER_SIZE = 4;

    /// <summary>Active LBA at index 0; <see cref="DVD_LBA_START" /> or <see cref="BD_LBA_START" />.</summary>
    int _lbaStart;

    /// <summary>True when the data file is .sbram (Blu-ray DataFrame) instead of .sdram.</summary>
    bool _isBluRay;

    /// <summary>Wii U–style BD: physical structure payload all zeros (see redumper bd_extract_iso).</summary>
    bool _bdNintendo;

    readonly Decoders.DVD.Sector      _decoding = new();
    readonly Decoders.Nintendo.Sector _nintendoDecoder = new();

    /// <summary>Derived Nintendo key from LBA 0 so sectors 16+ can be descrambled.</summary>
    byte? _nintendoDerivedKey;
    ulong _ngcwRegularDataSectors;

    IFilter                          _imageFilter;
    ImageInfo                        _imageInfo;
    Dictionary<MediaTagType, byte[]> _mediaTags;
    byte[]                           _stateData;
    IFilter                          _ramFilter;
    string                           _ramPath;
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