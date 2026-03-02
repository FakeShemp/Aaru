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
using System.Linq;
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
    ///     Finds and decodes all directory entries by reading the root directory system file entry
    ///     at sector 6, extracting its SUBF extent list pointer, and parsing the directory B-tree.
    /// </summary>
    ErrorNumber FindAndDecodeRootDirectory(out Dictionary<string, CachedDirectoryEntry> rootEntries)
    {
        rootEntries = null;

        uint totalSectors = _bpb.big_sectors > 0 ? _bpb.big_sectors : _bpb.sectors;

        // Collect all entries from directory DATA sectors
        List<(string fullPath, uint timestamp, byte attributes, uint sectorAddress, uint creationTimestamp, uint
            modificationTimestamp, uint fileSize)> allEntries = new();

        // Read the root directory system file entry at sector 6.
        // This sector contains the directory's SUBF extent list pointer at offset +0x4C.
        ErrorNumber dirEntryErrno =
            _image.ReadSector(ROOT_DIR_ENTRY_SECTOR + _partition.Start, false, out byte[] dirEntryData, out _);

        if(dirEntryErrno != ErrorNumber.NoError || dirEntryData.Length < 0x50)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read root directory entry at sector {0}", ROOT_DIR_ENTRY_SECTOR);

            return ErrorNumber.NoSuchFile;
        }

        // Verify the system file type is SYSFILE_TYPE_DIRECTORY (3)
        var sysFileType = BigEndianBitConverter.ToUInt32(dirEntryData, 0x00);

        if(sysFileType != SYSFILE_TYPE_DIRECTORY)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Sector {0} is not a directory system file entry (type={1}, expected={2})",
                              ROOT_DIR_ENTRY_SECTOR,
                              sysFileType,
                              SYSFILE_TYPE_DIRECTORY);

            return ErrorNumber.NoSuchFile;
        }

        // Extract the SUBF extent list sector from offset +0x4C
        var subfSector = BigEndianBitConverter.ToUInt32(dirEntryData, 0x4C);

        AaruLogging.Debug(MODULE_NAME, "Root directory SUBF sector=0x{0:X4}", subfSector);

        if(subfSector == 0 || subfSector >= totalSectors || subfSector == EXTENT_END_MARKER)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid SUBF sector pointer: 0x{0:X4}", subfSector);

            return ErrorNumber.NoSuchFile;
        }

        ReadDirectoryFromSubf(subfSector, allEntries);

        if(allEntries.Count == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "FindAndDecodeRootDirectory: no entries found in any DATA sector");

            return ErrorNumber.NoSuchFile;
        }

        AaruLogging.Debug(MODULE_NAME, "FindAndDecodeRootDirectory: collected {0} total entries", allEntries.Count);

        // Build directory tree from full paths
        rootEntries = new Dictionary<string, CachedDirectoryEntry>(StringComparer.OrdinalIgnoreCase);

        HashSet<string> rootDirectories = new(StringComparer.OrdinalIgnoreCase);

        // Determine directory separator
        var dirSep = '/';

        foreach((string fullPath, uint timestamp, byte attributes, uint sectorAddress, uint creationTimestamp,
                 uint modificationTimestamp, uint fileSize) in allEntries)
        {
            bool isDirectory = (attributes & 0x10) != 0;

            int sepIdx = fullPath.IndexOf(dirSep);

            if(sepIdx < 0)
            {
                // Root-level entry
                if(!rootEntries.ContainsKey(fullPath))
                {
                    rootEntries[fullPath] = new CachedDirectoryEntry
                    {
                        Name                  = fullPath,
                        Timestamp             = timestamp,
                        IsDirectory           = isDirectory,
                        Attributes            = attributes,
                        SectorAddress         = sectorAddress,
                        CreationTimestamp     = creationTimestamp,
                        ModificationTimestamp = modificationTimestamp,
                        FileSize              = fileSize
                    };
                }
                else if(isDirectory) rootEntries[fullPath].IsDirectory = true;
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
                        Name                  = fileName,
                        Timestamp             = timestamp,
                        IsDirectory           = isDirectory,
                        Attributes            = attributes,
                        SectorAddress         = sectorAddress,
                        CreationTimestamp     = creationTimestamp,
                        ModificationTimestamp = modificationTimestamp,
                        FileSize              = fileSize
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
                            Timestamp     = 0,
                            IsDirectory   = true,
                            Attributes    = 0x10,
                            SectorAddress = 0
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
                    Timestamp     = 0,
                    IsDirectory   = true,
                    Attributes    = 0x10,
                    SectorAddress = 0
                };
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory B-tree DATA nodes from a SUBF extent chain</summary>
    void ReadDirectoryFromSubf(uint subfSector,
                               List<(string fullPath, uint timestamp, byte attributes, uint sectorAddress, uint
                                   creationTimestamp, uint modificationTimestamp, uint fileSize)> allEntries)
    {
        ErrorNumber subfErrno = _image.ReadSector(subfSector + _partition.Start, false, out byte[] subfData, out _);

        if(subfErrno != ErrorNumber.NoError || subfData.Length < 0x20) return;

        if(!subfData[..4].SequenceEqual(_subfSignature)) return;

        var extentCount = BigEndianBitConverter.ToUInt16(subfData, 0x10);

        AaruLogging.Debug(MODULE_NAME, "SUBF at sector 0x{0:X4}: {1} extents", subfSector, extentCount);

        // Collect extent information for both per-extent scanning and gap detection
        List<(uint startLba, ushort sectorCount)> extents = new();

        for(var ei = 0; ei < extentCount; ei++)
        {
            int extOff = 0x20 + ei * 8;

            if(extOff + 8 > subfData.Length) break;

            var sectorCount = BigEndianBitConverter.ToUInt16(subfData, extOff);
            var startLba    = BigEndianBitConverter.ToUInt32(subfData, extOff + 4);

            if(startLba == EXTENT_END_MARKER) break;

            AaruLogging.Debug(MODULE_NAME, "SUBF extent [{0}]: {1} sectors at LBA 0x{2:X4}", ei, sectorCount, startLba);

            extents.Add((startLba, sectorCount));
        }

        if(extents.Count == 0) return;

        // Phase 1: Scan within each SUBF extent (nodes are 4-sector aligned from extent start)
        foreach((uint startLba, ushort sectorCount) in extents)
        {
            for(uint nodeOff = 0; nodeOff + 4 <= sectorCount; nodeOff += 4)
                ReadAndParseDataNode(startLba + nodeOff, allEntries);
        }

        // Phase 2: Scan gap areas between extents for additional DATA leaf nodes.
        // B-tree INDX and DATA nodes may exist in sectors between extents.
        // Sort extents by start LBA and find gaps.
        var sorted = extents.OrderBy(e => e.startLba).ToList();

        for(var i = 0; i < sorted.Count - 1; i++)
        {
            uint gapStart = sorted[i].startLba + sorted[i].sectorCount;
            uint gapEnd   = sorted[i + 1].startLba;

            if(gapStart >= gapEnd) continue;

            AaruLogging.Debug(MODULE_NAME, "Scanning gap between extents: LBA 0x{0:X4} to 0x{1:X4}", gapStart, gapEnd);

            // Scan gap with step 4, trying both alignments from adjacent extents
            for(uint sector = gapStart; sector + 4 <= gapEnd; sector += 4) ReadAndParseDataNode(sector, allEntries);

            // Also try offset +2 alignment (in case gap nodes align to the other extent)
            uint altStart = gapStart + (4 - (gapStart - sorted[0].startLba) % 4) % 4;

            if(altStart != gapStart && altStart + 4 <= gapEnd)
                for(uint sector = altStart; sector + 4 <= gapEnd; sector += 4)
                    ReadAndParseDataNode(sector, allEntries);
        }
    }

    /// <summary>Reads a 4-sector B-tree node and parses it if it's a DATA leaf node</summary>
    void ReadAndParseDataNode(uint nodeSector,
                              List<(string fullPath, uint timestamp, byte attributes, uint sectorAddress, uint
                                  creationTimestamp, uint modificationTimestamp, uint fileSize)> allEntries)
    {
        ErrorNumber nodeErrno = _image.ReadSectors(nodeSector + _partition.Start, false, 4, out byte[] nodeData, out _);

        if(nodeErrno != ErrorNumber.NoError || nodeData.Length < 0x28) return;

        // Check for DATA signature and level 0 (leaf nodes only)
        if(!nodeData[..4].SequenceEqual(_dataSignature)) return;

        var level = BigEndianBitConverter.ToUInt16(nodeData, 0x14);

        if(level != 0) return;

        List<(string fullPath, uint timestamp, byte attributes, uint sectorAddress, uint creationTimestamp, uint
            modificationTimestamp, uint fileSize)> nodeEntries = ParseDataEntries(nodeData);

        AaruLogging.Debug(MODULE_NAME, "DATA node at sector 0x{0:X4}: {1} entries", nodeSector, nodeEntries.Count);

        allEntries.AddRange(nodeEntries);
    }
}