// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class NTFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path;

        if(!normalizedPath.StartsWith("/", StringComparison.Ordinal)) normalizedPath = "/" + normalizedPath;

        uint mftRecordNumber;

        if(normalizedPath == "/")
            mftRecordNumber = (uint)SystemFileNumber.Root;
        else
        {
            ErrorNumber resolveErrno = ResolvePathToMftRecord(normalizedPath, out mftRecordNumber);

            if(resolveErrno != ErrorNumber.NoError) return resolveErrno;
        }

        ErrorNumber errno = ReadMftRecord(mftRecordNumber, out byte[] recordData);

        if(errno != ErrorNumber.NoError) return errno;

        MftRecord header =
            Marshal.ByteArrayToStructureLittleEndian<MftRecord>(recordData, 0, Marshal.SizeOf<MftRecord>());

        if(header.magic != NtfsRecordMagic.File)
        {
            AaruLogging.Debug(MODULE_NAME, "MFT record {0} has invalid magic", mftRecordNumber);

            return ErrorNumber.InvalidArgument;
        }

        stat = new FileEntryInfo
        {
            Inode     = mftRecordNumber,
            Links     = header.link_count,
            BlockSize = _bytesPerCluster,
            Attributes = header.flags.HasFlag(MftRecordFlags.IsDirectory)
                             ? FileAttributes.Directory
                             : FileAttributes.File
        };

        // Walk attributes to find $STANDARD_INFORMATION, $FILE_NAME, and $DATA
        int offset = header.attrs_offset;

        FileAttributeFlags ntfsAttributes    = 0;
        var                foundStdInfo      = false;
        var                foundFileName     = false;
        long               dataSize          = 0;
        long               dataAllocatedSize = 0;
        var                foundData         = false;
        FileNameAttribute  bestFileName      = default;
        FileNameNamespace  bestNamespace     = FileNameNamespace.Dos;

        while(offset + 4 <= recordData.Length)
        {
            var attrType = (AttributeType)BitConverter.ToUInt32(recordData, offset);

            if(attrType == AttributeType.End || attrType == AttributeType.Unused) break;

            var attrLength = BitConverter.ToUInt32(recordData, offset + 4);

            if(attrLength == 0 || offset + attrLength > recordData.Length) break;

            byte nonResident = recordData[offset + 8];

            switch(attrType)
            {
                case AttributeType.StandardInformation when nonResident == 0:
                {
                    var valueOffset = BitConverter.ToUInt16(recordData, offset + 0x14);
                    var valueLength = BitConverter.ToUInt32(recordData, offset + 0x10);

                    int valueStart = offset + valueOffset;

                    if(valueStart + valueLength <= recordData.Length)
                    {
                        if(valueLength >= Marshal.SizeOf<StandardInformationV3>())
                        {
                            StandardInformationV3 stdInfo =
                                Marshal.ByteArrayToStructureLittleEndian<StandardInformationV3>(recordData,
                                    valueStart,
                                    Marshal.SizeOf<StandardInformationV3>());

                            ntfsAttributes = stdInfo.file_attributes;

                            SetTimestamps(stat,
                                          stdInfo.creation_time,
                                          stdInfo.last_data_change_time,
                                          stdInfo.last_mft_change_time,
                                          stdInfo.last_access_time);

                            stat.UID = stdInfo.owner_id;
                        }
                        else if(valueLength >= (uint)Marshal.SizeOf<StandardInformationV1>())
                        {
                            StandardInformationV1 stdInfo =
                                Marshal.ByteArrayToStructureLittleEndian<StandardInformationV1>(recordData,
                                    valueStart,
                                    Marshal.SizeOf<StandardInformationV1>());

                            ntfsAttributes = stdInfo.file_attributes;

                            SetTimestamps(stat,
                                          stdInfo.creation_time,
                                          stdInfo.last_data_change_time,
                                          stdInfo.last_mft_change_time,
                                          stdInfo.last_access_time);
                        }

                        foundStdInfo = true;
                    }

                    break;
                }
                case AttributeType.FileName when nonResident == 0:
                {
                    var valueOffset = BitConverter.ToUInt16(recordData, offset + 0x14);
                    var valueLength = BitConverter.ToUInt32(recordData, offset + 0x10);

                    int valueStart = offset + valueOffset;

                    if(valueStart + Marshal.SizeOf<FileNameAttribute>() <= recordData.Length)
                    {
                        FileNameAttribute fnAttr =
                            Marshal.ByteArrayToStructureLittleEndian<FileNameAttribute>(recordData,
                                valueStart,
                                Marshal.SizeOf<FileNameAttribute>());

                        // Prefer Win32 or Win32AndDos over Posix over Dos
                        if(!foundFileName                                                                    ||
                           fnAttr.file_name_type is FileNameNamespace.Win32 or FileNameNamespace.Win32AndDos ||
                           fnAttr.file_name_type == FileNameNamespace.Posix && bestNamespace == FileNameNamespace.Dos)
                        {
                            bestFileName  = fnAttr;
                            bestNamespace = fnAttr.file_name_type;
                            foundFileName = true;
                        }
                    }

                    break;
                }
                case AttributeType.Data:
                {
                    // Only process the unnamed (default) $DATA attribute
                    byte nameLength = recordData[offset + 9];

                    if(nameLength == 0)
                    {
                        if(nonResident == 0)
                        {
                            // Resident $DATA
                            var valueLength = BitConverter.ToUInt32(recordData, offset + 0x10);
                            dataSize          = valueLength;
                            dataAllocatedSize = valueLength;
                        }
                        else
                        {
                            // Non-resident $DATA
                            NonResidentAttributeRecord nrAttr =
                                Marshal.ByteArrayToStructureLittleEndian<NonResidentAttributeRecord>(recordData,
                                    offset,
                                    Marshal.SizeOf<NonResidentAttributeRecord>());

                            dataSize          = (long)nrAttr.data_size;
                            dataAllocatedSize = (long)nrAttr.allocated_size;
                        }

                        foundData = true;
                    }

                    break;
                }
            }

            offset += (int)attrLength;
        }

        // Set file size from $DATA or $FILE_NAME
        if(foundData)
        {
            stat.Length = dataSize;
            stat.Blocks = (dataAllocatedSize + _bytesPerCluster - 1) / _bytesPerCluster;
        }
        else if(foundFileName)
        {
            stat.Length = (long)bestFileName.data_size;
            stat.Blocks = (long)((bestFileName.allocated_size + _bytesPerCluster - 1) / _bytesPerCluster);
        }

        // Map NTFS FileAttributeFlags to Aaru FileAttributes
        if(ntfsAttributes.HasFlag(FileAttributeFlags.ReadOnly)) stat.Attributes     |= FileAttributes.ReadOnly;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.Hidden)) stat.Attributes       |= FileAttributes.Hidden;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.System)) stat.Attributes       |= FileAttributes.System;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.Archive)) stat.Attributes      |= FileAttributes.Archive;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.Device)) stat.Attributes       |= FileAttributes.Device;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.Temporary)) stat.Attributes    |= FileAttributes.Temporary;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.SparseFile)) stat.Attributes   |= FileAttributes.Sparse;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.ReparsePoint)) stat.Attributes |= FileAttributes.ReparsePoint;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.Compressed)) stat.Attributes   |= FileAttributes.Compressed;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.Offline)) stat.Attributes      |= FileAttributes.Offline;
        if(ntfsAttributes.HasFlag(FileAttributeFlags.Encrypted)) stat.Attributes    |= FileAttributes.Encrypted;

        if(ntfsAttributes.HasFlag(FileAttributeFlags.NotContentIndexed)) stat.Attributes |= FileAttributes.NotIndexed;

        // Synthesize POSIX mode from NTFS attributes
        // NTFS doesn't store POSIX mode natively, but we can approximate:
        //   directories get 0755, files get 0644, read-only files get 0444
        uint mode;

        if(header.flags.HasFlag(MftRecordFlags.IsDirectory))
        {
            // S_IFDIR | rwxr-xr-x
            mode = 0x4000 | 0755;
        }
        else if(ntfsAttributes.HasFlag(FileAttributeFlags.ReparsePoint))
        {
            // S_IFLNK | rwxrwxrwx (reparse points are often symlinks)
            mode            =  0xA000 | 0777;
            stat.Attributes |= FileAttributes.Symlink;
        }
        else if(ntfsAttributes.HasFlag(FileAttributeFlags.Device))
        {
            // S_IFCHR | rw-rw-rw-
            mode = 0x2000 | 0666;
        }
        else
        {
            // S_IFREG
            mode = 0x8000;

            if(ntfsAttributes.HasFlag(FileAttributeFlags.ReadOnly))
                mode |= 0444; // r--r--r--
            else
                mode |= 0644; // rw-r--r--
        }

        stat.Mode = mode;

        // Device number: use volume serial number as the device number
        stat.DeviceNo = _bpb.serial_no;

        return ErrorNumber.NoError;
    }

    /// <summary>Sets timestamps on a <see cref="FileEntryInfo" /> from NTFS FILETIME values.</summary>
    /// <param name="info">The file entry info to populate.</param>
    /// <param name="creationTime">NTFS creation time (FILETIME).</param>
    /// <param name="lastWriteTime">NTFS last data modification time (FILETIME).</param>
    /// <param name="lastMftChangeTime">NTFS last MFT record change time (FILETIME).</param>
    /// <param name="lastAccessTime">NTFS last access time (FILETIME).</param>
    static void SetTimestamps(FileEntryInfo info, long creationTime, long lastWriteTime, long lastMftChangeTime,
                              long          lastAccessTime)
    {
        if(creationTime > 0) info.CreationTimeUtc = DateTime.FromFileTimeUtc(creationTime);

        if(lastWriteTime > 0) info.LastWriteTimeUtc = DateTime.FromFileTimeUtc(lastWriteTime);

        if(lastMftChangeTime > 0) info.StatusChangeTimeUtc = DateTime.FromFileTimeUtc(lastMftChangeTime);

        if(lastAccessTime > 0) info.AccessTimeUtc = DateTime.FromFileTimeUtc(lastAccessTime);
    }

    /// <summary>Resolves a normalized path to its MFT record number by traversing the directory tree.</summary>
    /// <param name="normalizedPath">Absolute path starting with '/'.</param>
    /// <param name="mftRecordNumber">Output MFT record number.</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ResolvePathToMftRecord(string normalizedPath, out uint mftRecordNumber)
    {
        mftRecordNumber = 0;

        string cutPath = normalizedPath[1..]; // Remove leading '/'

        string[] pieces = cutPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pieces.Length == 0) return ErrorNumber.NoSuchFile;

        // Start from root directory
        Dictionary<string, ulong> currentDirectory = _rootDirectoryCache;

        for(var p = 0; p < pieces.Length; p++)
        {
            string component = pieces[p];

            if(!currentDirectory.TryGetValue(component, out ulong mftRef)) return ErrorNumber.NoSuchFile;

            var recordNum = (uint)(mftRef & 0x0000FFFFFFFFFFFF);

            // If this is the last component, return it
            if(p == pieces.Length - 1)
            {
                mftRecordNumber = recordNum;

                return ErrorNumber.NoError;
            }

            // Not the last component — must be a directory
            ErrorNumber errno = ReadDirectoryEntries(recordNum, out Dictionary<string, ulong> dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentDirectory = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }
}