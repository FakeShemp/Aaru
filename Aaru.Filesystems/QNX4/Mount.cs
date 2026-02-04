// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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
using System.Linq;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class QNX4
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        // Read the superblock (located at block 1, offset 512 bytes)
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
            Clusters         = partition.Length,
            ClusterSize      = QNX4_BLOCK_SIZE,
            Type             = FS_TYPE,
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(_superblock.RootDir.di_ftime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(_superblock.RootDir.di_mtime),
            Bootable         = _superblock.Boot.di_size > 0 || _superblock.AltBoot.di_size > 0
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
        _superblock  = default(qnx4_super_block);
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

        // Superblock is at block 1 (offset 512 bytes from partition start)
        if(_partition.Start + 1 >= _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME, "Partition too small for superblock");

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start + 1, false, out byte[] sbSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock sector: {0}", errno);

            return errno;
        }

        if(sbSector.Length < QNX4_BLOCK_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Superblock sector too small");

            return ErrorNumber.InvalidArgument;
        }

        // Parse superblock using Marshal
        _superblock = Marshal.ByteArrayToStructureLittleEndian<qnx4_super_block>(sbSector);

        AaruLogging.Debug(MODULE_NAME, "Validating superblock...");

        // Validate root directory name (must be "/")
        if(!_rootDirFname.SequenceEqual(_superblock.RootDir.di_fname))
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid root directory name");

            return ErrorNumber.InvalidArgument;
        }

        // Check root directory is in use
        if((_superblock.RootDir.di_status & QNX4_FILE_USED) != QNX4_FILE_USED)
        {
            AaruLogging.Debug(MODULE_NAME, "Root directory not in use");

            return ErrorNumber.InvalidArgument;
        }

        // Check inode file is in use
        if((_superblock.Inode.di_status & QNX4_FILE_USED) != QNX4_FILE_USED)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode file not in use");

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock validation successful");
        AaruLogging.Debug(MODULE_NAME, "Root dir size: {0} bytes",         _superblock.RootDir.di_size);
        AaruLogging.Debug(MODULE_NAME, "Root dir first extent block: {0}", _superblock.RootDir.di_first_xtnt.xtnt_blk);
        AaruLogging.Debug(MODULE_NAME, "Root dir first extent size: {0}",  _superblock.RootDir.di_first_xtnt.xtnt_size);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory entries into the cache</summary>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();

        // Read directory entries from the root directory inode
        ErrorNumber errno = ReadDirectoryEntries(_superblock.RootDir, out Dictionary<string, qnx4_inode_entry> entries);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading directory entries: {0}", errno);

            return errno;
        }

        foreach(KeyValuePair<string, qnx4_inode_entry> entry in entries) _rootDirectoryCache[entry.Key] = entry.Value;

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}