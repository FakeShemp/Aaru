// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Inode reading methods.
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

using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
    /// <summary>Reads the INODE_ITEM for the specified objectid from the FS tree</summary>
    /// <param name="objectId">The objectid (inode number) to read</param>
    /// <param name="treeRoot">The logical byte address of the tree to read from</param>
    /// <param name="inodeItem">The parsed InodeItem structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(ulong objectId, ulong treeRoot, out InodeItem inodeItem)
    {
        inodeItem = default(InodeItem);

        ErrorNumber errno = ReadTreeBlock(treeRoot, out byte[] fsTreeData);

        if(errno != ErrorNumber.NoError) return errno;

        Header fsTreeHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(fsTreeData);

        return WalkTreeForInodeItem(fsTreeData, fsTreeHeader, objectId, out inodeItem);
    }

    /// <summary>Walks a tree node recursively to find the INODE_ITEM for the specified objectid</summary>
    /// <param name="nodeData">Raw tree node data</param>
    /// <param name="header">Parsed node header</param>
    /// <param name="objectId">The objectid to search for</param>
    /// <param name="inodeItem">The parsed InodeItem if found</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber WalkTreeForInodeItem(byte[] nodeData, in Header header, ulong objectId, out InodeItem inodeItem)
    {
        inodeItem = default(InodeItem);
        int headerSize = Marshal.SizeOf<Header>();

        if(header.level == 0) return ExtractInodeItemFromLeaf(nodeData, header, objectId, out inodeItem);

        // Internal node — follow all key pointers
        int keyPtrSize = Marshal.SizeOf<KeyPtr>();

        for(uint i = 0; i < header.nritems; i++)
        {
            int keyPtrOffset = headerSize + (int)i * keyPtrSize;

            if(keyPtrOffset + keyPtrSize > nodeData.Length) break;

            KeyPtr keyPtr = Marshal.ByteArrayToStructureLittleEndian<KeyPtr>(nodeData, keyPtrOffset, keyPtrSize);

            ErrorNumber errno = ReadTreeBlock(keyPtr.blockptr, out byte[] childData);

            if(errno != ErrorNumber.NoError) continue;

            Header childHeader = Marshal.ByteArrayToStructureLittleEndian<Header>(childData);

            errno = WalkTreeForInodeItem(childData, childHeader, objectId, out inodeItem);

            if(errno == ErrorNumber.NoError) return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Extracts an INODE_ITEM from a leaf node for the specified objectid</summary>
    /// <param name="leafData">Raw leaf node data</param>
    /// <param name="header">Parsed leaf header</param>
    /// <param name="objectId">The objectid to search for</param>
    /// <param name="inodeItem">The parsed InodeItem if found</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ExtractInodeItemFromLeaf(byte[] leafData, in Header header, ulong objectId, out InodeItem inodeItem)
    {
        inodeItem = default(InodeItem);
        int itemSize      = Marshal.SizeOf<Item>();
        int headerSize    = Marshal.SizeOf<Header>();
        int inodeItemSize = Marshal.SizeOf<InodeItem>();

        for(uint i = 0; i < header.nritems; i++)
        {
            int itemOffset = headerSize + (int)i * itemSize;

            if(itemOffset + itemSize > leafData.Length) break;

            Item item = Marshal.ByteArrayToStructureLittleEndian<Item>(leafData, itemOffset, itemSize);

            if(item.key.objectid != objectId || item.key.type != BTRFS_INODE_ITEM_KEY) continue;

            int dataOffset = headerSize + (int)item.offset;

            if(dataOffset + inodeItemSize > leafData.Length) continue;

            inodeItem = Marshal.ByteArrayToStructureLittleEndian<InodeItem>(leafData, dataOffset, inodeItemSize);

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts a btrfs InodeItem to a FileEntryInfo</summary>
    /// <param name="inode">The btrfs inode item</param>
    /// <param name="objectId">The objectid (inode number)</param>
    /// <returns>Populated FileEntryInfo</returns>
    static FileEntryInfo InodeItemToFileEntryInfo(in InodeItem inode, ulong objectId)
    {
        var info = new FileEntryInfo
        {
            Inode               = objectId,
            Links               = inode.nlink,
            Length              = (long)inode.size,
            BlockSize           = (long)inode.nbytes > 0 ? (long)inode.nbytes : (long)inode.size,
            Blocks              = inode.nbytes       > 0 ? (long)((inode.nbytes + 4095) / 4096) : 0,
            UID                 = inode.uid,
            GID                 = inode.gid,
            Mode                = inode.mode & 0x0FFF,
            AccessTimeUtc       = DateHandlers.UnixUnsignedToDateTime((uint)inode.atime.sec, inode.atime.nsec),
            LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime((uint)inode.mtime.sec, inode.mtime.nsec),
            StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime((uint)inode.ctime.sec, inode.ctime.nsec),
            CreationTimeUtc     = DateHandlers.UnixUnsignedToDateTime((uint)inode.otime.sec, inode.otime.nsec)
        };

        // Determine file type from S_IFMT bits in mode
        info.Attributes = (inode.mode & S_IFMT) switch
                          {
                              S_IFDIR  => FileAttributes.Directory,
                              S_IFREG  => FileAttributes.File,
                              S_IFLNK  => FileAttributes.Symlink,
                              S_IFCHR  => FileAttributes.CharDevice,
                              S_IFBLK  => FileAttributes.BlockDevice,
                              S_IFIFO  => FileAttributes.FIFO,
                              S_IFSOCK => FileAttributes.Socket,
                              _        => FileAttributes.File
                          };

        // Map btrfs inode flags to FileAttributes
        if((inode.flags & BTRFS_INODE_NODATACOW) != 0) info.Attributes |= FileAttributes.NoCopyOnWrite;
        if((inode.flags & BTRFS_INODE_COMPRESS)  != 0) info.Attributes |= FileAttributes.Compressed;
        if((inode.flags & BTRFS_INODE_IMMUTABLE) != 0) info.Attributes |= FileAttributes.Immutable;
        if((inode.flags & BTRFS_INODE_APPEND)    != 0) info.Attributes |= FileAttributes.AppendOnly;
        if((inode.flags & BTRFS_INODE_SYNC)      != 0) info.Attributes |= FileAttributes.Sync;
        if((inode.flags & BTRFS_INODE_NOATIME)   != 0) info.Attributes |= FileAttributes.NoAccessTime;
        if((inode.flags & BTRFS_INODE_NODATASUM) != 0) info.Attributes |= FileAttributes.NoScrub;

        // Device number for character/block devices
        if((inode.mode & S_IFMT) is S_IFCHR or S_IFBLK) info.DeviceNo = inode.rdev;

        return info;
    }
}