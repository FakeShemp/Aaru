// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = LookupInode(path, out ulong inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ReadInode(inodeNumber, out Dinode inode);

        if(errno != ErrorNumber.NoError) return errno;

        xattrs = new List<string>();

        if(inode.di_forkoff == 0 || inode.di_aformat <= 0) return ErrorNumber.NoError;

        // Read attributes based on format
        errno = inode.di_aformat switch
                {
                    1 => ReadShortformAttrs(inodeNumber, inode, xattrs),
                    2 => ReadLeafAttrs(inodeNumber, inode, xattrs),
                    3 => ReadNodeAttrs(inodeNumber, inode, xattrs),
                    _ => ErrorNumber.NoError
                };

        return errno;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = LookupInode(path, out ulong inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ReadInode(inodeNumber, out Dinode inode);

        if(errno != ErrorNumber.NoError) return errno;

        if(inode.di_forkoff == 0 || inode.di_aformat <= 0) return ErrorNumber.NoSuchExtendedAttribute;

        List<XfsXattrEntry> sfAttrs = null;

        errno = inode.di_aformat switch
                {
                    1 => ReadShortformXattrs(inodeNumber, inode, out sfAttrs),
                    2 => ReadLeafXattrs(inodeNumber, inode, out sfAttrs),
                    3 => ReadNodeXattrs(inodeNumber, inode, out sfAttrs),
                    _ => ErrorNumber.NoError
                };

        if(errno != ErrorNumber.NoError) return errno;

        foreach(XfsXattrEntry entry in sfAttrs)
        {
            if(entry.FullName == xattr)
            {
                buf = entry.Value;

                return ErrorNumber.NoError;
            }
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    // Helper methods for reading attribute formats
    ErrorNumber ReadShortformAttrs(ulong inodeNumber, Dinode inode, List<string> xattrs)
    {
        ErrorNumber errno = ReadShortformXattrs(inodeNumber, inode, out List<XfsXattrEntry> entries);

        if(errno != ErrorNumber.NoError) return errno;

        foreach(XfsXattrEntry entry in entries) xattrs.Add(entry.FullName);

        return ErrorNumber.NoError;
    }

    ErrorNumber ReadLeafAttrs(ulong inodeNumber, Dinode inode, List<string> xattrs)
    {
        ErrorNumber errno = ReadLeafXattrs(inodeNumber, inode, out List<XfsXattrEntry> entries);

        if(errno != ErrorNumber.NoError) return errno;

        foreach(XfsXattrEntry entry in entries) xattrs.Add(entry.FullName);

        return ErrorNumber.NoError;
    }

    ErrorNumber ReadNodeAttrs(ulong inodeNumber, Dinode inode, List<string> xattrs)
    {
        ErrorNumber errno = ReadNodeXattrs(inodeNumber, inode, out List<XfsXattrEntry> entries);

        if(errno != ErrorNumber.NoError) return errno;

        foreach(XfsXattrEntry entry in entries) xattrs.Add(entry.FullName);

        return ErrorNumber.NoError;
    }

    // Core read methods that return XfsXattrEntry list
    ErrorNumber ReadShortformXattrs(ulong inodeNumber, Dinode inode, out List<XfsXattrEntry> entries)
    {
        entries = new List<XfsXattrEntry>();

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int dinodeSize     = inode.di_version >= 3 ? 176 : 100;
        int attrForkOffset = dinodeSize + (inode.di_forkoff << 3);

        if(attrForkOffset + 4 > rawInode.Length) return ErrorNumber.NoError;

        int  pos     = attrForkOffset;
        var  totsize = BigEndianBitConverter.ToUInt16(rawInode, pos);
        byte count   = rawInode[pos + 2];
        pos += 4;

        for(var i = 0; i < count; i++)
        {
            if(pos + 3 > rawInode.Length) break;

            byte nameLen  = rawInode[pos];
            byte valueLen = rawInode[pos + 1];
            byte flags    = rawInode[pos + 2];
            pos += 3;

            if(pos + nameLen + valueLen > rawInode.Length) break;

            string name = _encoding.GetString(rawInode, pos, nameLen);
            pos += nameLen;

            var value = new byte[valueLen];
            Array.Copy(rawInode, pos, value, 0, valueLen);
            pos += valueLen;

            entries.Add(new XfsXattrEntry
            {
                Name     = name,
                Value    = value,
                Flags    = flags,
                FullName = PrefixAttributeName(name, flags)
            });
        }

        return ErrorNumber.NoError;
    }

    ErrorNumber ReadLeafXattrs(ulong inodeNumber, Dinode inode, out List<XfsXattrEntry> entries)
    {
        entries = new List<XfsXattrEntry>();

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int dinodeSize     = inode.di_version >= 3 ? 176 : 100;
        int attrForkOffset = dinodeSize + (inode.di_forkoff << 3);

        // Read extent count for attr fork, handling NREXT64 properly
        // When NREXT64 is set, di_big_anextents (uint32) overlaps the di_nextents field at offset 76,
        // and di_anextents at offset 80 becomes padding (zero).
        uint extentCount;

        if(_v3Inodes && (inode.di_flags2 & XFS_DIFLAG2_NREXT64) != 0)
            extentCount = inode.di_nextents;
        else
            extentCount = inode.di_anextents;

        if(!TryFindAttrLeafBlock(rawInode, attrForkOffset, extentCount, inode, out byte[] leafBlockData))
            return ErrorNumber.NoError;

        return ParseAttrLeaf(leafBlockData, entries, rawInode, attrForkOffset, inode);
    }

    /// <summary>Tries to find and read a valid attribute leaf block from extents</summary>
    bool TryFindAttrLeafBlock(byte[]     rawInode, int attrForkOffset, uint extentCount, Dinode inode,
                              out byte[] leafBlockData)
    {
        leafBlockData = null;

        if(inode.di_aformat != XFS_DINODE_FMT_EXTENTS) return false;

        int pos = attrForkOffset;

        for(var i = 0; i < extentCount; i++)
        {
            if(pos + 16 > rawInode.Length) break;

            var l0 = BigEndianBitConverter.ToUInt64(rawInode, pos);
            var l1 = BigEndianBitConverter.ToUInt64(rawInode, pos + 8);
            pos += 16;

            DecodeBmbtRec(l0, l1, out _, out ulong startBlock, out uint blockCount, out _);

            for(ulong blockOffset = 0; blockOffset < blockCount; blockOffset++)
            {
                ulong physBlock = startBlock + blockOffset;

                ErrorNumber errno = ReadBlock(physBlock, out byte[] blockData);

                if(errno != ErrorNumber.NoError) continue;

                if(blockData.Length < 10) continue;

                var magic4 = BigEndianBitConverter.ToUInt16(blockData, 4);
                var magic8 = BigEndianBitConverter.ToUInt16(blockData, 8);

                if(magic4 == XFS_ATTR_LEAF_MAGIC  ||
                   magic4 == XFS_ATTR3_LEAF_MAGIC ||
                   magic8 == XFS_ATTR_LEAF_MAGIC  ||
                   magic8 == XFS_ATTR3_LEAF_MAGIC)
                {
                    leafBlockData = blockData;

                    return true;
                }
            }
        }

        return false;
    }

    ErrorNumber ReadNodeXattrs(ulong inodeNumber, Dinode inode, out List<XfsXattrEntry> entries)
    {
        entries = new List<XfsXattrEntry>();

        ErrorNumber errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int dinodeSize     = inode.di_version >= 3 ? 176 : 100;
        int attrForkOffset = dinodeSize + (inode.di_forkoff << 3);

        if(attrForkOffset + 4 > rawInode.Length) return ErrorNumber.NoError;

        int pos     = attrForkOffset;
        var level   = BigEndianBitConverter.ToUInt16(rawInode, pos);
        var numrecs = BigEndianBitConverter.ToUInt16(rawInode, pos + 2);

        if(level == 0)
        {
            // Direct extent records in btree root
            int recPos = pos + 4;

            for(var i = 0; i < numrecs; i++)
            {
                if(recPos + 16 > rawInode.Length) break;

                var l0 = BigEndianBitConverter.ToUInt64(rawInode, recPos);
                var l1 = BigEndianBitConverter.ToUInt64(rawInode, recPos + 8);
                recPos += 16;

                DecodeBmbtRec(l0,
                              l1,
                              out ulong startOff,
                              out ulong startBlock,
                              out uint blockCount,
                              out bool unwritten);

                // Map logical block 0 to physical
                if(startOff == 0)
                {
                    errno = ReadBlock(startBlock, out byte[] blockData);
                    if(errno == ErrorNumber.NoError) ParseAttrLeaf(blockData, entries, rawInode, attrForkOffset, inode);

                    break;
                }
            }
        }
        else
        {
            // Traverse btree to find leaf blocks
            TraverseBtreeForAttrs(rawInode, attrForkOffset, inode, entries);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Traverses btree structure to find and parse attribute leaf blocks</summary>
    void TraverseBtreeForAttrs(byte[] rawInode, int attrForkOffset, Dinode inode, List<XfsXattrEntry> entries)
    {
        int pos = attrForkOffset;

        if(pos + 4 > rawInode.Length) return;

        var level   = BigEndianBitConverter.ToUInt16(rawInode, pos);
        var numrecs = BigEndianBitConverter.ToUInt16(rawInode, pos + 2);

        // Linux: xfs_bmdr_maxrecs(dblocklen, 0) = (dblocklen - sizeof(xfs_bmdr_block_t)) / (sizeof(key) + sizeof(ptr))
        // Linux: xfs_bmdr_ptr_addr(block, i, maxrecs) = block + sizeof(bmdr_block) + maxrecs * sizeof(key) + (i-1) * sizeof(ptr)
        int attrForkSize = rawInode.Length - attrForkOffset;
        int maxrecs      = (attrForkSize - 4) / 16;
        int ptrsStart    = pos + 4 + maxrecs * 8;

        for(var i = 0; i < numrecs; i++)
        {
            int ptrPos = ptrsStart + i * 8;

            if(ptrPos + 8 > rawInode.Length) break;

            var childBlock = BigEndianBitConverter.ToUInt64(rawInode, ptrPos);

            // Recursively traverse down
            TraverseBtreeNode(childBlock, level - 1, entries, rawInode, attrForkOffset, inode);
        }
    }

    /// <summary>Recursively traverses btree nodes to find leaf blocks</summary>
    void TraverseBtreeNode(ulong  fsBlock, int level, List<XfsXattrEntry> entries, byte[] rawInode, int attrForkOffset,
                           Dinode inode)
    {
        ErrorNumber errno = ReadBlock(fsBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError) return;

        if(blockData.Length < 8) return;

        var magic = BigEndianBitConverter.ToUInt32(blockData, 0);

        if(magic != XFS_DA_NODE_MAGIC && magic != XFS_DA3_NODE_MAGIC) return;

        int headerSize = magic == XFS_DA3_NODE_MAGIC ? Marshal.SizeOf<Da3NodeHeader>() : Marshal.SizeOf<DaNodeHeader>();

        if(blockData.Length < headerSize) return;

        var btNumrecs = BigEndianBitConverter.ToUInt16(blockData, 6);

        if(level == 0)
        {
            // This is a leaf block, parse attributes
            ParseAttrLeaf(blockData, entries, rawInode, attrForkOffset, inode);
        }
        else
        {
            // Internal node, traverse children
            int ptrsPos = headerSize + btNumrecs * 8;

            for(var i = 0; i < btNumrecs; i++)
            {
                int ptrOffset = ptrsPos + i * 8;

                if(ptrOffset + 8 > blockData.Length) break;

                var childBlock = BigEndianBitConverter.ToUInt64(blockData, ptrOffset);

                TraverseBtreeNode(childBlock, level - 1, entries, rawInode, attrForkOffset, inode);
            }
        }
    }

    /// <summary>Parses an attribute leaf block</summary>
    ErrorNumber ParseAttrLeaf(byte[] blockData, List<XfsXattrEntry> entries, byte[] rawInode, int attrForkOffset,
                              Dinode inode)
    {
        if(blockData.Length < 16) return ErrorNumber.NoError;

        var magic = BigEndianBitConverter.ToUInt16(blockData, 8);

        if(magic != XFS_ATTR_LEAF_MAGIC && magic != XFS_ATTR3_LEAF_MAGIC) return ErrorNumber.NoError;

        int    headerSize;
        ushort entryCount;

        if(magic == XFS_ATTR_LEAF_MAGIC)
        {
            int hdrSize = Marshal.SizeOf<AttrLeafHeader>();

            if(blockData.Length < hdrSize) return ErrorNumber.NoError;
            AttrLeafHeader hdr = Marshal.ByteArrayToStructureBigEndian<AttrLeafHeader>(blockData);
            headerSize = hdrSize;
            entryCount = hdr.count;
        }
        else
        {
            int hdrSize = Marshal.SizeOf<Attr3LeafHeader>();

            if(blockData.Length < hdrSize) return ErrorNumber.NoError;
            Attr3LeafHeader hdr = Marshal.ByteArrayToStructureBigEndian<Attr3LeafHeader>(blockData);
            headerSize = hdrSize;
            entryCount = hdr.count;
        }

        int entrySize = Marshal.SizeOf<AttrLeafEntry>();

        for(var i = 0; i < entryCount; i++)
        {
            int entryPos = headerSize + i * entrySize;

            if(entryPos + entrySize > blockData.Length) break;

            AttrLeafEntry leafEntry =
                Marshal.ByteArrayToStructureBigEndian<AttrLeafEntry>(blockData, entryPos, entrySize);

            if((leafEntry.flags & XFS_ATTR_INCOMPLETE) != 0) continue;
            if((leafEntry.flags & XFS_ATTR_PARENT)     != 0) continue;

            int nameIdx = leafEntry.nameidx;

            if(nameIdx <= 0 || nameIdx >= blockData.Length) continue;

            if((leafEntry.flags & XFS_ATTR_LOCAL) != 0)
            {
                if(nameIdx + 3 > blockData.Length) continue;
                var  valueLen = BigEndianBitConverter.ToUInt16(blockData, nameIdx);
                byte nameLen  = blockData[nameIdx + 2];

                if(nameIdx + 3 + nameLen + valueLen > blockData.Length) continue;

                string name  = _encoding.GetString(blockData, nameIdx + 3, nameLen);
                var    value = new byte[valueLen];
                Array.Copy(blockData, nameIdx + 3 + nameLen, value, 0, valueLen);

                entries.Add(new XfsXattrEntry
                {
                    Name     = name,
                    Value    = value,
                    Flags    = leafEntry.flags,
                    FullName = PrefixAttributeName(name, leafEntry.flags)
                });
            }
            else
            {
                if(nameIdx + 9 > blockData.Length) continue;
                var  valueBlk = BigEndianBitConverter.ToUInt32(blockData, nameIdx);
                var  valueLen = BigEndianBitConverter.ToUInt32(blockData, nameIdx + 4);
                byte nameLen  = blockData[nameIdx                                 + 8];

                if(nameIdx + 9 + nameLen > blockData.Length) continue;

                string name  = _encoding.GetString(blockData, nameIdx + 9, nameLen);
                byte[] value = ReadRemoteAttrValue(valueBlk, valueLen, rawInode, attrForkOffset, inode);

                entries.Add(new XfsXattrEntry
                {
                    Name     = name,
                    Value    = value,
                    Flags    = leafEntry.flags,
                    FullName = PrefixAttributeName(name, leafEntry.flags)
                });
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads remote attribute value from disk using the extent map</summary>
    byte[] ReadRemoteAttrValue(uint valueBlk, uint valueLen, byte[] rawInode, int attrForkOffset, Dinode inode)
    {
        if(valueLen == 0) return [];

        // Build extent list from inode attr fork
        var extents = new List<(ulong startOff, ulong startBlock, uint blockCount)>();
        int pos     = attrForkOffset;

        // When NREXT64 is set, di_big_anextents overlaps di_nextents and di_anextents is padding
        int extentCount = _v3Inodes && (inode.di_flags2 & XFS_DIFLAG2_NREXT64) != 0
                              ? (int)inode.di_nextents
                              : inode.di_anextents;

        for(var i = 0; i < extentCount; i++)
        {
            if(pos + 16 > rawInode.Length) break;
            var l0 = BigEndianBitConverter.ToUInt64(rawInode, pos);
            var l1 = BigEndianBitConverter.ToUInt64(rawInode, pos + 8);
            pos += 16;
            DecodeBmbtRec(l0, l1, out ulong startOff, out ulong startBlock, out uint blockCount, out bool unwritten);
            extents.Add((startOff, startBlock, blockCount));
        }

        var result    = new byte[valueLen];
        var bytesRead = 0;

        // valueBlk is logical block number in the attr fork
        ulong logicalBlock = valueBlk;

        while(bytesRead < (int)valueLen)
        {
            // Map logical block to physical
            ulong physBlock = 0;
            var   found     = false;

            foreach((ulong startOff, ulong startBlock, uint blockCount) in extents)
            {
                if(logicalBlock >= startOff && logicalBlock < startOff + blockCount)
                {
                    physBlock = startBlock + (logicalBlock - startOff);
                    found     = true;

                    break;
                }
            }

            if(!found) break;

            ErrorNumber errno = ReadBlock(physBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError) break;

            var dataOffset = 0;
            int dataLen    = blockData.Length;

            // Skip v3 remote header if present
            if(_v3Inodes && blockData.Length >= 4)
            {
                var rmMagic = BigEndianBitConverter.ToUInt32(blockData, 0);

                if(rmMagic == XFS_ATTR3_RMT_MAGIC)
                {
                    dataOffset = Marshal.SizeOf<Attr3RemoteHeader>();
                    dataLen    = blockData.Length - dataOffset;
                }
            }

            int toCopy = Math.Min(dataLen, (int)valueLen - bytesRead);
            Array.Copy(blockData, dataOffset, result, bytesRead, toCopy);
            bytesRead += toCopy;
            logicalBlock++;
        }

        return result;
    }

    /// <summary>Adds the appropriate namespace prefix to an attribute name based on flags</summary>
    static string PrefixAttributeName(string name, byte flags)
    {
        if((flags & XFS_ATTR_SECURE) != 0) return "security." + name;

        if((flags & XFS_ATTR_ROOT) != 0) return "trusted." + name;

        return "user." + name;
    }

    /// <summary>Internal representation of an XFS extended attribute entry</summary>
    struct XfsXattrEntry
    {
        public string Name;
        public string FullName;
        public byte[] Value;
        public byte   Flags;
    }
}