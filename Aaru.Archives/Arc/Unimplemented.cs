using System;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Arc
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetEntry(int entryNumber, out IFilter filter) => throw new NotImplementedException();

#endregion
}