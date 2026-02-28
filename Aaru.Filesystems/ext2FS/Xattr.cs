// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
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
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        uint inodeNumber;

        if(normalizedPath is "/")
            inodeNumber = EXT2_ROOT_INO;
        else
        {
            ErrorNumber errno = ResolvePathToInode(normalizedPath, out inodeNumber);

            if(errno != ErrorNumber.NoError) return errno;
        }

        ErrorNumber readErr = ReadInode(inodeNumber, out Inode inode);

        if(readErr != ErrorNumber.NoError) return readErr;

        xattrs = [];

        // Read inline xattrs (ibody)
        ReadInlineXattrNames(inodeNumber, inode, xattrs);

        // Read external xattr block
        ReadExternalXattrNames(inode, xattrs);

        AaruLogging.Debug(MODULE_NAME, "ListXAttr: inode={0}, found {1} attributes", inodeNumber, xattrs.Count);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(xattr)) return ErrorNumber.InvalidArgument;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        uint inodeNumber;

        if(normalizedPath is "/")
            inodeNumber = EXT2_ROOT_INO;
        else
        {
            ErrorNumber errno = ResolvePathToInode(normalizedPath, out inodeNumber);

            if(errno != ErrorNumber.NoError) return errno;
        }

        ErrorNumber readErr = ReadInode(inodeNumber, out Inode inode);

        if(readErr != ErrorNumber.NoError) return readErr;

        // Try inline xattrs first
        ErrorNumber result = ReadInlineXattrValue(inodeNumber, inode, xattr, ref buf);

        if(result == ErrorNumber.NoError) return ErrorNumber.NoError;

        // Try external xattr block
        result = ReadExternalXattrValue(inode, xattr, ref buf);

        return result;
    }

    /// <summary>Reads xattr names from inline (ibody) storage</summary>
    void ReadInlineXattrNames(uint inodeNumber, Inode inode, List<string> names)
    {
        if(_inodeSize <= EXT2_GOOD_OLD_INODE_SIZE || inode.extra_isize == 0) return;

        ErrorNumber errno = ReadRawInodeBytes(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return;

        int ibodyOffset = EXT2_GOOD_OLD_INODE_SIZE + inode.extra_isize;

        if(ibodyOffset + EXT4_XATTR_IBODY_HEADER_SIZE > rawInode.Length) return;

        // Check ibody magic
        var magic = BitConverter.ToUInt32(rawInode, ibodyOffset);

        if(magic != EXT4_XATTR_MAGIC) return;

        int entryStart = ibodyOffset + EXT4_XATTR_IBODY_HEADER_SIZE;
        int entryEnd   = rawInode.Length;

        ParseXattrEntryNames(rawInode, entryStart, entryEnd, names);
    }

    /// <summary>Reads xattr names from the external xattr block</summary>
    void ReadExternalXattrNames(Inode inode, List<string> names)
    {
        ulong xattrBlock = _is64Bit && !_isHurd
                               ? (ulong)inode.file_acl_high << 32 | inode.file_acl_lo
                               : inode.file_acl_lo;

        if(xattrBlock == 0) return;

        ErrorNumber errno = ReadBytes(xattrBlock * _blockSize, _blockSize, out byte[] blockData);

        if(errno != ErrorNumber.NoError) return;

        if(blockData.Length < EXT4_XATTR_HEADER_SIZE) return;

        // Validate header magic
        var magic = BitConverter.ToUInt32(blockData, 0);

        if(magic != EXT4_XATTR_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "External xattr block magic mismatch: 0x{0:X8}, expected 0x{1:X8}",
                              magic,
                              EXT4_XATTR_MAGIC);

            return;
        }

        ParseXattrEntryNames(blockData, EXT4_XATTR_HEADER_SIZE, blockData.Length, names);
    }

    /// <summary>Reads a specific xattr value from inline (ibody) storage</summary>
    ErrorNumber ReadInlineXattrValue(uint inodeNumber, Inode inode, string xattr, ref byte[] buf)
    {
        if(_inodeSize <= EXT2_GOOD_OLD_INODE_SIZE || inode.extra_isize == 0) return ErrorNumber.NoSuchExtendedAttribute;

        ErrorNumber errno = ReadRawInodeBytes(inodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int ibodyOffset = EXT2_GOOD_OLD_INODE_SIZE + inode.extra_isize;

        if(ibodyOffset + EXT4_XATTR_IBODY_HEADER_SIZE > rawInode.Length) return ErrorNumber.NoSuchExtendedAttribute;

        var magic = BitConverter.ToUInt32(rawInode, ibodyOffset);

        if(magic != EXT4_XATTR_MAGIC) return ErrorNumber.NoSuchExtendedAttribute;

        int entryStart = ibodyOffset + EXT4_XATTR_IBODY_HEADER_SIZE;
        int entryEnd   = rawInode.Length;

        // For ibody xattrs, value offsets are relative to IFIRST (first entry after ibody header)
        return FindXattrValue(rawInode, entryStart, entryEnd, ibodyOffset, xattr, ref buf);
    }

    /// <summary>Reads a specific xattr value from the external xattr block</summary>
    ErrorNumber ReadExternalXattrValue(Inode inode, string xattr, ref byte[] buf)
    {
        ulong xattrBlock = _is64Bit && !_isHurd
                               ? (ulong)inode.file_acl_high << 32 | inode.file_acl_lo
                               : inode.file_acl_lo;

        if(xattrBlock == 0) return ErrorNumber.NoSuchExtendedAttribute;

        ErrorNumber errno = ReadBytes(xattrBlock * _blockSize, _blockSize, out byte[] blockData);

        if(errno != ErrorNumber.NoError) return errno;

        if(blockData.Length < EXT4_XATTR_HEADER_SIZE) return ErrorNumber.NoSuchExtendedAttribute;

        var magic = BitConverter.ToUInt32(blockData, 0);

        if(magic != EXT4_XATTR_MAGIC) return ErrorNumber.NoSuchExtendedAttribute;

        // For external block xattrs, value offsets are relative to the end of the header
        return FindXattrValue(blockData, EXT4_XATTR_HEADER_SIZE, blockData.Length, 0, xattr, ref buf);
    }

    /// <summary>Parses xattr entries and collects their full names</summary>
    void ParseXattrEntryNames(byte[] data, int start, int end, List<string> names)
    {
        int offset = start;

        while(offset + EXT4_XATTR_ENTRY_HDR_SIZE <= end)
        {
            // Check for end-of-entries sentinel (4 zero bytes)
            if(BitConverter.ToUInt32(data, offset) == 0) break;

            byte nameLen   = data[offset];
            byte nameIndex = data[offset + 1];

            if(nameLen == 0) break;

            int entrySize = EXT4_XATTR_ENTRY_HDR_SIZE + nameLen + EXT4_XATTR_ROUND & ~EXT4_XATTR_ROUND;

            if(offset + entrySize > end) break;

            // Extract the name
            var nameBytes = new byte[nameLen];
            Array.Copy(data, offset + EXT4_XATTR_ENTRY_HDR_SIZE, nameBytes, 0, nameLen);
            string name = StringHandlers.CToString(nameBytes, Encoding.UTF8);

            // Prepend the prefix for this name index
            string prefix   = GetXattrPrefix(nameIndex);
            string fullName = string.IsNullOrEmpty(prefix) ? name : prefix + name;

            names.Add(fullName);

            offset += entrySize;
        }
    }

    /// <summary>Finds a specific xattr value by name from parsed entries</summary>
    ErrorNumber FindXattrValue(byte[] data, int entryStart, int entryEnd, int valueBase, string xattr, ref byte[] buf)
    {
        int offset = entryStart;

        while(offset + EXT4_XATTR_ENTRY_HDR_SIZE <= entryEnd)
        {
            if(BitConverter.ToUInt32(data, offset) == 0) break;

            byte nameLen   = data[offset];
            byte nameIndex = data[offset                        + 1];
            var  valueOffs = BitConverter.ToUInt16(data, offset + 2);
            var  valueInum = BitConverter.ToUInt32(data, offset + 4);
            var  valueSize = BitConverter.ToUInt32(data, offset + 8);

            if(nameLen == 0) break;

            int entrySize = EXT4_XATTR_ENTRY_HDR_SIZE + nameLen + EXT4_XATTR_ROUND & ~EXT4_XATTR_ROUND;

            if(offset + entrySize > entryEnd) break;

            var nameBytes = new byte[nameLen];
            Array.Copy(data, offset + EXT4_XATTR_ENTRY_HDR_SIZE, nameBytes, 0, nameLen);
            string name = StringHandlers.CToString(nameBytes, Encoding.UTF8);

            string prefix   = GetXattrPrefix(nameIndex);
            string fullName = string.IsNullOrEmpty(prefix) ? name : prefix + name;

            if(fullName == xattr)
            {
                if(valueSize == 0)
                {
                    buf = [];

                    return ErrorNumber.NoError;
                }

                // EA inode: value stored in a dedicated inode's data blocks
                if(valueInum != 0) return ReadEaInodeValue(valueInum, valueSize, ref buf);

                // Value offset is relative to the value base
                int absoluteOffset = valueBase + valueOffs;

                if(absoluteOffset + (int)valueSize > data.Length)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Xattr value extends beyond data: offset={0}, size={1}, data.Length={2}",
                                      absoluteOffset,
                                      valueSize,
                                      data.Length);

                    return ErrorNumber.InvalidArgument;
                }

                buf = new byte[valueSize];
                Array.Copy(data, absoluteOffset, buf, 0, (int)valueSize);

                return ErrorNumber.NoError;
            }

            offset += entrySize;
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>Returns the string prefix for a given xattr name index</summary>
    static string GetXattrPrefix(byte nameIndex) => nameIndex switch
                                                    {
                                                        EXT4_XATTR_INDEX_USER             => "user.",
                                                        EXT4_XATTR_INDEX_POSIX_ACL_ACCESS => "system.posix_acl_access",
                                                        EXT4_XATTR_INDEX_POSIX_ACL_DEFAULT =>
                                                            "system.posix_acl_default",
                                                        EXT4_XATTR_INDEX_TRUSTED    => "trusted.",
                                                        EXT4_XATTR_INDEX_LUSTRE     => "lustre.",
                                                        EXT4_XATTR_INDEX_SECURITY   => "security.",
                                                        EXT4_XATTR_INDEX_SYSTEM     => "system.",
                                                        EXT4_XATTR_INDEX_RICHACL    => "system.richacl",
                                                        EXT4_XATTR_INDEX_ENCRYPTION => "encryption.",
                                                        EXT4_XATTR_INDEX_HURD       => "gnu.",
                                                        _                           => ""
                                                    };

    /// <summary>Reads an xattr value stored in an EA inode</summary>
    /// <param name="inodeNumber">The EA inode number</param>
    /// <param name="valueSize">Expected size of the value</param>
    /// <param name="buf">Buffer to receive the value</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadEaInodeValue(uint inodeNumber, uint valueSize, ref byte[] buf)
    {
        ErrorNumber errno = ReadInode(inodeNumber, out Inode eaInode);

        if(errno != ErrorNumber.NoError) return errno;

        errno = GetInodeDataBlocks(eaInode, out List<(ulong physicalBlock, uint length, bool unwritten)> blockList);

        if(errno != ErrorNumber.NoError) return errno;

        buf = new byte[valueSize];

        uint  bytesRead    = 0;
        ulong logicalBlock = 0;

        while(bytesRead < valueSize)
        {
            errno = ReadLogicalBlock(blockList, logicalBlock, out byte[] blockData);

            if(errno != ErrorNumber.NoError) return errno;

            uint toCopy = Math.Min(_blockSize, valueSize - bytesRead);

            if(blockData != null && blockData.Length > 0) Array.Copy(blockData, 0, buf, (int)bytesRead, (int)toCopy);

            bytesRead += toCopy;
            logicalBlock++;
        }

        AaruLogging.Debug(MODULE_NAME, "ReadEaInodeValue: read {0} bytes from EA inode {1}", valueSize, inodeNumber);

        return ErrorNumber.NoError;
    }
}