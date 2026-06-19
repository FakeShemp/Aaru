// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Twiggy.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Lisa filesystem plugin.
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

using System;
using System.Collections.Generic;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class LisaFS
{
    const ushort TWIGGY_BAD_BLOCK_OSID          = 0x4AFC;
    const ulong  TWIGGY_SPARE_TRACK_START       = 1684;
    const ulong  TWIGGY_SECOND_DIRECTORY_SECTOR = TWIGGY_SPARE_TRACK_START + 13;

    void InitializeTwiggyBadBlockMap()
    {
        _twiggyBadBlockMap = null;

        if(_device.Info.MediaType != MediaType.AppleFileWare || _device.Info.Sectors < TWIGGY_SPARE_TRACK_START + 18)
            return;

        ErrorNumber errno = _device.ReadSector(TWIGGY_SPARE_TRACK_START, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError ||
           !TryDecodeTwiggyBadBlockTable(sector, out Dictionary<ulong, ulong> badBlocks))
        {
            errno = _device.ReadSector(TWIGGY_SECOND_DIRECTORY_SECTOR, false, out sector, out _);

            if(errno != ErrorNumber.NoError || !TryDecodeTwiggyBadBlockTable(sector, out badBlocks)) return;
        }

        _twiggyBadBlockMap = badBlocks;

        foreach(KeyValuePair<ulong, ulong> badBlock in _twiggyBadBlockMap)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Twiggy remap logical sector {0} to spare sector {1}",
                              badBlock.Key,
                              badBlock.Value);
        }
    }

    static bool TryDecodeTwiggyBadBlockTable(byte[] sector, out Dictionary<ulong, ulong> badBlocks)
    {
        badBlocks = null;

        if(sector == null || sector.Length < 0x26) return false;

        var osid    = BigEndianBitConverter.ToUInt16(sector, 0x00);
        var version = BigEndianBitConverter.ToUInt16(sector, 0x02);
        var numbad  = BigEndianBitConverter.ToInt16(sector, 0x04);

        if(osid != TWIGGY_BAD_BLOCK_OSID || version != 1 || numbad is < 0 or > 16) return false;

        badBlocks = [];

        for(var i = 0; i < numbad; i++)
        {
            var logicalBlock = BigEndianBitConverter.ToInt16(sector, 0x06 + i * 2);

            if(logicalBlock < 0) continue;

            badBlocks[(ulong)logicalBlock] = TWIGGY_SPARE_TRACK_START + (ulong)(i + 1 + (i > 11 ? 1 : 0));
        }

        return true;
    }

    ulong ResolveSectorAddress(ulong sectorAddress)
    {
        if(_twiggyBadBlockMap != null && _twiggyBadBlockMap.TryGetValue(sectorAddress, out ulong remappedSector))
            return remappedSector;

        return sectorAddress;
    }

    ErrorNumber ReadLisaSector(ulong sectorAddress, out byte[] buffer)
    {
        ulong mappedSector = ResolveSectorAddress(sectorAddress);

        return _device.ReadSector(mappedSector, false, out buffer, out _);
    }

    ErrorNumber ReadLisaSectorTag(ulong sectorAddress, out byte[] buffer)
    {
        ulong mappedSector = ResolveSectorAddress(sectorAddress);

        return _device.ReadSectorTag(mappedSector, false, SectorTagType.AppleSonyTag, out buffer);
    }

    ErrorNumber ReadLisaSectors(ulong sectorAddress, uint length, out byte[] buffer)
    {
        buffer = null;

        if(_twiggyBadBlockMap == null || _twiggyBadBlockMap.Count == 0)
            return _device.ReadSectors(sectorAddress, false, length, out buffer, out _);

        buffer = new byte[length * _device.Info.SectorSize];

        for(uint i = 0; i < length; i++)
        {
            ErrorNumber errno = ReadLisaSector(sectorAddress + i, out byte[] sector);

            if(errno != ErrorNumber.NoError) return errno;

            Array.Copy(sector, 0, buffer, i * _device.Info.SectorSize, sector.Length);
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber ReadLisaSectorsTag(ulong sectorAddress, uint length, out byte[] buffer)
    {
        buffer = null;

        if(_twiggyBadBlockMap == null || _twiggyBadBlockMap.Count == 0)
            return _device.ReadSectorsTag(sectorAddress, false, length, SectorTagType.AppleSonyTag, out buffer);

        buffer = new byte[length * (uint)_devTagSize];

        for(uint i = 0; i < length; i++)
        {
            ErrorNumber errno = ReadLisaSectorTag(sectorAddress + i, out byte[] sectorTag);

            if(errno != ErrorNumber.NoError) return errno;

            Array.Copy(sectorTag, 0, buffer, i * _devTagSize, sectorTag.Length);
        }

        return ErrorNumber.NoError;
    }
}