// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MMC.PlayStation3.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     PS3 disc key extraction from Sony PS-SYSTEM optical drives during dump.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.Core.Devices.Dumping.PlayStation3;
using Aaru.Core.Image.PS3;
using Aaru.Localization;
using Aaru.Logging;

namespace Aaru.Core.Devices.Dumping;

partial class Dump
{
    const string PS3_DUMP_MODULE = "PS3 dump";

    static readonly byte[] _ps3BootId = "PlayStation3\0\0\0\0"u8.ToArray();

    static bool IsPlayStation3BlurayMedia(MediaType mediaType) =>
        mediaType is MediaType.BDROM or MediaType.BDR or MediaType.BDRE or MediaType.BDRXL or MediaType.BDREXL or
                    MediaType.PS3BD;

    /// <summary>
    ///     When dumping from a Sony PS-SYSTEM optical drive, extracts PS3 Data1/Data2 keys and stores them as media tags.
    /// </summary>
    void TryExtractPs3DriveKeys(ref MediaType dskType, Dictionary<MediaTagType, byte[]> mediaTags)
    {
        if(!_dev.IsPlayStation3Drive() || !IsPlayStation3BlurayMedia(dskType)) return;

        if(TryDetectPlayStation3BootSector(out _))
        {
            if(dskType == MediaType.BDROM)
            {
                dskType = MediaType.PS3BD;
                AaruLogging.Debug(PS3_DUMP_MODULE, Localization.Core.Found_Sony_PlayStation_3_boot_sectors);
            }
        }

        UpdateStatus?.Invoke(UI.PS3_extracting_drive_keys);

        if(!Ps3DriveAuth.ExtractData1Data2(_dev, out byte[] data1, out byte[] data2))
        {
            ErrorMessage?.Invoke(UI.PS3_drive_key_extraction_failed);

            return;
        }

        mediaTags[MediaTagType.PS3_Data1]  = data1;
        mediaTags[MediaTagType.PS3_Data2]  = data2;
        mediaTags[MediaTagType.PS3_DiscKey] = Ps3Crypto.DeriveDiscKey(data1);

        UpdateStatus?.Invoke(UI.PS3_drive_keys_extracted);
        AaruLogging.WriteLine(UI.PS3_drive_keys_data1_0, Convert.ToHexString(data1));
        AaruLogging.WriteLine(UI.PS3_drive_keys_data2_0, Convert.ToHexString(data2));
    }

    /// <summary>Reads sector 1 and checks for the PlayStation 3 boot sector identifier.</summary>
    bool TryDetectPlayStation3BootSector(out byte[] sector1)
    {
        sector1 = null;

        bool sense = _dev.Read10(out byte[] buffer,
                                 out _,
                                 0,
                                 false,
                                 true,
                                 false,
                                 false,
                                 1,
                                 2048,
                                 0,
                                 1,
                                 _dev.Timeout,
                                 out _);

        if(sense || _dev.Error || buffer == null || buffer.Length < _ps3BootId.Length) return false;

        sector1 = buffer;

        byte[] tmp = new byte[_ps3BootId.Length];
        Array.Copy(sector1, 0, tmp, 0, tmp.Length);

        return tmp.SequenceEqual(_ps3BootId);
    }
}
