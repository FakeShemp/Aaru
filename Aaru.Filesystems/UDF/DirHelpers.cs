// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DirHelpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Universal Disk Format plugin.
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

public sealed partial class UDF
{
    /// <summary>
    ///     Reads the contents of a directory from its ICB
    /// </summary>
    /// <param name="icb">ICB of the directory</param>
    /// <param name="entries">Dictionary of directory entries keyed by filename</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadDirectoryContents(LongAllocationDescriptor icb, out Dictionary<string, UdfDirectoryEntry> entries)
    {
        entries = [];

        // Read the File Entry for this directory
        ulong fileEntrySector = TranslateLogicalBlock(icb.extentLocation.logicalBlockNumber,
                                                      icb.extentLocation.partitionReferenceNumber,
                                                      _partitionStartingLocation);

        if(_imagePlugin.ReadSector(fileEntrySector, false, out byte[] feBuffer, out _) != ErrorNumber.NoError)
            return ErrorNumber.InvalidArgument;

        FileEntry fileEntry = Marshal.ByteArrayToStructureLittleEndian<FileEntry>(feBuffer);

        if(fileEntry.tag.tagIdentifier != TagIdentifier.FileEntry) return ErrorNumber.InvalidArgument;

        // Check this is a directory
        if(fileEntry.icbTag.fileType != FileType.Directory) return ErrorNumber.NotDirectory;

        // Read directory data based on allocation descriptor type
        // The allocation descriptor type is in bits 0-2 of the flags
        var adType = (byte)((ushort)fileEntry.icbTag.flags & 0x07);

        ErrorNumber errno = ReadFileData(fileEntry, feBuffer, adType, out byte[] directoryData);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse File Identifier Descriptors from directory data
        var offset  = 0;
        int fidSize = System.Runtime.InteropServices.Marshal.SizeOf<FileIdentifierDescriptor>();

        while(offset < directoryData.Length)
        {
            // Check we have enough data for the fixed part of the FID
            if(offset + fidSize > directoryData.Length) break;

            FileIdentifierDescriptor fid =
                Marshal.ByteArrayToStructureLittleEndian<FileIdentifierDescriptor>(directoryData, offset, fidSize);

            if(fid.tag.tagIdentifier != TagIdentifier.FileIdentifierDescriptor) break;

            // Calculate the total length of this FID entry
            // Fixed part + lengthOfImplementationUse + lengthOfFileIdentifier + padding to 4-byte boundary
            int fidLength = fidSize + fid.lengthOfImplementationUse + fid.lengthOfFileIdentifier;

            // Pad to 4-byte boundary
            fidLength = fidLength + 3 & ~3;

            // Skip parent directory entry (has Parent flag and empty filename)
            if(fid.fileCharacteristics.HasFlag(FileCharacteristics.Parent))
            {
                offset += fidLength;

                continue;
            }

            // Skip deleted entries
            if(fid.fileCharacteristics.HasFlag(FileCharacteristics.Deleted))
            {
                offset += fidLength;

                continue;
            }

            // Extract the filename
            // The filename is at offset fidSize + lengthOfImplementationUse
            int filenameOffset = offset + fidSize + fid.lengthOfImplementationUse;

            if(filenameOffset + fid.lengthOfFileIdentifier > directoryData.Length) break;

            var filenameBytes = new byte[fid.lengthOfFileIdentifier];
            Array.Copy(directoryData, filenameOffset, filenameBytes, 0, fid.lengthOfFileIdentifier);

            string filename = StringHandlers.DecompressUnicode(filenameBytes);

            if(!string.IsNullOrEmpty(filename))
            {
                entries[filename] = new UdfDirectoryEntry
                {
                    Filename            = filename,
                    Icb                 = fid.icb,
                    FileCharacteristics = fid.fileCharacteristics
                };
            }

            offset += fidLength;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Reads file data from a FileEntry based on allocation descriptor type
    /// </summary>
    /// <param name="fileEntry">The file entry</param>
    /// <param name="feBuffer">The buffer containing the FileEntry sector</param>
    /// <param name="adType">Allocation descriptor type (0-3)</param>
    /// <param name="data">The file data</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadFileData(FileEntry fileEntry, byte[] feBuffer, byte adType, out byte[] data)
    {
        data = null;

        // The allocation descriptors follow the extended attributes in the FileEntry
        // FileEntry fixed size is 176 bytes
        const int fileEntryFixedSize = 176;
        int       adOffset           = fileEntryFixedSize + (int)fileEntry.lengthOfExtendedAttributes;
        var       adLength           = (int)fileEntry.lengthOfAllocationDescriptors;


        switch(adType)
        {
            case 0: // Short Allocation Descriptors
                return ReadDataFromShortAd(feBuffer, adOffset, adLength, (long)fileEntry.informationLength, out data);

            case 1: // Long Allocation Descriptors
                return ReadDataFromLongAd(feBuffer, adOffset, adLength, (long)fileEntry.informationLength, out data);

            case 2: // Extended Allocation Descriptors - not supported in UDF 1.02
                return ErrorNumber.NotSupported;

            case 3: // Data is embedded in the allocation descriptor area
                data = new byte[fileEntry.informationLength];
                Array.Copy(feBuffer, adOffset, data, 0, (int)fileEntry.informationLength);

                return ErrorNumber.NoError;

            default:
                return ErrorNumber.InvalidArgument;
        }
    }

    /// <summary>
    ///     Reads file data using Short Allocation Descriptors (8-byte descriptors with partition-relative addresses)
    /// </summary>
    /// <param name="feBuffer">The buffer containing the FileEntry sector</param>
    /// <param name="adOffset">Offset within the buffer where allocation descriptors start</param>
    /// <param name="adLength">Total length of allocation descriptors in bytes</param>
    /// <param name="dataLength">Expected file data length</param>
    /// <param name="data">The file data read from the extents</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadDataFromShortAd(byte[] feBuffer, int adOffset, int adLength, long dataLength, out byte[] data)
    {
        data = new byte[dataLength];

        var dataOffset = 0;
        int adPos      = adOffset;

        while(adPos < adOffset + adLength && dataOffset < dataLength)
        {
            ShortAllocationDescriptor sad =
                Marshal.ByteArrayToStructureLittleEndian<ShortAllocationDescriptor>(feBuffer,
                    adPos,
                    System.Runtime.InteropServices.Marshal.SizeOf<ShortAllocationDescriptor>());

            // Extract length (lower 30 bits) and type (upper 2 bits)
            uint extentLength = sad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            // Short ADs don't have partition reference, use partition 0
            ulong extentSector  = TranslateLogicalBlock(sad.extentLocation, 0, _partitionStartingLocation);
            uint  sectorsToRead = (extentLength + _sectorSize - 1) / _sectorSize;

            if(_imagePlugin.ReadSectors(extentSector, false, sectorsToRead, out byte[] extentData, out _) !=
               ErrorNumber.NoError)
                return ErrorNumber.InvalidArgument;

            var bytesToCopy = (int)Math.Min(extentLength, dataLength - dataOffset);
            Array.Copy(extentData, 0, data, dataOffset, bytesToCopy);
            dataOffset += bytesToCopy;

            adPos += 8; // ShortAllocationDescriptor is 8 bytes
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Reads file data using Long Allocation Descriptors (16-byte descriptors with partition reference numbers)
    /// </summary>
    /// <param name="feBuffer">The buffer containing the FileEntry sector</param>
    /// <param name="adOffset">Offset within the buffer where allocation descriptors start</param>
    /// <param name="adLength">Total length of allocation descriptors in bytes</param>
    /// <param name="dataLength">Expected file data length</param>
    /// <param name="data">The file data read from the extents</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadDataFromLongAd(byte[] feBuffer, int adOffset, int adLength, long dataLength, out byte[] data)
    {
        data = new byte[dataLength];

        var dataOffset = 0;
        int adPos      = adOffset;

        while(adPos < adOffset + adLength && dataOffset < dataLength)
        {
            LongAllocationDescriptor lad =
                Marshal.ByteArrayToStructureLittleEndian<LongAllocationDescriptor>(feBuffer,
                    adPos,
                    System.Runtime.InteropServices.Marshal.SizeOf<LongAllocationDescriptor>());

            // Extract length (lower 30 bits) and type (upper 2 bits)
            uint extentLength = lad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            ulong extentSector = TranslateLogicalBlock(lad.extentLocation.logicalBlockNumber,
                                                       lad.extentLocation.partitionReferenceNumber,
                                                       _partitionStartingLocation);

            uint sectorsToRead = (extentLength + _sectorSize - 1) / _sectorSize;

            if(_imagePlugin.ReadSectors(extentSector, false, sectorsToRead, out byte[] extentData, out _) !=
               ErrorNumber.NoError)
                return ErrorNumber.InvalidArgument;

            var bytesToCopy = (int)Math.Min(extentLength, dataLength - dataOffset);
            Array.Copy(extentData, 0, data, dataOffset, bytesToCopy);
            dataOffset += bytesToCopy;

            adPos += 16; // LongAllocationDescriptor is 16 bytes
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Traverses the directory tree to find a directory at a given path
    /// </summary>
    /// <param name="path">Path to the directory</param>
    /// <param name="entries">Directory entries if found</param>
    /// <returns>Error number</returns>
    ErrorNumber GetDirectoryEntries(string path, out Dictionary<string, UdfDirectoryEntry> entries)
    {
        entries = null;

        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            entries = _rootDirectoryCache;

            return ErrorNumber.NoError;
        }

        string cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        // Check cache first
        if(_directoryCache.TryGetValue(cutPath.ToLowerInvariant(), out entries)) return ErrorNumber.NoError;

        string[] pieces = cutPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Dictionary<string, UdfDirectoryEntry> currentDirectory = _rootDirectoryCache;
        var                                   currentPath      = "";

        for(var i = 0; i < pieces.Length; i++)
        {
            // Find the entry in current directory (case-insensitive)
            UdfDirectoryEntry entry = null;

            foreach(KeyValuePair<string, UdfDirectoryEntry> kvp in currentDirectory)
            {
                if(kvp.Key.Equals(pieces[i], StringComparison.OrdinalIgnoreCase))
                {
                    entry = kvp.Value;

                    break;
                }
            }

            if(entry == null) return ErrorNumber.NoSuchFile;

            if(!entry.FileCharacteristics.HasFlag(FileCharacteristics.Directory)) return ErrorNumber.NotDirectory;

            currentPath = i == 0 ? pieces[0] : $"{currentPath}/{pieces[i]}";

            // Check cache for this path
            if(_directoryCache.TryGetValue(currentPath.ToLowerInvariant(), out currentDirectory)) continue;

            // Read directory contents
            ErrorNumber errno = ReadDirectoryContents(entry.Icb, out currentDirectory);

            if(errno != ErrorNumber.NoError) return errno;

            // Cache this directory
            _directoryCache[currentPath.ToLowerInvariant()] = currentDirectory;
        }

        entries = currentDirectory;

        return ErrorNumber.NoError;
    }
}