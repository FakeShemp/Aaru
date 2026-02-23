// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser filesystem plugin
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
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class Reiser
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath is "/")
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new ReiserDirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        // Subdirectory traversal
        string stripped = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        string[] components = stripped.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(components.Length == 0) return ErrorNumber.InvalidArgument;

        AaruLogging.Debug(MODULE_NAME, "OpenDir: traversing {0} components", components.Length);

        // Start from root directory cache
        Dictionary<string, (uint dirId, uint objectId)> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < components.Length; i++)
        {
            string component = components[i];

            AaruLogging.Debug(MODULE_NAME, "OpenDir: navigating to '{0}'", component);

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out (uint dirId, uint objectId) target))
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Verify it's a directory
            ErrorNumber errno = ReadObjectMode(target.dirId, target.objectId, out ushort mode);

            if(errno != ErrorNumber.NoError) return errno;

            if((mode & S_IFMT) != S_IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "OpenDir: '{0}' is not a directory (mode=0x{1:X4})", component, mode);

                return ErrorNumber.NotDirectory;
            }

            // Read this subdirectory's entries
            errno = ReadDirectoryEntries(target.dirId,
                                         target.objectId,
                                         out Dictionary<string, (uint dirId, uint objectId)> subEntries);

            if(errno != ErrorNumber.NoError) return errno;

            // Filter . and ..
            var filtered = new Dictionary<string, (uint dirId, uint objectId)>();

            foreach(KeyValuePair<string, (uint dirId, uint objectId)> entry in subEntries)
            {
                if(entry.Key is "." or "..") continue;

                filtered[entry.Key] = entry.Value;
            }

            // Last component — this is the directory being opened
            if(i == components.Length - 1)
            {
                node = new ReiserDirNode
                {
                    Path     = normalizedPath,
                    Position = 0,
                    Entries  = filtered.Keys.ToArray()
                };

                AaruLogging.Debug(MODULE_NAME,
                                  "OpenDir: opened '{0}' with {1} entries",
                                  normalizedPath,
                                  filtered.Count);

                return ErrorNumber.NoError;
            }

            // Intermediate component — descend
            currentEntries = filtered;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not ReiserDirNode reiserNode) return ErrorNumber.InvalidArgument;

        reiserNode.Position = -1;
        reiserNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not ReiserDirNode reiserNode) return ErrorNumber.InvalidArgument;

        if(reiserNode.Position < 0) return ErrorNumber.InvalidArgument;

        // End of directory
        if(reiserNode.Position >= reiserNode.Entries.Length) return ErrorNumber.NoError;

        filename = reiserNode.Entries[reiserNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the mode (file type and permissions) from an object's stat data</summary>
    /// <param name="dirId">Directory (packing locality) id</param>
    /// <param name="objectId">Object id</param>
    /// <param name="mode">The mode field from the stat data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadObjectMode(uint dirId, uint objectId, out ushort mode)
    {
        mode = 0;

        ErrorNumber errno = SearchByKey(dirId, objectId, 0, TYPE_STAT_DATA, out byte[] leaf, out int index);

        if(errno != ErrorNumber.NoError) return errno;

        if(index < 0) return ErrorNumber.NoSuchFile;

        ItemHead ih  = ReadItemHead(leaf, BLKH_SIZE + index * IH_SIZE);
        int      ver = GetItemKeyVersion(ih.ih_version);

        if(ih.ih_item_location + ih.ih_item_len > leaf.Length) return ErrorNumber.InvalidArgument;

        if(ver == KEY_FORMAT_3_6 && ih.ih_item_len >= Marshal.SizeOf<StatDataV2>())
        {
            StatDataV2 sd =
                Marshal.ByteArrayToStructureLittleEndian<StatDataV2>(leaf,
                                                                     ih.ih_item_location,
                                                                     Marshal.SizeOf<StatDataV2>());

            mode = sd.sd_mode;
        }
        else
        {
            StatDataV1 sd =
                Marshal.ByteArrayToStructureLittleEndian<StatDataV1>(leaf,
                                                                     ih.ih_item_location,
                                                                     Marshal.SizeOf<StatDataV1>());

            mode = sd.sd_mode;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads all directory entry items for a given object and returns the entries</summary>
    /// <param name="dirId">Directory (packing locality) id of the directory</param>
    /// <param name="objectId">Object id of the directory</param>
    /// <param name="entries">Parsed directory entries (filename -> (dirId, objectId) of target)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(uint                                                dirId, uint objectId,
                                     out Dictionary<string, (uint dirId, uint objectId)> entries)
    {
        entries = new Dictionary<string, (uint dirId, uint objectId)>();

        // Search for the first directory item at DOT_OFFSET
        ErrorNumber errno = SearchByKey(dirId,
                                        objectId,
                                        DOT_OFFSET,
                                        TYPE_DIRENTRY,
                                        out byte[] leafBlock,
                                        out int itemIndex);

        if(errno != ErrorNumber.NoError) return errno;

        if(itemIndex < 0) return ErrorNumber.NoSuchFile;

        // Track the last offset we've processed to search for continuation items
        ulong lastOffset = 0;

        // Read all directory items for this object from this leaf
        BlockHead blkHead = ReadBlockHead(leafBlock);

        for(int i = itemIndex; i < blkHead.blk_nr_item; i++)
        {
            ItemHead ih        = ReadItemHead(leafBlock, BLKH_SIZE + i * IH_SIZE);
            int      ihVersion = GetItemKeyVersion(ih.ih_version);

            // Check this item still belongs to our directory object
            if(ih.ih_key.k_dir_id != dirId || ih.ih_key.k_objectid != objectId) break;

            // Check it's a directory entry item
            int ihType = GetKeyType(ih.ih_key, ihVersion);

            if(ihType != TYPE_DIRENTRY) break;

            if(ih.ih_item_location + ih.ih_item_len > leafBlock.Length) continue;

            // Parse each directory entry header within this item
            ParseDirectoryItem(leafBlock,
                               ih.ih_item_location,
                               ih.ih_item_len,
                               ih.ih_free_space_or_entry_count,
                               entries);

            lastOffset = GetKeyOffset(ih.ih_key, ihVersion);
        }

        // There may be additional directory items in subsequent leaf nodes.
        // Keep searching with increasing offsets until no more directory items are found.
        while(true)
        {
            errno = SearchByKey(dirId, objectId, lastOffset + 1, TYPE_DIRENTRY, out byte[] nextLeaf, out int nextIndex);

            if(errno != ErrorNumber.NoError || nextIndex < 0) break;

            BlockHead nextBlkHead = ReadBlockHead(nextLeaf);
            var       foundMore   = false;

            for(int i = nextIndex; i < nextBlkHead.blk_nr_item; i++)
            {
                ItemHead nextIh        = ReadItemHead(nextLeaf, BLKH_SIZE + i * IH_SIZE);
                int      nextIhVersion = GetItemKeyVersion(nextIh.ih_version);

                if(nextIh.ih_key.k_dir_id != dirId || nextIh.ih_key.k_objectid != objectId) break;

                int nextIhType = GetKeyType(nextIh.ih_key, nextIhVersion);

                if(nextIhType != TYPE_DIRENTRY) break;

                ulong nextOffset = GetKeyOffset(nextIh.ih_key, nextIhVersion);

                if(nextOffset <= lastOffset) break;

                if(nextIh.ih_item_location + nextIh.ih_item_len > nextLeaf.Length) continue;

                ParseDirectoryItem(nextLeaf,
                                   nextIh.ih_item_location,
                                   nextIh.ih_item_len,
                                   nextIh.ih_free_space_or_entry_count,
                                   entries);

                lastOffset = nextOffset;
                foundMore  = true;
            }

            if(!foundMore) break;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses directory entry headers and names from a directory item body</summary>
    /// <param name="blockData">Block data containing the item</param>
    /// <param name="itemLocation">Offset of the item body within the block</param>
    /// <param name="itemLen">Length of the item body</param>
    /// <param name="entryCount">Number of directory entry headers in this item</param>
    /// <param name="entries">Dictionary to add parsed entries to</param>
    void ParseDirectoryItem(byte[] blockData, int itemLocation, int itemLen, int entryCount,
                            Dictionary<string, (uint dirId, uint objectId)> entries)
    {
        if(entryCount == 0) return;

        // Directory item body layout:
        // [DEH_0][DEH_1]...[DEH_N-1]  (entry headers, packed at start of item body)
        // ...name data...              (names stored at offsets given by deh_location, relative to item start)
        // Names are stored from the end of the item backward.

        for(var e = 0; e < entryCount; e++)
        {
            int dehOff = itemLocation + e * DEH_SIZE;

            if(dehOff + DEH_SIZE > blockData.Length) break;

            DirectoryEntryHead deh = ReadDirEntryHead(blockData, dehOff);

            // Check if entry is visible
            if((deh.deh_state & 1 << DEH_VISIBLE) == 0) continue;

            // Calculate name boundaries
            int nameStart = itemLocation + deh.deh_location;
            int nameEnd;

            if(e == 0)
            {
                // First entry (entries are stored backward):
                // name extends from deh_location to end of item
                nameEnd = itemLocation + itemLen;
            }
            else
            {
                // Name ends at the previous entry's deh_location
                DirectoryEntryHead prevDeh = ReadDirEntryHead(blockData, itemLocation + (e - 1) * DEH_SIZE);
                nameEnd = itemLocation + prevDeh.deh_location;
            }

            if(nameStart < 0 || nameStart >= blockData.Length || nameEnd <= nameStart) continue;

            int nameLen = Math.Min(nameEnd - nameStart, REISERFS_MAX_NAME);

            if(nameStart + nameLen > blockData.Length) nameLen = blockData.Length - nameStart;

            if(nameLen <= 0) continue;

            var nameBytes = new byte[nameLen];
            Array.Copy(blockData, nameStart, nameBytes, 0, nameLen);

            string name = StringHandlers.CToString(nameBytes, _encoding);

            if(string.IsNullOrEmpty(name)) continue;

            entries[name] = (deh.deh_dir_id, deh.deh_objectid);
        }
    }

    /// <summary>Reads a DirectoryEntryHead struct from raw block data at the given offset</summary>
    static DirectoryEntryHead ReadDirEntryHead(byte[] data, int offset) =>
        Marshal.ByteArrayToStructureLittleEndian<DirectoryEntryHead>(data, offset, DEH_SIZE);
}