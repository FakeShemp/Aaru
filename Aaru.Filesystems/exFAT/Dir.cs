// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft exFAT filesystem plugin.
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
using System.Linq;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
public sealed partial class exFAT
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetDirectoryEntries(path, out Dictionary<string, CompleteDirectoryEntry> entries);

        if(errno != ErrorNumber.NoError) return errno;

        node = new ExFatDirNode
        {
            Path     = path,
            Position = 0,
            Entries  = entries.Values.ToArray()
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not ExFatDirNode myNode) return ErrorNumber.InvalidArgument;

        myNode.Position = -1;
        myNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not ExFatDirNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(myNode.Position >= myNode.Entries.Length) return ErrorNumber.NoError;

        filename = myNode.Entries[myNode.Position].FileName;
        myNode.Position++;

        return ErrorNumber.NoError;
    }

    /// <summary>Gets directory entries for a given path, using cache when available.</summary>
    /// <param name="path">Path to the directory.</param>
    /// <param name="entries">Directory entries if found.</param>
    /// <returns>Error number.</returns>
    ErrorNumber GetDirectoryEntries(string path, out Dictionary<string, CompleteDirectoryEntry> entries)
    {
        entries = null;

        // Root directory
        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            entries = _rootDirectoryCache;

            return ErrorNumber.NoError;
        }

        string cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        // Check cache first
        if(_directoryCache.TryGetValue(cutPath, out entries)) return ErrorNumber.NoError;

        string[] pieces = cutPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Dictionary<string, CompleteDirectoryEntry> currentDirectory = _rootDirectoryCache;
        var                                        currentPath      = "";

        for(var i = 0; i < pieces.Length; i++)
        {
            // Find the entry in current directory (case-insensitive per exFAT spec)
            CompleteDirectoryEntry entry = null;

            foreach(KeyValuePair<string, CompleteDirectoryEntry> kvp in currentDirectory)
            {
                if(kvp.Key.Equals(pieces[i], StringComparison.OrdinalIgnoreCase))
                {
                    entry = kvp.Value;

                    break;
                }
            }

            if(entry == null) return ErrorNumber.NoSuchFile;

            if(!entry.IsDirectory) return ErrorNumber.NotDirectory;

            currentPath = i == 0 ? pieces[0] : $"{currentPath}/{pieces[i]}";

            // Check cache for this path
            if(_directoryCache.TryGetValue(currentPath, out currentDirectory)) continue;

            // Read directory contents
            ErrorNumber errno = ReadDirectoryContents(entry, out currentDirectory);

            if(errno != ErrorNumber.NoError) return errno;

            // Cache this directory
            _directoryCache[currentPath] = currentDirectory;
        }

        entries = currentDirectory;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and parses directory contents from a directory entry.</summary>
    /// <param name="dirEntry">The directory entry to read.</param>
    /// <param name="entries">Parsed directory entries.</param>
    /// <returns>Error number.</returns>
    ErrorNumber ReadDirectoryContents(CompleteDirectoryEntry                         dirEntry,
                                      out Dictionary<string, CompleteDirectoryEntry> entries)
    {
        entries = new Dictionary<string, CompleteDirectoryEntry>();

        if(dirEntry.FirstCluster < 2) return ErrorNumber.NoError; // Empty directory

        byte[] directoryData = ReadClusterChain(dirEntry.FirstCluster, dirEntry.IsContiguous, dirEntry.DataLength);

        if(directoryData == null) return ErrorNumber.InvalidArgument;

        ParseDirectoryContents(directoryData, entries);

        return ErrorNumber.NoError;
    }

    /// <summary>Parses directory data into entries without modifying volume metadata.</summary>
    /// <param name="directoryData">Raw directory data.</param>
    /// <param name="cache">Cache to populate with parsed entries.</param>
    void ParseDirectoryContents(byte[] directoryData, Dictionary<string, CompleteDirectoryEntry> cache)
    {
        const int entrySize = 32;

        for(var i = 0; i < directoryData.Length; i += entrySize)
        {
            byte entryType = directoryData[i];

            // End of directory
            if(entryType == 0x00) break;

            // Unused entry (deleted or available)
            if(entryType < 0x80) continue;

            // Skip non-file entries (Allocation Bitmap, Up-case Table, Volume Label, Volume GUID, TexFAT Padding)
            if(entryType is (byte)EntryType.AllocationBitmap
                         or (byte)EntryType.UpcaseTable
                         or (byte)EntryType.VolumeLabel
                         or (byte)EntryType.VolumeGuid
                         or (byte)EntryType.TexFatPadding)
                continue;

            // File entry (0x85)
            if(entryType != (byte)EntryType.File) continue;

            FileDirectoryEntry fileEntry =
                Marshal.ByteArrayToStructureLittleEndian<FileDirectoryEntry>(directoryData, i, entrySize);

            if(fileEntry.SecondaryCount < 2) continue; // Need at least Stream Extension and one File Name entry

            // Check we have enough data for all secondary entries
            if(i + (fileEntry.SecondaryCount + 1) * entrySize > directoryData.Length) continue;

            // Read Stream Extension entry (must be immediately after File entry)
            int  streamOffset    = i + entrySize;
            byte streamEntryType = directoryData[streamOffset];

            if(streamEntryType != (byte)EntryType.StreamExtension) continue;

            StreamExtensionDirectoryEntry streamEntry =
                Marshal.ByteArrayToStructureLittleEndian<StreamExtensionDirectoryEntry>(directoryData,
                    streamOffset,
                    entrySize);

            // Read File Name entries
            var fileNameBuilder       = new StringBuilder();
            int fileNameEntriesNeeded = (streamEntry.NameLength + 14) / 15; // 15 chars per entry
            var fileNameEntriesRead   = 0;

            for(var j = 2; j <= fileEntry.SecondaryCount && fileNameEntriesRead < fileNameEntriesNeeded; j++)
            {
                int  nameOffset    = i + j * entrySize;
                byte nameEntryType = directoryData[nameOffset];

                if(nameEntryType != (byte)EntryType.FileName) continue;

                FileNameDirectoryEntry nameEntry =
                    Marshal.ByteArrayToStructureLittleEndian<FileNameDirectoryEntry>(directoryData,
                        nameOffset,
                        entrySize);

                // Each File Name entry contains up to 15 Unicode characters (30 bytes)
                int charsToRead = Math.Min(15, streamEntry.NameLength - fileNameEntriesRead * 15);

                if(charsToRead > 0)
                    fileNameBuilder.Append(Encoding.Unicode.GetString(nameEntry.FileName, 0, charsToRead * 2));

                fileNameEntriesRead++;
            }

            var fileName = fileNameBuilder.ToString();

            if(string.IsNullOrEmpty(fileName)) continue;

            // Skip . and .. entries
            if(fileName is "." or "..") continue;

            // Create complete entry
            var completeEntry = new CompleteDirectoryEntry
            {
                FileEntry       = fileEntry,
                StreamEntry     = streamEntry,
                FileName        = fileName,
                FirstCluster    = streamEntry.FirstCluster,
                DataLength      = streamEntry.DataLength,
                ValidDataLength = streamEntry.ValidDataLength,
                IsContiguous    = (streamEntry.GeneralSecondaryFlags & (byte)GeneralSecondaryFlags.NoFatChain) != 0,
                IsDirectory     = fileEntry.FileAttributes.HasFlag(FileAttributes.Directory)
            };

            cache[fileName] = completeEntry;

            // Skip the secondary entries we've processed
            i += fileEntry.SecondaryCount * entrySize;
        }
    }

    /// <summary>Parses a directory and populates the cache, also extracting volume label.</summary>
    /// <param name="directoryData">Raw directory data.</param>
    /// <param name="cache">Cache to populate with parsed entries.</param>
    void ParseDirectory(byte[] directoryData, Dictionary<string, CompleteDirectoryEntry> cache)
    {
        const int entrySize = 32;

        for(var i = 0; i < directoryData.Length; i += entrySize)
        {
            byte entryType = directoryData[i];

            // End of directory
            if(entryType == 0x00) break;

            // Unused entry (deleted or available)
            if(entryType < 0x80) continue;

            // Volume Label (0x83) - only in root directory
            if(entryType == (byte)EntryType.VolumeLabel)
            {
                VolumeLabelDirectoryEntry volumeLabelEntry =
                    Marshal.ByteArrayToStructureLittleEndian<VolumeLabelDirectoryEntry>(directoryData, i, entrySize);

                if(volumeLabelEntry.CharacterCount > 0 && volumeLabelEntry.CharacterCount <= 11)
                {
                    Metadata.VolumeName =
                        Encoding.Unicode.GetString(volumeLabelEntry.VolumeLabel,
                                                   0,
                                                   volumeLabelEntry.CharacterCount * 2);
                }

                continue;
            }

            // Skip other non-file entries
            if(entryType is (byte)EntryType.AllocationBitmap
                         or (byte)EntryType.UpcaseTable
                         or (byte)EntryType.VolumeGuid
                         or (byte)EntryType.TexFatPadding)
                continue;

            // File entry (0x85)
            if(entryType != (byte)EntryType.File) continue;

            FileDirectoryEntry fileEntry =
                Marshal.ByteArrayToStructureLittleEndian<FileDirectoryEntry>(directoryData, i, entrySize);

            if(fileEntry.SecondaryCount < 2) continue;

            if(i + (fileEntry.SecondaryCount + 1) * entrySize > directoryData.Length) continue;

            int  streamOffset    = i + entrySize;
            byte streamEntryType = directoryData[streamOffset];

            if(streamEntryType != (byte)EntryType.StreamExtension) continue;

            StreamExtensionDirectoryEntry streamEntry =
                Marshal.ByteArrayToStructureLittleEndian<StreamExtensionDirectoryEntry>(directoryData,
                    streamOffset,
                    entrySize);

            var fileNameBuilder       = new StringBuilder();
            int fileNameEntriesNeeded = (streamEntry.NameLength + 14) / 15;
            var fileNameEntriesRead   = 0;

            for(var j = 2; j <= fileEntry.SecondaryCount && fileNameEntriesRead < fileNameEntriesNeeded; j++)
            {
                int  nameOffset    = i + j * entrySize;
                byte nameEntryType = directoryData[nameOffset];

                if(nameEntryType != (byte)EntryType.FileName) continue;

                FileNameDirectoryEntry nameEntry =
                    Marshal.ByteArrayToStructureLittleEndian<FileNameDirectoryEntry>(directoryData,
                        nameOffset,
                        entrySize);

                int charsToRead = Math.Min(15, streamEntry.NameLength - fileNameEntriesRead * 15);

                if(charsToRead > 0)
                    fileNameBuilder.Append(Encoding.Unicode.GetString(nameEntry.FileName, 0, charsToRead * 2));

                fileNameEntriesRead++;
            }

            var fileName = fileNameBuilder.ToString();

            if(string.IsNullOrEmpty(fileName)) continue;

            if(fileName is "." or "..") continue;

            var completeEntry = new CompleteDirectoryEntry
            {
                FileEntry       = fileEntry,
                StreamEntry     = streamEntry,
                FileName        = fileName,
                FirstCluster    = streamEntry.FirstCluster,
                DataLength      = streamEntry.DataLength,
                ValidDataLength = streamEntry.ValidDataLength,
                IsContiguous    = (streamEntry.GeneralSecondaryFlags & (byte)GeneralSecondaryFlags.NoFatChain) != 0,
                IsDirectory     = fileEntry.FileAttributes.HasFlag(FileAttributes.Directory)
            };

            cache[fileName] = completeEntry;

            i += fileEntry.SecondaryCount * entrySize;
        }
    }
}