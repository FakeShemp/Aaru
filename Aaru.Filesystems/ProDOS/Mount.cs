// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using Marshal = Aaru.Helpers.Marshal;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Apple ProDOS 8 Technical Reference
/// <inheritdoc />
public sealed partial class ProDOSPlugin
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? new Apple2c();

        // ProDOS uses 512-byte blocks, but may be on 256-byte sector media
        _multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        AaruLogging.Debug(MODULE_NAME, "Mounting ProDOS volume");
        AaruLogging.Debug(MODULE_NAME, "Sector size: {0}, multiplier: {1}", imagePlugin.Info.SectorSize, _multiplier);

        // Read and validate the volume directory header
        ErrorNumber errno = ReadVolumeDirectoryHeader();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading volume directory header: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Volume name: {0}",  _volumeName);
        AaruLogging.Debug(MODULE_NAME, "Total blocks: {0}", _totalBlocks);
        AaruLogging.Debug(MODULE_NAME, "Bitmap block: {0}", _bitmapBlock);

        // Cache the root directory contents
        errno = CacheRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error caching root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} entries in root directory", _rootDirectoryCache.Count);

        // Populate metadata
        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            VolumeName   = _volumeName,
            Clusters     = _totalBlocks,
            ClusterSize  = 512, // ProDOS always uses 512-byte blocks
            Files        = _volumeHeader.entry_count,
            CreationDate = _creationTime
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        _rootDirectoryCache?.Clear();
        _rootDirectoryCache = null;
        _volumeHeader       = default(VolumeDirectoryHeader);
        _volumeName         = null;
        _imagePlugin        = null;
        _encoding           = null;
        _mounted            = false;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the volume directory header (superblock equivalent)</summary>
    ErrorNumber ReadVolumeDirectoryHeader()
    {
        // Volume directory starts at block 2 (blocks 0 and 1 are boot code)
        ErrorNumber errno = ReadBlock(2, out byte[] block);

        if(errno != ErrorNumber.NoError) return errno;

        // Read block header (prev/next pointers) - first 4 bytes
        _rootDirBlockHeader = Marshal.ByteArrayToStructureLittleEndian<DirectoryBlockHeader>(block);

        // Read volume directory header - starts after block header at offset 4
        _volumeHeader =
            Marshal.ByteArrayToStructureLittleEndian<VolumeDirectoryHeader>(block,
                                                                            4,
                                                                            System.Runtime.InteropServices.Marshal
                                                                               .SizeOf<VolumeDirectoryHeader>());

        // Validate storage type (should be 0xF0 for volume directory header)
        var storageType = (byte)((_volumeHeader.storage_type_name_length & STORAGE_TYPE_MASK) >> 4);

        if(storageType != ROOT_DIRECTORY_TYPE)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid storage type: 0x{0:X2}, expected 0x0F", storageType);

            return ErrorNumber.InvalidArgument;
        }

        // Validate entry length and entries per block
        if(_volumeHeader.entry_length != ENTRY_LENGTH || _volumeHeader.entries_per_block != ENTRIES_PER_BLOCK)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid directory format: entry_length={0}, entries_per_block={1}",
                              _volumeHeader.entry_length,
                              _volumeHeader.entries_per_block);

            return ErrorNumber.InvalidArgument;
        }

        // Extract volume name
        var nameLength = (byte)(_volumeHeader.storage_type_name_length & NAME_LENGTH_MASK);
        _volumeName = _encoding.GetString(_volumeHeader.volume_name, 0, nameLength);

        // Apply case bits if present (GS/OS extension)
        if((_volumeHeader.case_bits & 0x8000) != 0) _volumeName = ApplyCaseBits(_volumeName, _volumeHeader.case_bits);

        // Parse creation time
        _creationTime = DateHandlers.ProDosToDateTime(_volumeHeader.creation_date, _volumeHeader.creation_time);

        _bitmapBlock = _volumeHeader.bitmap_block;
        _totalBlocks = _volumeHeader.total_blocks;

        return ErrorNumber.NoError;
    }

    /// <summary>Caches the root directory contents</summary>
    ErrorNumber CacheRootDirectory()
    {
        _rootDirectoryCache = new Dictionary<string, CachedEntry>(StringComparer.OrdinalIgnoreCase);

        // Volume directory spans blocks 2-5 (4 blocks)
        ushort currentBlock = 2;
        var    isFirstBlock = true;
        var    entriesRead  = 0;

        while(currentBlock != 0 && entriesRead < _volumeHeader.entry_count)
        {
            ErrorNumber errno = ReadBlock(currentBlock, out byte[] block);

            if(errno != ErrorNumber.NoError) return errno;

            // Get next block pointer from header
            var nextBlock = BitConverter.ToUInt16(block, 0x02);

            // Determine entry start offset (skip header in first block)
            var entryOffset = 0x04; // After prev/next pointers

            if(isFirstBlock)
            {
                // First block has volume header, skip it
                entryOffset  += ENTRY_LENGTH; // Skip volume header (39 bytes)
                isFirstBlock =  false;
            }

            // Parse entries in this block
            while(entryOffset + ENTRY_LENGTH <= 512)
            {
                // Check if entry is active
                byte storageTypeNameLength = block[entryOffset];

                if(storageTypeNameLength == 0)
                {
                    entryOffset += ENTRY_LENGTH;

                    continue;
                }

                var storageType = (byte)((storageTypeNameLength & STORAGE_TYPE_MASK) >> 4);

                // Skip deleted entries
                if(storageType == EMPTY_STORAGE_TYPE)
                {
                    entryOffset += ENTRY_LENGTH;

                    continue;
                }

                // Parse the entry
                CachedEntry entry = ParseDirectoryEntry(block, entryOffset);

                if(entry != null && !string.IsNullOrEmpty(entry.Name))
                {
                    _rootDirectoryCache[entry.Name] = entry;
                    entriesRead++;
                }

                entryOffset += ENTRY_LENGTH;
            }

            currentBlock = nextBlock;
        }

        return ErrorNumber.NoError;
    }
}