// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class HPFS
{
    const string EA_PREFIX_OS2 = "com.microsoft.os2";

    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Get fnode for the file/directory
        uint fnode;

        if(normalizedPath == "/")
            fnode = _rootFnode;
        else
        {
            ErrorNumber errno = GetDirectoryEntry(normalizedPath, out DirectoryEntry entry);

            if(errno != ErrorNumber.NoError) return errno;

            fnode = entry.fnode;
        }

        // Read the fnode
        ErrorNumber fnodeErr = ReadFNode(fnode, out FNode fnodeStruct);

        if(fnodeErr != ErrorNumber.NoError) return fnodeErr;

        xattrs = [];

        // Read fnode-resident EAs
        // The ea_offs field is the offset from start of fnode to EA/ACL area (typically 0xC4).
        // The acl_size_s field is the size of ACL data that precedes the EAs.
        // Since our ea array in the struct starts at offset 0xC4, the offset into the ea array
        // to find EAs is just acl_size_s.
        if(fnodeStruct.ea_size_s > 0)
        {
            ErrorNumber errno = ParseEaList(fnodeStruct.ea, fnodeStruct.acl_size_s, fnodeStruct.ea_size_s, xattrs);

            if(errno != ErrorNumber.NoError) return errno;
        }

        // Read disk-resident EAs
        if(fnodeStruct.ea_size_l > 0 && fnodeStruct.ea_secno != 0)
        {
            ErrorNumber errno = ReadExternalEaList(fnodeStruct.ea_secno,
                                                   fnodeStruct.EaInAnode,
                                                   fnodeStruct.ea_size_l,
                                                   xattrs);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Get fnode for the file/directory
        uint fnode;

        if(normalizedPath == "/")
            fnode = _rootFnode;
        else
        {
            ErrorNumber errno = GetDirectoryEntry(normalizedPath, out DirectoryEntry entry);

            if(errno != ErrorNumber.NoError) return errno;

            fnode = entry.fnode;
        }

        // Read the fnode
        ErrorNumber fnodeErr = ReadFNode(fnode, out FNode fnodeStruct);

        if(fnodeErr != ErrorNumber.NoError) return fnodeErr;

        // Convert xattr name back to OS/2 EA name
        string eaName = ConvertXattrToEaName(xattr);

        // Search fnode-resident EAs
        // The offset into the ea array is acl_size_s (to skip past ACL data)
        if(fnodeStruct.ea_size_s > 0)
        {
            ErrorNumber errno =
                FindEaInBuffer(fnodeStruct.ea, fnodeStruct.acl_size_s, fnodeStruct.ea_size_s, eaName, ref buf);

            if(errno == ErrorNumber.NoError) return ErrorNumber.NoError;

            if(errno != ErrorNumber.NoSuchExtendedAttribute) return errno;
        }

        // Search disk-resident EAs
        if(fnodeStruct.ea_size_l > 0 && fnodeStruct.ea_secno != 0)
            return FindExternalEa(fnodeStruct.ea_secno, fnodeStruct.EaInAnode, fnodeStruct.ea_size_l, eaName, ref buf);

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>Converts an OS/2 EA name to an xattr name.</summary>
    /// <param name="eaName">The OS/2 EA name.</param>
    /// <returns>The xattr name.</returns>
    static string ConvertEaNameToXattr(string eaName)
    {
        if(string.IsNullOrEmpty(eaName)) return eaName;

        // .CLASSINFO becomes the workplace shell class info xattr
        if(eaName.Equals(".CLASSINFO", StringComparison.OrdinalIgnoreCase)) return Xattrs.XATTR_WORKPLACE_CLASSINFO;

        // EAs starting with "." get prefixed with com.microsoft.os2
        if(eaName.StartsWith(".", StringComparison.Ordinal)) return $"{EA_PREFIX_OS2}{eaName}";

        // Other EA names are left untouched
        return eaName;
    }

    /// <summary>Converts an xattr name back to an OS/2 EA name.</summary>
    /// <param name="xattr">The xattr name.</param>
    /// <returns>The OS/2 EA name.</returns>
    static string ConvertXattrToEaName(string xattr)
    {
        if(string.IsNullOrEmpty(xattr)) return xattr;

        // Workplace shell class info becomes .CLASSINFO
        if(xattr.Equals(Xattrs.XATTR_WORKPLACE_CLASSINFO, StringComparison.Ordinal)) return ".CLASSINFO";

        // Remove com.microsoft.os2 prefix if present
        if(xattr.StartsWith(EA_PREFIX_OS2 + ".", StringComparison.Ordinal)) return xattr[EA_PREFIX_OS2.Length..];

        // Other xattr names are left untouched
        return xattr;
    }

    /// <summary>Parses EA list from a buffer and adds names to the list.</summary>
    /// <param name="buffer">Buffer containing EAs.</param>
    /// <param name="offset">Starting offset in buffer.</param>
    /// <param name="length">Length of EA data.</param>
    /// <param name="xattrs">List to add xattr names to.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ParseEaList(byte[] buffer, int offset, int length, List<string> xattrs)
    {
        var pos = 0;

        while(pos < length)
        {
            if(pos + 4 > length) break;

            if(offset + pos + 4 > buffer.Length) break;

            ExtendedAttribute ea = Marshal.ByteArrayToStructureLittleEndian<ExtendedAttribute>(buffer, offset + pos, 4);

            if(ea.namelen == 0) break;

            // Read the name (follows the 4-byte header)
            if(offset + pos + 4 + ea.namelen > buffer.Length) break;

            string eaName = Encoding.ASCII.GetString(buffer, offset + pos + 4, ea.namelen);

            xattrs.Add(ConvertEaNameToXattr(eaName));

            // Move to next EA: header (4) + name + null terminator (1) + value
            pos += 4 + ea.namelen + 1 + ea.ValueLength;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Finds an EA by name in a buffer and returns its value.</summary>
    /// <param name="buffer">Buffer containing EAs.</param>
    /// <param name="offset">Starting offset in buffer.</param>
    /// <param name="length">Length of EA data.</param>
    /// <param name="eaName">EA name to find.</param>
    /// <param name="buf">Buffer to receive EA value.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber FindEaInBuffer(byte[] buffer, int offset, int length, string eaName, ref byte[] buf)
    {
        var pos = 0;

        while(pos < length)
        {
            if(pos + 4 > length) break;

            if(offset + pos + 4 > buffer.Length) break;

            ExtendedAttribute ea = Marshal.ByteArrayToStructureLittleEndian<ExtendedAttribute>(buffer, offset + pos, 4);

            if(ea.namelen == 0) break;

            // Read the name
            if(offset + pos + 4 + ea.namelen > buffer.Length) break;

            string currentName = Encoding.ASCII.GetString(buffer, offset + pos + 4, ea.namelen);

            if(currentName.Equals(eaName, StringComparison.OrdinalIgnoreCase))
            {
                // Found the EA
                if(ea.IsIndirect)
                {
                    // Value is stored externally
                    if(offset + pos + 4 + ea.namelen + 1 + 8 > buffer.Length) return ErrorNumber.InvalidArgument;

                    ExtendedAttributeIndirectValue indirect =
                        Marshal.ByteArrayToStructureLittleEndian<ExtendedAttributeIndirectValue>(buffer,
                            offset + pos + 4 + ea.namelen + 1,
                            8);

                    return ReadIndirectEaValue(indirect.secno, ea.InAnode, indirect.length, ref buf);
                }

                // Value is inline
                int valueOffset = offset + pos + 4 + ea.namelen + 1;

                if(valueOffset + ea.ValueLength > buffer.Length) return ErrorNumber.InvalidArgument;

                buf = new byte[ea.ValueLength];
                Array.Copy(buffer, valueOffset, buf, 0, ea.ValueLength);

                return ErrorNumber.NoError;
            }

            // Move to next EA
            pos += 4 + ea.namelen + 1 + ea.ValueLength;
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>Reads external EA list from disk sectors.</summary>
    /// <param name="sector">Starting sector or anode.</param>
    /// <param name="isAnode">True if sector is an anode.</param>
    /// <param name="length">Total length of EA data.</param>
    /// <param name="xattrs">List to add xattr names to.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadExternalEaList(uint sector, bool isAnode, uint length, List<string> xattrs)
    {
        // Read external EA data
        ErrorNumber errno = ReadEaData(sector, isAnode, length, out byte[] eaData);

        if(errno != ErrorNumber.NoError) return errno;

        return ParseEaList(eaData, 0, (int)length, xattrs);
    }

    /// <summary>Finds an EA by name in external storage.</summary>
    /// <param name="sector">Starting sector or anode.</param>
    /// <param name="isAnode">True if sector is an anode.</param>
    /// <param name="length">Total length of EA data.</param>
    /// <param name="eaName">EA name to find.</param>
    /// <param name="buf">Buffer to receive EA value.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber FindExternalEa(uint sector, bool isAnode, uint length, string eaName, ref byte[] buf)
    {
        // Read external EA data
        ErrorNumber errno = ReadEaData(sector, isAnode, length, out byte[] eaData);

        if(errno != ErrorNumber.NoError) return errno;

        return FindEaInBuffer(eaData, 0, (int)length, eaName, ref buf);
    }

    /// <summary>Reads EA data from disk.</summary>
    /// <param name="sector">Starting sector or anode.</param>
    /// <param name="isAnode">True if sector is an anode.</param>
    /// <param name="length">Length of data to read.</param>
    /// <param name="data">Buffer to receive data.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadEaData(uint sector, bool isAnode, uint length, out byte[] data)
    {
        data = null;

        if(isAnode)
        {
            // Read from anode - need to follow B+ tree
            // For now, read the anode and get extents
            ErrorNumber errno = _image.ReadSector(_partition.Start + sector, false, out byte[] anodeSector, out _);

            if(errno != ErrorNumber.NoError) return errno;

            ANode anode = Marshal.ByteArrayToStructureLittleEndian<ANode>(anodeSector);

            if(anode.magic != ANODE_MAGIC) return ErrorNumber.InvalidArgument;

            // Get leaf nodes from the anode's btree
            BPlusLeafNode[] leafNodes = GetBPlusLeafNodes(anode.btree, anode.btree_data);

            // Calculate total size and read all extents
            data = new byte[length];
            var bytesRead = 0;

            foreach(BPlusLeafNode leaf in leafNodes)
            {
                if(bytesRead >= length) break;

                uint sectorsToRead = Math.Min(leaf.length,
                                              (length - (uint)bytesRead + _bytesPerSector - 1) / _bytesPerSector);

                errno = _image.ReadSectors(_partition.Start + leaf.disk_secno,
                                           false,
                                           sectorsToRead,
                                           out byte[] extentData,
                                           out _);

                if(errno != ErrorNumber.NoError) return errno;

                var bytesToCopy = (int)Math.Min(extentData.Length, length - bytesRead);
                Array.Copy(extentData, 0, data, bytesRead, bytesToCopy);
                bytesRead += bytesToCopy;
            }
        }
        else
        {
            // Read directly from consecutive sectors
            uint sectorsToRead = (length + _bytesPerSector - 1) / _bytesPerSector;

            ErrorNumber errno = _image.ReadSectors(_partition.Start + sector, false, sectorsToRead, out data, out _);

            if(errno != ErrorNumber.NoError) return errno;

            // Trim to actual length
            if(data.Length > length) Array.Resize(ref data, (int)length);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an indirect EA value from disk.</summary>
    /// <param name="sector">Starting sector or anode.</param>
    /// <param name="isAnode">True if sector is an anode.</param>
    /// <param name="length">Length of value.</param>
    /// <param name="buf">Buffer to receive value.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadIndirectEaValue(uint sector, bool isAnode, uint length, ref byte[] buf)
    {
        ErrorNumber errno = ReadEaData(sector, isAnode, length, out byte[] data);

        if(errno != ErrorNumber.NoError) return errno;

        buf = data;

        return ErrorNumber.NoError;
    }
}