// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Xia filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class Xia
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
        AaruLogging.Debug(MODULE_NAME, "Zone size: {0} bytes", _superblock.s_zone_size);
        AaruLogging.Debug(MODULE_NAME, "Total zones: {0}",     _superblock.s_nzones);
        AaruLogging.Debug(MODULE_NAME, "Total inodes: {0}",    _superblock.s_ninodes);
        AaruLogging.Debug(MODULE_NAME, "Data zones: {0}",      _superblock.s_ndatazones);
        AaruLogging.Debug(MODULE_NAME, "First data zone: {0}", _superblock.s_firstdatazone);
        AaruLogging.Debug(MODULE_NAME, "Inode map zones: {0}", _superblock.s_imap_zones);
        AaruLogging.Debug(MODULE_NAME, "Zone map zones: {0}",  _superblock.s_zmap_zones);

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
            Bootable    = !ArrayHelpers.ArrayIsNullOrEmpty(_superblock.s_boot_segment),
            Clusters    = _superblock.s_nzones,
            ClusterSize = _superblock.s_zone_size,
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
        _superblock  = default(SuperBlock);
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

        int sbSizeInBytes   = Marshal.SizeOf<SuperBlock>();
        var sbSizeInSectors = (uint)(sbSizeInBytes / _imagePlugin.Info.SectorSize);

        if(sbSizeInBytes % _imagePlugin.Info.SectorSize > 0) sbSizeInSectors++;

        if(sbSizeInSectors + _partition.Start >= _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME, "Partition too small for superblock");

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno =
            _imagePlugin.ReadSectors(_partition.Start, false, sbSizeInSectors, out byte[] sbSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sectors: {0}", errno);

            return errno;
        }

        _superblock = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sbSector);

        AaruLogging.Debug(MODULE_NAME, "Validating superblock...");

        // Validate magic number
        if(_superblock.s_magic != XIAFS_SUPER_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid magic: 0x{0:X8}, expected 0x{1:X8}",
                              _superblock.s_magic,
                              XIAFS_SUPER_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        // Validate zone size (must be 1024, 2048, or 4096)
        if(_superblock.s_zone_size != 1024 && _superblock.s_zone_size != 2048 && _superblock.s_zone_size != 4096)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid zone size: {0}", _superblock.s_zone_size);

            return ErrorNumber.InvalidArgument;
        }

        // Validate zone_shift (zone_size = 1KB << zone_shift)
        if(1024u << (int)_superblock.s_zone_shift != _superblock.s_zone_size)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Zone shift mismatch: 1024 << {0} = {1}, but zone_size = {2}",
                              _superblock.s_zone_shift,
                              1024u << (int)_superblock.s_zone_shift,
                              _superblock.s_zone_size);

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
        ErrorNumber errno = ReadInode(XIAFS_ROOT_INO, out Inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.i_size);

        // Read the directory content from root inode's data zones
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