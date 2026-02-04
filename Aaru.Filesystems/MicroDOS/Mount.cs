// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MicroDOS filesystem plugin
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class MicroDOS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;

        // MicroDOS uses KOI8-R encoding, register the encoding provider
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encoding = Encoding.GetEncoding("koi8-r");

        // Read block 0 (superblock)
        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");

        // Load the root directory
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory loaded successfully with {0} entries",
                          _rootDirectoryCache.Count);

        Metadata = new FileSystem
        {
            Clusters     = _block0.blocks,
            ClusterSize  = BLOCK_SIZE,
            Type         = FS_TYPE,
            Files        = _block0.files,
            FreeClusters = (ulong)(_block0.blocks - _block0.usedBlocks)
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Mount complete");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _rootDirectoryCache.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _block0      = default(Block0);
        _encoding    = null;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the filesystem superblock (block 0)</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        // Read block 0
        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] block0Data, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading block 0: {0}", errno);

            return errno;
        }

        if(block0Data.Length < BLOCK_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Block 0 too small: {0} bytes", block0Data.Length);

            return ErrorNumber.InvalidArgument;
        }

        _block0 = Marshal.ByteArrayToStructureLittleEndian<Block0>(block0Data);

        // Validate magic numbers
        if(_block0.label != MAGIC || _block0.mklabel != MAGIC2)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid magic: label=0x{0:X4}, mklabel=0x{1:X4}",
                              _block0.label,
                              _block0.mklabel);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "blocks: {0}",         _block0.blocks);
        AaruLogging.Debug(MODULE_NAME, "files: {0}",          _block0.files);
        AaruLogging.Debug(MODULE_NAME, "usedBlocks: {0}",     _block0.usedBlocks);
        AaruLogging.Debug(MODULE_NAME, "firstUsedBlock: {0}", _block0.firstUsedBlock);

        AaruLogging.Debug(MODULE_NAME, "Superblock validated successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();

        // Read block 0 which contains the directory
        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] block0Data, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading block 0: {0}", errno);

            return errno;
        }

        // Directory entries start at offset 320 (octal 500) in block 0
        // Each entry is 24 bytes (octal 30)
        // Maximum entries in block 0: (512 - 320) / 24 = 8 entries in first block

        int entriesInBlock0 = (BLOCK_SIZE - DIR_START_OFFSET) / DIR_ENTRY_SIZE;
        int totalEntries    = _block0.files;
        var entriesRead     = 0;

        // Read entries from block 0
        for(var i = 0; i < entriesInBlock0 && entriesRead < totalEntries; i++)
        {
            int offset = DIR_START_OFFSET + i * DIR_ENTRY_SIZE;

            if(offset + DIR_ENTRY_SIZE > block0Data.Length) break;

            DirectoryEntry entry =
                Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(block0Data, offset, DIR_ENTRY_SIZE);

            ProcessDirectoryEntry(entry, ref entriesRead);
        }

        // If we need more entries, read subsequent blocks
        // Directory continues in blocks after block 0
        uint currentBlock = 1;

        while(entriesRead < totalEntries)
        {
            if(_partition.Start + currentBlock >= _partition.End)
            {
                AaruLogging.Debug(MODULE_NAME, "Reached end of partition while reading directory");

                break;
            }

            errno = _imagePlugin.ReadSector(_partition.Start + currentBlock, false, out byte[] blockData, out _);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading block {0}: {1}", currentBlock, errno);

                break;
            }

            int entriesPerBlock = BLOCK_SIZE / DIR_ENTRY_SIZE;

            for(var i = 0; i < entriesPerBlock && entriesRead < totalEntries; i++)
            {
                int offset = i * DIR_ENTRY_SIZE;

                if(offset + DIR_ENTRY_SIZE > blockData.Length) break;

                DirectoryEntry entry =
                    Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(blockData, offset, DIR_ENTRY_SIZE);

                ProcessDirectoryEntry(entry, ref entriesRead);
            }

            currentBlock++;
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}