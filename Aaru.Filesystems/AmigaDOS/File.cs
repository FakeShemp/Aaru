// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Amiga Fast File System plugin.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AmigaDOSPlugin
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "GetAttributes: path='{0}'", path);

        // Use Stat to get file information
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        attributes = stat.Attributes;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read root block
            ErrorNumber errno = ReadBlock(_rootBlockSector, out byte[] rootBlockData);

            if(errno != ErrorNumber.NoError) return errno;

            stat = new FileEntryInfo
            {
                Attributes       = FileAttributes.Directory,
                Inode            = _rootBlockSector,
                Links            = 1,
                BlockSize        = _blockSize,
                LastWriteTimeUtc = DateHandlers.AmigaToDateTime(_rootBlock.rDays, _rootBlock.rMins, _rootBlock.rTicks),
                CreationTimeUtc  = DateHandlers.AmigaToDateTime(_rootBlock.cDays, _rootBlock.cMins, _rootBlock.cTicks)
            };

            return ErrorNumber.NoError;
        }

        // Find the entry
        ErrorNumber error = GetBlockForPath(normalizedPath, out uint blockNum);

        if(error != ErrorNumber.NoError) return error;

        // Read the block
        error = ReadBlock(blockNum, out byte[] blockData);

        if(error != ErrorNumber.NoError) return error;

        // Validate block type
        var type = BigEndianBitConverter.ToUInt32(blockData, 0x00);

        if(type != TYPE_HEADER) return ErrorNumber.InvalidArgument;

        // Get secondary type
        int secTypeOffset = blockData.Length - 4;
        var secType       = BigEndianBitConverter.ToUInt32(blockData, secTypeOffset);

        // Build stat from block data
        stat = BuildStatFromBlock(blockData, blockNum, secType);

        return ErrorNumber.NoError;
    }


    /// <summary>Builds a FileEntryInfo from block data</summary>
    /// <param name="blockData">Block data</param>
    /// <param name="blockNum">Block number</param>
    /// <param name="secType">Secondary type</param>
    /// <returns>FileEntryInfo structure</returns>
    FileEntryInfo BuildStatFromBlock(byte[] blockData, uint blockNum, uint secType)
    {
        // Block offsets (in longs from end of block)
        int protectOffset  = blockData.Length - 48 * 4; // BLK_PROTECT = SizeBlock - 48
        int byteSizeOffset = blockData.Length - 47 * 4; // BLK_BYTE_SIZE = SizeBlock - 47
        int daysOffset     = blockData.Length - 23 * 4; // BLK_DAYS = SizeBlock - 23
        int minsOffset     = blockData.Length - 22 * 4; // BLK_MINS = SizeBlock - 22
        int ticksOffset    = blockData.Length - 21 * 4; // BLK_TICKS = SizeBlock - 21

        var protect  = BigEndianBitConverter.ToUInt32(blockData, protectOffset);
        var byteSize = BigEndianBitConverter.ToUInt32(blockData, byteSizeOffset);
        var days     = BigEndianBitConverter.ToUInt32(blockData, daysOffset);
        var mins     = BigEndianBitConverter.ToUInt32(blockData, minsOffset);
        var ticks    = BigEndianBitConverter.ToUInt32(blockData, ticksOffset);

        var stat = new FileEntryInfo
        {
            Inode            = blockNum,
            Links            = 1,
            BlockSize        = _blockSize,
            LastWriteTimeUtc = DateHandlers.AmigaToDateTime(days, mins, ticks),
            Mode             = AmigaProtectToUnixMode(protect)
        };

        // Determine type from secondary type
        // ST_FILE = -3, ST_ROOT = 1, ST_USERDIR = 2, ST_LINKDIR = 4, ST_LFILE = -4, ST_LSOFT = 3
        var signedSecType = (int)secType;

        // Handle signed comparison for negative values
        if(secType > 0x7FFFFFFF) signedSecType = (int)(secType - 0x100000000);

        switch(signedSecType)
        {
            case 1: // ST_ROOT
            case 2: // ST_USERDIR
            case 4: // ST_LINKDIR
                stat.Attributes = FileAttributes.Directory;

                break;

            case -3: // ST_FILE
                stat.Attributes = FileAttributes.File;
                stat.Length     = byteSize;

                break;

            case -4: // ST_LFILE (hard link to file)
                stat.Attributes = FileAttributes.File;
                stat.Length     = byteSize;

                break;

            case 3: // ST_LSOFT (soft link)
                stat.Attributes = FileAttributes.Symlink;

                break;

            default:
                stat.Attributes = FileAttributes.File;
                stat.Length     = byteSize;

                break;
        }

        // Apply Amiga protection flags to attributes
        // Note: Amiga protection bits for owner are active-low (0 = allowed)
        // FIBB_ARCHIVE = 4
        if((protect & 1 << 4) != 0) stat.Attributes |= FileAttributes.Archive;

        // FIBB_PURE = 5 - maps to System (resident)
        if((protect & 1 << 5) != 0) stat.Attributes |= FileAttributes.System;

        // FIBB_SCRIPT = 6
        // No direct mapping, could use Hidden but that's not accurate

        // Calculate blocks used (for files)
        if(stat.Length > 0) stat.Blocks = (stat.Length + _blockSize - 1) / _blockSize;

        return stat;
    }

    /// <summary>Converts Amiga protection bits to Unix-style mode</summary>
    /// <param name="protect">Amiga protection bits</param>
    /// <returns>Unix-style mode</returns>
    static uint AmigaProtectToUnixMode(uint protect)
    {
        // Amiga owner bits are active-low (0 = allowed), Unix are active-high
        // FIBB_READ = 3, FIBB_WRITE = 2, FIBB_EXECUTE = 1, FIBB_DELETE = 0
        uint mode = 0;

        // Owner permissions (active-low, so invert)
        if((protect & 1 << 3) == 0) // FIBB_READ
            mode |= 0x100;          // S_IRUSR

        if((protect & 1 << 2) == 0) // FIBB_WRITE
            mode |= 0x080;          // S_IWUSR

        if((protect & 1 << 1) == 0) // FIBB_EXECUTE
            mode |= 0x040;          // S_IXUSR

        // Group permissions (active-high for FIBB_GRP_*)
        // FIBB_GRP_READ = 11, FIBB_GRP_WRITE = 10, FIBB_GRP_EXECUTE = 9
        if((protect & 1 << 11) != 0) mode |= 0x020; // S_IRGRP

        if((protect & 1 << 10) != 0) mode |= 0x010; // S_IWGRP

        if((protect & 1 << 9) != 0) mode |= 0x008; // S_IXGRP

        // Other permissions (active-high for FIBB_OTR_*)
        // FIBB_OTR_READ = 15, FIBB_OTR_WRITE = 14, FIBB_OTR_EXECUTE = 13
        if((protect & 1 << 15) != 0) mode |= 0x004; // S_IROTH

        if((protect & 1 << 14) != 0) mode |= 0x002; // S_IWOTH

        if((protect & 1 << 13) != 0) mode |= 0x001; // S_IXOTH

        return mode;
    }
}