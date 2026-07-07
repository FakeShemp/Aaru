// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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

using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class GDFX
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string normalizedPath = string.IsNullOrEmpty(path)
                                    ? "/"
                                    : path.StartsWith('/')
                                        ? path
                                        : "/" + path;

        if(!_directoryCache.TryGetValue(normalizedPath, out List<DecodedEntry> entries)) return ErrorNumber.NoSuchFile;

        node = new GdfxDirNode
        {
            Path     = normalizedPath,
            Contents = entries.Select(e => e.Name).ToArray(),
            Position = 0
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not GdfxDirNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not GdfxDirNode dirNode) return ErrorNumber.InvalidArgument;

        if(dirNode.Position >= dirNode.Contents.Length) return ErrorNumber.NoError;

        filename = dirNode.Contents[dirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Parses a directory block (binary search tree) and returns a flat list of decoded entries. The block must
    ///     already be read into memory.
    /// </summary>
    List<DecodedEntry> ParseDirectoryBlock(byte[] block)
    {
        var entries = new List<DecodedEntry>();
        var visited = new HashSet<int>();
        var stack   = new Stack<int>();
        stack.Push(0);

        while(stack.Count > 0)
        {
            int byteOffset = stack.Pop();

            if(byteOffset < 0 || !visited.Add(byteOffset)) continue;

            const int headerSize = 14;

            if(byteOffset + headerSize > block.Length) continue;

            DirectoryEntryHeader header =
                Marshal.ByteArrayToStructureLittleEndian<DirectoryEntryHeader>(block, byteOffset, headerSize);

            if(header.filenameLength == 0) continue;

            if(byteOffset + headerSize + header.filenameLength > block.Length) continue;

            string name = _encoding.GetString(block, byteOffset + headerSize, header.filenameLength);

            entries.Add(new DecodedEntry
            {
                Name       = name,
                DataSector = header.dataSector,
                DataSize   = header.dataSize,
                Attributes = header.attributes
            });

            AaruLogging.Debug(MODULE_NAME,
                              "Entry: {0}, sector {1}, size {2}, attrs 0x{3:X2}",
                              name,
                              header.dataSector,
                              header.dataSize,
                              header.attributes);

            if(header.rightEntryOffset != NO_CHILD) stack.Push(header.rightEntryOffset * 4);

            if(header.leftEntryOffset != NO_CHILD) stack.Push(header.leftEntryOffset * 4);
        }

        return entries;
    }

    /// <summary>Reads a directory block from disk, parses it, and recursively caches all subdirectories.</summary>
    ErrorNumber CacheDirectory(string path, uint sector, uint size)
    {
        if(size == 0) return ErrorNumber.NoError;

        uint sectorCount = (size + SECTOR_SIZE - 1) / SECTOR_SIZE;

        ErrorNumber errno = ReadGameSectors(sector, sectorCount, out byte[] block);

        if(errno != ErrorNumber.NoError) return errno;

        List<DecodedEntry> entries = ParseDirectoryBlock(block);
        _directoryCache[path] = entries;

        foreach(DecodedEntry entry in entries)
        {
            if(!entry.IsDirectory) continue;

            string subPath = path == "/" ? "/" + entry.Name : path + "/" + entry.Name;

            errno = CacheDirectory(subPath, entry.DataSector, entry.DataSize);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }
}