using System.Collections.Generic;
using Aaru.CommonTypes.Enums;

namespace Aaru.Archives;

public sealed partial class Arc
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs)
    {
        xattrs = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        xattrs = [];

        if(_entries[entryNumber].Comment is not null) xattrs.Add("comment");

        return ErrorNumber.NoError;
    }

#endregion
}