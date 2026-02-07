// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
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
public sealed partial class HPFS
{
    /// <summary>Reads an fnode from disk and optionally caches it.</summary>
    /// <param name="sector">Sector number of the fnode.</param>
    /// <param name="fnode">The fnode structure read.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadFNode(uint sector, out FNode fnode)
    {
        fnode = default(FNode);

        if(_fnodeCache.TryGetValue(sector, out fnode)) return ErrorNumber.NoError;

        ErrorNumber errno = _image.ReadSector(_partition.Start + sector, false, out byte[] fnodeSector, out _);

        if(errno != ErrorNumber.NoError) return errno;

        fnode = Marshal.ByteArrayToStructureLittleEndian<FNode>(fnodeSector);

        if(fnode.magic != FNODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid fnode magic at sector {0}: 0x{1:X8} (expected 0x{2:X8})",
                              sector,
                              fnode.magic,
                              FNODE_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        _fnodeCache[sector] = fnode;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a dnode from disk (4 consecutive sectors) and optionally caches it.</summary>
    /// <param name="sector">Starting sector number of the dnode.</param>
    /// <param name="dnode">The dnode structure read.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadDNode(uint sector, out DNode dnode)
    {
        dnode = default(DNode);

        if(_dnodeCache.TryGetValue(sector, out dnode)) return ErrorNumber.NoError;

        // DNodes are 4 sectors (2048 bytes) long
        ErrorNumber errno = _image.ReadSectors(_partition.Start + sector, false, 4, out byte[] dnodeSectors, out _);

        if(errno != ErrorNumber.NoError) return errno;

        dnode = Marshal.ByteArrayToStructureLittleEndian<DNode>(dnodeSectors);

        if(dnode.magic != DNODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid dnode magic at sector {0}: 0x{1:X8} (expected 0x{2:X8})",
                              sector,
                              dnode.magic,
                              DNODE_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        // Validate self-pointer
        if(dnode.self != sector)
        {
            AaruLogging.Debug(MODULE_NAME, "DNode self-pointer mismatch at sector {0}: self={1}", sector, dnode.self);

            return ErrorNumber.InvalidArgument;
        }

        _dnodeCache[sector] = dnode;

        return ErrorNumber.NoError;
    }

    /// <summary>Extracts B+ tree leaf nodes from an fnode's btree structure.</summary>
    /// <param name="header">B+ tree header.</param>
    /// <param name="data">B+ tree data bytes.</param>
    /// <returns>Array of leaf nodes.</returns>
    static BPlusLeafNode[] GetBPlusLeafNodes(BPlusHeader header, byte[] data)
    {
        if(header.IsInternal || data == null || header.n_used_nodes == 0) return [];

        var nodes = new BPlusLeafNode[header.n_used_nodes];

        for(var i = 0; i < header.n_used_nodes; i++)
        {
            int offset = i * 12; // Each leaf node is 12 bytes

            if(offset + 12 > data.Length) break;

            nodes[i] = Marshal.ByteArrayToStructureLittleEndian<BPlusLeafNode>(data, offset, 12);
        }

        return nodes;
    }

    /// <summary>Recursively caches directory entries from a dnode and its children.</summary>
    /// <param name="dnode">The dnode to process.</param>
    /// <param name="cache">Dictionary to cache entries into (filename to fnode mapping).</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber CacheDNodeEntries(DNode dnode, Dictionary<string, uint> cache)
    {
        // Parse directory entries from the dnode
        var offset    = 0;
        int endOffset = (int)dnode.first_free - 20; // first_free is offset from start of dnode

        while(offset < endOffset && offset + 31 < dnode.dirent.Length)
        {
            // Read directory entry header
            DirectoryEntry entry = Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(dnode.dirent, offset, 31);

            // Check for end of entries
            if(entry.length is < 32 or > 2048) break;

            // Skip special entries (first entry "." and last entry with 0xFF name)
            if(!entry.flags.HasFlag(DirectoryEntryFlags.First) && !entry.flags.HasFlag(DirectoryEntryFlags.Last))
            {
                // Extract filename
                if(entry.namelen > 0 && offset + 31 + entry.namelen <= dnode.dirent.Length)
                {
                    var nameBytes = new byte[entry.namelen];
                    Array.Copy(dnode.dirent, offset + 31, nameBytes, 0, entry.namelen);
                    string filename = _encoding.GetString(nameBytes);

                    // Cache the entry (case-insensitive key)
                    cache[filename.ToUpperInvariant()] = entry.fnode;

                    AaruLogging.Debug(MODULE_NAME, "Cached entry: {0} -> fnode {1}", filename, entry.fnode);
                }
            }

            // Check if there's a down pointer (B-tree child)
            if(entry.flags.HasFlag(DirectoryEntryFlags.Down))
            {
                // Down pointer is at the end of the entry, 4 bytes before next entry
                int downPtrOffset = offset + entry.length - 4;

                if(downPtrOffset + 4 <= dnode.dirent.Length)
                {
                    var downDnode = BitConverter.ToUInt32(dnode.dirent, downPtrOffset);

                    // Recursively process child dnode
                    ErrorNumber errno = ReadDNode(downDnode, out DNode childDnode);

                    if(errno == ErrorNumber.NoError) CacheDNodeEntries(childDnode, cache);
                }
            }

            // Move to next entry
            offset += entry.length;

            // Handle last entry
            if(entry.flags.HasFlag(DirectoryEntryFlags.Last)) break;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory entries from a directory fnode.</summary>
    /// <param name="fnode">Fnode sector number of the directory.</param>
    /// <param name="entries">Dictionary of filename to fnode sector.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadDirectoryEntries(uint fnode, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>();

        // Read the fnode
        ErrorNumber errno = ReadFNode(fnode, out FNode fnodeStruct);

        if(errno != ErrorNumber.NoError) return errno;

        // Validate it's a directory
        if(!fnodeStruct.IsDirectory)
        {
            AaruLogging.Debug(MODULE_NAME, "Fnode {0} is not a directory", fnode);

            return ErrorNumber.NotDirectory;
        }

        // Get the root dnode for this directory from the fnode's btree
        BPlusLeafNode[] leafNodes = GetBPlusLeafNodes(fnodeStruct.btree, fnodeStruct.btree_data);

        if(leafNodes.Length == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Directory fnode {0} has no extents", fnode);

            return ErrorNumber.InvalidArgument;
        }

        uint dnodeSector = leafNodes[0].disk_secno;

        // Read the root dnode
        errno = ReadDNode(dnodeSector, out DNode rootDnode);

        if(errno != ErrorNumber.NoError) return errno;

        // Cache all entries from the directory tree
        return CacheDNodeEntries(rootDnode, entries);
    }
}