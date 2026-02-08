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

        _imagePlugin        = imagePlugin;
        _partition          = partition;
        _encoding           = encoding ?? Encoding.GetEncoding("iso-8859-1");
        _imageSectorSize    = imagePlugin.Info.SectorSize;
        _rootDirectoryCache = new Dictionary<string, DirectoryEntryInfo>(StringComparer.OrdinalIgnoreCase);
        _directoryCache     = new Dictionary<uint, Dictionary<string, DirectoryEntryInfo>>();
        _mapCache           = null;

        if(_imageSectorSize < 256)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size too small: {0}", _imageSectorSize);

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
        _directoryCache?.Clear();
        _mapCache        = null;
        _mounted         = false;
        _imagePlugin     = null;
        _partition       = default(Partition);
        _encoding        = null;
        _imageSectorSize = 0;
        Metadata         = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Tries to detect and validate old map format (ADFS-S, ADFS-M, ADFS-L, ADFS-D)</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber TryOldMapFormat()
    {
        AaruLogging.Debug(MODULE_NAME, "TryOldMapFormat: partition.Start={0}", _partition.Start);

        // Old map format only exists without partitions
        if(_partition.Start != 0)
        {
            AaruLogging.Debug(MODULE_NAME, "TryOldMapFormat: Partition not at start, skipping");

            return ErrorNumber.InvalidArgument;
        }

        ErrorNumber errno = _imagePlugin.ReadSector(0, false, out byte[] sector0, out _);

        if(errno != ErrorNumber.NoError) return errno;

        byte          oldChk0 = AcornMapChecksum(sector0, 255);
        OldMapSector0 oldMap0 = Marshal.ByteArrayToStructureLittleEndian<OldMapSector0>(sector0);

        errno = _imagePlugin.ReadSector(1, false, out byte[] sector1, out _);

        if(errno != ErrorNumber.NoError) return errno;

        byte          oldChk1 = AcornMapChecksum(sector1, 255);
        OldMapSector1 oldMap1 = Marshal.ByteArrayToStructureLittleEndian<OldMapSector1>(sector1);

        AaruLogging.Debug(MODULE_NAME,
                          "TryOldMapFormat: oldMap0.checksum={0}, oldChk0={1}, oldMap1.checksum={2}, oldChk1={3}",
                          oldMap0.checksum,
                          oldChk0,
                          oldMap1.checksum,
                          oldChk1);

        // According to documentation map1 MUST start on sector 1.
        // On ADFS-D it starts at 0x100, not on sector 1 (0x400)
        if(oldMap0.checksum == oldChk0 && oldMap1.checksum != oldChk1 && sector0.Length >= 512)
        {
            var tmp = new byte[256];
            Array.Copy(sector0, 256, tmp, 0, 256);
            oldChk1 = AcornMapChecksum(tmp, 255);
            oldMap1 = Marshal.ByteArrayToStructureLittleEndian<OldMapSector1>(tmp);
        }

        if(oldMap0.checksum != oldChk0 || oldMap1.checksum != oldChk1 || oldMap0.checksum == 0 || oldMap1.checksum == 0)
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
        if(errno != ErrorNumber.NoError) _isOldMap = false;

        return errno;
    }

    /// <summary>Tries to detect and validate new map format (ADFS-E, ADFS-F, ADFS-F+, ADFS-G)</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber TryNewMapFormat()
    {
        AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: Starting");

        ErrorNumber errno = _imagePlugin.ReadSector(_partition.Start, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: ReadSector failed with {0}", errno);

            return errno;
        }

        byte newChk = NewMapChecksum(sector);

        AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: newChk={0}, sector[0]={1}", newChk, sector[0]);

        // Try boot block first
        ulong sbSector      = BOOT_BLOCK_LOCATION / _imagePlugin.Info.SectorSize;
        uint  sectorsToRead = BOOT_BLOCK_SIZE     / _imagePlugin.Info.SectorSize;

        if(BOOT_BLOCK_SIZE % _imagePlugin.Info.SectorSize > 0) sectorsToRead++;

        AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: sbSector={0}, sectorsToRead={1}", sbSector, sectorsToRead);

        if(sbSector + _partition.Start + sectorsToRead >= _partition.End)
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: Boot block beyond partition end");

            return ErrorNumber.InvalidArgument;
        }

        errno = _imagePlugin.ReadSectors(sbSector + _partition.Start,
                                         false,
                                         sectorsToRead,
                                         out byte[] bootSector,
                                         out _);

        if(errno != ErrorNumber.NoError) return errno;

        AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: bootSector.Length={0}", bootSector.Length);

        var bootChk = 0;

        if(bootSector.Length < 512)
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: Boot sector too small");

            return ErrorNumber.InvalidArgument;
        }

        for(var i = 0; i < 0x1FF; i++) bootChk = (bootChk & 0xFF) + (bootChk >> 8) + bootSector[i];

        AaruLogging.Debug(MODULE_NAME,
                          "TryNewMapFormat: bootChk={0}, bootSector[0x1FF]={1}",
                          bootChk,
                          bootSector[0x1FF]);

        DiscRecord drSb;

        // Check if new map checksum is valid
        if(newChk == sector[0] && newChk != 0)
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: Using new map checksum");
            NewMap nmap = Marshal.ByteArrayToStructureLittleEndian<NewMap>(sector);
            drSb = nmap.discRecord;
        }

        // Check if boot block checksum is valid
        else if((bootChk & 0xFF) == bootSector[0x1FF])
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: Using boot block checksum");
            BootBlock bBlock = Marshal.ByteArrayToStructureLittleEndian<BootBlock>(bootSector);
            drSb = bBlock.discRecord;
        }
        else
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: No valid checksum found");

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "TryNewMapFormat: log2secsize={0}, idlen={1}, disc_size_high={2}, disc_size={3}",
                          drSb.log2secsize,
                          drSb.idlen,
                          drSb.disc_size_high,
                          drSb.disc_size);

        AaruLogging.Debug(MODULE_NAME,
                          "TryNewMapFormat: reserved is null or empty = {0}",
                          ArrayHelpers.ArrayIsNullOrEmpty(drSb.reserved));

        // Validate disc record
        if(drSb.log2secsize is < 8 or > 10)
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: Invalid log2secsize");

            return ErrorNumber.InvalidArgument;
        }

        if(drSb.idlen < drSb.log2secsize + 3 || drSb.idlen > 19)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "TryNewMapFormat: Invalid idlen ({0} < {1} or > 19)",
                              drSb.idlen,
                              drSb.log2secsize + 3);

            return ErrorNumber.InvalidArgument;
        }

        if(drSb.disc_size_high >> drSb.log2secsize != 0)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "TryNewMapFormat: disc_size_high validation failed ({0} >> {1} = {2})",
                              drSb.disc_size_high,
                              drSb.log2secsize,
                              drSb.disc_size_high >> drSb.log2secsize);

            return ErrorNumber.InvalidArgument;
        }

        if(!ArrayHelpers.ArrayIsNullOrEmpty(drSb.reserved))
        {
            AaruLogging.Debug(MODULE_NAME,
                              "TryNewMapFormat: Reserved field not empty (length={0})",
                              drSb.reserved?.Length ?? -1);

            return ErrorNumber.InvalidArgument;
        }

        _discRecord = drSb;

        // Calculate disc size
        _discSize =  drSb.disc_size_high;
        _discSize *= 0x100000000;
        _discSize += drSb.disc_size;

        if(_discSize > _imagePlugin.Info.Sectors * _imagePlugin.Info.SectorSize) return ErrorNumber.InvalidArgument;

        // Set block size and other parameters
        _blockSize          = 1 << drSb.log2secsize;
        _log2BytesPerMapBit = drSb.log2bpmb;

        // Calculate map parameters (following Linux kernel adfs_read_map)
        _nzones   = drSb.nzones | drSb.nzones_high << 8;
        _zoneSize = (8 << drSb.log2secsize) - drSb.zone_spare;
        _map2blk  = drSb.log2bpmb           - drSb.log2secsize;

        // IDs per zone: zone_size / (idlen + 1)
        _idsPerZone = (uint)(_zoneSize / (drSb.idlen + 1));

        // Load and cache the zone map for better performance
        ErrorNumber mapErr = LoadZoneMap();

        if(mapErr != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "TryNewMapFormat: Failed to load zone map: {0}", mapErr);

            return mapErr;
        }

        // Determine directory format and name length
        if(drSb.format_version > 0)
        {
            // ADFS-G or later: big directories
            _isBigDirectory    = true;
            _maxNameLen        = FPLUS_NAME_LEN;
            _rootDirectorySize = drSb.root_size;
        }
        else if((drSb.flags & 0x01) != 0)
        {
            // ADFS-F+: big directories for discs > 512MB
            _isBigDirectory    = true;
            _maxNameLen        = FPLUS_NAME_LEN;
            _rootDirectorySize = drSb.root_size;
        }
        else
        {
            // ADFS-E or ADFS-F: standard directories
            _isBigDirectory    = false;
            _maxNameLen        = F_NAME_LEN;
            _rootDirectorySize = NEW_DIRECTORY_SIZE;
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
        AaruLogging.Debug(MODULE_NAME,
                          "ValidateRootDirectory: rootAddr={0}, rootSize={1}, isBigDir={2}",
                          _rootDirectoryAddress,
                          _rootDirectorySize,
                          _isBigDirectory);

        ErrorNumber errno = ReadDirectoryData(_rootDirectoryAddress, _rootDirectorySize, out byte[] dirData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ValidateRootDirectory: ReadDirectoryData failed with {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "ValidateRootDirectory: dirData.Length={0}", dirData.Length);

        if(_isBigDirectory)
        {
            // Check big directory magic
            if(dirData.Length < 8)
            {
                AaruLogging.Debug(MODULE_NAME, "ValidateRootDirectory: dirData too short for big directory");

                return ErrorNumber.InvalidArgument;
            }

            var startMagic = BitConverter.ToUInt32(dirData, 4);

            AaruLogging.Debug(MODULE_NAME,
                              "ValidateRootDirectory: big dir startMagic=0x{0:X8}, expected=0x{1:X8}",
                              startMagic,
                              BIG_DIR_START_NAME);

            if(startMagic != BIG_DIR_START_NAME) return ErrorNumber.InvalidArgument;
        }
        else
        {
            // Check standard directory magic (Hugo or Nick)
            DirectoryHeader header = Marshal.ByteArrayToStructureLittleEndian<DirectoryHeader>(dirData);

            AaruLogging.Debug(MODULE_NAME,
                              "ValidateRootDirectory: magic=0x{0:X8}, OLD=0x{1:X8}, NEW=0x{2:X8}",
                              header.magic,
                              OLD_DIR_MAGIC,
                              NEW_DIR_MAGIC);

            if(header.magic != OLD_DIR_MAGIC && header.magic != NEW_DIR_MAGIC) return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Loads and caches the root directory contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        ErrorNumber errno = ReadDirectoryData(_rootDirectoryAddress, _rootDirectorySize, out byte[] dirData);

        if(errno != ErrorNumber.NoError) return errno;

        if(_isBigDirectory) return ParseBigDirectory(dirData);

        return ParseStandardDirectory(dirData);
    }

    /// <summary>Loads and caches the zone map for new format ADFS volumes</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadZoneMap()
    {
        if(_nzones <= 0 || _blockSize <= 0) return ErrorNumber.InvalidArgument;

        _mapCache = new byte[_nzones][];

        // Calculate map start address following Linux kernel algorithm:
        // zone_size is in bits: (8 << log2secsize) - zone_spare
        // map_addr_bits = (nzones >> 1) * zone_size - ((nzones > 1) ? ADFS_DR_SIZE_BITS : 0)
        // map_addr_sectors = signed_asl(map_addr_bits, map2blk)
        // where map2blk = log2bpmb - log2secsize
        long mapAddrBits = (long)(_nzones >> 1) * _zoneSize - (_nzones > 1 ? DISC_RECORD_SIZE * 8 : 0);

        AaruLogging.Debug(MODULE_NAME,
                          "LoadZoneMap: nzones={0}, zoneSize={1} bits, map2blk={2}, mapAddrBits={3}",
                          _nzones,
                          _zoneSize,
                          _map2blk,
                          mapAddrBits);

        // Apply map2blk shift to convert from bits to sectors
        long mapAddrSectors;

        if(_map2blk >= 0)
            mapAddrSectors = mapAddrBits << _map2blk;
        else
            mapAddrSectors = mapAddrBits >> -_map2blk;

        AaruLogging.Debug(MODULE_NAME, "LoadZoneMap: mapAddrSectors={0}", mapAddrSectors);

        // Read each zone's map data - each zone is one sector at map_addr + zone
        for(var zone = 0; zone < _nzones; zone++)
        {
            // Zone sector = map_addr + zone (each zone is one ADFS sector)
            var zoneSector = (ulong)(mapAddrSectors + zone);

            AaruLogging.Debug(MODULE_NAME, "LoadZoneMap: Reading zone {0} from ADFS sector {1}", zone, zoneSector);

            // Handle sector size differences between image and ADFS
            ErrorNumber errno = ReadAdfsSector(zoneSector, out byte[] zoneData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "LoadZoneMap: Failed to read zone {0} at sector {1}: {2}",
                                  zone,
                                  zoneSector,
                                  errno);

                return errno;
            }

            AaruLogging.Debug(MODULE_NAME,
                              "LoadZoneMap: Zone {0} read, length={1}, first bytes: {2:X2} {3:X2} {4:X2} {5:X2}",
                              zone,
                              zoneData.Length,
                              zoneData.Length > 0 ? zoneData[0] : 0,
                              zoneData.Length > 1 ? zoneData[1] : 0,
                              zoneData.Length > 2 ? zoneData[2] : 0,
                              zoneData.Length > 3 ? zoneData[3] : 0);

            _mapCache[zone] = zoneData;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a single ADFS sector, handling image sector size differences</summary>
    /// <param name="adfsSector">ADFS sector number</param>
    /// <param name="data">Output sector data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadAdfsSector(ulong adfsSector, out byte[] data)
    {
        data = null;

        // Handle sector size differences
        if(_imageSectorSize == (uint)_blockSize)
        {
            // Sector sizes match - direct read
            return _imagePlugin.ReadSector(adfsSector + _partition.Start, false, out data, out _);
        }

        if(_imageSectorSize > (uint)_blockSize)
        {
            // Image sectors larger than ADFS sectors - read containing sector and extract
            ulong adfsSecPerImgSec = _imageSectorSize / (uint)_blockSize;
            ulong imageSector      = adfsSector / adfsSecPerImgSec + _partition.Start;
            var   offsetInSector   = (int)(adfsSector % adfsSecPerImgSec * (uint)_blockSize);

            ErrorNumber errno = _imagePlugin.ReadSector(imageSector, false, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            data = new byte[_blockSize];

            if(offsetInSector + _blockSize <= sectorData.Length)
                Array.Copy(sectorData, offsetInSector, data, 0, _blockSize);
            else
                Array.Copy(sectorData, offsetInSector, data, 0, sectorData.Length - offsetInSector);

            return ErrorNumber.NoError;
        }

        // Image sectors smaller than ADFS sectors - read multiple sectors
        ulong imgSecPerAdfsSec = (uint)_blockSize / _imageSectorSize;
        ulong startSector      = adfsSector * imgSecPerAdfsSec + _partition.Start;

        ErrorNumber err = _imagePlugin.ReadSectors(startSector, false, (uint)imgSecPerAdfsSec, out data, out _);

        return err;
    }
}