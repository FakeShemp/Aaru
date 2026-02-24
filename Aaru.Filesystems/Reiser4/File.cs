// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Reiser4 filesystem plugin
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

public sealed partial class Reiser4
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
        if(normalizedPath is "/")
        {
            LargeKey rootKey = BuildRootStatDataKey();

            return ReadStatData(rootKey, out stat);
        }

        // Resolve path to a stat-data key
        ErrorNumber errno = ResolvePath(normalizedPath, out LargeKey targetKey);

        if(errno != ErrorNumber.NoError) return errno;

        return ReadStatData(targetKey, out stat);
    }

    /// <summary>Resolves a filesystem path to the stat-data key of the target object</summary>
    /// <param name="path">The path to resolve</param>
    /// <param name="statDataKey">Resolved stat-data key</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ResolvePath(string path, out LargeKey statDataKey)
    {
        statDataKey = default(LargeKey);

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath is "/")
        {
            statDataKey = BuildRootStatDataKey();

            return ErrorNumber.NoError;
        }

        string stripped = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        string[] components = stripped.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(components.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, LargeKey> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < components.Length; i++)
        {
            string component = components[i];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out LargeKey target))
            {
                AaruLogging.Debug(MODULE_NAME, "ResolvePath: '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Last component — found it
            if(i == components.Length - 1)
            {
                statDataKey = target;

                return ErrorNumber.NoError;
            }

            // Intermediate component — must be a directory, descend into it
            ErrorNumber errno = ReadObjectMode(target, out ushort mode);

            if(errno != ErrorNumber.NoError) return errno;

            if((mode & S_IFMT) != S_IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "ResolvePath: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            ulong objectId = GetStatDataObjectId(target);

            errno = ReadDirectoryEntries(objectId, out Dictionary<string, LargeKey> subEntries);

            if(errno != ErrorNumber.NoError) return errno;

            // Filter . and .. for traversal
            var filtered = new Dictionary<string, LargeKey>(StringComparer.Ordinal);

            foreach(KeyValuePair<string, LargeKey> entry in subEntries)
            {
                if(entry.Key is "." or "..") continue;

                filtered[entry.Key] = entry.Value;
            }

            currentEntries = filtered;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>
    ///     Reads the full stat-data for an object given its stat-data key
    ///     and populates a <see cref="FileEntryInfo" />.
    /// </summary>
    ErrorNumber ReadStatData(LargeKey statDataKey, out FileEntryInfo stat)
    {
        stat = null;

        ErrorNumber errno = SearchByKey(statDataKey, out byte[] leafData, out int itemPos);

        if(errno != ErrorNumber.NoError) return errno;

        if(itemPos < 0) return ErrorNumber.NoSuchFile;

        Node40Header nh = Marshal.ByteArrayToStructureLittleEndian<Node40Header>(leafData);

        if(itemPos >= nh.nr_items) return ErrorNumber.NoSuchFile;

        ReadItemHeader(leafData, itemPos, nh.nr_items, out _, out ushort bodyOff, out _, out ushort pluginId);

        if(pluginId != STATIC_STAT_DATA_ID) return ErrorNumber.NoSuchFile;

        int sdLen = GetItemLength(leafData, itemPos, nh.nr_items, nh.free_space_start);

        if(sdLen < Marshal.SizeOf<StatDataBase>()) return ErrorNumber.InvalidArgument;

        StatDataBase sdBase =
            Marshal.ByteArrayToStructureLittleEndian<StatDataBase>(leafData, bodyOff, Marshal.SizeOf<StatDataBase>());

        stat = new FileEntryInfo
        {
            Attributes = FileAttributes.None,
            BlockSize  = _blockSize,
            Inode      = GetStatDataObjectId(statDataKey)
        };

        int sdOff = bodyOff + Marshal.SizeOf<StatDataBase>();

        // Parse light-weight stat extension (mode, nlink, size)
        if((sdBase.extmask & SD_LIGHT_WEIGHT) != 0)
        {
            if(sdOff + Marshal.SizeOf<LightWeightStat>() > leafData.Length) return ErrorNumber.InvalidArgument;

            LightWeightStat lws =
                Marshal.ByteArrayToStructureLittleEndian<LightWeightStat>(leafData,
                                                                          sdOff,
                                                                          Marshal.SizeOf<LightWeightStat>());

            stat.Mode       = (uint)(lws.mode & 0x0FFF);
            stat.Links      = lws.nlink;
            stat.Length     = (long)lws.size;
            stat.Attributes = ModeToAttributes(lws.mode);

            sdOff += Marshal.SizeOf<LightWeightStat>();
        }

        // Parse unix stat extension (uid, gid, timestamps, rdev/bytes)
        if((sdBase.extmask & SD_UNIX) != 0)
        {
            if(sdOff + Marshal.SizeOf<UnixStat>() > leafData.Length) return ErrorNumber.InvalidArgument;

            UnixStat us =
                Marshal.ByteArrayToStructureLittleEndian<UnixStat>(leafData, sdOff, Marshal.SizeOf<UnixStat>());

            stat.UID                 = us.uid;
            stat.GID                 = us.gid;
            stat.AccessTimeUtc       = DateHandlers.UnixUnsignedToDateTime(us.atime);
            stat.LastWriteTimeUtc    = DateHandlers.UnixUnsignedToDateTime(us.mtime);
            stat.StatusChangeTimeUtc = DateHandlers.UnixUnsignedToDateTime(us.ctime);

            // For regular files, rdev_or_bytes holds number of bytes used on disk
            if(stat.Attributes.HasFlag(FileAttributes.File)) stat.Blocks = (long)(us.rdev_or_bytes / _blockSize);

            // For device files, rdev_or_bytes holds the device number
            if(stat.Attributes.HasFlag(FileAttributes.BlockDevice) ||
               stat.Attributes.HasFlag(FileAttributes.CharDevice))
                stat.DeviceNo = us.rdev_or_bytes;

            sdOff += Marshal.SizeOf<UnixStat>();
        }

        // Parse large times extension (sub-second timestamps — we just skip past it)
        if((sdBase.extmask & SD_LARGE_TIMES) != 0) sdOff += Marshal.SizeOf<LargeTimesStat>();

        // Skip symlink extension (bit 3) — variable length, not needed for stat
        // Skip plugin extension (bit 4) — variable length, not needed for stat

        // Parse flags extension
        if((sdBase.extmask & SD_FLAGS) != 0)
        {
            // Must skip symlink (bit 3) and plugin (bit 4) extensions first if present.
            // Since those are variable length and hard to parse without more context,
            // we only attempt flags if neither is present.
            if((sdBase.extmask & (SD_SYMLINK | SD_PLUGIN)) == 0 &&
               sdOff + Marshal.SizeOf<FlagsStat>()         <= leafData.Length)
            {
                FlagsStat fs =
                    Marshal.ByteArrayToStructureLittleEndian<FlagsStat>(leafData, sdOff, Marshal.SizeOf<FlagsStat>());

                if((fs.flags & 0x00000010) != 0) stat.Attributes |= FileAttributes.Immutable;
                if((fs.flags & 0x00000020) != 0) stat.Attributes |= FileAttributes.AppendOnly;
                if((fs.flags & 0x00000040) != 0) stat.Attributes |= FileAttributes.NoDump;
                if((fs.flags & 0x00000080) != 0) stat.Attributes |= FileAttributes.NoAccessTime;
                if((fs.flags & 0x00000008) != 0) stat.Attributes |= FileAttributes.Sync;
            }
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