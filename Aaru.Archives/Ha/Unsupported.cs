using System.Collections.Generic;
using Aaru.CommonTypes.Enums;

namespace Aaru.Archives;

public sealed partial class Ha
{
#region IArchive Members

    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs)
    {
        xattrs = [];

        return ErrorNumber.NotSupported;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer)
    {
        buffer = null;

        return ErrorNumber.NotSupported;
    }

#endregion
}