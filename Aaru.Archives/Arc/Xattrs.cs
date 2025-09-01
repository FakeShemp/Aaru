using System.Collections.Generic;
using System.Text;
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

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer)
    {
        buffer = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        if(xattr != "comment" || _entries[entryNumber].Comment is null) return ErrorNumber.NoSuchExtendedAttribute;

        buffer = Encoding.UTF8.GetBytes(_entries[entryNumber].Comment);

        return ErrorNumber.NoError;
    }

#endregion
}