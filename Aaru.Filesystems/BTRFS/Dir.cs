// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Directory reading and enumeration methods.
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
using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
    /// <summary>Loads the root directory of the default subvolume from the FS tree and caches its entries</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory from FS tree...");

        _rootDirectoryCache = new Dictionary<string, DirEntry>();

        ErrorNumber errno = ReadTreeBlock(_fsTreeRoot, out byte[] fsTreeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading FS tree root: {0}", errno);

            return errno;
        }

        Header fsTreeHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(fsTreeData);

        AaruLogging.Debug(MODULE_NAME, "FS tree root level: {0}, items: {1}", fsTreeHeader.level, fsTreeHeader.nritems);

        // The root directory inode is BTRFS_FIRST_FREE_OBJECTID (256) in the default subvolume
        // We search for DIR_INDEX items for objectid 256 to get directory entries sorted by index
        errno = WalkTreeForDirEntries(fsTreeData, fsTreeHeader, BTRFS_FIRST_FREE_OBJECTID);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error walking FS tree for directory entries: {0}", errno);

            return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Walks a tree node recursively to find all DIR_INDEX items for the specified directory objectid. DIR_INDEX items
    ///     are preferred over DIR_ITEM because they provide entries sorted by insertion order (index).
    /// </summary>
    /// <param name="nodeData">Raw tree node data</param>
    /// <param name="header">Parsed node header</param>
    /// <param name="dirObjectId">The objectid of the directory (inode) to enumerate</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber WalkTreeForDirEntries(byte[] nodeData, in Header header, ulong dirObjectId)
    {
        int headerSize = Marshal.SizeOf<Header>();

        if(header.level == 0) return ExtractDirEntriesFromLeaf(nodeData, header, dirObjectId);

        // Internal node - follow all key pointers
        int keyPtrSize = Marshal.SizeOf<KeyPtr>();

        for(uint i = 0; i < header.nritems; i++)
        {
            int keyPtrOffset = headerSize + (int)i * keyPtrSize;

            if(keyPtrOffset + keyPtrSize > nodeData.Length) break;

            KeyPtr keyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, keyPtrOffset, keyPtrSize);

            ErrorNumber errno = ReadTreeBlock(keyPtr.blockptr, out byte[] childData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading FS tree child at {0}: {1}", keyPtr.blockptr, errno);

                continue;
            }

            Header childHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(childData);

            errno = WalkTreeForDirEntries(childData, childHeader, dirObjectId);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Extracts directory entries from a leaf node for the specified directory objectid. Looks for DIR_INDEX items and
    ///     caches the directory entries.
    /// </summary>
    /// <param name="leafData">Raw leaf node data</param>
    /// <param name="header">Parsed leaf header</param>
    /// <param name="dirObjectId">The objectid of the directory to enumerate</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ExtractDirEntriesFromLeaf(byte[] leafData, in Header header, ulong dirObjectId)
    {
        int itemSize    = Marshal.SizeOf<Item>();
        int headerSize  = Marshal.SizeOf<Header>();
        int dirItemSize = Marshal.SizeOf<DirItem>();

        for(uint i = 0; i < header.nritems; i++)
        {
            int itemOffset = headerSize + (int)i * itemSize;

            if(itemOffset + itemSize > leafData.Length) break;

            Item item = Marshal.ByteArrayToStructureLittleEndian<Item>(leafData, itemOffset, itemSize);

            // We want DIR_INDEX items for our directory objectid
            if(item.key.objectid != dirObjectId || item.key.type != BTRFS_DIR_INDEX_KEY) continue;

            int dataOffset = headerSize + (int)item.offset;

            if(dataOffset + dirItemSize > leafData.Length) continue;

            DirItem dirItem = Marshal.ByteArrayToStructureLittleEndian<DirItem>(leafData, dataOffset, dirItemSize);

            int nameOffset = dataOffset + dirItemSize;

            if(nameOffset + dirItem.name_len > leafData.Length) continue;

            string name = _encoding.GetString(leafData, nameOffset, dirItem.name_len);

            // Skip . and .. entries
            if(name is "." or "..") continue;

            _rootDirectoryCache[name] = new DirEntry
            {
                ObjectId = dirItem.location.objectid,
                Type     = dirItem.type,
                Index    = item.key.offset
            };

            AaruLogging.Debug(MODULE_NAME,
                              "Root directory entry: \"{0}\" -> objectid {1}, type {2}",
                              name,
                              dirItem.location.objectid,
                              dirItem.type);
        }

        return ErrorNumber.NoError;
    }
}