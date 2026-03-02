// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Mounts and unmounts the High Performance Optical File System.
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
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class HPOFS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _encoding = encoding ?? Encoding.GetEncoding("ibm850");

        if(options is not null && options.TryGetValue("debug", out string debugString))
            bool.TryParse(debugString, out _debug);

        // Read BPB at sector 0 (little-endian)
        ErrorNumber errno = imagePlugin.ReadSector(0 + partition.Start, false, out byte[] bpbSector, out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(bpbSector.Length < 512) return ErrorNumber.InvalidArgument;

        _bpb = Marshal.ByteArrayToStructureLittleEndian<BiosParameterBlock>(bpbSector);

        if(!_bpb.fs_type.SequenceEqual(_type)) return ErrorNumber.InvalidArgument;

        // Read Media Information Block at sector 0x0D (big-endian)
        errno = imagePlugin.ReadSector(MEDINFO_SECTOR_PRIMARY + partition.Start,
                                       false,
                                       out byte[] medInfoSector,
                                       out _);

        if(errno != ErrorNumber.NoError) return errno;

        _medInfo = Marshal.ByteArrayToStructureBigEndian<MediaInformationBlock>(medInfoSector);

        if(!_medInfo.blockId.SequenceEqual(_medinfoSignature)) return ErrorNumber.InvalidArgument;

        // Read Volume Information Block at sector 0x0E (big-endian)
        errno = imagePlugin.ReadSector(VOLINFO_SECTOR_PRIMARY + partition.Start,
                                       false,
                                       out byte[] volInfoSector,
                                       out _);

        if(errno != ErrorNumber.NoError) return errno;

        _volInfo = Marshal.ByteArrayToStructureBigEndian<VolumeInformationBlock>(volInfoSector);

        if(!_volInfo.blockId.SequenceEqual(_volinfoSignature)) return ErrorNumber.InvalidArgument;

        // Read DCI Record at sector 0x14 (big-endian)
        errno = imagePlugin.ReadSector(DCI_SECTOR_PRIMARY + partition.Start, false, out byte[] dciSector, out _);

        if(errno != ErrorNumber.NoError) return errno;

        _dciRecord = Marshal.ByteArrayToStructureBigEndian<DciRecord>(dciSector);

        if(!_dciRecord.signature.SequenceEqual(_mastSignature)) return ErrorNumber.InvalidArgument;

        // Try to find and read the full SMI block for directory sector pointers
        _hasSmiBlock = false;
        uint smiScanLimit = _bpb.big_sectors > 0 ? _bpb.big_sectors : _bpb.sectors;

        if(smiScanLimit > 0x200) smiScanLimit = 0x200;

        for(uint s = 0x15; s < smiScanLimit; s++)
        {
            ErrorNumber smiErrno = imagePlugin.ReadSector(s + partition.Start, false, out byte[] smiData, out _);

            if(smiErrno != ErrorNumber.NoError || smiData.Length < 8) continue;

            if(!smiData[..8].SequenceEqual(_smiSignature)) continue;

            _smiBlock    = Marshal.ByteArrayToStructureBigEndian<SmiBlock>(smiData);
            _hasSmiBlock = true;

            AaruLogging.Debug(MODULE_NAME, "Found SMI block at sector 0x{0:X4}", s);

            AaruLogging.Debug(MODULE_NAME,
                              "  dirRange1Sector=0x{0:X8}, dirData1Sector=0x{1:X8}",
                              _smiBlock.dirRange1Sector,
                              _smiBlock.dirData1Sector);

            break;
        }

        // Set up encoding based on codepage type
        if(_medInfo.codepageType == CODEPAGE_TYPE_EBCDIC && _medInfo.codepage > 0)
        {
            try
            {
                _encoding = Encoding.GetEncoding(_medInfo.codepage);
            }
            catch
            {
                // Fallback to default
            }
        }
        else if(_medInfo.codepage > 0)
        {
            try
            {
                _encoding = Encoding.GetEncoding(_medInfo.codepage);
            }
            catch
            {
                // Fallback to default
            }
        }

        _image     = imagePlugin;
        _partition = partition;

        // Build metadata
        uint totalSectors = _bpb.big_sectors > 0 ? _bpb.big_sectors : _bpb.sectors;

        Metadata = new FileSystem
        {
            Clusters               = totalSectors / _bpb.spc,
            ClusterSize            = (uint)(_bpb.bps * _bpb.spc),
            CreationDate           = DateHandlers.DosToDateTime(_medInfo.creationDate, _medInfo.creationTime),
            Type                   = FS_TYPE,
            VolumeName             = StringHandlers.SpacePaddedToString(_medInfo.volumeLabel, _encoding),
            VolumeSerial           = $"{_medInfo.serial:X8}",
            SystemIdentifier       = StringHandlers.SpacePaddedToString(_bpb.oem_name),
            DataPreparerIdentifier = StringHandlers.SpacePaddedToString(_volInfo.owner, _encoding)
        };

        _statfs = new FileSystemInfo
        {
            Blocks         = totalSectors / (ulong)_bpb.spc,
            FilenameLength = MAX_FILENAME_LENGTH,
            FreeBlocks     = 0,
            Id = new FileSystemId
            {
                IsInt    = true,
                Serial32 = _medInfo.serial
            },
            PluginId = Id,
            Type     = FS_TYPE
        };

        // Decode root directory
        _directoryCache = new Dictionary<string, Dictionary<string, CachedDirectoryEntry>>();

        ErrorNumber dirError = FindAndDecodeRootDirectory(out Dictionary<string, CachedDirectoryEntry> rootEntries);

        if(dirError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Could not decode root directory");

            _rootDirectoryCache = new Dictionary<string, CachedDirectoryEntry>();
        }
        else
            _rootDirectoryCache = rootEntries;

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        _mounted            = false;
        _rootDirectoryCache = null;
        _directoryCache     = null;

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Finds and decodes all directory entries by scanning for INDX sectors and building a directory tree from
    ///     full paths
    /// </summary>
    ErrorNumber FindAndDecodeRootDirectory(out Dictionary<string, CachedDirectoryEntry> rootEntries)
    {
        rootEntries = null;

        uint totalSectors = _bpb.big_sectors > 0 ? _bpb.big_sectors : _bpb.sectors;

        // Collect all entries from all leaf INDX sectors
        List<(string fullPath, uint sectorAddr, uint timestamp)> allEntries = new();

        for(ulong s = 0; s < totalSectors; s++)
        {
            ErrorNumber errno = _image.ReadSector(s + _partition.Start, false, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError || sectorData.Length < 0x24) continue;

            // Check for INDX signature
            if(!sectorData[..4].SequenceEqual(_indxSignature)) continue;

            // Determine if this is a master (non-leaf) or leaf node
            // Master nodes have key separator character at header offset 0x18
            var fieldAt18 = BigEndianBitConverter.ToUInt16(sectorData, 0x18);

            bool isMaster = fieldAt18 == KEY_SEPARATOR_ASCII || fieldAt18 == KEY_SEPARATOR_EBCDIC;

            if(isMaster)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "INDX sector 0x{0:X4}: master node (keySep=0x{1:X2}), skipping",
                                  s,
                                  fieldAt18);

                continue;
            }

            // For leaf nodes, fieldAt18 is the total data size in bytes
            ushort totalDataSize = fieldAt18;

            var sectorsToRead = (uint)((0x24 + totalDataSize + _bpb.bps - 1) / _bpb.bps);

            if(sectorsToRead < 1) sectorsToRead = 1;

            if(sectorsToRead > 64) sectorsToRead = 64;

            byte[] nodeData;

            if(sectorsToRead > 1)
            {
                errno = _image.ReadSectors(s + _partition.Start, false, sectorsToRead, out nodeData, out _);

                if(errno != ErrorNumber.NoError) nodeData = sectorData;
            }
            else
                nodeData = sectorData;

            AaruLogging.Debug(MODULE_NAME,
                              "INDX sector 0x{0:X4}: leaf node, dataSize={1}, reading {2} sectors",
                              s,
                              totalDataSize,
                              sectorsToRead);

            // Parse leaf entries
            List<(string fullPath, uint sectorAddr, uint timestamp)> sectorEntries = ParseLeafEntries(nodeData);

            AaruLogging.Debug(MODULE_NAME, "Parsed {0} entries from INDX at 0x{1:X4}", sectorEntries.Count, s);

            allEntries.AddRange(sectorEntries);

            // Skip continuation sectors of multi-sector nodes
            s += sectorsToRead - 1;
        }

        if(allEntries.Count == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "FindAndDecodeRootDirectory: no entries found in any INDX sector");

            return ErrorNumber.NoSuchFile;
        }

        AaruLogging.Debug(MODULE_NAME, "FindAndDecodeRootDirectory: collected {0} total entries", allEntries.Count);

        // Build directory tree from full paths
        rootEntries = new Dictionary<string, CachedDirectoryEntry>(StringComparer.OrdinalIgnoreCase);

        HashSet<string> rootDirectories = new(StringComparer.OrdinalIgnoreCase);

        // Determine directory separator
        var dirSep = '/';

        foreach((string fullPath, uint sectorAddr, uint timestamp) in allEntries)
        {
            int sepIdx = fullPath.IndexOf(dirSep);

            if(sepIdx < 0)
            {
                // Root-level file
                if(!rootEntries.ContainsKey(fullPath))
                {
                    rootEntries[fullPath] = new CachedDirectoryEntry
                    {
                        Name          = fullPath,
                        SectorAddress = sectorAddr,
                        Timestamp     = timestamp,
                        IsDirectory   = false
                    };
                }
            }
            else
            {
                // Multi-component path: first component is a root directory
                string rootDir = fullPath[..sepIdx];
                rootDirectories.Add(rootDir);

                // Get the directory path and filename
                int lastSep = fullPath.LastIndexOf(dirSep);

                string dirPath  = fullPath[..lastSep];
                string fileName = fullPath[(lastSep + 1)..];

                if(string.IsNullOrWhiteSpace(fileName)) continue;

                if(!_directoryCache.ContainsKey(dirPath))
                {
                    _directoryCache[dirPath] =
                        new Dictionary<string, CachedDirectoryEntry>(StringComparer.OrdinalIgnoreCase);
                }

                if(!_directoryCache[dirPath].ContainsKey(fileName))
                {
                    _directoryCache[dirPath][fileName] = new CachedDirectoryEntry
                    {
                        Name          = fileName,
                        SectorAddress = sectorAddr,
                        Timestamp     = timestamp,
                        IsDirectory   = false
                    };
                }

                // Create intermediate directory entries
                string[] parts = fullPath.Split(dirSep);

                for(var i = 1; i < parts.Length - 1; i++)
                {
                    var    parentPath = string.Join(dirSep, parts[..i]);
                    string childName  = parts[i];

                    if(!_directoryCache.ContainsKey(parentPath))
                    {
                        _directoryCache[parentPath] =
                            new Dictionary<string, CachedDirectoryEntry>(StringComparer.OrdinalIgnoreCase);
                    }

                    if(!_directoryCache[parentPath].ContainsKey(childName))
                    {
                        _directoryCache[parentPath][childName] = new CachedDirectoryEntry
                        {
                            Name          = childName,
                            SectorAddress = 0,
                            Timestamp     = 0,
                            IsDirectory   = true
                        };
                    }
                }
            }
        }

        // Add root directories to root entries (or mark existing entries as directories)
        foreach(string dirName in rootDirectories)
        {
            if(rootEntries.TryGetValue(dirName, out CachedDirectoryEntry existing))
                existing.IsDirectory = true;
            else
            {
                rootEntries[dirName] = new CachedDirectoryEntry
                {
                    Name          = dirName,
                    SectorAddress = 0,
                    Timestamp     = 0,
                    IsDirectory   = true
                };
            }
        }

        return ErrorNumber.NoError;
    }
}