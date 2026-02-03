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
using System.Text;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
public sealed partial class exFAT
{
    /// <summary>Parses a directory and populates the cache.</summary>
    /// <param name="directoryData">Raw directory data.</param>
    /// <param name="cache">Cache to populate with parsed entries.</param>
    void ParseDirectory(byte[] directoryData, Dictionary<string, CompleteDirectoryEntry> cache)
    {
        const int ENTRY_SIZE = 32;

        for(var i = 0; i < directoryData.Length; i += ENTRY_SIZE)
        {
            byte entryType = directoryData[i];

            // End of directory
            if(entryType == 0x00) break;

            // Unused entry (deleted or available)
            if(entryType < 0x80) continue;

            // Volume Label (0x83)
            if(entryType == (byte)EntryType.VolumeLabel)
            {
                VolumeLabelDirectoryEntry volumeLabelEntry =
                    Marshal.ByteArrayToStructureLittleEndian<VolumeLabelDirectoryEntry>(directoryData, i, ENTRY_SIZE);

                if(volumeLabelEntry.CharacterCount > 0 && volumeLabelEntry.CharacterCount <= 11)
                {
                    Metadata.VolumeName =
                        Encoding.Unicode.GetString(volumeLabelEntry.VolumeLabel,
                                                   0,
                                                   volumeLabelEntry.CharacterCount * 2);
                }

                continue;
            }

            // Allocation Bitmap (0x81) - skip
            if(entryType == (byte)EntryType.AllocationBitmap) continue;

            // Up-case Table (0x82) - skip
            if(entryType == (byte)EntryType.UpcaseTable) continue;

            // Volume GUID (0xA0) - skip
            if(entryType == (byte)EntryType.VolumeGuid) continue;

            // TexFAT Padding (0xA1) - skip
            if(entryType == (byte)EntryType.TexFatPadding) continue;

            // File entry (0x85)
            if(entryType == (byte)EntryType.File)
            {
                FileDirectoryEntry fileEntry =
                    Marshal.ByteArrayToStructureLittleEndian<FileDirectoryEntry>(directoryData, i, ENTRY_SIZE);

                if(fileEntry.SecondaryCount < 2) continue; // Need at least Stream Extension and one File Name entry

                // Check we have enough data for all secondary entries
                if(i + (fileEntry.SecondaryCount + 1) * ENTRY_SIZE > directoryData.Length) continue;

                // Read Stream Extension entry (must be immediately after File entry)
                int  streamOffset    = i + ENTRY_SIZE;
                byte streamEntryType = directoryData[streamOffset];

                if(streamEntryType != (byte)EntryType.StreamExtension) continue;

                StreamExtensionDirectoryEntry streamEntry =
                    Marshal.ByteArrayToStructureLittleEndian<StreamExtensionDirectoryEntry>(directoryData,
                        streamOffset,
                        ENTRY_SIZE);

                // Read File Name entries
                var fileNameBuilder       = new StringBuilder();
                int fileNameEntriesNeeded = (streamEntry.NameLength + 14) / 15; // 15 chars per entry
                var fileNameEntriesRead   = 0;

                for(var j = 2; j <= fileEntry.SecondaryCount && fileNameEntriesRead < fileNameEntriesNeeded; j++)
                {
                    int  nameOffset    = i + j * ENTRY_SIZE;
                    byte nameEntryType = directoryData[nameOffset];

                    if(nameEntryType != (byte)EntryType.FileName) continue;

                    FileNameDirectoryEntry nameEntry =
                        Marshal.ByteArrayToStructureLittleEndian<FileNameDirectoryEntry>(directoryData,
                            nameOffset,
                            ENTRY_SIZE);

                    // Each File Name entry contains up to 15 Unicode characters (30 bytes)
                    int charsToRead = Math.Min(15, streamEntry.NameLength - fileNameEntriesRead * 15);

                    if(charsToRead > 0)
                        fileNameBuilder.Append(Encoding.Unicode.GetString(nameEntry.FileName, 0, charsToRead * 2));

                    fileNameEntriesRead++;
                }

                var fileName = fileNameBuilder.ToString();

                if(string.IsNullOrEmpty(fileName)) continue;

                // Skip . and .. entries
                if(fileName == "." || fileName == "..") continue;

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
                i += fileEntry.SecondaryCount * ENTRY_SIZE;
            }
        }
    }
}