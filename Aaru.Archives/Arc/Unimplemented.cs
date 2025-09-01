using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using FileAttributes = System.IO.FileAttributes;

namespace Aaru.Archives;

public partial class Arc
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber Open(IFilter filter, Encoding encoding) => throw new NotImplementedException();

    /// <inheritdoc />
    public void Close()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ErrorNumber GetFilename(int entryNumber, out string fileName) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetEntryNumber(string fileName, bool caseInsensitiveMatch, out int entryNumber) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetCompressedSize(int entryNumber, out long length) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetUncompressedSize(int entryNumber, out long length) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetAttributes(int entryNumber, out FileAttributes attributes) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber Stat(int entryNumber, out FileEntryInfo stat) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetEntry(int entryNumber, out IFilter filter) => throw new NotImplementedException();

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        throw new NotImplementedException();
    }

#endregion
}