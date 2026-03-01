// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft NT File System plugin.
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
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class NTFS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _image     = imagePlugin;
        _partition = partition;
        _encoding  = encoding ?? Encoding.Unicode;

        options ??= GetDefaultOptions();

        if(options.TryGetValue("debug", out string debugString)) bool.TryParse(debugString, out _debug);

        // Read boot sector (sector 0)
        AaruLogging.Debug(MODULE_NAME, "Reading boot sector at sector {0}", _partition.Start);

        ErrorNumber errno = _image.ReadSector(_partition.Start, false, out byte[] bootSector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading boot sector: {0}", errno);

            return errno;
        }

        _bpb = Marshal.ByteArrayToStructureLittleEndian<BiosParameterBlock>(bootSector);

        // Validate OEM name
        string oemName = StringHandlers.CToString(_bpb.oem_name);

        if(oemName != "NTFS    ")
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid OEM name: \"{0}\" (expected \"NTFS    \")", oemName);

            return ErrorNumber.InvalidArgument;
        }

        // Validate boot sector signature
        if(_bpb.signature2 != 0xAA55)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid boot sector signature: 0x{0:X4} (expected 0xAA55)",
                              _bpb.signature2);

            return ErrorNumber.InvalidArgument;
        }

        // Calculate sizes
        _bytesPerSector    = _bpb.bps;
        _sectorsPerCluster = _bpb.spc;
        _bytesPerCluster   = _bytesPerSector * _sectorsPerCluster;

        if(_bpb.mft_rc_clusters > 0)
            _mftRecordSize = (uint)(_bpb.mft_rc_clusters * _bytesPerCluster);
        else
            _mftRecordSize = (uint)(1 << -_bpb.mft_rc_clusters);

        if(_bpb.index_blk_cts > 0)
            _indexBlockSize = (uint)(_bpb.index_blk_cts * _bytesPerCluster);
        else
            _indexBlockSize = (uint)(1 << -_bpb.index_blk_cts);

        AaruLogging.Debug(MODULE_NAME, "Bytes per sector: {0}",         _bytesPerSector);
        AaruLogging.Debug(MODULE_NAME, "Sectors per cluster: {0}",      _sectorsPerCluster);
        AaruLogging.Debug(MODULE_NAME, "Bytes per cluster: {0}",        _bytesPerCluster);
        AaruLogging.Debug(MODULE_NAME, "MFT record size: {0}",          _mftRecordSize);
        AaruLogging.Debug(MODULE_NAME, "Index block size: {0}",         _indexBlockSize);
        AaruLogging.Debug(MODULE_NAME, "MFT starts at cluster {0}",     _bpb.mft_lsn);
        AaruLogging.Debug(MODULE_NAME, "MFTMirr starts at cluster {0}", _bpb.mftmirror_lsn);

        // Read MFT record #0 ($MFT) to validate the MFT is readable
        errno = ReadMftRecord((uint)SystemFileNumber.Mft, out byte[] mftRecord0);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading $MFT record: {0}", errno);

            return errno;
        }

        MftRecord mftHeader = Marshal.ByteArrayToStructureLittleEndian<MftRecord>(mftRecord0);

        if(mftHeader.magic != NtfsRecordMagic.File)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid $MFT magic: 0x{0:X8} (expected 0x{1:X8})",
                              (uint)mftHeader.magic,
                              (uint)NtfsRecordMagic.File);

            return ErrorNumber.InvalidArgument;
        }

        // Parse $MFT's own $DATA attribute data runs for fragmented MFT support
        _mftDataRuns = ParseMftDataRuns(mftRecord0, mftHeader);

        if(_mftDataRuns == null || _mftDataRuns.Count == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Could not parse $MFT data runs, MFT access will be limited");

            _mftDataRuns = null;
        }

        // Read MFT record #3 ($Volume) to get volume name and version info
        errno = ReadMftRecord((uint)SystemFileNumber.Volume, out byte[] volumeRecord);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading $Volume record: {0}", errno);

            return errno;
        }

        MftRecord volHeader = Marshal.ByteArrayToStructureLittleEndian<MftRecord>(volumeRecord);

        if(volHeader.magic != NtfsRecordMagic.File)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid $Volume magic: 0x{0:X8} (expected 0x{1:X8})",
                              (uint)volHeader.magic,
                              (uint)NtfsRecordMagic.File);

            return ErrorNumber.InvalidArgument;
        }

        // Parse $Volume attributes to get volume name and version
        var  volumeName = "";
        byte majorVer   = 0;
        byte minorVer   = 0;
        var  isDirty    = false;

        ParseVolumeRecord(volumeRecord, volHeader, out volumeName, out majorVer, out minorVer, out isDirty);

        AaruLogging.Debug(MODULE_NAME, "Volume name: \"{0}\"",  volumeName);
        AaruLogging.Debug(MODULE_NAME, "NTFS version: {0}.{1}", majorVer, minorVer);
        AaruLogging.Debug(MODULE_NAME, "Volume dirty: {0}",     isDirty);

        _ntfsVersion = $"{majorVer}.{minorVer}";

        // Read MFT record #5 (root directory)
        errno = ReadMftRecord((uint)SystemFileNumber.Root, out byte[] rootRecord);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root directory record: {0}", errno);

            return errno;
        }

        MftRecord rootHeader = Marshal.ByteArrayToStructureLittleEndian<MftRecord>(rootRecord);

        if(rootHeader.magic != NtfsRecordMagic.File)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid root directory magic: 0x{0:X8} (expected 0x{1:X8})",
                              (uint)rootHeader.magic,
                              (uint)NtfsRecordMagic.File);

            return ErrorNumber.InvalidArgument;
        }

        // Validate the root record is a directory
        if(!rootHeader.flags.HasFlag(MftRecordFlags.IsDirectory))
        {
            AaruLogging.Debug(MODULE_NAME, "Root MFT record is not a directory");

            return ErrorNumber.InvalidArgument;
        }

        if(!rootHeader.flags.HasFlag(MftRecordFlags.InUse))
        {
            AaruLogging.Debug(MODULE_NAME, "Root MFT record is not in use");

            return ErrorNumber.InvalidArgument;
        }

        // Parse $Secure (MFT record #9) to load centralized security descriptors (NTFS 3.0+)
        _securityDescriptors = new Dictionary<uint, byte[]>();

        if(majorVer >= 3) LoadSecureDescriptors();

        // Initialize caches
        _rootDirectoryCache = new Dictionary<string, ulong>();

        // Cache root directory entries from the $INDEX_ROOT attribute
        errno = CacheRootDirectory(rootRecord, rootHeader);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error caching root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory contains {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            ClusterSize  = _bytesPerCluster,
            Clusters     = (ulong)(_bpb.sectors / _bpb.spc),
            VolumeSerial = $"{_bpb.serial_no:X16}",
            VolumeName   = volumeName,
            Dirty        = isDirty
        };

        // Build filesystem info for StatFs
        _statfs = new FileSystemInfo
        {
            Blocks         = (ulong)(_bpb.sectors / _bpb.spc),
            FilenameLength = 255,
            Files          = 0,
            FreeBlocks     = 0,
            FreeFiles      = 0,
            Id = new FileSystemId
            {
                IsLong   = true,
                Serial64 = _bpb.serial_no
            },
            PluginId = Id,
            Type     = FS_TYPE
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        _rootDirectoryCache?.Clear();
        _securityDescriptors?.Clear();
        _mounted = false;

        return ErrorNumber.NoError;
    }

    /// <summary>Parses the $Volume MFT record to extract volume name, version, and dirty flag.</summary>
    /// <param name="recordData">Raw MFT record data after USA fixup.</param>
    /// <param name="header">Parsed MFT record header.</param>
    /// <param name="volumeName">Output volume name string.</param>
    /// <param name="majorVer">Output NTFS major version.</param>
    /// <param name="minorVer">Output NTFS minor version.</param>
    /// <param name="isDirty">Output dirty flag.</param>
    void ParseVolumeRecord(byte[]   recordData, in  MftRecord header, out string volumeName, out byte majorVer,
                           out byte minorVer,   out bool      isDirty)
    {
        volumeName = "";
        majorVer   = 0;
        minorVer   = 0;
        isDirty    = false;

        int offset = header.attrs_offset;

        while(offset + 4 <= recordData.Length)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(recordData, offset);

            if(attrType == AttributeType.End || attrType == AttributeType.Unused) break;

            var attrLength = BitConverter.ToUInt32(recordData, offset + 4);

            if(attrLength == 0 || offset + attrLength > recordData.Length) break;

            byte nonResident = recordData[offset + 8];

            if(nonResident == 0) // Resident attribute
            {
                var valueLength = BitConverter.ToUInt32(recordData, offset + 0x10);
                var valueOffset = BitConverter.ToUInt16(recordData, offset + 0x14);

                int valueStart = offset + valueOffset;

                if(valueStart + valueLength > recordData.Length)
                {
                    offset += (int)attrLength;

                    continue;
                }

                switch(attrType)
                {
                    case AttributeType.VolumeName:
                        if(valueLength > 0)
                            volumeName = Encoding.Unicode.GetString(recordData, valueStart, (int)valueLength);

                        break;

                    case AttributeType.VolumeInformation:
                        if(valueLength >= 12)
                        {
                            VolumeInformation volInfo =
                                Marshal.ByteArrayToStructureLittleEndian<VolumeInformation>(recordData,
                                    valueStart,
                                    (int)valueLength);

                            majorVer = volInfo.major_ver;
                            minorVer = volInfo.minor_ver;
                            isDirty  = volInfo.flags.HasFlag(VolumeFlags.IsDirty);
                        }

                        break;
                }
            }

            offset += (int)attrLength;
        }
    }

    /// <summary>
    ///     Loads security descriptors from the $Secure system file's $SDS named data stream.
    ///     Populates <see cref="_securityDescriptors" /> with a mapping from security_id to raw descriptor bytes.
    /// </summary>
    void LoadSecureDescriptors()
    {
        ErrorNumber errno = ReadMftRecord((uint)SystemFileNumber.Secure, out byte[] secureRecord);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading $Secure MFT record: {0}", errno);

            return;
        }

        MftRecord secureHeader = Marshal.ByteArrayToStructureLittleEndian<MftRecord>(secureRecord);

        if(secureHeader.magic != NtfsRecordMagic.File) return;

        // Read the $SDS stream data (may be non-resident)
        byte[] sdsData = null;

        // Try assembling non-resident data runs for $SDS
        ErrorNumber runErrno = AssembleNonResidentRuns((uint)SystemFileNumber.Secure,
                                                       AttributeType.Data,
                                                       "$SDS",
                                                       out List<(long offset, long length)> allDataRuns,
                                                       out long sdsDataSize,
                                                       out _,
                                                       out _,
                                                       out _);

        if(runErrno == ErrorNumber.NoError && allDataRuns.Count > 0 && sdsDataSize > 0)
        {
            byte[] readBuf = Array.Empty<byte>();
            errno = ReadNonResidentData(allDataRuns, sdsDataSize, ref readBuf);

            if(errno == ErrorNumber.NoError) sdsData = readBuf;
        }
        else
        {
            // Resident $SDS (unlikely but handle it)
            ErrorNumber findErrno = FindAttributes(secureRecord,
                                                   secureHeader,
                                                   (uint)SystemFileNumber.Secure,
                                                   AttributeType.Data,
                                                   "$SDS",
                                                   out List<FoundAttribute> sdsResults);

            if(findErrno == ErrorNumber.NoError && sdsResults.Count > 0)
            {
                FoundAttribute attr   = sdsResults[0];
                byte           nonRes = attr.RecordData[attr.Offset + 8];

                if(nonRes == 0)
                {
                    var valueOffset = BitConverter.ToUInt16(attr.RecordData, attr.Offset + 0x14);
                    var valueLength = BitConverter.ToUInt32(attr.RecordData, attr.Offset + 0x10);

                    int valueStart = attr.Offset + valueOffset;

                    if(valueStart + valueLength <= attr.RecordData.Length && valueLength > 0)
                    {
                        sdsData = new byte[valueLength];
                        Array.Copy(attr.RecordData, valueStart, sdsData, 0, valueLength);
                    }
                }
            }
        }

        if(sdsData == null || sdsData.Length == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Could not read $Secure::$SDS data stream");

            return;
        }

        // Parse SDS entries: each has a 20-byte SdsEntryHeader followed by the security descriptor
        int sdsHeaderSize = Marshal.SizeOf<SdsEntryHeader>();
        var pos           = 0;

        while(pos + sdsHeaderSize <= sdsData.Length)
        {
            SdsEntryHeader entryHeader =
                Marshal.ByteArrayToStructureLittleEndian<SdsEntryHeader>(sdsData, pos, sdsHeaderSize);

            // End marker or invalid entry
            if(entryHeader.size < sdsHeaderSize || entryHeader.security_id == 0) break;

            // Validate bounds
            if(pos + entryHeader.size > sdsData.Length) break;

            int sdSize = (int)entryHeader.size - sdsHeaderSize;

            if(sdSize > 0 && !_securityDescriptors.ContainsKey(entryHeader.security_id))
            {
                var sd = new byte[sdSize];
                Array.Copy(sdsData, pos + sdsHeaderSize, sd, 0, sdSize);
                _securityDescriptors[entryHeader.security_id] = sd;
            }

            // Entries are 16-byte aligned
            var totalSize = (int)(entryHeader.size + 15 & ~15u);

            if(totalSize == 0) break;

            pos += totalSize;
        }

        AaruLogging.Debug(MODULE_NAME, "Loaded {0} security descriptors from $Secure", _securityDescriptors.Count);
    }
}