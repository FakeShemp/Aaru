using System;
using Aaru.CommonTypes.Enums;

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

#endregion
}