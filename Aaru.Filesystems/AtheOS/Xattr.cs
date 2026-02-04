// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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
public sealed partial class AtheOS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ListXAttr: path='{0}'", path);

        // Get the inode for the path
        ErrorNumber errno = GetInodeForPath(path, out Inode inode, out byte[] inodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting inode for path: {0}", errno);

            return errno;
        }

        xattrs = new List<string>();

        // First, read SmallData attributes from the inode itself
        // SmallData starts after the Inode structure within the inode block
        int smallDataOffset = _superblock.inode_size > 0 ? Marshal.SizeOf<Inode>() : 0;

        int smallDataSize = _superblock.inode_size - smallDataOffset;

        if(smallDataSize > 0 && inodeData != null && inodeData.Length >= _superblock.inode_size)
        {
            int offset = smallDataOffset;

            while(offset + 8 <= _superblock.inode_size) // SmallData header is 8 bytes
            {
                // Read SmallData header
                var sdType     = BitConverter.ToUInt32(inodeData, offset);
                var sdNameSize = BitConverter.ToUInt16(inodeData, offset + 4);
                var sdDataSize = BitConverter.ToUInt16(inodeData, offset + 6);

                // End of small data entries
                if(sdNameSize == 0) break;

                // Validate bounds
                if(offset + 8 + sdNameSize + sdDataSize > _superblock.inode_size) break;

                // Extract attribute name
                string attrName = _encoding.GetString(inodeData, offset + 8, sdNameSize);
                xattrs.Add(attrName);

                AaruLogging.Debug(MODULE_NAME,
                                  "Found SmallData attribute: name='{0}', type={1}, dataSize={2}",
                                  attrName,
                                  sdType,
                                  sdDataSize);

                // Move to next entry
                offset += 8 + sdNameSize + sdDataSize;
            }
        }

        // Then, read attributes from attribute directory if it exists
        if(inode.attrib_dir.len > 0)
        {
            long attrDirBlockAddr = (long)inode.attrib_dir.group * _superblock.blocks_per_ag + inode.attrib_dir.start;

            AaruLogging.Debug(MODULE_NAME, "Reading attribute directory at block {0}", attrDirBlockAddr);

            errno = ReadInode(attrDirBlockAddr, out Inode attrDirInode);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading attribute directory inode: {0}", errno);

                // Return what we have from SmallData
                return ErrorNumber.NoError;
            }

            // Parse the attribute directory's B+tree to get attribute names
            errno = ParseDirectoryBTree(attrDirInode.data, out Dictionary<string, long> attrEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error parsing attribute directory B+tree: {0}", errno);

                // Return what we have from SmallData
                return ErrorNumber.NoError;
            }

            // Add attribute names from the directory
            foreach(string attrName in attrEntries.Keys)
            {
                if(!xattrs.Contains(attrName)) xattrs.Add(attrName);
            }
        }

        AaruLogging.Debug(MODULE_NAME, "ListXAttr: found {0} attributes", xattrs.Count);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "GetXattr: path='{0}', xattr='{1}'", path, xattr);

        // Get the inode for the path
        ErrorNumber errno = GetInodeForPath(path, out Inode inode, out byte[] inodeData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting inode for path: {0}", errno);

            return errno;
        }

        // First, try to find in SmallData
        int smallDataOffset = Marshal.SizeOf<Inode>();
        int smallDataSize   = _superblock.inode_size - smallDataOffset;

        if(smallDataSize > 0 && inodeData != null && inodeData.Length >= _superblock.inode_size)
        {
            int offset = smallDataOffset;

            while(offset + 8 <= _superblock.inode_size)
            {
                var sdNameSize = BitConverter.ToUInt16(inodeData, offset + 4);
                var sdDataSize = BitConverter.ToUInt16(inodeData, offset + 6);

                if(sdNameSize == 0) break;

                if(offset + 8 + sdNameSize + sdDataSize > _superblock.inode_size) break;

                string attrName = _encoding.GetString(inodeData, offset + 8, sdNameSize);

                if(attrName == xattr)
                {
                    // Found the attribute in SmallData
                    buf = new byte[sdDataSize];
                    Array.Copy(inodeData, offset + 8 + sdNameSize, buf, 0, sdDataSize);

                    AaruLogging.Debug(MODULE_NAME,
                                      "GetXattr: found SmallData attribute '{0}', size={1}",
                                      xattr,
                                      sdDataSize);

                    return ErrorNumber.NoError;
                }

                offset += 8 + sdNameSize + sdDataSize;
            }
        }

        // Not found in SmallData, try attribute directory
        if(inode.attrib_dir.len == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Attribute '{0}' not found (no attribute directory)", xattr);

            return ErrorNumber.NoSuchExtendedAttribute;
        }

        long attrDirBlockAddr = (long)inode.attrib_dir.group * _superblock.blocks_per_ag + inode.attrib_dir.start;

        errno = ReadInode(attrDirBlockAddr, out Inode attrDirInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading attribute directory inode: {0}", errno);

            return errno;
        }

        // Parse the attribute directory to find the attribute
        errno = ParseDirectoryBTree(attrDirInode.data, out Dictionary<string, long> attrEntries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error parsing attribute directory B+tree: {0}", errno);

            return errno;
        }

        if(!attrEntries.TryGetValue(xattr, out long attrInodeAddr))
        {
            AaruLogging.Debug(MODULE_NAME, "Attribute '{0}' not found in attribute directory", xattr);

            return ErrorNumber.NoSuchExtendedAttribute;
        }

        // Read the attribute inode
        errno = ReadInode(attrInodeAddr, out Inode attrInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading attribute inode: {0}", errno);

            return errno;
        }

        // Read the attribute data from the attribute inode's data stream
        if(attrInode.data.size <= 0)
        {
            buf = [];

            return ErrorNumber.NoError;
        }

        errno = ReadFromDataStream(attrInode.data, 0, (int)attrInode.data.size, out buf);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading attribute data: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "GetXattr: found attribute '{0}' in attribute directory, size={1}",
                          xattr,
                          buf.Length);

        return ErrorNumber.NoError;
    }
}