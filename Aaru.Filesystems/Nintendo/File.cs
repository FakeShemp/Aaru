// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the filesystem used by Nintendo Gamecube and Wii discs</summary>
public sealed partial class NintendoPlugin
{
    /// <summary>Resolve a filesystem path to an FST entry index</summary>
    /// <param name="path">Absolute or relative path</param>
    /// <param name="entryIndex">Resulting FST entry index (may be DOL_VIRTUAL_INDEX for main.dol)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ResolvePathToIndex(string path, out int entryIndex)
    {
        entryIndex = 0;

        if(string.IsNullOrWhiteSpace(path) || path == "/") return ErrorNumber.NoError;

        string cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] pieces          = cutPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var      currentDirIndex = 0;

        for(var p = 0; p < pieces.Length; p++)
        {
            Dictionary<string, int> currentEntries = currentDirIndex == 0
                                                         ? _rootDirectoryCache
                                                         : GetDirectoryEntries(currentDirIndex);

            KeyValuePair<string, int> match =
                currentEntries.FirstOrDefault(e => e.Key.Equals(pieces[p], StringComparison.OrdinalIgnoreCase));

            if(match.Key is null) return ErrorNumber.NoSuchFile;

            int idx = match.Value;

            if(p < pieces.Length - 1)
            {
                // Intermediate components must be directories
                // Virtual files (negative indices) cannot be directories
                if(idx < 0) return ErrorNumber.NotDirectory;

                if(_fstEntries[idx].TypeAndNameOffset >> 24 == 0) return ErrorNumber.NotDirectory;

                currentDirIndex = idx;
            }
            else
            {
                // Final component — can be file or directory
                entryIndex = idx;
            }
        }

        return ErrorNumber.NoError;
    }

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Root directory
        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = 2048,
                Blocks     = 0,
                Length     = 0
            };

            return ErrorNumber.NoError;
        }

        // Resolve path to FST index
        ErrorNumber errno = ResolvePathToIndex(path, out int entryIndex);

        if(errno != ErrorNumber.NoError) return errno;

        // Handle virtual DOL file
        if(entryIndex == DOL_VIRTUAL_INDEX)
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.File,
                BlockSize  = 2048,
                Blocks     = (_dolSize + 2047) / 2048,
                Length     = _dolSize
            };

            return ErrorNumber.NoError;
        }

        bool isDirectory = _fstEntries[entryIndex].TypeAndNameOffset >> 24 != 0;

        if(isDirectory)
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = 2048,
                Blocks     = 0,
                Length     = 0
            };
        }
        else
        {
            long length = _fstEntries[entryIndex].SizeOrNext;

            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.File,
                BlockSize  = 2048,
                Blocks     = (length + 2047) / 2048,
                Length     = length
            };
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = ResolvePathToIndex(path, out int entryIndex);

        if(errno != ErrorNumber.NoError) return errno;

        // Handle virtual DOL file
        if(entryIndex == DOL_VIRTUAL_INDEX)
        {
            node = new NintendoFileNode
            {
                Path     = path,
                Length   = _dolSize,
                Offset   = 0,
                FstIndex = DOL_VIRTUAL_INDEX
            };

            return ErrorNumber.NoError;
        }

        if(_fstEntries[entryIndex].TypeAndNameOffset >> 24 != 0) return ErrorNumber.IsDirectory;

        node = new NintendoFileNode
        {
            Path     = path,
            Length   = _fstEntries[entryIndex].SizeOrNext,
            Offset   = 0,
            FstIndex = entryIndex
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not NintendoFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not NintendoFileNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Offset >= myNode.Length) return ErrorNumber.NoError;

        long remaining = myNode.Length - myNode.Offset;

        if(length > remaining) length = remaining;

        if(length > buffer.Length) length = buffer.Length;

        // Get the file's data offset and size from the FST entry
        // For virtual files (like main.dol), use stored offset directly
        uint fileDataOffset = myNode.FstIndex == DOL_VIRTUAL_INDEX
                                  ? _dolOffset
                                  : _fstEntries[myNode.FstIndex].OffsetOrParent;

        if(_isWii)
        {
            // Wii: read from encrypted partition data
            byte[] data = ReadWiiPartitionData((uint)(fileDataOffset + myNode.Offset), (uint)length);

            if(data == null) return ErrorNumber.InOutError;

            Array.Copy(data, 0, buffer, 0, length);
        }
        else
        {
            // GameCube: read directly from image, accounting for sub-sector offset
            ulong absoluteOffset = fileDataOffset + (ulong)myNode.Offset;
            uint  sectorSize     = _imagePlugin.Info.SectorSize;
            ulong startSector    = absoluteOffset / sectorSize;
            var   sectorOffset   = (uint)(absoluteOffset % sectorSize);
            uint  sectorsToRead  = (sectorOffset + (uint)length + sectorSize - 1) / sectorSize;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(startSector, false, sectorsToRead, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            Array.Copy(sectorData, sectorOffset, buffer, 0, length);
        }

        read          =  length;
        myNode.Offset += length;

        return ErrorNumber.NoError;
    }

#endregion
}