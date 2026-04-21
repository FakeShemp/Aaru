// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dxp.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages Disk eXPress (DXP) disk images.
//
//     Based on the work of Michal Necasek (fdimg).
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

using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

/// <inheritdoc cref="Aaru.CommonTypes.Interfaces.IMediaImage" />
/// <summary>Implements reading Disk eXPress (DXP) disk images</summary>
public sealed partial class Dxp : IMediaImage, IVerifiableImage
{
    const string MODULE_NAME = "DXP plugin";
    int          _compression;
    uint         _crcSeed;
    bool         _dataCrcOk;
    long         _dataOffset;
    byte[]       _decodedDisk;
    Header       _header;
    long         _headerOffset;
    ImageInfo    _imageInfo;

    public Dxp() => _imageInfo = new ImageInfo
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