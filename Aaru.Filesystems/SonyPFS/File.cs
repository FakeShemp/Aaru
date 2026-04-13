// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

public partial class SonyPFS
{
    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetFileEntry(path, out DirEntry entry);

        if(err != ErrorNumber.NoError) return err;

        err = ReadInode(entry.Inode, entry.SubPart, out Inode inode);

        if(err != ErrorNumber.NoError) return err;

        stat = new FileEntryInfo
        {
            Inode     = entry.Inode,
            BlockSize = _superBlock.zone_size,
            Blocks    = inode.number_blocks,
            Length    = (long)inode.size,
            Links     = 1,
            UID       = inode.uid,
            GID       = inode.gid,
            Mode      = inode.mode
        };

        stat.Attributes = new CommonTypes.Structs.FileAttributes();

        var fileType = (ushort)(inode.mode & (ushort)FileType.IFMT);

        if(fileType == (ushort)FileType.IFDIR) stat.Attributes |= CommonTypes.Structs.FileAttributes.Directory;

        if((inode.attr & (ushort)FileAttributes.HIDDEN) != 0)
            stat.Attributes |= CommonTypes.Structs.FileAttributes.Hidden;

        DateTimeOffset ctime = PfsDateTimeToDateTimeOffset(inode.ctime);
        DateTimeOffset mtime = PfsDateTimeToDateTimeOffset(inode.mtime);
        DateTimeOffset atime = PfsDateTimeToDateTimeOffset(inode.atime);

        if(ctime != DateTimeOffset.MinValue) stat.CreationTimeUtc = ctime.UtcDateTime;

        if(mtime != DateTimeOffset.MinValue) stat.LastWriteTimeUtc = mtime.UtcDateTime;

        if(atime != DateTimeOffset.MinValue) stat.AccessTimeUtc = atime.UtcDateTime;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetFileEntry(path, out DirEntry entry);

        if(err != ErrorNumber.NoError) return err;

        if((entry.Mode & (ushort)FileType.IFMT) == (ushort)FileType.IFDIR) return ErrorNumber.IsDirectory;

        err = ReadInode(entry.Inode, entry.SubPart, out Inode inode);

        if(err != ErrorNumber.NoError) return err;

        node = new PfsFileNode
        {
            Path        = path,
            Length      = (long)inode.size,
            Offset      = 0,
            InodeData   = inode,
            InodeNumber = entry.Inode,
            SubPart     = entry.SubPart
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not PfsFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not PfsFileNode pfsNode) return ErrorNumber.InvalidArgument;

        if(length < 0) return ErrorNumber.InvalidArgument;

        if(pfsNode.Offset >= pfsNode.Length) return ErrorNumber.NoError;

        // Clamp read length to remaining file size
        if(pfsNode.Offset + length > pfsNode.Length) length = pfsNode.Length - pfsNode.Offset;

        if(length == 0) return ErrorNumber.NoError;

        uint  zoneSize       = _superBlock.zone_size;
        Inode currentSegment = pfsNode.InodeData;
        uint  totalData      = pfsNode.InodeData.number_data;
        long  fileOffset     = pfsNode.Offset;
        long  remaining      = length;
        var   bufferOffset   = 0;

        // Walk the data segments, starting from data[1] (data[0] is the inode block itself)
        // We need to skip past zones until we reach the one containing fileOffset
        long zonesBefore  = fileOffset / zoneSize;
        long skipBytes    = fileOffset % zoneSize;
        long zonesSkipped = 0;

        for(uint dataIndex = 1; dataIndex < totalData && remaining > 0; dataIndex++)
        {
            uint fixedIndex = FixIndex(dataIndex);

            // Follow indirect segment descriptor if needed
            if(fixedIndex == 0 && dataIndex >= PFS_INODE_MAX_BLOCKS)
            {
                if(currentSegment.next_segment.number == 0) break;

                ErrorNumber errno = ReadInode(currentSegment.next_segment.number,
                                              currentSegment.next_segment.subpart,
                                              out currentSegment);

                if(errno != ErrorNumber.NoError) return errno;

                dataIndex++;

                if(dataIndex >= totalData) break;

                fixedIndex = FixIndex(dataIndex);
            }

            BlockInfo bi = currentSegment.data[fixedIndex];

            for(uint offset = 0; offset < bi.count && remaining > 0; offset++)
            {
                // Skip zones before the current offset
                if(zonesSkipped < zonesBefore)
                {
                    zonesSkipped++;

                    continue;
                }

                ErrorNumber err = ReadDataBlock(bi, offset, out byte[] zoneData);

                if(err != ErrorNumber.NoError) return err;

                // Calculate how many bytes to copy from this zone
                var  startInZone = (int)skipBytes;
                long available   = zoneData.Length - startInZone;
                long toCopy      = remaining < available ? remaining : available;

                // Clamp to file size
                if(pfsNode.Offset + (length - remaining) + toCopy > pfsNode.Length)
                    toCopy = pfsNode.Length - (pfsNode.Offset + (length - remaining));

                if(toCopy <= 0) break;

                Array.Copy(zoneData, startInZone, buffer, bufferOffset, toCopy);

                bufferOffset += (int)toCopy;
                remaining    -= toCopy;
                skipBytes    =  0; // Only skip in the first zone
            }
        }

        read           =  length - remaining;
        pfsNode.Offset += read;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber err = GetFileEntry(path, out DirEntry entry);

        if(err != ErrorNumber.NoError) return err;

        err = ReadInode(entry.Inode, entry.SubPart, out Inode inode);

        if(err != ErrorNumber.NoError) return err;

        if((inode.mode & (ushort)FileType.IFMT) != (ushort)FileType.IFLNK) return ErrorNumber.InvalidArgument;

        // Symlink target is stored as raw bytes starting at data[1] in the inode
        // data[0] is the inode's own block; data[1..] onward contains the path string
        // Reinterpret data[1..] as bytes
        int blockInfoSize  = Marshal.SizeOf<BlockInfo>();
        int maxStringBytes = (PFS_INODE_MAX_BLOCKS - 1) * blockInfoSize;
        var linkBytes      = new byte[maxStringBytes];

        for(var i = 1; i < inode.data.Length; i++)
        {
            int offset = (i - 1) * blockInfoSize;

            byte[] biBytes = Helpers.Marshal.StructureToByteArrayLittleEndian(inode.data[i]);

            Array.Copy(biBytes, 0, linkBytes, offset, blockInfoSize);
        }

        // Find null terminator
        int len = Array.IndexOf(linkBytes, (byte)0);

        if(len < 0) len = maxStringBytes;

        dest = Encoding.ASCII.GetString(linkBytes, 0, len);

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a path to a DirEntry by walking the directory tree.</summary>
    ErrorNumber GetFileEntry(string path, out DirEntry entry)
    {
        entry = null;

        if(string.IsNullOrWhiteSpace(path) || path == "/")
        {
            // Root directory itself
            entry = new DirEntry
            {
                Inode   = _superBlock.root.number,
                SubPart = _superBlock.root.subpart,
                Mode    = (ushort)FileType.IFDIR
            };

            return ErrorNumber.NoError;
        }

        string cutPath = path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;

        string[] pieces = cutPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pieces.Length == 0)
        {
            entry = new DirEntry
            {
                Inode   = _superBlock.root.number,
                SubPart = _superBlock.root.subpart,
                Mode    = (ushort)FileType.IFDIR
            };

            return ErrorNumber.NoError;
        }

        Dictionary<string, DirEntry> currentDirectory = _rootDirectoryCache;
        var                          currentPath      = "";

        for(var p = 0; p < pieces.Length; p++)
        {
            KeyValuePair<string, DirEntry> found =
                currentDirectory.FirstOrDefault(t => t.Key.Equals(pieces[p],
                                                                  StringComparison.CurrentCultureIgnoreCase));

            if(string.IsNullOrEmpty(found.Key)) return ErrorNumber.NoSuchFile;

            // Last piece — this is the target
            if(p == pieces.Length - 1)
            {
                entry = found.Value;

                return ErrorNumber.NoError;
            }

            // Intermediate piece must be a directory
            if((found.Value.Mode & (ushort)FileType.IFMT) != (ushort)FileType.IFDIR) return ErrorNumber.NotDirectory;

            currentPath = p == 0 ? pieces[0] : $"{currentPath}/{pieces[p]}";

            if(_directoryCache.TryGetValue(currentPath.ToLower(CultureInfo.CurrentUICulture), out currentDirectory))
                continue;

            ErrorNumber errno = ReadInode(found.Value.Inode, found.Value.SubPart, out Inode dirInode);

            if(errno != ErrorNumber.NoError) return errno;

            currentDirectory = ReadDirectory(dirInode);

            _directoryCache[currentPath.ToLower(CultureInfo.CurrentUICulture)] = currentDirectory;
        }

        return ErrorNumber.NoSuchFile;
    }
}