// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Properties.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains properties for Redumper raw DVD dump images.
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

using System;
using System.Collections.Generic;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Partition = Aaru.CommonTypes.Partition;
using Track = Aaru.CommonTypes.Structs.Track;
using Session = Aaru.CommonTypes.Structs.Session;

namespace Aaru.Images;

public sealed partial class Redumper
{
    #region IOpticalMediaImage Members

    /// <inheritdoc />
    // ReSharper disable once ConvertToAutoProperty
    public ImageInfo Info => _imageInfo;

    /// <inheritdoc />
    public string Name => Localization.Redumper_Name;

    /// <inheritdoc />
    public Guid Id => new("F2D3E4A5-B6C7-4D8E-9F0A-1B2C3D4E5F60");

    /// <inheritdoc />
    public string Author => Authors.RebeccaWallander;

    /// <inheritdoc />
    public string Format => Localization.Redumper_disc_image;

    /// <inheritdoc />
    public List<Partition> Partitions { get; private set; }

    /// <inheritdoc />
    public List<Track> Tracks { get; private set; }

    /// <inheritdoc />
    public List<Session> Sessions { get; private set; }

    /// <inheritdoc />
    public List<DumpHardware> DumpHardware => null;

    /// <inheritdoc />
    public Metadata AaruMetadata => null;

    /// <inheritdoc />
    public IEnumerable<MediaTagType> SupportedMediaTags =>
    [
        MediaTagType.DVD_PFI,
        MediaTagType.DVD_PFI_2ndLayer,
        MediaTagType.DVD_DMI,
        MediaTagType.DVD_BCA
    ];

    /// <inheritdoc />
    public IEnumerable<SectorTagType> SupportedSectorTags =>
    [
        SectorTagType.DvdSectorInformation,
        SectorTagType.DvdSectorNumber,
        SectorTagType.DvdSectorIed,
        SectorTagType.DvdSectorCmi,
        SectorTagType.DvdSectorTitleKey,
        SectorTagType.DvdSectorEdc
    ];

    /// <inheritdoc />
    public IEnumerable<MediaType> SupportedMediaTypes =>
    [
        MediaType.DVDROM, MediaType.DVDR, MediaType.DVDRDL, MediaType.DVDRW, MediaType.DVDRWDL, MediaType.DVDRAM,
        MediaType.DVDPR, MediaType.DVDPRDL, MediaType.DVDPRW, MediaType.DVDPRWDL, MediaType.GOD, MediaType.WOD,
    ];

    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description, object @default)> SupportedOptions => [];

    /// <inheritdoc />
    public IEnumerable<string> KnownExtensions => [".state"];

    /// <inheritdoc />
    public bool IsWriting => false;

    /// <inheritdoc />
    public string ErrorMessage { get; private set; }

    #endregion
}