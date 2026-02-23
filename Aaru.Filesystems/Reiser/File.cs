// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser filesystem plugin
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class Reiser
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Stat: path='{0}'", path);

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath is "/") return ReadStatData(REISERFS_ROOT_PARENT_OBJECTID, REISERFS_ROOT_OBJECTID, out stat);

        // Split path into components
        string stripped = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        string[] components = stripped.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(components.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, (uint dirId, uint objectId)> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < components.Length; i++)
        {
            string component = components[i];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out (uint dirId, uint objectId) target))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Last component — stat this object
            if(i == components.Length - 1) return ReadStatData(target.dirId, target.objectId, out stat);

            // Intermediate component — must be a directory, descend into it
            ErrorNumber errno = ReadObjectMode(target.dirId, target.objectId, out ushort mode);

            if(errno != ErrorNumber.NoError) return errno;

            if((mode & S_IFMT) != S_IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            errno = ReadDirectoryEntries(target.dirId,
                                         target.objectId,
                                         out Dictionary<string, (uint dirId, uint objectId)> subEntries);

            if(errno != ErrorNumber.NoError) return errno;

            // Filter . and .. for traversal
            var filtered = new Dictionary<string, (uint dirId, uint objectId)>();

            foreach(KeyValuePair<string, (uint dirId, uint objectId)> entry in subEntries)
            {
                if(entry.Key is "." or "..") continue;

                filtered[entry.Key] = entry.Value;
            }

            currentEntries = filtered;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Reads the stat data for an object and returns a populated FileEntryInfo</summary>
    /// <param name="dirId">Directory (packing locality) id</param>
    /// <param name="objectId">Object id</param>
    /// <param name="stat">The populated file entry information</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadStatData(uint dirId, uint objectId, out FileEntryInfo stat)
    {
        stat = null;

        ErrorNumber errno = SearchByKey(dirId, objectId, 0, TYPE_STAT_DATA, out byte[] leaf, out int index);

        if(errno != ErrorNumber.NoError) return errno;

        if(index < 0) return ErrorNumber.NoSuchFile;

        ItemHead ih  = ReadItemHead(leaf, BLKH_SIZE + index * IH_SIZE);
        int      ver = GetItemKeyVersion(ih.ih_version);

        if(ih.ih_item_location + ih.ih_item_len > leaf.Length) return ErrorNumber.InvalidArgument;

        stat = new FileEntryInfo
        {
            Attributes = FileAttributes.None,
            BlockSize  = _blockSize,
            Inode      = objectId
        };

        if(ver == KEY_FORMAT_3_6 && ih.ih_item_len >= Marshal.SizeOf<StatDataV2>())
        {
            StatDataV2 sd =
                Marshal.ByteArrayToStructureLittleEndian<StatDataV2>(leaf,
                                                                     ih.ih_item_location,
                                                                     Marshal.SizeOf<StatDataV2>());

            stat.Mode   = (uint)(sd.sd_mode & 0x0FFF);
            stat.Length = (long)sd.sd_size;
            stat.Links  = sd.sd_nlink;
            stat.UID    = sd.sd_uid;
            stat.GID    = sd.sd_gid;
            stat.Blocks = sd.sd_blocks;

            stat.AccessTimeUtc       = DateHandlers.UnixUnsignedToDateTime(sd.sd_atime);
            stat.LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime(sd.sd_mtime);
            stat.StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime(sd.sd_ctime);

            stat.Attributes = ModeToAttributes(sd.sd_mode);

            // Device files
            var fileType = (ushort)(sd.sd_mode & S_IFMT);

            if(fileType is S_IFCHR or S_IFBLK) stat.DeviceNo = sd.sd_rdev_or_generation;
        }
        else
        {
            StatDataV1 sd =
                Marshal.ByteArrayToStructureLittleEndian<StatDataV1>(leaf,
                                                                     ih.ih_item_location,
                                                                     Marshal.SizeOf<StatDataV1>());

            stat.Mode   = (uint)(sd.sd_mode & 0x0FFF);
            stat.Length = sd.sd_size;
            stat.Links  = sd.sd_nlink;
            stat.UID    = sd.sd_uid;
            stat.GID    = sd.sd_gid;

            stat.AccessTimeUtc       = DateHandlers.UnixUnsignedToDateTime(sd.sd_atime);
            stat.LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime(sd.sd_mtime);
            stat.StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime(sd.sd_ctime);

            stat.Attributes = ModeToAttributes(sd.sd_mode);

            // Device files
            var fileType = (ushort)(sd.sd_mode & S_IFMT);

            if(fileType is S_IFCHR or S_IFBLK) stat.DeviceNo = sd.sd_rdev_or_blocks;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Converts a POSIX mode to Aaru FileAttributes</summary>
    static FileAttributes ModeToAttributes(ushort mode)
    {
        var fileType = (ushort)(mode & S_IFMT);

        return fileType switch
               {
                   S_IFDIR  => FileAttributes.Directory,
                   S_IFREG  => FileAttributes.File,
                   S_IFLNK  => FileAttributes.Symlink,
                   S_IFCHR  => FileAttributes.CharDevice,
                   S_IFBLK  => FileAttributes.BlockDevice,
                   S_IFIFO  => FileAttributes.FIFO,
                   S_IFSOCK => FileAttributes.Socket,
                   _        => FileAttributes.None
               };
    }
}