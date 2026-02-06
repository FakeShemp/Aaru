// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : SmartFileSystem plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class SFS
{
    /// <summary>Amiga epoch: January 1, 1978</summary>
    static readonly DateTime _amigaEpoch = new(1978, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Use Stat to get the file information
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        attributes = stat.Attributes;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read root object to get its metadata
            ErrorNumber errno = FindObjectNode(ROOTNODE, out uint rootBlock);

            if(errno != ErrorNumber.NoError) return errno;

            errno = ReadBlock(rootBlock, out byte[] rootData);

            if(errno != ErrorNumber.NoError) return errno;

            errno = FindObjectInContainer(rootData, ROOTNODE, out int rootOffset);

            if(errno != ErrorNumber.NoError) return errno;

            return StatFromObjectData(rootData, rootOffset, ROOTNODE, out stat);
        }

        // Remove leading slash and split path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start traversal from root directory cache
        Dictionary<string, uint> currentDirectory = _rootDirectoryCache;
        uint                     targetNode       = 0;

        // Traverse all path components
        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            // Find the component in current directory (handle case sensitivity)
            string foundKey = null;

            foreach(string key in currentDirectory.Keys)
            {
                if(string.Equals(key,
                                 component,
                                 _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                {
                    foundKey = key;

                    break;
                }
            }

            if(foundKey == null) return ErrorNumber.NoSuchFile;

            targetNode = currentDirectory[foundKey];

            // If this is the last component, we found our target
            if(i == pathComponents.Length - 1) break;

            // Not the last component - read directory contents for next iteration
            ErrorNumber errno = ReadDirectoryContents(targetNode, out Dictionary<string, uint> childEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentDirectory = childEntries;
        }

        // Read the target object's metadata
        ErrorNumber findErr = FindObjectNode(targetNode, out uint objectBlock);

        if(findErr != ErrorNumber.NoError) return findErr;

        findErr = ReadBlock(objectBlock, out byte[] objectData);

        if(findErr != ErrorNumber.NoError) return findErr;

        findErr = FindObjectInContainer(objectData, targetNode, out int objectOffset);

        if(findErr != ErrorNumber.NoError) return findErr;

        return StatFromObjectData(objectData, objectOffset, targetNode, out stat);
    }

    /// <summary>Creates a FileEntryInfo from raw object data in an ObjectContainer</summary>
    /// <param name="objectData">The ObjectContainer block data</param>
    /// <param name="objectOffset">Offset to the object within the container</param>
    /// <param name="objectNode">The object's node number</param>
    /// <param name="stat">Output file entry information</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber StatFromObjectData(byte[] objectData, int objectOffset, uint objectNode, out FileEntryInfo stat)
    {
        stat = null;

        // Object structure (from objects.h):
        // owneruid (2) + ownergid (2) + objectnode (4) + protection (4) +
        // data/hashtable (4) + size/firstdirblock (4) + datemodified (4) + bits (1)
        // Total fixed size: 25 bytes, followed by name and comment

        if(objectOffset + OBJECT_SIZE > objectData.Length) return ErrorNumber.InvalidArgument;

        var ownerUid     = BigEndianBitConverter.ToUInt16(objectData, objectOffset);
        var ownerGid     = BigEndianBitConverter.ToUInt16(objectData, objectOffset + 2);
        var protection   = BigEndianBitConverter.ToUInt32(objectData, objectOffset + 8);
        var dataOrHash   = BigEndianBitConverter.ToUInt32(objectData, objectOffset + 12);
        var sizeOrDir    = BigEndianBitConverter.ToUInt32(objectData, objectOffset + 16);
        var dateModified = BigEndianBitConverter.ToUInt32(objectData, objectOffset + 20);
        var bits         = (ObjectBits)objectData[objectOffset + 24];

        // Determine file attributes
        FileAttributes attributes = FileAttributes.None;

        if((bits & ObjectBits.Directory) != 0)
            attributes |= FileAttributes.Directory;
        else
            attributes |= FileAttributes.File;

        if((bits & ObjectBits.Hidden) != 0) attributes |= FileAttributes.Hidden;

        if((bits & ObjectBits.Link) != 0 && (bits & ObjectBits.HardLink) == 0) attributes |= FileAttributes.Symlink;

        // SFS protection bits: opposite of AmigaDOS
        // Default is 0x0000000F (R, W, E, D set)
        // If write bit (bit 1) is NOT set, file is read-only
        if((protection & 0x02) == 0) attributes |= FileAttributes.ReadOnly;

        // Calculate file size and blocks
        long length = 0;
        long blocks = 0;

        if((bits & ObjectBits.Directory) == 0)
        {
            length = sizeOrDir;
            blocks = (length + _blockSize - 1) / _blockSize;
        }

        // Convert SFS timestamp (seconds since 1-1-1978) to DateTime
        DateTime? lastWriteTimeUtc = _amigaEpoch.AddSeconds(dateModified);

        stat = new FileEntryInfo
        {
            Attributes       = attributes,
            Inode            = objectNode,
            Length           = length,
            Blocks           = blocks,
            BlockSize        = _blockSize,
            Links            = 1, // SFS doesn't track hard link count in the object
            UID              = ownerUid,
            GID              = ownerGid,
            Mode             = protection,
            LastWriteTimeUtc = lastWriteTimeUtc
        };

        return ErrorNumber.NoError;
    }
}