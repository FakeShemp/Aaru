// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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
public sealed partial class AcornADFS
{

    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting ADFS volume");

        _imagePlugin         = imagePlugin;
        _partition           = partition;
        _encoding            = encoding ?? Encoding.GetEncoding("iso-8859-1");
        _rootDirectoryCache  = new Dictionary<string, DirectoryEntryInfo>();

        if(imagePlugin.Info.SectorSize < 256)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size too small: {0}", imagePlugin.Info.SectorSize);

            return ErrorNumber.InvalidArgument;
        }

        // Try to detect old map format first (ADFS-S, ADFS-M, ADFS-L, ADFS-D)
        ErrorNumber errno = TryOldMapFormat();

        if(errno == ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Detected old map format");
            _isOldMap = true;
        }
        else
        {
            // Try new map format (ADFS-E, ADFS-F, ADFS-F+, ADFS-G)
            errno = TryNewMapFormat();

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Failed to detect ADFS format: {0}", errno);

                return errno;
            }

            AaruLogging.Debug(MODULE_NAME, "Detected new map format");
            _isOldMap = false;
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
        Metadata = new FileSystem
        {
            Type        = FS_TYPE,
            ClusterSize = (uint)_blockSize,
            Clusters    = _discSize / (ulong)_blockSize
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
        _partition   = default;
        _encoding    = null;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Tries to detect and validate old map format (ADFS-S, ADFS-M, ADFS-L, ADFS-D)</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber TryOldMapFormat()
    {
        // Old map format only exists without partitions
        if(_partition.Start != 0)
            return ErrorNumber.InvalidArgument;

        ErrorNumber errno = _imagePlugin.ReadSector(0, false, out byte[] sector0, out _);

        if(errno != ErrorNumber.NoError)
            return errno;

        byte          oldChk0 = AcornMapChecksum(sector0, 255);
        OldMapSector0 oldMap0 = Marshal.ByteArrayToStructureLittleEndian<OldMapSector0>(sector0);

        errno = _imagePlugin.ReadSector(1, false, out byte[] sector1, out _);

        if(errno != ErrorNumber.NoError)
            return errno;

        byte          oldChk1 = AcornMapChecksum(sector1, 255);
        OldMapSector1 oldMap1 = Marshal.ByteArrayToStructureLittleEndian<OldMapSector1>(sector1);

        // According to documentation map1 MUST start on sector 1.
        // On ADFS-D it starts at 0x100, not on sector 1 (0x400)
        if(oldMap0.checksum == oldChk0 && oldMap1.checksum != oldChk1 && sector0.Length >= 512)
        {
            var tmp = new byte[256];
            Array.Copy(sector0, 256, tmp, 0, 256);
            oldChk1 = AcornMapChecksum(tmp, 255);
            oldMap1 = Marshal.ByteArrayToStructureLittleEndian<OldMapSector1>(tmp);
        }

        if(oldMap0.checksum != oldChk0 || oldMap1.checksum != oldChk1 ||
           oldMap0.checksum == 0       || oldMap1.checksum == 0)
            return ErrorNumber.InvalidArgument;

        _oldMap0 = oldMap0;
        _oldMap1 = oldMap1;

        // Calculate disc size from old map
        _discSize = (ulong)((oldMap0.size[2] << 16) + (oldMap0.size[1] << 8) + oldMap0.size[0]) * 256;

        // Old formats use 256 byte sectors as block size
        _blockSize          = 256;
        _log2BytesPerMapBit = 8; // 256 bytes
        _maxNameLen         = F_NAME_LEN;
        _isBigDirectory     = false;

        // Set this BEFORE calling ValidateRootDirectory so ReadDirectoryData uses correct calculation
        _isOldMap = true;

        // Root directory is at fixed location for old formats
        // For old formats, store the byte offset directly (will be handled specially in ReadDirectoryData)
        // Try OLD_DIRECTORY_LOCATION first, then NEW_DIRECTORY_LOCATION
        _rootDirectoryAddress = (uint)OLD_DIRECTORY_LOCATION;
        _rootDirectorySize    = OLD_DIRECTORY_SIZE;

        // Validate root directory exists
        errno = ValidateRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            // Try new directory location (RISC OS 3.10 puts it there sometimes)
            _rootDirectoryAddress = (uint)NEW_DIRECTORY_LOCATION;
            _rootDirectorySize    = NEW_DIRECTORY_SIZE;
            errno                 = ValidateRootDirectory();
        }

        // If validation failed, reset _isOldMap so TryNewMapFormat works correctly
        if(errno != ErrorNumber.NoError)
            _isOldMap = false;

        return errno;
    }

    /// <summary>Tries to detect and validate new map format (ADFS-E, ADFS-F, ADFS-F+, ADFS-G)</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber TryNewMapFormat()
    {
        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
            return errno;

        byte newChk = NewMapChecksum(sector);

        // Try boot block first
        ulong sbSector      = BOOT_BLOCK_LOCATION / _imagePlugin.Info.SectorSize;
        uint  sectorsToRead = BOOT_BLOCK_SIZE     / _imagePlugin.Info.SectorSize;

        if(BOOT_BLOCK_SIZE % _imagePlugin.Info.SectorSize > 0) sectorsToRead++;

        if(sbSector + _partition.Start + sectorsToRead >= _partition.End)
            return ErrorNumber.InvalidArgument;

        errno = _imagePlugin.ReadSectors(sbSector + _partition.Start, false, sectorsToRead, out byte[] bootSector,
                                         out _);

        if(errno != ErrorNumber.NoError)
            return errno;

        var bootChk = 0;

        if(bootSector.Length < 512)
            return ErrorNumber.InvalidArgument;

        for(var i = 0; i < 0x1FF; i++)
            bootChk = (bootChk & 0xFF) + (bootChk >> 8) + bootSector[i];

        DiscRecord drSb;

        // Check if new map checksum is valid
        if(newChk == sector[0] && newChk != 0)
        {
            NewMap nmap = Marshal.ByteArrayToStructureLittleEndian<NewMap>(sector);
            drSb = nmap.discRecord;
        }
        // Check if boot block checksum is valid
        else if((bootChk & 0xFF) == bootSector[0x1FF])
        {
            BootBlock bBlock = Marshal.ByteArrayToStructureLittleEndian<BootBlock>(bootSector);
            drSb = bBlock.discRecord;
        }
        else
            return ErrorNumber.InvalidArgument;

        // Validate disc record
        if(drSb.log2secsize is < 8 or > 10)
            return ErrorNumber.InvalidArgument;

        if(drSb.idlen < drSb.log2secsize + 3 || drSb.idlen > 19)
            return ErrorNumber.InvalidArgument;

        if(drSb.disc_size_high >> drSb.log2secsize != 0)
            return ErrorNumber.InvalidArgument;

        if(!ArrayHelpers.ArrayIsNullOrEmpty(drSb.reserved))
            return ErrorNumber.InvalidArgument;

        _discRecord = drSb;

        // Calculate disc size
        _discSize =  drSb.disc_size_high;
        _discSize *= 0x100000000;
        _discSize += drSb.disc_size;

        if(_discSize > _imagePlugin.Info.Sectors * _imagePlugin.Info.SectorSize)
            return ErrorNumber.InvalidArgument;

        // Set block size and other parameters
        _blockSize          = 1 << drSb.log2secsize;
        _log2BytesPerMapBit = drSb.log2bpmb;

        // Determine directory format and name length
        if(drSb.format_version > 0)
        {
            // ADFS-G or later: big directories
            _isBigDirectory     = true;
            _maxNameLen         = FPLUS_NAME_LEN;
            _rootDirectorySize  = drSb.root_size;
        }
        else if((drSb.flags & 0x01) != 0)
        {
            // ADFS-F+: big directories for discs > 512MB
            _isBigDirectory     = true;
            _maxNameLen         = FPLUS_NAME_LEN;
            _rootDirectorySize  = drSb.root_size;
        }
        else
        {
            // ADFS-E or ADFS-F: standard directories
            _isBigDirectory     = false;
            _maxNameLen         = F_NAME_LEN;
            _rootDirectorySize  = NEW_DIRECTORY_SIZE;
        }

        // Root directory address comes from disc record
        _rootDirectoryAddress = drSb.root;

        // Validate root directory exists
        return ValidateRootDirectory();
    }

    /// <summary>Validates that the root directory can be read and has valid magic</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ValidateRootDirectory()
    {
        ErrorNumber errno = ReadDirectoryData(_rootDirectoryAddress, _rootDirectorySize, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
            return errno;

        if(_isBigDirectory)
        {
            // Check big directory magic
            if(dirData.Length < 8)
                return ErrorNumber.InvalidArgument;

            uint startMagic = BitConverter.ToUInt32(dirData, 4);

            if(startMagic != BIG_DIR_START_NAME)
                return ErrorNumber.InvalidArgument;
        }
        else
        {
            // Check standard directory magic (Hugo or Nick)
            DirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<DirectoryHeader>(dirData);


            if(header.magic != OLD_DIR_MAGIC && header.magic != NEW_DIR_MAGIC)
                return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and caches the root directory contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        ErrorNumber errno = ReadDirectoryData(_rootDirectoryAddress, _rootDirectorySize, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
            return errno;

        if(_isBigDirectory)
            return ParseBigDirectory(dirData);

        return ParseStandardDirectory(dirData);
    }
}
