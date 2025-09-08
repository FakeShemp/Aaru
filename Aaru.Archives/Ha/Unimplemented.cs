using System;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using FileAttributes = System.IO.FileAttributes;

namespace Aaru.Archives;

public sealed partial class Ha
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetAttributes(int entryNumber, out FileAttributes attributes) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    /// <inheritdoc />
    public ErrorNumber Stat(int entryNumber, out FileEntryInfo stat) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetEntry(int entryNumber, out IFilter filter) => throw new NotImplementedException();

#endregion
}