// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <summary>Reads directory data from the specified indirect disc address</summary>
    /// <param name="indAddr">Indirect disc address</param>
    /// <param name="size">Size of directory in bytes</param>
    /// <param name="data">Output directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryData(uint indAddr, uint size, out byte[] data)
    {
        data = null;

        if(size == 0)
            size = _isBigDirectory ? 0 : NEW_DIRECTORY_SIZE;

        // For old formats, indAddr is the direct byte offset (e.g., 0x200 or 0x400)
        // For new formats, we need to use the map to look up the fragment
        ulong byteOffset;

        if(_isOldMap)
        {
            // Old format: indAddr is already the byte offset
            byteOffset = indAddr;
        }
        else
        {
            // New format: indirect disc address contains fragment ID and offset
            // For the root directory, the address from disc record is usually
            // directly usable as a sector offset after accounting for the map
            // This is a simplified approach - full implementation would use the map
            byteOffset = indAddr * (ulong)_blockSize;
        }

        ulong sector        = byteOffset / _imagePlugin.Info.SectorSize + _partition.Start;
        var   offsetInSect  = (int)(byteOffset % _imagePlugin.Info.SectorSize);
        uint  sectorsToRead = (uint)((offsetInSect + size + _imagePlugin.Info.SectorSize - 1) /
                                     _imagePlugin.Info.SectorSize);

        if(sector + sectorsToRead > _partition.End)
            return ErrorNumber.InvalidArgument;

        ErrorNumber errno = _imagePlugin.ReadSectors(sector, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError)
            return errno;

        data = new byte[size];

        // For old format directories, when physical sector size > 256 bytes,
        // the directory data is interleaved - the tail is at the end of the physical sector
        if(_isOldMap && size == OLD_DIRECTORY_SIZE && sectorData.Length > OLD_DIRECTORY_SIZE)
        {
            // Old directory: copy first part (1227 bytes), then tail (53 bytes) from end of sector
            // This matches the logic in Info.cs
            Array.Copy(sectorData, offsetInSect, data, 0, (int)OLD_DIRECTORY_SIZE - 53);
            Array.Copy(sectorData, sectorData.Length - 54, data, (int)OLD_DIRECTORY_SIZE - 54, 53);
        }
        else if(offsetInSect + size <= sectorData.Length)
        {
            Array.Copy(sectorData, offsetInSect, data, 0, size);
        }
        else
        {
            Array.Copy(sectorData, offsetInSect, data, 0, sectorData.Length - offsetInSect);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a standard (F format) directory and caches entries</summary>
    /// <param name="dirData">Raw directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseStandardDirectory(byte[] dirData)
    {
        // Check minimum size - old format is 1280 bytes, new format is 2048 bytes
        if(dirData.Length < OLD_DIRECTORY_SIZE)
            return ErrorNumber.InvalidArgument;

        // Validate directory magic
        DirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<DirectoryHeader>(dirData);


        if(header.magic != OLD_DIR_MAGIC && header.magic != NEW_DIR_MAGIC)
            return ErrorNumber.InvalidArgument;

        // Directory entries start at offset 5
        const int entryOffset = 5;
        const int entrySize   = 26;

        // Old format (1280 bytes) has 47 entries, new format (2048 bytes) has 77
        int maxEntries = dirData.Length >= NEW_DIRECTORY_SIZE ? NEW_DIR_MAX_ENTRIES : OLD_DIR_MAX_ENTRIES;

        for(var i = 0; i < maxEntries; i++)
        {
            int offset = entryOffset + i * entrySize;

            if(offset + entrySize > dirData.Length)
                break;

            // Check if entry is used (first byte of name is non-zero and >= space)
            if(dirData[offset] < 0x20)
                break;

            var entryBytes = new byte[entrySize];
            Array.Copy(dirData, offset, entryBytes, 0, entrySize);

            DirectoryEntry entry = Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(entryBytes);

            // Extract filename (up to 10 characters, terminated by char < 0x20)
            var nameLen = 0;

            for(var j = 0; j < F_NAME_LEN; j++)
            {
                if(entry.name[j] < 0x20)
                    break;

                nameLen++;
            }

            if(nameLen == 0)
                break;

            string filename = _encoding.GetString(entry.name, 0, nameLen);

            // Calculate indirect disc address from 3 bytes
            uint indAddr = (uint)(entry.address[0] | entry.address[1] << 8 | entry.address[2] << 16);

            var entryInfo = new DirectoryEntryInfo
            {
                Name       = filename,
                LoadAddr   = entry.load,
                ExecAddr   = entry.exec,
                Length     = entry.length,
                IndAddr    = indAddr,
                Attributes = entry.atts
            };

            // Don't add duplicate entries
            if(!_rootDirectoryCache.ContainsKey(filename))
                _rootDirectoryCache[filename] = entryInfo;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a big (F+ format) directory and caches entries</summary>
    /// <param name="dirData">Raw directory data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ParseBigDirectory(byte[] dirData)
    {
        if(dirData.Length < 28) // Minimum header size
            return ErrorNumber.InvalidArgument;

        // Parse big directory header
        BigDirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<BigDirectoryHeader>(dirData);

        if(header.bigDirStartName != BIG_DIR_START_NAME)
            return ErrorNumber.InvalidArgument;

        uint numEntries  = header.bigDirEntries;
        uint nameHeapOff = 28 + header.bigDirNameLen; // After header + directory name
        uint entriesOff  = nameHeapOff;

        // Align to 4 bytes
        if(entriesOff % 4 != 0)
            entriesOff += 4 - entriesOff % 4;

        const int bigEntrySize = 28;

        for(uint i = 0; i < numEntries; i++)
        {
            uint offset = entriesOff + i * bigEntrySize;

            if(offset + bigEntrySize > dirData.Length)
                break;

            var entryBytes = new byte[bigEntrySize];
            Array.Copy(dirData, offset, entryBytes, 0, bigEntrySize);

            BigDirectoryEntry entry = Marshal.ByteArrayToStructureLittleEndian<BigDirectoryEntry>(entryBytes);

            // Get filename from name heap
            uint nameOff = header.bigDirSize - 8 - header.bigDirNameSize + entry.bigDirObNamePtr;
            uint nameLen = entry.bigDirObNameLen;

            if(nameOff + nameLen > dirData.Length || nameLen == 0)
                continue;

            var nameBytes = new byte[nameLen];
            Array.Copy(dirData, nameOff, nameBytes, 0, nameLen);

            string filename = _encoding.GetString(nameBytes).TrimEnd('\0');

            if(string.IsNullOrEmpty(filename))
                continue;

            var entryInfo = new DirectoryEntryInfo
            {
                Name       = filename,
                LoadAddr   = entry.bigDirLoad,
                ExecAddr   = entry.bigDirExec,
                Length     = entry.bigDirLen,
                IndAddr    = entry.bigDirIndAddr,
                Attributes = (byte)entry.bigDirAttr
            };

            // Don't add duplicate entries
            if(!_rootDirectoryCache.ContainsKey(filename))
                _rootDirectoryCache[filename] = entryInfo;
        }

        return ErrorNumber.NoError;
    }
}

