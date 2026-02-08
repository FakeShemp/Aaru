// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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
public sealed partial class MinixFS
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

        // Root directory handling
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            // Read root inode
            ErrorNumber errno = ReadInode(ROOT_INODE, out object rootInodeObj);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInodeObj, ROOT_INODE);

            return ErrorNumber.NoError;
        }

        // Remove leading slash
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        // Traverse path to find the file
        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            // Skip "." and ".."
            if(component is "." or "..") continue;

            // Find the component in current directory
            if(!currentEntries.TryGetValue(component, out uint inodeNumber))
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // Read the inode
            ErrorNumber errno = ReadInode(inodeNumber, out object inodeObj);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading inode {0}: {1}", inodeNumber, errno);

                return errno;
            }

            // If this is the last component, return its stat
            if(p == pathComponents.Length - 1)
            {
                stat = InodeToFileEntryInfo(inodeObj, inodeNumber);

                return ErrorNumber.NoError;
            }

            // Not the last component - must be a directory
            ushort mode = _version == FilesystemVersion.V1
                              ? ((V1DiskInode)inodeObj).d1_mode
                              : ((V2DiskInode)inodeObj).d2_mode;

            if((mode & (ushort)InodeMode.TypeMask) != (ushort)InodeMode.Directory)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            // Read directory contents for next iteration
            errno = ReadDirectoryContents(inodeNumber, out Dictionary<string, uint> dirEntries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Stat: Error reading directory contents: {0}", errno);

                return errno;
            }

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts an inode to a FileEntryInfo structure</summary>
    /// <param name="inodeObj">The inode object (V1DiskInode or V2DiskInode)</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The FileEntryInfo structure</returns>
    FileEntryInfo InodeToFileEntryInfo(object inodeObj, uint inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes = FileAttributes.None,
            BlockSize  = _blockSize,
            Inode      = inodeNumber
        };

        ushort mode;
        uint   size;
        ushort uid;
        ushort gid;
        byte   nlinks;
        uint   mtime;
        uint   atime;
        uint   ctime;

        if(_version == FilesystemVersion.V1)
        {
            var inode = (V1DiskInode)inodeObj;
            mode   = inode.d1_mode;
            size   = inode.d1_size;
            uid    = inode.d1_uid;
            gid    = inode.d1_gid;
            nlinks = inode.d1_nlinks;
            mtime  = inode.d1_mtime;

            // V1 only has mtime
            atime = mtime;
            ctime = mtime;
        }
        else
        {
            var inode = (V2DiskInode)inodeObj;
            mode   = inode.d2_mode;
            size   = inode.d2_size;
            uid    = (ushort)inode.d2_uid;
            gid    = inode.d2_gid;
            nlinks = (byte)inode.d2_nlinks;
            mtime  = inode.d2_mtime;
            atime  = inode.d2_atime;
            ctime  = inode.d2_ctime;
        }

        info.Length = size;
        info.Links  = nlinks;
        info.UID    = uid;
        info.GID    = gid;
        info.Mode   = (uint)(mode & (ushort)InodeMode.PermissionMask);

        // Set timestamps
        info.LastWriteTimeUtc    = DateHandlers.UnixToDateTime(mtime);
        info.AccessTimeUtc       = DateHandlers.UnixToDateTime(atime);
        info.StatusChangeTimeUtc = DateHandlers.UnixToDateTime(ctime);
        info.CreationTimeUtc     = DateHandlers.UnixToDateTime(ctime);

        // Determine file type from mode
        var fileType = (InodeMode)(mode & (ushort)InodeMode.TypeMask);

        switch(fileType)
        {
            case InodeMode.Directory:
                info.Attributes = FileAttributes.Directory;

                break;
            case InodeMode.Regular:
                info.Attributes = FileAttributes.File;

                break;
            case InodeMode.SymbolicLink:
                info.Attributes = FileAttributes.Symlink;

                break;
            case InodeMode.CharDevice:
                info.Attributes = FileAttributes.CharDevice;

                // Extract device numbers from first zone
                if(_version == FilesystemVersion.V1)
                {
                    var inode = (V1DiskInode)inodeObj;

                    if(inode.d1_zone is { Length: > 0 })
                    {
                        uint dev   = inode.d1_zone[0];
                        uint major = dev >> 8 & 0xFF;
                        uint minor = dev      & 0xFF;
                        info.DeviceNo = (ulong)major << 32 | minor;
                    }
                }
                else
                {
                    var inode = (V2DiskInode)inodeObj;

                    if(inode.d2_zone is { Length: > 0 })
                    {
                        uint dev   = inode.d2_zone[0];
                        uint major = dev >> 8 & 0xFF;
                        uint minor = dev      & 0xFF;
                        info.DeviceNo = (ulong)major << 32 | minor;
                    }
                }

                break;
            case InodeMode.BlockDevice:
                info.Attributes = FileAttributes.BlockDevice;

                // Extract device numbers from first zone
                if(_version == FilesystemVersion.V1)
                {
                    var inode = (V1DiskInode)inodeObj;

                    if(inode.d1_zone is { Length: > 0 })
                    {
                        uint dev   = inode.d1_zone[0];
                        uint major = dev >> 8 & 0xFF;
                        uint minor = dev      & 0xFF;
                        info.DeviceNo = (ulong)major << 32 | minor;
                    }
                }
                else
                {
                    var inode = (V2DiskInode)inodeObj;

                    if(inode.d2_zone is { Length: > 0 })
                    {
                        uint dev   = inode.d2_zone[0];
                        uint major = dev >> 8 & 0xFF;
                        uint minor = dev      & 0xFF;
                        info.DeviceNo = (ulong)major << 32 | minor;
                    }
                }

                break;
            case InodeMode.Fifo:
                info.Attributes = FileAttributes.Pipe;

                break;
            case InodeMode.Socket:
                info.Attributes = FileAttributes.Socket;

                break;
            default:
                info.Attributes = FileAttributes.File;

                break;
        }

        // Calculate blocks (size / block size, rounded up)
        info.Blocks = (size + _blockSize - 1) / _blockSize;

        return info;
    }
}