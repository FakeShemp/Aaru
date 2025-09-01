using System;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Archives;

public sealed partial class Arc
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber Stat(int entryNumber, out FileEntryInfo stat) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetEntry(int entryNumber, out IFilter filter) => throw new NotImplementedException();

#endregion
}