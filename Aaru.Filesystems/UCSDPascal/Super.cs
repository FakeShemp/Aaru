// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : U.C.S.D. Pascal filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles mounting and umounting the U.C.S.D. Pascal filesystem.
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

using System.Collections.Generic;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;
using Marshal = Aaru.Helpers.Marshal;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Call-A.P.P.L.E. Pascal Disk Directory Structure
public sealed partial class PascalPlugin
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _device   = imagePlugin;
        _encoding = encoding ?? new Apple2();

        options ??= GetDefaultOptions();

        if(options.TryGetValue("debug", out string debugString)) bool.TryParse(debugString, out _debug);

        if(options.TryGetValue("decode_text", out string decodeTextString))
            bool.TryParse(decodeTextString, out _decodeText);

        if(_device.Info.Sectors < 3) return ErrorNumber.InvalidArgument;

        _multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        // Blocks 0 and 1 are boot code
        ErrorNumber errno = _device.ReadSectors(_multiplier * 2, false, _multiplier, out _catalogBlocks, out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Try little endian first (Apple II), then big endian (other platforms)
        _bigEndian = false;

        PascalVolumeEntry volEntry = Marshal.ByteArrayToStructureLittleEndian<PascalVolumeEntry>(_catalogBlocks);

        if(!IsValidVolumeEntry(volEntry))
        {
            _bigEndian = true;
            volEntry   = Marshal.ByteArrayToStructureBigEndian<PascalVolumeEntry>(_catalogBlocks);

            if(!IsValidVolumeEntry(volEntry)) return ErrorNumber.InvalidArgument;
        }

        _mountedVolEntry = volEntry;

        if(_mountedVolEntry.FirstBlock       != 0                                                                     ||
           _mountedVolEntry.LastBlock        <= _mountedVolEntry.FirstBlock                                           ||
           (ulong)_mountedVolEntry.LastBlock > _device.Info.Sectors / _multiplier - 2                                 ||
           _mountedVolEntry.EntryType != PascalFileKind.Volume && _mountedVolEntry.EntryType != PascalFileKind.Secure ||
           _mountedVolEntry.VolumeName[0] > 7                                                                         ||
           _mountedVolEntry.Blocks        < 0                                                                         ||
           (ulong)_mountedVolEntry.Blocks != _device.Info.Sectors / _multiplier                                       ||
           _mountedVolEntry.Files         < 0)
            return ErrorNumber.InvalidArgument;

        errno = _device.ReadSectors(_multiplier * 2,
                                    false,
                                    (uint)(_mountedVolEntry.LastBlock - _mountedVolEntry.FirstBlock - 2) * _multiplier,
                                    out _catalogBlocks,
                                    out _);

        if(errno != ErrorNumber.NoError) return errno;

        const int entrySize = 26;
        int       offset    = entrySize; // Skip first entry (volume entry)

        _fileEntries = [];

        while(offset + entrySize <= _catalogBlocks.Length)
        {
            PascalFileEntry entry = _bigEndian
                                        ? Marshal.ByteArrayToStructureBigEndian<PascalFileEntry>(_catalogBlocks,
                                            offset,
                                            entrySize)
                                        : Marshal.ByteArrayToStructureLittleEndian<PascalFileEntry>(_catalogBlocks,
                                            offset,
                                            entrySize);

            if(entry.Filename?[0] <= 15 && entry.Filename?[0] > 0) _fileEntries.Add(entry);

            offset += entrySize;
        }

        // Validate filesystem consistency
        ValidateFilesystem();

        errno = _device.ReadSectors(0, false, 2 * _multiplier, out _bootBlocks, out _);

        if(errno != ErrorNumber.NoError) return errno;

        Metadata = new FileSystem
        {
            Bootable    = !ArrayHelpers.ArrayIsNullOrEmpty(_bootBlocks),
            Clusters    = (ulong)_mountedVolEntry.Blocks,
            ClusterSize = _device.Info.SectorSize,
            Files       = (ulong)_mountedVolEntry.Files,
            Type        = FS_TYPE,
            VolumeName  = StringHandlers.PascalToString(_mountedVolEntry.VolumeName, _encoding)
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <summary>Checks if a volume entry is valid</summary>
    /// <param name="volEntry">Volume entry to validate</param>
    /// <returns>True if the volume entry appears valid</returns>
    bool IsValidVolumeEntry(PascalVolumeEntry volEntry)
    {
        // First block is always 0
        if(volEntry.FirstBlock != 0) return false;

        // Last volume record block must be after first block, and before end of device
        if(volEntry.LastBlock        <= volEntry.FirstBlock ||
           (ulong)volEntry.LastBlock > _device.Info.Sectors / _multiplier - 2)
            return false;

        // Volume record entry type must be volume or secure
        if(volEntry.EntryType != PascalFileKind.Volume && volEntry.EntryType != PascalFileKind.Secure) return false;

        // Volume name is max 7 characters
        if(volEntry.VolumeName?[0] > 7) return false;

        // Volume blocks is equal to volume sectors
        if(volEntry.Blocks < 0 || (ulong)volEntry.Blocks != _device.Info.Sectors / _multiplier) return false;

        // There can be not less than zero files
        return volEntry.Files >= 0;
    }

    /// <summary>Validates filesystem consistency and logs any issues found</summary>
    void ValidateFilesystem()
    {
        var errorCount = 0;

        // Check 1: File count matches volume header
        if(_fileEntries.Count != _mountedVolEntry.Files)
        {
            AaruLogging.Error("File count mismatch: volume header says {0} files, but found {1} directory entries",
                              _mountedVolEntry.Files,
                              _fileEntries.Count);

            errorCount++;
        }

        if(_fileEntries.Count == 0)
        {
            if(errorCount > 0) AaruLogging.Error("Filesystem validation completed with {0} issue(s)", errorCount);

            return;
        }

        // Check 2: Files are in block order
        for(var i = 1; i < _fileEntries.Count; i++)
        {
            if(_fileEntries[i - 1].LastBlock > _fileEntries[i].FirstBlock)
            {
                AaruLogging.Error("Directory entries not in block order: '{0}' (ends at block {1}) comes before '{2}' (starts at block {3})",
                                  StringHandlers.PascalToString(_fileEntries[i - 1].Filename, _encoding),
                                  _fileEntries[i - 1].LastBlock,
                                  StringHandlers.PascalToString(_fileEntries[i].Filename, _encoding),
                                  _fileEntries[i].FirstBlock);

                errorCount++;

                break; // Only report once
            }
        }

        // Check 3: No block overlaps - first file must start after directory
        int expectedFirstBlock = _mountedVolEntry.LastBlock;

        foreach(PascalFileEntry entry in _fileEntries)
        {
            string fileName = StringHandlers.PascalToString(entry.Filename, _encoding);

            // File starts before expected position (overlap with previous file or directory)
            if(entry.FirstBlock < expectedFirstBlock)
            {
                AaruLogging.Error("File '{0}' starts at block {1}, but expected block {2} or later (block overlap detected)",
                                  fileName,
                                  entry.FirstBlock,
                                  expectedFirstBlock);

                errorCount++;
            }

            // File ends before it starts (invalid)
            if(entry.LastBlock <= entry.FirstBlock)
            {
                AaruLogging.Error("File '{0}' has invalid block range: FirstBlock={1}, LastBlock={2}",
                                  fileName,
                                  entry.FirstBlock,
                                  entry.LastBlock);

                errorCount++;

                continue;
            }

            // File extends beyond end of volume
            if(entry.LastBlock > _mountedVolEntry.Blocks)
            {
                AaruLogging.Error("File '{0}' extends beyond end of volume: LastBlock={1}, VolumeBlocks={2}",
                                  fileName,
                                  entry.LastBlock,
                                  _mountedVolEntry.Blocks);

                errorCount++;
            }

            // Validate LastBytes field (should be 1-512)
            if(entry.LastBytes <= 0 || entry.LastBytes > 512)
            {
                AaruLogging.Error("File '{0}' has invalid LastBytes value: {1} (expected 1-512)",
                                  fileName,
                                  entry.LastBytes);

                errorCount++;
            }

            // Validate file type
            if(entry.EntryType < PascalFileKind.Bad || entry.EntryType > PascalFileKind.Secure)
            {
                AaruLogging.Error("File '{0}' has invalid file type: {1}", fileName, (int)entry.EntryType);

                errorCount++;
            }

            // Validate filename length
            if(entry.Filename[0] == 0 || entry.Filename[0] > 15)
            {
                AaruLogging.Error("File '{0}' has invalid filename length: {1} (expected 1-15)",
                                  fileName,
                                  entry.Filename[0]);

                errorCount++;
            }

            // Update expected first block for next file
            expectedFirstBlock = entry.LastBlock;
        }

        // Check 4: Last file doesn't extend beyond volume
        if(_fileEntries.Count > 0)
        {
            PascalFileEntry lastEntry = _fileEntries[^1];

            if(lastEntry.LastBlock > _mountedVolEntry.Blocks)
            {
                AaruLogging.Error("Last file '{0}' extends beyond volume: ends at block {1}, volume has {2} blocks",
                                  StringHandlers.PascalToString(lastEntry.Filename, _encoding),
                                  lastEntry.LastBlock,
                                  _mountedVolEntry.Blocks);

                errorCount++;
            }
        }

        if(errorCount > 0)
            AaruLogging.Error("Filesystem validation completed with {0} issue(s)", errorCount);
        else
            AaruLogging.Debug(MODULE_NAME, "Filesystem validation passed");
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        _mounted     = false;
        _fileEntries = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = new FileSystemInfo
        {
            Blocks         = (ulong)_mountedVolEntry.Blocks,
            FilenameLength = 16,
            Files          = (ulong)_mountedVolEntry.Files,
            FreeBlocks     = 0,
            PluginId       = Id,
            Type           = FS_TYPE
        };

        stat.FreeBlocks = (ulong)(_mountedVolEntry.Blocks - (_mountedVolEntry.LastBlock - _mountedVolEntry.FirstBlock));

        foreach(PascalFileEntry entry in _fileEntries) stat.FreeBlocks -= (ulong)(entry.LastBlock - entry.FirstBlock);

        return ErrorNumber.NoError;
    }

#endregion
}