// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Properties.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains properties for A2R flux images.
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
using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class A2R
{
    bool IsWritingRwcps { get; set; }

#region IFluxImage Members

    /// <inheritdoc />

    // ReSharper disable once ConvertToAutoProperty
    public ImageInfo Info => _imageInfo;

    /// <inheritdoc />
    public string Name => Localization.A2R_Name;

    /// <inheritdoc />
    public Guid Id => new("7497c26a-fe44-4b50-a2e6-de50a9f3c13f");

    /// <inheritdoc />
    public string Author => Authors.RebeccaWallander;

    /// <inheritdoc />
    public string Format => "A2R";

    /// <inheritdoc />
    public List<DumpHardware> DumpHardware => null;

    /// <inheritdoc />
    public Metadata AaruMetadata => null;

#endregion

#region IWritableImage Members

    /// <inheritdoc />
    public IEnumerable<string> KnownExtensions => [".a2r"];

    /// <inheritdoc />
    public IEnumerable<MediaTagType> SupportedMediaTags => [MediaTagType.Floppy_WriteProtection];

    /// <inheritdoc />
    public IEnumerable<MediaType> SupportedMediaTypes =>
    [
        // Apple formats
        MediaType.Apple32SS, MediaType.Apple32DS, MediaType.Apple33SS, MediaType.Apple33DS,
        MediaType.AppleSonySS, MediaType.AppleSonyDS, MediaType.AppleFileWare,
        
        // IBM PC/DOS formats - 5.25"
        MediaType.DOS_525_SS_DD_8, MediaType.DOS_525_SS_DD_9,
        MediaType.DOS_525_DS_DD_8, MediaType.DOS_525_DS_DD_9, MediaType.DOS_525_HD,
        
        // IBM PC/DOS formats - 3.5"
        MediaType.DOS_35_SS_DD_8, MediaType.DOS_35_SS_DD_9,
        MediaType.DOS_35_DS_DD_8, MediaType.DOS_35_DS_DD_9, MediaType.DOS_35_HD, MediaType.DOS_35_ED,
        
        // Microsoft formats
        MediaType.DMF, MediaType.DMF_82, MediaType.XDF_525, MediaType.XDF_35,
        
        // Atari formats
        MediaType.ATARI_525_SD, MediaType.ATARI_525_DD, MediaType.ATARI_525_ED,
        MediaType.ATARI_35_SS_DD, MediaType.ATARI_35_DS_DD,
        MediaType.ATARI_35_SS_DD_11, MediaType.ATARI_35_DS_DD_11,
        
        // Commodore/Amiga formats
        MediaType.CBM_35_DD, MediaType.CBM_AMIGA_35_DD, MediaType.CBM_AMIGA_35_HD,
        MediaType.CBM_1540, MediaType.CBM_1540_Ext, MediaType.CBM_1571,
        
        // NEC/Sharp formats
        MediaType.NEC_525_SS, MediaType.NEC_525_DS, MediaType.NEC_525_HD,
        MediaType.NEC_35_HD_8, MediaType.NEC_35_HD_15, MediaType.NEC_35_TD,
        MediaType.SHARP_525, MediaType.SHARP_525_9, MediaType.SHARP_35, MediaType.SHARP_35_9,
        
        // 8" formats
        MediaType.NEC_8_SD, MediaType.NEC_8_DD,
        MediaType.ECMA_99_8, MediaType.ECMA_69_8,
        
        // IBM formats
        MediaType.IBM23FD, MediaType.IBM33FD_128, MediaType.IBM33FD_256, MediaType.IBM33FD_512,
        MediaType.IBM43FD_128, MediaType.IBM43FD_256,
        MediaType.IBM53FD_256, MediaType.IBM53FD_512, MediaType.IBM53FD_1024,
        
        // DEC formats
        MediaType.RX01, MediaType.RX02, MediaType.RX03, MediaType.RX50,
        
        // Acorn formats
        MediaType.ACORN_525_SS_SD_40, MediaType.ACORN_525_SS_SD_80,
        MediaType.ACORN_525_SS_DD_40, MediaType.ACORN_525_SS_DD_80, MediaType.ACORN_525_DS_DD,
        MediaType.ACORN_35_DS_DD, MediaType.ACORN_35_DS_HD,
        
        // ECMA standard formats
        MediaType.ECMA_54, MediaType.ECMA_59, MediaType.ECMA_66, MediaType.ECMA_69_8,
        MediaType.ECMA_69_15, MediaType.ECMA_69_26, MediaType.ECMA_70, MediaType.ECMA_78,
        MediaType.ECMA_78_2, MediaType.ECMA_99_15, MediaType.ECMA_99_26, MediaType.ECMA_100,
        MediaType.ECMA_125, MediaType.ECMA_147,
        
        // FDFORMAT formats
        MediaType.FDFORMAT_525_DD, MediaType.FDFORMAT_525_HD,
        MediaType.FDFORMAT_35_DD, MediaType.FDFORMAT_35_HD,
        
        // Other formats
        MediaType.Apricot_35, MediaType.MetaFloppy_Mod_I, MediaType.MetaFloppy_Mod_II,
        
        // Unknown/fallback
        MediaType.Unknown
    ];

    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description, object @default)> SupportedOptions => [];

    /// <inheritdoc />
    public IEnumerable<SectorTagType> SupportedSectorTags => [];

    /// <inheritdoc />
    public bool IsWriting { get; private set; }

    /// <inheritdoc />
    public string ErrorMessage { get; private set; }

#endregion
}