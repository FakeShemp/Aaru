// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class ProDOSPlugin
{
    /// <inheritdoc />
    public ErrorNumber GetAttributes(string path, out FileAttributes attributes)
    {
        attributes = new FileAttributes();

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Get the entry for this path
        ErrorNumber errno = GetEntryForPath(path, out CachedEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        // Directory
        if(entry.IsDirectory)
        {
            attributes = FileAttributes.Directory;

            return ErrorNumber.NoError;
        }

        // File attributes
        attributes = FileAttributes.File;

        // ProDOS access flags
        if((entry.Access & READ_ATTRIBUTE) == 0) attributes |= FileAttributes.Hidden;

        if((entry.Access & WRITE_ATTRIBUTE) == 0) attributes |= FileAttributes.ReadOnly;

        // Backup needed flag
        if((entry.Access & BACKUP_ATTRIBUTE) != 0) attributes |= FileAttributes.Archive;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Handle root directory specially
        if(string.IsNullOrEmpty(path) || path == "/" || path == ".")
        {
            stat = new FileEntryInfo
            {
                Attributes   = FileAttributes.Directory,
                BlockSize    = 512,
                Blocks       = 4, // Volume directory is always 4 blocks
                CreationTime = _creationTime,
                Inode        = 2, // Root directory starts at block 2
                Links        = 1,
                Mode         = 0x16D, // drwxrw-r-x
                DeviceNo     = 0,
                GID          = 0,
                UID          = 0,
                Length       = 4 * 512
            };

            return ErrorNumber.NoError;
        }

        // Get the entry for this path
        ErrorNumber errno = GetEntryForPath(path, out CachedEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        stat = new FileEntryInfo
        {
            BlockSize     = 512,
            CreationTime  = entry.CreationTime,
            LastWriteTime = entry.ModificationTime,
            Links         = 1,
            DeviceNo      = 0,
            GID           = 0,
            UID           = 0
        };

        // Directory
        if(entry.IsDirectory)
        {
            stat.Attributes = FileAttributes.Directory;
            stat.Blocks     = entry.BlocksUsed;
            stat.Inode      = entry.KeyBlock;
            stat.Length     = entry.BlocksUsed * 512;
            stat.Mode       = 0x16D; // drwxrw-r-x

            return ErrorNumber.NoError;
        }

        // File
        stat.Inode = entry.KeyBlock;

        // Set attributes
        stat.Attributes = FileAttributes.File;

        if((entry.Access & READ_ATTRIBUTE) == 0) stat.Attributes |= FileAttributes.Hidden;

        if((entry.Access & WRITE_ATTRIBUTE) == 0) stat.Attributes |= FileAttributes.ReadOnly;

        if((entry.Access & BACKUP_ATTRIBUTE) != 0) stat.Attributes |= FileAttributes.Archive;

        // Calculate mode from access flags
        uint mode = 0x8000; // Regular file

        if((entry.Access & READ_ATTRIBUTE) != 0) mode |= 0x124; // r--r--r--

        if((entry.Access & WRITE_ATTRIBUTE) != 0) mode |= 0x92; // -w--w--w-

        stat.Mode = mode;

        // For extended files (with resource fork), read the extended key block to get data fork size
        if(entry.StorageType == EXTENDED_FILE_TYPE)
        {
            errno = ReadBlock(entry.KeyBlock, out byte[] extBlock);

            if(errno != ErrorNumber.NoError) return errno;

            ExtendedKeyBlock extKeyBlock = Marshal.ByteArrayToStructureLittleEndian<ExtendedKeyBlock>(extBlock);

            // Data fork size (per user requirement: data fork mandates file size)
            var dataForkEof = (uint)(extKeyBlock.data_fork.eof[0]      |
                                     extKeyBlock.data_fork.eof[1] << 8 |
                                     extKeyBlock.data_fork.eof[2] << 16);

            stat.Length = dataForkEof;
            stat.Blocks = extKeyBlock.data_fork.blocks_used;
        }
        else
        {
            // Non-extended file: use entry's EOF and blocks_used
            stat.Length = entry.Eof;
            stat.Blocks = entry.BlocksUsed;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Gets a cached entry for the given path</summary>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="entry">Output cached entry</param>
    /// <returns>Error number</returns>
    ErrorNumber GetEntryForPath(string path, out CachedEntry entry)
    {
        entry = null;

        if(string.IsNullOrEmpty(path) || path == "/" || path == ".") return ErrorNumber.IsDirectory;

        string[] pathComponents = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.IsDirectory;

        // Start from root directory cache
        Dictionary<string, CachedEntry> currentDir = _rootDirectoryCache;

        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            if(component is "." or "..") continue;

            if(!currentDir.TryGetValue(component, out CachedEntry currentEntry)) return ErrorNumber.NoSuchFile;

            // Last component - return this entry
            if(i == pathComponents.Length - 1)
            {
                entry = currentEntry;

                return ErrorNumber.NoError;
            }

            // Intermediate component must be a directory
            if(!currentEntry.IsDirectory) return ErrorNumber.NotDirectory;

            // Read subdirectory contents
            ErrorNumber errno =
                ReadDirectoryContents(currentEntry.KeyBlock, false, out Dictionary<string, CachedEntry> subDir);

            if(errno != ErrorNumber.NoError) return errno;

            currentDir = subDir;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Applies GS/OS case bits to a filename</summary>
    static string ApplyCaseBits(string name, ushort caseBits)
    {
        if((caseBits & 0x8000) == 0) return name;

        char[] chars = name.ToCharArray();
        var    bit   = 0x4000;

        for(var i = 0; i < chars.Length && bit > 0; i++)
        {
            if((caseBits & bit) != 0) chars[i] = char.ToLower(chars[i]);

            bit >>= 1;
        }

        return new string(chars);
    }
}