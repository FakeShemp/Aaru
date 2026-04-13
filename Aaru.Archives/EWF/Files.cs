// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Files.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : EWF logical evidence plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains file access methods for Expert Witness Format logical evidence.
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
using System.IO;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Filters;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Archives;

public sealed partial class EwfArchive
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetFilename(int entryNumber, out string fileName)
    {
        fileName = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        fileName = _entries[entryNumber].FullPath;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetCompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        length = _entries[entryNumber].DataSize;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetUncompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        length = _entries[entryNumber].Size;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetEntryNumber(string fileName, bool caseInsensitiveMatch, out int entryNumber)
    {
        entryNumber = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        StringComparison comparison = caseInsensitiveMatch
                                          ? StringComparison.CurrentCultureIgnoreCase
                                          : StringComparison.CurrentCulture;

        for(var i = 0; i < _entries.Count; i++)
        {
            if(!_entries[i].FullPath.Equals(fileName, comparison)) continue;

            entryNumber = i;

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(int entryNumber, out FileEntryInfo stat)
    {
        stat = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        EwfFileEntry entry = _entries[entryNumber];

        stat = new FileEntryInfo
        {
            Length     = entry.Size,
            Attributes = entry.IsDirectory ? FileAttributes.Directory : FileAttributes.File,
            Blocks     = entry.Size / 512,
            BlockSize  = 512
        };

        if(entry.Size % 512 != 0) stat.Blocks++;

        if(entry.CreationTime != DateTime.MinValue)
        {
            stat.CreationTime    = entry.CreationTime;
            stat.CreationTimeUtc = entry.CreationTime;
        }

        if(entry.ModifyTime != DateTime.MinValue)
        {
            stat.LastWriteTime    = entry.ModifyTime;
            stat.LastWriteTimeUtc = entry.ModifyTime;
        }

        if(entry.AccessTime != DateTime.MinValue)
        {
            stat.AccessTime    = entry.AccessTime;
            stat.AccessTimeUtc = entry.AccessTime;
        }

        // Map EWF flags to file attributes
        if((entry.Flags & (uint)EwfLefEntryFlags.ReadOnly) != 0) stat.Attributes |= FileAttributes.ReadOnly;

        if((entry.Flags & (uint)EwfLefEntryFlags.Hidden) != 0) stat.Attributes |= FileAttributes.Hidden;

        if((entry.Flags & (uint)EwfLefEntryFlags.System) != 0) stat.Attributes |= FileAttributes.System;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetEntry(int entryNumber, out IFilter filter)
    {
        filter = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        EwfFileEntry entry = _entries[entryNumber];

        if(entry.IsDirectory) return ErrorNumber.IsDirectory;

        Stream stream;

        if(entry.Size == 0)
            stream = new MemoryStream([]);
        else
            stream = new EwfFileStream(this, entry.DataOffset, entry.Size);

        filter = new ZZZNoFilter();
        ErrorNumber errno = filter.Open(stream);

        if(errno == ErrorNumber.NoError) return ErrorNumber.NoError;

        stream.Close();

        return errno;
    }

#endregion
}