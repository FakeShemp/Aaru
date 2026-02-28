// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ECMA-67 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Mounts the ECMA-67 file system and provides access to its contents.
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
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the filesystem described in ECMA-67</summary>
public sealed partial class ECMA67
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.ASCII;

        if(partition.Start > 0) return ErrorNumber.InvalidArgument;

        if(partition.End < 8) return ErrorNumber.InvalidArgument;

        // Read and validate Volume Label (VOL1) — cylinder 00, side 0, sector 07 (LBA 6)
        ErrorNumber errno = _imagePlugin.ReadSector(SECTOR_VOL1 - 1, false, out byte[] volSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading Volume Label sector: {0}", errno);

            return errno;
        }

        if(volSector.Length != PHYSICAL_RECORD_LENGTH_INDEX_SIDE0)
        {
            AaruLogging.Debug(MODULE_NAME, "Unexpected Volume Label sector size: {0}", volSector.Length);

            return ErrorNumber.InvalidArgument;
        }

        _volumeLabel = Marshal.ByteArrayToStructureLittleEndian<VolumeLabel>(volSector);

        if(!_magic.SequenceEqual(_volumeLabel.labelIdentifier))
        {
            AaruLogging.Debug(MODULE_NAME, "Volume Label identifier mismatch");

            return ErrorNumber.InvalidArgument;
        }

        // Match the same validation as Identify
        if(_volumeLabel is not { labelNumber: 1, recordLength: 0x31 })
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid Volume Label number (0x{0:X2}) or Physical Record Length Identifier (0x{1:X2})",
                              _volumeLabel.labelNumber,
                              _volumeLabel.recordLength);

            return ErrorNumber.InvalidArgument;
        }

        _doubleSided = _volumeLabel.surface == SURFACE_DOUBLE_SIDE;

        bool hasLabelsOnSide1 = _doubleSided && _volumeLabel.fileLabelAllocation == LABEL_NUMBER;

        string volumeName = Encoding.ASCII.GetString(_volumeLabel.volumeIdentifier).Trim();
        string ownerName  = Encoding.ASCII.GetString(_volumeLabel.owner).Trim();

        AaruLogging.Debug(MODULE_NAME, "Volume name: {0}",      volumeName);
        AaruLogging.Debug(MODULE_NAME, "Volume owner: {0}",     ownerName);
        AaruLogging.Debug(MODULE_NAME, "Double-sided: {0}",     _doubleSided);
        AaruLogging.Debug(MODULE_NAME, "Labels on side 1: {0}", hasLabelsOnSide1);

        // Read Error Map Label (ERMAP) — cylinder 00, side 0, sector 05 (LBA 4)
        errno = _imagePlugin.ReadSector(SECTOR_ERMAP - 1, false, out byte[] ermapSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading ERMAP sector: {0}", errno);

            return errno;
        }

        _errorMap = Marshal.ByteArrayToStructureLittleEndian<ErrorMapLabel>(ermapSector);

        if(_ermapMagic.SequenceEqual(_errorMap.labelIdentifier))
            AaruLogging.Debug(MODULE_NAME, "ERMAP label validated");
        else
            AaruLogging.Debug(MODULE_NAME, "ERMAP label identifier mismatch, continuing anyway");

        // Read File Labels (HDR1) from side 0 — sectors 08-16 (LBA 7-15)
        _fileLabels = new Dictionary<string, FileLabel>();

        for(int sector = SECTOR_HDR1_SIDE0_FIRST; sector <= SECTOR_HDR1_SIDE0_LAST; sector++)
        {
            var lba = (ulong)(sector - 1);

            errno = _imagePlugin.ReadSector(lba, false, out byte[] hdrSector, out _);

            if(errno != ErrorNumber.NoError) continue;

            if(hdrSector.Length < PHYSICAL_RECORD_LENGTH_INDEX_SIDE0) continue;

            FileLabel hdr = Marshal.ByteArrayToStructureLittleEndian<FileLabel>(hdrSector);

            if(!_hdrMagic.SequenceEqual(hdr.labelIdentifier)) continue;

            string fileName = Encoding.ASCII.GetString(hdr.fileIdentifier).Trim();

            if(string.IsNullOrEmpty(fileName)) continue;

            _fileLabels.TryAdd(fileName, hdr);

            AaruLogging.Debug(MODULE_NAME, "Found file: \"{0}\"", fileName);
        }

        // Read File Labels from side 1 — sectors 01-16 (LBA 16-31) if applicable
        if(hasLabelsOnSide1)
        {
            for(int sector = SECTOR_HDR1_SIDE1_FIRST; sector <= SECTOR_HDR1_SIDE1_LAST; sector++)
            {
                // Side 1 of cylinder 00 starts after all 16 sectors of side 0
                var lba = (ulong)(SECTOR_HDR1_SIDE0_LAST + sector - 1);

                errno = _imagePlugin.ReadSector(lba, false, out byte[] hdrSector, out _);

                if(errno != ErrorNumber.NoError) continue;

                if(hdrSector.Length < PHYSICAL_RECORD_LENGTH_INDEX_SIDE0) continue;

                FileLabel hdr = Marshal.ByteArrayToStructureLittleEndian<FileLabel>(hdrSector);

                if(!_hdrMagic.SequenceEqual(hdr.labelIdentifier)) continue;

                string fileName = Encoding.ASCII.GetString(hdr.fileIdentifier).Trim();

                if(string.IsNullOrEmpty(fileName)) continue;

                _fileLabels.TryAdd(fileName, hdr);

                AaruLogging.Debug(MODULE_NAME, "Found file on side 1: \"{0}\"", fileName);
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Found {0} file(s)", _fileLabels.Count);

        // Build metadata
        Metadata = new FileSystem
        {
            Type        = FS_TYPE,
            ClusterSize = PHYSICAL_RECORD_LENGTH_DATA,
            Clusters    = partition.End - partition.Start + 1,
            VolumeName  = volumeName,
            Files       = (ulong)_fileLabels.Count
        };

        _statfs = new FileSystemInfo
        {
            Blocks         = partition.End - partition.Start + 1,
            FilenameLength = 17,
            Files          = (ulong)_fileLabels.Count,
            PluginId       = Id,
            Type           = FS_TYPE
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Volume mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        _fileLabels?.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _encoding    = null;
        _volumeLabel = default(VolumeLabel);
        _errorMap    = default(ErrorMapLabel);
        _doubleSided = false;
        _statfs      = null;
        Metadata     = null;

        return ErrorNumber.NoError;
    }
}