// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
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

using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the filesystem used by Nintendo Gamecube and Wii discs</summary>
public sealed partial class NintendoPlugin
{
    /// <summary>Get the direct children of a directory FST entry within a partition</summary>
    /// <param name="partition">Partition containing the FST</param>
    /// <param name="dirIndex">FST index of the directory</param>
    /// <returns>Dictionary mapping child names to their FST indices</returns>
    static Dictionary<string, int> GetDirectoryEntries(PartitionInfo partition, int dirIndex)
    {
        Dictionary<string, int> entries = new();

        uint dirEnd = partition.FstEntries[dirIndex].SizeOrNext;

        for(int i = dirIndex + 1; i < (int)dirEnd; i++)
        {
            bool isDirectory = partition.FstEntries[i].TypeAndNameOffset >> 24 != 0;

            if(isDirectory)
            {
                // A directory is a direct child if its parent index points to dirIndex
                if((int)partition.FstEntries[i].OffsetOrParent == dirIndex) entries[partition.FstNames[i]] = i;

                // Skip past this directory's children — they belong to it, not to us
                i = (int)partition.FstEntries[i].SizeOrNext - 1; // -1 because loop will i++
            }
            else
            {
                // Files between dirIndex+1 and dirEnd that are not inside a subdirectory
                // are direct children. Since we skip past subdirectories above, any file
                // we encounter here is a direct child.
                entries[partition.FstNames[i]] = i;
            }
        }

        return entries;
    }

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            if(_multiPartition)
            {
                // Virtual root: list partition names as subdirectories
                node = new NintendoDirNode
                {
                    Path     = "/",
                    Contents = _partitions.Select(p => p.Name).ToArray(),
                    Position = 0
                };

                return ErrorNumber.NoError;
            }

            node = new NintendoDirNode
            {
                Path     = "/",
                Contents = _partitions[0].RootDirectoryCache.Keys.ToArray(),
                Position = 0
            };

            return ErrorNumber.NoError;
        }

        ErrorNumber errno = ResolvePath(path, out int partitionIndex, out int entryIndex);

        if(errno != ErrorNumber.NoError) return errno;

        // Virtual root shouldn't happen here (we handled "/" above)
        if(partitionIndex < 0) return ErrorNumber.InvalidArgument;

        PartitionInfo partition = _partitions[partitionIndex];

        // Virtual files are not directories
        if(entryIndex < 0) return ErrorNumber.NotDirectory;

        // Partition root
        if(entryIndex == 0)
        {
            node = new NintendoDirNode
            {
                Path     = path,
                Contents = partition.RootDirectoryCache.Keys.ToArray(),
                Position = 0
            };

            return ErrorNumber.NoError;
        }

        // Verify it's a directory entry in the FST
        if(partition.FstEntries[entryIndex].TypeAndNameOffset >> 24 == 0) return ErrorNumber.NotDirectory;

        // Get/cache subdirectory entries using in-partition path as cache key
        string inPartitionPath = GetInPartitionPath(path);

        if(!partition.DirectoryCache.TryGetValue(inPartitionPath, out Dictionary<string, int> entries))
        {
            entries                                   = GetDirectoryEntries(partition, entryIndex);
            partition.DirectoryCache[inPartitionPath] = entries;
        }

        node = new NintendoDirNode
        {
            Path     = path,
            Contents = entries.Keys.ToArray(),
            Position = 0
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not NintendoDirNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(myNode.Position >= myNode.Contents.Length) return ErrorNumber.NoError;

        filename = myNode.Contents[myNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not NintendoDirNode myNode) return ErrorNumber.InvalidArgument;

        myNode.Position = -1;
        myNode.Contents = null;

        return ErrorNumber.NoError;
    }

#endregion
}