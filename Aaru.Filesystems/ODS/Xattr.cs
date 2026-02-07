// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Extended attributes for the Files-11 On-Disk Structure.
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
using System.Runtime.InteropServices;
using System.Text;
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

public sealed partial class ODS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Get file header
        ErrorNumber errno;
        FileHeader  fileHeader;
        byte[]      rawHeader;

        if(normalizedPath == "/")
        {
            errno = ReadFileHeaderRaw(MFD_FID, out fileHeader, out rawHeader);

            if(errno != ErrorNumber.NoError) return errno;
        }
        else
        {
            errno = LookupFile(normalizedPath, out CachedFile cachedFile);

            if(errno != ErrorNumber.NoError) return errno;

            errno = ReadFileHeaderRaw(cachedFile.Fid.num, out fileHeader, out rawHeader);

            if(errno != ErrorNumber.NoError) return errno;
        }

        xattrs = [];

        // Expose the FAT (File Attributes record) if it contains non-null data
        if(HasNonNullFat(rawHeader)) xattrs.Add(Xattrs.XATTR_VMS_FAT);

        // Expose the file organization only if it's NOT sequential (sequential is the default)
        if(fileHeader.recattr.Organization != FileOrganization.Sequential) xattrs.Add(Xattrs.XATTR_VMS_ORGANIZATION);

        // Check if there's an ACL (access control area has non-null data)
        if(HasAcl(fileHeader, rawHeader)) xattrs.Add(Xattrs.XATTR_VMS_ACL);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        // Get file header
        ErrorNumber errno;
        FileHeader  fileHeader;
        byte[]      rawHeader;

        if(normalizedPath == "/")
        {
            errno = ReadFileHeaderRaw(MFD_FID, out fileHeader, out rawHeader);

            if(errno != ErrorNumber.NoError) return errno;
        }
        else
        {
            errno = LookupFile(normalizedPath, out CachedFile cachedFile);

            if(errno != ErrorNumber.NoError) return errno;

            errno = ReadFileHeaderRaw(cachedFile.Fid.num, out fileHeader, out rawHeader);

            if(errno != ErrorNumber.NoError) return errno;
        }

        switch(xattr)
        {
            case Xattrs.XATTR_VMS_FAT:
                // Return the raw FAT (File Attributes) structure (32 bytes)
                if(!HasNonNullFat(rawHeader)) return ErrorNumber.NoSuchExtendedAttribute;

                buf = new byte[Marshal.SizeOf<FileAttributes>()];
                Buffer.BlockCopy(rawHeader, 0x14, buf, 0, buf.Length);

                return ErrorNumber.NoError;

            case Xattrs.XATTR_VMS_ORGANIZATION:
                // Return the file organization as a human-readable string
                // Don't expose sequential organization (it's the default)
                if(fileHeader.recattr.Organization == FileOrganization.Sequential)
                    return ErrorNumber.NoSuchExtendedAttribute;

                string orgName = fileHeader.recattr.Organization switch
                                 {
                                     FileOrganization.Relative => "relative",
                                     FileOrganization.Indexed  => "indexed",
                                     FileOrganization.Direct   => "direct",
                                     FileOrganization.Special  => "special",
                                     _                         => "unknown"
                                 };

                buf = Encoding.ASCII.GetBytes(orgName);

                return ErrorNumber.NoError;

            case Xattrs.XATTR_VMS_ACL:
                // Return the raw ACL data
                if(!HasAcl(fileHeader, rawHeader)) return ErrorNumber.NoSuchExtendedAttribute;

                // ACL is in the access control area (between acoffset and rsoffset)
                int aclOffset = fileHeader.acoffset * 2;
                int aclEnd    = fileHeader.rsoffset * 2;
                int aclSize   = aclEnd - aclOffset;

                if(aclSize <= 0 || aclOffset >= rawHeader.Length) return ErrorNumber.NoSuchExtendedAttribute;

                // Clamp to available data
                if(aclOffset + aclSize > rawHeader.Length) aclSize = rawHeader.Length - aclOffset;

                buf = new byte[aclSize];
                Buffer.BlockCopy(rawHeader, aclOffset, buf, 0, aclSize);

                return ErrorNumber.NoError;

            default:
                return ErrorNumber.NoSuchExtendedAttribute;
        }
    }

    /// <summary>Checks if the FAT (File Attributes) area contains non-null data.</summary>
    /// <param name="rawHeader">Raw file header bytes.</param>
    /// <returns>True if the FAT contains non-null data.</returns>
    static bool HasNonNullFat(byte[] rawHeader)
    {
        if(rawHeader == null || rawHeader.Length < 0x14 + 32) return false;

        // Check if all FAT bytes are null
        for(var i = 0x14; i < 0x14 + 32; i++)
        {
            if(rawHeader[i] != 0) return true;
        }

        return false;
    }

    /// <summary>Checks if a file header has an ACL (Access Control List) with non-null data.</summary>
    /// <param name="header">File header.</param>
    /// <param name="rawHeader">Raw file header bytes.</param>
    /// <returns>True if the file has non-null ACL data.</returns>
    static bool HasAcl(in FileHeader header, byte[] rawHeader)
    {
        // ACL is not present if BadAcl flag is set
        if(header.filechar.HasFlag(FileCharacteristicFlags.BadAcl)) return false;

        // ACL area must have non-zero size
        if(header.acoffset >= header.rsoffset) return false;

        // Calculate ACL area bounds
        int aclOffset = header.acoffset * 2;
        int aclEnd    = header.rsoffset * 2;
        int aclSize   = aclEnd - aclOffset;

        if(aclSize <= 0 || aclOffset >= rawHeader.Length) return false;

        // Clamp to available data
        if(aclOffset + aclSize > rawHeader.Length) aclSize = rawHeader.Length - aclOffset;

        // Check if all ACL bytes are null
        for(var i = 0; i < aclSize; i++)
        {
            if(rawHeader[aclOffset + i] != 0) return true;
        }

        return false;
    }

    /// <summary>Reads a file header by file ID and also returns the raw bytes.</summary>
    /// <param name="fileNum">File number (1-based).</param>
    /// <param name="header">Output file header.</param>
    /// <param name="rawHeader">Output raw header bytes.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadFileHeaderRaw(ushort fileNum, out FileHeader header, out byte[] rawHeader)
    {
        header    = default(FileHeader);
        rawHeader = null;

        // File header LBN = ibmaplbn + ibmapsize + (fileNum - 1)
        uint headerLbn = _homeBlock.ibmaplbn + _homeBlock.ibmapsize + fileNum - 1;

        ErrorNumber errno = ReadOdsBlock(_image, _partition, headerLbn, out byte[] headerSector);

        if(errno != ErrorNumber.NoError) return errno;

        header = Helpers.Marshal.ByteArrayToStructureLittleEndian<FileHeader>(headerSector);

        // Validate file header checksum
        ushort calculatedChecksum = 0;

        for(var i = 0; i < 0x1FE; i += 2) calculatedChecksum += BitConverter.ToUInt16(headerSector, i);

        if(calculatedChecksum != header.checksum) return ErrorNumber.InvalidArgument;

        // Validate structure level
        var headerStrucLevel = (byte)(header.struclev >> 8 & 0xFF);

        if(headerStrucLevel != 2 && headerStrucLevel != 5) return ErrorNumber.InvalidArgument;

        // Validate offsets are in correct order
        if(header.idoffset > header.mpoffset || header.mpoffset > header.acoffset || header.acoffset > header.rsoffset)
            return ErrorNumber.InvalidArgument;

        rawHeader = headerSector;

        return ErrorNumber.NoError;
    }
}