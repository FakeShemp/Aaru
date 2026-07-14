// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;

namespace Aaru.Filesystems;

public sealed partial class GDFX
{
    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        stat = _statFs.ShallowCopy();

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(string.IsNullOrEmpty(path) || string.Equals(path, "/", StringComparison.OrdinalIgnoreCase))
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = SECTOR_SIZE,
                Links      = 1
            };

            return ErrorNumber.NoError;
        }

        ErrorNumber errno = ResolveEntry(path, out DecodedEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        FileAttributes attrs = entry.IsDirectory ? FileAttributes.Directory : FileAttributes.File;

        if((entry.Attributes & ATTR_READONLY) != 0) attrs |= FileAttributes.ReadOnly;
        if((entry.Attributes & ATTR_HIDDEN)   != 0) attrs |= FileAttributes.Hidden;
        if((entry.Attributes & ATTR_SYSTEM)   != 0) attrs |= FileAttributes.System;
        if((entry.Attributes & ATTR_ARCHIVE)  != 0) attrs |= FileAttributes.Archive;

        stat = new FileEntryInfo
        {
            Attributes = attrs,
            BlockSize  = SECTOR_SIZE,
            Blocks     = (entry.DataSize + SECTOR_SIZE - 1) / SECTOR_SIZE,
            Length     = entry.DataSize,
            Links      = 1
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        ErrorNumber errno = ResolveEntry(path, out DecodedEntry entry);

        if(errno != ErrorNumber.NoError) return errno;

        if(entry.IsDirectory) return ErrorNumber.IsDirectory;

        node = new GdfxFileNode
        {
            Path        = path,
            Length      = entry.DataSize,
            Offset      = 0,
            StartSector = entry.DataSector
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(node is not GdfxFileNode) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not GdfxFileNode fileNode) return ErrorNumber.InvalidArgument;

        if(length <= 0) return ErrorNumber.InvalidArgument;
        if(buffer is null) return ErrorNumber.InvalidArgument;
        if(buffer.Length   < length) return ErrorNumber.InvalidArgument;
        if(fileNode.Offset >= fileNode.Length) return ErrorNumber.NoError;

        long remaining = fileNode.Length - fileNode.Offset;
        long toRead    = Math.Min(length, remaining);

        var  firstSector = (uint)(fileNode.Offset / SECTOR_SIZE);
        uint sectorCount = (uint)((fileNode.Offset + toRead + SECTOR_SIZE - 1) / SECTOR_SIZE) - firstSector;

        ErrorNumber errno = ReadGameSectors(fileNode.StartSector + firstSector, sectorCount, out byte[] sectorData);

        if(errno != ErrorNumber.NoError) return errno;

        long offsetInFirstSector = fileNode.Offset % SECTOR_SIZE;
        Array.Copy(sectorData, offsetInFirstSector, buffer, 0, toRead);

        fileNode.Offset += toRead;
        read            =  toRead;

        return ErrorNumber.NoError;
    }
}