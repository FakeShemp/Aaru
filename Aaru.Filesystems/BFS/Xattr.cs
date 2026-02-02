// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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
using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

// Information from Practical Filesystem Design, ISBN 1-55860-497-9
/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class BeFS
{
    /// <summary>Lists all extended attributes for a file or directory</summary>
    /// <remarks>
    ///     BeFS stores attributes in two places:
    ///     - Small data area: embedded in the i-node block for attributes under approximately 760 bytes total
    ///     - Attribute directory: separate i-node pointed to by the attributes field for larger attributes
    ///     This method enumerates both sources.
    /// </remarks>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="xattrs">Output list of attribute names</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = new List<string>();

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ListXAttr: path='{0}'", path);

        // Get the i-node for the file
        ErrorNumber inodeError = GetInodeForPath(path, out bfs_inode inode);

        if(inodeError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting i-node for path: {0}", inodeError);

            return inodeError;
        }

        // List attributes from small data area
        ErrorNumber smallDataError = ListSmallDataAttributes(inode, xattrs);

        if(smallDataError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error listing small data attributes: {0}", smallDataError);

            // Don't return error - continue to try attribute directory
        }

        // List attributes from attribute directory if present
        if(inode.attributes.len > 0)
        {
            ErrorNumber attrDirError = ListAttributeDirectoryAttributes(inode.attributes, xattrs);

            if(attrDirError != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error listing attribute directory: {0}", attrDirError);

                // Don't return error - we may have gotten some attributes from small data area
            }
        }

        AaruLogging.Debug(MODULE_NAME, "ListXAttr complete: found {0} attributes", xattrs.Count);

        return ErrorNumber.NoError;
    }

    /// <summary>Retrieves the value of an extended attribute</summary>
    /// <remarks>
    ///     Searches for the attribute in both the small data area and the attribute directory.
    ///     Returns the attribute data in the provided buffer.
    /// </remarks>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="xattr">Name of the attribute to retrieve</param>
    /// <param name="buf">Buffer to receive the attribute data</param>
    /// <returns>Error code indicating success or failure</returns>
    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "GetXattr: path='{0}', xattr='{1}'", path, xattr);

        // Get the i-node for the file
        ErrorNumber inodeError = GetInodeForPath(path, out bfs_inode inode);

        if(inodeError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error getting i-node for path: {0}", inodeError);

            return inodeError;
        }

        // Try to get attribute from small data area first
        ErrorNumber smallDataError = GetSmallDataAttribute(inode, xattr, out byte[] attrData);

        if(smallDataError == ErrorNumber.NoError)
        {
            buf = attrData;
            AaruLogging.Debug(MODULE_NAME, "GetXattr: found in small data area, size={0}", attrData.Length);

            return ErrorNumber.NoError;
        }

        // Try attribute directory if present
        if(inode.attributes.len > 0)
        {
            ErrorNumber attrDirError = GetAttributeDirectoryAttribute(inode.attributes, xattr, out attrData);

            if(attrDirError == ErrorNumber.NoError)
            {
                buf = attrData;
                AaruLogging.Debug(MODULE_NAME, "GetXattr: found in attribute directory, size={0}", attrData.Length);

                return ErrorNumber.NoError;
            }
        }

        AaruLogging.Debug(MODULE_NAME, "GetXattr: attribute '{0}' not found", xattr);

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Gets the i-node for a given path</summary>
    private ErrorNumber GetInodeForPath(string path, out bfs_inode inode)
    {
        inode = default(bfs_inode);

        // Normalize and parse the path
        string normalizedPath                                            = path ?? "/";
        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
            return ReadInode(_superblock.root_dir, out inode);

        // For non-root paths, use Stat to navigate and then get the i-node
        ErrorNumber statError = Stat(path, out FileEntryInfo fileInfo);

        if(statError != ErrorNumber.NoError) return statError;

        // Convert i-node address to block_run
        var ag          = (uint)(fileInfo.Inode >> 32);
        var blockOffset = (uint)(fileInfo.Inode & 0xFFFFFFFF);

        var inodeBlockRun = new block_run
        {
            allocation_group = ag,
            start            = (ushort)blockOffset,
            len              = 1
        };

        return ReadInode(inodeBlockRun, out inode);
    }

    /// <summary>Lists attributes stored in the small data area of an i-node</summary>
    private ErrorNumber ListSmallDataAttributes(bfs_inode inode, List<string> xattrs)
    {
        AaruLogging.Debug(MODULE_NAME, "ListSmallDataAttributes: parsing small data area");

        // The small_data area starts after the bfs_inode structure in the i-node block
        // We need to read the raw i-node block to access this data

        // Calculate the i-node block address from the inode_num field
        uint ag    = inode.inode_num.allocation_group;
        uint block = (ag << _superblock.ag_shift) + inode.inode_num.start;

        // Read the i-node block
        uint sectorSize           = _imagePlugin.Info.SectorSize;
        var  partitionStartSector = (long)_partition.Start;

        long blockByteAddr  = block * _superblock.block_size;
        long startingSector = blockByteAddr / sectorSize + partitionStartSector;
        var  sectorsToRead  = (int)((_superblock.inode_size + sectorSize - 1) / sectorSize);

        ErrorNumber readError = _imagePlugin.ReadSectors((ulong)startingSector,
                                                         false,
                                                         (uint)sectorsToRead,
                                                         out byte[] inodeBlock,
                                                         out SectorStatus[] _);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading i-node block: {0}", readError);

            return readError;
        }

        // Parse small_data entries starting after the main bfs_inode structure
        // The small_data area is approximately 760 bytes in a 1024-byte i-node block
        const int SMALL_DATA_START = 296; // Offset where small_data area begins in the i-node
        int       offset           = SMALL_DATA_START;
        var       seenAttributes   = new HashSet<string>();

        while(offset < _superblock.inode_size - 8)
        {
            // Check if we've reached the end of attributes (all zeros or beyond bounds)
            if(offset + 8 > inodeBlock.Length) break;

            uint type = _littleEndian
                            ? BitConverter.ToUInt32(inodeBlock, offset)
                            : BigEndianBitConverter.ToUInt32(inodeBlock, offset);

            if(type == 0) break; // End of small_data entries

            ushort nameSize = _littleEndian
                                  ? BitConverter.ToUInt16(inodeBlock, offset          + 4)
                                  : BigEndianBitConverter.ToUInt16(inodeBlock, offset + 4);

            ushort dataSize = _littleEndian
                                  ? BitConverter.ToUInt16(inodeBlock, offset          + 6)
                                  : BigEndianBitConverter.ToUInt16(inodeBlock, offset + 6);

            // Validate sizes
            if(nameSize == 0 || nameSize > 255 || offset + 8 + nameSize > inodeBlock.Length) break;

            // Extract attribute name
            string attrName = _encoding.GetString(inodeBlock, offset + 8, nameSize).TrimEnd('\0');

            // Only add if not already seen (handle potential duplicates)
            if(!seenAttributes.Contains(attrName))
            {
                xattrs.Add(attrName);
                seenAttributes.Add(attrName);
            }

            AaruLogging.Debug(MODULE_NAME,
                              "Found small data attribute: name='{0}', type=0x{1:X8}, dataSize={2}",
                              attrName,
                              type,
                              dataSize);

            // Move to next entry: header (8 bytes) + name (with padding) + data (with padding)
            int nameWithPadding = nameSize + 3 & ~3;
            int dataWithPadding = dataSize + 3 & ~3;
            offset += 8 + nameWithPadding + dataWithPadding;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Gets a single attribute from the small data area</summary>
    private ErrorNumber GetSmallDataAttribute(bfs_inode inode, string attrName, out byte[] attrData)
    {
        attrData = null;

        AaruLogging.Debug(MODULE_NAME, "GetSmallDataAttribute: looking for '{0}'", attrName);

        // Read the i-node block (same as ListSmallDataAttributes)
        uint ag    = inode.inode_num.allocation_group;
        uint block = (ag << _superblock.ag_shift) + inode.inode_num.start;

        uint sectorSize           = _imagePlugin.Info.SectorSize;
        var  partitionStartSector = (long)_partition.Start;

        long blockByteAddr  = block * _superblock.block_size;
        long startingSector = blockByteAddr / sectorSize + partitionStartSector;
        var  sectorsToRead  = (int)((_superblock.inode_size + sectorSize - 1) / sectorSize);

        ErrorNumber readError = _imagePlugin.ReadSectors((ulong)startingSector,
                                                         false,
                                                         (uint)sectorsToRead,
                                                         out byte[] inodeBlock,
                                                         out SectorStatus[] _);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading i-node block: {0}", readError);

            return readError;
        }

        // Search for the attribute in small_data area
        const int SMALL_DATA_START = 296;
        int       offset           = SMALL_DATA_START;

        while(offset < _superblock.inode_size - 8)
        {
            if(offset + 8 > inodeBlock.Length) break;

            uint type = _littleEndian
                            ? BitConverter.ToUInt32(inodeBlock, offset)
                            : BigEndianBitConverter.ToUInt32(inodeBlock, offset);

            if(type == 0) break;

            ushort nameSize = _littleEndian
                                  ? BitConverter.ToUInt16(inodeBlock, offset          + 4)
                                  : BigEndianBitConverter.ToUInt16(inodeBlock, offset + 4);

            ushort dataSize = _littleEndian
                                  ? BitConverter.ToUInt16(inodeBlock, offset          + 6)
                                  : BigEndianBitConverter.ToUInt16(inodeBlock, offset + 6);

            if(nameSize == 0 || nameSize > 255 || offset + 8 + nameSize > inodeBlock.Length) break;

            // Extract attribute name
            string entryName = _encoding.GetString(inodeBlock, offset + 8, nameSize).TrimEnd('\0');

            if(entryName == attrName)
            {
                // Found the attribute - extract its data
                int nameWithPadding = nameSize + 3 & ~3;
                int dataOffset      = offset + 8 + nameWithPadding;

                if(dataOffset + dataSize <= inodeBlock.Length)
                {
                    attrData = new byte[dataSize];
                    Array.Copy(inodeBlock, dataOffset, attrData, 0, dataSize);

                    AaruLogging.Debug(MODULE_NAME, "GetSmallDataAttribute: found '{0}', size={1}", attrName, dataSize);

                    return ErrorNumber.NoError;
                }

                AaruLogging.Debug(MODULE_NAME, "GetSmallDataAttribute: attribute data extends beyond i-node block");

                return ErrorNumber.InOutError;
            }

            // Move to next entry
            int nameWithPadding2 = nameSize + 3 & ~3;
            int dataWithPadding  = dataSize + 3 & ~3;
            offset += 8 + nameWithPadding2 + dataWithPadding;
        }

        AaruLogging.Debug(MODULE_NAME, "GetSmallDataAttribute: attribute '{0}' not found in small data area", attrName);

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Lists attributes from the attribute directory i-node</summary>
    private ErrorNumber ListAttributeDirectoryAttributes(block_run attrDirBlock, List<string> xattrs)
    {
        AaruLogging.Debug(MODULE_NAME, "ListAttributeDirectoryAttributes: reading from attribute directory");

        // Read the attribute directory i-node
        ErrorNumber readError = ReadInode(attrDirBlock, out bfs_inode attrDirInode);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading attribute directory i-node: {0}", readError);

            return readError;
        }

        // The attribute directory is a B+tree, parse it to get all attribute names
        ErrorNumber parseError = ParseDirectoryBTree(attrDirInode.data, out Dictionary<string, long> attrEntries);

        if(parseError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error parsing attribute directory B+tree: {0}", parseError);

            return parseError;
        }

        // Add all attribute names from the directory (skip if already in list from small data)
        foreach(string attrName in attrEntries.Keys)
            if(!xattrs.Contains(attrName))
                xattrs.Add(attrName);

        AaruLogging.Debug(MODULE_NAME, "ListAttributeDirectoryAttributes: found {0} attributes", attrEntries.Count);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets a single attribute from the attribute directory</summary>
    private ErrorNumber GetAttributeDirectoryAttribute(block_run attrDirBlock, string attrName, out byte[] attrData)
    {
        attrData = null;

        AaruLogging.Debug(MODULE_NAME, "GetAttributeDirectoryAttribute: looking for '{0}'", attrName);

        // Read the attribute directory i-node
        ErrorNumber readError = ReadInode(attrDirBlock, out bfs_inode attrDirInode);

        if(readError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading attribute directory i-node: {0}", readError);

            return readError;
        }

        // Parse the attribute directory B+tree to find the attribute
        ErrorNumber parseError = ParseDirectoryBTree(attrDirInode.data, out Dictionary<string, long> attrEntries);

        if(parseError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error parsing attribute directory B+tree: {0}", parseError);

            return parseError;
        }

        // Look for the attribute in the directory
        if(!attrEntries.TryGetValue(attrName, out long attrInodeAddr))
        {
            AaruLogging.Debug(MODULE_NAME, "Attribute '{0}' not found in directory", attrName);

            return ErrorNumber.NoSuchFile;
        }

        // Read the attribute i-node
        var ag          = (uint)(attrInodeAddr >> 32);
        var blockOffset = (uint)(attrInodeAddr & 0xFFFFFFFF);

        var attrInodeBlockRun = new block_run
        {
            allocation_group = ag,
            start            = (ushort)blockOffset,
            len              = 1
        };

        ErrorNumber attrReadError = ReadInode(attrInodeBlockRun, out bfs_inode attrInode);

        if(attrReadError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading attribute i-node: {0}", attrReadError);

            return attrReadError;
        }

        // Read the attribute data from the i-node's data stream
        ErrorNumber dataError = ReadFromDataStream(attrInode.data, 0, (int)attrInode.data.size, out attrData);

        if(dataError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading attribute data: {0}", dataError);

            return dataError;
        }

        AaruLogging.Debug(MODULE_NAME, "GetAttributeDirectoryAttribute: found attribute, size={0}", attrData.Length);

        return ErrorNumber.NoError;
    }
}