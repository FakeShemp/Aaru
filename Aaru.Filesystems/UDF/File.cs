// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Universal Disk Format plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Marshal = Aaru.Helpers.Marshal;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

public sealed partial class UDF
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetFileEntry(path, out FileEntry fileEntry);

        if(errno != ErrorNumber.NoError) return errno;

        stat = new FileEntryInfo
        {
            Attributes          = new FileAttributes(),
            Blocks              = (long)fileEntry.logicalBlocksRecorded,
            BlockSize           = _sectorSize,
            Length              = (long)fileEntry.informationLength,
            Links               = fileEntry.fileLinkCount,
            Inode               = fileEntry.uniqueId,
            UID                 = fileEntry.uid,
            GID                 = fileEntry.gid,
            Mode                = ConvertPermissionsToMode(fileEntry.permissions),
            AccessTimeUtc       = EcmaToDateTime(fileEntry.accessTime),
            LastWriteTimeUtc    = EcmaToDateTime(fileEntry.modificationTime),
            StatusChangeTimeUtc = EcmaToDateTime(fileEntry.attributeTime)
        };

        // Set file attributes based on file type and flags
        if(fileEntry.icbTag.fileType == FileType.Directory) stat.Attributes |= FileAttributes.Directory;

        if(fileEntry.icbTag.flags.HasFlag(FileFlags.System)) stat.Attributes |= FileAttributes.System;

        if(fileEntry.icbTag.flags.HasFlag(FileFlags.Archive)) stat.Attributes |= FileAttributes.Archive;

        // Check for MacVolumeInfo extended attribute to get additional timestamps
        if(fileEntry.lengthOfExtendedAttributes <= 0) return ErrorNumber.NoError;

        errno = GetFileEntryBuffer(path, out byte[] feBuffer);

        if(errno != ErrorNumber.NoError) return ErrorNumber.NoError;

        MacVolumeInfo? macVolumeInfo = GetMacVolumeInfo(feBuffer, fileEntry);

        if(!macVolumeInfo.HasValue) return ErrorNumber.NoError;

        stat.LastWriteTimeUtc = EcmaToDateTime(macVolumeInfo.Value.lastModificationDate);
        stat.BackupTimeUtc    = EcmaToDateTime(macVolumeInfo.Value.lastBackupDate);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = new FileAttributes();

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetFileEntry(path, out FileEntry fileEntry);

        if(errno != ErrorNumber.NoError) return errno;

        // Set file attributes based on file type
        switch(fileEntry.icbTag.fileType)
        {
            case FileType.Directory:
                attributes |= FileAttributes.Directory;

                break;
            case FileType.SymbolicLink:
                attributes |= FileAttributes.Symlink;

                break;
            case FileType.BlockDevice:
                attributes |= FileAttributes.BlockDevice;

                break;
            case FileType.CharacterDevice:
                attributes |= FileAttributes.CharDevice;

                break;
            case FileType.Fifo:
                attributes |= FileAttributes.Pipe;

                break;
            case FileType.Socket:
                attributes |= FileAttributes.Socket;

                break;
        }

        // Set attributes based on flags
        if(fileEntry.icbTag.flags.HasFlag(FileFlags.System)) attributes |= FileAttributes.System;

        if(fileEntry.icbTag.flags.HasFlag(FileFlags.Archive)) attributes |= FileAttributes.Archive;

        // Check for hidden flag in file characteristics (from directory entry)
        // We need to check if the file was marked as hidden in its directory entry
        errno = GetFileCharacteristics(path, out FileCharacteristics characteristics);

        if(errno == ErrorNumber.NoError && characteristics.HasFlag(FileCharacteristics.Hidden))
            attributes |= FileAttributes.Hidden;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = GetFileEntry(path, out FileEntry fileEntry);

        if(errno != ErrorNumber.NoError) return errno;

        // Check if this is a symbolic link
        if(fileEntry.icbTag.fileType != FileType.SymbolicLink) return ErrorNumber.InvalidArgument;

        // Read the file entry buffer to get the symlink data
        errno = GetFileEntryBuffer(path, out byte[] feBuffer);

        if(errno != ErrorNumber.NoError) return errno;

        // Read the symlink data based on allocation descriptor type
        var adType = (byte)((ushort)fileEntry.icbTag.flags & 0x07);

        errno = ReadFileData(fileEntry, feBuffer, adType, out byte[] linkData);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse the symbolic link path components per ECMA-167 4/14.16
        // The symlink data contains path components
        dest = ParseSymbolicLinkData(linkData);

        return string.IsNullOrEmpty(dest) ? ErrorNumber.InvalidArgument : ErrorNumber.NoError;
    }

    /// <summary>
    ///     Gets the FileCharacteristics for a file from its parent directory entry
    /// </summary>
    ErrorNumber GetFileCharacteristics(string path, out FileCharacteristics characteristics)
    {
        characteristics = 0;

        // Root directory has no parent entry
        if(string.IsNullOrWhiteSpace(path) || path == "/") return ErrorNumber.NoError;

        string   cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;
        string[] pieces  = cutPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        string parentPath = pieces.Length > 1 ? string.Join("/", pieces[..^1]) : "";
        string fileName   = pieces[^1];

        ErrorNumber errno = GetDirectoryEntries(parentPath, out Dictionary<string, UdfDirectoryEntry> parentEntries);

        if(errno != ErrorNumber.NoError) return errno;

        foreach(KeyValuePair<string, UdfDirectoryEntry> kvp in parentEntries)
        {
            if(!kvp.Key.Equals(fileName, StringComparison.OrdinalIgnoreCase)) continue;

            characteristics = kvp.Value.FileCharacteristics;

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>
    ///     Gets MacVolumeInfo from the extended attributes if present
    /// </summary>
    static MacVolumeInfo? GetMacVolumeInfo(byte[] feBuffer, in FileEntry fileEntry)
    {
        const int fileEntryFixedSize = 176;
        int       eaOffset           = fileEntryFixedSize;
        int       eaEnd              = fileEntryFixedSize + (int)fileEntry.lengthOfExtendedAttributes;

        // First, check for Extended Attribute Header Descriptor
        if(eaEnd - eaOffset >= 24)
        {
            var tagId = (TagIdentifier)BitConverter.ToUInt16(feBuffer, eaOffset);

            if(tagId == TagIdentifier.ExtendedAttributeHeaderDescriptor) eaOffset += 24; // Skip the header descriptor
        }

        while(eaOffset + 12 <= eaEnd)
        {
            GenericExtendedAttributeHeader eaHeader =
                Marshal.ByteArrayToStructureLittleEndian<GenericExtendedAttributeHeader>(feBuffer, eaOffset, 12);

            if(eaHeader.attributeLength == 0) break;

            if(eaHeader.attributeType == 2048) // EA_TYPE_IMPLEMENTATION
            {
                int headerSize = System.Runtime.InteropServices.Marshal.SizeOf<ImplementationUseExtendedAttribute>();

                if(eaOffset + headerSize <= feBuffer.Length)
                {
                    ImplementationUseExtendedAttribute iuea =
                        Marshal.ByteArrayToStructureLittleEndian<ImplementationUseExtendedAttribute>(feBuffer,
                            eaOffset,
                            headerSize);

                    if(CompareIdentifier(iuea.implementationIdentifier.identifier, _mac_VolumeInfo))
                    {
                        int macVolumeInfoSize = System.Runtime.InteropServices.Marshal.SizeOf<MacVolumeInfo>();
                        int dataOffset        = eaOffset + headerSize;

                        if(dataOffset + macVolumeInfoSize <= feBuffer.Length)
                        {
                            return Marshal.ByteArrayToStructureLittleEndian<MacVolumeInfo>(feBuffer,
                                dataOffset,
                                macVolumeInfoSize);
                        }
                    }
                }
            }

            eaOffset += (int)eaHeader.attributeLength;
        }

        return null;
    }

    /// <summary>
    ///     Gets the FileEntry for a given path
    /// </summary>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="fileEntry">The FileEntry if found</param>
    /// <returns>Error number</returns>
    ErrorNumber GetFileEntry(string path, out FileEntry fileEntry)
    {
        fileEntry = default(FileEntry);

        // Root directory
        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            ulong rootSector = _partitionStartingLocation + _rootDirectoryIcb.extentLocation.logicalBlockNumber;

            if(_imagePlugin.ReadSector(rootSector, false, out byte[] buffer, out _) != ErrorNumber.NoError)
                return ErrorNumber.InvalidArgument;

            fileEntry = Marshal.ByteArrayToStructureLittleEndian<FileEntry>(buffer);

            return fileEntry.tag.tagIdentifier == TagIdentifier.FileEntry
                       ? ErrorNumber.NoError
                       : ErrorNumber.InvalidArgument;
        }

        string   cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;
        string[] pieces  = cutPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Traverse directories to find the parent directory
        string parentPath = pieces.Length > 1 ? string.Join("/", pieces[..^1]) : "";
        string fileName   = pieces[^1];

        ErrorNumber errno = GetDirectoryEntries(parentPath, out Dictionary<string, UdfDirectoryEntry> parentEntries);

        if(errno != ErrorNumber.NoError) return errno;

        // Find the entry in the parent directory (case-insensitive)
        UdfDirectoryEntry entry =
            (from kvp in parentEntries
             where kvp.Key.Equals(fileName, StringComparison.OrdinalIgnoreCase)
             select kvp.Value).FirstOrDefault();

        if(entry == null) return ErrorNumber.NoSuchFile;

        // Read the FileEntry
        ulong fileEntrySector = _partitionStartingLocation + entry.Icb.extentLocation.logicalBlockNumber;

        if(_imagePlugin.ReadSector(fileEntrySector, false, out byte[] feBuffer, out _) != ErrorNumber.NoError)
            return ErrorNumber.InvalidArgument;

        fileEntry = Marshal.ByteArrayToStructureLittleEndian<FileEntry>(feBuffer);

        return fileEntry.tag.tagIdentifier == TagIdentifier.FileEntry
                   ? ErrorNumber.NoError
                   : ErrorNumber.InvalidArgument;
    }

    /// <summary>
    ///     Converts UDF permissions to POSIX mode
    /// </summary>
    static uint ConvertPermissionsToMode(Permissions permissions)
    {
        uint mode = 0;

        // Owner permissions
        if(permissions.HasFlag(Permissions.OwnerRead)) mode |= 0x100; // S_IRUSR

        if(permissions.HasFlag(Permissions.OwnerWrite)) mode |= 0x080; // S_IWUSR

        if(permissions.HasFlag(Permissions.OwnerExecute)) mode |= 0x040; // S_IXUSR

        // Group permissions
        if(permissions.HasFlag(Permissions.GroupRead)) mode |= 0x020; // S_IRGRP

        if(permissions.HasFlag(Permissions.GroupWrite)) mode |= 0x010; // S_IWGRP

        if(permissions.HasFlag(Permissions.GroupExecute)) mode |= 0x008; // S_IXGRP

        // Other permissions
        if(permissions.HasFlag(Permissions.OtherRead)) mode |= 0x004; // S_IROTH

        if(permissions.HasFlag(Permissions.OtherWrite)) mode |= 0x002; // S_IWOTH

        if(permissions.HasFlag(Permissions.OtherExecute)) mode |= 0x001; // S_IXOTH

        return mode;
    }

    /// <summary>
    ///     Parses symbolic link data per ECMA-167 4/14.16
    /// </summary>
    /// <param name="linkData">The raw symbolic link data</param>
    /// <returns>The resolved path string</returns>
    static string ParseSymbolicLinkData(byte[] linkData)
    {
        if(linkData == null || linkData.Length == 0) return null;

        var path   = new StringBuilder();
        var offset = 0;

        while(offset < linkData.Length)
        {
            // Path component format per ECMA-167 4/14.16.1:
            // - 1 byte: Component Type
            // - 1 byte: Length of Component Identifier (L_CI)
            // - 2 bytes: Component File Version Number
            // - L_CI bytes: Component Identifier

            if(offset + 4 > linkData.Length) break;

            byte componentType = linkData[offset];
            byte identifierLen = linkData[offset + 1];

            // ushort fileVersion = BitConverter.ToUInt16(linkData, offset + 2); // Usually ignored

            offset += 4;

            switch(componentType)
            {
                case 1: // Root directory
                    path.Clear();
                    path.Append('/');

                    break;

                case 2: // Current directory (.)
                    // Skip, don't add anything
                    break;

                case 3: // Parent directory (..)
                    if(path.Length > 0 && path[^1] != '/') path.Append('/');

                    path.Append("..");

                    break;

                case 4: // Path component name
                case 5: // Path component name (with d-string encoding)
                    if(identifierLen > 0 && offset + identifierLen <= linkData.Length)
                    {
                        if(path.Length > 0 && path[^1] != '/') path.Append('/');

                        var identifierBytes = new byte[identifierLen];
                        Array.Copy(linkData, offset, identifierBytes, 0, identifierLen);

                        string componentName = StringHandlers.DecompressUnicode(identifierBytes);

                        if(!string.IsNullOrEmpty(componentName)) path.Append(componentName);
                    }

                    break;
            }

            offset += identifierLen;
        }

        return path.Length > 0 ? path.ToString() : null;
    }
}