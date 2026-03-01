// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Blocks.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class SysVfs
{
    /// <summary>Reads a block from the filesystem</summary>
    /// <param name="blockNumber">The filesystem block number</param>
    /// <param name="data">The block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlock(long blockNumber, out byte[] data)
    {
        data = null;

        uint sectorSize                         = _imagePlugin.Info.SectorSize;
        long sectorsPerBlock                    = _blockSize / sectorSize;
        if(sectorsPerBlock < 1) sectorsPerBlock = 1;

        ulong sectorNumber = (ulong)(blockNumber * sectorsPerBlock) + _partition.Start;

        if(sectorNumber + (ulong)sectorsPerBlock > _partition.End + 1)
        {
            AaruLogging.Debug(MODULE_NAME, "Block {0} is out of partition bounds", blockNumber);

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorNumber, false, (uint)sectorsPerBlock, out data, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading block {0}: {1}", blockNumber, errno);

            return errno;
        }

        // If sector size > block size, trim to block size
        if(data.Length > _blockSize)
        {
            var trimmed = new byte[_blockSize];
            Array.Copy(data, 0, trimmed, 0, _blockSize);
            data = trimmed;
        }

        return ErrorNumber.NoError;
    }
}