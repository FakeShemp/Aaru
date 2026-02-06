// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class RBF
{
    // Maximum filename length in RBF
    const int RBF_NAMELEN = 28;

    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Calculate free blocks by reading the allocation bitmap
        ulong freeBlocks = CountFreeBlocks();

        if(_isOs9000)
        {
            stat = new FileSystemInfo
            {
                Blocks         = _newIdSector.rid_totblocks,
                FilenameLength = RBF_NAMELEN,
                FreeBlocks     = freeBlocks,
                FreeFiles      = 0, // RBF doesn't have a fixed inode count
                PluginId       = Id,
                Type           = FS_TYPE
            };
        }
        else
        {
            stat = new FileSystemInfo
            {
                Blocks         = _totalSectors,
                FilenameLength = RBF_NAMELEN,
                FreeBlocks     = freeBlocks,
                FreeFiles      = 0, // RBF doesn't have a fixed inode count
                PluginId       = Id,
                Type           = FS_TYPE
            };
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Counts free blocks by reading the allocation bitmap</summary>
    /// <returns>Number of free sectors</returns>
    ulong CountFreeBlocks()
    {
        // Get bitmap size and location
        var bitmapBytes = (uint)(_isOs9000 ? 0 : _idSector.dd_map);

        if(bitmapBytes == 0)
        {
            // For OS-9000 or if dd_map is 0, we can't determine free blocks
            return 0;
        }

        // Read the allocation bitmap starting at _bitmapLsn
        ulong freeCount   = 0;
        uint  bytesRead   = 0;
        var   currentLsn  = _bitmapLsn;
        uint  bytesPerLsn = _lsnSize;

        while(bytesRead < bitmapBytes)
        {
            ErrorNumber errno = ReadLsn(currentLsn, out byte[] bitmapData);

            if(errno != ErrorNumber.NoError) break;

            // Process each byte in this sector of the bitmap
            uint bytesToProcess = Math.Min(bytesPerLsn, bitmapBytes - bytesRead);

            for(uint i = 0; i < bytesToProcess && i < bitmapData.Length; i++)
            {
                byte b = bitmapData[i];

                // Count zero bits (free clusters)
                // Each bit represents one cluster of _sectorsPerCluster sectors
                for(var bit = 0; bit < 8; bit++)
                {
                    if((b & 1 << 7 - bit) == 0) freeCount++;
                }
            }

            bytesRead += bytesToProcess;
            currentLsn++;
        }

        // Convert clusters to sectors
        return freeCount * _sectorsPerCluster;
    }
}