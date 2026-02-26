// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Resolve path to inode
        ErrorNumber errno = LookupFile(path, out uint nid);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ReadInode(nid, out Inode inode);

        if(errno != ErrorNumber.NoError) return errno;

        // Read the raw node block for inline xattr extraction
        errno = LookupNat(nid, out uint blockAddr);

        if(errno != ErrorNumber.NoError) return errno;

        if(blockAddr == 0) return ErrorNumber.InvalidArgument;

        errno = ReadBlock(blockAddr, out byte[] nodeBlock);

        if(errno != ErrorNumber.NoError) return errno;

        // Build the combined xattr buffer
        errno = ReadAllXattrs(inode, nodeBlock, out byte[] xattrData, out int xattrSize);

        if(errno != ErrorNumber.NoError) return errno;

        if(xattrData == null || xattrSize == 0)
        {
            xattrs = [];

            return ErrorNumber.NoError;
        }

        // Parse xattr entries
        xattrs = [];

        int headerSize = Marshal.SizeOf<XattrHeader>();
        int offset     = headerSize;

        while(offset + 4 <= xattrSize)
        {
            // IS_XATTR_LAST_ENTRY: check if the next 4 bytes are all zeros
            if(BitConverter.ToUInt32(xattrData, offset) == 0) break;

            // Read the fixed entry header (4 bytes)
            if(offset + 4 > xattrSize) break;

            byte nameIndex = xattrData[offset];
            byte nameLen   = xattrData[offset                        + 1];
            var  valueSize = BitConverter.ToUInt16(xattrData, offset + 2);

            int entrySize = XattrAlign(4 + nameLen + valueSize);

            if(offset + entrySize > xattrSize) break;

            // Get the full xattr name: prefix + entry name
            string prefix = GetXattrPrefix(nameIndex);

            if(prefix != null)
            {
                string entryName = nameLen > 0 ? Encoding.UTF8.GetString(xattrData, offset + 4, nameLen) : "";
                xattrs.Add(prefix + entryName);
            }

            offset += entrySize;
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(xattr)) return ErrorNumber.InvalidArgument;

        // Parse the requested xattr name into index + name
        if(!ParseXattrName(xattr, out byte targetIndex, out string targetName))
            return ErrorNumber.NoSuchExtendedAttribute;

        // Resolve path to inode
        ErrorNumber errno = LookupFile(path, out uint nid);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ReadInode(nid, out Inode inode);

        if(errno != ErrorNumber.NoError) return errno;

        // Read the raw node block for inline xattr extraction
        errno = LookupNat(nid, out uint blockAddr);

        if(errno != ErrorNumber.NoError) return errno;

        if(blockAddr == 0) return ErrorNumber.InvalidArgument;

        errno = ReadBlock(blockAddr, out byte[] nodeBlock);

        if(errno != ErrorNumber.NoError) return errno;

        // Build the combined xattr buffer
        errno = ReadAllXattrs(inode, nodeBlock, out byte[] xattrData, out int xattrSize);

        if(errno != ErrorNumber.NoError) return errno;

        if(xattrData == null || xattrSize == 0) return ErrorNumber.NoSuchExtendedAttribute;

        // Search for the matching entry
        int headerSize = Marshal.SizeOf<XattrHeader>();
        int offset     = headerSize;

        while(offset + 4 <= xattrSize)
        {
            if(BitConverter.ToUInt32(xattrData, offset) == 0) break;

            if(offset + 4 > xattrSize) break;

            byte nameIndex = xattrData[offset];
            byte nameLen   = xattrData[offset                        + 1];
            var  valueSize = BitConverter.ToUInt16(xattrData, offset + 2);

            int entrySize = XattrAlign(4 + nameLen + valueSize);

            if(offset + entrySize > xattrSize) break;

            // Check for match
            if(nameIndex == targetIndex && nameLen == targetName.Length)
            {
                string entryName = nameLen > 0 ? Encoding.UTF8.GetString(xattrData, offset + 4, nameLen) : "";

                if(entryName == targetName)
                {
                    // Found it — extract value
                    int valueOffset = offset + 4 + nameLen;

                    if(valueSize == 0)
                    {
                        buf = [];

                        return ErrorNumber.NoError;
                    }

                    buf = new byte[valueSize];
                    Array.Copy(xattrData, valueOffset, buf, 0, valueSize);

                    return ErrorNumber.NoError;
                }
            }

            offset += entrySize;
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>
    ///     Reads all extended attribute data for an inode, combining inline xattr and xattr block
    ///     into a single buffer matching the kernel's read_all_xattrs() logic.
    /// </summary>
    ErrorNumber ReadAllXattrs(Inode inode, byte[] nodeBlock, out byte[] xattrData, out int xattrSize)
    {
        xattrData = null;
        xattrSize = 0;

        // Calculate inline xattr size
        var inlineXattrSize = 0;

        if((inode.i_inline & F2FS_INLINE_XATTR) != 0)
        {
            int inlineXattrAddrs = GetInlineXattrAddrs(inode);
            inlineXattrSize = inlineXattrAddrs * 4;
        }

        bool hasXattrBlock = inode.i_xattr_nid != 0;

        if(inlineXattrSize == 0 && !hasXattrBlock) return ErrorNumber.NoError;

        // VALID_XATTR_BLOCK_SIZE = block_size - sizeof(node_footer) = 4096 - 24 = 4072
        int validXattrBlockSize = (int)_blockSize - Marshal.SizeOf<NodeFooter>();

        int totalSize = inlineXattrSize + (hasXattrBlock ? validXattrBlockSize : 0) + XATTR_PADDING_SIZE;
        xattrData = new byte[totalSize];

        // Read inline xattr data from the end of the inode's i_addr area
        if(inlineXattrSize > 0)
        {
            // inline_xattr_addr = &ri->i_addr[DEF_ADDRS_PER_INODE - inline_xattr_addrs]
            int inodeFixedSize    = Marshal.SizeOf<Inode>() - DEF_ADDRS_PER_INODE * 4 - DEF_NIDS_PER_INODE * 4;
            int inlineXattrAddrs  = inlineXattrSize / 4;
            int inlineXattrOffset = inodeFixedSize + (DEF_ADDRS_PER_INODE - inlineXattrAddrs) * 4;

            if(inlineXattrOffset + inlineXattrSize <= nodeBlock.Length)
                Array.Copy(nodeBlock, inlineXattrOffset, xattrData, 0, inlineXattrSize);
            else
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "Inline xattr extends beyond node block: offset={0}, size={1}",
                                  inlineXattrOffset,
                                  inlineXattrSize);

                return ErrorNumber.InvalidArgument;
            }
        }

        // Read the xattr block if present
        if(hasXattrBlock)
        {
            ErrorNumber errno = LookupNat(inode.i_xattr_nid, out uint xattrBlockAddr);

            if(errno != ErrorNumber.NoError) return errno;

            if(xattrBlockAddr == 0) return ErrorNumber.InvalidArgument;

            errno = ReadBlock(xattrBlockAddr, out byte[] xattrBlockData);

            if(errno != ErrorNumber.NoError) return errno;

            int copyLen = Math.Min(validXattrBlockSize, xattrBlockData.Length);
            Array.Copy(xattrBlockData, 0, xattrData, inlineXattrSize, copyLen);
        }

        // Validate the magic header
        XattrHeader header = Marshal.ByteArrayToStructureLittleEndian<XattrHeader>(xattrData);

        if(header.h_magic != F2FS_XATTR_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid xattr magic: 0x{0:X8} (expected 0x{1:X8})",
                              header.h_magic,
                              F2FS_XATTR_MAGIC);

            return ErrorNumber.NoError; // No xattrs, not an error
        }

        xattrSize = totalSize - XATTR_PADDING_SIZE;

        return ErrorNumber.NoError;
    }

    /// <summary>Returns the Linux xattr namespace prefix for a given F2FS name index</summary>
    static string GetXattrPrefix(byte nameIndex) => nameIndex switch
                                                    {
                                                        F2FS_XATTR_INDEX_USER             => "user.",
                                                        F2FS_XATTR_INDEX_POSIX_ACL_ACCESS => "system.posix_acl_access",
                                                        F2FS_XATTR_INDEX_POSIX_ACL_DEFAULT =>
                                                            "system.posix_acl_default",
                                                        F2FS_XATTR_INDEX_TRUSTED  => "trusted.",
                                                        F2FS_XATTR_INDEX_SECURITY => "security.",
                                                        F2FS_XATTR_INDEX_ADVISE   => "system.",
                                                        _                         => null
                                                    };

    /// <summary>Parses a full xattr name (e.g. "user.foo") into its F2FS name index and entry name</summary>
    static bool ParseXattrName(string fullName, out byte index, out string name)
    {
        index = 0;
        name  = "";

        // system.advise is special — fixed name
        if(fullName == F2FS_SYSTEM_ADVISE_NAME)
        {
            index = F2FS_XATTR_INDEX_ADVISE;
            name  = "advise";

            return true;
        }

        // system.posix_acl_access — no dot separator, the full suffix is the entry name
        if(fullName == "system.posix_acl_access")
        {
            index = F2FS_XATTR_INDEX_POSIX_ACL_ACCESS;
            name  = "";

            return true;
        }

        if(fullName == "system.posix_acl_default")
        {
            index = F2FS_XATTR_INDEX_POSIX_ACL_DEFAULT;
            name  = "";

            return true;
        }

        if(fullName.StartsWith("user.", StringComparison.Ordinal))
        {
            index = F2FS_XATTR_INDEX_USER;
            name  = fullName[5..];

            return true;
        }

        if(fullName.StartsWith("trusted.", StringComparison.Ordinal))
        {
            index = F2FS_XATTR_INDEX_TRUSTED;
            name  = fullName[8..];

            return true;
        }

        if(fullName.StartsWith("security.", StringComparison.Ordinal))
        {
            index = F2FS_XATTR_INDEX_SECURITY;
            name  = fullName[9..];

            return true;
        }

        if(fullName.StartsWith("system.", StringComparison.Ordinal))
        {
            index = F2FS_XATTR_INDEX_ADVISE;
            name  = fullName[7..];

            return true;
        }

        return false;
    }

    /// <summary>Aligns a size to a 4-byte boundary, matching XATTR_ALIGN()</summary>
    static int XattrAlign(int size) => size + XATTR_ROUND & ~XATTR_ROUND;
}