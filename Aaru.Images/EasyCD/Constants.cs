// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for Easy CD Creator disc images.
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

namespace Aaru.Images;

public sealed partial class EasyCD
{
    /// <summary>Minimum number of frames per track</summary>
    const int MIN_TRACK_LENGTH = 300;

    /// <summary>CD-DA audio sector size in bytes</summary>
    const int AUDIO_SECTOR_SIZE = 2352;
    /// <summary>CD-ROM Mode 1 sector size in bytes</summary>
    const int MODE1_SECTOR_SIZE = 2048;
    /// <summary>CD-ROM Mode 2 Form 1 sector size in bytes</summary>
    const int MODE2_FORM1_SECTOR_SIZE = 2056;
    /// <summary>CD-ROM Mode 2 Mixed sector size in bytes</summary>
    const int MODE2_MIXED_SECTOR_SIZE = 2332;

    /// <summary>CD pregap length in sectors</summary>
    const int PREGAP_LENGTH = 150;
    /// <summary>"adio" offset entry type for audio tracks</summary>
    readonly byte[] _cifAudioEntryType = [0x61, 0x64, 0x69, 0x6F];
    /// <summary>"disc" block type containing disc, session and track descriptors</summary>
    readonly byte[] _cifDiscType = [0x64, 0x69, 0x73, 0x63];
    /// <summary>"imag" block type identifying a CIF image file</summary>
    readonly byte[] _cifImageType = [0x69, 0x6D, 0x61, 0x67];
    /// <summary>"info" offset entry type for data tracks</summary>
    readonly byte[] _cifInfoEntryType = [0x69, 0x6E, 0x66, 0x6F];
    /// <summary>"ofs " block type containing the offset table</summary>
    readonly byte[] _cifOffsetType = [0x6F, 0x66, 0x73, 0x20];

    /// <summary>"RIFF" block signature</summary>
    readonly byte[] _cifRiffSignature = [0x52, 0x49, 0x46, 0x46];
}