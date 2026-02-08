// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Squash file system plugin.
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
using Aaru.Logging;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Squash
{
    /// <summary>Xattr namespace prefixes</summary>
    static readonly string[] _xattrPrefixes = ["user.", "trusted.", "security."];

    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Check if xattr table exists
        if(_superBlock.xattr_id_table_start == unchecked((ulong)SQUASHFS_INVALID_BLK))
        {
            xattrs = [];

            return ErrorNumber.NoError;
        }

        // Get file entry to find xattr index
        ErrorNumber errno = LookupPath(path, out _, out uint xattrIndex, out uint xattrCount);

        if(errno != ErrorNumber.NoError) return errno;

        // No xattrs for this file
        if(xattrIndex == SQUASHFS_INVALID_XATTR || xattrCount == 0)
        {
            xattrs = [];

            return ErrorNumber.NoError;
        }

        // Read xattr entries
        errno = ReadXattrList(xattrIndex, xattrCount, out xattrs);

        return errno;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        buf = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Check if xattr table exists
        if(_superBlock.xattr_id_table_start == unchecked((ulong)SQUASHFS_INVALID_BLK))
            return ErrorNumber.NoSuchExtendedAttribute;

        // Get file entry to find xattr index
        ErrorNumber errno = LookupPath(path, out _, out uint xattrIndex, out uint xattrCount);

        if(errno != ErrorNumber.NoError) return errno;

        // No xattrs for this file
        if(xattrIndex == SQUASHFS_INVALID_XATTR || xattrCount == 0) return ErrorNumber.NoSuchExtendedAttribute;

        // Read xattr value
        errno = ReadXattrValue(xattrIndex, xattrCount, xattr, out buf);

        return errno;
    }

    /// <summary>Looks up a path and returns its xattr information</summary>
    /// <param name="path">Path to lookup</param>
    /// <param name="entry">Output: Directory entry info</param>
    /// <param name="xattrIndex">Output: Xattr index</param>
    /// <param name="xattrCount">Output: Xattr count</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupPath(string path, out DirectoryEntryInfo entry, out uint xattrIndex, out uint xattrCount)
    {
        entry      = null;
        xattrIndex = SQUASHFS_INVALID_XATTR;
        xattrCount = 0;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
            return ReadRootXattrInfo(out xattrIndex, out xattrCount);

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, DirectoryEntryInfo> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out DirectoryEntryInfo foundEntry))
            {
                AaruLogging.Debug(MODULE_NAME, "LookupPath: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // If this is the last component, get its xattr info
            if(p == pathComponents.Length - 1)
            {
                entry = foundEntry;

                return ReadInodeXattrInfo(foundEntry.InodeBlock,
                                          foundEntry.InodeOffset,
                                          out xattrIndex,
                                          out xattrCount);
            }

            // Not the last component - must be a directory
            if(foundEntry.Type != SquashInodeType.Directory && foundEntry.Type != SquashInodeType.ExtendedDirectory)
            {
                AaruLogging.Debug(MODULE_NAME, "LookupPath: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory inode to get directory parameters
            ErrorNumber dirErrno = ReadDirectoryInode(foundEntry.InodeBlock,
                                                      foundEntry.InodeOffset,
                                                      out uint dirStartBlock,
                                                      out uint dirOffset,
                                                      out uint dirSize);

            if(dirErrno != ErrorNumber.NoError) return dirErrno;

            // Read directory contents for next iteration
            var dirEntries = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

            dirErrno = ReadDirectoryContents(dirStartBlock, dirOffset, dirSize, dirEntries);

            if(dirErrno != ErrorNumber.NoError) return dirErrno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Reads xattr info for the root inode</summary>
    /// <param name="xattrIndex">Output: Xattr index</param>
    /// <param name="xattrCount">Output: Xattr count</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadRootXattrInfo(out uint xattrIndex, out uint xattrCount)
    {
        xattrIndex = SQUASHFS_INVALID_XATTR;
        xattrCount = 0;

        // Extract root inode block and offset from root_inode reference
        var rootInodeBlock  = (uint)(_superBlock.root_inode >> 16);
        var rootInodeOffset = (ushort)(_superBlock.root_inode & 0xFFFF);

        return ReadInodeXattrInfo(rootInodeBlock, rootInodeOffset, out xattrIndex, out xattrCount);
    }

    /// <summary>Reads xattr info from an inode</summary>
    /// <param name="inodeBlock">Block containing the inode</param>
    /// <param name="inodeOffset">Offset within the block</param>
    /// <param name="xattrIndex">Output: Xattr index</param>
    /// <param name="xattrCount">Output: Xattr count</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInodeXattrInfo(uint inodeBlock, ushort inodeOffset, out uint xattrIndex, out uint xattrCount)
    {
        xattrIndex = SQUASHFS_INVALID_XATTR;
        xattrCount = 0;

        // Calculate absolute position of the inode
        ulong inodePosition = _superBlock.inode_table_start + inodeBlock;

        // Read the metadata block containing the inode
        ErrorNumber errno = ReadMetadataBlock(inodePosition, out byte[] inodeBlockData);

        if(errno != ErrorNumber.NoError) return errno;

        if(inodeBlockData == null || inodeBlockData.Length <= inodeOffset) return ErrorNumber.InvalidArgument;

        // Read the base inode to get the type
        var baseInodeData = new byte[Marshal.SizeOf<BaseInode>()];
        Array.Copy(inodeBlockData, inodeOffset, baseInodeData, 0, baseInodeData.Length);

        BaseInode baseInode = _littleEndian
                                  ? Helpers.Marshal.ByteArrayToStructureLittleEndian<BaseInode>(baseInodeData)
                                  : Helpers.Marshal.ByteArrayToStructureBigEndian<BaseInode>(baseInodeData);

        var inodeType = (SquashInodeType)baseInode.inode_type;

        // Only extended inode types have xattrs
        switch(inodeType)
        {
            case SquashInodeType.ExtendedDirectory:
            {
                var extDirInodeData = new byte[Marshal.SizeOf<ExtendedDirInode>()];
                Array.Copy(inodeBlockData, inodeOffset, extDirInodeData, 0, extDirInodeData.Length);

                ExtendedDirInode extDirInode = _littleEndian
                                                   ? Helpers.Marshal
                                                            .ByteArrayToStructureLittleEndian<
                                                                 ExtendedDirInode>(extDirInodeData)
                                                   : Helpers.Marshal
                                                            .ByteArrayToStructureBigEndian<
                                                                 ExtendedDirInode>(extDirInodeData);

                xattrIndex = extDirInode.xattr;

                break;
            }

            case SquashInodeType.ExtendedRegularFile:
            {
                var extRegInodeData = new byte[Marshal.SizeOf<ExtendedRegInode>()];
                Array.Copy(inodeBlockData, inodeOffset, extRegInodeData, 0, extRegInodeData.Length);

                ExtendedRegInode extRegInode = _littleEndian
                                                   ? Helpers.Marshal
                                                            .ByteArrayToStructureLittleEndian<
                                                                 ExtendedRegInode>(extRegInodeData)
                                                   : Helpers.Marshal
                                                            .ByteArrayToStructureBigEndian<
                                                                 ExtendedRegInode>(extRegInodeData);

                xattrIndex = extRegInode.xattr;

                break;
            }

            case SquashInodeType.ExtendedSymlink:
            {
                // ExtendedSymlinkInode is same as SymlinkInode + xattr at end
                // For now, we'll read SymlinkInode and then read xattr separately
                var symlinkInodeData = new byte[Marshal.SizeOf<SymlinkInode>()];
                Array.Copy(inodeBlockData, inodeOffset, symlinkInodeData, 0, symlinkInodeData.Length);

                SymlinkInode symlinkInode = _littleEndian
                                                ? Helpers.Marshal
                                                         .ByteArrayToStructureLittleEndian<
                                                              SymlinkInode>(symlinkInodeData)
                                                : Helpers.Marshal
                                                         .ByteArrayToStructureBigEndian<SymlinkInode>(symlinkInodeData);

                // xattr is after symlink target
                int xattrOffset = inodeOffset + Marshal.SizeOf<SymlinkInode>() + (int)symlinkInode.symlink_size;

                if(xattrOffset + 4 <= inodeBlockData.Length)
                {
                    xattrIndex = _littleEndian
                                     ? BitConverter.ToUInt32(inodeBlockData, xattrOffset)
                                     : (uint)(inodeBlockData[xattrOffset]     << 24 |
                                              inodeBlockData[xattrOffset + 1] << 16 |
                                              inodeBlockData[xattrOffset + 2] << 8  |
                                              inodeBlockData[xattrOffset + 3]);
                }

                break;
            }

            case SquashInodeType.ExtendedBlockDevice:
            case SquashInodeType.ExtendedCharDevice:
            {
                var extDevInodeData = new byte[Marshal.SizeOf<ExtendedDevInode>()];
                Array.Copy(inodeBlockData, inodeOffset, extDevInodeData, 0, extDevInodeData.Length);

                ExtendedDevInode extDevInode = _littleEndian
                                                   ? Helpers.Marshal
                                                            .ByteArrayToStructureLittleEndian<
                                                                 ExtendedDevInode>(extDevInodeData)
                                                   : Helpers.Marshal
                                                            .ByteArrayToStructureBigEndian<
                                                                 ExtendedDevInode>(extDevInodeData);

                xattrIndex = extDevInode.xattr;

                break;
            }

            case SquashInodeType.ExtendedFifo:
            case SquashInodeType.ExtendedSocket:
            {
                var extIpcInodeData = new byte[Marshal.SizeOf<ExtendedIpcInode>()];
                Array.Copy(inodeBlockData, inodeOffset, extIpcInodeData, 0, extIpcInodeData.Length);

                ExtendedIpcInode extIpcInode = _littleEndian
                                                   ? Helpers.Marshal
                                                            .ByteArrayToStructureLittleEndian<
                                                                 ExtendedIpcInode>(extIpcInodeData)
                                                   : Helpers.Marshal
                                                            .ByteArrayToStructureBigEndian<
                                                                 ExtendedIpcInode>(extIpcInodeData);

                xattrIndex = extIpcInode.xattr;

                break;
            }

            default:
                // Non-extended inodes don't have xattrs
                return ErrorNumber.NoError;
        }

        if(xattrIndex == SQUASHFS_INVALID_XATTR) return ErrorNumber.NoError;

        // Get xattr count from xattr ID table
        errno = ReadXattrId(xattrIndex, out XattrId xattrId);

        if(errno != ErrorNumber.NoError) return errno;

        xattrCount = xattrId.count;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an xattr ID entry</summary>
    /// <param name="index">Index into the xattr ID table</param>
    /// <param name="xattrId">Output: Xattr ID entry</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadXattrId(uint index, out XattrId xattrId)
    {
        xattrId = default(XattrId);

        // Read xattr ID table header
        ErrorNumber errno = ReadMetadataBlock(_superBlock.xattr_id_table_start, out byte[] tableData);

        if(errno != ErrorNumber.NoError) return errno;

        if(tableData == null || tableData.Length < Marshal.SizeOf<XattrIdTable>()) return ErrorNumber.InvalidArgument;

        var tableHeaderData = new byte[Marshal.SizeOf<XattrIdTable>()];
        Array.Copy(tableData, 0, tableHeaderData, 0, tableHeaderData.Length);

        XattrIdTable table = _littleEndian
                                 ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrIdTable>(tableHeaderData)
                                 : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrIdTable>(tableHeaderData);

        if(index >= table.xattr_ids) return ErrorNumber.InvalidArgument;

        // Calculate position of xattr ID entry
        // The xattr IDs are stored in metadata blocks starting at xattr_id_table_start + sizeof(XattrIdTable)
        int xattrIdSize     = Marshal.SizeOf<XattrId>();
        int entriesPerBlock = SQUASHFS_METADATA_SIZE / xattrIdSize;
        var blockIndex      = (int)(index / entriesPerBlock);
        int offsetInBlock   = (int)(index % entriesPerBlock) * xattrIdSize;

        // Read the block containing the xattr ID
        // Block pointers follow the header
        int blockPtrOffset = Marshal.SizeOf<XattrIdTable>() + blockIndex * 8;

        if(blockPtrOffset + 8 > tableData.Length)
        {
            // Need to read more data
            errno = ReadMetadataBlock(_superBlock.xattr_id_table_start, out tableData);

            if(errno != ErrorNumber.NoError) return errno;
        }

        ulong blockPtr = _littleEndian
                             ? BitConverter.ToUInt64(tableData, blockPtrOffset)
                             : (ulong)((long)tableData[blockPtrOffset]     << 56 |
                                       (long)tableData[blockPtrOffset + 1] << 48 |
                                       (long)tableData[blockPtrOffset + 2] << 40 |
                                       (long)tableData[blockPtrOffset + 3] << 32 |
                                       (long)tableData[blockPtrOffset + 4] << 24 |
                                       (long)tableData[blockPtrOffset + 5] << 16 |
                                       (long)tableData[blockPtrOffset + 6] << 8  |
                                       tableData[blockPtrOffset + 7]);

        errno = ReadMetadataBlock(blockPtr, out byte[] idBlockData);

        if(errno != ErrorNumber.NoError) return errno;

        if(idBlockData == null || offsetInBlock + xattrIdSize > idBlockData.Length) return ErrorNumber.InvalidArgument;

        var xattrIdData = new byte[xattrIdSize];
        Array.Copy(idBlockData, offsetInBlock, xattrIdData, 0, xattrIdSize);

        xattrId = _littleEndian
                      ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrId>(xattrIdData)
                      : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrId>(xattrIdData);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the list of xattr names for a file</summary>
    /// <param name="xattrIndex">Xattr index</param>
    /// <param name="count">Number of xattrs</param>
    /// <param name="xattrs">Output: List of xattr names</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadXattrList(uint xattrIndex, uint count, out List<string> xattrs)
    {
        xattrs = [];

        // Read xattr ID to get the location
        ErrorNumber errno = ReadXattrId(xattrIndex, out XattrId xattrId);

        if(errno != ErrorNumber.NoError) return errno;

        // Read xattr ID table header to get xattr table start
        errno = ReadMetadataBlock(_superBlock.xattr_id_table_start, out byte[] tableData);

        if(errno != ErrorNumber.NoError) return errno;

        var tableHeaderData = new byte[Marshal.SizeOf<XattrIdTable>()];
        Array.Copy(tableData, 0, tableHeaderData, 0, tableHeaderData.Length);

        XattrIdTable table = _littleEndian
                                 ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrIdTable>(tableHeaderData)
                                 : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrIdTable>(tableHeaderData);

        // Calculate position of xattr data
        ulong xattrBlock  = (xattrId.xattr >> 16) + table.xattr_table_start;
        var   xattrOffset = (int)(xattrId.xattr & 0xFFFF);

        // Read xattr metadata block
        errno = ReadMetadataBlock(xattrBlock, out byte[] xattrData);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse xattr entries
        int currentOffset = xattrOffset;

        for(uint i = 0; i < count && currentOffset < xattrData.Length; i++)
        {
            if(currentOffset + Marshal.SizeOf<XattrEntry>() > xattrData.Length) break;

            var entryData = new byte[Marshal.SizeOf<XattrEntry>()];
            Array.Copy(xattrData, currentOffset, entryData, 0, entryData.Length);

            XattrEntry entry = _littleEndian
                                   ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrEntry>(entryData)
                                   : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrEntry>(entryData);

            currentOffset += Marshal.SizeOf<XattrEntry>();

            // Read attribute name
            if(currentOffset + entry.size > xattrData.Length) break;

            var nameBytes = new byte[entry.size];
            Array.Copy(xattrData, currentOffset, nameBytes, 0, entry.size);
            string name = _encoding.GetString(nameBytes).TrimEnd('\0');

            currentOffset += entry.size;

            // Get prefix based on type
            int    prefixType = entry.type & (ushort)SquashXattrType.PrefixMask;
            string prefix     = prefixType < _xattrPrefixes.Length ? _xattrPrefixes[prefixType] : "unknown.";

            xattrs.Add(prefix + name);

            // Skip value
            if(currentOffset + Marshal.SizeOf<XattrVal>() > xattrData.Length) break;

            var valData = new byte[Marshal.SizeOf<XattrVal>()];
            Array.Copy(xattrData, currentOffset, valData, 0, valData.Length);

            XattrVal val = _littleEndian
                               ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrVal>(valData)
                               : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrVal>(valData);

            currentOffset += Marshal.SizeOf<XattrVal>();

            // Skip value data (or out-of-line reference)
            if((entry.type & (ushort)SquashXattrType.ValueOutOfLine) != 0)
                currentOffset += 8; // Skip 64-bit reference
            else
                currentOffset += (int)val.vsize;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a specific xattr value</summary>
    /// <param name="xattrIndex">Xattr index</param>
    /// <param name="count">Number of xattrs</param>
    /// <param name="xattrName">Name of the xattr to read</param>
    /// <param name="value">Output: Xattr value</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadXattrValue(uint xattrIndex, uint count, string xattrName, out byte[] value)
    {
        value = null;

        // Parse the xattr name to get prefix and name
        int    prefixType        = -1;
        string nameWithoutPrefix = xattrName;

        for(var i = 0; i < _xattrPrefixes.Length; i++)
        {
            if(!xattrName.StartsWith(_xattrPrefixes[i], StringComparison.Ordinal)) continue;

            prefixType        = i;
            nameWithoutPrefix = xattrName[_xattrPrefixes[i].Length..];

            break;
        }

        if(prefixType < 0) return ErrorNumber.NoSuchExtendedAttribute;

        // Read xattr ID to get the location
        ErrorNumber errno = ReadXattrId(xattrIndex, out XattrId xattrId);

        if(errno != ErrorNumber.NoError) return errno;

        // Read xattr ID table header to get xattr table start
        errno = ReadMetadataBlock(_superBlock.xattr_id_table_start, out byte[] tableData);

        if(errno != ErrorNumber.NoError) return errno;

        var tableHeaderData = new byte[Marshal.SizeOf<XattrIdTable>()];
        Array.Copy(tableData, 0, tableHeaderData, 0, tableHeaderData.Length);

        XattrIdTable table = _littleEndian
                                 ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrIdTable>(tableHeaderData)
                                 : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrIdTable>(tableHeaderData);

        // Calculate position of xattr data
        ulong xattrBlock  = (xattrId.xattr >> 16) + table.xattr_table_start;
        var   xattrOffset = (int)(xattrId.xattr & 0xFFFF);

        // Read xattr metadata block
        errno = ReadMetadataBlock(xattrBlock, out byte[] xattrData);

        if(errno != ErrorNumber.NoError) return errno;

        // Search for the xattr
        int currentOffset = xattrOffset;

        for(uint i = 0; i < count && currentOffset < xattrData.Length; i++)
        {
            if(currentOffset + Marshal.SizeOf<XattrEntry>() > xattrData.Length) break;

            var entryData = new byte[Marshal.SizeOf<XattrEntry>()];
            Array.Copy(xattrData, currentOffset, entryData, 0, entryData.Length);

            XattrEntry entry = _littleEndian
                                   ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrEntry>(entryData)
                                   : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrEntry>(entryData);

            currentOffset += Marshal.SizeOf<XattrEntry>();

            // Read attribute name
            if(currentOffset + entry.size > xattrData.Length) break;

            var nameBytes = new byte[entry.size];
            Array.Copy(xattrData, currentOffset, nameBytes, 0, entry.size);
            string name = _encoding.GetString(nameBytes).TrimEnd('\0');

            currentOffset += entry.size;

            int entryPrefixType = entry.type & (ushort)SquashXattrType.PrefixMask;

            // Check if this is the xattr we're looking for
            if(entryPrefixType == prefixType && string.Equals(name, nameWithoutPrefix, StringComparison.Ordinal))
            {
                // Found it - read the value
                if(currentOffset + Marshal.SizeOf<XattrVal>() > xattrData.Length) return ErrorNumber.InvalidArgument;

                var valData = new byte[Marshal.SizeOf<XattrVal>()];
                Array.Copy(xattrData, currentOffset, valData, 0, valData.Length);

                XattrVal val = _littleEndian
                                   ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrVal>(valData)
                                   : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrVal>(valData);

                currentOffset += Marshal.SizeOf<XattrVal>();

                if((entry.type & (ushort)SquashXattrType.ValueOutOfLine) != 0)
                {
                    // Value is stored out-of-line
                    if(currentOffset + 8 > xattrData.Length) return ErrorNumber.InvalidArgument;

                    ulong oolRef = _littleEndian
                                       ? BitConverter.ToUInt64(xattrData, currentOffset)
                                       : (ulong)((long)xattrData[currentOffset]     << 56 |
                                                 (long)xattrData[currentOffset + 1] << 48 |
                                                 (long)xattrData[currentOffset + 2] << 40 |
                                                 (long)xattrData[currentOffset + 3] << 32 |
                                                 (long)xattrData[currentOffset + 4] << 24 |
                                                 (long)xattrData[currentOffset + 5] << 16 |
                                                 (long)xattrData[currentOffset + 6] << 8  |
                                                 xattrData[currentOffset + 7]);

                    // Read from out-of-line location
                    ulong oolBlock  = (oolRef >> 16) + table.xattr_table_start;
                    var   oolOffset = (int)(oolRef & 0xFFFF);

                    errno = ReadMetadataBlock(oolBlock, out byte[] oolData);

                    if(errno != ErrorNumber.NoError) return errno;

                    // Re-read the val structure from OOL location
                    if(oolOffset + Marshal.SizeOf<XattrVal>() > oolData.Length) return ErrorNumber.InvalidArgument;

                    Array.Copy(oolData, oolOffset, valData, 0, valData.Length);

                    val = _littleEndian
                              ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrVal>(valData)
                              : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrVal>(valData);

                    oolOffset += Marshal.SizeOf<XattrVal>();

                    if(oolOffset + val.vsize > oolData.Length) return ErrorNumber.InvalidArgument;

                    value = new byte[val.vsize];
                    Array.Copy(oolData, oolOffset, value, 0, (int)val.vsize);
                }
                else
                {
                    // Value is inline
                    if(currentOffset + val.vsize > xattrData.Length) return ErrorNumber.InvalidArgument;

                    value = new byte[val.vsize];
                    Array.Copy(xattrData, currentOffset, value, 0, (int)val.vsize);
                }

                return ErrorNumber.NoError;
            }

            // Skip this xattr's value
            if(currentOffset + Marshal.SizeOf<XattrVal>() > xattrData.Length) break;

            var skipValData = new byte[Marshal.SizeOf<XattrVal>()];
            Array.Copy(xattrData, currentOffset, skipValData, 0, skipValData.Length);

            XattrVal skipVal = _littleEndian
                                   ? Helpers.Marshal.ByteArrayToStructureLittleEndian<XattrVal>(skipValData)
                                   : Helpers.Marshal.ByteArrayToStructureBigEndian<XattrVal>(skipValData);

            currentOffset += Marshal.SizeOf<XattrVal>();

            if((entry.type & (ushort)SquashXattrType.ValueOutOfLine) != 0)
                currentOffset += 8;
            else
                currentOffset += (int)skipVal.vsize;
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }
}