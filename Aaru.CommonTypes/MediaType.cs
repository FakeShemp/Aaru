// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MediaType.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru common types.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains common media types.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

// ReSharper disable InconsistentNaming
// TODO: Rename contents

using System;
using System.ComponentModel;

#pragma warning disable 1591

// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo

namespace Aaru.CommonTypes;

public enum MediaEncoding
{
    Unknown,
    FM,
    MFM,
    M2FM,
    AppleGCR,
    CommodoreGCR
}

/// <summary>Contains an enumeration of all known types of media.</summary>
public enum MediaType : uint
{
#region Generics, types 0 to 9

    /// <summary>Unknown disk type</summary>
    Unknown = 0,
    /// <summary>Unknown magneto-optical</summary>
    [Description("Unknown magneto-optical")]
    UnknownMO = 1,
    /// <summary>Generic hard disk</summary>
    [Description("Generic hard disk")]
    GENERIC_HDD = 2,
    /// <summary>Microdrive type hard disk</summary>
    Microdrive = 3,
    /// <summary>Zoned hard disk</summary>
    [Description("Zoned hard disk")]
    Zone_HDD = 4,
    /// <summary>USB flash drives</summary>
    FlashDrive = 5,
    /// <summary>Unknown data tape</summary>
    [Description("Unknown data tape")]
    UnknownTape = 6,

#endregion Generics, types 0 to 9

#region Somewhat standard Compact Disc formats, types 10 to 39

    /// <summary>Any unknown or standard violating CD</summary>
    CD = 10,
    /// <summary>CD Digital Audio (Red Book)</summary>
    [Description("CD-DA")]
    CDDA = 11,
    /// <summary>CD+G (Red Book)</summary>
    [Description("CD+G")]
    CDG = 12,
    /// <summary>CD+EG (Red Book)</summary>
    [Description("CD+EG")]
    CDEG = 13,
    /// <summary>CD-i (Green Book)</summary>
    [Description("CD-i")]
    CDI = 14,
    /// <summary>CD-ROM (Yellow Book)</summary>
    [Description("CD-ROM")]
    CDROM = 15,
    /// <summary>CD-ROM XA (Yellow Book)</summary>
    [Description("CD-ROM XA")]
    CDROMXA = 16,
    /// <summary>CD+ (Blue Book)</summary>
    [Description("CD+")]
    CDPLUS = 17,
    /// <summary>CD-MO (Orange Book)</summary>
    [Description("CD-MO")]
    CDMO = 18,
    /// <summary>CD-Recordable (Orange Book)</summary>
    [Description("CD-R")]
    CDR = 19,
    /// <summary>CD-ReWritable (Orange Book)</summary>
    [Description("CD-RW")]
    CDRW = 20,
    /// <summary>Mount-Rainier CD-RW</summary>
    [Description("CD-MRW")]
    CDMRW = 21,
    /// <summary>Video CD (White Book)</summary>
    [Description("Video CD")]
    VCD = 22,
    /// <summary>Super Video CD (White Book)</summary>
    [Description("Super Video CD")]
    SVCD = 23,
    /// <summary>Photo CD (Beige Book)</summary>
    [Description("Photo CD")]
    PCD = 24,
    /// <summary>Super Audio CD (Scarlet Book)</summary>
    [Description("Super Audio CD")]
    SACD = 25,
    /// <summary>Double-Density CD-ROM (Purple Book)</summary>
    [Description("DDCD-ROM")]
    DDCD = 26,
    /// <summary>DD CD-R (Purple Book)</summary>
    [Description("DDCD-R")]
    DDCDR = 27,
    /// <summary>DD CD-RW (Purple Book)</summary>
    [Description("DDCD-RW")]
    DDCDRW = 28,
    /// <summary>DTS audio CD (non-standard)</summary>
    [Description("DTS CD")]
    DTSCD = 29,
    /// <summary>CD-MIDI (Red Book)</summary>
    [Description("CD-MIDI")]
    CDMIDI = 30,
    /// <summary>CD-Video (ISO/IEC 61104)</summary>
    [Description("CD-Video")]
    CDV = 31,
    /// <summary>120mm, Phase-Change, 1298496 sectors, 512 bytes/sector, PD650, ECMA-240, ISO 15485</summary>
    [Description("PD-650")]
    PD650 = 32,
    /// <summary>120mm, Write-Once, 1281856 sectors, 512 bytes/sector, PD650, ECMA-240, ISO 15485</summary>
    [Description("PD-650 (WORM)")]
    PD650_WORM = 33,
    /// <summary>
    ///     CD-i Ready, contains a track before the first TOC track, in mode 2, and all TOC tracks are Audio. Subchannel
    ///     marks track as audio pause.
    /// </summary>
    [Description("CD-i Ready")]
    CDIREADY = 34,
    [Description("FM-Towns")]
    FMTOWNS = 35,

#endregion Somewhat standard Compact Disc formats, types 10 to 39

#region Standard DVD formats, types 40 to 50

    /// <summary>DVD-ROM (applies to DVD Video and DVD Audio)</summary>
    [Description("DVD-ROM")]
    DVDROM = 40,
    /// <summary>DVD-R</summary>
    [Description("DVD-R")]
    DVDR = 41,
    /// <summary>DVD-RW</summary>
    [Description("DVD-RW")]
    DVDRW = 42,
    /// <summary>DVD+R</summary>
    [Description("DVD+R")]
    DVDPR = 43,
    /// <summary>DVD+RW</summary>
    [Description("DVD+RW")]
    DVDPRW = 44,
    /// <summary>DVD+RW DL</summary>
    [Description("DVD+RW DL")]
    DVDPRWDL = 45,
    /// <summary>DVD-R DL</summary>
    [Description("DVD-R DL")]
    DVDRDL = 46,
    /// <summary>DVD+R DL</summary>
    [Description("DVD+R DL")]
    DVDPRDL = 47,
    /// <summary>DVD-RAM</summary>
    [Description("DVD-RAM")]
    DVDRAM = 48,
    /// <summary>DVD-RW DL</summary>
    [Description("DVD-RW DL")]
    DVDRWDL = 49,
    /// <summary>DVD-Download</summary>
    [Description("DVD-Download")]
    DVDDownload = 50,

#endregion Standard DVD formats, types 40 to 50

#region Standard HD-DVD formats, types 51 to 59

    /// <summary>HD DVD-ROM (applies to HD DVD Video)</summary>
    [Description("HD DVD-ROM")]
    HDDVDROM = 51,
    /// <summary>HD DVD-RAM</summary>
    [Description("HD DVD-RAM")]
    HDDVDRAM = 52,
    /// <summary>HD DVD-R</summary>
    [Description("HD DVD-R")]
    HDDVDR = 53,
    /// <summary>HD DVD-RW</summary>
    [Description("HD DVD-RW")]
    HDDVDRW = 54,
    /// <summary>HD DVD-R DL</summary>
    [Description("HD DVD-R DL")]
    HDDVDRDL = 55,
    /// <summary>HD DVD-RW DL</summary>
    [Description("HD DVD-RW DL")]
    HDDVDRWDL = 56,

#endregion Standard HD-DVD formats, types 51 to 59

#region Standard Blu-ray formats, types 60 to 69

    /// <summary>BD-ROM (and BD Video)</summary>
    [Description("BD-ROM")]
    BDROM = 60,
    /// <summary>BD-R</summary>
    [Description("BD-R")]
    BDR = 61,
    /// <summary>BD-RE</summary>
    [Description("BD-RE")]
    BDRE = 62,
    /// <summary>BD-R XL</summary>
    [Description("BD-R XL")]
    BDRXL = 63,
    /// <summary>BD-RE XL</summary>
    [Description("BD-RE XL")]
    BDREXL = 64,
    /// <summary>Ultra HD Blu-ray</summary>
    [Description("Ultra HD Blu-ray")]
    UHDBD = 65,

#endregion Standard Blu-ray formats, types 60 to 69

#region Rare or uncommon optical standards, types 70 to 79

    /// <summary>Enhanced Versatile Disc</summary>
    EVD = 70,
    /// <summary>Forward Versatile Disc</summary>
    FVD = 71,
    /// <summary>Holographic Versatile Disc</summary>
    HVD = 72,
    /// <summary>China Blue High Definition</summary>
    CBHD = 73,
    /// <summary>High Definition Versatile Multilayer Disc</summary>
    HDVMD = 74,
    /// <summary>Versatile Compact Disc High Density</summary>
    VCDHD = 75,
    /// <summary>Stacked Volumetric Optical Disc</summary>
    SVOD = 76,
    /// <summary>Five Dimensional disc</summary>
    FDDVD = 77,
    /// <summary>China Video Disc</summary>
    CVD = 78,

#endregion Rare or uncommon optical standards, types 70 to 79

#region LaserDisc based, types 80 to 89

    /// <summary>Pioneer LaserDisc</summary>
    [Description("LaserDisc")]
    LD = 80,
    /// <summary>Pioneer LaserDisc data</summary>
    [Description("LD-ROM")]
    LDROM = 81,
    LDROM2 = 82,
    LVROM  = 83,
    MegaLD = 84,
    /// <summary>Writable LaserDisc with support for component video</summary>
    CRVdisc = 85,

#endregion LaserDisc based, types 80 to 89

#region MiniDisc based, types 90 to 99

    /// <summary>Sony Hi-MD</summary>
    [Description("Hi-MD")]
    HiMD = 90,
    /// <summary>Sony MiniDisc</summary>
    [Description("MiniDisc")]
    MD = 91,
    /// <summary>Sony MD-Data</summary>
    [Description("MD-Data")]
    MDData = 92,
    /// <summary>Sony MD-Data2</summary>
    [Description("MD-Data2")]
    MDData2 = 93,
    /// <summary>Sony MiniDisc, 60 minutes, formatted with Hi-MD format</summary>
    [Description("MiniDisc (60 minutes, Hi-MD format)")]
    MD60 = 94,
    /// <summary>Sony MiniDisc, 74 minutes, formatted with Hi-MD format</summary>
    [Description("MiniDisc (74 minutes, Hi-MD format")]
    MD74 = 95,
    /// <summary>Sony MiniDisc, 80 minutes, formatted with Hi-MD format</summary>
    [Description("MiniDisc (80 minutes, Hi-MD format)")]
    MD80 = 96,

#endregion MiniDisc based, types 90 to 99

#region Plasmon UDO, types 100 to 109

    /// <summary>5.25", Phase-Change, 1834348 sectors, 8192 bytes/sector, Ultra Density Optical, ECMA-350, ISO 17345</summary>
    [Description("UDO")]
    UDO = 100,
    /// <summary>5.25", Phase-Change, 3669724 sectors, 8192 bytes/sector, Ultra Density Optical 2, ECMA-380, ISO 11976</summary>
    [Description("UDO 2")]
    UDO2 = 101,
    /// <summary>5.25", Write-Once, 3668759 sectors, 8192 bytes/sector, Ultra Density Optical 2, ECMA-380, ISO 11976</summary>
    [Description("UDO 2 (WORM)")]
    UDO2_WORM = 102,

#endregion Plasmon UDO, types 100 to 109

#region Sony game media, types 110 to 129

    [Description("PlayStation Memory Card")]
    PlayStationMemoryCard = 110,
    [Description("PlayStation 2 Memory Card")]
    PlayStationMemoryCard2 = 111,
    /// <summary>Sony PlayStation game CD</summary>
    [Description("PlayStation game")]
    PS1CD = 112,
    /// <summary>Sony PlayStation 2 game CD</summary>
    [Description("PlayStation 2 game CD")]
    PS2CD = 113,
    /// <summary>Sony PlayStation 2 game DVD</summary>
    [Description("PlayStation 2 game DVD")]
    PS2DVD = 114,
    /// <summary>Sony PlayStation 3 game DVD</summary>
    [Description("PlayStation 3 game DVD")]
    PS3DVD = 115,
    /// <summary>Sony PlayStation 3 game Blu-ray</summary>
    [Description("PlayStation 3 game Blu-ray")]
    PS3BD = 116,
    /// <summary>Sony PlayStation 4 game Blu-ray</summary>
    [Description("PlayStation 4 game")]
    PS4BD = 117,
    /// <summary>Sony PlayStation Portable Universal Media Disc (ECMA-365)</summary>
    [Description("PlayStation Portable UMD")]
    UMD = 118,
    [Description("PlayStation Vita game card")]
    PlayStationVitaGameCard = 119,
    /// <summary>Sony PlayStation 5 game Ultra HD Blu-ray</summary>
    [Description("PlayStation 5 game")]
    PS5BD = 120,

#endregion Sony game media, types 110 to 129

#region Microsoft game media, types 130 to 149

    /// <summary>Microsoft X-box Game Disc</summary>
    [Description("X-box Game Disc")]
    XGD = 130,
    /// <summary>Microsoft X-box 360 Game Disc</summary>
    [Description("X-box 360 Game Disc")]
    XGD2 = 131,
    /// <summary>Microsoft X-box 360 Game Disc</summary>
    [Description("X-box 360 Game Disc (XGD3)")]
    XGD3 = 132,
    /// <summary>Microsoft X-box One Game Disc</summary>
    [Description("X-box One/Series Game Disc")]
    XGD4 = 133,

#endregion Microsoft game media, types 130 to 149

#region Sega game media, types 150 to 169

    /// <summary>Sega MegaCD</summary>
    [Description("Sega Mega CD game")]
    MEGACD = 150,
    /// <summary>Sega Saturn disc</summary>
    [Description("Sega Saturn game")]
    SATURNCD = 151,
    /// <summary>Sega/Yamaha Gigabyte Disc</summary>
    [Description("Sega/Yamaha Gigabyte disc")]
    GDROM = 152,
    /// <summary>Sega/Yamaha recordable Gigabyte Disc</summary>
    [Description("Sega/Yamaha recordable Gigabyte disc")]
    GDR = 153,
    [Description("Sega card")]
    SegaCard = 154,
    [Description("MilCD")]
    MilCD = 155,
    [Description("Sega Genesis / Mega Drive cartridge")]
    MegaDriveCartridge = 156,
    [Description("Sega 32X cartridge")]
    _32XCartridge = 157,
    [Description("Sega Pico cartridge")]
    SegaPicoCartridge = 158,
    [Description("Sega Master System cartridge")]
    MasterSystemCartridge = 159,
    [Description("Sega Game Gear cartridge")]
    GameGearCartridge = 160,
    [Description("Sega Saturn cartridge")]
    SegaSaturnCartridge = 161,

#endregion Sega game media, types 150 to 169

#region Other game media, types 170 to 179

    /// <summary>PC-Engine / TurboGrafx cartridge</summary>
    [Description("HuCard")]
    HuCard = 170,
    /// <summary>PC-Engine / TurboGrafx CD</summary>
    [Description("Super CD-ROM²")]
    SuperCDROM2 = 171,
    /// <summary>Atari Jaguar CD</summary>
    [Description("Atari Jaguar CD")]
    JaguarCD = 172,
    /// <summary>3DO CD</summary>
    [Description("3DO game")]
    ThreeDO = 173,
    /// <summary>NEC PC-FX</summary>
    [Description("PC-FX game")]
    PCFX = 174,
    /// <summary>NEO-GEO CD</summary>
    [Description("NeoGeo CD game")]
    NeoGeoCD = 175,
    /// <summary>Commodore CDTV</summary>
    [Description("Commodore CDTV disc")]
    CDTV = 176,
    /// <summary>Amiga CD32</summary>
    [Description("Amiga CD32 game")]
    CD32 = 177,
    /// <summary>Nuon (DVD based videogame console)</summary>
    [Description("Nuon game")]
    Nuon = 178,
    /// <summary>Bandai Playdia</summary>
    [Description("Playdia game")]
    Playdia = 179,

#endregion Other game media, types 170 to 179

#region Apple standard floppy format, types 180 to 189

    /// <summary>5.25", SS, DD, 35 tracks, 13 spt, 256 bytes/sector, GCR</summary>
    [Description("Apple DOS 3.2 single-sided floppy")]
    Apple32SS = 180,
    /// <summary>5.25", DS, DD, 35 tracks, 13 spt, 256 bytes/sector, GCR</summary>
    [Description("Apple DOS 3.2 double-sided floppy")]
    Apple32DS = 181,
    /// <summary>5.25", SS, DD, 35 tracks, 16 spt, 256 bytes/sector, GCR</summary>
    [Description("Apple DOS 3.3 single-sided floppy")]
    Apple33SS = 182,
    /// <summary>5.25", DS, DD, 35 tracks, 16 spt, 256 bytes/sector, GCR</summary>
    [Description("Apple DOS 3.3 double-sided floppy")]
    Apple33DS = 183,
    /// <summary>3.5", SS, DD, 80 tracks, 8 to 12 spt, 512 bytes/sector, GCR</summary>
    [Description("Apple Sony single-sided floppy")]
    AppleSonySS = 184,
    /// <summary>3.5", DS, DD, 80 tracks, 8 to 12 spt, 512 bytes/sector, GCR</summary>
    [Description("Apple Sony double-sided floppy")]
    AppleSonyDS = 185,
    /// <summary>5.25", DS, ?D, ?? tracks, ?? spt, 512 bytes/sector, GCR, opposite side heads, aka Twiggy</summary>
    [Description("Apple FileWare floppy")]
    AppleFileWare = 186,

#endregion Apple standard floppy format

#region IBM/Microsoft PC floppy formats, types 190 to 209

    /// <summary>5.25", SS, DD, 40 tracks, 8 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 5.25\" single-sided floppy (8 sectors)")]
    DOS_525_SS_DD_8 = 190,
    /// <summary>5.25", SS, DD, 40 tracks, 9 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 5.25\" single-sided floppy (9 sectors)")]
    DOS_525_SS_DD_9 = 191,
    /// <summary>5.25", DS, DD, 40 tracks, 8 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 5.25\" double-sided floppy (8 sectors)")]
    DOS_525_DS_DD_8 = 192,
    /// <summary>5.25", DS, DD, 40 tracks, 9 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 5.25\" double-sided floppy (9 sectors)")]
    DOS_525_DS_DD_9 = 193,
    /// <summary>5.25", DS, HD, 80 tracks, 15 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 5.25\" high density floppy")]
    DOS_525_HD = 194,
    /// <summary>3.5", SS, DD, 80 tracks, 8 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 3.5\" single-sided floppy (8 sectors)")]
    DOS_35_SS_DD_8 = 195,
    /// <summary>3.5", SS, DD, 80 tracks, 9 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 3.5\" single-sided floppy (9 sectors)")]
    DOS_35_SS_DD_9 = 196,
    /// <summary>3.5", DS, DD, 80 tracks, 8 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 3.5\" double-sided floppy (8 sectors)")]
    DOS_35_DS_DD_8 = 197,
    /// <summary>3.5", DS, DD, 80 tracks, 9 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 3.5\" double-sided floppy (9 sectors)")]
    DOS_35_DS_DD_9 = 198,
    /// <summary>3.5", DS, HD, 80 tracks, 18 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 3.5\" high density floppy")]
    DOS_35_HD = 199,
    /// <summary>3.5", DS, ED, 80 tracks, 36 spt, 512 bytes/sector, MFM</summary>
    [Description("IBM PC 3.5\" extended density floppy")]
    DOS_35_ED = 200,
    /// <summary>3.5", DS, HD, 80 tracks, 21 spt, 512 bytes/sector, MFM</summary>
    [Description("Microsoft 3.5\" DMF floppy")]
    DMF = 201,
    /// <summary>3.5", DS, HD, 82 tracks, 21 spt, 512 bytes/sector, MFM</summary>
    [Description("Microsoft 3.5\" DMF floppy (82 tracks)")]
    DMF_82 = 202,
    /// <summary>
    ///     5.25", DS, HD, 80 tracks, ? spt, ??? + ??? + ??? bytes/sector, MFM track 0 = ??15 sectors, 512 bytes/sector,
    ///     falsified to DOS as 19 spt, 512 bps
    /// </summary>
    [Description("IBM 5.25\" XDF floppy")]
    XDF_525 = 203,
    /// <summary>
    ///     3.5", DS, HD, 80 tracks, 4 spt, 8192 + 2048 + 1024 + 512 bytes/sector, MFM track 0 = 19 sectors, 512
    ///     bytes/sector, falsified to DOS as 23 spt, 512 bps
    /// </summary>
    [Description("IBM 3.5\" XDF floppy")]
    XDF_35 = 204,

#endregion IBM/Microsoft PC standard floppy formats, types 190 to 209

#region IBM standard floppy formats, types 210 to 219

    /// <summary>8", SS, SD, 32 tracks, 8 spt, 319 bytes/sector, FM</summary>
    [Description("IBM 23FD floppy")]
    IBM23FD = 210,
    /// <summary>8", SS, SD, 73 tracks, 26 spt, 128 bytes/sector, FM</summary>
    [Description("IBM 33FD floppy (128bps)")]
    IBM33FD_128 = 211,
    /// <summary>8", SS, SD, 74 tracks, 15 spt, 256 bytes/sector, FM, track 0 = 26 sectors, 128 bytes/sector</summary>
    [Description("IBM 33FD floppy (256bps)")]
    IBM33FD_256 = 212,
    /// <summary>8", SS, SD, 74 tracks, 8 spt, 512 bytes/sector, FM, track 0 = 26 sectors, 128 bytes/sector</summary>
    [Description("IBM 33FD floppy (512bps)")]
    IBM33FD_512 = 213,
    /// <summary>8", DS, SD, 74 tracks, 26 spt, 128 bytes/sector, FM, track 0 = 26 sectors, 128 bytes/sector</summary>
    [Description("IBM 43FD floppy (128bps)")]
    IBM43FD_128 = 214,
    /// <summary>8", DS, SD, 74 tracks, 26 spt, 256 bytes/sector, FM, track 0 = 26 sectors, 128 bytes/sector</summary>
    [Description("IBM 43FD floppy (256bps)")]
    IBM43FD_256 = 215,
    /// <summary>
    ///     8", DS, DD, 74 tracks, 26 spt, 256 bytes/sector, MFM, track 0 side 0 = 26 sectors, 128 bytes/sector, track 0
    ///     side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    [Description("IBM 53FD floppy (256bps)")]
    IBM53FD_256 = 216,
    /// <summary>
    ///     8", DS, DD, 74 tracks, 15 spt, 512 bytes/sector, MFM, track 0 side 0 = 26 sectors, 128 bytes/sector, track 0
    ///     side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    [Description("IBM 43FD floppy (512bps)")]
    IBM53FD_512 = 217,
    /// <summary>
    ///     8", DS, DD, 74 tracks, 8 spt, 1024 bytes/sector, MFM, track 0 side 0 = 26 sectors, 128 bytes/sector, track 0
    ///     side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    [Description("IBM 43FD floppy (1024bps)")]
    IBM53FD_1024 = 218,

#endregion IBM standard floppy formats, types 210 to 219

#region DEC standard floppy formats, types 220 to 229

    /// <summary>8", SS, DD, 77 tracks, 26 spt, 128 bytes/sector, FM</summary>
    [Description("DEC RX01 floppy")]
    RX01 = 220,
    /// <summary>8", SS, DD, 77 tracks, 26 spt, 256 bytes/sector, FM/MFM</summary>
    [Description("DEC RX02 floppy")]
    RX02 = 221,
    /// <summary>8", DS, DD, 77 tracks, 26 spt, 256 bytes/sector, FM/MFM</summary>
    [Description("DEC RX03 floppy")]
    RX03 = 222,
    /// <summary>5.25", SS, DD, 80 tracks, 10 spt, 512 bytes/sector, MFM</summary>
    [Description("DEC RX50 floppy")]
    RX50 = 223,

#endregion DEC standard floppy formats, types 220 to 229

#region Acorn standard floppy formats, types 230 to 239

    /// <summary>5,25", SS, SD, 40 tracks, 10 spt, 256 bytes/sector, FM</summary>
    [Description("Acorn 5.25\" single density (40 tracks) floppy")]
    ACORN_525_SS_SD_40 = 230,
    /// <summary>5,25", SS, SD, 80 tracks, 10 spt, 256 bytes/sector, FM</summary>
    [Description("Acorn 5.25\" single density (80 tracks) floppy")]
    ACORN_525_SS_SD_80 = 231,
    /// <summary>5,25", SS, DD, 40 tracks, 16 spt, 256 bytes/sector, MFM</summary>
    [Description("Acorn 5.25\" double density (40 tracks) floppy")]
    ACORN_525_SS_DD_40 = 232,
    /// <summary>5,25", SS, DD, 80 tracks, 16 spt, 256 bytes/sector, MFM</summary>
    [Description("Acorn 5.25\" double density (80 tracks) floppy")]
    ACORN_525_SS_DD_80 = 233,
    /// <summary>5,25", DS, DD, 80 tracks, 16 spt, 256 bytes/sector, MFM</summary>
    [Description("Acorn 5.25\" double sided double density (40 tracks) floppy")]
    ACORN_525_DS_DD = 234,
    /// <summary>3,5", DS, DD, 80 tracks, 5 spt, 1024 bytes/sector, MFM</summary>
    [Description("Acorn 3.5\" double density floppy")]
    ACORN_35_DS_DD = 235,
    /// <summary>3,5", DS, HD, 80 tracks, 10 spt, 1024 bytes/sector, MFM</summary>
    [Description("Acorn 3.5\" high density floppy")]
    ACORN_35_DS_HD = 236,

#endregion Acorn standard floppy formats, types 230 to 239

#region Atari standard floppy formats, types 240 to 249

    /// <summary>5,25", SS, SD, 40 tracks, 18 spt, 128 bytes/sector, FM</summary>
    [Description("Atari 5.25\" single density floppy")]
    ATARI_525_SD = 240,
    /// <summary>5,25", SS, ED, 40 tracks, 26 spt, 128 bytes/sector, MFM</summary>
    [Description("Atari 5.25\" extended density floppy")]
    ATARI_525_ED = 241,
    /// <summary>5,25", SS, DD, 40 tracks, 18 spt, 256 bytes/sector, MFM</summary>
    [Description("Atari 5.25\" double density floppy")]
    ATARI_525_DD = 242,
    /// <summary>3,5", SS, DD, 80 tracks, 10 spt, 512 bytes/sector, MFM</summary>
    [Description("Atari 3.5\" single sided (10 spt) floppy")]
    ATARI_35_SS_DD = 243,
    /// <summary>3,5", DS, DD, 80 tracks, 10 spt, 512 bytes/sector, MFM</summary>
    [Description("Atari 3.5\" double sided (10 spt) floppy")]
    ATARI_35_DS_DD = 244,
    /// <summary>3,5", SS, DD, 80 tracks, 11 spt, 512 bytes/sector, MFM</summary>
    [Description("Atari 3.5\" single sided (11 spt) floppy")]
    ATARI_35_SS_DD_11 = 245,
    /// <summary>3,5", DS, DD, 80 tracks, 11 spt, 512 bytes/sector, MFM</summary>
    [Description("Atari 3.5\" double sided (11 spt) floppy")]
    ATARI_35_DS_DD_11 = 246,

#endregion Atari standard floppy formats, types 240 to 249

#region Commodore standard floppy formats, types 250 to 259

    /// <summary>3,5", DS, DD, 80 tracks, 10 spt, 512 bytes/sector, MFM (1581)</summary>
    [Description("Commodore 3.5\" double density floppy")]
    CBM_35_DD = 250,
    /// <summary>3,5", DS, DD, 80 tracks, 11 spt, 512 bytes/sector, MFM (Amiga)</summary>
    [Description("Amiga 3.5\" double density floppy")]
    CBM_AMIGA_35_DD = 251,
    /// <summary>3,5", DS, HD, 80 tracks, 22 spt, 512 bytes/sector, MFM (Amiga)</summary>
    [Description("Amiga 3.5\" high density floppy")]
    CBM_AMIGA_35_HD = 252,
    /// <summary>5,25", SS, DD, 35 tracks, GCR</summary>
    [Description("Commodore 1540 floppy")]
    CBM_1540 = 253,
    /// <summary>5,25", SS, DD, 40 tracks, GCR</summary>
    [Description("Commodore 1540 extended floppy")]
    CBM_1540_Ext = 254,
    /// <summary>5,25", DS, DD, 35 tracks, GCR</summary>
    [Description("Commodore 1571 floppy")]
    CBM_1571 = 255,

#endregion Commodore standard floppy formats, types 250 to 259

#region NEC/SHARP standard floppy formats, types 260 to 269

    /// <summary>8", DS, SD, 77 tracks, 26 spt, 128 bytes/sector, FM</summary>
    [Description("NEC 8\" floppy (128 bps)")]
    NEC_8_SD = 260,
    /// <summary>8", DS, DD, 77 tracks, 26 spt, 256 bytes/sector, MFM</summary>
    [Description("NEC 8\" floppy (256 bps)")]
    NEC_8_DD = 261,
    /// <summary>5.25", SS, SD, 80 tracks, 16 spt, 256 bytes/sector, FM</summary>
    [Description("NEC 5.25\" single sided floppy")]
    NEC_525_SS = 262,
    /// <summary>5.25", DS, SD, 80 tracks, 16 spt, 256 bytes/sector, MFM</summary>
    [Description("NEC 5.25\" double sided floppy")]
    NEC_525_DS = 263,
    /// <summary>5,25", DS, HD, 77 tracks, 8 spt, 1024 bytes/sector, MFM</summary>
    [Description("NEC 5.25\" high density floppy")]
    NEC_525_HD = 264,
    /// <summary>3,5", DS, HD, 77 tracks, 8 spt, 1024 bytes/sector, MFM, aka mode 3</summary>
    [Description("NEC 3.5\" floppy")]
    NEC_35_HD_8 = 265,
    /// <summary>3,5", DS, HD, 80 tracks, 15 spt, 512 bytes/sector, MFM</summary>
    [Description("NEC 3.5\" floppy (80 tracks)")]
    NEC_35_HD_15 = 266,
    /// <summary>3,5", DS, TD, 240 tracks, 38 spt, 512 bytes/sector, MFM</summary>
    [Description("NEC 3.5\" triple density floppy")]
    NEC_35_TD = 267,
    /// <summary>5,25", DS, HD, 77 tracks, 8 spt, 1024 bytes/sector, MFM</summary>
    SHARP_525 = NEC_525_HD,
    /// <summary>3,5", DS, HD, 80 tracks, 9 spt, 1024 bytes/sector, MFM</summary>
    [Description("Sharp 5.25\" floppy")]
    SHARP_525_9 = 268,
    /// <summary>3,5", DS, HD, 77 tracks, 8 spt, 1024 bytes/sector, MFM</summary>
    SHARP_35 = NEC_35_HD_8,
    /// <summary>3,5", DS, HD, 80 tracks, 9 spt, 1024 bytes/sector, MFM</summary>
    [Description("Sharp 3.5\" floppy")]
    SHARP_35_9 = 269,

#endregion NEC/SHARP standard floppy formats, types 260 to 269

#region ECMA floppy standards, types 270 to 289

    /// <summary>
    ///     5,25", DS, DD, 80 tracks, 8 spt, 1024 bytes/sector, MFM, track 0 side 0 = 26 sectors, 128 bytes/sector, track
    ///     0 side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    ECMA_99_8 = 270,
    /// <summary>
    ///     5,25", DS, DD, 77 tracks, 15 spt, 512 bytes/sector, MFM, track 0 side 0 = 26 sectors, 128 bytes/sector, track
    ///     0 side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    ECMA_99_15 = 271,
    /// <summary>
    ///     5,25", DS, DD, 77 tracks, 26 spt, 256 bytes/sector, MFM, track 0 side 0 = 26 sectors, 128 bytes/sector, track
    ///     0 side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    ECMA_99_26 = 272,
    /// <summary>3,5", DS, DD, 80 tracks, 9 spt, 512 bytes/sector, MFM</summary>
    ECMA_100 = DOS_35_DS_DD_9,
    /// <summary>3,5", DS, HD, 80 tracks, 18 spt, 512 bytes/sector, MFM</summary>
    ECMA_125 = DOS_35_HD,
    /// <summary>3,5", DS, ED, 80 tracks, 36 spt, 512 bytes/sector, MFM</summary>
    ECMA_147 = DOS_35_ED,
    /// <summary>8", SS, SD, 77 tracks, 26 spt, 128 bytes/sector, FM</summary>
    ECMA_54 = 273,
    /// <summary>8", DS, SD, 77 tracks, 26 spt, 128 bytes/sector, FM</summary>
    ECMA_59 = 274,
    /// <summary>5,25", SS, DD, 35 tracks, 9 spt, 256 bytes/sector, FM, track 0 side 0 = 16 sectors, 128 bytes/sector</summary>
    ECMA_66 = 275,
    /// <summary>
    ///     8", DS, DD, 77 tracks, 8 spt, 1024 bytes/sector, FM, track 0 side 0 = 26 sectors, 128 bytes/sector, track 0
    ///     side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    ECMA_69_8 = 276,
    /// <summary>
    ///     8", DS, DD, 77 tracks, 15 spt, 512 bytes/sector, FM, track 0 side 0 = 26 sectors, 128 bytes/sector, track 0
    ///     side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    ECMA_69_15 = 277,
    /// <summary>
    ///     8", DS, DD, 77 tracks, 26 spt, 256 bytes/sector, FM, track 0 side 0 = 26 sectors, 128 bytes/sector, track 0
    ///     side 1 = 26 sectors, 256 bytes/sector
    /// </summary>
    ECMA_69_26 = 278,
    /// <summary>
    ///     5,25", DS, DD, 40 tracks, 16 spt, 256 bytes/sector, FM, track 0 side 0 = 16 sectors, 128 bytes/sector, track 0
    ///     side 1 = 16 sectors, 256 bytes/sector
    /// </summary>
    ECMA_70 = 279,
    /// <summary>
    ///     5,25", DS, DD, 80 tracks, 16 spt, 256 bytes/sector, FM, track 0 side 0 = 16 sectors, 128 bytes/sector, track 0
    ///     side 1 = 16 sectors, 256 bytes/sector
    /// </summary>
    ECMA_78 = 280,
    /// <summary>5,25", DS, DD, 80 tracks, 9 spt, 512 bytes/sector, FM</summary>
    ECMA_78_2 = 281,

#endregion ECMA floppy standards, types 270 to 289

#region Non-standard PC formats (FDFORMAT, 2M, etc), types 290 to 308

    /// <summary>5,25", DS, DD, 82 tracks, 10 spt, 512 bytes/sector, MFM</summary>
    [Description("FDFORMAT 5.25\" double density floppy")]
    FDFORMAT_525_DD = 290,
    /// <summary>5,25", DS, HD, 82 tracks, 17 spt, 512 bytes/sector, MFM</summary>
    [Description("FDFORMAT 5.25\" high density floppy")]
    FDFORMAT_525_HD = 291,
    /// <summary>3,5", DS, DD, 82 tracks, 10 spt, 512 bytes/sector, MFM</summary>
    [Description("FDFORMAT 3.5\" double density floppy")]
    FDFORMAT_35_DD = 292,
    /// <summary>3,5", DS, HD, 82 tracks, 21 spt, 512 bytes/sector, MFM</summary>
    [Description("FDFORMAT 3.5\" high density floppy")]
    FDFORMAT_35_HD = 293,

#endregion Non-standard PC formats (FDFORMAT, 2M, etc), types 290 to 308

#region Apricot ACT standard floppy formats, type 309

    /// <summary>3.5", DS, DD, 70 tracks, 9 spt, 512 bytes/sector, MFM</summary>
    [Description("Apricot ACT 3.5\" floppy")]
    Apricot_35 = 309,

#endregion Apricot ACT standard floppy formats, type 309

#region OnStream ADR, types 310 to 319

    [Description("ADR-2120")]
    ADR2120 = 310,
    [Description("ADR-260")]
    ADR260 = 311,
    [Description("ADR-30")]
    ADR30 = 312,
    [Description("ADR-50")]
    ADR50 = 313,

#endregion OnStream ADR, types 310 to 319

#region Advanced Intelligent Tape, types 320 to 339

    [Description("AIT")]
    AIT1 = 320,
    [Description("AIT Turbo")]
    AIT1Turbo = 321,
    [Description("AIT-2")]
    AIT2 = 322,
    [Description("AIT-2 Turbo")]
    AIT2Turbo = 323,
    [Description("AIT-3")]
    AIT3 = 324,
    [Description("AIT-3 Ex")]
    AIT3Ex = 325,
    [Description("AIT-3 Turbo")]
    AIT3Turbo = 326,
    [Description("AIT-4")]
    AIT4 = 327,
    [Description("AIT-5")]
    AIT5 = 328,
    [Description("AIT-E Turbo")]
    AITETurbo = 329,
    [Description("Super AIT")]
    SAIT1 = 330,
    [Description("Super AIT-2")]
    SAIT2 = 331,

#endregion Advanced Intelligent Tape, types 320 to 339

#region Iomega, types 340 to 359

    /// <summary>Obsolete type for 8"x11" Bernoulli Box disk</summary>
    [Obsolete]
    [Description("Bernoulli Box")]
    Bernoulli = 340,
    /// <summary>Obsolete type for 5⅓" Bernoulli Box II disks</summary>
    [Obsolete]
    [Description("Bernoulli Box II")]
    Bernoulli2 = 341,
    [Description("Ditto")]
    Ditto = 342,
    [Description("Ditto MAX")]
    DittoMax = 343,
    [Description("Jaz")]
    Jaz = 344,
    [Description("Jaz 2")]
    Jaz2 = 345,
    [Description("PocketZip / Clik!")]
    PocketZip = 346,
    [Description("REV (120Gb)")]
    REV120 = 347,
    [Description("REV (35Gb)")]
    REV35 = 348,
    [Description("REV (70Gb)")]
    REV70 = 349,
    [Description("ZIP (100Mb)")]
    ZIP100 = 350,
    [Description("ZIP (250Mb)")]
    ZIP250 = 351,
    [Description("ZIP (750Mb)")]
    ZIP750 = 352,
    /// <summary>5⅓" Bernoulli Box II disk with 35Mb capacity</summary>
    [Description("Bernoulli Box II (35Mb)")]
    Bernoulli35 = 353,
    /// <summary>5⅓" Bernoulli Box II disk with 44Mb capacity</summary>
    [Description("Bernoulli Box II (44Mb)")]
    Bernoulli44 = 354,
    /// <summary>5⅓" Bernoulli Box II disk with 65Mb capacity</summary>
    [Description("Bernoulli Box II (65Mb)")]
    Bernoulli65 = 355,
    /// <summary>5⅓" Bernoulli Box II disk with 90Mb capacity</summary>
    [Description("Bernoulli Box II (90Mb)")]
    Bernoulli90 = 356,
    /// <summary>5⅓" Bernoulli Box II disk with 105Mb capacity</summary>
    [Description("Bernoulli Box II (105Mb)")]
    Bernoulli105 = 357,
    /// <summary>5⅓" Bernoulli Box II disk with 150Mb capacity</summary>
    [Description("Bernoulli Box II (150Mb)")]
    Bernoulli150 = 358,
    /// <summary>5⅓" Bernoulli Box II disk with 230Mb capacity</summary>
    [Description("Bernoulli Box II (230Mb)")]
    Bernoulli230 = 359,

#endregion Iomega, types 340 to 359

#region Audio or video media, types 360 to 369

    CompactCassette = 360,
    Data8           = 361,
    MiniDV          = 362,
    /// <summary>D/CAS-25: Digital data on Compact Cassette form factor, special magnetic media, 9-track</summary>
    [Description("D/CAS-25")]
    Dcas25 = 363,
    /// <summary>D/CAS-85: Digital data on Compact Cassette form factor, special magnetic media, 17-track</summary>
    [Description("D/CAS-85")]
    Dcas85 = 364,
    /// <summary>D/CAS-103: Digital data on Compact Cassette form factor, special magnetic media, 21-track</summary>
    [Description("D/CAS-103")]
    Dcas103 = 365,

#endregion Audio media, types 360 to 369

#region CompactFlash Association, types 370 to 379

    CFast = 370,
    [Description("CompactFlash")]
    CompactFlash = 371,
    [Description("CompactFlash Type 2")]
    CompactFlashType2 = 372,

#endregion CompactFlash Association, types 370 to 379

#region Digital Audio Tape / Digital Data Storage, types 380 to 389

    [Description("Digital Audio Tape")]
    DigitalAudioTape = 380,
    [Description("DAT-160")]
    DAT160 = 381,
    [Description("DAT-320")]
    DAT320 = 382,
    [Description("DAT-72")]
    DAT72 = 383,
    [Description("DDS")]
    DDS1 = 384,
    [Description("DDS-2")]
    DDS2 = 385,
    [Description("DDS-3")]
    DDS3 = 386,
    [Description("DDS-4")]
    DDS4 = 387,

#endregion Digital Audio Tape / Digital Data Storage, types 380 to 389

#region DEC, types 390 to 399

    [Description("CompactTapeI")]
    CompactTapeI = 390,
    [Description("CompactTapeII")]
    CompactTapeII = 391,
    [Description("DECtapeII")]
    DECtapeII = 392,
    [Description("DLTtapeIII")]
    DLTtapeIII = 393,
    [Description("DLTtapeIIIxt")]
    DLTtapeIIIxt = 394,
    [Description("DLTtapeIV")]
    DLTtapeIV = 395,
    [Description("DLTtapeS4")]
    DLTtapeS4 = 396,
    [Description("Super DLT")]
    SDLT1 = 397,
    [Description("Super DLT 2")]
    SDLT2 = 398,
    [Description("VStapeI")]
    VStapeI = 399,

#endregion DEC, types 390 to 399

#region Exatape, types 400 to 419

    Exatape15m    = 400,
    Exatape22m    = 401,
    Exatape22mAME = 402,
    Exatape28m    = 403,
    Exatape40m    = 404,
    Exatape45m    = 405,
    Exatape54m    = 406,
    Exatape75m    = 407,
    Exatape76m    = 408,
    Exatape80m    = 409,
    Exatape106m   = 410,
    Exatape160mXL = 411,
    Exatape112m   = 412,
    Exatape125m   = 413,
    Exatape150m   = 414,
    Exatape170m   = 415,
    Exatape225m   = 416,

#endregion Exatape, types 400 to 419

#region PCMCIA / ExpressCard, types 420 to 429

    ExpressCard34 = 420,
    ExpressCard54 = 421,
    PCCardTypeI   = 422,
    PCCardTypeII  = 423,
    PCCardTypeIII = 424,
    PCCardTypeIV  = 425,

#endregion PCMCIA / ExpressCard, types 420 to 429

#region SyQuest, types 430 to 449

    /// <summary>SyQuest 135Mb cartridge for use in EZ135 and EZFlyer drives</summary>
    EZ135 = 430,
    /// <summary>SyQuest EZFlyer 230Mb cartridge for use in EZFlyer drive</summary>
    EZ230 = 431,
    /// <summary>SyQuest 4.7Gb for use in Quest drive</summary>
    Quest = 432,
    /// <summary>SyQuest SparQ 1Gb cartridge</summary>
    SparQ = 433,
    /// <summary>
    ///     SyQuest 5Mb, 3.9&quot;, 306 tracks, 2 sides, 32 sectors per track, 256 bytes/sector cartridge for SQ306RD
    ///     drive
    /// </summary>
    SQ100 = 434,
    /// <summary>
    ///     SyQuest 10Mb, 3.9&quot;, 615 tracks, 2 sides, 32 sectors per track, 256 bytes/sector cartridge for SQ312RD
    ///     drive
    /// </summary>
    SQ200 = 435,
    /// <summary>SyQuest 15Mb cartridge for SQ319RD drive</summary>
    SQ300 = 436,
    /// <summary>SyQuest 105Mb cartridge for SQ3105 and SQ3270 drives</summary>
    SQ310 = 437,
    /// <summary>SyQuest 270Mb cartridge for SQ3270 drive</summary>
    SQ327 = 438,
    /// <summary>SyQuest 44Mb cartridge for SQ555, SQ5110 and SQ5200C/SQ200 drives</summary>
    SQ400 = 439,
    /// <summary>SyQuest 88Mb cartridge for SQ5110 and SQ5200C/SQ200 drives</summary>
    SQ800 = 440,
    /// <summary>SyQuest 1.5Gb cartridge for SyJet drive</summary>
    [Obsolete]
    SQ1500 = 441,
    /// <summary>SyQuest 200Mb cartridge for use in SQ5200C drive</summary>
    SQ2000 = 442,
    /// <summary>SyQuest 1.5Gb cartridge for SyJet drive</summary>
    SyJet = 443,

#endregion SyQuest, types 430 to 449

#region Nintendo, types 450 to 469

    FamicomGamePak        = 450,
    GameBoyAdvanceGamePak = 451,
    GameBoyGamePak        = 452,
    /// <summary>Nintendo GameCube Optical Disc</summary>
    [Description("Nintendo GameCube Optical Disc")]
    GOD = 453,
    [Description("Nintendo 64 DD")]
    N64DD = 454,
    [Description("Nintendo 64 Game Pak")]
    N64GamePak = 455,
    [Description("NES Game Pak")]
    NESGamePak = 456,
    [Description("Nintendo 3DS Game Card")]
    Nintendo3DSGameCard = 457,
    [Description("Nintendo Disk Card")]
    NintendoDiskCard = 458,
    [Description("Nintendo DS Game Card")]
    NintendoDSGameCard = 459,
    [Description("Nintendo DSi Game Card")]
    NintendoDSiGameCard = 460,
    [Description("SNES Game Pak")]
    SNESGamePak = 461,
    [Description("SNES Game Pak (US)")]
    SNESGamePakUS = 462,
    /// <summary>Nintendo Wii Optical Disc</summary>
    [Description("Nintendo Wii Optical Disc")]
    WOD = 463,
    /// <summary>Nintendo Wii U Optical Disc</summary>
    [Description("Nintendo Wii U Optical Disc")]
    WUOD = 464,
    [Description("Switch Game Card")]
    SwitchGameCard = 465,

#endregion Nintendo, types 450 to 469

#region IBM Tapes, types 470 to 479

    IBM3470  = 470,
    IBM3480  = 471,
    IBM3490  = 472,
    IBM3490E = 473,
    IBM3592  = 474,

#endregion IBM Tapes, types 470 to 479

#region LTO Ultrium, types 480 to 509

    [Description("LTO")]
    LTO = 480,
    [Description("LTO-2")]
    LTO2 = 481,
    [Description("LTO-3")]
    LTO3 = 482,
    [Description("LTO-3 (WORM)")]
    LTO3WORM = 483,
    [Description("LTO-4")]
    LTO4 = 484,
    [Description("LTO-4 (WORM)")]
    LTO4WORM = 485,
    [Description("LTO-5")]
    LTO5 = 486,
    [Description("LTO-5 (WORM)")]
    LTO5WORM = 487,
    [Description("LTO-6")]
    LTO6 = 488,
    [Description("LTO-6 (WORM)")]
    LTO6WORM = 489,
    [Description("LTO-7")]
    LTO7 = 490,
    [Description("LTO-7 (WORM)")]
    LTO7WORM = 491,

#endregion LTO Ultrium, types 480 to 509

#region MemoryStick, types 510 to 519

    [Description("MemoryStick")]
    MemoryStick = 510,
    [Description("MemoryStick Duo")]
    MemoryStickDuo = 511,
    [Description("MemoryStick Micro")]
    MemoryStickMicro = 512,
    [Description("MemoryStick Pro")]
    MemoryStickPro = 513,
    [Description("MemoryStick Pro Duo")]
    MemoryStickProDuo = 514,

#endregion MemoryStick, types 510 to 519

#region SecureDigital, types 520 to 529

    [Description("microSD")]
    microSD = 520,
    [Description("miniSD")]
    miniSD = 521,
    [Description("SecureDigital")]
    SecureDigital = 522,

#endregion SecureDigital, types 520 to 529

#region MultiMediaCard, types 530 to 539

    [Description("MultiMediaCard")]
    MMC = 530,
    [Description("MMCmicro")]
    MMCmicro = 531,
    [Description("RS-MMC")]
    RSMMC = 532,
    [Description("MMC+")]
    MMCplus = 533,
    [Description("MMCmobile")]
    MMCmobile = 534,

#endregion MultiMediaCard, types 530 to 539

#region SLR, types 540 to 569

    MLR1        = 540,
    MLR1SL      = 541,
    MLR3        = 542,
    SLR1        = 543,
    SLR2        = 544,
    SLR3        = 545,
    SLR32       = 546,
    SLR32SL     = 547,
    SLR4        = 548,
    SLR5        = 549,
    SLR5SL      = 550,
    SLR6        = 551,
    SLRtape7    = 552,
    SLRtape7SL  = 553,
    SLRtape24   = 554,
    SLRtape24SL = 555,
    SLRtape40   = 556,
    SLRtape50   = 557,
    SLRtape60   = 558,
    SLRtape75   = 559,
    SLRtape100  = 560,
    SLRtape140  = 561,

#endregion SLR, types 540 to 569

#region QIC, types 570 to 589

    [Description("QIC-11")]
    QIC11 = 570,
    [Description("QIC-120")]
    QIC120 = 571,
    [Description("QIC-1350")]
    QIC1350 = 572,
    [Description("QIC-150")]
    QIC150 = 573,
    [Description("QIC-24")]
    QIC24 = 574,
    [Description("QIC-3010")]
    QIC3010 = 575,
    [Description("QIC-3020")]
    QIC3020 = 576,
    [Description("QIC-3080")]
    QIC3080 = 577,
    [Description("QIC-3095")]
    QIC3095 = 578,
    [Description("QIC-320")]
    QIC320 = 579,
    [Description("QIC-40")]
    QIC40 = 580,
    [Description("QIC-525")]
    QIC525 = 581,
    [Description("QIC-80")]
    QIC80 = 582,

#endregion QIC, types 570 to 589

#region StorageTek tapes, types 590 to 609

    STK4480 = 590,
    STK4490 = 591,
    STK9490 = 592,
    T9840A  = 593,
    T9840B  = 594,
    T9840C  = 595,
    T9840D  = 596,
    T9940A  = 597,
    T9940B  = 598,
    T10000A = 599,
    T10000B = 600,
    T10000C = 601,
    T10000D = 602,

#endregion StorageTek tapes, types 590 to 609

#region Travan, types 610 to 619

    Travan    = 610,
    Travan1Ex = 611,
    Travan3   = 612,
    Travan3Ex = 613,
    Travan4   = 614,
    Travan5   = 615,
    Travan7   = 616,

#endregion Travan, types 610 to 619

#region VXA, types 620 to 629

    VXA1 = 620,
    VXA2 = 621,
    VXA3 = 622,

#endregion VXA, types 620 to 629

#region Magneto-optical, types 630 to 659

    /// <summary>5,25", M.O., WORM, 650Mb, 318750 sectors, 1024 bytes/sector, ECMA-153, ISO 11560</summary>
    ECMA_153 = 630,
    /// <summary>5,25", M.O., WORM, 600Mb, 581250 sectors, 512 bytes/sector, ECMA-153, ISO 11560</summary>
    ECMA_153_512 = 631,
    /// <summary>3,5", M.O., RW, 128Mb, 248826 sectors, 512 bytes/sector, ECMA-154, ISO 10090</summary>
    ECMA_154 = 632,
    /// <summary>5,25", M.O., RW/WORM, 1Gb, 904995 sectors, 512 bytes/sector, ECMA-183, ISO 13481</summary>
    ECMA_183_512 = 633,
    /// <summary>5,25", M.O., RW/WORM, 1Gb, 498526 sectors, 1024 bytes/sector, ECMA-183, ISO 13481</summary>
    ECMA_183 = 634,
    /// <summary>5,25", M.O., RW/WORM, 1.2Gb, 1165600 sectors, 512 bytes/sector, ECMA-184, ISO 13549</summary>
    ECMA_184_512 = 635,
    /// <summary>5,25", M.O., RW/WORM, 1.3Gb, 639200 sectors, 1024 bytes/sector, ECMA-184, ISO 13549</summary>
    ECMA_184 = 636,
    /// <summary>300mm, M.O., WORM, ??? sectors, 1024 bytes/sector, ECMA-189, ISO 13614</summary>
    ECMA_189 = 637,
    /// <summary>300mm, M.O., WORM, ??? sectors, 1024 bytes/sector, ECMA-190, ISO 13403</summary>
    ECMA_190 = 638,
    /// <summary>5,25", M.O., RW/WORM, 936921 or 948770 sectors, 1024 bytes/sector, ECMA-195, ISO 13842</summary>
    ECMA_195 = 639,
    /// <summary>5,25", M.O., RW/WORM, 1644581 or 1647371 sectors, 512 bytes/sector, ECMA-195, ISO 13842</summary>
    ECMA_195_512 = 640,
    /// <summary>3,5", M.O., 446325 sectors, 512 bytes/sector, ECMA-201, ISO 13963</summary>
    ECMA_201 = 641,
    /// <summary>3,5", M.O., 429975 sectors, 512 bytes/sector, embossed, ISO 13963</summary>
    ECMA_201_ROM = 642,
    /// <summary>3,5", M.O., 371371 sectors, 1024 bytes/sector, ECMA-223</summary>
    ECMA_223 = 643,
    /// <summary>3,5", M.O., 694929 sectors, 512 bytes/sector, ECMA-223</summary>
    ECMA_223_512 = 644,
    /// <summary>5,25", M.O., 1244621 sectors, 1024 bytes/sector, ECMA-238, ISO 15486</summary>
    ECMA_238 = 645,
    /// <summary>3,5", M.O., 310352, 320332 or 321100 sectors, 2048 bytes/sector, ECMA-239, ISO 15498</summary>
    ECMA_239 = 646,
    /// <summary>356mm, M.O., 14476734 sectors, 1024 bytes/sector, ECMA-260, ISO 15898</summary>
    ECMA_260 = 647,
    /// <summary>356mm, M.O., 24445990 sectors, 1024 bytes/sector, ECMA-260, ISO 15898</summary>
    ECMA_260_Double = 648,
    /// <summary>5,25", M.O., 1128134 sectors, 2048 bytes/sector, ECMA-280, ISO 18093</summary>
    ECMA_280 = 649,
    /// <summary>300mm, M.O., 7355716 sectors, 2048 bytes/sector, ECMA-317, ISO 20162</summary>
    ECMA_317 = 650,
    /// <summary>5,25", M.O., 1095840 sectors, 4096 bytes/sector, ECMA-322, ISO 22092, 9.1Gb/cart</summary>
    ECMA_322 = 651,
    /// <summary>5,25", M.O., 2043664 sectors, 2048 bytes/sector, ECMA-322, ISO 22092, 8.6Gb/cart</summary>
    ECMA_322_2k = 652,
    /// <summary>3,5", M.O., 605846 sectors, 2048 bytes/sector, Cherry Book, GigaMo, ECMA-351, ISO 17346</summary>
    GigaMo = 653,
    /// <summary>3,5", M.O., 1063146 sectors, 2048 bytes/sector, Cherry Book 2, GigaMo 2, ECMA-353, ISO 22533</summary>
    GigaMo2 = 654,
    /// <summary>5,25", M.O., 1263472 sectors, 2048 bytes/sector, ISO 15286, 5.2Gb/cart</summary>
    ISO_15286 = 655,
    /// <summary>5,25", M.O., 2319786 sectors, 1024 bytes/sector, ISO 15286, 4.8Gb/cart</summary>
    ISO_15286_1024 = 656,
    /// <summary>5,25", M.O., ??????? sectors, 512 bytes/sector, ISO 15286, 4.1Gb/cart</summary>
    ISO_15286_512 = 657,
    /// <summary>5,25", M.O., 314569 sectors, 1024 bytes/sector, ISO 10089, 650Mb/cart</summary>
    ISO_10089 = 658,
    /// <summary>5,25", M.O., ?????? sectors, 512 bytes/sector, ISO 10089, 594Mb/cart</summary>
    ISO_10089_512 = 659,

#endregion Magneto-optical, types 630 to 659

#region Other floppy standards, types 660 to 689

    CompactFloppy = 660,
    DemiDiskette  = 661,
    /// <summary>3.5", 652 tracks, 2 sides, 512 bytes/sector, Floptical, ECMA-207, ISO 14169</summary>
    Floptical = 662,
    HiFD         = 663,
    QuickDisk    = 664,
    UHD144       = 665,
    VideoFloppy  = 666,
    Wafer        = 667,
    ZXMicrodrive = 668,
    /// <summary>5.25", SS, DD, 77 tracks, 16 spt, 256 bytes/sector, MFM, 100 tpi, 300rpm</summary>
    MetaFloppy_Mod_II = 669,

#endregion Other floppy standards, types 660 to 669

#region Miscellaneous, types 670 to 689

    BeeCard     = 670,
    Borsu       = 671,
    DataStore   = 672,
    DIR         = 673,
    DST         = 674,
    DTF         = 675,
    DTF2        = 676,
    Flextra3020 = 677,
    Flextra3225 = 678,
    HiTC1       = 679,
    HiTC2       = 680,
    LT1         = 681,
    MiniCard    = 872,
    Orb         = 683,
    Orb5        = 684,
    SmartMedia  = 685,
    xD          = 686,
    XQD         = 687,
    DataPlay    = 688,

#endregion Miscellaneous, types 670 to 689

#region Apple specific media, types 690 to 699

    [Description("Apple Profile")]
    AppleProfile = 690,
    [Description("Apple Widget")]
    AppleWidget = 691,
    [Description("Apple HD20")]
    AppleHD20 = 692,
    [Description("Priam Data Tower")]
    PriamDataTower = 693,
    [Description("Bandai Pippin game")]
    Pippin = 694,

#endregion Apple specific media, types 690 to 699

#region DEC hard disks, types 700 to 729

    /// <summary>
    ///     2382 cylinders, 4 tracks/cylinder, 42 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     204890112 bytes
    /// </summary>
    RA60 = 700,
    /// <summary>
    ///     546 cylinders, 14 tracks/cylinder, 31 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     121325568 bytes
    /// </summary>
    RA80 = 701,
    /// <summary>
    ///     1248 cylinders, 14 tracks/cylinder, 51 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     456228864 bytes
    /// </summary>
    RA81 = 702,
    /// <summary>
    ///     302 cylinders, 4 tracks/cylinder, 42 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector, 25976832
    ///     bytes
    /// </summary>
    RC25 = 703,
    /// <summary>
    ///     615 cylinders, 4 tracks/cylinder, 17 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector, 21411840
    ///     bytes
    /// </summary>
    RD31 = 704,
    /// <summary>
    ///     820 cylinders, 6 tracks/cylinder, 17 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector, 42823680
    ///     bytes
    /// </summary>
    RD32 = 705,
    /// <summary>
    ///     306 cylinders, 4 tracks/cylinder, 17 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector, 10653696
    ///     bytes
    /// </summary>
    RD51 = 706,
    /// <summary>
    ///     480 cylinders, 7 tracks/cylinder, 18 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector, 30965760
    ///     bytes
    /// </summary>
    RD52 = 707,
    /// <summary>
    ///     1024 cylinders, 7 tracks/cylinder, 18 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     75497472 bytes
    /// </summary>
    RD53 = 708,
    /// <summary>
    ///     1225 cylinders, 8 tracks/cylinder, 18 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     159936000 bytes
    /// </summary>
    RD54 = 709,
    /// <summary>
    ///     411 cylinders, 3 tracks/cylinder, 22 sectors/track, 256 words/sector, 16 bits/word, 512 bytes/sector, 13888512
    ///     bytes
    /// </summary>
    RK06 = 710,
    /// <summary>
    ///     411 cylinders, 3 tracks/cylinder, 20 sectors/track, 256 words/sector, 18 bits/word, 576 bytes/sector, 14204160
    ///     bytes
    /// </summary>
    RK06_18 = 711,
    /// <summary>
    ///     815 cylinders, 3 tracks/cylinder, 22 sectors/track, 256 words/sector, 16 bits/word, 512 bytes/sector, 27540480
    ///     bytes
    /// </summary>
    RK07 = 712,
    /// <summary>
    ///     815 cylinders, 3 tracks/cylinder, 20 sectors/track, 256 words/sector, 18 bits/word, 576 bytes/sector, 28166400
    ///     bytes
    /// </summary>
    RK07_18 = 713,
    /// <summary>
    ///     823 cylinders, 5 tracks/cylinder, 32 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector, 67420160
    ///     bytes
    /// </summary>
    RM02 = 714,
    /// <summary>
    ///     823 cylinders, 5 tracks/cylinder, 32 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector, 67420160
    ///     bytes
    /// </summary>
    RM03 = 715,
    /// <summary>
    ///     823 cylinders, 19 tracks/cylinder, 32 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     256196608 bytes
    /// </summary>
    RM05 = 716,
    /// <summary>
    ///     203 cylinders, 10 tracks/cylinder, 22 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     22865920 bytes
    /// </summary>
    RP02 = 717,
    /// <summary>
    ///     203 cylinders, 10 tracks/cylinder, 20 sectors/track, 128 words/sector, 36 bits/word, 576 bytes/sector,
    ///     23385600 bytes
    /// </summary>
    RP02_18 = 718,
    /// <summary>
    ///     400 cylinders, 10 tracks/cylinder, 22 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     45056000 bytes
    /// </summary>
    RP03 = 719,
    /// <summary>
    ///     400 cylinders, 10 tracks/cylinder, 20 sectors/track, 128 words/sector, 36 bits/word, 576 bytes/sector,
    ///     46080000 bytes
    /// </summary>
    RP03_18 = 720,
    /// <summary>
    ///     411 cylinders, 19 tracks/cylinder, 22 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     87960576 bytes
    /// </summary>
    RP04 = 721,
    /// <summary>
    ///     411 cylinders, 19 tracks/cylinder, 20 sectors/track, 128 words/sector, 36 bits/word, 576 bytes/sector,
    ///     89959680 bytes
    /// </summary>
    RP04_18 = 722,
    /// <summary>
    ///     411 cylinders, 19 tracks/cylinder, 22 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     87960576 bytes
    /// </summary>
    RP05 = 723,
    /// <summary>
    ///     411 cylinders, 19 tracks/cylinder, 20 sectors/track, 128 words/sector, 36 bits/word, 576 bytes/sector,
    ///     89959680 bytes
    /// </summary>
    RP05_18 = 724,
    /// <summary>
    ///     815 cylinders, 19 tracks/cylinder, 22 sectors/track, 128 words/sector, 32 bits/word, 512 bytes/sector,
    ///     174423040 bytes
    /// </summary>
    RP06 = 725,
    /// <summary>
    ///     815 cylinders, 19 tracks/cylinder, 20 sectors/track, 128 words/sector, 36 bits/word, 576 bytes/sector,
    ///     178387200 bytes
    /// </summary>
    RP06_18 = 726,

#endregion DEC hard disks, types 700 to 729

#region Imation, types 730 to 739

    [Description("LS-120")]
    LS120 = 730,
    [Description("LS-240")]
    LS240 = 731,
    FD32MB = 732,
    RDX    = 733,
    /// <summary>Imation 320Gb RDX</summary>
    RDX320 = 734,

#endregion Imation, types 730 to 739

#region VideoNow, types 740 to 749

    [Description("VideoNow")]
    VideoNow = 740,
    [Description("VideoNow Color")]
    VideoNowColor = 741,
    [Description("VideoNow XP")]
    VideoNowXp = 742,

#endregion

#region Iomega, types 750 to 759

    /// <summary>8"x11" Bernoulli Box disk with 10Mb capacity</summary>
    [Description("Bernoulli Box (10Mb)")]
    Bernoulli10 = 750,
    /// <summary>8"x11" Bernoulli Box disk with 20Mb capacity</summary>
    [Description("Bernoulli Box (20Mb)")]
    Bernoulli20 = 751,
    /// <summary>5⅓" Bernoulli Box II disk with 20Mb capacity</summary>
    [Description("Bernoulli Box II (20Mb)")]
    BernoulliBox2_20 = 752,

#endregion Iomega, types 750 to 759

#region Kodak, types 760 to 769

    [Description("Kodak/Verbatim (3Mb)")]
    KodakVerbatim3 = 760,
    [Description("Kodak/Verbatim (6Mb)")]
    KodakVerbatim6 = 761,
    [Description("Kodak/Verbatim (12Mb)")]
    KodakVerbatim12 = 762,

#endregion Kodak, types 760 to 769

#region Sony and Panasonic Blu-ray derived, types 770 to 799

    /// <summary>Professional Disc for video, single layer, rewritable, 23Gb</summary>
    [Description("Professional Disc for video")]
    ProfessionalDisc = 770,
    /// <summary>Professional Disc for video, dual layer, rewritable, 50Gb</summary>
    [Description("Professional Disc for video")]
    ProfessionalDiscDual = 771,
    /// <summary>Professional Disc for video, triple layer, rewritable, 100Gb</summary>
    [Description("Professional Disc for video")]
    ProfessionalDiscTriple = 772,
    /// <summary>Professional Disc for video, quad layer, write once, 128Gb</summary>
    [Description("Professional Disc for video")]
    ProfessionalDiscQuad = 773,
    /// <summary>Professional Disc for DATA, single layer, rewritable, 23Gb</summary>
    [Description("Professional Disc for DATA")]
    PDD = 774,
    /// <summary>Professional Disc for DATA, single layer, write once, 23Gb</summary>
    [Description("Professional Disc for DATA")]
    PDD_WORM = 775,
    /// <summary>Archival Disc, 1st gen., 300Gb</summary>
    [Description("Archival Disc")]
    ArchivalDisc = 776,
    /// <summary>Archival Disc, 2nd gen., 500Gb</summary>
    [Description("Archival Disc")]
    ArchivalDisc2 = 777,
    /// <summary>Archival Disc, 3rd gen., 1Tb</summary>
    [Description("Archival Disc")]
    ArchivalDisc3 = 778,
    /// <summary>Optical Disc archive, 1st gen., write once, 300Gb</summary>
    [Description("Optical Disc archive")]
    ODC300R = 779,
    /// <summary>Optical Disc archive, 1st gen., rewritable, 300Gb</summary>
    [Description("Optical Disc archive")]
    ODC300RE = 780,
    /// <summary>Optical Disc archive, 2nd gen., write once, 600Gb</summary>
    [Description("Optical Disc archive")]
    ODC600R = 781,
    /// <summary>Optical Disc archive, 2nd gen., rewritable, 600Gb</summary>
    [Description("Optical Disc archive")]
    ODC600RE = 782,
    /// <summary>Optical Disc archive, 3rd gen., rewritable, 1200Gb</summary>
    [Description("Optical Disc archive")]
    ODC1200RE = 783,
    /// <summary>Optical Disc archive, 3rd gen., write once, 1500Gb</summary>
    [Description("Optical Disc archive")]
    ODC1500R = 784,
    /// <summary>Optical Disc archive, 4th gen., write once, 3300Gb</summary>
    [Description("Optical Disc archive")]
    ODC3300R = 785,
    /// <summary>Optical Disc archive, 5th gen., write once, 5500Gb</summary>
    [Description("Optical Disc archive")]
    ODC5500R = 786,

#endregion Sony and Panasonic Blu-ray derived, types 770 to 799

#region Magneto-optical, types 800 to 819

    /// <summary>5,25", M.O., 4383356 sectors, 1024 bytes/sector, ECMA-322, ISO 22092, 9.1Gb/cart</summary>
    ECMA_322_1k = 800,
    /// <summary>5,25", M.O., ??????? sectors, 512 bytes/sector, ECMA-322, ISO 22092, 9.1Gb/cart</summary>
    ECMA_322_512 = 801,
    /// <summary>5,25", M.O., 1273011 sectors, 1024 bytes/sector, ISO 14517, 2.6Gb/cart</summary>
    ISO_14517 = 802,
    /// <summary>5,25", M.O., 2244958 sectors, 512 bytes/sector, ISO 14517, 2.3Gb/cart</summary>
    ISO_14517_512 = 803,
    /// <summary>3,5", M.O., 1041500 sectors, 512 bytes/sector, ISO 15041, 540Mb/cart</summary>
    ISO_15041_512 = 804,
    /// <summary>3,5", M.O., ??????? sectors, propietary, 650Mb/cart, Sony HyperStorage</summary>
    [Description("Sony HyperStorage")]
    HSM650 = 805,

#endregion Magneto-optical, types 800 to 819

#region More floppy formats, types 820 to deprecated

    /// <summary>5.25", SS, DD, 35 tracks, 16 spt, 256 bytes/sector, MFM, 48 tpi, ???rpm</summary>
    MetaFloppy_Mod_I = 820,
    /// <summary>HyperFlex (12Mb), 5.25", DS, 301 tracks, 78 spt, 256 bytes/sector, MFM, 333 tpi, 600rpm</summary>
    [Description("HyperFlex (12Mb)")]
    HF12 = 823,
    /// <summary>HyperFlex (24Mb), 5.25", DS, 506 tracks, 78 spt, 256 bytes/sector, MFM, 666 tpi, 720rpm</summary>
    [Description("HyperFlex (24Mb)")]
    HF24 = 824,

#endregion

    [Description("Atari Lynx card")]
    AtariLynxCard = 821,
    [Description("Atari Jaguar cartridge")]
    AtariJaguarCartridge = 822
}