// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System Plus plugin.
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

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrEmpty(path) ? "/" : path;

        xattrs = [];

        // Get file entry
        ErrorNumber error = GetFileEntry(normalizedPath, out CatalogEntry entry);

        if(error != ErrorNumber.NoError) return error;

        // Only files can have these xattrs, not directories
        if(entry is not FileEntry fileEntry) return ErrorNumber.NoError;

        // Add Finder Info xattr (always present for files)
        xattrs.Add("com.apple.FinderInfo");

        // Add HFS+ creator and type xattrs
        xattrs.Add("hfs.creator");
        xattrs.Add("hfs.type");

        // Add Resource Fork xattr if it exists and is non-empty
        if(fileEntry.ResourceForkLogicalSize > 0) xattrs.Add("com.apple.ResourceFork");

        xattrs.Sort();

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        buf = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(xattr)) return ErrorNumber.InvalidArgument;

        // Normalize path
        string normalizedPath = string.IsNullOrEmpty(path) ? "/" : path;

        // Get file entry
        ErrorNumber error = GetFileEntry(normalizedPath, out CatalogEntry entry);

        if(error != ErrorNumber.NoError) return error;

        // Only files can have these xattrs
        if(entry is not FileEntry fileEntry) return ErrorNumber.NoSuchExtendedAttribute;

        // Handle Finder Info xattr: concatenate FInfo (16 bytes) + FXInfo (16 bytes) = 32 bytes
        if(string.Equals(xattr, "com.apple.FinderInfo", StringComparison.OrdinalIgnoreCase))
        {
            buf = new byte[32];

            // FInfo (16 bytes)
            byte[] finderInfoBytes = Marshal.StructureToByteArrayBigEndian(fileEntry.FinderInfo);
            Array.Copy(finderInfoBytes, 0, buf, 0, Math.Min(finderInfoBytes.Length, 16));

            // FXInfo (16 bytes)
            byte[] extendedFinderInfoBytes = Marshal.StructureToByteArrayBigEndian(fileEntry.ExtendedFinderInfo);
            Array.Copy(extendedFinderInfoBytes, 0, buf, 16, Math.Min(extendedFinderInfoBytes.Length, 16));

            return ErrorNumber.NoError;
        }

        // Handle HFS+ creator xattr (4 bytes, as stored)
        if(string.Equals(xattr, "hfs.creator", StringComparison.OrdinalIgnoreCase))
        {
            buf = BitConverter.GetBytes(fileEntry.FinderInfo.fdCreator);

            return ErrorNumber.NoError;
        }

        // Handle HFS+ type xattr (4 bytes, as stored)
        if(string.Equals(xattr, "hfs.type", StringComparison.OrdinalIgnoreCase))
        {
            buf = BitConverter.GetBytes(fileEntry.FinderInfo.fdType);

            return ErrorNumber.NoError;
        }

        // Handle Resource Fork xattr
        if(string.Equals(xattr, "com.apple.ResourceFork", StringComparison.OrdinalIgnoreCase))
        {
            return fileEntry.ResourceForkLogicalSize == 0
                       ? ErrorNumber.NoSuchExtendedAttribute
                       : ReadResourceFork(fileEntry, out buf);
        }

        return ErrorNumber.NoSuchExtendedAttribute;
    }

    /// <summary>Gets a catalog entry (file or directory) by path</summary>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="entry">The catalog entry if found</param>
    /// <returns>Error number</returns>
    private ErrorNumber GetFileEntry(string path, out CatalogEntry entry)
    {
        entry = null;

        // Normalize path
        string normalizedPath = string.IsNullOrEmpty(path) ? "/" : path;

        // Root directory case
        if(string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
            return ErrorNumber.InvalidArgument; // Root is a directory, not a file

        // Parse path components
        string cutPath = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                             ? normalizedPath[1..]
                             : normalizedPath;

        string[] pieces = cutPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pieces.Length == 0) return ErrorNumber.NoSuchFile;

        // Start from root directory
        Dictionary<string, CatalogEntry> currentDirectory = _rootDirectoryCache;
        uint                             currentDirCNID   = kHFSRootFolderID;

        // Traverse through all but the last path component
        for(var p = 0; p < pieces.Length - 1; p++)
        {
            string component = pieces[p];

            // Convert colons back to slashes for catalog lookup
            component = component.Replace(":", "/");

            // Look for the component in current directory
            CatalogEntry foundEntry = null;

            if(currentDirectory != null)
            {
                foreach(CatalogEntry catalogEntry in currentDirectory.Values)
                {
                    if(CompareNames(catalogEntry.Name, component))
                    {
                        foundEntry = catalogEntry;

                        break;
                    }
                }
            }

            if(foundEntry == null) return ErrorNumber.NoSuchFile;

            // Check if it's a directory
            if(foundEntry.Type != (int)BTreeRecordType.kHFSPlusFolderRecord) return ErrorNumber.NotDirectory;

            // Update current directory info
            currentDirCNID = foundEntry.CNID;

            // Load next directory level
            ErrorNumber cacheErr = CacheDirectoryIfNeeded(currentDirCNID);

            if(cacheErr != ErrorNumber.NoError) return cacheErr;

            currentDirectory = GetDirectoryEntries(currentDirCNID);

            if(currentDirectory == null) return ErrorNumber.NoSuchFile;
        }

        // Now look for the final component
        string lastComponent = pieces[^1];

        // Convert colons back to slashes for catalog lookup
        lastComponent = lastComponent.Replace(":", "/");

        if(currentDirectory != null)
        {
            foreach(CatalogEntry catalogEntry in currentDirectory.Values)
            {
                if(CompareNames(catalogEntry.Name, lastComponent))
                {
                    entry = catalogEntry;

                    return ErrorNumber.NoError;
                }
            }
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Reads the resource fork of a file</summary>
    /// <param name="fileEntry">The file entry to read the resource fork from</param>
    /// <param name="buf">Buffer containing the resource fork data</param>
    /// <returns>Error number</returns>
    private ErrorNumber ReadResourceFork(FileEntry fileEntry, out byte[] buf)
    {
        buf = null;

        if(fileEntry.ResourceForkLogicalSize == 0) return ErrorNumber.NoSuchExtendedAttribute;

        ulong logicalSize = fileEntry.ResourceForkLogicalSize;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadResourceFork: Reading {0} bytes from resource fork (CNID={1})",
                          logicalSize,
                          fileEntry.CNID);

        buf = new byte[logicalSize];
        ulong bytesRead = 0;

        // Get all extents for this fork (may include overflow extents from Extents Overflow File)
        ErrorNumber extentErr = GetResourceForkExtents(fileEntry, out List<HFSPlusExtentDescriptor> allExtents);

        if(extentErr != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ReadResourceFork: Failed to get extents for resource fork of file {0}: {1}",
                              fileEntry.CNID,
                              extentErr);

            return extentErr;
        }

        if(allExtents == null || allExtents.Count == 0)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ReadResourceFork: No extents found for resource fork of file {0}",
                              fileEntry.CNID);

            buf = [];

            return ErrorNumber.NoError;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "ReadResourceFork: Got {0} total extents for resource fork of file {1}",
                          allExtents.Count,
                          fileEntry.CNID);

        // Read each extent
        foreach(HFSPlusExtentDescriptor extent in allExtents)
        {
            if(extent.blockCount == 0) break;

            ulong extentSizeBytes = (ulong)extent.blockCount * _volumeHeader.blockSize;
            ulong toRead          = Math.Min(extentSizeBytes, logicalSize - bytesRead);

            // Calculate sector offset for this extent
            // HFS+ uses allocation blocks as the unit, which are blockSize bytes
            ulong blockOffsetBytes = (ulong)extent.startBlock * _volumeHeader.blockSize;

            // Convert to device sector address
            // For wrapped volumes, blocks start after the HFS+ volume offset
            // For pure HFS+, _hfsPlusVolumeOffset is 0
            ulong deviceSector = ((_partitionStart + _hfsPlusVolumeOffset) * _sectorSize + blockOffsetBytes) /
                                 _sectorSize;

            var byteOffset = (uint)(((_partitionStart + _hfsPlusVolumeOffset) * _sectorSize + blockOffsetBytes) %
                                    _sectorSize);

            var sectorsToRead = (uint)((toRead + byteOffset + _sectorSize - 1) / _sectorSize);

            AaruLogging.Debug(MODULE_NAME,
                              "ReadResourceFork: Reading extent: startBlock={0}, blockCount={1}, size={2} bytes at sector {3}",
                              extent.startBlock,
                              extent.blockCount,
                              toRead,
                              deviceSector);

            ErrorNumber readErr = _imagePlugin.ReadSectors(deviceSector,
                                                           false,
                                                           sectorsToRead,
                                                           out byte[] sectorData,
                                                           out _);

            if(readErr != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadResourceFork: Failed to read sectors: {0}", readErr);

                return readErr;
            }

            if(sectorData == null || sectorData.Length == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "ReadResourceFork: Got empty sector data");

                return ErrorNumber.InvalidArgument;
            }

            // Copy data from sector buffer to output buffer, accounting for offset
            uint bytesToCopy = Math.Min((uint)toRead, (uint)(sectorData.Length - byteOffset));

            AaruLogging.Debug(MODULE_NAME,
                              "ReadResourceFork: Read {0} bytes from extent, copying {1} bytes from offset {2}",
                              sectorData.Length,
                              bytesToCopy,
                              byteOffset);

            Array.Copy(sectorData, (int)byteOffset, buf, (int)bytesRead, bytesToCopy);

            bytesRead += bytesToCopy;

            if(bytesRead >= logicalSize) break;
        }

        if(bytesRead < logicalSize)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ReadResourceFork: Warning - Read only {0} bytes out of {1} bytes for resource fork of file {2}",
                              bytesRead,
                              logicalSize,
                              fileEntry.CNID);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Gets all extents for a resource fork, including overflow extents from the Extents Overflow File</summary>
    /// <param name="fileEntry">The file entry containing the resource fork</param>
    /// <param name="allExtents">List of all extent descriptors for the resource fork</param>
    /// <returns>Error number</returns>
    private ErrorNumber GetResourceForkExtents(FileEntry fileEntry, out List<HFSPlusExtentDescriptor> allExtents)
    {
        allExtents = [];

        // Add the first 8 extents from the fork data
        for(var i = 0; i < fileEntry.ResourceForkExtents.extentDescriptors.Length; i++)
        {
            HFSPlusExtentDescriptor extent = fileEntry.ResourceForkExtents.extentDescriptors[i];

            if(extent.blockCount == 0) break;

            allExtents.Add(extent);

            AaruLogging.Debug(MODULE_NAME,
                              "GetResourceForkExtents: Adding extent from fork data: startBlock={0}, blockCount={1}",
                              extent.startBlock,
                              extent.blockCount);
        }

        // If we've got all extents (less than 8), we're done
        if(allExtents.Count < 8) return ErrorNumber.NoError;

        // If total blocks match, we have all extents
        uint totalBlocksFromExtents                                                  = 0;
        foreach(HFSPlusExtentDescriptor extent in allExtents) totalBlocksFromExtents += extent.blockCount;

        if(totalBlocksFromExtents >= fileEntry.ResourceForkTotalBlocks) return ErrorNumber.NoError;

        // We need to read overflow extents from the Extents Overflow File
        AaruLogging.Debug(MODULE_NAME,
                          "GetResourceForkExtents: Need to read overflow extents for resource fork (CNID={0})",
                          fileEntry.CNID);

        // Read overflow extents from the Extents Overflow File
        ErrorNumber overflowErr = ReadResourceForkOverflowExtents(fileEntry, allExtents);

        if(overflowErr != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "GetResourceForkExtents: Failed to read overflow extents: {0}", overflowErr);

            return overflowErr;
        }

        return ErrorNumber.NoError;
    }
}