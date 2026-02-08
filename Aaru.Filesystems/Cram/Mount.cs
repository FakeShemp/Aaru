// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class Cram
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting CramFS volume");

        _imagePlugin        = imagePlugin;
        _partition          = partition;
        _encoding           = encoding ?? Encoding.GetEncoding("iso-8859-15");
        _rootDirectoryCache = new Dictionary<string, DirectoryEntryInfo>(StringComparer.Ordinal);

        // Read and validate the superblock
        ErrorNumber errno = ReadSuperBlock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");

        // Check for unsupported flags
        var flags = (CramFlags)_superBlock.flags;

        if(((uint)flags & ~(uint)CramSupportedFlags.All) != 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Unsupported flags: 0x{0:X8}", _superBlock.flags);

            return ErrorNumber.NotSupported;
        }

        // Validate root inode is a directory
        if(!IsDirectory(GetInodeMode(_superBlock.root)))
        {
            AaruLogging.Debug(MODULE_NAME, "Root is not a directory, mode: 0x{0:X4}", GetInodeMode(_superBlock.root));

            return ErrorNumber.InvalidArgument;
        }

        // Load root directory
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        string volumeName = StringHandlers.CToString(_superBlock.name, _encoding);

        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            VolumeName   = volumeName,
            ClusterSize  = 4096, // CramFS uses 4K pages
            Clusters     = _superBlock.fsid.blocks,
            Files        = _superBlock.fsid.files,
            FreeClusters = 0 // CramFS is read-only, no free space
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Volume mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _rootDirectoryCache?.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _superBlock  = default(SuperBlock);
        _encoding    = null;
        _baseOffset  = 0;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the CramFS superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperBlock()
    {
        AaruLogging.Debug(MODULE_NAME, "Reading superblock...");

        // Read the first sector
        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading first sector: {0}", errno);

            return errno;
        }

        // Check magic at offset 0
        var magic = BitConverter.ToUInt32(sector, 0);

        _baseOffset = 0;

        if(magic == CRAM_MAGIC)
        {
            _superBlock   = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);
            _littleEndian = true;
        }
        else if(magic == CRAM_CIGAM)
        {
            _superBlock   = Marshal.ByteArrayToStructureBigEndian<SuperBlock>(sector);
            _littleEndian = false;
        }
        else
        {
            // Check at 512 byte offset (some cramfs images have the superblock shifted)
            if(sector.Length >= 512 + 4)
            {
                magic = BitConverter.ToUInt32(sector, 512);

                if(magic == CRAM_MAGIC)
                {
                    var sbData = new byte[sector.Length - 512];
                    Array.Copy(sector, 512, sbData, 0, sbData.Length);
                    _superBlock   = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sbData);
                    _littleEndian = true;
                    _baseOffset   = 512;
                }
                else if(magic == CRAM_CIGAM)
                {
                    var sbData = new byte[sector.Length - 512];
                    Array.Copy(sector, 512, sbData, 0, sbData.Length);
                    _superBlock   = Marshal.ByteArrayToStructureBigEndian<SuperBlock>(sbData);
                    _littleEndian = false;
                    _baseOffset   = 512;
                }
                else
                {
                    AaruLogging.Debug(MODULE_NAME, "Invalid magic: 0x{0:X8}", magic);

                    return ErrorNumber.InvalidArgument;
                }
            }
            else
            {
                AaruLogging.Debug(MODULE_NAME, "Invalid magic: 0x{0:X8}", magic);

                return ErrorNumber.InvalidArgument;
            }
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Magic: 0x{0:X8}, little-endian: {1}, base offset: {2}",
                          _superBlock.magic,
                          _littleEndian,
                          _baseOffset);

        // Validate signature
        string signature = Encoding.ASCII.GetString(_superBlock.signature);

        if(!signature.StartsWith(CRAMFS_SIGNATURE, StringComparison.Ordinal))
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid signature: {0}", signature);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Filesystem size: {0} bytes", _superBlock.size);
        AaruLogging.Debug(MODULE_NAME, "Flags: 0x{0:X8}",            _superBlock.flags);
        AaruLogging.Debug(MODULE_NAME, "Files: {0}, Blocks: {1}",    _superBlock.fsid.files, _superBlock.fsid.blocks);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and caches the root directory contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        // Root directory offset is in the root inode
        uint rootOffset = GetInodeOffset(_superBlock.root) << 2;
        uint rootSize   = GetInodeSize(_superBlock.root);

        AaruLogging.Debug(MODULE_NAME, "Root directory offset: {0}, size: {1}", rootOffset, rootSize);

        if(rootOffset == 0)
        {
            // Empty filesystem
            AaruLogging.Debug(MODULE_NAME, "Empty filesystem (root offset is 0)");

            return ErrorNumber.NoError;
        }

        return ReadDirectoryContents(rootOffset, rootSize, _rootDirectoryCache);
    }
}