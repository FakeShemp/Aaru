using System;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using FileAttributes = System.IO.FileAttributes;

namespace Aaru.Archives;

public sealed partial class Stfs
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetFilename(int entryNumber, out string fileName)
    {
        fileName = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Length) return ErrorNumber.OutOfRange;

        fileName = _entries[entryNumber].Filename;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetEntryNumber(string fileName, bool caseInsensitiveMatch, out int entryNumber)
    {
        entryNumber = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Length) return ErrorNumber.OutOfRange;

        StringComparison comparison = caseInsensitiveMatch
                                          ? StringComparison.CurrentCultureIgnoreCase
                                          : StringComparison.CurrentCulture;

        for(int i = 0, count = _entries.Length; i < count; i++)
        {
            if(!_entries[i].Filename.Equals(fileName, comparison)) continue;

            entryNumber = i;

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }

    public ErrorNumber GetCompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Length) return ErrorNumber.OutOfRange;

        length = _entries[entryNumber].FileSize;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetUncompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Length) return ErrorNumber.OutOfRange;

        length = _entries[entryNumber].FileSize;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetAttributes(int entryNumber, out FileAttributes attributes)
    {
        attributes = FileAttributes.None;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Length) return ErrorNumber.OutOfRange;

        if(_entries[entryNumber].IsDirectory)
            attributes |= FileAttributes.Directory;
        else
            attributes |= FileAttributes.Normal;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(int entryNumber, out FileEntryInfo stat)
    {
        stat = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Length) return ErrorNumber.OutOfRange;

        FileEntry entry = _entries[entryNumber];

        stat = new FileEntryInfo
        {
            Attributes       = CommonTypes.Structs.FileAttributes.None,
            Blocks           = entry.FileSize / 4096,
            BlockSize        = 4096,
            Length           = entry.FileSize,
            LastWriteTime    = entry.LastWrite,
            LastWriteTimeUtc = entry.LastWrite,
            AccessTime       = entry.LastAccess,
            AccessTimeUtc    = entry.LastAccess
        };

        if(entry.FileSize % 4096 != 0) stat.Blocks++;

        if(entry.IsDirectory)
            stat.Attributes |= CommonTypes.Structs.FileAttributes.Directory;
        else
            stat.Attributes |= CommonTypes.Structs.FileAttributes.File;

        return ErrorNumber.NoError;
    }

#endregion
}