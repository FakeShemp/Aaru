// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains enumerations for A2R flux images.
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

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class A2R
{
#region A2RDiskType enum

    public enum A2RDiskType : byte
    {
        _525 = 0x01,
        _35  = 0x2
    }

#endregion

#region A2rDriveType enum

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum A2rDriveType : byte
    {
        SS_525_40trk_quarterStep = 0x1,
        DS_35_80trk_appleCLV     = 0x2,
        DS_525_80trk             = 0x3,
        DS_525_40trk             = 0x4,
        DS_35_80trk              = 0x5,
        DS_8                     = 0x6,
        DS_3_80trk               = 0x7,
        DS_3_40trk               = 0x8
    }

#endregion

#region A2R Platform Constants

    /// <summary>
    ///     Per A2R 3.x spec: Standard values for requires_platform metadata key.
    ///     These values indicate which platform the software runs on.
    /// </summary>
    public static class A2RPlatform
    {
        /// <summary>Apple II platform</summary>
        public const string Apple2 = "apple2";

        /// <summary>Macintosh platform</summary>
        public const string Mac = "mac";

        /// <summary>PC (IBM PC compatible) platform</summary>
        public const string Pc = "pc";

        /// <summary>Commodore Business Machines (CBM) platform</summary>
        public const string Cbm = "cbm";

        /// <summary>Atari platform</summary>
        public const string Atari = "atari";

        /// <summary>Amiga platform</summary>
        public const string Amiga = "amiga";
    }

#endregion

#region A2R Machine Constants

    /// <summary>
    ///     Per A2R 3.x spec: Standard values for requires_machine metadata key.
    ///     These values indicate which Apple machine models the software is compatible with.
    ///     Values can be pipe-separated for multiple machine compatibility.
    /// </summary>
    public static class A2RMachine
    {
        /// <summary>Apple II (original)</summary>
        public const string II = "2";

        /// <summary>Apple II Plus</summary>
        public const string IIPlus = "2+";

        /// <summary>Apple IIe</summary>
        public const string IIe = "2e";

        /// <summary>Apple IIc</summary>
        public const string IIc = "2c";

        /// <summary>Apple IIe Enhanced</summary>
        public const string IIePlus = "2e+";

        /// <summary>Apple IIGS</summary>
        public const string IIGs = "2gs";

        /// <summary>Apple IIc Plus</summary>
        public const string IIcPlus = "2c+";

        /// <summary>Apple III</summary>
        public const string III = "3";

        /// <summary>Apple III Plus</summary>
        public const string IIIPlus = "3+";

        /// <summary>Macintosh (for Mac-compatible Apple II software)</summary>
        public const string Mac = "mac";
    }

#endregion

#region A2R RAM Constants

    /// <summary>
    ///     Per A2R 3.x spec: Standard values for requires_ram metadata key.
    ///     These values indicate RAM requirements for the software.
    ///     Values can be pipe-separated for multiple RAM requirement options.
    /// </summary>
    public static class A2RRam
    {
        /// <summary>16 kilobytes</summary>
        public const string K16 = "16K";

        /// <summary>24 kilobytes</summary>
        public const string K24 = "24K";

        /// <summary>32 kilobytes</summary>
        public const string K32 = "32K";

        /// <summary>48 kilobytes</summary>
        public const string K48 = "48K";

        /// <summary>64 kilobytes</summary>
        public const string K64 = "64K";

        /// <summary>128 kilobytes</summary>
        public const string K128 = "128K";

        /// <summary>256 kilobytes</summary>
        public const string K256 = "256K";

        /// <summary>512 kilobytes</summary>
        public const string K512 = "512K";

        /// <summary>768 kilobytes</summary>
        public const string K768 = "768K";

        /// <summary>1 megabyte</summary>
        public const string M1 = "1M";

        /// <summary>1.25 megabytes</summary>
        public const string M1_25 = "1.25M";

        /// <summary>1.5 megabytes or more</summary>
        public const string M1_5Plus = "1.5M+";

        /// <summary>Unknown RAM requirement</summary>
        public const string Unknown = "Unknown";
    }

#endregion

#region A2R Language Constants

    /// <summary>
    ///     Per A2R 3.x spec: Standard values for language metadata key.
    ///     These values indicate the language of the software.
    /// </summary>
    public static class A2RLanguage
    {
        /// <summary>English</summary>
        public const string English = "English";

        /// <summary>Spanish</summary>
        public const string Spanish = "Spanish";

        /// <summary>French</summary>
        public const string French = "French";

        /// <summary>German</summary>
        public const string German = "German";

        /// <summary>Chinese</summary>
        public const string Chinese = "Chinese";

        /// <summary>Japanese</summary>
        public const string Japanese = "Japanese";

        /// <summary>Italian</summary>
        public const string Italian = "Italian";

        /// <summary>Dutch</summary>
        public const string Dutch = "Dutch";

        /// <summary>Portuguese</summary>
        public const string Portuguese = "Portuguese";

        /// <summary>Danish</summary>
        public const string Danish = "Danish";

        /// <summary>Finnish</summary>
        public const string Finnish = "Finnish";

        /// <summary>Norwegian</summary>
        public const string Norwegian = "Norwegian";

        /// <summary>Swedish</summary>
        public const string Swedish = "Swedish";

        /// <summary>Russian</summary>
        public const string Russian = "Russian";

        /// <summary>Polish</summary>
        public const string Polish = "Polish";

        /// <summary>Turkish</summary>
        public const string Turkish = "Turkish";

        /// <summary>Arabic</summary>
        public const string Arabic = "Arabic";

        /// <summary>Thai</summary>
        public const string Thai = "Thai";

        /// <summary>Czech</summary>
        public const string Czech = "Czech";

        /// <summary>Hungarian</summary>
        public const string Hungarian = "Hungarian";

        /// <summary>Catalan</summary>
        public const string Catalan = "Catalan";

        /// <summary>Croatian</summary>
        public const string Croatian = "Croatian";

        /// <summary>Greek</summary>
        public const string Greek = "Greek";

        /// <summary>Hebrew</summary>
        public const string Hebrew = "Hebrew";

        /// <summary>Romanian</summary>
        public const string Romanian = "Romanian";

        /// <summary>Slovak</summary>
        public const string Slovak = "Slovak";

        /// <summary>Ukrainian</summary>
        public const string Ukrainian = "Ukrainian";

        /// <summary>Indonesian</summary>
        public const string Indonesian = "Indonesian";

        /// <summary>Malay</summary>
        public const string Malay = "Malay";

        /// <summary>Vietnamese</summary>
        public const string Vietnamese = "Vietnamese";

        /// <summary>Other</summary>
        public const string Other = "Other";
    }

#endregion
}