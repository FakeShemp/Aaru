// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AmigaDOSPlugin
{
    /// <summary>Reads a block from the filesystem</summary>
    /// <param name="block">Block number (relative to filesystem start)</param>
    /// <param name="data">Output block data</param>
    /// <returns>Error code</returns>
    ErrorNumber ReadBlock(uint block, out byte[] data)
    {
        data = null;

        ulong sectorAddress = _partition.Start + (ulong)block * _sectorsPerBlock;

        if(sectorAddress >= _partition.End) return ErrorNumber.InvalidArgument;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddress, false, _sectorsPerBlock, out data, out _);

        return errno;
    }

    /// <summary>Gets the block number for a given path</summary>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="blockNum">Output block number</param>
    /// <returns>Error code</returns>
    ErrorNumber GetBlockForPath(string path, out uint blockNum)
    {
        blockNum = 0;

        string pathWithoutLeadingSlash = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            // Find component in current directory (case-sensitive or insensitive based on INTL flag)
            string foundKey = null;

            foreach(string key in currentEntries.Keys)
            {
                if(string.Equals(key,
                                 component,
                                 _isIntl ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    foundKey = key;

                    break;
                }
            }

            if(foundKey == null) return ErrorNumber.NoSuchFile;

            blockNum = currentEntries[foundKey];

            // If not the last component, read the directory entries
            if(i < pathComponents.Length - 1)
            {
                ErrorNumber errno = ReadBlock(blockNum, out byte[] blockData);

                if(errno != ErrorNumber.NoError) return errno;

                // Validate it's a directory
                var type = BigEndianBitConverter.ToUInt32(blockData, 0x00);

                if(type != TYPE_HEADER) return ErrorNumber.InvalidArgument;

                int secTypeOffset = blockData.Length - 4;
                var secType       = BigEndianBitConverter.ToUInt32(blockData, secTypeOffset);

                if(secType != SUBTYPE_DIR && secType != SUBTYPE_ROOT) return ErrorNumber.NotDirectory;

                errno = ReadDirectoryEntries(blockData, out currentEntries);

                if(errno != ErrorNumber.NoError) return errno;
            }
        }

        return ErrorNumber.NoError;
    }
}