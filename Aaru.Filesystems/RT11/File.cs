// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : RT-11 file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the RT-11 file system and shows information.
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
public sealed partial class RT11
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Use Stat to get file information
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        attributes = stat.Attributes;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                Inode      = _firstDirectoryBlock,
                Links      = 1,
                BlockSize  = BLOCK_SIZE_BYTES
            };

            return ErrorNumber.NoError;
        }

        // RT-11 only has a root directory (flat filesystem)
        // Remove leading slash if present
        string filename = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        // Check if the file exists in cache
        if(!_rootDirectoryCache.ContainsKey(filename)) return ErrorNumber.NoSuchFile;

        // Get file information from directory entry
        ErrorNumber error = GetFileInfo(filename, out stat);

        return error;
    }

    /// <summary>Gets file information from directory entry</summary>
    /// <param name="filename">Filename</param>
    /// <param name="stat">Output file entry info</param>
    /// <returns>Error code</returns>
    ErrorNumber GetFileInfo(string filename, out FileEntryInfo stat)
    {
        stat = null;

        // Read the first directory segment to get file information
        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + _firstDirectoryBlock,
                                                     false,
                                                     2,
                                                     out byte[] dirSegmentData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse directory segment header
        DirectorySegmentHeader segmentHeader =
            Marshal.PtrToStructure<DirectorySegmentHeader>(Marshal.UnsafeAddrOfPinnedArrayElement(dirSegmentData, 0));

        // Directory entries start after the 5-word header (10 bytes)
        var offset          = 10;
        int entrySize       = DIRECTORY_ENTRY_WORDS * 2 + segmentHeader.extraBytesPerEntry;
        var currentBlockNum = (uint)segmentHeader.dataBlockStart;

        while(offset + entrySize <= dirSegmentData.Length)
        {
            DirectoryEntry entry =
                Marshal.PtrToStructure<DirectoryEntry>(Marshal.UnsafeAddrOfPinnedArrayElement(dirSegmentData, offset));

            // Check for end-of-segment marker
            if((entry.status & 0xFF00) == E_EOS) break;

            // Process permanent files only
            if((entry.status & 0xFF00) == E_PERM)
            {
                string entryFilename = DecodeRadix50Filename(entry.filename1, entry.filename2, entry.filetype);

                if(string.Equals(entryFilename, filename, StringComparison.OrdinalIgnoreCase))
                {
                    // Build FileEntryInfo from directory entry
                    stat = new FileEntryInfo
                    {
                        Attributes = FileAttributes.File,
                        Inode      = currentBlockNum,
                        Links      = 1,
                        BlockSize  = BLOCK_SIZE_BYTES,
                        Length     = entry.length * BLOCK_SIZE_BYTES, // Convert blocks to bytes
                        Blocks     = entry.length
                    };

                    // Decode creation date if present
                    if(entry.creationDate != 0) stat.CreationTimeUtc = DecodeRT11Date(entry.creationDate);

                    return ErrorNumber.NoError;
                }

                currentBlockNum += entry.length;
            }
            else if((entry.status & 0xFF00) == E_TENT || (entry.status & 0xFF00) == E_MPTY)
                currentBlockNum += entry.length;

            offset += entrySize;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Decodes RT-11 date format to DateTime</summary>
    /// <param name="dateWord">RT-11 date word</param>
    /// <returns>DateTime in UTC</returns>
    static DateTime DecodeRT11Date(ushort dateWord)
    {
        // RT-11 date format:
        // Bits 0-4: Day (1-31)
        // Bits 5-8: Month (1-12)
        // Bits 9-13: Year (0-31, representing years since base year)
        // Bits 14-15: Age (0-3, each age represents 32 years)

        int day   = dateWord & DATE_DAY_MASK;
        int month = (dateWord & DATE_MONTH_MASK) >> DATE_MONTH_SHIFT;
        int year  = (dateWord & DATE_YEAR_MASK)  >> DATE_YEAR_SHIFT;
        int age   = (dateWord & DATE_AGE_MASK)   >> DATE_AGE_SHIFT;

        // Calculate actual year based on age
        int baseYear = age switch
                       {
                           0 => BASE_YEAR_AGE0, // 1972
                           1 => BASE_YEAR_AGE1, // 2004
                           2 => BASE_YEAR_AGE2, // 2036
                           3 => BASE_YEAR_AGE3, // 2068
                           _ => BASE_YEAR_AGE0
                       };

        int actualYear = baseYear + year;

        // Validate date components
        if(day < 1 || day > 31 || month < 1 || month > 12) return DateTime.UnixEpoch;

        try
        {
            return new DateTime(actualYear, month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        catch
        {
            return DateTime.UnixEpoch;
        }
    }

    /// <summary>Gets the file length from cached directory information</summary>
    /// <param name="filename">Filename</param>
    /// <param name="fileLength">Output file length in blocks</param>
    /// <returns>Error code</returns>
    ErrorNumber GetFileLengthFromCache(string filename, out uint fileLength)
    {
        fileLength = 0;

        if(!_rootDirectoryCache.ContainsKey(filename)) return ErrorNumber.NoSuchFile;

        // Read the first directory segment to get file length
        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + _firstDirectoryBlock,
                                                     false,
                                                     2,
                                                     out byte[] dirSegmentData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse directory segment header
        DirectorySegmentHeader segmentHeader =
            Marshal.PtrToStructure<DirectorySegmentHeader>(Marshal.UnsafeAddrOfPinnedArrayElement(dirSegmentData, 0));

        // Directory entries start after the 5-word header (10 bytes)
        var offset    = 10;
        int entrySize = DIRECTORY_ENTRY_WORDS * 2 + segmentHeader.extraBytesPerEntry;

        while(offset + entrySize <= dirSegmentData.Length)
        {
            DirectoryEntry entry =
                Marshal.PtrToStructure<DirectoryEntry>(Marshal.UnsafeAddrOfPinnedArrayElement(dirSegmentData, offset));

            // Check for end-of-segment marker
            if((entry.status & 0xFF00) == E_EOS) break;

            // Process permanent files only
            if((entry.status & 0xFF00) == E_PERM)
            {
                string entryFilename = DecodeRadix50Filename(entry.filename1, entry.filename2, entry.filetype);

                if(string.Equals(entryFilename, filename, StringComparison.OrdinalIgnoreCase))
                {
                    fileLength = entry.length;

                    return ErrorNumber.NoError;
                }
            }

            offset += entrySize;
        }

        return ErrorNumber.NoSuchFile;
    }
}