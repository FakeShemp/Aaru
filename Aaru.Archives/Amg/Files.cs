using Aaru.CommonTypes.Enums;

namespace Aaru.Archives;

public sealed partial class Amg
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber GetFilename(int entryNumber, out string fileName)
    {
        fileName = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _files.Count) return ErrorNumber.OutOfRange;

        fileName = _files[entryNumber].Filename;

        return ErrorNumber.NoError;
    }

#endregion
}