// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Extended attribute support methods.
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber pathErrno = ResolvePath(path, out ulong objectId, out ulong treeRoot);

        if(pathErrno != ErrorNumber.NoError) return pathErrno;

        ErrorNumber errno = ReadTreeBlock(treeRoot, out byte[] fsTreeData);

        if(errno != ErrorNumber.NoError) return errno;

        Header fsTreeHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(fsTreeData);

        List<XattrEntry> entries = [];
        errno = WalkTreeForXattrs(fsTreeData, fsTreeHeader, objectId, entries);

        if(errno != ErrorNumber.NoError) return errno;

        xattrs = new List<string>(entries.Count);

        foreach(XattrEntry entry in entries) xattrs.Add(entry.Name);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber pathErrno = ResolvePath(path, out ulong objectId, out ulong treeRoot);

        if(pathErrno != ErrorNumber.NoError) return pathErrno;

        ErrorNumber errno = ReadTreeBlock(treeRoot, out byte[] fsTreeData);

        if(errno != ErrorNumber.NoError) return errno;

        Header fsTreeHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(fsTreeData);

        List<XattrEntry> entries = [];
        errno = WalkTreeForXattrs(fsTreeData, fsTreeHeader, objectId, entries);

        if(errno != ErrorNumber.NoError) return errno;

        foreach(XattrEntry entry in entries)
        {
            if(entry.Name != xattr) continue;

            buf = entry.Value;

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>Walks a tree node recursively to collect all XATTR_ITEM entries for the specified objectid</summary>
    /// <param name="nodeData">Raw tree node data</param>
    /// <param name="header">Parsed node header</param>
    /// <param name="objectId">The objectid to search for</param>
    /// <param name="entries">List to add found xattr entries to</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber WalkTreeForXattrs(byte[] nodeData, in Header header, ulong objectId, List<XattrEntry> entries)
    {
        int headerSize = Marshal.SizeOf<Header>();

        if(header.level == 0) return ExtractXattrsFromLeaf(nodeData, header, objectId, entries);

        int keyPtrSize = Marshal.SizeOf<KeyPtr>();

        BinarySearchNodeRange(nodeData, header, objectId, out int startIdx, out int endIdx);

        for(int i = startIdx; i < endIdx; i++)
        {
            int keyPtrOffset = headerSize + i * keyPtrSize;

            if(keyPtrOffset + keyPtrSize > nodeData.Length) break;

            KeyPtr keyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, keyPtrOffset, keyPtrSize);

            ErrorNumber errno = ReadTreeBlock(keyPtr.blockptr, out byte[] childData);

            if(errno != ErrorNumber.NoError) continue;

            Header childHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(childData);

            WalkTreeForXattrs(childData, childHeader, objectId, entries);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Extracts all XATTR_ITEM entries from a leaf node for the specified objectid</summary>
    /// <param name="leafData">Raw leaf node data</param>
    /// <param name="header">Parsed leaf header</param>
    /// <param name="objectId">The objectid to search for</param>
    /// <param name="entries">List to add found xattr entries to</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ExtractXattrsFromLeaf(byte[] leafData, in Header header, ulong objectId, List<XattrEntry> entries)
    {
        int itemSize    = Marshal.SizeOf<Item>();
        int headerSize  = Marshal.SizeOf<Header>();
        int dirItemSize = Marshal.SizeOf<DirItem>();

        uint startItem = BinarySearchLeaf(leafData, header, objectId);

        for(uint i = startItem; i < header.nritems; i++)
        {
            int itemOffset = headerSize + (int)i * itemSize;

            if(itemOffset + itemSize > leafData.Length) break;

            Item item = Marshal.ByteArrayToStructureLittleEndian<Item>(leafData, itemOffset, itemSize);

            if(item.key.objectid > objectId) break;

            if(item.key.objectid != objectId || item.key.type != BTRFS_XATTR_ITEM_KEY) continue;

            int dataOffset = headerSize + (int)item.offset;
            var remaining  = (int)item.size;

            // Multiple xattr entries can be packed in a single item (hash collision)
            while(remaining >= dirItemSize)
            {
                if(dataOffset + dirItemSize > leafData.Length) break;

                DirItem dirItem = Marshal.ByteArrayToStructureLittleEndian<DirItem>(leafData, dataOffset, dirItemSize);

                int nameOffset = dataOffset  + dirItemSize;
                int dataStart  = nameOffset  + dirItem.name_len;
                int entryTotal = dirItemSize + dirItem.name_len + dirItem.data_len;

                if(dataOffset + entryTotal > leafData.Length) break;

                string name = _encoding.GetString(leafData, nameOffset, dirItem.name_len);

                var value = new byte[dirItem.data_len];

                if(dirItem.data_len > 0) Array.Copy(leafData, dataStart, value, 0, dirItem.data_len);

                entries.Add(new XattrEntry
                {
                    Name  = name,
                    Value = value
                });

                dataOffset += entryTotal;
                remaining  -= entryTotal;
            }
        }

        return ErrorNumber.NoError;
    }
}