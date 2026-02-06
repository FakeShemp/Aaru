// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : RT-11 file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the RT-11 file system and shows information.
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

using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
public sealed partial class RT11
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "StatFs: Getting filesystem statistics");

        // Calculate total blocks in the partition
        // RT-11 uses 512-byte blocks
        ulong totalBlocks = _partition.End - _partition.Start + 1;

        // Calculate used blocks by iterating through directory entries
        ulong usedBlocks = RESERVED_BLOCKS; // Blocks 0-5 are reserved

        // Add directory segments
        usedBlocks += (ulong)(_totalSegments * 2); // Each segment is 2 blocks

        // Add file blocks from directory cache
        foreach(string filename in _rootDirectoryCache.Keys)
        {
            // We need to read the directory to get file sizes
            // For now, we'll estimate based on what's in the cache
            // This is a limitation without parsing all directory segments
            ErrorNumber errno = GetFileLengthFromCache(filename, out uint fileLength);

            if(errno == ErrorNumber.NoError) usedBlocks += fileLength;
        }

        ulong freeBlocks = totalBlocks > usedBlocks ? totalBlocks - usedBlocks : 0;

        stat = new FileSystemInfo
        {
            Blocks         = totalBlocks,
            FilenameLength = 10, // RT-11 supports 6.3 filenames (6 chars + 3 char extension = max 9 chars + dot = 10)
            Files          = (ulong)_rootDirectoryCache.Count,
            FreeBlocks     = freeBlocks,
            FreeFiles      = 0, // RT-11 doesn't have a fixed file limit, it depends on directory segments
            PluginId       = Id,
            Type           = FS_TYPE
        };

        AaruLogging.Debug(MODULE_NAME,
                          $"StatFs: Total blocks={totalBlocks}, Used blocks={usedBlocks}, Free blocks={freeBlocks}, Files={_rootDirectoryCache.Count}");

        return ErrorNumber.NoError;
    }
}