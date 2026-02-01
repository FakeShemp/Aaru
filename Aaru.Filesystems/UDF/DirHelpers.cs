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

        // Read the File Entry for this directory (using partition-aware read for metadata partition support)
        ErrorNumber errno = ReadSectorFromPartition(icb.extentLocation.logicalBlockNumber,
                                                    icb.extentLocation.partitionReferenceNumber,
                                                    _partitionStartingLocation,
                                                    out byte[] feBuffer);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse as unified file entry info (handles both FileEntry and ExtendedFileEntry)
        errno = ParseFileEntryInfo(feBuffer, out UdfFileEntryInfo fileEntryInfo);

        if(errno != ErrorNumber.NoError) return errno;

        // Check this is a directory
        if(fileEntryInfo.IcbTag.fileType != FileType.Directory) return ErrorNumber.NotDirectory;

        // Read directory data based on allocation descriptor type
        // The allocation descriptor type is in bits 0-2 of the flags
        var adType = (byte)((ushort)fileEntryInfo.IcbTag.flags & 0x07);

        errno = ReadFileDataFromInfo(fileEntryInfo, feBuffer, adType, out byte[] directoryData);

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
        data = null;

        // Sanity check: reject files larger than 1GB to prevent memory exhaustion
        if(dataLength > 1073741824) // 1GB
            return ErrorNumber.InvalidArgument;

        data = new byte[dataLength];

        var dataOffset = 0;
        int adPos      = adOffset;
        int sadSize    = System.Runtime.InteropServices.Marshal.SizeOf<ShortAllocationDescriptor>();

        while(adPos < adOffset + adLength && dataOffset < dataLength)
        {
            ShortAllocationDescriptor sad =
                Marshal.ByteArrayToStructureLittleEndian<ShortAllocationDescriptor>(feBuffer, adPos, sadSize);

            // Extract length (lower 30 bits) and type (upper 2 bits)
            uint extentLength = sad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            // Short ADs don't have partition reference, use partition 0
            uint sectorsToRead = (extentLength + _sectorSize - 1) / _sectorSize;

            ErrorNumber errno = ReadSectorsFromPartition(sad.extentLocation,
                                                         0,
                                                         _partitionStartingLocation,
                                                         sectorsToRead,
                                                         out byte[] extentData);

            if(errno != ErrorNumber.NoError) return errno;

            var bytesToCopy = (int)Math.Min(extentLength, dataLength - dataOffset);
            Array.Copy(extentData, 0, data, dataOffset, bytesToCopy);
            dataOffset += bytesToCopy;

            adPos += sadSize; // ShortAllocationDescriptor size
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
        data = null;

        // Sanity check: reject files larger than 1GB to prevent memory exhaustion
        if(dataLength > 1073741824) // 1GB
            return ErrorNumber.InvalidArgument;

        data = new byte[dataLength];

        var dataOffset = 0;
        int adPos      = adOffset;
        int ladSize    = System.Runtime.InteropServices.Marshal.SizeOf<LongAllocationDescriptor>();

        while(adPos < adOffset + adLength && dataOffset < dataLength)
        {
            LongAllocationDescriptor lad =
                Marshal.ByteArrayToStructureLittleEndian<LongAllocationDescriptor>(feBuffer, adPos, ladSize);

            // Extract length (lower 30 bits) and type (upper 2 bits)
            uint extentLength = lad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            uint sectorsToRead = (extentLength + _sectorSize - 1) / _sectorSize;

            ErrorNumber errno = ReadSectorsFromPartition(lad.extentLocation.logicalBlockNumber,
                                                         lad.extentLocation.partitionReferenceNumber,
                                                         _partitionStartingLocation,
                                                         sectorsToRead,
                                                         out byte[] extentData);

            if(errno != ErrorNumber.NoError) return errno;

            var bytesToCopy = (int)Math.Min(extentLength, dataLength - dataOffset);
            Array.Copy(extentData, 0, data, dataOffset, bytesToCopy);
            dataOffset += bytesToCopy;

            adPos += ladSize; // LongAllocationDescriptor size
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
            // Normalize the search key for case-insensitive lookup
            string            normalizedKey = pieces[i].ToLowerInvariant();
            UdfDirectoryEntry entry         = null;

            foreach(KeyValuePair<string, UdfDirectoryEntry> kvp in currentDirectory)
            {
                if(kvp.Key.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    ///     Parses a buffer containing either a FileEntry or ExtendedFileEntry into a unified UdfFileEntryInfo
    /// </summary>
    /// <param name="feBuffer">The buffer containing the file entry</param>
    /// <param name="info">The parsed file entry info</param>
    /// <returns>Error number</returns>
    ErrorNumber ParseFileEntryInfo(byte[] feBuffer, out UdfFileEntryInfo info)
    {
        info = null;

        if(feBuffer == null || feBuffer.Length < 16) return ErrorNumber.InvalidArgument;

        var tagId = (TagIdentifier)BitConverter.ToUInt16(feBuffer, 0);

        switch(tagId)
        {
            case TagIdentifier.FileEntry:
            {
                FileEntry fe = Marshal.ByteArrayToStructureLittleEndian<FileEntry>(feBuffer);

                info = new UdfFileEntryInfo
                {
                    IsExtended                    = false,
                    IcbTag                        = fe.icbTag,
                    Uid                           = fe.uid,
                    Gid                           = fe.gid,
                    Permissions                   = fe.permissions,
                    FileLinkCount                 = fe.fileLinkCount,
                    InformationLength             = fe.informationLength,
                    LogicalBlocksRecorded         = fe.logicalBlocksRecorded,
                    AccessTime                    = fe.accessTime,
                    ModificationTime              = fe.modificationTime,
                    CreationTime                  = default(Timestamp), // Not available in FileEntry
                    AttributeTime                 = fe.attributeTime,
                    ExtendedAttributeICB          = fe.extendedAttributeICB,
                    StreamDirectoryICB            = default(LongAllocationDescriptor), // Not available in FileEntry
                    UniqueId                      = fe.uniqueId,
                    LengthOfExtendedAttributes    = fe.lengthOfExtendedAttributes,
                    LengthOfAllocationDescriptors = fe.lengthOfAllocationDescriptors
                };

                return ErrorNumber.NoError;
            }

            case TagIdentifier.ExtendedFileEntry:
            {
                ExtendedFileEntry efe = Marshal.ByteArrayToStructureLittleEndian<ExtendedFileEntry>(feBuffer);

                info = new UdfFileEntryInfo
                {
                    IsExtended                    = true,
                    IcbTag                        = efe.icbTag,
                    Uid                           = efe.uid,
                    Gid                           = efe.gid,
                    Permissions                   = efe.permissions,
                    FileLinkCount                 = efe.fileLinkCount,
                    InformationLength             = efe.informationLength,
                    LogicalBlocksRecorded         = efe.logicalBlocksRecorded,
                    AccessTime                    = efe.accessTime,
                    ModificationTime              = efe.modificationTime,
                    CreationTime                  = efe.creationTime,
                    AttributeTime                 = efe.attributeTime,
                    ExtendedAttributeICB          = efe.extendedAttributeICB,
                    StreamDirectoryICB            = efe.streamDirectoryICB,
                    UniqueId                      = efe.uniqueId,
                    LengthOfExtendedAttributes    = efe.lengthOfExtendedAttributes,
                    LengthOfAllocationDescriptors = efe.lengthOfAllocationDescriptors
                };

                return ErrorNumber.NoError;
            }

            default:
                return ErrorNumber.InvalidArgument;
        }
    }

    /// <summary>
    ///     Reads file data from a UdfFileEntryInfo based on allocation descriptor type
    /// </summary>
    /// <param name="info">The file entry info</param>
    /// <param name="feBuffer">The buffer containing the file entry sector</param>
    /// <param name="adType">Allocation descriptor type (0-3)</param>
    /// <param name="data">The file data</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadFileDataFromInfo(UdfFileEntryInfo info, byte[] feBuffer, byte adType, out byte[] data)
    {
        data = null;

        // Sanity check: reject files larger than 1GB to prevent memory exhaustion
        if(info.InformationLength > 1073741824) // 1GB
            return ErrorNumber.InvalidArgument;

        // The allocation descriptors follow the extended attributes in the file entry
        // FileEntry fixed size is 176 bytes, ExtendedFileEntry fixed size is 216 bytes
        int fixedSize = info.IsExtended ? 216 : 176;
        int adOffset  = fixedSize + (int)info.LengthOfExtendedAttributes;
        var adLength  = (int)info.LengthOfAllocationDescriptors;

        switch(adType)
        {
            case 0: // Short Allocation Descriptors
                return ReadDataFromShortAd(feBuffer, adOffset, adLength, (long)info.InformationLength, out data);

            case 1: // Long Allocation Descriptors
                return ReadDataFromLongAd(feBuffer, adOffset, adLength, (long)info.InformationLength, out data);

            case 2: // Extended Allocation Descriptors - not fully supported
                return ErrorNumber.NotSupported;

            case 3: // Data is embedded in the allocation descriptor area
                data = new byte[info.InformationLength];
                Array.Copy(feBuffer, adOffset, data, 0, (int)info.InformationLength);

                return ErrorNumber.NoError;

            default:
                return ErrorNumber.InvalidArgument;
        }
    }

    /// <summary>
    ///     Reads a specific byte range from a file using Short Allocation Descriptors
    ///     without loading the entire file into memory
    /// </summary>
    /// <param name="feBuffer">The buffer containing the FileEntry sector</param>
    /// <param name="adOffset">Offset within the buffer where allocation descriptors start</param>
    /// <param name="adLength">Total length of allocation descriptors in bytes</param>
    /// <param name="fileOffset">Byte offset in the file to start reading from</param>
    /// <param name="readLength">Number of bytes to read</param>
    /// <param name="buffer">Buffer to read into</param>
    /// <param name="bytesRead">Number of bytes actually read</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadDataFromShortAdRange(byte[] feBuffer, int adOffset, int adLength, long fileOffset, long readLength,
                                         byte[] buffer,   out long bytesRead)
    {
        bytesRead = 0;
        long currentOffset = 0;
        var  bufferOffset  = 0;
        int  adPos         = adOffset;
        int  sadSize       = System.Runtime.InteropServices.Marshal.SizeOf<ShortAllocationDescriptor>();

        while(adPos < adOffset + adLength && bytesRead < readLength)
        {
            ShortAllocationDescriptor sad =
                Marshal.ByteArrayToStructureLittleEndian<ShortAllocationDescriptor>(feBuffer, adPos, sadSize);

            uint extentLength = sad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            long extentEnd = currentOffset + extentLength;

            // Check if this extent contains any data we need
            if(extentEnd > fileOffset && currentOffset < fileOffset + readLength)
            {
                // Calculate the offset within this extent to start reading
                long offsetInExtent = fileOffset > currentOffset ? fileOffset - currentOffset : 0;

                // Calculate how much to read from this extent
                long toRead                                = extentLength - offsetInExtent;
                if(toRead > readLength - bytesRead) toRead = readLength - bytesRead;

                // Read the extent data
                uint sectorsToRead = (extentLength + _sectorSize - 1) / _sectorSize;

                ErrorNumber errno = ReadSectorsFromPartition(sad.extentLocation,
                                                             0,
                                                             _partitionStartingLocation,
                                                             sectorsToRead,
                                                             out byte[] extentData);

                if(errno != ErrorNumber.NoError) return errno;

                // Copy the relevant portion
                Array.Copy(extentData, (int)offsetInExtent, buffer, bufferOffset, (int)toRead);

                bytesRead    += toRead;
                bufferOffset += (int)toRead;
            }

            currentOffset += extentLength;
            adPos         += sadSize;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Reads a specific byte range from a file using Long Allocation Descriptors
    ///     without loading the entire file into memory
    /// </summary>
    ErrorNumber ReadDataFromLongAdRange(byte[] feBuffer, int adOffset, int adLength, long fileOffset, long readLength,
                                        byte[] buffer,   out long bytesRead)
    {
        bytesRead = 0;
        long currentOffset = 0;
        var  bufferOffset  = 0;
        int  adPos         = adOffset;
        int  ladSize       = System.Runtime.InteropServices.Marshal.SizeOf<LongAllocationDescriptor>();

        while(adPos < adOffset + adLength && bytesRead < readLength)
        {
            LongAllocationDescriptor lad =
                Marshal.ByteArrayToStructureLittleEndian<LongAllocationDescriptor>(feBuffer, adPos, ladSize);

            uint extentLength = lad.extentLength & 0x3FFFFFFF;

            if(extentLength == 0) break;

            long extentEnd = currentOffset + extentLength;

            // Check if this extent contains any data we need
            if(extentEnd > fileOffset && currentOffset < fileOffset + readLength)
            {
                // Calculate the offset within this extent to start reading
                long offsetInExtent = fileOffset > currentOffset ? fileOffset - currentOffset : 0;

                // Calculate how much to read from this extent
                long toRead                                = extentLength - offsetInExtent;
                if(toRead > readLength - bytesRead) toRead = readLength - bytesRead;

                // Read the extent data
                uint sectorsToRead = (extentLength + _sectorSize - 1) / _sectorSize;

                ErrorNumber errno = ReadSectorsFromPartition(lad.extentLocation.logicalBlockNumber,
                                                             lad.extentLocation.partitionReferenceNumber,
                                                             _partitionStartingLocation,
                                                             sectorsToRead,
                                                             out byte[] extentData);

                if(errno != ErrorNumber.NoError) return errno;

                // Copy the relevant portion
                Array.Copy(extentData, (int)offsetInExtent, buffer, bufferOffset, (int)toRead);

                bytesRead    += toRead;
                bufferOffset += (int)toRead;
            }

            currentOffset += extentLength;
            adPos         += ladSize;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Reads a specific byte range from a file based on allocation descriptor type,
    ///     without loading the entire file into memory
    /// </summary>
    ErrorNumber ReadFileDataFromInfoRange(UdfFileEntryInfo info,       byte[] feBuffer, byte adType, long fileOffset,
                                          long             readLength, byte[] buffer,   out long bytesRead)
    {
        bytesRead = 0;

        // Sanity checks
        if((ulong)fileOffset >= info.InformationLength) return ErrorNumber.NoError;

        if(fileOffset < 0) return ErrorNumber.InvalidArgument;

        // Adjust read length to not exceed file bounds
        if(fileOffset + readLength > (long)info.InformationLength)
            readLength = (long)info.InformationLength - fileOffset;

        int fixedSize = info.IsExtended ? 216 : 176;
        int adOffset  = fixedSize + (int)info.LengthOfExtendedAttributes;
        var adLength  = (int)info.LengthOfAllocationDescriptors;

        switch(adType)
        {
            case 0: // Short Allocation Descriptors
                return ReadDataFromShortAdRange(feBuffer,
                                                adOffset,
                                                adLength,
                                                fileOffset,
                                                readLength,
                                                buffer,
                                                out bytesRead);

            case 1: // Long Allocation Descriptors
                return ReadDataFromLongAdRange(feBuffer,
                                               adOffset,
                                               adLength,
                                               fileOffset,
                                               readLength,
                                               buffer,
                                               out bytesRead);

            case 2: // Extended Allocation Descriptors
                return ErrorNumber.NotSupported;

            case 3: // Data is embedded in the allocation descriptor area
                // For embedded data, we still need to read it all at once
                long toRead = Math.Min(readLength, (long)info.InformationLength - fileOffset);
                Array.Copy(feBuffer, adOffset + (int)fileOffset, buffer, 0, (int)toRead);
                bytesRead = toRead;

                return ErrorNumber.NoError;

            default:
                return ErrorNumber.InvalidArgument;
        }
    }

    /// <summary>
    ///     Reads the named streams (UDF 2.00+) for a file from its stream directory ICB
    /// </summary>
    /// <param name="streamDirIcb">The stream directory ICB</param>
    /// <param name="streams">List of named streams</param>
    /// <returns>Error number</returns>
    ErrorNumber ReadNamedStreams(LongAllocationDescriptor streamDirIcb, out List<UdfNamedStream> streams)
    {
        streams = [];

        // Stream directory ICB with all zeros means no streams
        if(streamDirIcb.extentLength == 0) return ErrorNumber.NoError;

        // Read the stream directory file entry (using partition-aware read for metadata partition support)
        ErrorNumber errno = ReadSectorFromPartition(streamDirIcb.extentLocation.logicalBlockNumber,
                                                    streamDirIcb.extentLocation.partitionReferenceNumber,
                                                    _partitionStartingLocation,
                                                    out byte[] sdBuffer);

        if(errno != ErrorNumber.NoError) return errno;

        errno = ParseFileEntryInfo(sdBuffer, out UdfFileEntryInfo streamDirInfo);

        if(errno != ErrorNumber.NoError) return errno;

        // Read the stream directory data
        var adType = (byte)((ushort)streamDirInfo.IcbTag.flags & 0x07);

        errno = ReadFileDataFromInfo(streamDirInfo, sdBuffer, adType, out byte[] streamDirData);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse File Identifier Descriptors for each named stream
        var offset  = 0;
        int fidSize = System.Runtime.InteropServices.Marshal.SizeOf<FileIdentifierDescriptor>();

        while(offset < streamDirData.Length)
        {
            if(offset + fidSize > streamDirData.Length) break;

            FileIdentifierDescriptor fid =
                Marshal.ByteArrayToStructureLittleEndian<FileIdentifierDescriptor>(streamDirData, offset, fidSize);

            if(fid.tag.tagIdentifier != TagIdentifier.FileIdentifierDescriptor) break;

            int fidLength = fidSize + fid.lengthOfImplementationUse + fid.lengthOfFileIdentifier;
            fidLength = fidLength + 3 & ~3;

            // Skip parent entry
            if(fid.fileCharacteristics.HasFlag(FileCharacteristics.Parent))
            {
                offset += fidLength;

                continue;
            }

            // Extract stream name
            int filenameOffset = offset + fidSize + fid.lengthOfImplementationUse;

            if(filenameOffset + fid.lengthOfFileIdentifier > streamDirData.Length) break;

            var filenameBytes = new byte[fid.lengthOfFileIdentifier];
            Array.Copy(streamDirData, filenameOffset, filenameBytes, 0, fid.lengthOfFileIdentifier);

            string streamName = StringHandlers.DecompressUnicode(filenameBytes);

            if(!string.IsNullOrEmpty(streamName))
            {
                // Read the stream file entry to get its length (using partition-aware read)
                ErrorNumber streamErr = ReadSectorFromPartition(fid.icb.extentLocation.logicalBlockNumber,
                                                                fid.icb.extentLocation.partitionReferenceNumber,
                                                                _partitionStartingLocation,
                                                                out byte[] streamBuffer);

                if(streamErr == ErrorNumber.NoError)
                {
                    if(ParseFileEntryInfo(streamBuffer, out UdfFileEntryInfo streamInfo) == ErrorNumber.NoError)
                    {
                        streams.Add(new UdfNamedStream
                        {
                            Name      = streamName,
                            XattrName = MapStreamNameToXattr(streamName),
                            Icb       = fid.icb,
                            Length    = streamInfo.InformationLength
                        });
                    }
                }
            }

            offset += fidLength;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Maps a UDF named stream name to the appropriate xattr name
    /// </summary>
    /// <param name="streamName">The UDF stream name</param>
    /// <returns>The mapped xattr name</returns>
    static string MapStreamNameToXattr(string streamName) => streamName switch
                                                             {
                                                                 STREAM_MAC_RESOURCE_FORK => "com.apple.ResourceFork",
                                                                 STREAM_OS2_EA =>
                                                                     null, // OS/2 EAs are decoded separately
                                                                 STREAM_NT_ACL => "com.microsoft.ntacl",
                                                                 STREAM_UNIX_ACL => "org.posix.acl",
                                                                 STREAM_BACKUP =>
                                                                     null, // Backup time is handled separately
                                                                 STREAM_POWER_CAL => "org.osta.udf.powercalibration",
                                                                 _ => streamName // Return as-is for other streams
                                                             };
}