// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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
public sealed partial class RBF
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _encoding       = encoding ?? Encoding.GetEncoding("iso-8859-15");
        _imagePlugin    = imagePlugin;
        _partitionStart = partition.Start;
        _sectorSize     = imagePlugin.Info.SectorSize;

        if(_sectorSize < 256)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size {0} is too small for RBF", _sectorSize);

            return ErrorNumber.InvalidArgument;
        }

        // Initialize metadata
        Metadata = new FileSystem();

        // Try to find and validate the ID sector
        ErrorNumber errno = FindAndValidateIdSector();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to find or validate ID sector: {0}", errno);

            return errno;
        }

        // Find and validate the root directory FD
        errno = FindAndValidateRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to find or validate root directory: {0}", errno);

            return errno;
        }

        // Cache root directory contents
        errno = CacheRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to cache root directory: {0}", errno);

            return errno;
        }

        // Populate metadata
        PopulateMetadata();

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "RBF filesystem mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        // Clear root directory cache
        _rootDirectoryCache?.Clear();
        _rootDirectoryCache = null;

        // Clear root directory FD
        _rootDirectoryFd = default(FileDescriptor);

        // Clear ID sectors
        _idSector    = default(IdSector);
        _newIdSector = default(NewIdSector);

        // Reset flags
        _mounted      = false;
        _isOs9000     = false;
        _littleEndian = false;

        // Clear filesystem parameters
        _lsnSize           = 0;
        _rootDirLsn        = 0;
        _totalSectors      = 0;
        _sectorsPerCluster = 0;
        _bitmapLsn         = 0;
        _idSectorLocation  = 0;

        // Clear metadata
        Metadata = null;

        // Clear image reference
        _imagePlugin = null;

        AaruLogging.Debug(MODULE_NAME, "RBF filesystem unmounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Finds the ID sector at one of the known locations (0, 4, or 15) and validates it</summary>
    ErrorNumber FindAndValidateIdSector()
    {
        // Documentation says ID should be at sector 0
        // OS-9/X68000 has it at sector 4
        // OS-9/Apple2 has it at sector 15
        foreach(int i in new[]
                {
                    0, 4, 15
                })
        {
            var location = (ulong)i;

            var sbSize = (uint)(Marshal.SizeOf<IdSector>() / _sectorSize);

            if(Marshal.SizeOf<IdSector>() % _sectorSize != 0) sbSize++;

            if(_partitionStart + location + sbSize >= _imagePlugin.Info.Sectors) continue;

            ErrorNumber errno =
                _imagePlugin.ReadSectors(_partitionStart + location, false, sbSize, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) continue;

            if(sector.Length < Marshal.SizeOf<IdSector>()) continue;

            // Try OS-9 classic format (big-endian)
            IdSector rbfSb = Marshal.ByteArrayToStructureBigEndian<IdSector>(sector);

            if(rbfSb.dd_sync == RBF_SYNC)
            {
                _isOs9000          = false;
                _littleEndian      = false;
                _idSector          = rbfSb;
                _idSectorLocation  = location;
                _lsnSize           = (uint)(rbfSb.dd_lsnsize == 0 ? 256 : rbfSb.dd_lsnsize);
                _rootDirLsn        = LSNToUInt32(rbfSb.dd_dir);
                _totalSectors      = LSNToUInt32(rbfSb.dd_tot);
                _sectorsPerCluster = rbfSb.dd_bit;
                _bitmapLsn         = rbfSb.dd_maplsn == 0 ? 1 : rbfSb.dd_maplsn;

                AaruLogging.Debug(MODULE_NAME,
                                  "Found OS-9 ID sector at location {0}, LSN size {1}, root at LSN {2}",
                                  location,
                                  _lsnSize,
                                  _rootDirLsn);

                return ErrorNumber.NoError;
            }

            // Try OS-9000 format (big-endian first)
            NewIdSector rbf9000Sb = Marshal.ByteArrayToStructureBigEndian<NewIdSector>(sector);

            if(rbf9000Sb.rid_sync == RBF_SYNC)
            {
                _isOs9000          = true;
                _littleEndian      = false;
                _newIdSector       = rbf9000Sb;
                _idSectorLocation  = location;
                _lsnSize           = rbf9000Sb.rid_blocksize;
                _rootDirLsn        = rbf9000Sb.rid_rootdir;
                _totalSectors      = rbf9000Sb.rid_totblocks;
                _sectorsPerCluster = 1; // OS-9000 uses blocks directly
                _bitmapLsn         = rbf9000Sb.rid_bitmap == 0 ? 1 : rbf9000Sb.rid_bitmap;

                AaruLogging.Debug(MODULE_NAME,
                                  "Found OS-9000 (big-endian) ID sector at location {0}, block size {1}, root at LSN {2}",
                                  location,
                                  _lsnSize,
                                  _rootDirLsn);

                return ErrorNumber.NoError;
            }

            // Try OS-9000 format (little-endian)
            if(rbf9000Sb.rid_sync == RBF_CNYS)
            {
                _isOs9000          = true;
                _littleEndian      = true;
                _newIdSector       = rbf9000Sb.SwapEndian();
                _idSectorLocation  = location;
                _lsnSize           = _newIdSector.rid_blocksize;
                _rootDirLsn        = _newIdSector.rid_rootdir;
                _totalSectors      = _newIdSector.rid_totblocks;
                _sectorsPerCluster = 1;
                _bitmapLsn         = _newIdSector.rid_bitmap == 0 ? 1 : _newIdSector.rid_bitmap;

                AaruLogging.Debug(MODULE_NAME,
                                  "Found OS-9000 (little-endian) ID sector at location {0}, block size {1}, root at LSN {2}",
                                  location,
                                  _lsnSize,
                                  _rootDirLsn);

                return ErrorNumber.NoError;
            }
        }

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Finds and validates the root directory file descriptor</summary>
    ErrorNumber FindAndValidateRootDirectory()
    {
        if(_rootDirLsn == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Root directory LSN is 0, invalid");

            return ErrorNumber.InvalidArgument;
        }

        // Read the root directory FD sector
        ErrorNumber errno = ReadLsn(_rootDirLsn, out byte[] fdSector);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Failed to read root directory FD at LSN {0}: {1}", _rootDirLsn, errno);

            return errno;
        }

        // Parse the file descriptor
        _rootDirectoryFd = _littleEndian
                               ? Marshal.ByteArrayToStructureLittleEndian<FileDescriptor>(fdSector)
                               : Marshal.ByteArrayToStructureBigEndian<FileDescriptor>(fdSector);

        // Validate it's a directory
        if((_rootDirectoryFd.fd_att & 0x80) == 0)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Root directory FD at LSN {0} is not a directory (attr={1:X2})",
                              _rootDirLsn,
                              _rootDirectoryFd.fd_att);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Root directory FD validated: attr={0:X2}, size={1}, link={2}",
                          _rootDirectoryFd.fd_att,
                          _rootDirectoryFd.fd_fsize,
                          _rootDirectoryFd.fd_link);

        return ErrorNumber.NoError;
    }

    /// <summary>Caches the root directory contents</summary>
    ErrorNumber CacheRootDirectory()
    {
        _rootDirectoryCache = new Dictionary<string, CachedDirectoryEntry>(StringComparer.OrdinalIgnoreCase);

        // Parse the segment list from root directory FD
        List<(uint lsn, uint sectors)> segments = ParseSegmentList(_rootDirectoryFd.fd_seg);

        if(segments.Count == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Root directory has no segments");

            return ErrorNumber.NoError; // Empty directory is valid
        }

        uint fileSize  = _rootDirectoryFd.fd_fsize;
        uint bytesRead = 0;

        foreach((uint lsn, uint sectors) in segments)
        {
            for(uint s = 0; s < sectors && bytesRead < fileSize; s++)
            {
                ErrorNumber errno = ReadLsn(lsn + s, out byte[] sectorData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "Failed to read directory sector at LSN {0}: {1}", lsn + s, errno);

                    continue;
                }

                // Parse directory entries in this sector
                var entriesPerSector = (int)(_lsnSize / 32); // Each entry is 32 bytes

                for(var e = 0; e < entriesPerSector && bytesRead < fileSize; e++)
                {
                    int entryOffset = e * 32;

                    if(entryOffset + 32 > sectorData.Length) break;

                    // Check if entry is used (first byte != 0)
                    if(sectorData[entryOffset] == 0)
                    {
                        bytesRead += 32;

                        continue;
                    }

                    // Parse directory entry
                    DirectoryEntry dirEntry = _littleEndian
                                                  ? Marshal.ByteArrayToStructureLittleEndian<DirectoryEntry>(sectorData,
                                                      entryOffset,
                                                      32)
                                                  : Marshal.ByteArrayToStructureBigEndian<DirectoryEntry>(sectorData,
                                                      entryOffset,
                                                      32);

                    // Extract filename (MSB of last char set indicates end)
                    string filename = ReadRbfFilename(dirEntry.dir_name);

                    if(string.IsNullOrEmpty(filename))
                    {
                        bytesRead += 32;

                        continue;
                    }

                    // Get FD LSN
                    uint fdLsn = LSNToUInt32(dirEntry.dir_fd);

                    if(fdLsn == 0)
                    {
                        bytesRead += 32;

                        continue;
                    }

                    // Read the file descriptor for this entry
                    errno = ReadLsn(fdLsn, out byte[] fdData);

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME, "Failed to read FD for '{0}' at LSN {1}", filename, fdLsn);
                        bytesRead += 32;

                        continue;
                    }

                    FileDescriptor fd = _littleEndian
                                            ? Marshal.ByteArrayToStructureLittleEndian<FileDescriptor>(fdData)
                                            : Marshal.ByteArrayToStructureBigEndian<FileDescriptor>(fdData);

                    var cachedEntry = new CachedDirectoryEntry
                    {
                        Name     = filename,
                        FdLsn    = fdLsn,
                        Fd       = fd,
                        FileSize = fd.fd_fsize
                    };

                    // Use filename as key, handle duplicates
                    if(!_rootDirectoryCache.ContainsKey(filename)) _rootDirectoryCache[filename] = cachedEntry;

                    bytesRead += 32;
                }
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} entries from root directory", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a logical sector by its LSN</summary>
    ErrorNumber ReadLsn(uint lsn, out byte[] data)
    {
        data = null;

        // Calculate the physical sector address
        // LSN 0 is at _idSectorLocation, subsequent LSNs follow
        ulong physicalSector = _partitionStart + _idSectorLocation + lsn;

        // Calculate how many physical sectors we need to read
        uint sectorsToRead = _lsnSize / _sectorSize;

        if(_lsnSize % _sectorSize != 0) sectorsToRead++;

        if(sectorsToRead == 0) sectorsToRead = 1;

        ErrorNumber errno =
            _imagePlugin.ReadSectors(physicalSector, false, sectorsToRead, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        // If LSN size matches physical sector size, return directly
        if(_lsnSize == _sectorSize)
        {
            data = sectorData;

            return ErrorNumber.NoError;
        }

        // Otherwise, extract just the LSN-sized portion
        data = new byte[_lsnSize];
        Array.Copy(sectorData, 0, data, 0, Math.Min(sectorData.Length, (int)_lsnSize));

        return ErrorNumber.NoError;
    }

    /// <summary>Parses the segment list from file descriptor</summary>
    List<(uint lsn, uint sectors)> ParseSegmentList(byte[] segmentData)
    {
        var segments = new List<(uint lsn, uint sectors)>();

        if(segmentData == null || segmentData.Length < 5) return segments;

        // Each segment is 5 bytes: 3-byte LSN + 2-byte size
        int maxSegments = segmentData.Length / 5;

        for(var i = 0; i < maxSegments; i++)
        {
            int offset = i * 5;

            uint lsn;
            uint size;

            if(_littleEndian)
            {
                lsn  = (uint)(segmentData[offset]     | segmentData[offset + 1] << 8 | segmentData[offset + 2] << 16);
                size = (uint)(segmentData[offset + 3] | segmentData[offset + 4] << 8);
            }
            else
            {
                lsn  = (uint)(segmentData[offset] << 16 | segmentData[offset + 1] << 8 | segmentData[offset + 2]);
                size = (uint)(segmentData[offset                             + 3] << 8 | segmentData[offset + 4]);
            }

            // End of segment list when both are 0
            if(lsn == 0 && size == 0) break;

            if(size > 0) segments.Add((lsn, size));
        }

        return segments;
    }

    /// <summary>Populates metadata from the ID sector</summary>
    void PopulateMetadata()
    {
        if(_isOs9000)
        {
            Metadata = new FileSystem
            {
                Type             = FS_TYPE,
                Bootable         = _newIdSector.rid_bootfile > 0,
                ClusterSize      = _newIdSector.rid_blocksize,
                Clusters         = _newIdSector.rid_totblocks,
                CreationDate     = DateHandlers.UnixToDateTime(_newIdSector.rid_ctime),
                ModificationDate = DateHandlers.UnixToDateTime(_newIdSector.rid_mtime),
                VolumeName       = StringHandlers.CToString(_newIdSector.rid_name, _encoding),
                VolumeSerial     = $"{_newIdSector.rid_diskid:X8}"
            };
        }
        else
        {
            Metadata = new FileSystem
            {
                Type         = FS_TYPE,
                Bootable     = LSNToUInt32(_idSector.dd_bt) > 0 && _idSector.dd_bsz > 0,
                ClusterSize  = _idSector.dd_bit * _lsnSize,
                Clusters     = _totalSectors,
                CreationDate = DateHandlers.Os9ToDateTime(_idSector.dd_dat),
                VolumeName   = StringHandlers.CToString(_idSector.dd_nam, _encoding),
                VolumeSerial = $"{_idSector.dd_dsk:X4}"
            };
        }
    }
}