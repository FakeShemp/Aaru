using System.Collections.Generic;
using Aaru.CommonTypes.Enums;

namespace Aaru.Archives;

public sealed partial class Stfs
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs)
    {
        xattrs = null;

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