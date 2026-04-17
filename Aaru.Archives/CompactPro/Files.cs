using System;
using System.IO;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Compression.CompactPro;
using Aaru.Filters;
using Aaru.Helpers.IO;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Archives;

public sealed partial class CompactPro
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetFilename(int entryNumber, out string fileName)
    {
        fileName = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        Entry entry = _entries[entryNumber];

        fileName = entry.DirectoryPath is not null
                       ? entry.DirectoryPath + "/" + (entry.Filename ?? "")
                       : entry.Filename ?? "";

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

        for(int i = 0, count = _entries.Count; i < count; i++)
        {
            GetFilename(i, out string name);

            if(name is null) continue;

            if(!name.Equals(fileName, comparison)) continue;

            entryNumber = i;

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber GetCompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        length = _entries[entryNumber].DataCompressedSize;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetUncompressedSize(int entryNumber, out long length)
    {
        length = -1;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        length = _entries[entryNumber].DataUncompressedSize;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(int entryNumber, out FileEntryInfo stat)
    {
        stat = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        Entry entry = _entries[entryNumber];

        stat = new FileEntryInfo
        {
            Attributes       = entry.IsDirectory ? FileAttributes.Directory : FileAttributes.File,
            Blocks           = entry.DataUncompressedSize / 512,
            BlockSize        = 512,
            Length           = entry.DataUncompressedSize,
            LastWriteTimeUtc = entry.ModificationTime,
            CreationTimeUtc  = entry.CreationTime
        };

        if(entry.DataUncompressedSize % 512 != 0) stat.Blocks++;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetEntry(int entryNumber, out IFilter filter)
    {
        filter = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        Entry entry = _entries[entryNumber];

        if(entry.IsDirectory) return ErrorNumber.InvalidArgument;

        if(entry.Encrypted) return ErrorNumber.NotSupported;

        Stream stream;

        if(entry.DataUncompressedSize == 0)
            stream = new MemoryStream([]);
        else
        {
            stream = new OffsetStream(new NonClosableStream(_stream),
                                      entry.DataOffset,
                                      entry.DataOffset + entry.DataCompressedSize - 1);

            if(entry.DataLzh)
                stream = new LzhStream(stream, entry.DataUncompressedSize);
            else
                stream = new RleStream(stream, entry.DataUncompressedSize);
        }

        filter = new ZZZNoFilter();
        ErrorNumber errno = filter.Open(stream);

        if(errno == ErrorNumber.NoError) return ErrorNumber.NoError;

        stream.Close();

        return errno;
    }

#endregion
}