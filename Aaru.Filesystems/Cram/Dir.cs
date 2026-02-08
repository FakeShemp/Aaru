// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Cram
{
    /// <summary>Reads and parses directory contents</summary>
    /// <param name="offset">Byte offset of the directory data</param>
    /// <param name="size">Size of the directory in bytes</param>
    /// <param name="entries">Dictionary to populate with entries</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(uint offset, uint size, Dictionary<string, DirectoryEntryInfo> entries)
    {
        if(size == 0) return ErrorNumber.NoError;

        // Read the directory data
        ErrorNumber errno = ReadBytes(offset, size, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory data at offset {0}: {1}", offset, errno);

            return errno;
        }

        // Parse directory entries
        // Each entry is: cramfs_inode (12 bytes) + name (padded to 4 bytes)
        uint currentOffset = 0;

        while(currentOffset < size)
        {
            if(currentOffset + 12 > dirData.Length) break;

            var inodeData = new byte[12];
            Array.Copy(dirData, currentOffset, inodeData, 0, 12);

            Inode inode = _littleEndian
                              ? Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData)
                              : Marshal.ByteArrayToStructureBigEndian<Inode>(inodeData);

            // Name length is stored as (actual_length + 3) / 4, so multiply by 4 to get padded length
            int nameLen = inode.NameLen << 2;

            if(nameLen == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "Zero name length at offset {0}", currentOffset);

                break;
            }

            if(currentOffset + 12 + nameLen > dirData.Length)
            {
                AaruLogging.Debug(MODULE_NAME, "Name extends beyond directory data");

                break;
            }

            // Read the name (it's null-padded to 4-byte boundary)
            var nameBytes = new byte[nameLen];
            Array.Copy(dirData, currentOffset + 12, nameBytes, 0, nameLen);

            string name = StringHandlers.CToString(nameBytes, _encoding);

            if(string.IsNullOrEmpty(name))
            {
                AaruLogging.Debug(MODULE_NAME, "Empty name at offset {0}", currentOffset);

                break;
            }

            // Skip . and .. entries
            if(name is not "." and not "..")
            {
                var entry = new DirectoryEntryInfo
                {
                    Name   = name,
                    Inode  = inode,
                    Offset = offset + currentOffset
                };

                entries.TryAdd(name, entry);
            }

            currentOffset += (uint)(12 + nameLen);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Checks if a mode indicates a directory</summary>
    static bool IsDirectory(ushort mode) => (mode & 0xF000) == 0x4000; // S_IFDIR
}