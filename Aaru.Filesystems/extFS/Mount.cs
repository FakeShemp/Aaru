// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class extFS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");
        AaruLogging.Debug(MODULE_NAME, "Inodes: {0}",          _superblock.s_ninodes);
        AaruLogging.Debug(MODULE_NAME, "Zones: {0}",           _superblock.s_nzones);
        AaruLogging.Debug(MODULE_NAME, "First data zone: {0}", _superblock.s_firstdatazone);
        AaruLogging.Debug(MODULE_NAME, "Log zone size: {0}",   _superblock.s_log_zone_size);
        AaruLogging.Debug(MODULE_NAME, "Free blocks: {0}",     _superblock.s_freeblockscount);
        AaruLogging.Debug(MODULE_NAME, "Free inodes: {0}",     _superblock.s_freeinodescount);

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
            Clusters    = _superblock.s_nzones,
            ClusterSize = 1024u << (int)_superblock.s_log_zone_size,
            Type        = FS_TYPE
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
        _superblock  = default(ext_super_block);
        _encoding    = null;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the filesystem superblock</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        // Superblock is at offset 0x400 (1024 bytes), block 1
        ulong sbSectorOff = SB_POS / _imagePlugin.Info.SectorSize;
        uint  sbOff       = SB_POS % _imagePlugin.Info.SectorSize;

        if(sbSectorOff + _partition.Start >= _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME, "Partition too small for superblock");

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno = _imagePlugin.ReadSector(sbSectorOff + _partition.Start, false, out byte[] sbSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

            return errno;
        }

        if(sbOff + 512 > sbSector.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock sector too small");

            return ErrorNumber.InvalidArgument;
        }

        // Parse superblock using Marshal
        var sbData = new byte[Marshal.SizeOf<ext_super_block>()];
        Array.Copy(sbSector, sbOff, sbData, 0, sbData.Length);
        _superblock = Marshal.ByteArrayToStructureLittleEndian<ext_super_block>(sbData);

        AaruLogging.Debug(MODULE_NAME, "Validating superblock...");

        // Validate magic number
        if(_superblock.s_magic != EXT_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid magic: 0x{0:X4}, expected 0x{1:X4}",
                              _superblock.s_magic,
                              EXT_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock validation successful");

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory entries into the cache</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();

        // Read the root inode (inode 1)
        ErrorNumber errno = ReadInode(EXT_ROOT_INO, out ext_inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.i_size);

        // Read the directory content from root inode's data blocks
        errno = ReadDirectoryEntries(rootInode, out Dictionary<string, uint> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory entries: {0}", errno);

            return errno;
        }

        foreach(KeyValuePair<string, uint> entry in entries) _rootDirectoryCache[entry.Key] = entry.Value;

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}