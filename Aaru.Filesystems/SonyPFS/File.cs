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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

public partial class SonyPFS
{
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

        string cutPath = path.StartsWith("/", StringComparison.Ordinal)
                             ? path[1..]
                             : path;

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
        string                       currentPath      = "";

        for(var p = 0; p < pieces.Length; p++)
        {
            KeyValuePair<string, DirEntry> found =
                currentDirectory.FirstOrDefault(t => t.Key.Equals(pieces[p],
                                                                   StringComparison.CurrentCultureIgnoreCase));

            if(string.IsNullOrEmpty(found.Key))
                return ErrorNumber.NoSuchFile;

            // Last piece — this is the target
            if(p == pieces.Length - 1)
            {
                entry = found.Value;

                return ErrorNumber.NoError;
            }

            // Intermediate piece must be a directory
            if((found.Value.Mode & (ushort)FileType.IFMT) != (ushort)FileType.IFDIR)
                return ErrorNumber.NotDirectory;

            currentPath = p == 0 ? pieces[0] : $"{currentPath}/{pieces[p]}";

            if(_directoryCache.TryGetValue(currentPath.ToLower(CultureInfo.CurrentUICulture),
                                           out currentDirectory))
                continue;

            ErrorNumber errno = ReadInode(found.Value.Inode, found.Value.SubPart, out Inode dirInode);

            if(errno != ErrorNumber.NoError)
                return errno;

            currentDirectory = ReadDirectory(dirInode);

            _directoryCache[currentPath.ToLower(CultureInfo.CurrentUICulture)] = currentDirectory;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted)
            return ErrorNumber.AccessDenied;

        ErrorNumber err = GetFileEntry(path, out DirEntry entry);

        if(err != ErrorNumber.NoError)
            return err;

        err = ReadInode(entry.Inode, entry.SubPart, out Inode inode);

        if(err != ErrorNumber.NoError)
            return err;

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

        stat.Attributes = new Aaru.CommonTypes.Structs.FileAttributes();

        ushort fileType = (ushort)(inode.mode & (ushort)FileType.IFMT);

        if(fileType == (ushort)FileType.IFDIR)
            stat.Attributes |= Aaru.CommonTypes.Structs.FileAttributes.Directory;

        if((inode.attr & (ushort)SonyPFS.FileAttributes.HIDDEN) != 0)
            stat.Attributes |= Aaru.CommonTypes.Structs.FileAttributes.Hidden;

        DateTimeOffset ctime = PfsDateTimeToDateTimeOffset(inode.ctime);
        DateTimeOffset mtime = PfsDateTimeToDateTimeOffset(inode.mtime);
        DateTimeOffset atime = PfsDateTimeToDateTimeOffset(inode.atime);

        if(ctime != DateTimeOffset.MinValue)
            stat.CreationTimeUtc = ctime.UtcDateTime;

        if(mtime != DateTimeOffset.MinValue)
            stat.LastWriteTimeUtc = mtime.UtcDateTime;

        if(atime != DateTimeOffset.MinValue)
            stat.AccessTimeUtc = atime.UtcDateTime;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read) =>
        throw new NotImplementedException();
}