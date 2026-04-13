// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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
using System.Globalization;
using System.Linq;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Filesystems;

public partial class SonyPFS
{
    /// <summary>Reads directory entries from an inode, returning a dictionary of name → DirEntry.</summary>
    Dictionary<string, DirEntry> ReadDirectory(Inode dirInode)
    {
        Dictionary<string, DirEntry> entries = new();

        // Directory data starts at data[1]; data[0] is the inode's own block
        // Walk through all data segments
        uint zoneSize = _superBlock.zone_size;

        // We need to follow the inode's data array and possibly indirect segment descriptors
        // For simplicity, read from the direct inode first
        Inode currentSegment  = dirInode;
        uint  dataIndex       = 1; // Start from data[1]
        uint  totalDataBlocks = dirInode.number_data;
        ulong bytesRead       = 0;

        while(dataIndex < totalDataBlocks)
        {
            uint fixedIndex = FixIndex(dataIndex);

            // If fixedIndex wrapped to 0 and we're past the direct blocks,
            // we need to follow the next segment descriptor
            if(fixedIndex == 0 && dataIndex >= PFS_INODE_MAX_BLOCKS)
            {
                if(currentSegment.next_segment.number == 0)
                    break;

                ErrorNumber errno = ReadInode(currentSegment.next_segment.number,
                                              currentSegment.next_segment.subpart,
                                              out currentSegment);

                if(errno != ErrorNumber.NoError)
                    break;

                // After loading next segment, fixedIndex 0 is the segment's own block, skip to next
                dataIndex++;

                continue;
            }

            BlockInfo bi = currentSegment.data[fixedIndex];

            // Read each zone in this block run
            for(uint offset = 0; offset < bi.count && bytesRead < dirInode.size; offset++)
            {
                ErrorNumber errno = ReadDataBlock(bi, offset, out byte[] zoneData);

                if(errno != ErrorNumber.NoError)
                    return entries;

                // Parse directory entries from the zone data
                // Each metadata chunk is PFS_META_SIZE (1024) bytes
                uint chunksPerZone = zoneSize / PFS_META_SIZE;

                for(uint chunk = 0; chunk < chunksPerZone && bytesRead < dirInode.size; chunk++)
                {
                    uint chunkOffset = chunk * PFS_META_SIZE;

                    if(chunkOffset + 8 > zoneData.Length)
                        break;

                    // Parse directory entries within this 512-byte sector-aligned area
                    // Entries are parsed in 512-byte sectors within the 1024-byte metadata chunk
                    ParseDentryChunk(zoneData, chunkOffset, dirInode.size, ref bytesRead, entries);
                }
            }

            dataIndex++;
        }

        return entries;
    }

    /// <summary>Parses directory entries from a metadata chunk.</summary>
    void ParseDentryChunk(byte[] data, uint chunkOffset, ulong dirSize, ref ulong bytesRead,
                          Dictionary<string, DirEntry> entries)
    {
        uint pos = chunkOffset;
        uint end = chunkOffset + PFS_META_SIZE;

        while(pos < end && pos + 8 <= (uint)data.Length && bytesRead < dirSize)
        {
            uint   inode = BitConverter.ToUInt32(data, (int)pos);
            byte   sub   = data[pos + 4];
            byte   pLen  = data[pos + 5];
            ushort aLen  = BitConverter.ToUInt16(data, (int)(pos + 6));

            uint allocLen = (uint)(aLen & 0x0FFF);
            var  mode     = (ushort)(aLen & 0xF000);

            if(allocLen == 0 || (allocLen & 3) != 0)
                break;

            if(pos + allocLen > end)
                break;

            bytesRead += allocLen;

            if(pLen > 0 && inode != 0 && pos + 8 + pLen <= (uint)data.Length)
            {
                string name = Encoding.ASCII.GetString(data, (int)(pos + 8), pLen);

                if(name != "." && name != ".." && !entries.ContainsKey(name))
                {
                    entries[name] = new DirEntry
                    {
                        Inode   = inode,
                        SubPart = sub,
                        Mode    = mode
                    };
                }
            }

            pos += allocLen;
        }
    }

    /// <summary>Wraps the data array index past PFS_INODE_MAX_BLOCKS using modulo 123.</summary>
    static uint FixIndex(uint index)
    {
        if(index < PFS_INODE_MAX_BLOCKS)
            return index;

        return (index - PFS_INODE_MAX_BLOCKS) % 123;
    }

    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted)
            return ErrorNumber.AccessDenied;

        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            node = new PfsDirNode
            {
                Path     = path,
                Contents = _rootDirectoryCache.Keys.ToArray(),
                Position = 0
            };

            return ErrorNumber.NoError;
        }

        string cutPath = path.StartsWith("/", StringComparison.Ordinal)
                             ? path[1..].ToLower(CultureInfo.CurrentUICulture)
                             : path.ToLower(CultureInfo.CurrentUICulture);

        if(_directoryCache.TryGetValue(cutPath, out Dictionary<string, DirEntry> currentDirectory))
        {
            node = new PfsDirNode
            {
                Path     = path,
                Contents = currentDirectory.Keys.ToArray(),
                Position = 0
            };

            return ErrorNumber.NoError;
        }

        string[] pieces = cutPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        currentDirectory = _rootDirectoryCache;
        string currentPath = "";

        for(var p = 0; p < pieces.Length; p++)
        {
            KeyValuePair<string, DirEntry> entry =
                currentDirectory.FirstOrDefault(t => t.Key.Equals(pieces[p],
                                                                   StringComparison.CurrentCultureIgnoreCase));

            if(string.IsNullOrEmpty(entry.Key))
                return ErrorNumber.NoSuchFile;

            if((entry.Value.Mode & (ushort)FileType.IFMT) != (ushort)FileType.IFDIR)
                return ErrorNumber.NotDirectory;

            currentPath = p == 0 ? pieces[0] : $"{currentPath}/{pieces[p]}";

            if(_directoryCache.TryGetValue(currentPath, out currentDirectory))
                continue;

            // Read the inode for this subdirectory
            ErrorNumber errno = ReadInode(entry.Value.Inode, entry.Value.SubPart, out Inode dirInode);

            if(errno != ErrorNumber.NoError)
                return errno;

            currentDirectory = ReadDirectory(dirInode);

            _directoryCache[currentPath] = currentDirectory;
        }

        if(currentDirectory is null)
            return ErrorNumber.NoSuchFile;

        node = new PfsDirNode
        {
            Path     = path,
            Contents = currentDirectory.Keys.ToArray(),
            Position = 0
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted)
            return ErrorNumber.AccessDenied;

        if(node is not PfsDirNode mynode)
            return ErrorNumber.InvalidArgument;

        if(mynode.Position < 0)
            return ErrorNumber.InvalidArgument;

        if(mynode.Position >= mynode.Contents.Length)
            return ErrorNumber.NoError;

        filename = mynode.Contents[mynode.Position++];

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not PfsDirNode mynode)
            return ErrorNumber.InvalidArgument;

        mynode.Position = -1;
        mynode.Contents = null;

        return ErrorNumber.NoError;
    }
}