// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class XFS
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
        if(normalizedPath == "/")
        {
            ErrorNumber errno = ReadInode(_superblock.rootino, out Dinode rootInode);

            if(errno != ErrorNumber.NoError) return errno;

            stat = InodeToFileEntryInfo(rootInode, _superblock.rootino);

            return ErrorNumber.NoError;
        }

        // Traverse the path to find the target inode
        ErrorNumber lookupErrno = LookupInode(normalizedPath, out ulong inodeNumber);

        if(lookupErrno != ErrorNumber.NoError) return lookupErrno;

        ErrorNumber readErrno = ReadInode(inodeNumber, out Dinode inode);

        if(readErrno != ErrorNumber.NoError) return readErrno;

        stat = InodeToFileEntryInfo(inode, inodeNumber);

        AaruLogging.Debug(MODULE_NAME,
                          "Stat successful: path='{0}', size={1}, inode={2}, mode=0x{3:X4}",
                          normalizedPath,
                          stat.Length,
                          stat.Inode,
                          stat.Mode);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || path == "/") return ErrorNumber.IsDirectory;

        AaruLogging.Debug(MODULE_NAME, "OpenFile: path='{0}'", path);

        // Verify the file exists and get its stat info
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: Stat failed with {0}", errno);

            return errno;
        }

        // Reject directories
        if(stat.Attributes.HasFlag(FileAttributes.Directory))
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: path is a directory");

            return ErrorNumber.IsDirectory;
        }

        // Look up inode number
        errno = LookupInode(path, out ulong inodeNumber);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LookupInode failed with {0}", errno);

            return errno;
        }

        // Read the inode
        errno = ReadInode(inodeNumber, out Dinode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: ReadInode failed with {0}", errno);

            return errno;
        }

        // Load extent map
        errno = LoadFileExtents(inodeNumber, inode, out XfsExtent[] extents);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "OpenFile: LoadFileExtents failed with {0}", errno);

            return errno;
        }

        node = new XfsFileNode
        {
            Path        = path,
            Length      = inode.di_size,
            Offset      = 0,
            InodeNumber = inodeNumber,
            Inode       = inode,
            Extents     = extents
        };

        AaruLogging.Debug(MODULE_NAME, "OpenFile: success, size={0}, extents={1}", inode.di_size, extents.Length);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not XfsFileNode xfsNode) return ErrorNumber.InvalidArgument;

        xfsNode.Extents = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not XfsFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(buffer == null) return ErrorNumber.InvalidArgument;

        if(fileNode.Offset < 0 || fileNode.Offset >= fileNode.Length) return ErrorNumber.InvalidArgument;

        // Clamp read length to remaining file size and buffer size
        long toRead = length;

        if(fileNode.Offset + toRead > fileNode.Length) toRead = fileNode.Length - fileNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        if(toRead > buffer.Length) toRead = buffer.Length;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: offset={0}, length={1}, toRead={2}", fileNode.Offset, length, toRead);

        // Handle inline (local format) data stored directly in the inode fork
        if(fileNode.Inode.di_format == XFS_DINODE_FMT_LOCAL)
            return ReadInlineFileData(fileNode, toRead, buffer, out read);

        // Normal extent-based file reading
        uint blockSize     = _superblock.blocksize;
        long bytesRead     = 0;
        long currentOffset = fileNode.Offset;

        while(bytesRead < toRead)
        {
            // Calculate the logical file block number and offset within that block
            var logicalBlock  = (ulong)(currentOffset / blockSize);
            var offsetInBlock = (int)(currentOffset   % blockSize);

            // Find the extent containing this logical block
            var found = false;

            foreach(XfsExtent extent in fileNode.Extents)
            {
                if(logicalBlock < extent.StartOff || logicalBlock >= extent.StartOff + extent.BlockCount) continue;

                long bytesToCopy = Math.Min(blockSize - offsetInBlock, toRead - bytesRead);

                // Unwritten (preallocated) extents return zeros
                if(extent.Unwritten)
                {
                    Array.Clear(buffer, (int)bytesRead, (int)bytesToCopy);

                    bytesRead     += bytesToCopy;
                    currentOffset += bytesToCopy;
                    found         =  true;

                    break;
                }

                // Calculate the physical filesystem block
                ulong blockInExtent = logicalBlock      - extent.StartOff;
                ulong physBlock     = extent.StartBlock + blockInExtent;

                ErrorNumber errno = ReadBlock(physBlock, out byte[] blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME, "ReadFile: ReadBlock failed for block {0}: {1}", physBlock, errno);

                    if(bytesRead > 0) goto done;

                    return errno;
                }

                Array.Copy(blockData, offsetInBlock, buffer, bytesRead, bytesToCopy);

                bytesRead     += bytesToCopy;
                currentOffset += bytesToCopy;
                found         =  true;

                break;
            }

            if(!found)
            {
                // Sparse hole — fill with zeros
                long bytesToCopy = Math.Min(blockSize - offsetInBlock, toRead - bytesRead);
                Array.Clear(buffer, (int)bytesRead, (int)bytesToCopy);

                bytesRead     += bytesToCopy;
                currentOffset += bytesToCopy;
            }
        }

    done:
        read            =  bytesRead;
        fileNode.Offset += bytesRead;

        AaruLogging.Debug(MODULE_NAME, "ReadFile: read {0} bytes, new offset={1}", read, fileNode.Offset);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "ReadLink: path='{0}'", path);

        // Verify it exists and is a symlink
        ErrorNumber errno = Stat(path, out FileEntryInfo stat);

        if(errno != ErrorNumber.NoError) return errno;

        if(!stat.Attributes.HasFlag(FileAttributes.Symlink)) return ErrorNumber.InvalidArgument;

        // Look up the inode
        errno = LookupInode(path, out ulong inodeNumber);

        if(errno != ErrorNumber.NoError) return errno;

        // Read the inode
        errno = ReadInode(inodeNumber, out Dinode inode);

        if(errno != ErrorNumber.NoError) return errno;

        if(inode.di_size <= 0 || inode.di_size > 1024)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadLink: invalid symlink size {0}", inode.di_size);

            return ErrorNumber.InvalidArgument;
        }

        // Inline symlink: target stored directly in the inode data fork
        if(inode.di_format == XFS_DINODE_FMT_LOCAL)
        {
            errno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

            if(errno != ErrorNumber.NoError) return errno;

            int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;
            var dataLen  = (int)inode.di_size;

            if(coreSize + dataLen > rawInode.Length) dataLen = rawInode.Length - coreSize;

            dest = _encoding.GetString(rawInode, coreSize, dataLen).TrimEnd('\0');

            return ErrorNumber.NoError;
        }

        // Remote symlink: target stored in extent/btree data blocks
        errno = LoadFileExtents(inodeNumber, inode, out XfsExtent[] extents);

        if(errno != ErrorNumber.NoError) return errno;

        uint blockSize = _superblock.blocksize;
        var  pathLen   = (int)inode.di_size;
        var  linkData  = new byte[pathLen];
        var  offset    = 0;

        // V5 (CRC) filesystems have a symlink header per block
        int headerSize = _v3Inodes ? Marshal.SizeOf<SymlinkHeader>() : 0;

        foreach(XfsExtent extent in extents)
        {
            for(uint b = 0; b < extent.BlockCount && offset < pathLen; b++)
            {
                errno = ReadBlock(extent.StartBlock + b, out byte[] blockData);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "ReadLink: ReadBlock failed for block {0}: {1}",
                                      extent.StartBlock + b,
                                      errno);

                    return errno;
                }

                int dataOffset = headerSize;
                int available  = (int)blockSize - headerSize;

                if(available > pathLen - offset) available = pathLen - offset;

                Array.Copy(blockData, dataOffset, linkData, offset, available);
                offset += available;
            }
        }

        dest = _encoding.GetString(linkData, 0, pathLen).TrimEnd('\0');

        AaruLogging.Debug(MODULE_NAME, "ReadLink: target='{0}'", dest);

        return ErrorNumber.NoError;
    }

    /// <summary>Looks up an inode number by traversing a path from the root directory</summary>
    /// <param name="path">Absolute path to look up</param>
    /// <param name="inodeNumber">Output inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LookupInode(string path, out ulong inodeNumber)
    {
        inodeNumber = 0;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or "." or "/")
        {
            inodeNumber = _superblock.rootino;

            return ErrorNumber.NoError;
        }

        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Start from root directory cache
        Dictionary<string, ulong> currentEntries = _rootDirectoryCache;

        for(var p = 0; p < pathComponents.Length; p++)
        {
            string component = pathComponents[p];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out ulong foundIno))
            {
                AaruLogging.Debug(MODULE_NAME, "LookupInode: Component '{0}' not found", component);

                return ErrorNumber.NoSuchFile;
            }

            // If this is the last component, we found it
            if(p == pathComponents.Length - 1)
            {
                inodeNumber = foundIno;

                return ErrorNumber.NoError;
            }

            // Not the last component — must be a directory, read its contents
            ErrorNumber errno = ReadInode(foundIno, out Dinode inode);

            if(errno != ErrorNumber.NoError) return errno;

            if((inode.di_mode & S_IFMT) != S_IFDIR)
            {
                AaruLogging.Debug(MODULE_NAME, "LookupInode: '{0}' is not a directory", component);

                return ErrorNumber.NotDirectory;
            }

            errno = GetDirectoryContents(foundIno, inode, out Dictionary<string, ulong> dirEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = dirEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>Converts an XFS dinode to a FileEntryInfo structure</summary>
    /// <param name="inode">The dinode</param>
    /// <param name="inodeNumber">The inode number</param>
    /// <returns>The populated FileEntryInfo</returns>
    FileEntryInfo InodeToFileEntryInfo(Dinode inode, ulong inodeNumber)
    {
        var info = new FileEntryInfo
        {
            Attributes          = FileAttributes.None,
            BlockSize           = _superblock.blocksize,
            Inode               = inodeNumber,
            Length              = inode.di_size,
            Links               = inode.di_nlink,
            UID                 = inode.di_uid,
            GID                 = inode.di_gid,
            Mode                = (uint)(inode.di_mode & 0x0FFF),
            Blocks              = inode.di_nblocks,
            AccessTimeUtc       = XfsTimestampToDateTime(inode, inode.di_atime),
            LastWriteTimeUtc    = XfsTimestampToDateTime(inode, inode.di_mtime),
            StatusChangeTimeUtc = XfsTimestampToDateTime(inode, inode.di_ctime)
        };

        // v3 inodes have a creation time
        if(_v3Inodes) info.CreationTimeUtc = XfsTimestampToDateTime(inode, inode.di_crtime);

        // Determine file type from di_mode
        info.Attributes = (inode.di_mode & S_IFMT) switch
                          {
                              0x4000 => FileAttributes.Directory,   // S_IFDIR
                              0x8000 => FileAttributes.File,        // S_IFREG
                              0xA000 => FileAttributes.Symlink,     // S_IFLNK
                              0x2000 => FileAttributes.CharDevice,  // S_IFCHR
                              0x6000 => FileAttributes.BlockDevice, // S_IFBLK
                              0x1000 => FileAttributes.FIFO,        // S_IFIFO
                              0xC000 => FileAttributes.Socket,      // S_IFSOCK
                              _      => FileAttributes.File
                          };

        // XFS inode flags → file attributes
        if((inode.di_flags & XFS_DIFLAG_IMMUTABLE) != 0) info.Attributes |= FileAttributes.Immutable;

        if((inode.di_flags & XFS_DIFLAG_APPEND) != 0) info.Attributes |= FileAttributes.AppendOnly;

        // Extract device major/minor for char and block device inodes
        // The 32-bit SYSV dev_t is stored at the start of the data fork (FMT_DEV)
        if((inode.di_mode & S_IFMT) is 0x2000 or 0x6000 && inode.di_format == XFS_DINODE_FMT_DEV)
        {
            ErrorNumber devErrno = ReadInodeRaw(inodeNumber, out byte[] rawInode);

            if(devErrno == ErrorNumber.NoError)
            {
                int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;

                if(rawInode.Length >= coreSize + 4)
                {
                    var  sysvDev = BigEndianBitConverter.ToUInt32(rawInode, coreSize);
                    uint major   = sysvDev >> 8 & 0xFF;
                    uint minor   = sysvDev      & 0xFF;
                    info.DeviceNo = (ulong)major << 32 | minor;
                }
            }
        }

        return info;
    }

    /// <summary>
    ///     Converts an XFS on-disk timestamp to a DateTime in UTC, handling both legacy and bigtime formats.
    ///     Legacy format: upper 32 bits = seconds (signed), lower 32 bits = nanoseconds.
    ///     Bigtime format: single uint64 of nanoseconds since the bigtime epoch
    ///     (Unix epoch minus 2^31 seconds, i.e. XFS_BIGTIME_EPOCH_OFFSET = -(int64)INT32_MIN = 2147483648).
    /// </summary>
    /// <param name="inode">The dinode (needed to check bigtime flag)</param>
    /// <param name="xfsTimestamp">The packed XFS timestamp</param>
    /// <returns>DateTime in UTC</returns>
    DateTime XfsTimestampToDateTime(Dinode inode, long xfsTimestamp)
    {
        // Bigtime: di_version >= 3 && (di_flags2 & XFS_DIFLAG2_BIGTIME) != 0
        if(_v3Inodes && (inode.di_flags2 & XFS_DIFLAG2_BIGTIME) != 0)
        {
            // The 64-bit value is total nanoseconds since the bigtime epoch.
            // bigtime_epoch = unix_epoch - 2147483648 seconds
            // unix_seconds = (ts / 1_000_000_000) - 2147483648
            var   totalNs     = (ulong)xfsTimestamp;
            ulong seconds     = totalNs / 1_000_000_000UL;
            ulong nanosRem    = totalNs % 1_000_000_000UL;
            long  unixSeconds = (long)seconds - 2147483648L;

            return DateHandlers.UnixToDateTime(unixSeconds).AddTicks((long)nanosRem / 100);
        }

        // Legacy format: upper 32 = signed seconds, lower 32 = unsigned nanoseconds
        var legacySeconds     = (int)(xfsTimestamp >> 32);
        var legacyNanoseconds = (int)(xfsTimestamp & 0xFFFFFFFF);

        return DateHandlers.UnixToDateTime(legacySeconds).AddTicks(legacyNanoseconds / 100);
    }

    /// <summary>Reads data from a file stored inline in the inode data fork (local format)</summary>
    /// <param name="fileNode">The file node</param>
    /// <param name="toRead">Number of bytes to read</param>
    /// <param name="buffer">Output buffer</param>
    /// <param name="read">Actual bytes read</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInlineFileData(XfsFileNode fileNode, long toRead, byte[] buffer, out long read)
    {
        read = 0;

        ErrorNumber errno = ReadInodeRaw(fileNode.InodeNumber, out byte[] rawInode);

        if(errno != ErrorNumber.NoError) return errno;

        int coreSize = _v3Inodes ? Marshal.SizeOf<Dinode>() : 100;
        var dataLen  = (int)fileNode.Inode.di_size;

        if(coreSize + dataLen > rawInode.Length) dataLen = rawInode.Length - coreSize;

        var copySize = (int)Math.Min(toRead, dataLen - fileNode.Offset);

        if(copySize <= 0) return ErrorNumber.NoError;

        Array.Copy(rawInode, coreSize + (int)fileNode.Offset, buffer, 0, copySize);
        read            =  copySize;
        fileNode.Offset += copySize;

        return ErrorNumber.NoError;
    }
}