// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : B-tree file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     File stat and inode reading methods.
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
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class BTRFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath is "/")
        {
            ErrorNumber errno = ReadInode(BTRFS_FIRST_FREE_OBJECTID, out InodeItem rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat            = InodeItemToFileEntryInfo(rootInode, BTRFS_FIRST_FREE_OBJECTID);
            stat.Attributes = FileAttributes.Directory;

            return ErrorNumber.NoError;
        }

        // Resolve path to an objectid
        ErrorNumber pathErrno = ResolvePath(normalizedPath, out ulong objectId);

        if(pathErrno != ErrorNumber.NoError) return pathErrno;

        ErrorNumber inodeErrno = ReadInode(objectId, out InodeItem inode);

        if(inodeErrno != ErrorNumber.NoError) return inodeErrno;

        stat = InodeItemToFileEntryInfo(inode, objectId);

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a filesystem path to its target objectid by walking the directory tree</summary>
    /// <param name="path">Absolute path to resolve (must start with /)</param>
    /// <param name="objectId">The objectid of the target file or directory</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ResolvePath(string path, out ulong objectId)
    {
        objectId = 0;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath is "/")
        {
            objectId = BTRFS_FIRST_FREE_OBJECTID;

            return ErrorNumber.NoError;
        }

        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        Dictionary<string, DirEntry> currentEntries = _rootDirectoryCache;

        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out DirEntry entry)) return ErrorNumber.NoSuchFile;

            // Last component — this is the target
            if(p == pathComponents.Length - 1)
            {
                objectId = entry.ObjectId;

                return ErrorNumber.NoError;
            }

            // Intermediate — must be a directory
            if(entry.Type != BTRFS_FT_DIR) return ErrorNumber.NotDirectory;

            ErrorNumber errno = ReadDirectoryContents(entry.ObjectId, out Dictionary<string, DirEntry> dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Reads the INODE_ITEM for the specified objectid from the FS tree</summary>
    /// <param name="objectId">The objectid (inode number) to read</param>
    /// <param name="inodeItem">The parsed InodeItem structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(ulong objectId, out InodeItem inodeItem)
    {
        inodeItem = default(InodeItem);

        ErrorNumber errno = ReadTreeBlock(_fsTreeRoot, out byte[] fsTreeData);

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