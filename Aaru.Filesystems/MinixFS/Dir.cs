// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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
public sealed partial class MinixFS
{
    /// <summary>Reads the contents of a directory</summary>
    /// <param name="inodeNumber">Inode number of the directory</param>
    /// <param name="entries">Dictionary of filename -> inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(uint inodeNumber, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>();

        // Get inode
        ErrorNumber errno = ReadInode(inodeNumber, out object inodeObj);

        if(errno != ErrorNumber.NoError) return errno;

        uint   size;
        uint[] zones;
        int    nrDzones;

        if(_version == FilesystemVersion.V1)
        {
            var inode = (V1DiskInode)inodeObj;
            size = inode.d1_size;

            // Convert ushort[] to uint[]
            zones = new uint[inode.d1_zone.Length];

            for(var i = 0; i < inode.d1_zone.Length; i++) zones[i] = inode.d1_zone[i];

            nrDzones = V1_NR_DZONES;
        }
        else
        {
            var inode = (V2DiskInode)inodeObj;
            size     = inode.d2_size;
            zones    = inode.d2_zone;
            nrDzones = V2_NR_DZONES;
        }

        if(size == 0) return ErrorNumber.NoError;

        // Read directory data block by block
        var dirData   = new byte[size];
        var bytesRead = 0;

        // Read direct zones
        for(var i = 0; i < nrDzones && bytesRead < size; i++)
        {
            if(zones[i] == 0)
            {
                // Sparse - fill with zeros
                int toFill = Math.Min(_blockSize, (int)(size - bytesRead));
                bytesRead += toFill;

                continue;
            }

            errno = ReadBlock((int)zones[i], out byte[] blockData);

            if(errno != ErrorNumber.NoError) return errno;

            int toCopy = Math.Min(blockData.Length, (int)(size - bytesRead));
            Array.Copy(blockData, 0, dirData, bytesRead, toCopy);
            bytesRead += toCopy;
        }

        // Read single indirect zone if needed
        if(bytesRead < size && nrDzones < zones.Length && zones[nrDzones] != 0)
        {
            errno = ReadIndirectZone(zones[nrDzones], ref dirData, ref bytesRead, (int)size, 1);

            if(errno != ErrorNumber.NoError) return errno;
        }

        // Read double indirect zone if needed
        if(bytesRead < size && nrDzones + 1 < zones.Length && zones[nrDzones + 1] != 0)
        {
            errno = ReadIndirectZone(zones[nrDzones + 1], ref dirData, ref bytesRead, (int)size, 2);

            if(errno != ErrorNumber.NoError) return errno;
        }

        // Read triple indirect zone if needed (V2 only)
        if(bytesRead < size && nrDzones + 2 < zones.Length && zones[nrDzones + 2] != 0)
        {
            errno = ReadIndirectZone(zones[nrDzones + 2], ref dirData, ref bytesRead, (int)size, 3);

            if(errno != ErrorNumber.NoError) return errno;
        }

        // Parse directory entries
        int entrySize  = _filenameSize + (_version == FilesystemVersion.V3 ? 4 : 2);
        int numEntries = (int)size / entrySize;

        for(var i = 0; i < numEntries; i++)
        {
            int offset = i * entrySize;

            uint ino;

            if(_version == FilesystemVersion.V3)
            {
                ino = _littleEndian
                          ? BitConverter.ToUInt32(dirData, offset)
                          : (uint)(dirData[offset]     << 24 |
                                   dirData[offset + 1] << 16 |
                                   dirData[offset + 2] << 8  |
                                   dirData[offset + 3]);

                offset += 4;
            }
            else
            {
                ino = _littleEndian
                          ? BitConverter.ToUInt16(dirData, offset)
                          : (ushort)(dirData[offset] << 8 | dirData[offset + 1]);

                offset += 2;
            }

            // Skip empty entries
            if(ino == 0) continue;

            // Extract filename
            var nameBytes = new byte[_filenameSize];
            Array.Copy(dirData, offset, nameBytes, 0, _filenameSize);

            string name = StringHandlers.CToString(nameBytes, _encoding);

            if(string.IsNullOrEmpty(name)) continue;

            entries[name] = ino;
        }

        return ErrorNumber.NoError;
    }
}